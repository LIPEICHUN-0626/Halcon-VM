using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;

namespace HalconWinFormsDemo.Services
{
    public sealed class ModbusTcpRequestS118
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public int TimeoutMs { get; set; }
        public int UnitId { get; set; }
        public int FunctionCode { get; set; }
        public int Address { get; set; }
        public int Quantity { get; set; }
        public int WriteValue { get; set; }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Host) || Host.Length > 255) throw new InvalidOperationException("Modbus 主机不能为空且最多 255 字符。");
            if (Port < 1 || Port > 65535) throw new InvalidOperationException("Modbus TCP 端口须为 1–65535。");
            if (TimeoutMs < 100 || TimeoutMs > 60000) throw new InvalidOperationException("Modbus 超时须为 100–60000 ms。");
            if (UnitId < 0 || UnitId > 255) throw new InvalidOperationException("Unit ID 须为 0–255；网关站号请按设备手册填写。");
            if (Address < 0 || Address > 65535) throw new InvalidOperationException("Modbus 起始地址须为 0–65535 的 PDU 地址，不是 4xxxx 展示编号。");
            if (!new[] { 1, 2, 3, 4, 5, 6 }.Contains(FunctionCode)) throw new InvalidOperationException("当前只支持 Modbus 功能码 01/02/03/04/05/06。");
            if (FunctionCode <= 4)
            {
                int maximum = FunctionCode <= 2 ? 2000 : 125;
                if (Quantity < 1 || Quantity > maximum || Address + Quantity > 65536)
                    throw new InvalidOperationException("本次读取数量须为 1–" + maximum.ToString(CultureInfo.InvariantCulture) + "，且地址范围不能越过 65535。");
            }
            else if (FunctionCode == 5 && WriteValue != 0 && WriteValue != 1)
                throw new InvalidOperationException("写单线圈只接受 0（断开）或 1（闭合）。");
            else if (FunctionCode == 6 && (WriteValue < 0 || WriteValue > 65535))
                throw new InvalidOperationException("写单寄存器只接受 0–65535 的无符号值。");
        }
    }

    public sealed class ModbusTcpResponseS118
    {
        public int TransactionId { get; set; }
        public int FunctionCode { get; set; }
        public int Address { get; set; }
        public double[] Values { get; set; }
        public double RoundTripMs { get; set; }
        public string FrameHex { get; set; }
        public double FirstValue { get { return Values == null || Values.Length == 0 ? 0 : Values[0]; } }
        public bool FirstIsOn { get { return FirstValue != 0; } }
    }

    public sealed class ModbusTcpExceptionS118 : InvalidOperationException
    {
        public int ExceptionCode { get; private set; }
        public ModbusTcpExceptionS118(int code, string message) : base(message) { ExceptionCode = code; }
    }

    public static class ModbusTcpServiceS118
    {
        private static int transactionSequence;

        public static ModbusTcpResponseS118 Execute(ModbusTcpRequestS118 request, Func<bool> stopRequested)
        {
            request.Validate();
            int transaction = Interlocked.Increment(ref transactionSequence) & 0xFFFF;
            byte[] frame = BuildRequest(request, transaction);
            if (stopRequested != null && stopRequested()) throw new OperationCanceledException("Modbus 任务已停止。");
            Stopwatch timer = Stopwatch.StartNew();
            using (TcpClient client = new TcpClient())
            {
                IAsyncResult connection = null;
                try
                {
                    connection = client.BeginConnect(request.Host.Trim(), request.Port, null, null);
                    while (!connection.AsyncWaitHandle.WaitOne(100))
                    {
                        if (stopRequested != null && stopRequested()) throw new OperationCanceledException("Modbus 任务已停止。");
                        if (timer.ElapsedMilliseconds >= request.TimeoutMs) throw new TimeoutException("Modbus TCP 连接超时。");
                    }
                    client.EndConnect(connection);
                }
                finally { if (connection != null) connection.AsyncWaitHandle.Close(); }
                if (stopRequested != null && stopRequested()) throw new OperationCanceledException("Modbus 任务已停止。");
                client.NoDelay = true;
                client.SendTimeout = request.TimeoutMs;
                client.ReceiveTimeout = request.TimeoutMs;
                using (NetworkStream stream = client.GetStream())
                {
                    int sendBudget = request.TimeoutMs - (int)timer.ElapsedMilliseconds;
                    if (sendBudget <= 0) throw new TimeoutException("Modbus 连接耗尽本次超时预算。");
                    stream.WriteTimeout = sendBudget;
                    stream.Write(frame, 0, frame.Length);
                    stream.Flush();
                    byte[] header = ReadExact(stream, 7, stopRequested, timer, request.TimeoutMs);
                    int length = ReadWord(header, 4);
                    if (length < 2 || length > 254) throw new InvalidOperationException("MBAP 长度无效或超过 254 字节上限。");
                    byte[] pdu = ReadExact(stream, length - 1, stopRequested, timer, request.TimeoutMs);
                    byte[] response = new byte[7 + pdu.Length];
                    Buffer.BlockCopy(header, 0, response, 0, 7);
                    Buffer.BlockCopy(pdu, 0, response, 7, pdu.Length);
                    timer.Stop();
                    ModbusTcpResponseS118 result = ParseResponse(response, request, transaction);
                    result.RoundTripMs = timer.Elapsed.TotalMilliseconds;
                    return result;
                }
            }
        }

        public static byte[] BuildRequest(ModbusTcpRequestS118 request, int transactionId)
        {
            request.Validate();
            byte[] frame = new byte[12];
            WriteWord(frame, 0, transactionId);
            WriteWord(frame, 2, 0);
            WriteWord(frame, 4, 6); // Unit ID + five-byte PDU.
            frame[6] = (byte)request.UnitId;
            frame[7] = (byte)request.FunctionCode;
            WriteWord(frame, 8, request.Address);
            int data = request.FunctionCode <= 4 ? request.Quantity :
                request.FunctionCode == 5 ? (request.WriteValue == 1 ? 0xFF00 : 0x0000) : request.WriteValue;
            WriteWord(frame, 10, data);
            return frame;
        }

        public static byte[] BuildSampleResponse(ModbusTcpRequestS118 request, int transactionId)
        {
            request.Validate();
            if (request.FunctionCode >= 5) return BuildRequest(request, transactionId);
            int count = request.FunctionCode <= 2 ? (request.Quantity + 7) / 8 : request.Quantity * 2;
            byte[] frame = new byte[9 + count];
            WriteWord(frame, 0, transactionId);
            WriteWord(frame, 2, 0);
            WriteWord(frame, 4, frame.Length - 6);
            frame[6] = (byte)request.UnitId;
            frame[7] = (byte)request.FunctionCode;
            frame[8] = (byte)count;
            for (int index = 0; index < request.Quantity; index++)
            {
                if (request.FunctionCode <= 2)
                {
                    if (index % 2 == 0) frame[9 + index / 8] |= (byte)(1 << (index % 8));
                }
                else WriteWord(frame, 9 + index * 2, index + 1);
            }
            return frame;
        }

        public static byte[] ParseHexFrame(string text)
        {
            if (text != null && text.Length > 800) throw new InvalidOperationException("示例响应不能超过 800 字符。");
            string compact = Regex.Replace(text ?? string.Empty, @"[\s:\-]", string.Empty);
            if (compact.Length < 18 || compact.Length > 520 || compact.Length % 2 != 0 ||
                !Regex.IsMatch(compact, @"\A[0-9a-fA-F]+\z"))
                throw new InvalidOperationException("示例响应必须是 9–260 字节的十六进制 MBAP 帧。");
            byte[] frame = new byte[compact.Length / 2];
            for (int index = 0; index < frame.Length; index++)
                frame[index] = byte.Parse(compact.Substring(index * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return frame;
        }

        public static ModbusTcpResponseS118 ParseResponse(byte[] response, ModbusTcpRequestS118 request, int transactionId)
        {
            request.Validate();
            if (response == null || response.Length < 9 || response.Length > 260)
                throw new InvalidOperationException("Modbus 响应帧长度无效。");
            if (ReadWord(response, 0) != (transactionId & 0xFFFF)) throw new InvalidOperationException("MBAP 事务号与本次请求不一致，已拒绝旧响应。");
            if (ReadWord(response, 2) != 0) throw new InvalidOperationException("MBAP 协议号不是 0，不是标准 Modbus TCP 响应。");
            int length = ReadWord(response, 4);
            if (length != response.Length - 6 || length < 2 || length > 254)
                throw new InvalidOperationException("MBAP 长度与实际响应帧不一致。");
            if (response[6] != request.UnitId) throw new InvalidOperationException("Unit ID 与本次请求不一致。");
            int function = response[7];
            if (function == (request.FunctionCode | 0x80))
            {
                if (response.Length != 9) throw new InvalidOperationException("Modbus 异常响应长度无效。");
                int code = response[8];
                throw new ModbusTcpExceptionS118(code, "设备返回 Modbus 异常码 0x" + code.ToString("X2", CultureInfo.InvariantCulture) + "（" + ExceptionName(code) + "）。");
            }
            if (function != request.FunctionCode) throw new InvalidOperationException("响应功能码与请求不一致。");
            double[] values;
            if (function <= 4)
            {
                if (response.Length < 9) throw new InvalidOperationException("读取响应缺少字节数。");
                int expectedBytes = function <= 2 ? (request.Quantity + 7) / 8 : request.Quantity * 2;
                if (response[8] != expectedBytes || response.Length != 9 + expectedBytes)
                    throw new InvalidOperationException("读取响应字节数与请求数量不一致。");
                values = new double[request.Quantity];
                for (int index = 0; index < values.Length; index++)
                    values[index] = function <= 2
                        ? (response[9 + index / 8] >> (index % 8)) & 1
                        : ReadWord(response, 9 + index * 2);
            }
            else
            {
                if (response.Length != 12 || ReadWord(response, 8) != request.Address ||
                    ReadWord(response, 10) != (function == 5 ? (request.WriteValue == 1 ? 0xFF00 : 0) : request.WriteValue))
                    throw new InvalidOperationException("单点写入回显与请求地址或数值不一致。");
                values = new[] { (double)request.WriteValue };
            }
            return new ModbusTcpResponseS118
            {
                TransactionId = transactionId & 0xFFFF,
                FunctionCode = function,
                Address = request.Address,
                Values = values,
                FrameHex = BitConverter.ToString(response).Replace("-", " ")
            };
        }

        private static byte[] ReadExact(NetworkStream stream, int count, Func<bool> stopRequested, Stopwatch timer, int timeoutMs)
        {
            byte[] result = new byte[count];
            int offset = 0;
            while (offset < count)
            {
                if (stopRequested != null && stopRequested()) throw new OperationCanceledException("Modbus 任务已停止。");
                int remaining = timeoutMs - (int)timer.ElapsedMilliseconds;
                if (remaining <= 0) throw new TimeoutException("Modbus 本次连接/收发超过总超时。");
                stream.ReadTimeout = Math.Min(remaining, 100);
                int received;
                try { received = stream.Read(result, offset, count - offset); }
                catch (IOException ex)
                {
                    SocketException socket = ex.InnerException as SocketException;
                    if (socket != null && socket.SocketErrorCode == SocketError.TimedOut) continue;
                    throw new IOException("Modbus 响应读取中断：" + ex.Message, ex);
                }
                if (received <= 0) throw new EndOfStreamException("Modbus 设备提前断开；响应帧未收齐。");
                offset += received;
            }
            return result;
        }

        private static int ReadWord(byte[] bytes, int offset) { return (bytes[offset] << 8) | bytes[offset + 1]; }
        private static void WriteWord(byte[] bytes, int offset, int value) { bytes[offset] = (byte)(value >> 8); bytes[offset + 1] = (byte)value; }
        private static string ExceptionName(int code)
        {
            switch (code)
            {
                case 1: return "非法功能";
                case 2: return "非法地址";
                case 3: return "非法数据值";
                case 4: return "设备故障";
                case 6: return "设备忙";
                case 10: return "网关路径不可用";
                case 11: return "网关目标未响应";
                default: return "设备定义异常";
            }
        }
    }
}
