using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HalconWinFormsDemo.Services
{
    public sealed class TcpCommunicationService : IDisposable
    {
        private readonly object syncRoot = new object();
        private TcpListener listener;
        private TcpClient client;
        private NetworkStream stream;
        private CancellationTokenSource cancellation;
        private bool serverMode;
        private TcpClient connectingClient;
        private long sessionVersion;

        public long SessionVersion { get { lock (syncRoot) return sessionVersion; } }

        public event EventHandler<TcpCommunicationMessageEventArgs> MessageReceived;
        public event EventHandler<TcpCommunicationStatusEventArgs> StatusChanged;
        public event EventHandler<TcpCommunicationErrorEventArgs> ErrorOccurred;

        public bool IsRunning { get; private set; }

        public bool IsConnected
        {
            get { return CanSend; }
        }

        public bool CanSend
        {
            get
            {
                lock (syncRoot)
                {
                    return client != null && stream != null && stream.CanWrite;
                }
            }
        }

        public void StartServer(string ipAddress, int port, Encoding encoding)
        {
            Stop();
            IPAddress address = string.IsNullOrWhiteSpace(ipAddress) || ipAddress == "0.0.0.0"
                ? IPAddress.Any
                : IPAddress.Parse(ipAddress);
            TcpListener ownedListener = new TcpListener(address, port);
            CancellationToken token;
            long version;
            lock (syncRoot)
            {
                ownedListener.Start();
                serverMode = true;
                cancellation = new CancellationTokenSource();
                token = cancellation.Token;
                listener = ownedListener;
                version = sessionVersion;
                IsRunning = true;
            }
            OnStatus("服务端监听已启动：" + address + ":" + port, version);
            Task.Run(delegate { AcceptLoop(ownedListener, encoding ?? Encoding.UTF8, token, version); });
        }

        public void ConnectClient(string ipAddress, int port, Encoding encoding)
        {
            Stop();
            TcpClient ownedClient = new TcpClient();
            CancellationToken token;
            long version;
            lock (syncRoot)
            {
                serverMode = false;
                cancellation = new CancellationTokenSource();
                token = cancellation.Token;
                connectingClient = ownedClient;
                version = sessionVersion;
                IsRunning = true;
            }
            OnStatus("客户端连接中：" + ipAddress + ":" + port, version);
            Task.Run(delegate
            {
                try
                {
                    if (token.IsCancellationRequested) return;
                    ownedClient.Connect(ipAddress, port);
                    NetworkStream ownedStream;
                    if (!TrySetClient(ownedClient, token, version, out ownedStream)) return;
                    OnStatus("客户端已连接：" + ipAddress + ":" + port, version);
                    ReceiveLoop(ownedClient, ownedStream, encoding ?? Encoding.UTF8, token, version, false);
                }
                catch (Exception ex)
                {
                    long stoppedVersion = StopSession(version);
                    if (stoppedVersion >= 0) OnError("客户端连接失败", ex, stoppedVersion);
                }
                finally { CloseClientOnly(ownedClient, version); ownedClient.Close(); }
            });
        }

        public void Send(string text, Encoding encoding, bool appendNewLine, int writeTimeoutMs = 0)
        {
            string payload = appendNewLine ? (text ?? string.Empty) + Environment.NewLine : (text ?? string.Empty);
            byte[] bytes = (encoding ?? Encoding.UTF8).GetBytes(payload);
            long version;
            lock (syncRoot)
            {
                version = sessionVersion;
                if (stream == null || !stream.CanWrite)
                {
                    throw new InvalidOperationException("TCP 未连接。");
                }

                int previousTimeout = stream.WriteTimeout;
                try
                {
                    if (writeTimeoutMs > 0) stream.WriteTimeout = writeTimeoutMs;
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush();
                }
                finally
                {
                    if (stream != null && stream.CanWrite) stream.WriteTimeout = previousTimeout;
                }
            }

            OnStatus("发送：" + payload.TrimEnd('\r', '\n'), version);
        }

        public void Stop()
        {
            StopSession(null);
        }

        // A worker may only finish its own session, never a later user connection.
        private long StopSession(long? expectedVersion)
        {
            CancellationTokenSource token;
            bool wasRunning, wasServer;
            long stoppedVersion;
            lock (syncRoot)
            {
                if (expectedVersion.HasValue && expectedVersion.Value != sessionVersion) return -1;
                stoppedVersion = ++sessionVersion;
                token = cancellation; cancellation = null;
                wasRunning = IsRunning; wasServer = serverMode;
                IsRunning = false;
                if (connectingClient != null) { connectingClient.Close(); connectingClient = null; }
                if (stream != null)
                {
                    stream.Dispose();
                    stream = null;
                }

                if (client != null)
                {
                    client.Close();
                    client = null;
                }

                if (listener != null)
                {
                    listener.Stop();
                    listener = null;
                }
            }
            if (token != null) { token.Cancel(); token.Dispose(); }
            if (wasRunning) OnStatus(wasServer ? "服务端已停止。" : "客户端已断开。", stoppedVersion);
            return stoppedVersion;
        }

        public void Dispose()
        {
            Stop();
        }

        private void AcceptLoop(TcpListener ownedListener, Encoding encoding, CancellationToken token, long version)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    TcpClient accepted = ownedListener.AcceptTcpClient();
                    try
                    {
                        NetworkStream ownedStream;
                        if (!TrySetClient(accepted, token, version, out ownedStream)) return;
                        IPEndPoint endpoint = accepted.Client.RemoteEndPoint as IPEndPoint;
                        OnStatus("客户端已接入：" + (endpoint == null ? "unknown" : endpoint.ToString()), version);
                        ReceiveLoop(accepted, ownedStream, encoding, token, version, true);
                    }
                    finally { CloseClientOnly(accepted, version); accepted.Close(); }
                    if (!token.IsCancellationRequested)
                    {
                        OnStatus("服务端监听中，等待新的客户端接入。", version);
                    }
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (SocketException)
                {
                    if (!token.IsCancellationRequested)
                    {
                        OnStatus("服务端监听已结束。", version);
                    }
                    return;
                }
                catch (Exception ex)
                {
                    if (!token.IsCancellationRequested) OnError("服务端监听异常", ex, version);
                }
            }
        }

        private void ReceiveLoop(TcpClient ownedClient, NetworkStream ownedStream, Encoding encoding,
            CancellationToken token, long version, bool ownedServerMode)
        {
            byte[] buffer = new byte[4096];
            while (!token.IsCancellationRequested)
            {
                try
                {
                    lock (syncRoot)
                    {
                        if (sessionVersion != version || !ReferenceEquals(client, ownedClient)) return;
                    }
                    int count = ownedStream.Read(buffer, 0, buffer.Length);
                    if (count <= 0)
                    {
                        CloseClientOnly(ownedClient, version);
                        OnStatus("对端已断开连接。", version);
                        if (!ownedServerMode)
                        {
                            lock (syncRoot) { if (sessionVersion == version) IsRunning = false; }
                            OnStatus("客户端已断开。", version);
                        }
                        return;
                    }

                    string text = encoding.GetString(buffer, 0, count);
                    OnMessage(text, version);
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    if (!token.IsCancellationRequested)
                    {
                        OnError("TCP 接收异常", ex, version);
                    }
                    CloseClientOnly(ownedClient, version);
                    if (!ownedServerMode)
                    {
                        lock (syncRoot) { if (sessionVersion == version) IsRunning = false; }
                    }
                    return;
                }
            }
        }

        private bool TrySetClient(TcpClient tcpClient, CancellationToken token, long version, out NetworkStream ownedStream)
        {
            ownedStream = null;
            lock (syncRoot)
            {
                if (sessionVersion != version || token.IsCancellationRequested || !IsRunning)
                { tcpClient.Close(); return false; }
                if (stream != null)
                {
                    stream.Dispose();
                }

                if (client != null)
                {
                    client.Close();
                }

                client = tcpClient;
                stream = tcpClient.GetStream();
                ownedStream = stream;
                if (ReferenceEquals(connectingClient, tcpClient)) connectingClient = null;
                return true;
            }
        }

        private void CloseClientOnly(TcpClient ownedClient, long version)
        {
            lock (syncRoot)
            {
                if (sessionVersion != version || !ReferenceEquals(client, ownedClient)) return;
                if (stream != null)
                {
                    stream.Dispose();
                    stream = null;
                }

                if (client != null)
                {
                    client.Close();
                    client = null;
                }
            }
        }

        private void OnMessage(string text, long version)
        {
            lock (syncRoot)
            {
                if (version != sessionVersion) return;
                EventHandler<TcpCommunicationMessageEventArgs> handler = MessageReceived;
                if (handler != null) handler(this, new TcpCommunicationMessageEventArgs(text, version));
            }
        }

        private void OnStatus(string message, long? version = null)
        {
            lock (syncRoot)
            {
                long current = version ?? sessionVersion;
                if (current != sessionVersion) return;
                EventHandler<TcpCommunicationStatusEventArgs> handler = StatusChanged;
                if (handler != null) handler(this, new TcpCommunicationStatusEventArgs(message, current));
            }
        }

        private void OnError(string message, Exception exception, long version)
        {
            lock (syncRoot)
            {
                if (version != sessionVersion) return;
                EventHandler<TcpCommunicationErrorEventArgs> handler = ErrorOccurred;
                if (handler != null) handler(this, new TcpCommunicationErrorEventArgs(message, exception, version));
            }
        }
    }

    public sealed class TcpCommunicationMessageEventArgs : EventArgs
    {
        public TcpCommunicationMessageEventArgs(string text, long sessionVersion = 0)
        {
            Text = text;
            SessionVersion = sessionVersion;
        }

        public string Text { get; private set; }
        public long SessionVersion { get; private set; }
    }

    public sealed class TcpCommunicationStatusEventArgs : EventArgs
    {
        public TcpCommunicationStatusEventArgs(string message, long sessionVersion = 0)
        {
            Message = message;
            SessionVersion = sessionVersion;
        }

        public string Message { get; private set; }
        public long SessionVersion { get; private set; }
    }

    public sealed class TcpCommunicationErrorEventArgs : EventArgs
    {
        public TcpCommunicationErrorEventArgs(string message, Exception exception, long sessionVersion = 0)
        {
            Message = message;
            Exception = exception;
            SessionVersion = sessionVersion;
        }

        public string Message { get; private set; }

        public Exception Exception { get; private set; }
        public long SessionVersion { get; private set; }
    }
}
