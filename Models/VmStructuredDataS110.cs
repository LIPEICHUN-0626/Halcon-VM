using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace HalconWinFormsDemo.Models
{
    [DataContract]
    public sealed class VmJsonFieldS110
    {
        [DataMember(Order = 1)] public string Id { get; set; } = Guid.NewGuid().ToString("N");
        [DataMember(Order = 2)] public string Name { get; set; } = "数值";
        [DataMember(Order = 3)] public string Path { get; set; } = "/value";
        [DataMember(Order = 4)] public string DataType { get; set; } = "Number";
        public string PortName { get { return "Field_" + Id; } }
        public VmJsonFieldS110 Clone() { return (VmJsonFieldS110)MemberwiseClone(); }
    }

    public static class VmStructuredDataS110
    {
        public const int MaxText = 1048576;
        public static readonly string[] Types = { "Number", "String", "Boolean", "Number[]", "String[]" };
        public static bool IsTool(VmToolKind kind) { return kind == VmToolKind.JsonRead || kind == VmToolKind.JsonWrite; }
        public static List<VmJsonFieldS110> Fields(VmToolParameterData p)
        {
            return p.JsonFields == null ? new List<VmJsonFieldS110> { new VmJsonFieldS110 { Id = "default" } } : p.JsonFields;
        }
        public static string[] DecodePath(string path)
        {
            if (path == null || path.Length > 1024) throw new InvalidOperationException("字段路径不能为空且最多 1024 字符；根值请留空文本。");
            if (path.Length == 0) return new string[0];
            if (!path.StartsWith("/", StringComparison.Ordinal)) throw new InvalidOperationException("路径须以 / 开头，例如 /result/score；不是点号表达式。");
            var parts = path.Substring(1).Split('/');
            if (parts.Length > 16) throw new InvalidOperationException("字段路径最多 16 层。");
            for (int i = 0; i < parts.Length; i++)
            {
                var decoded = new StringBuilder();
                for (int j = 0; j < parts[i].Length; j++)
                {
                    char c = parts[i][j];
                    if (c != '~') { decoded.Append(c); continue; }
                    if (++j >= parts[i].Length || parts[i][j] != '0' && parts[i][j] != '1') throw new InvalidOperationException("路径转义只允许 ~0（~）与 ~1（/）。");
                    decoded.Append(parts[i][j] == '0' ? '~' : '/');
                }
                parts[i] = decoded.ToString();
            }
            return parts;
        }
        public static string Validate(VmToolKind kind, VmToolParameterData p)
        {
            try
            {
                var fields = Fields(p);
                if (fields.Count < 1 || fields.Count > 32) throw new InvalidOperationException("请配置 1–32 个字段。");
                var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var paths = new List<string[]>();
                foreach (var field in fields)
                {
                    if (field == null || string.IsNullOrWhiteSpace(field.Id) || field.Id.Length > 64 || field.Id.Any(c => !char.IsLetterOrDigit(c)) || !ids.Add(field.Id))
                        throw new InvalidOperationException("字段身份缺失、重复或无效，请重新创建字段。");
                    if (string.IsNullOrWhiteSpace(field.Name) || field.Name.Length > 64 || field.Name.Any(char.IsControl) || !names.Add(field.Name.Trim()))
                        throw new InvalidOperationException("字段名称须为 1–64 个非控制字符且不能重复。");
                    if (!Types.Contains(field.DataType)) throw new InvalidOperationException(field.Name + "：请选择支持的数据类型。");
                    string[] path;
                    try { path = DecodePath(field.Path); }
                    catch (InvalidOperationException ex) { throw new InvalidOperationException(field.Name + "：" + ex.Message); }
                    if (kind == VmToolKind.JsonWrite && path.Length == 0) throw new InvalidOperationException(field.Name + "：组装路径须指定对象字段，不能覆盖整个根。");
                    if (paths.Any(other => other.SequenceEqual(path) || kind == VmToolKind.JsonWrite && other.Take(Math.Min(other.Length, path.Length)).SequenceEqual(path.Take(Math.Min(other.Length, path.Length)))))
                        throw new InvalidOperationException(field.Name + "：路径重复或与另一组装字段的父子路径冲突。");
                    paths.Add(path);
                }
                if ((p.JsonText ?? string.Empty).Length > MaxText) throw new InvalidOperationException("JSON 文本超过 1048576 字符。");
                return null;
            }
            catch (InvalidOperationException ex) { return ex.Message; }
        }

        public static Dictionary<string, object> Read(string text, IList<VmJsonFieldS110> fields)
        {
            object root = new Parser(text).Parse();
            var result = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (var field in fields)
            {
                try
                {
                    object current = root;
                    foreach (string part in DecodePath(field.Path))
                    {
                        var map = current as Dictionary<string, object>; var array = current as List<object>;
                        if (map != null)
                        {
                            if (!map.TryGetValue(part, out current)) throw new InvalidOperationException("缺少字段，不会回退到空值或默认值。");
                        }
                        else if (array != null)
                        {
                            int index;
                            if (part.Length == 0 || part.Length > 1 && part[0] == '0' || part.Any(c => c < '0' || c > '9') || !int.TryParse(part, out index) || index >= array.Count)
                                throw new InvalidOperationException("数组索引无效或越界，索引从 0 开始。");
                            current = array[index];
                        }
                        else throw new InvalidOperationException("路径中间值不是对象或数组。");
                    }
                    result.Add(field.PortName, ToValue(field.DataType, current));
                }
                catch (InvalidOperationException ex) { throw new InvalidOperationException(field.Name + "（" + field.Path + "）：" + ex.Message); }
            }
            return result;
        }
        private static object ToValue(string type, object value)
        {
            if (value == null) throw new InvalidOperationException("值为 null，与缺失、空文本和空数组不同；当前字段不接受 null。");
            if (type == "Number" && value is double || type == "String" && value is string || type == "Boolean" && value is bool) return value;
            var array = value as List<object>;
            if (type == "Number[]" && array != null && array.All(item => item is double)) return new VmNumberArrayS105(array.Cast<double>());
            if (type == "String[]" && array != null && array.All(item => item is string)) return new VmStringArrayS105(array.Cast<string>());
            throw new InvalidOperationException("实际值与 " + type + " 不一致；不隐式转换数字字符串、混合数组或布尔值。");
        }
        public static string Write(IList<VmJsonFieldS110> fields, Func<VmJsonFieldS110, object> read)
        {
            var root = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (var field in fields)
            {
                try
                {
                    object value = read(field);
                    if (VmArrayContractS105.IsType(field.DataType))
                    {
                        VmArrayContractS105.RequireType(field.DataType, value);
                        value = value is VmNumberArrayS105 ? ((VmNumberArrayS105)value).ToArray().Cast<object>().ToList()
                            : Enumerable.Range(0, ((VmStringArrayS105)value).Count).Select(i => (object)((VmStringArrayS105)value)[i]).ToList();
                    }
                    else if (field.DataType == "Number")
                    {
                        if (!(value is double || value is float || value is decimal || value is int || value is long || value is short || value is byte || value is uint || value is ushort || value is ulong || value is sbyte))
                            throw new InvalidOperationException("需要 Number，不接受数字字符串或布尔值。");
                        value = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                        if (double.IsNaN((double)value) || double.IsInfinity((double)value)) throw new InvalidOperationException("需要有限 Number。");
                    }
                    else if (field.DataType == "String" ? !(value is string) : !(value is bool)) throw new InvalidOperationException("输入类型必须为 " + field.DataType + "。");
                    if (value is string) RequireText((string)value);
                    var strings = value as List<object>;
                    if (strings != null) foreach (var item in strings.OfType<string>()) RequireText(item);
                    var path = DecodePath(field.Path); var map = root;
                    for (int i = 0; i < path.Length - 1; i++)
                    {
                        object next;
                        if (!map.TryGetValue(path[i], out next)) map.Add(path[i], next = new Dictionary<string, object>(StringComparer.Ordinal));
                        map = next as Dictionary<string, object>;
                        if (map == null) throw new InvalidOperationException("组装字段父子路径冲突。");
                    }
                    if (path.Length == 0 || map.ContainsKey(path[path.Length - 1])) throw new InvalidOperationException("组装字段重复或覆盖根对象。");
                    map.Add(path[path.Length - 1], value);
                }
                catch (InvalidOperationException ex) { throw new InvalidOperationException(field.Name + "（" + field.Path + "）：" + ex.Message); }
            }
            var builder = new StringBuilder(); int nodes = 0; Emit(builder, root, ref nodes); return builder.ToString();
        }
        private static void Emit(StringBuilder text, object value, ref int nodes)
        {
            if (++nodes > 65536) throw new InvalidOperationException("生成 JSON 超过 65536 个值，未发布输出。");
            var map = value as Dictionary<string, object>; var array = value as List<object>;
            if (map != null)
            {
                text.Append('{'); bool first = true;
                foreach (var pair in map) { if (!first) text.Append(','); first = false; Quote(text, pair.Key); text.Append(':'); Emit(text, pair.Value, ref nodes); }
                text.Append('}');
            }
            else if (array != null)
            {
                text.Append('['); for (int i = 0; i < array.Count; i++) { if (i > 0) text.Append(','); Emit(text, array[i], ref nodes); } text.Append(']');
            }
            else if (value is string) Quote(text, (string)value);
            else if (value is bool) text.Append((bool)value ? "true" : "false");
            else if (value is double) text.Append(((double)value).ToString("R", CultureInfo.InvariantCulture));
            else throw new InvalidOperationException("不能生成未知类型或 null。");
            if (text.Length > MaxText) throw new InvalidOperationException("生成 JSON 超过 1048576 字符，未发布输出。");
        }
        private static void RequireText(string value)
        {
            if (value.Length > 262144) throw new InvalidOperationException("单个文本超过 262144 字符。");
            for (int i = 0; i < value.Length; i++)
                if (char.IsSurrogate(value[i]) && (!char.IsHighSurrogate(value[i]) || ++i >= value.Length || !char.IsLowSurrogate(value[i])))
                    throw new InvalidOperationException("文本包含不完整的 Unicode 代理项。");
        }
        private static void Quote(StringBuilder text, string value)
        {
            RequireText(value); text.Append('"');
            foreach (char c in value)
                if (c == '"' || c == '\\') text.Append('\\').Append(c);
                else if (c < 32) text.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                else text.Append(c);
            text.Append('"');
        }

        // Bounded strict parser. No CLR type metadata, object activation or evaluation.
        private sealed class Parser
        {
            private readonly string text; private int position, nodes;
            public Parser(string source)
            {
                text = source ?? string.Empty;
                if (text.Length > MaxText) throw new InvalidOperationException("JSON 文本超过 1048576 字符。");
            }
            private InvalidOperationException Error(string message) { return new InvalidOperationException("JSON 第 " + (position + 1) + " 字符：" + message); }
            private void Space() { while (position < text.Length && " \r\n\t".IndexOf(text[position]) >= 0) position++; }
            private bool Eat(char c) { Space(); if (position < text.Length && text[position] == c) { position++; return true; } return false; }
            public object Parse() { object value = Value(0); Space(); if (position != text.Length) throw Error("根值后存在多余内容。"); return value; }
            private object Value(int depth)
            {
                if (depth > 32 || ++nodes > 65536) throw Error("超过 32 层或 65536 个值的容量限制。");
                Space(); if (position >= text.Length) throw Error("缺少值。");
                char c = text[position];
                if (c == '"') return String();
                if (c == '{')
                {
                    position++; var map = new Dictionary<string, object>(StringComparer.Ordinal); if (Eat('}')) return map;
                    do
                    {
                        Space(); if (position >= text.Length || text[position] != '"') throw Error("对象键必须是双引号文本。");
                        string key = String(); if (!Eat(':')) throw Error("字段后缺少冒号。");
                        if (map.ContainsKey(key)) throw Error("对象包含重复键：" + VmArrayContractS105.ShortText(key));
                        map.Add(key, Value(depth + 1));
                    } while (Eat(','));
                    if (!Eat('}')) throw Error("对象缺少右花括号。"); return map;
                }
                if (c == '[')
                {
                    position++; var items = new List<object>(); if (Eat(']')) return items;
                    do { if (items.Count >= 10000) throw Error("单个数组超过 10000 项。"); items.Add(Value(depth + 1)); } while (Eat(','));
                    if (!Eat(']')) throw Error("数组缺少右方括号。"); return items;
                }
                foreach (string literal in new[] { "true", "false", "null" })
                    if (position + literal.Length <= text.Length && string.CompareOrdinal(text, position, literal, 0, literal.Length) == 0)
                    { position += literal.Length; return literal == "null" ? null : (object)(literal == "true"); }
                int start = position;
                if (text[position] == '-') position++;
                if (position >= text.Length) throw Error("数字不完整。");
                if (text[position] == '0') position++;
                else { if (text[position] < '1' || text[position] > '9') throw Error("不是有效 JSON 值。"); Digits(); }
                if (position < text.Length && text[position] == '.') { position++; RequireDigits(); }
                if (position < text.Length && (text[position] == 'e' || text[position] == 'E'))
                { position++; if (position < text.Length && (text[position] == '+' || text[position] == '-')) position++; RequireDigits(); }
                double number;
                if (!double.TryParse(text.Substring(start, position - start), NumberStyles.Float, CultureInfo.InvariantCulture, out number) || double.IsNaN(number) || double.IsInfinity(number)) throw Error("数值超出有限 Number 范围。");
                return number;
            }
            private void Digits() { while (position < text.Length && text[position] >= '0' && text[position] <= '9') position++; }
            private void RequireDigits() { int start = position; Digits(); if (position == start) throw Error("缺少数字。"); }
            private string String()
            {
                position++; var value = new StringBuilder();
                while (position < text.Length)
                {
                    char c = text[position++];
                    if (c == '"') { string result = value.ToString(); RequireText(result); return result; }
                    if (c < 32) throw Error("文本中控制字符必须转义。");
                    if (c == '\\')
                    {
                        if (position >= text.Length) throw Error("文本转义不完整。");
                        c = text[position++];
                        switch (c)
                        {
                            case '"': case '\\': case '/': break;
                            case 'b': c = '\b'; break; case 'f': c = '\f'; break; case 'n': c = '\n'; break; case 'r': c = '\r'; break; case 't': c = '\t'; break;
                            case 'u':
                                int code;
                                if (position + 4 > text.Length || !int.TryParse(text.Substring(position, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out code)) throw Error("Unicode 转义必须为四位十六进制数。");
                                c = (char)code; position += 4; break;
                            default: throw Error("不支持的文本转义。");
                        }
                    }
                    value.Append(c); if (value.Length > 262144) throw Error("文本超过 262144 字符。");
                }
                throw Error("文本缺少结束双引号。");
            }
        }
    }
}
