using System;

namespace HalconWinFormsDemo.Models
{
    public sealed class RuntimeStatistics
    {
        public int TotalCount { get; private set; }

        public int OkCount { get; private set; }

        public int NgCount { get; private set; }

        public int ConsecutiveFailCount { get; private set; }

        public double LastCycleMilliseconds { get; private set; }

        public string LastNgReason { get; private set; }

        public double YieldRate
        {
            get { return TotalCount == 0 ? 0 : OkCount * 100.0 / TotalCount; }
        }

        public void Record(string resultCode, double cycleMilliseconds, string message)
        {
            TotalCount++;
            LastCycleMilliseconds = cycleMilliseconds;

            if (string.Equals(resultCode, "OK", StringComparison.OrdinalIgnoreCase))
            {
                OkCount++;
                ConsecutiveFailCount = 0;
                return;
            }

            NgCount++;
            ConsecutiveFailCount++;
            LastNgReason = message;
        }

        public void Reset()
        {
            TotalCount = 0;
            OkCount = 0;
            NgCount = 0;
            ConsecutiveFailCount = 0;
            LastCycleMilliseconds = 0;
            LastNgReason = string.Empty;
        }
    }
}
