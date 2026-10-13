using System;
using System.Collections.Generic;
using System.Linq;
using HalconDotNet;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo.Services
{
    public sealed class ConnectedObjectResultS144 : IDisposable
    {
        public List<VmConnectedObjectRowS144> Rows { get; } = new List<VmConnectedObjectRowS144>();
        public HObject Regions { get; internal set; }
        public HObject SelectedRegion { get; internal set; }
        public int ConnectedCount { get; internal set; }
        public double TotalArea { get; internal set; }
        public void Dispose()
        {
            if (Regions != null) { Regions.Dispose(); Regions = null; }
            if (SelectedRegion != null) { SelectedRegion.Dispose(); SelectedRegion = null; }
        }
    }

    public static class ConnectedObjectServiceS144
    {
        public const int MaxInputObjects = 256, MaxConnectedObjects = 1000;
        public const double MaxInputArea = 100000;

        // Borrowed SDK object: budgets are checked before copying or asking HALCON
        // for per-component arrays. Sum includes overlaps deliberately.
        public static double ValidateBudget(HObject region, int maximumObjects = MaxInputObjects)
        {
            if (region == null) throw new InvalidOperationException("Region 来源为空。");
            HTuple count = null, areas = null, rows = null, columns = null;
            HTuple r1 = null, c1 = null, r2 = null, c2 = null;
            try
            {
                HOperatorSet.CountObj(region, out count);
                int n = count.I;
                if (n < 0 || n > maximumObjects) throw new InvalidOperationException("Region 对象数超出预算（输入256，连接结果1000）；不截断对象。请在上游合并或限定检测域。");
                if (n == 0) return 0;
                HOperatorSet.AreaCenter(region, out areas, out rows, out columns);
                if (areas.Length != n || rows.Length != n || columns.Length != n) throw new InvalidOperationException("Region 面积/中心数组不完整。");
                double sum = 0;
                for (int i = 0; i < n; i++)
                {
                    double area = areas[i].D;
                    if (!VmImageEdgeLineParametersS143.Finite(area) || area < 0 || area > MaxInputArea - sum)
                        throw new InvalidOperationException("输入累计区域面积最多100000 px²（重叠也计预算），不对超预算数据先复制再截断。请缩小上游检测域。");
                    sum += area;
                }
                if (sum == 0) return 0;
                HOperatorSet.SmallestRectangle1(region, out r1, out c1, out r2, out c2);
                if (r1.Length != n || c1.Length != n || r2.Length != n || c2.Length != n) throw new InvalidOperationException("Region 边界数组不完整。");
                for (int i = 0; i < n; i++)
                    if (areas[i].D > 0 && new[] { r1[i].D, c1[i].D, r2[i].D, c2[i].D }.Any(v =>
                        !VmImageEdgeLineParametersS143.Finite(v) || v < 0 || v > 16383))
                        throw new InvalidOperationException("Region 须为0–16383内的真实图像像素区域，不接受负坐标或世界坐标冒充。");
                return sum;
            }
            finally
            {
                foreach (var tuple in new[] { count, areas, rows, columns, r1, c1, r2, c2 }) if (tuple != null) tuple.Dispose();
            }
        }

        private static void RequireCompleteConnection()
        {
            HTuple maximum = null, neighborhood = null;
            try
            {
                HOperatorSet.GetSystem("max_connection", out maximum);
                HOperatorSet.GetSystem("neighborhood", out neighborhood);
                if (maximum.I != 0 || neighborhood.I != 8)
                    throw new InvalidOperationException("本工具要求HALCON完整8邻域连接（max_connection=0、neighborhood=8）。当前进程设置已改变；拒绝把截断或不同邻域冒充相同结果，不修改全局设置。");
            }
            finally { if (maximum != null) maximum.Dispose(); if (neighborhood != null) neighborhood.Dispose(); }
        }

        public static ConnectedObjectResultS144 Run(HObject source, VmConnectedObjectParametersS144 parameters)
        {
            string error = parameters.Validate(); if (error.Length > 0) throw new InvalidOperationException(error);
            double inputArea = ValidateBudget(source); RequireCompleteConnection();
            var result = new ConnectedObjectResultS144();
            HObject union = null, connected = null;
            HTuple inputCount = null, unionArea = null, unionRow = null, unionColumn = null;
            HTuple count = null, areas = null, rows = null, columns = null;
            try
            {
                HObject empty; HOperatorSet.GenEmptyObj(out empty); result.Regions = empty;
                HOperatorSet.CountObj(source, out inputCount);
                if (inputCount.I == 0 || inputArea == 0) return result;
                HOperatorSet.Union1(source, out union);
                HOperatorSet.AreaCenter(union, out unionArea, out unionRow, out unionColumn);
                if (unionArea.Length != 1) throw new InvalidOperationException("合并区域数量不符。");
                if (unionArea.D == 0) return result;
                HOperatorSet.Connection(union, out connected);
                RequireCompleteConnection();
                HOperatorSet.CountObj(connected, out count);
                int n = count.I;
                if (n > MaxConnectedObjects) throw new InvalidOperationException("真实连通对象超过1000个，未截断或伪造完整统计。请在上游去噪/缩小检测域。");
                if (n == 0) throw new InvalidOperationException("非空合并区域未产生完整连通对象。");
                result.ConnectedCount = n;
                HOperatorSet.AreaCenter(connected, out areas, out rows, out columns);
                if (areas.Length != n || rows.Length != n || columns.Length != n) throw new InvalidOperationException("连通对象同序面积/中心不完整。");
                var candidates = new List<VmConnectedObjectRowS144>();
                for (int i = 0; i < n; i++)
                {
                    double area = areas[i].D, row = rows[i].D, column = columns[i].D;
                    if (!VmImageEdgeLineParametersS143.Finite(area) || area <= 0 || area > MaxInputArea ||
                        !VmImageEdgeLineParametersS143.Finite(row) || !VmImageEdgeLineParametersS143.Finite(column) ||
                        row < 0 || column < 0 || row > 16383 || column > 16383)
                        throw new InvalidOperationException("HALCON连通对象面积/中心无效，不补造几何。");
                    if (area >= parameters.MinimumArea && area <= parameters.MaximumArea)
                        candidates.Add(new VmConnectedObjectRowS144 { SourceIndex = i, Area = area, Row = row, Column = column });
                }
                IOrderedEnumerable<VmConnectedObjectRowS144> sorted;
                switch (parameters.SortMode)
                {
                    case "area-asc": sorted = candidates.OrderBy(item => item.Area).ThenBy(item => item.Row).ThenBy(item => item.Column); break;
                    case "row": sorted = candidates.OrderBy(item => item.Row).ThenBy(item => item.Column).ThenByDescending(item => item.Area); break;
                    case "column": sorted = candidates.OrderBy(item => item.Column).ThenBy(item => item.Row).ThenByDescending(item => item.Area); break;
                    default: sorted = candidates.OrderByDescending(item => item.Area).ThenBy(item => item.Row).ThenBy(item => item.Column); break;
                }
                result.Rows.AddRange(sorted.ThenBy(item => item.SourceIndex));
                for (int i = 0; i < result.Rows.Count; i++) { result.Rows[i].Index = i; result.Rows[i].Selected = i == parameters.SelectionIndex; }
                result.TotalArea = result.Rows.Sum(item => item.Area);
                if (result.Rows.Count > 0)
                {
                    using (var indices = new HTuple(result.Rows.Select(item => item.SourceIndex + 1).ToArray()))
                    { HObject ordered; HOperatorSet.SelectObj(connected, out ordered, indices); result.Regions.Dispose(); result.Regions = ordered; }
                    if (parameters.SelectionIndex < result.Rows.Count)
                    { HObject selected; HOperatorSet.SelectObj(result.Regions, out selected, parameters.SelectionIndex + 1); result.SelectedRegion = selected; }
                }
                return result;
            }
            catch { result.Dispose(); throw; }
            finally
            {
                if (union != null) union.Dispose(); if (connected != null) connected.Dispose();
                foreach (var tuple in new[] { inputCount, unionArea, unionRow, unionColumn, count, areas, rows, columns }) if (tuple != null) tuple.Dispose();
            }
        }
    }
}
