using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private VmImageSnapshot FindGeometryReferenceImageS103(VmToolInstance tool, out string sourceName, bool outputs = false)
        {
            sourceName = null;
            var contexts = new HashSet<string>(StringComparer.Ordinal);
            var ports = (outputs ? ToolMetadata.GetInstanceOutputPorts(tool) : ToolMetadata.GetInstanceInputPorts(tool))
                .Where(port => VmFlowValueContractS103.IsGeometryType(port.DataType)).ToList();
            var selectedBranch = tool.Kind == VmToolKind.BranchValueSelection && !outputs ? GetSelectedBranchBindingS101(tool) : null;
            if (tool.Kind == VmToolKind.BranchValueSelection && !outputs)
            {
                if (selectedBranch == null) return null;
                ports = ports.Where(port => port.PortName == selectedBranch.TargetPortName).ToList();
            }
            foreach (var port in ports)
            {
                var binding = outputs ? null : tool.GetInputBinding(port.PortName);
                var producer = outputs ? tool : GetInputSourceTool(binding);
                string sourcePort = outputs ? port.PortName : binding == null ? null : binding.SourcePortName;
                object raw;
                if (producer == null || sourcePort == null || (producer.ResultCode != "OK" && producer.ResultCode != "NG") ||
                    !string.IsNullOrWhiteSpace(producer.ErrorMessage) || !producer.TryGetOutputValue(sourcePort, out raw)) return null;
                try { VmFlowValueContractS103.RequireGeometry(port.DataType, raw); }
                catch (InvalidOperationException) { return null; }
                string context = VmFlowValueContractS103.GetContextId(raw);
                if (string.IsNullOrWhiteSpace(context) || IsPendingCoordinateContextS102(context)) return null;
                contexts.Add(context);
            }
            // A mixed-coordinate geometry operation must not select an arbitrary reference image.
            if (contexts.Count != 1) return null;
            string wanted = contexts.First();
            var sources = subflowRuntimeScopesS102.SelectMany(scope => scope).Concat(flowTools)
                .Concat(subflowEditorsS102.SelectMany(frame => frame.ParentTools)).Distinct();
            foreach (var producer in sources)
            {
                if ((producer.ResultCode != "OK" && producer.ResultCode != "NG") || !string.IsNullOrWhiteSpace(producer.ErrorMessage)) continue;
                foreach (var port in ToolMetadata.GetInstanceOutputPorts(producer).Where(port => port.DataType == "Image"))
                {
                    var image = GetImageSnapshot(producer, port.PortName);
                    if (image == null || image.IsDisposed || ResolveOutputCoordinateContextS102(producer, port.PortName) != wanted) continue;
                    sourceName = producer.InstanceName + "." + port.PortName + " · 几何坐标关联底图";
                    return image;
                }
            }
            return null;
        }

        private bool TryResolveGeometryImageContextS103(VmToolInstance tool, string requested, bool outputs, out ResolvedImageContext context)
        {
            context = null;
            if (tool == null) return false;
            var ports = outputs ? ToolMetadata.GetInstanceOutputPorts(tool) : ToolMetadata.GetInstanceInputPorts(tool);
            if (!ports.Any(port => VmFlowValueContractS103.IsGeometryType(port.DataType)) || (!outputs && ports.Any(port => port.DataType == "Image"))) return false;
            string sourceName;
            var snapshot = FindGeometryReferenceImageS103(tool, out sourceName, outputs);
            context = snapshot == null
                ? CreateEmptyImageContext(requested, "几何来源未就绪、坐标来源不唯一或对应图像已失效；不回退到全局图像。", tool.InstanceName + " · 等待几何关联图像")
                : CreateSnapshotContext(requested, snapshot, tool.InstanceName + " ← " + sourceName, outputs ? "几何输出关联底图 · 非 Image 输出" : "几何输入关联底图 · 只作参考");
            return true;
        }

        private sealed class PointDetailRowS103
        {
            public int Index { get; set; }
            public string Row { get; set; }
            public string Column { get; set; }
            public string State { get; set; }
        }

        private void ShowPointSetDetailsS103_Click(object sender, RoutedEventArgs e)
        {
            var element = sender as FrameworkElement;
            VmToolInstance tool; VmPortDefinition port; object raw;
            if (!TryResolveDetailValueUI06(element == null ? null : element.DataContext, out tool, out port, out raw) || port.DataType != "Point2D[]") return;
            BuildPointDetailWindowUI06(tool, port, raw).ShowDialog();
        }

        private Window BuildPointDetailWindowUI06(VmToolInstance tool, VmPortDefinition port, object raw)
        {
            var snapshot = ((VmPoint2DArray)raw).Clone();
            if (((VmPoint2DArray)raw).Points == null) snapshot.IsValid = false;
            var points = snapshot.Points;
            string containerError = snapshot.IsValid && snapshot.CoordinateContext == VmPoint2D.ImagePixelContext && !string.IsNullOrWhiteSpace(snapshot.CoordinateContextId)
                ? null : "点集容器或坐标身份无效";
            Func<VmPoint2D, string> errorFor = point => point == null ? "空项" :
                point.CoordinateContext != snapshot.CoordinateContext || point.CoordinateContextId != snapshot.CoordinateContextId ? "坐标来源不一致" :
                !string.IsNullOrWhiteSpace(point.GetValidationError()) ? point.GetValidationError() : containerError;
            var errors = points.Select(errorFor).ToList();
            var root = new DockPanel { Margin = new Thickness(12) };
            var heading = new TextBlock { Text = tool.InstanceName + " · " + port.DisplayName + "\n" + snapshot.ToDisplayString() +
                "\n" + BuildResultOriginTextS88(tool) + "\n只读快照：" + DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture) + "；不随重跑变化，不修改或过滤实际数据。",
                TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0,0,0,8) };
            DockPanel.SetDock(heading, Dock.Top); root.Children.Add(heading);
            var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0,8,0,0) };
            var onlyInvalid = new CheckBox { Content = "只看异常点", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0,0,12,0) };
            var previous = new Button { Content = "上一页", Padding = new Thickness(8,3,8,3) };
            var next = new Button { Content = "下一页", Padding = new Thickness(8,3,8,3), Margin = new Thickness(6,0,0,0) };
            var pageText = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10,0,0,0) };
            actions.Children.Add(onlyInvalid); actions.Children.Add(previous); actions.Children.Add(next); actions.Children.Add(pageText);
            DockPanel.SetDock(actions, Dock.Bottom); root.Children.Add(actions);
            var grid = new DataGrid { AutoGenerateColumns = false, IsReadOnly = true, CanUserAddRows = false, CanUserDeleteRows = false, HeadersVisibility = DataGridHeadersVisibility.Column };
            grid.Columns.Add(new DataGridTextColumn { Header = "原始序号", Binding = new Binding("Index"), Width = new DataGridLength(70) });
            grid.Columns.Add(new DataGridTextColumn { Header = "Row / Y", Binding = new Binding("Row"), Width = new DataGridLength(110) });
            grid.Columns.Add(new DataGridTextColumn { Header = "Column / X", Binding = new Binding("Column"), Width = new DataGridLength(110) });
            grid.Columns.Add(new DataGridTextColumn { Header = "状态 / 原因", Binding = new Binding("State"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            root.Children.Add(grid);
            int page = 0; const int pageSize = 200;
            var indices = Enumerable.Range(0, points.Count).ToList();
            Action refresh = delegate
            {
                int pages = Math.Max(1, (indices.Count + pageSize - 1) / pageSize);
                page = Math.Max(0, Math.Min(page, pages - 1));
                grid.ItemsSource = indices.Skip(page * pageSize).Take(pageSize).Select(index => new PointDetailRowS103
                {
                    Index = index + 1, Row = points[index] == null ? "--" : points[index].Row.ToString("G17", CultureInfo.InvariantCulture),
                    Column = points[index] == null ? "--" : points[index].Column.ToString("G17", CultureInfo.InvariantCulture),
                    State = string.IsNullOrWhiteSpace(errors[index]) ? "有效" : errors[index]
                }).ToList();
                previous.IsEnabled = page > 0; next.IsEnabled = page + 1 < pages;
                pageText.Text = (page + 1) + " / " + pages + " 页 · " + indices.Count + " 项";
            };
            previous.Click += delegate { page--; refresh(); }; next.Click += delegate { page++; refresh(); };
            RoutedEventHandler filter = delegate
            {
                indices = Enumerable.Range(0, points.Count).Where(index => onlyInvalid.IsChecked != true || !string.IsNullOrWhiteSpace(errors[index])).ToList();
                page = 0; refresh();
            };
            onlyInvalid.Checked += filter; onlyInvalid.Unchecked += filter; refresh();
            var detail = new Window { Owner = ShowActivated ? moduleParameterWindowS89 ?? this : null, ShowActivated = ShowActivated,
                Title = "点集明细 · " + tool.InstanceName, Content = root,
                Width = 660, Height = 470, MinWidth = 520, MinHeight = 320, ShowInTaskbar = false, WindowStartupLocation = WindowStartupLocation.CenterOwner };
            if (!ShowActivated) { detail.WindowStartupLocation = WindowStartupLocation.Manual; detail.Left = Left; detail.Top = Top; }
            return detail;
        }

        private void LoadGeometryReuseExampleS103_Click(object sender, RoutedEventArgs e) { LoadExampleProgramS78("geometry-reuse"); }
        private void LoadGeometryBranchExampleS103_Click(object sender, RoutedEventArgs e) { LoadExampleProgramS78("geometry-branch"); }

        private bool BuildGeometryBranchExampleS103()
        {
            bool available = BuildGeometryReuseExampleS103();
            var measure = flowTools[2]; var relation = flowTools[3]; var judge = flowTools[4];
            flowTools.Remove(relation);
            judge.InstanceName = "04 半径是否落入目标范围";
            var alternative = CreateFlowTool(VmToolKind.SubflowCall, "05 未通过时几何拟合", true, null);
            alternative.Parameters = measure.Parameters.Clone();
            alternative.ReplaceInputBindings(measure.InputBindings.Select(binding => binding.Clone()).ToList());
            alternative.Parameters.Subflow.Tools.First(tool => tool.ToolType == VmToolKind.TypedPointSetCircleFittingMeasurement.ToString())
                .Parameters.XldCircleFitAlgorithm = VmXldCircleFitAlgorithm.Geometric;
            alternative.SetInputBinding("$RunCondition", judge.ToolId, "Passed");
            alternative.Parameters.ExecutionConditionWhenFalse = true;
            flowTools.Add(alternative);
            var select = CreateFlowTool(VmToolKind.BranchValueSelection, "06 选择本次拟合圆", true, null);
            select.Parameters.BranchValueType = "Circle2D";
            select.SetInputBinding("Selector", judge.ToolId, "Passed");
            select.SetInputBinding("WhenTrue", measure.ToolId, "CircleOut");
            select.SetInputBinding("WhenFalse", alternative.ToolId, "CircleOut");
            flowTools.Add(select);
            relation.InstanceName = "07 选中圆继续点圆测量";
            relation.SetInputBinding("Circle", select.ToolId, "Value"); flowTools.Add(relation);
            return available;
        }

        private bool BuildGeometryReuseExampleS103()
        {
            string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HalconVision", "LearningImages");
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, "circle-points-" + Guid.NewGuid().ToString("N") + ".png");
            using (var bitmap = new System.Drawing.Bitmap(320, 240))
            {
                for (int y = 0; y < 240; y++) for (int x = 0; x < 320; x++)
                {
                    int gray = (y - 120) * (y - 120) + (x - 160) * (x - 160) <= 65 * 65 ? 225 : 30;
                    bitmap.SetPixel(x, y, System.Drawing.Color.FromArgb(gray, gray, gray));
                }
                bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            }
            var source = CreateFlowTool(VmToolKind.ImageSource, "01 自带圆形图像", true, null);
            source.Parameters.LocalImagePath = path; source.Parameters.LocalImageSerialNumber = "LEARN-GEOMETRY-REUSE";
            flowTools.Add(source);
            var finder = CreateFlowTool(VmToolKind.CircularCaliperCircleFinder, "01 圆弧卡尺点集", true, null);
            var centroid = CreateFlowTool(VmToolKind.PointSetGeometryStatistics, "02 圆弧点集质心", true, null);
            var statistics = CreateFlowTool(VmToolKind.PointSetGeometryStatistics, "01 输入点集统计", true, null);
            var fit = CreateFlowTool(VmToolKind.TypedPointSetCircleFittingMeasurement, "02 点集拟合圆", true, null);
            try
            {
                finder.Parameters.CircularCaliperCenterRow = 120; finder.Parameters.CircularCaliperCenterColumn = 160;
                finder.Parameters.CircularCaliperSearchRadius = 65;
                finder.Parameters.CircularCaliperStartAngleDegrees = 15; finder.Parameters.CircularCaliperEndAngleDegrees = 255;
                finder.Parameters.CircularCaliperCount = 16;
                finder.Parameters.CircularCaliperLength1 = 16; finder.Parameters.CircularCaliperLength2 = 3;
                finder.Parameters.CircularCaliperThreshold = 20;
                finder.SetInputBinding("Image", VmSubflowData.InputProxyId, "ImageIn");
                centroid.Parameters.PointSetMinimumCount = 3;
                centroid.SetInputBinding("Points", finder.ToolId, "EdgePoints");
                var extract = CreateFlowTool(VmToolKind.SubflowCall, "02 提取圆弧点集", true, null);
                extract.Parameters.Subflow = new VmSubflowData
                {
                    Tools = CaptureFlowRecipeS102(new[] { finder, centroid }),
                    Inputs = new List<VmSubflowPortData> { new VmSubflowPortData { PortName = "ImageIn", DisplayName = "检测图像", DataType = "Image" } },
                    Outputs = new List<VmSubflowPortData>
                    {
                        new VmSubflowPortData { PortName = "PointsOut", DisplayName = "圆弧边缘点集", DataType = "Point2D[]", SourceToolId = finder.ToolId, SourcePortName = "EdgePoints" },
                        new VmSubflowPortData { PortName = "CentroidOut", DisplayName = "圆弧点集质心", DataType = "Point2D", SourceToolId = centroid.ToolId, SourcePortName = "Centroid" }
                    }
                };
                extract.SetInputBinding("ImageIn", source.ToolId, "Image"); flowTools.Add(extract);
                statistics.Parameters.PointSetMinimumCount = 3; fit.Parameters.PointSetMinimumCount = 3;
                statistics.SetInputBinding("Points", VmSubflowData.InputProxyId, "PointsIn");
                fit.SetInputBinding("Points", VmSubflowData.InputProxyId, "PointsIn");
                var measure = CreateFlowTool(VmToolKind.SubflowCall, "03 复用点集拟合圆", true, null);
                measure.Parameters.Subflow = new VmSubflowData
                {
                    Tools = CaptureFlowRecipeS102(new[] { statistics, fit }),
                    Inputs = new List<VmSubflowPortData> { new VmSubflowPortData { PortName = "PointsIn", DisplayName = "待拟合点集", DataType = "Point2D[]" } },
                    Outputs = new List<VmSubflowPortData>
                    {
                        new VmSubflowPortData { PortName = "CircleOut", DisplayName = "拟合圆", DataType = "Circle2D", SourceToolId = fit.ToolId, SourcePortName = "MainCircle" },
                        new VmSubflowPortData { PortName = "RadiusOut", DisplayName = "拟合半径", DataType = "Number", SourceToolId = fit.ToolId, SourcePortName = "Radius" }
                    }
                };
                measure.SetInputBinding("PointsIn", extract.ToolId, "PointsOut"); flowTools.Add(measure);
                var relation = CreateFlowTool(VmToolKind.PointToCircleRadialDeviationMeasurement, "04 点集质心与拟合圆关系", true, null);
                relation.SetInputBinding("Point", extract.ToolId, "CentroidOut"); relation.SetInputBinding("Circle", measure.ToolId, "CircleOut");
                flowTools.Add(relation);
                var judge = CreateFlowTool(VmToolKind.NumericJudge, "05 拟合半径判定", true, null);
                judge.SetInputBinding("Value", measure.ToolId, "RadiusOut"); judge.InputToolId = measure.ToolId; judge.InputPortName = "RadiusOut";
                judge.NumericLowerLimit = 60; judge.NumericUpperLimit = 70; flowTools.Add(judge);
            }
            finally { finder.Dispose(); centroid.Dispose(); statistics.Dispose(); fit.Dispose(); }
            StopOnNgCheckBox.IsChecked = false;
            return true;
        }

        private static void ConfigureFlowValueTypePickerS103(ComboBox picker, bool includeResultCode, string selectedType)
        {
            var options = VmFlowValueContractS103.GetTypes(includeResultCode).ToList();
            var view = new ListCollectionView(options);
            view.GroupDescriptions.Add(new PropertyGroupDescription("Group"));
            picker.DisplayMemberPath = "DisplayText"; picker.SelectedValuePath = "Key";
            var heading = new FrameworkElementFactory(typeof(TextBlock));
            heading.SetBinding(TextBlock.TextProperty, new Binding("Name"));
            heading.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
            heading.SetValue(TextBlock.ForegroundProperty, Brushes.SlateGray);
            heading.SetValue(TextBlock.MarginProperty, new Thickness(6, 6, 0, 2));
            picker.GroupStyle.Clear();
            picker.GroupStyle.Add(new GroupStyle { HeaderTemplate = new DataTemplate { VisualTree = heading } });
            picker.ItemsSource = view;
            picker.SelectedValue = VmFlowValueContractS103.Normalize(selectedType, includeResultCode) ?? "Number";
            picker.ToolTip = "按任务选择数据类型；来源必须类型兼容。几何和点集保留原坐标身份，不自动做标定转换。";
        }
    }
}
