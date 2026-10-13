using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace HalconWinFormsDemo.Models
{
    [DataContract]
    public sealed class VmCommunicationSampleS149
    {
        [DataMember(Order = 1)] public string Name { get; set; }
        [DataMember(Order = 2)] public string Payload { get; set; }
        public VmCommunicationSampleS149 Clone() { return new VmCommunicationSampleS149 { Name = Name, Payload = Payload }; }
        public override string ToString() { return Name ?? "空样本名称"; }
    }

    [DataContract]
    public sealed class VmCommunicationSampleQueueS149
    {
        [DataMember(Order = 1)] public List<VmCommunicationSampleS149> Samples { get; set; }
        [DataMember(Order = 2)] public int SelectedIndex { get; set; }

        public VmCommunicationSampleQueueS149 Clone()
        {
            if (Samples != null && Samples.Count > 16)
                throw new InvalidOperationException("通讯样本超过16项，不能复制部分队列。");
            return new VmCommunicationSampleQueueS149 { SelectedIndex = SelectedIndex,
                Samples = Samples == null ? null : Samples.Select(sample => sample == null ? null : sample.Clone()).ToList() };
        }

        public string Validate(VmToolKind kind)
        {
            if (Samples == null || Samples.Count < 1 || Samples.Count > 16) return "通讯样本队列必须有1–16项，不截断队列。";
            if (SelectedIndex < 0 || SelectedIndex >= Samples.Count) return "通讯样本选取编号越界，请明确重新选择，不自动夹紧。";
            int characterLimit = kind == VmToolKind.TcpClientExchange ? 65536 : 2048;
            int budget = 65536;
            foreach (var sample in Samples)
            {
                if (sample == null || string.IsNullOrWhiteSpace(sample.Name) || sample.Name.Length > 64)
                    return "每个通讯样本必须有1–64字符的名称。";
                if (sample.Payload == null || sample.Payload.Length > characterLimit)
                    return "通讯样本内容缺失或超过本协议的" + characterLimit + "字符预算。";
                budget -= Encoding.UTF8.GetByteCount(sample.Name) + Encoding.UTF8.GetByteCount(sample.Payload);
                if (budget < 0) return "通讯样本名称和内容合计超过64KiB，不能使用部分队列。";
            }
            // Empty/malformed responses are deliberately useful failure samples.
            // Original request/response parsers decide their execution outcome.
            return string.Empty;
        }

        public string Resolve(VmToolKind kind)
        {
            string error = Validate(kind);
            if (error.Length > 0) throw new InvalidOperationException(error);
            return Samples[SelectedIndex].Payload;
        }
    }
}
