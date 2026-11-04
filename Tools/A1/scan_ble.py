# -*- coding: utf-8 -*-
import asyncio
import sys

sys.path.insert(0, r"D:\freebike\Tools\deps\pydeps")

from bleak import BleakScanner


async def main():
    print("Scanning BLE for 15 seconds...")
    try:
        devices = await BleakScanner.discover(timeout=15, return_adv=True)
    except Exception as exc:
        print("SCAN_ERROR:", repr(exc))
        return

    print(f"FOUND {len(devices)} devices")
    rows = []
    for address, (device, adv) in devices.items():
        rows.append((adv.rssi if adv.rssi is not None else -999, address, device.name or adv.local_name or "", adv))

    for rssi, address, name, adv in sorted(rows, reverse=True):
        services = ",".join(adv.service_uuids or [])
        manufacturer = ";".join(
            f"{k}:{bytes(v).hex()}" for k, v in (adv.manufacturer_data or {}).items()
        )
        print(f"{rssi:4d} dBm  {address}  name={name!r}  services=[{services}]  mfr=[{manufacturer}]")


if __name__ == "__main__":
    asyncio.run(main())
