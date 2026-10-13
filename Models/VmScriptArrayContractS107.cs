using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;

namespace HalconWinFormsDemo.Models
{
    public static class VmScriptArrayContractS107
    {
        public static bool TryParseDefault(string type, string source, out object value, out string error)
        {
            return TryParseArray(type, source, 4096, out value, out error);
        }

        public static bool TryParseArray(string type, string source, int maxCharacters, out object value, out string error)
        {
            value = null; error = null;
            try
            {
                if (string.IsNullOrWhiteSpace(source)) throw new InvalidOperationException("数组默认值使用 JSON，例如 [1,2,3] 或 [\"工位1\",\"工位2\"]；空数组请写 []。");
                if (source.Length > maxCharacters) throw new InvalidOperationException("JSON 文本最多 " + maxCharacters + " 字符。");
                var quotas = new XmlDictionaryReaderQuotas { MaxDepth = 8, MaxStringContentLength = maxCharacters, MaxArrayLength = VmArrayContractS105.MaxCount, MaxBytesPerRead = 4096, MaxNameTableCharCount = 4096 };
                var document = new XmlDocument();
                using (var reader = JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(source), quotas)) document.Load(reader);
                XmlElement root = document.DocumentElement;
                if (root == null || root.GetAttribute("type") != "array") throw new InvalidOperationException("默认值必须是 JSON 数组，不能是 null、对象或单个值。");
                var numbers = new List<double>(); var texts = new List<string>();
                bool numeric = type == VmScriptScalarType.NumberArray;
                foreach (XmlNode child in root.ChildNodes)
                {
                    if (numbers.Count + texts.Count >= VmArrayContractS105.MaxCount) throw new InvalidOperationException("数组最多 10000 项。");
                    var item = child as XmlElement;
                    if (item == null || item.GetAttribute("type") != (numeric ? "number" : "string"))
                        throw new InvalidOperationException("数组每项必须是 " + (numeric ? "JSON 数值" : "JSON 字符串") + "，不接受 null、嵌套数组或混合类型。");
                    if (numeric)
                    {
                        double number;
                        if (!double.TryParse(item.InnerText, NumberStyles.Float, CultureInfo.InvariantCulture, out number) || double.IsNaN(number) || double.IsInfinity(number))
                            throw new InvalidOperationException("数组只能包含有限数值。");
                        numbers.Add(number);
                    }
                    else texts.Add(item.InnerText);
                }
                value = numeric ? (object)new VmNumberArrayS105(numbers) : new VmStringArrayS105(texts);
                return true;
            }
            catch (Exception ex) when (ex is XmlException || ex is ArgumentException || ex is InvalidOperationException || ex is System.Runtime.Serialization.SerializationException)
            {
                error = "数组默认值无效：" + ex.Message; return false;
            }
        }
    }
}
