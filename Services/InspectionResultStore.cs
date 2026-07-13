using System;
using System.Collections.Generic;
using System.Linq;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo.Services
{
    public sealed class InspectionResultStore : IDisposable
    {
        private readonly List<InspectionRecord> records = new List<InspectionRecord>();
        private int nextId = 1;

        public event EventHandler Changed;

        public IReadOnlyList<InspectionRecord> Records
        {
            get { return records; }
        }

        public InspectionRecord Add(InspectionRecord record)
        {
            record.Id = nextId++;
            records.Add(record);
            OnChanged();
            return record;
        }

        public IEnumerable<InspectionRecord> Query(DateTime? startTime, DateTime? endTime, string resultCode, string imageSource)
        {
            IEnumerable<InspectionRecord> query = records;

            if (startTime.HasValue)
            {
                query = query.Where(record => record.Timestamp >= startTime.Value);
            }

            if (endTime.HasValue)
            {
                query = query.Where(record => record.Timestamp <= endTime.Value);
            }

            if (!string.IsNullOrWhiteSpace(resultCode))
            {
                query = query.Where(record => string.Equals(record.ResultCode, resultCode.Trim(), StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(imageSource))
            {
                query = query.Where(record => record.ImageSource != null &&
                                              record.ImageSource.IndexOf(imageSource.Trim(), StringComparison.OrdinalIgnoreCase) >= 0);
            }

            return query.OrderByDescending(record => record.Timestamp).ToList();
        }

        public void Clear()
        {
            foreach (InspectionRecord record in records)
            {
                record.Dispose();
            }

            records.Clear();
            nextId = 1;
            OnChanged();
        }

        public void Dispose()
        {
            Clear();
        }

        private void OnChanged()
        {
            EventHandler handler = Changed;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }
    }
}
