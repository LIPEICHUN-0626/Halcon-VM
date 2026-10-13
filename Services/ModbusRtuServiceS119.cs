using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace HalconWinFormsDemo.Services
{
    public sealed class ModbusRtuRequestS119
    {
        public string PortName { get; set; }
        public int BaudRate { get; set; }
        public string ParityName { get; set; }
        public int StopBitCount { get; set; }
        public int TimeoutMs { get; set; }
        public int Station { get; set; }
        public int FunctionCode { get; set; }
        public int Address { get; set; }
        public int Quantity { get; set; }
        public int WriteValue { get; set; }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(PortName) || !Regex.IsMatch(PortName.Trim(), @"\ACOM[1-9][0-9]{0,3}\z", RegexOptions.IgnoreCase))
                throw new InvalidOperationException("串口名须为 COM1、COM2 等 Windows 串口名。");
            if (BaudRate < 300 || BaudRate > 921600) throw new InvalidOperationException("波特率须为 300–921600，且应与设备一致。");
            if (ParityName != "None" && ParityName != "Even" && ParityName != "Odd") throw new InvalidOperationException("校验位只支持无、偶、奇。");
            if (StopBitCount != 1 && StopBitCount != 2) throw new InvalidOperationException("停止位只支持 1 或 2。");
            if (TimeoutMs < 100 || TimeoutMs > 60000) throw new InvalidOperationException("RTU 响应超时须为 100–60000 ms。");
            if (Station < 1 || Station > 247) throw new InvalidOperationException("RTU 站号须为 1–247；广播站号 0 不等待响应，本工具不支持。");
            if (Address < 0 || Address > 65535) throw new InvalidOperationException("RTU 地址须为零起始 PDU 地址 0–65535。");
            if (FunctionCode < 1 || FunctionCode > 6) throw new InvalidOperationException("RTU 目前仅支持功能码 01–06。");
            if (FunctionCode <= 4)
            {
                int limit = FunctionCode <= 2 ? 2000 : 125;
                if (Quantity < 1 || Quantity > limit || Address + Quantity > 65536)
                    throw new InvalidOperationException("读取数量须为 1–" + limit.ToString(CultureInfo.InvariantCulture) + " 且不能越过末地址。");
            }
            else if (FunctionCode == 5 && WriteValue != 0 && WriteValue != 1)
                throw new InvalidOperationException("写单线圈只接受 0/1。");
            else if (FunctionCode == 6 && (WriteValue < 0 || WriteValue > 65535))
                throw new InvalidOperationException("写单寄存器只接受 0–65535。");
        }

        public Parity GetParity()
        {
            return ParityName == "Even" ? Parity.Even : ParityName == "Odd" ? Parity.Odd : Parity.None;
        }
    }

    public sealed class ModbusRtuResponseS119
    {
        public int FunctionCode { get; set; }
        public double[] Values { get; set; }
        public double RoundTripMs { get; set; }
        public string FrameHex { get; set; }
        public double FirstValue { get { return Values == null || Values.Length == 0 ? 0 : Values[0]; } }
        public bool FirstIsOn { get { return FirstValue != 0; } }
    }

    public sealed class ModbusRtuExceptionS119 : InvalidOperationException
    {
        public int ExceptionCode { get; private set; }
        public ModbusRtuExceptionS119(int code) : base("设备返回 RTU 异常码 0x" + code.ToString("X2", CultureInfo.InvariantCulture) + "。")
        {
            ExceptionCode = code;
        }
    }

    public sealed class ModbusRtuFrameExceptionS119 : InvalidOperationException
    {
        public ModbusRtuFrameExceptionS119(string message) : base(message) { }
    }

    public static class ModbusRtuServiceS119
    {
        private static readonly object portGate = new object();
        private static readonly HashSet<string> reservedPorts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public static bool IsReserved(string portName)
        {
            lock (portGate) return !string.IsNullOrWhiteSpace(portName) && reservedPorts.Contains(portName.Trim());
        }

        public static ModbusRtuResponseS119 Execute(ModbusRtuRequestS119 request, Func<bool> stopRequested)
        {
            request.Validate();
            string portName = request.PortName.Trim().ToUpperInvariant();
            lock (portGate)
            {
                if (!reservedPorts.Add(portName)) throw new InvalidOperationException(portName + " 正被本程序另一任务使用。");
            }
            try
            {
                if (stopRequested != null && stopRequested()) throw new OperationCanceledException("RTU 任务已停止。");
                Stopwatch timer = Stopwatch.StartNew();
                using (SerialPort port = new SerialPort(portName, request.BaudRate, request.GetParity(), 8,
                    request.StopBitCount == 2 ? StopBits.Two : StopBits.One))
                {
                    port.Handshake = Handshake.None;
                    port.ReadTimeout = Math.Min(request.TimeoutMs, 100);
                    port.WriteTimeout = request.TimeoutMs;
                    port.Open();
                    port.DiscardInBuffer();
                    int gapMs = request.BaudRate > 19200 ? 2 : Math.Max(2, (int)Math.Ceiling(3500.0 * (1 + 8 + (request.ParityName == "None" ? 0 : 1) + request.StopBitCount) / request.BaudRate));
                    Thread.Sleep(gapMs);
                    if (stopRequested != null && stopRequested()) throw new OperationCanceledException("RTU 任务已停止。");
                    int sendBudget = request.TimeoutMs - (int)timer.ElapsedMilliseconds;
                    if (sendBudget <= 0) throw new TimeoutException("RTU 串口打开已耗尽本次超时预算。");
                    port.WriteTimeout = sendBudget;
                    byte[] command = BuildRequest(request);
                    port.Write(command, 0, command.Length);
                    List<byte> response = new List<byte>(256);
                    response.AddRange(ReadExact(port, 2, timer, request.TimeoutMs, stopRequested));
                    if (response[0] != request.Station) throw new InvalidOperationException("RTU 响应站号与本次请求不一致。");
                    int function = response[1];
                    if (function == (request.FunctionCode | 0x80)) response.AddRange(ReadExact(port, 3, timer, request.TimeoutMs, stopRequested));
                    else if (function == request.FunctionCode && function <= 4)
                    {
                        byte[] count = ReadExact(port, 1, timer, request.TimeoutMs, stopRequested);
                        response.Add(count[0]);
                        int expected = function <= 2 ? (request.Quantity + 7) / 8 : request.Quantity * 2;
                        if (count[0] != expected) throw new InvalidOperationException("RTU 响应字节数与本次读取数量不一致。");
                        response.AddRange(ReadExact(port, count[0] + 2, timer, request.TimeoutMs, stopRequested));
                    }
                    else if (function == request.FunctionCode) response.AddRange(ReadExact(port, 6, timer, request.TimeoutMs, stopRequested));
                    else throw new InvalidOperationException("RTU 响应功能码与请求不一致。");
                    timer.Stop();
                    ModbusRtuResponseS119 result = ParseResponse(response.ToArray(), request);
                    result.RoundTripMs = timer.Elapsed.TotalMilliseconds;
                    return result;
                }
            }
            finally { lock (portGate) reservedPorts.Remove(portName); }
        }

        public static byte[] BuildRequest(ModbusRtuRequestS119 request)
        {
            request.Validate();
            byte[] frame = new byte[8];
            frame[0] = (byte)request.Station;
            frame[1] = (byte)request.FunctionCode;
            WriteWord(frame, 2, request.Address);
            WriteWord(frame, 4, request.FunctionCode <= 4 ? request.Quantity :
                request.FunctionCode == 5 ? (request.WriteValue == 1 ? 0xFF00 : 0) : request.WriteValue);
            WriteCrc(frame);
            return frame;
        }

        public static byte[] BuildSampleResponse(ModbusRtuRequestS119 request)
        {
            request.Validate();
            if (request.FunctionCode >= 5) return BuildRequest(request);
            int count = request.FunctionCode <= 2 ? (request.Quantity + 7) / 8 : request.Quantity * 2;
            byte[] frame = new byte[5 + count];
            frame[0] = (byte)request.Station;
            frame[1] = (byte)request.FunctionCode;
            frame[2] = (byte)count;
            for (int index = 0; index < request.Quantity; index++)
            {
                if (request.FunctionCode <= 2)
                {
                    if (index % 2 == 0) frame[3 + index / 8] |= (byte)(1 << (index % 8));
                }
                else WriteWord(frame, 3 + index * 2, index + 1);
            }
            WriteCrc(frame);
            return frame;
        }

        public static ModbusRtuResponseS119 ParseResponse(byte[] frame, ModbusRtuRequestS119 request)
        {
            request.Validate();
            if (frame == null || frame.Length < 5 || frame.Length > 256) throw new InvalidOperationException("RTU 响应长度必须为 5–256 字节。");
            int crc = CalculateCrc(frame, frame.Length - 2);
            if (frame[frame.Length - 2] != (byte)crc || frame[frame.Length - 1] != (byte)(crc >> 8))
                throw new ModbusRtuFrameExceptionS119("RTU CRC 校验失败，已拒绝本次数据。");
            if (frame[0] != request.Station) throw new InvalidOperationException("RTU 站号与本次请求不一致。");
            int function = frame[1];
            if (function == (request.FunctionCode | 0x80))
            {
                if (frame.Length != 5) throw new InvalidOperationException("RTU 异常帧长度无效。");
                throw new ModbusRtuExceptionS119(frame[2]);
            }
            if (function != request.FunctionCode) throw new InvalidOperationException("RTU 功能码与本次请求不一致。");
            double[] values;
            if (function <= 4)
            {
                int expected = function <= 2 ? (request.Quantity + 7) / 8 : request.Quantity * 2;
                if (frame[2] != expected || frame.Length != 5 + expected) throw new InvalidOperationException("RTU 数据字节数与读取数量不一致。");
                values = new double[request.Quantity];
                for (int index = 0; index < values.Length; index++)
                    values[index] = function <= 2 ? (frame[3 + index / 8] >> (index % 8)) & 1 : ReadWord(frame, 3 + index * 2);
            }
            else
            {
                int expectedValue = function == 5 ? (request.WriteValue == 1 ? 0xFF00 : 0) : request.WriteValue;
                if (frame.Length != 8 || ReadWord(frame, 2) != request.Address || ReadWord(frame, 4) != expectedValue)
                    throw new InvalidOperationException("RTU 单点写入回显与请求不一致。");
                values = new[] { (double)request.WriteValue };
            }
            return new ModbusRtuResponseS119 { FunctionCode = function, Values = values, FrameHex = BitConverter.ToString(frame).Replace("-", " ") };
        }

        public static byte[] ParseHexFrame(string text)
        {
            if (text != null && text.Length > 800) throw new InvalidOperationException("示例帧不能超过 800 字符。");
            string compact = Regex.Replace(text ?? string.Empty, @"[\s:\-]", string.Empty);
            if (compact.Length < 10 || compact.Length > 512 || compact.Length % 2 != 0 || !Regex.IsMatch(compact, @"\A[0-9a-fA-F]+\z"))
                throw new InvalidOperationException("示例 RTU 响应须为 5–256 字节的十六进制帧。");
            byte[] frame = new byte[compact.Length / 2];
            for (int index = 0; index < frame.Length; index++) frame[index] = byte.Parse(compact.Substring(index * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return frame;
        }

        private static byte[] ReadExact(SerialPort port, int count, Stopwatch timer, int timeoutMs, Func<bool> stopRequested)
        {
            byte[] bytes = new byte[count];
            int offset = 0;
            while (offset < count)
            {
                if (stopRequested != null && stopRequested()) throw new OperationCanceledException("RTU 任务已停止。");
                int remaining = timeoutMs - (int)timer.ElapsedMilliseconds;
                if (remaining <= 0) throw new TimeoutException("RTU 响应未在总超时内收齐。");
                port.ReadTimeout = Math.Min(remaining, 100);
                try
                {
                    int read = port.Read(bytes, offset, count - offset);
                    if (read > 0) offset += read;
                }
                catch (TimeoutException) { /* Poll stop flag and total deadline. */ }
            }
            return bytes;
        }

        private static int CalculateCrc(byte[] bytes, int count)
        {
            int crc = 0xFFFF;
            for (int index = 0; index < count; index++)
            {
                crc ^= bytes[index];
                for (int bit = 0; bit < 8; bit++) crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xA001 : crc >> 1;
            }
            return crc & 0xFFFF;
        }

        private static void WriteCrc(byte[] bytes)
        {
            int crc = CalculateCrc(bytes, bytes.Length - 2);
            bytes[bytes.Length - 2] = (byte)crc;
            bytes[bytes.Length - 1] = (byte)(crc >> 8);
        }

        private static int ReadWord(byte[] bytes, int offset) { return (bytes[offset] << 8) | bytes[offset + 1]; }
        private static void WriteWord(byte[] bytes, int offset, int value) { bytes[offset] = (byte)(value >> 8); bytes[offset + 1] = (byte)value; }
    }
}
