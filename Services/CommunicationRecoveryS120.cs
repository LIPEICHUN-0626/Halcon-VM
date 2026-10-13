using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace HalconWinFormsDemo.Services
{
    public sealed class CommunicationRecoveryPolicyS120
    {
        public int AdditionalReadAttempts { get; set; }
        public int BackoffMs { get; set; }
        public int TotalBudgetMs { get; set; }

        public void Validate()
        {
            if (AdditionalReadAttempts < 0 || AdditionalReadAttempts > 3)
                throw new InvalidOperationException("读取失败后的额外尝试次数须为 0–3。");
            if (BackoffMs < 0 || BackoffMs > 2000)
                throw new InvalidOperationException("读取重试间隔须为 0–2000 ms。");
            if (TotalBudgetMs < 100 || TotalBudgetMs > 120000)
                throw new InvalidOperationException("通讯任务总预算须为 100–120000 ms。");
        }
    }

    public sealed class CommunicationAttemptS120
    {
        public int Index { get; set; }
        public double ElapsedMs { get; set; }
        public string Status { get; set; }
        public string Detail { get; set; }
    }

    public sealed class CommunicationRecoveryResultS120<T>
    {
        public T Value { get; set; }
        public Exception Error { get; set; }
        public IList<CommunicationAttemptS120> Attempts { get; set; }
        public int AttemptCount { get { return Attempts == null ? 0 : Attempts.Count; } }
        public string Trace
        {
            get
            {
                if (Attempts == null || Attempts.Count == 0) return "未尝试";
                var lines = new List<string>();
                foreach (CommunicationAttemptS120 attempt in Attempts)
                    lines.Add("#" + attempt.Index + " " + attempt.Status + " " + attempt.ElapsedMs.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + " ms" +
                        (string.IsNullOrWhiteSpace(attempt.Detail) ? string.Empty : " · " + attempt.Detail));
                return string.Join("；", lines);
            }
        }
    }

    public static class CommunicationRecoveryS120
    {
        public static CommunicationRecoveryResultS120<T> Execute<T>(
            Func<int, T> singleAttempt, bool readOnly, CommunicationRecoveryPolicyS120 policy, Func<bool> stopRequested)
        {
            if (singleAttempt == null) throw new ArgumentNullException("singleAttempt");
            if (policy == null) throw new ArgumentNullException("policy");
            policy.Validate();
            int maximum = readOnly ? policy.AdditionalReadAttempts + 1 : 1;
            var result = new CommunicationRecoveryResultS120<T> { Attempts = new List<CommunicationAttemptS120>() };
            Stopwatch total = Stopwatch.StartNew();
            for (int index = 1; index <= maximum; index++)
            {
                if (stopRequested != null && stopRequested()) throw new OperationCanceledException("通讯任务已停止，不再重试。");
                int remaining = policy.TotalBudgetMs - (int)total.ElapsedMilliseconds;
                if (remaining < 100)
                {
                    result.Error = new TimeoutException("通讯任务总预算已耗尽，不再重试。");
                    break;
                }
                Stopwatch attemptTimer = Stopwatch.StartNew();
                try
                {
                    result.Value = singleAttempt(remaining);
                    result.Error = null;
                    result.Attempts.Add(new CommunicationAttemptS120 { Index = index, ElapsedMs = attemptTimer.Elapsed.TotalMilliseconds, Status = "成功" });
                    return result;
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    result.Error = ex;
                    result.Attempts.Add(new CommunicationAttemptS120
                    {
                        Index = index, ElapsedMs = attemptTimer.Elapsed.TotalMilliseconds,
                        Status = "失败", Detail = ex.Message
                    });
                    if (!readOnly || index == maximum || !IsRetryable(ex)) break;
                    int delay = policy.BackoffMs;
                    if (policy.TotalBudgetMs - (int)total.ElapsedMilliseconds - delay < 100) break;
                    while (delay > 0)
                    {
                        if (stopRequested != null && stopRequested()) throw new OperationCanceledException("通讯任务已停止，不再重试。");
                        int slice = Math.Min(delay, 50);
                        Thread.Sleep(slice);
                        delay -= slice;
                    }
                }
            }
            return result;
        }

        private static bool IsRetryable(Exception error)
        {
            return error is TimeoutException || error is IOException || error is SocketException || error is ModbusRtuFrameExceptionS119;
        }
    }
}
