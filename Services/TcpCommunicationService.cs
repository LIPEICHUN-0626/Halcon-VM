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
            serverMode = true;
            cancellation = new CancellationTokenSource();
            IPAddress address = string.IsNullOrWhiteSpace(ipAddress) || ipAddress == "0.0.0.0"
                ? IPAddress.Any
                : IPAddress.Parse(ipAddress);
            listener = new TcpListener(address, port);
            listener.Start();
            IsRunning = true;
            OnStatus("服务端监听已启动：" + address + ":" + port);
            Task.Run(delegate { AcceptLoop(encoding ?? Encoding.UTF8, cancellation.Token); });
        }

        public void ConnectClient(string ipAddress, int port, Encoding encoding)
        {
            Stop();
            serverMode = false;
            cancellation = new CancellationTokenSource();
            IsRunning = true;
            OnStatus("客户端连接中：" + ipAddress + ":" + port);
            Task.Run(delegate
            {
                try
                {
                    TcpClient tcpClient = new TcpClient();
                    tcpClient.Connect(ipAddress, port);
                    SetClient(tcpClient);
                    OnStatus("客户端已连接：" + ipAddress + ":" + port);
                    ReceiveLoop(encoding ?? Encoding.UTF8, cancellation.Token);
                }
                catch (Exception ex)
                {
                    OnError("客户端连接失败", ex);
                    Stop();
                }
            });
        }

        public void Send(string text, Encoding encoding, bool appendNewLine)
        {
            string payload = appendNewLine ? (text ?? string.Empty) + Environment.NewLine : (text ?? string.Empty);
            byte[] bytes = (encoding ?? Encoding.UTF8).GetBytes(payload);
            lock (syncRoot)
            {
                if (stream == null || !stream.CanWrite)
                {
                    throw new InvalidOperationException("TCP 未连接。");
                }

                stream.Write(bytes, 0, bytes.Length);
                stream.Flush();
            }

            OnStatus("发送：" + payload.TrimEnd('\r', '\n'));
        }

        public void Stop()
        {
            CancellationTokenSource token = cancellation;
            cancellation = null;
            if (token != null)
            {
                token.Cancel();
                token.Dispose();
            }

            lock (syncRoot)
            {
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

            if (IsRunning)
            {
                OnStatus(serverMode ? "服务端已停止。" : "客户端已断开。");
            }

            IsRunning = false;
        }

        public void Dispose()
        {
            Stop();
        }

        private void AcceptLoop(Encoding encoding, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    TcpClient accepted = listener.AcceptTcpClient();
                    SetClient(accepted);
                    IPEndPoint endpoint = accepted.Client.RemoteEndPoint as IPEndPoint;
                    OnStatus("客户端已接入：" + (endpoint == null ? "unknown" : endpoint.ToString()));
                    ReceiveLoop(encoding, token);
                    if (serverMode && IsRunning && !token.IsCancellationRequested)
                    {
                        OnStatus("服务端监听中，等待新的客户端接入。");
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
                        OnStatus("服务端监听已结束。");
                    }
                    return;
                }
                catch (Exception ex)
                {
                    OnError("服务端监听异常", ex);
                }
            }
        }

        private void ReceiveLoop(Encoding encoding, CancellationToken token)
        {
            byte[] buffer = new byte[4096];
            while (!token.IsCancellationRequested && CanSend)
            {
                try
                {
                    NetworkStream currentStream;
                    lock (syncRoot)
                    {
                        currentStream = stream;
                    }

                    if (currentStream == null)
                    {
                        return;
                    }

                    int count = currentStream.Read(buffer, 0, buffer.Length);
                    if (count <= 0)
                    {
                        OnStatus("对端已断开连接。");
                        CloseClientOnly();
                        if (!serverMode)
                        {
                            IsRunning = false;
                            OnStatus("客户端已断开。");
                        }
                        return;
                    }

                    string text = encoding.GetString(buffer, 0, count);
                    OnMessage(text);
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    if (!token.IsCancellationRequested)
                    {
                        OnError("TCP 接收异常", ex);
                    }
                    CloseClientOnly();
                    if (!serverMode)
                    {
                        IsRunning = false;
                    }
                    return;
                }
            }
        }

        private void SetClient(TcpClient tcpClient)
        {
            lock (syncRoot)
            {
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
            }
        }

        private void CloseClientOnly()
        {
            lock (syncRoot)
            {
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

        private void OnMessage(string text)
        {
            EventHandler<TcpCommunicationMessageEventArgs> handler = MessageReceived;
            if (handler != null)
            {
                handler(this, new TcpCommunicationMessageEventArgs(text));
            }
        }

        private void OnStatus(string message)
        {
            EventHandler<TcpCommunicationStatusEventArgs> handler = StatusChanged;
            if (handler != null)
            {
                handler(this, new TcpCommunicationStatusEventArgs(message));
            }
        }

        private void OnError(string message, Exception exception)
        {
            EventHandler<TcpCommunicationErrorEventArgs> handler = ErrorOccurred;
            if (handler != null)
            {
                handler(this, new TcpCommunicationErrorEventArgs(message, exception));
            }
        }
    }

    public sealed class TcpCommunicationMessageEventArgs : EventArgs
    {
        public TcpCommunicationMessageEventArgs(string text)
        {
            Text = text;
        }

        public string Text { get; private set; }
    }

    public sealed class TcpCommunicationStatusEventArgs : EventArgs
    {
        public TcpCommunicationStatusEventArgs(string message)
        {
            Message = message;
        }

        public string Message { get; private set; }
    }

    public sealed class TcpCommunicationErrorEventArgs : EventArgs
    {
        public TcpCommunicationErrorEventArgs(string message, Exception exception)
        {
            Message = message;
            Exception = exception;
        }

        public string Message { get; private set; }

        public Exception Exception { get; private set; }
    }
}
