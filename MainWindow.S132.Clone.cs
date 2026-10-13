using System;
using System.Collections.Generic;
using System.Linq;
using HalconWinFormsDemo.Models;

namespace HalconWinFormsDemo
{
    public partial class MainWindow
    {
        private sealed class AlignmentCopyAuthorizationS132
        {
            public VmToolInputBindingData ImageBinding;
            public VmToolInputBindingData PoseBinding;
            public string Context;
            public string Signature;
            public string ReferenceVersion;
        }

        private readonly Dictionary<string, AlignmentCopyAuthorizationS132> alignmentCopyAuthorizationsS132 =
            new Dictionary<string, AlignmentCopyAuthorizationS132>(StringComparer.OrdinalIgnoreCase);

        private ISet<string> CaptureCurrentAlignmentReferenceIdsS132()
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            alignmentCopyAuthorizationsS132.Clear();
            foreach (var tool in flowTools.Where(t => t.Kind == VmToolKind.PoseImageAlignment))
            {
                var p = tool.Parameters == null ? null : tool.Parameters.PoseImageAlignmentS132;
                if (p == null || !p.HasReference || string.IsNullOrWhiteSpace(p.ReferenceVersion)) continue;
                try
                {
                    string context = AlignmentInputContextS132(tool), signature = AlignmentSourceSignatureS132(tool);
                    var imageBinding = tool.GetInputBinding("Image");
                    var poseBinding = tool.GetInputBinding("Pose");
                    if (imageBinding == null || poseBinding == null || string.IsNullOrWhiteSpace(context) ||
                        context == "无效来源" || IsPendingCoordinateContextS102(context) ||
                        p.ReferenceContextId != context || p.ReferenceSourceSignature != signature ||
                        !string.IsNullOrWhiteSpace(p.Validate())) continue;
                    result.Add(tool.ToolId);
                    // A flow copy releases the original runtime scope before applying the new one.
                    // Capture both bindings and the exact applied identity, not just a permissive old tool ID.
                    alignmentCopyAuthorizationsS132[tool.ToolId] = new AlignmentCopyAuthorizationS132
                    {
                        ImageBinding = imageBinding.Clone(), PoseBinding = poseBinding.Clone(),
                        Context = context, Signature = signature, ReferenceVersion = p.ReferenceVersion
                    };
                }
                catch { /* An unresolved or stale reference is not authorized for identity migration. */ }
            }
            return result;
        }

        private void RebindCopiedAlignmentReferencesS132(IEnumerable<VmToolInstance> tools,
            IDictionary<string, string> idMap, ISet<string> authorizedOldIds = null)
        {
            if (tools == null || idMap == null || idMap.Count == 0) return;
            var inverse = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in idMap)
            {
                if (string.IsNullOrWhiteSpace(pair.Key) || string.IsNullOrWhiteSpace(pair.Value) || inverse.ContainsKey(pair.Value))
                    return; // Ambiguous configuration mappings never authorize a reference change.
                inverse.Add(pair.Value, pair.Key);
            }
            try
            {
                foreach (var tool in tools.Where(t => t != null && t.Kind == VmToolKind.PoseImageAlignment))
                {
                    string originalToolId;
                    if (!inverse.TryGetValue(tool.ToolId, out originalToolId)) continue;
                    var p = tool.Parameters == null ? null : tool.Parameters.PoseImageAlignmentS132;
                    var imageBinding = tool.GetInputBinding("Image");
                    var poseBinding = tool.GetInputBinding("Pose");
                    if (p == null || !p.HasReference || imageBinding == null || poseBinding == null ||
                        string.IsNullOrWhiteSpace(p.ReferenceVersion)) continue;
                    try
                    {
                        var originalImageBinding = RestoreAlignmentCopyBindingS132(imageBinding, inverse);
                        var originalPoseBinding = RestoreAlignmentCopyBindingS132(poseBinding, inverse);
                        if (authorizedOldIds != null)
                        {
                            AlignmentCopyAuthorizationS132 authorization;
                            if (!authorizedOldIds.Contains(originalToolId) ||
                                !alignmentCopyAuthorizationsS132.TryGetValue(originalToolId, out authorization) ||
                                p.ReferenceContextId != authorization.Context || p.ReferenceSourceSignature != authorization.Signature ||
                                p.ReferenceVersion != authorization.ReferenceVersion ||
                                !SameAlignmentCopyBindingS132(originalImageBinding, authorization.ImageBinding) ||
                                !SameAlignmentCopyBindingS132(originalPoseBinding, authorization.PoseBinding)) continue;
                        }
                        else
                        {
                            var originalImageSource = GetInputSourceTool(originalImageBinding);
                            var originalPoseSource = GetInputSourceTool(originalPoseBinding);
                            if (originalImageSource == null || originalPoseSource == null ||
                                ResolveOutputCoordinateContextS102(originalImageSource, originalImageBinding.SourcePortName) != p.ReferenceContextId ||
                                ResolveOutputCoordinateContextS102(originalPoseSource, originalPoseBinding.SourcePortName) != p.ReferenceContextId ||
                                AlignmentSourceSignatureForBindingsS132(originalImageBinding, originalPoseBinding) != p.ReferenceSourceSignature) continue;
                        }
                        string newContext = AlignmentInputContextS132(tool), newSignature = AlignmentSourceSignatureS132(tool);
                        if (string.IsNullOrWhiteSpace(newContext) || newContext == "无效来源" || IsPendingCoordinateContextS102(newContext) ||
                            string.IsNullOrWhiteSpace(newSignature)) continue;
                        // Explicit configuration cloning changes only the dependency identity.
                        // Never sample a live pose or alter saved reference coordinates, dimensions, or version.
                        p.ReferenceContextId = newContext;
                        p.ReferenceSourceSignature = newSignature;
                    }
                    catch { /* Keep the original identity; execution must reject unresolved or changed sources. */ }
                }
            }
            finally
            {
                if (authorizedOldIds != null) alignmentCopyAuthorizationsS132.Clear();
            }
        }

        private static VmToolInputBindingData RestoreAlignmentCopyBindingS132(VmToolInputBindingData binding,
            IDictionary<string, string> inverse)
        {
            var restored = binding.Clone(); string originalSourceId;
            if (inverse.TryGetValue(restored.SourceToolId, out originalSourceId)) restored.SourceToolId = originalSourceId;
            return restored;
        }

        private static bool SameAlignmentCopyBindingS132(VmToolInputBindingData a, VmToolInputBindingData b)
        {
            return a != null && b != null &&
                string.Equals(a.TargetPortName, b.TargetPortName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(a.SourceToolId, b.SourceToolId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(a.SourcePortName, b.SourcePortName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
