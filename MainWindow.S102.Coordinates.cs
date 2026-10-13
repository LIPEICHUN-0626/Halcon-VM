using System;
using System.Collections.Generic;
using System.Linq;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private static string GetCoordinateIdentityBaseS102(VmToolInstance tool)
        {
            return string.IsNullOrWhiteSpace(tool.CoordinateIdentityBase) ? tool.ToolId : tool.CoordinateIdentityBase;
        }

        private static bool IsPendingCoordinateContextS102(string context)
        {
            return context != null && context.StartsWith("Pending:", StringComparison.Ordinal);
        }

        private string ResolveOutputCoordinateContextS102(VmToolInstance tool, string portName)
        {
            return ResolveOutputCoordinateContextS102(tool, portName, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        }

        private string ResolveOutputCoordinateContextS102(VmToolInstance tool, string portName, ISet<string> visited)
        {
            if (tool == null) return "无效来源";
            string identity = GetCoordinateIdentityBaseS102(tool) + "." + portName;
            if (!visited.Add(identity)) return "Pending:" + identity;
            string context;
            if (tool.TryGetOutputCoordinateContext(portName, out context)) return context;
            object raw;
            if (tool.TryGetOutputValue(portName, out raw))
            {
                string geometryContext = VmFlowValueContractS103.GetContextId(raw);
                if (!string.IsNullOrWhiteSpace(geometryContext)) return geometryContext;
            }
            var port = ToolMetadata.GetInstanceOutputPorts(tool).FirstOrDefault(item => item.PortName == portName);
            if (port == null) return "Pending:" + identity;
            if (tool.Kind == VmToolKind.PoseImageAlignment && (port.DataType == "Image" || port.DataType == "Region" || port.DataType == "Pose2D"))
            {
                var alignment = tool.Parameters == null ? null : tool.Parameters.PoseImageAlignmentS132;
                if (alignment == null || !alignment.HasReference || !string.IsNullOrWhiteSpace(alignment.Validate())) return "Pending:" + identity;
                return AlignmentOutputContextS132(tool, alignment) ?? "Pending:" + identity;
            }
            if (tool.Kind == VmToolKind.CameraDistortionCorrection &&
                (port.DataType == "Image" || port.DataType == "Point2D" || port.DataType == "XLD"))
            {
                var camera = tool.Parameters == null ? null : tool.Parameters.CameraIntrinsicsS126;
                var binding = tool.GetInputBinding("Image");
                if (camera == null || !string.IsNullOrWhiteSpace(camera.Validate()) || binding == null) return "Pending:" + identity;
                string inputContext = ResolveOutputCoordinateContextS102(GetInputSourceTool(binding), binding.SourcePortName, visited);
                return IsPendingCoordinateContextS102(inputContext) || inputContext == "无效来源" ? "Pending:" + identity :
                    GetCameraOutputContextS128(tool, camera, tool.Parameters.CameraRectifyModeS126, inputContext);
            }
            if (tool.Kind == VmToolKind.SubflowCall || tool.Kind == VmToolKind.SubflowInput) return "Pending:" + identity;
            if (tool.Kind == VmToolKind.BranchValueSelection)
            {
                var selected = GetSelectedBranchBindingS101(tool);
                return selected == null ? "Pending:" + identity : ResolveOutputCoordinateContextS102(GetInputSourceTool(selected), selected.SourcePortName, visited);
            }
            if (port.DataType == "WorldPoint2D") return "Pending:" + identity;
            if (port.DataType == "Image") return identity;
            var inputs = ToolMetadata.GetInstanceInputPorts(tool);
            foreach (var input in inputs.Where(item => item.DataType == "Image").Concat(inputs.Where(item => item.DataType == "Region" || item.DataType == "XLD")))
            {
                var binding = tool.GetInputBinding(input.PortName);
                if (binding == null) continue;
                return ResolveOutputCoordinateContextS102(GetInputSourceTool(binding), binding.SourcePortName, visited);
            }
            return ResolveImagePixelContextIdS66(tool, visited) ?? "Global.Image";
        }
    }
}
