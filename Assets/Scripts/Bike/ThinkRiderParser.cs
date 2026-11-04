using System;

// ThinkRider A1 标准 BLE 数据解析（按 Bluetooth SIG 规范实现，与厂商 App 无关）
// - FTMS 0x1826 -> Indoor Bike Data 0x2AD2: 速度/踏频/功率/阻力/心率
// - CPS  0x1818 -> Cycling Power Measurement 0x2A63: 瞬时功率 + 曲柄/车轮圈数
public static class ThinkRiderParser
{
    // 安全的按小端读取器，所有读取带越界检查
    struct Reader
    {
        readonly byte[] d;
        readonly int len;
        public int pos;

        public Reader(byte[] data, int length)
        {
            d = data;
            len = Math.Min(length, data != null ? data.Length : 0);
            pos = 0;
        }

        public bool CanRead(int n) { return d != null && pos + n <= len; }

        public bool U8(out byte v)
        {
            v = 0;
            if (!CanRead(1)) return false;
            v = d[pos++];
            return true;
        }

        public bool U16(out ushort v)
        {
            v = 0;
            if (!CanRead(2)) return false;
            v = (ushort)(d[pos] | (d[pos + 1] << 8));
            pos += 2;
            return true;
        }

        public bool S16(out short v)
        {
            v = 0;
            if (!U16(out ushort u)) return false;
            v = (short)u;
            return true;
        }

        public bool U24(out int v)
        {
            v = 0;
            if (!CanRead(3)) return false;
            v = d[pos] | (d[pos + 1] << 8) | (d[pos + 2] << 16);
            pos += 3;
            return true;
        }

        public bool U32(out uint v)
        {
            v = 0;
            if (!CanRead(4)) return false;
            v = (uint)(d[pos] | (d[pos + 1] << 8) | (d[pos + 2] << 16) | (d[pos + 3] << 24));
            pos += 4;
            return true;
        }

        public bool Skip(int n)
        {
            if (!CanRead(n)) return false;
            pos += n;
            return true;
        }
    }

    // ============================================================
    // FTMS Indoor Bike Data (0x2AD2)
    // 字段按 flags 顺序出现，未置位的字段必须跳过
    // ============================================================
    public static bool TryParseIndoorBikeData(byte[] data, int length, ref BikeTelemetry telemetry)
    {
        var r = new Reader(data, length);
        if (!r.U16(out ushort flags)) return false;

        // bit0 瞬时速度 0.01 km/h
        if ((flags & 0x0001) != 0)
        {
            if (!r.U16(out ushort speed)) return false;
            telemetry.speedKph = speed * 0.01f;
        }

        // bit1 平均速度
        if ((flags & 0x0002) != 0 && !r.Skip(2)) return false;

        // bit2 瞬时踏频 0.5 rpm
        if ((flags & 0x0004) != 0)
        {
            if (!r.U16(out ushort cadence)) return false;
            telemetry.cadenceRpm = cadence * 0.5f;
        }

        // bit3 平均踏频
        if ((flags & 0x0008) != 0 && !r.Skip(2)) return false;

        // bit4 总距离 uint24 米
        if ((flags & 0x0010) != 0 && !r.Skip(3)) return false;

        // bit5 阻力等级 sint16
        if ((flags & 0x0020) != 0)
        {
            if (!r.S16(out short resistance)) return false;
            telemetry.resistanceLevel = resistance;
        }

        // bit6 瞬时功率 sint16 W
        if ((flags & 0x0040) != 0)
        {
            if (!r.S16(out short power)) return false;
            telemetry.powerWatts = power;
        }

        // bit7 平均功率
        if ((flags & 0x0080) != 0 && !r.Skip(2)) return false;

        // bit8 消耗能量 uint16 + uint16 + uint8
        if ((flags & 0x0100) != 0 && !r.Skip(5)) return false;

        // bit9 心率 uint8
        if ((flags & 0x0200) != 0)
        {
            if (!r.U8(out byte hr)) return false;
            telemetry.heartRate = hr;
        }

        // bit10 代谢当量 uint8
        if ((flags & 0x0400) != 0 && !r.Skip(1)) return false;

        // bit11 已用时间 uint16 s
        if ((flags & 0x0800) != 0 && !r.Skip(2)) return false;

        // bit12 剩余时间 uint16 s
        if ((flags & 0x1000) != 0 && !r.Skip(2)) return false;

        return true;
    }

    // ============================================================
    // CPS Cycling Power Measurement (0x2A63)
    // 瞬时功率永远存在；踏频/速度由曲柄/车轮累计圈数与事件时间推算
    // ============================================================
    public struct CyclingPowerFrame
    {
        public bool valid;
        public short powerWatts;
        public bool hasCrankData;
        public ushort crankRevs;
        public ushort crankTime;   // 1/1024 s
        public bool hasWheelData;
        public uint wheelRevs;
        public ushort wheelTime;   // 1/1024 s
    }

    public static CyclingPowerFrame ParseCyclingPower(byte[] data, int length)
    {
        var frame = new CyclingPowerFrame();
        var r = new Reader(data, length);
        if (!r.U16(out ushort flags)) return frame;

        // A1 实测（2026-10-21）：flags=0x0820 时固件把瞬时功率写在「曲柄圈数」槽、
        // 累计能量写在最后一槽，中间两槽恒 0。按标准解析会把累计能量当成功率（一路涨到 1200W）。
        if ((flags & 0x0820) == 0x0820 && length >= 10 &&
            data[4] == 0 && data[5] == 0 && data[6] == 0 && data[7] == 0)
        {
            frame.powerWatts = (short)(data[2] | (data[3] << 8));
            frame.valid = true;
            return frame;
        }

        // bit0 踏板平衡 uint8
        if ((flags & 0x0001) != 0 && !r.Skip(1)) return frame;
        // bit2 累计扭矩 uint16
        if ((flags & 0x0004) != 0 && !r.Skip(2)) return frame;

        // bit4 车轮圈数 uint32 + 时间 uint16
        if ((flags & 0x0010) != 0)
        {
            if (!r.U32(out uint wheelRevs)) return frame;
            if (!r.U16(out ushort wheelTime)) return frame;
            frame.hasWheelData = true;
            frame.wheelRevs = wheelRevs;
            frame.wheelTime = wheelTime;
        }

        // bit5 曲柄圈数 uint16 + 时间 uint16
        if ((flags & 0x0020) != 0)
        {
            if (!r.U16(out ushort crankRevs)) return frame;
            if (!r.U16(out ushort crankTime)) return frame;
            frame.hasCrankData = true;
            frame.crankRevs = crankRevs;
            frame.crankTime = crankTime;
        }

        // bit6 极限力 uint16 * 2
        if ((flags & 0x0040) != 0 && !r.Skip(4)) return frame;
        // bit7 极限扭矩 uint16 * 2
        if ((flags & 0x0080) != 0 && !r.Skip(4)) return frame;
        // bit8 极限角度 uint16 * 2
        if ((flags & 0x0100) != 0 && !r.Skip(4)) return frame;
        // bit9/10 上下止点角度 uint16 * 2
        if ((flags & 0x0200) != 0 && !r.Skip(2)) return frame;
        if ((flags & 0x0400) != 0 && !r.Skip(2)) return frame;
        // bit11 累计能量 uint16
        if ((flags & 0x0800) != 0 && !r.Skip(2)) return frame;

        // 瞬时功率，始终存在
        if (!r.S16(out short power)) return frame;
        frame.powerWatts = power;
        frame.valid = true;
        return frame;
    }

    // 曲柄累计圈数 -> 踏频 RPM（处理 16 位回绕）
    public class CadenceFromCrank
    {
        bool hasPrev;
        ushort prevRevs;
        ushort prevTime;

        public float Update(ushort revs, ushort time)
        {
            if (!hasPrev)
            {
                hasPrev = true;
                prevRevs = revs;
                prevTime = time;
                return 0f;
            }

            int dRev = (revs - prevRevs) & 0xFFFF;
            int dTime = (time - prevTime) & 0xFFFF;
            prevRevs = revs;
            prevTime = time;

            if (dTime == 0) return 0f;
            double seconds = dTime / 1024.0;
            return (float)(dRev / seconds * 60.0);
        }
    }

    // 车轮累计圈数 -> 速度 km/h（默认 700x25c 周长 2.105 m）
    public class SpeedFromWheel
    {
        bool hasPrev;
        uint prevRevs;
        ushort prevTime;
        public float wheelCircumference = 2.105f;

        public float Update(uint revs, ushort time)
        {
            if (!hasPrev)
            {
                hasPrev = true;
                prevRevs = revs;
                prevTime = time;
                return 0f;
            }

            uint dRev = (uint)((revs - prevRevs) & 0xFFFFFFFF);
            int dTime = (time - prevTime) & 0xFFFF;
            prevRevs = revs;
            prevTime = time;

            if (dTime == 0) return 0f;
            double seconds = dTime / 1024.0;
            double meters = dRev * (double)wheelCircumference;
            return (float)(meters / seconds * 3.6);
        }
    }
}
