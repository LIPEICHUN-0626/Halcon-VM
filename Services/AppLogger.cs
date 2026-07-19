using System;
using System.IO;
using System.Text;

namespace HalconWinFormsDemo.Services
{
    public sealed class AppLogger : IDisposable
    {
        private readonly object syncRoot = new object();
        private readonly string logDirectory;

        public AppLogger()
            : this(AppDomain.CurrentDomain.BaseDirectory)
        {
        }

        public AppLogger(string baseDirectory)
        {
            if (string.IsNullOrWhiteSpace(baseDirectory))
            {
                throw new ArgumentException("日志基础目录不能为空。", "baseDirectory");
            }

            logDirectory = Path.Combine(Path.GetFullPath(baseDirectory), "logs");
            Directory.CreateDirectory(logDirectory);
        }

        public string LogDirectory
        {
            get { return logDirectory; }
        }

        public event EventHandler<LogMessageEventArgs> MessageLogged;

        public void Info(string message)
        {
            Write("INFO", message);
        }

        public void Error(string message, Exception exception)
        {
            string detail = exception == null ? message : message + " - " + exception.Message;
            Write("ERROR", detail);
        }

        public void Dispose()
        {
        }

        private void Write(string level, string message)
        {
            string line = string.Format("{0:yyyy-MM-dd HH:mm:ss.fff} [{1}] {2}", DateTime.Now, level, message);

            lock (syncRoot)
            {
                string filePath = Path.Combine(logDirectory, DateTime.Now.ToString("yyyyMMdd") + ".log");
                File.AppendAllText(filePath, line + Environment.NewLine, Encoding.UTF8);
            }

            EventHandler<LogMessageEventArgs> handler = MessageLogged;
            if (handler != null)
            {
                handler(this, new LogMessageEventArgs(line, level));
            }
        }
    }

    public sealed class LogMessageEventArgs : EventArgs
    {
        public LogMessageEventArgs(string message, string level)
        {
            Message = message;
            Level = level;
        }

        public string Message { get; private set; }

        public string Level { get; private set; }
    }
}
