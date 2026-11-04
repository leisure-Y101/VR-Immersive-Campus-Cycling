# -*- coding: utf-8 -*-
"""Think A102 (A1) BLE -> UDP bridge for Unity.

PC 端桥接：连接骑行台，订阅 Cycling Power Measurement (0x2A63)，
把瞬时功率按 ASCII float 发到 127.0.0.1:50506（Unity PanoBikeSpeed 监听）。

依赖：D:\\freebike\\Tools\\deps\\.pydeps-ble（bleak + winrt，Python 3.14）
运行：python a1_bridge.py
"""

import asyncio
import socket
import sys
import time

DEPS = r"D:\freebike\Tools\deps\.pydeps-ble"
if DEPS not in sys.path:
    sys.path.insert(0, DEPS)

from bleak import BleakClient, BleakScanner

CPS_MEASUREMENT = "00002a63-0000-1000-8000-00805f9b34fb"
CPS_SERVICE = "00001818-0000-1000-8000-00805f9b34fb"
UDP_ADDR = ("127.0.0.1", 50506)
TARGET_ADDRESS = "F5:79:6C:84:32:07"
NAME_HINTS = ("think a102", "think a1", "thinkrider")

sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
started = time.monotonic()


def decode_power(data: bytearray):
    """Cycling Power Measurement: flags(2) + instantaneous power(2, sint16)."""
    if len(data) < 4:
        return None
    flags = int.from_bytes(data[0:2], "little")
    power = int.from_bytes(data[2:4], "little", signed=True)
    return flags, power


def on_notify(_sender, data: bytearray):
    decoded = decode_power(data)
    if decoded is None:
        return
    flags, power = decoded
    sock.sendto(f"{power:.1f}".encode("ascii"), UDP_ADDR)
    print(
        f"[{time.monotonic() - started:7.1f}s] power={power:5d} W  flags=0x{flags:04x}"
        f"  raw={bytes(data).hex(' ')}",
        flush=True,
    )


async def scan_trainer():
    print("Scanning BLE ...", flush=True)
    try:
        devices = await BleakScanner.discover(timeout=8, return_adv=True)
    except Exception as exc:
        print(f"Scan failed: {exc!r}", flush=True)
        return None

    for device, adv in devices.values():
        name = device.name or adv.local_name or ""
        services = [u.lower() for u in (adv.service_uuids or [])]
        if (
            device.address.lower() == TARGET_ADDRESS.lower()
            or any(hint in name.lower() for hint in NAME_HINTS)
            or CPS_SERVICE in services
        ):
            print(f"Found {name!r} {device.address} rssi={adv.rssi}", flush=True)
            return device

    print("Trainer not found (is it awake and not connected to a phone?).", flush=True)
    return None


async def connect_trainer():
    try:
        client = BleakClient(TARGET_ADDRESS, timeout=20)
        await client.connect()
        print(f"Connected {TARGET_ADDRESS}", flush=True)
        return client
    except Exception as exc:
        print(f"Direct connect failed: {exc!r}", flush=True)

    device = await scan_trainer()
    if device is None:
        return None

    try:
        client = BleakClient(device, timeout=20)
        await client.connect()
        print(f"Connected {device.address}", flush=True)
        return client
    except Exception as exc:
        print(f"Connect failed: {exc!r}", flush=True)
        return None


async def run():
    while True:
        client = await connect_trainer()
        if client is None:
            await asyncio.sleep(3)
            continue

        try:
            await client.start_notify(CPS_MEASUREMENT, on_notify)
            print(
                f"Subscribed 0x2A63 -> udp {UDP_ADDR[0]}:{UDP_ADDR[1]}  (Ctrl+C to stop)",
                flush=True,
            )
            while client.is_connected:
                await asyncio.sleep(1)
            print("Disconnected", flush=True)
        except Exception as exc:
            print(f"Bridge error: {exc!r}", flush=True)
        finally:
            try:
                await client.disconnect()
            except Exception:
                pass

        await asyncio.sleep(2)


if __name__ == "__main__":
    try:
        asyncio.run(run())
    except KeyboardInterrupt:
        print("Stopped.", flush=True)
    finally:
        sock.close()
