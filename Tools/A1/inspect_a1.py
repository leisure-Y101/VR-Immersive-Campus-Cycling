# -*- coding: utf-8 -*-
import asyncio
import sys
import time

sys.path.insert(0, r"D:\freebike\Tools\deps\pydeps")

from bleak import BleakClient

ADDRESS = "F5:79:6C:84:32:07"
CPS_MEASUREMENT = "00002a63-0000-1000-8000-00805f9b34fb"
INDOOR_BIKE_DATA = "00002ad2-0000-1000-8000-00805f9b34fb"


def decode_cps(data: bytearray):
    if len(data) < 4:
        return None
    flags = int.from_bytes(data[0:2], "little")
    power = int.from_bytes(data[2:4], "little", signed=True)
    return flags, power


async def main():
    print(f"Connecting {ADDRESS} ...")
    async with BleakClient(ADDRESS, timeout=15) as client:
        print("CONNECTED", client.is_connected)
        for service in client.services:
            print(f"SERVICE {service.uuid}  {service.description}")
            for char in service.characteristics:
                print(f"  CHAR {char.uuid}  props={','.join(char.properties)}  {char.description}")

        targets = []
        for service in client.services:
            for char in service.characteristics:
                uuid = char.uuid.lower()
                if uuid in (CPS_MEASUREMENT, INDOOR_BIKE_DATA):
                    targets.append(char)

        if not targets:
            print("NO known cycling-power / indoor-bike characteristic")
            return

        def on_notify(char, data: bytearray):
            stamp = time.strftime("%H:%M:%S")
            text = data.hex(" ")
            extra = ""
            if char.uuid.lower() == CPS_MEASUREMENT:
                decoded = decode_cps(data)
                if decoded:
                    flags, power = decoded
                    extra = f"  flags=0x{flags:04x} power={power} W"
            print(f"{stamp}  {char.uuid}  {text}{extra}")

        for char in targets:
            print(f"SUBSCRIBE {char.uuid}")
            await client.start_notify(char, on_notify)

        print("Listening 20 seconds; pedal the bike now.")
        await asyncio.sleep(20)

        for char in targets:
            try:
                await client.stop_notify(char)
            except Exception:
                pass

        print("DONE")


if __name__ == "__main__":
    asyncio.run(main())
