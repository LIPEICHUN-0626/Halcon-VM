using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace HalconWinFormsDemo.Services
{
    public sealed class TcpFlowClientRequest
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public int ConnectTimeoutMilliseconds { get; set; }
        public int SendTimeoutMilliseconds { get; set; }
        public int ReceiveTimeoutMilliseconds { get; set; }
        public string EncodingName { get; set; }
        public string LineEnding { get; set; }
        public string Message { get; set; }
        public bool WaitForResponse { get; set; }
    }

    public sealed class TcpFlowClientResult
    {
        public bool Connected { get; set; }
        public string SentText { get; set; }
        public string ReceivedText { get; set; }
        public long RoundTripMilliseconds { get; set; }
        public int BytesSent { get; set; }
        public int BytesReceived { get; set; }
    }

    public static class TcpFlowClientService
    {
        public static TcpFlowClientResult Execute(TcpFlowClientRequest request, Func<bool> stopRequested)
        {
            if (request == null) throw new ArgumentNullException("request");
            if (string.IsNullOrWhiteSpace(request.Host)) throw new ArgumentException("TCP 主机不能为空。", "request");
            if (request.Port < 1 || request.Port > 65535) throw new ArgumentOutOfRangeException("request", "TCP 端口必须是 1 到 65535。");
            if (request.ConnectTimeoutMilliseconds < 1 || request.SendTimeoutMilliseconds < 1 || request.ReceiveTimeoutMilliseconds < 1)
                throw new ArgumentOutOfRangeException("request", "TCP 超时必须大于 0 毫秒。");

            ThrowIfStopped(stopRequested);
            Encoding encoding = ResolveEncoding(request.EncodingName);
            string terminator = ResolveLineEnding(request.LineEnding);
            string sentText = (request.Message ?? string.Empty) + terminator;
            byte[] sendBuffer = encoding.GetBytes(sentText);
            Stopwatch stopwatch = Stopwatch.StartNew();

            using (TcpClient client = new TcpClient())
            {
                IAsyncResult connect = null;
                try
                {
                    connect = client.BeginConnect(request.Host.Trim(), request.Port, null, null);
                    while (!connect.AsyncWaitHandle.WaitOne(50))
                    {
                        ThrowIfStopped(stopRequested);
                        if (stopwatch.ElapsedMilliseconds >= request.ConnectTimeoutMilliseconds)
                            throw new TimeoutException("TCP 连接超时。");
                    }
                    client.EndConnect(connect);
                }
                finally
                {
                    if (connect != null) connect.AsyncWaitHandle.Close();
                }

                ThrowIfStopped(stopRequested);
                client.SendTimeout = request.SendTimeoutMilliseconds;
                client.ReceiveTimeout = request.ReceiveTimeoutMilliseconds;
                client.NoDelay = true;

                using (NetworkStream stream = client.GetStream())
                {
                    stream.Write(sendBuffer, 0, sendBuffer.Length);
                    stream.Flush();
                    ThrowIfStopped(stopRequested);

                    byte[] received = request.WaitForResponse
                        ? ReceiveResponse(client, stream, terminator, request.ReceiveTimeoutMilliseconds, stopRequested)
                        : new byte[0];
                    stopwatch.Stop();
                    return new TcpFlowClientResult
                    {
                        Connected = true,
                        SentText = sentText,
                        ReceivedText = encoding.GetString(received),
                        RoundTripMilliseconds = stopwatch.ElapsedMilliseconds,
                        BytesSent = sendBuffer.Length,
                        BytesReceived = received.Length
                    };
                }
            }
        }

        private static byte[] ReceiveResponse(TcpClient client, NetworkStream stream, string terminator, int timeoutMilliseconds, Func<bool> stopRequested)
        {
            Stopwatch wait = Stopwatch.StartNew();
            byte[] buffer = new byte[4096];
            using (System.IO.MemoryStream received = new System.IO.MemoryStream())
            {
                while (wait.ElapsedMilliseconds < timeoutMilliseconds)
                {
                    ThrowIfStopped(stopRequested);
                    if (stream.DataAvailable)
                    {
                        int count = stream.Read(buffer, 0, buffer.Length);
                        if (count <= 0) break;
                        received.Write(buffer, 0, count);
                        if (received.Length > 1024 * 1024) throw new InvalidOperationException("TCP 响应超过 1 MiB 安全上限。");
                        if (!string.IsNullOrEmpty(terminator) && EndsWith(received, Encoding.UTF8.GetBytes(terminator))) break;
                        continue;
                    }

                    if (client.Client.Poll(1000, SelectMode.SelectRead) && client.Available == 0) break;
                    Thread.Sleep(5);
                }

                if (received.Length == 0 && wait.ElapsedMilliseconds >= timeoutMilliseconds)
                    throw new TimeoutException("TCP 等待响应超时。");
                return received.ToArray();
            }
        }

        private static bool EndsWith(System.IO.MemoryStream stream, byte[] suffix)
        {
            if (suffix == null || suffix.Length == 0 || stream.Length < suffix.Length) return false;
            byte[] data = stream.GetBuffer();
            long start = stream.Length - suffix.Length;
            for (int index = 0; index < suffix.Length; index++)
            {
                if (data[start + index] != suffix[index]) return false;
            }
            return true;
        }

        private static Encoding ResolveEncoding(string name)
        {
            return string.Equals(name, "ASCII", StringComparison.OrdinalIgnoreCase)
                ? Encoding.ASCII
                : new UTF8Encoding(false, true);
        }

        private static string ResolveLineEnding(string value)
        {
            if (string.Equals(value, "CR", StringComparison.OrdinalIgnoreCase)) return "\r";
            if (string.Equals(value, "LF", StringComparison.OrdinalIgnoreCase)) return "\n";
            if (string.Equals(value, "CRLF", StringComparison.OrdinalIgnoreCase)) return "\r\n";
            return string.Empty;
        }

        private static void ThrowIfStopped(Func<bool> stopRequested)
        {
            if (stopRequested != null && stopRequested()) throw new OperationCanceledException("TCP 流程工具已停止。");
        }
    }
}
