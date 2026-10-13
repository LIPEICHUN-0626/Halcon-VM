using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace HalconWinFormsDemo.Models
{
    // A bounded arithmetic grammar, not a compiler or a scripting host.
    public static class VmNumericExpressionS106
    {
        public sealed class Program
        {
            private readonly Node root;
            public IList<string> Variables { get; private set; }
            internal Program(Node value, IEnumerable<string> variables)
            { root = value; Variables = variables.OrderBy(item => item, StringComparer.Ordinal).ToList().AsReadOnly(); }
            public double Evaluate(Func<string, double> resolve) { return Finite(root.Evaluate(resolve)); }
        }

        internal abstract class Node { internal abstract double Evaluate(Func<string, double> resolve); }
        private sealed class Constant : Node
        {
            private readonly double value;
            public Constant(double value) { this.value = value; }
            internal override double Evaluate(Func<string, double> resolve) { return value; }
        }
        private sealed class Variable : Node
        {
            private readonly string name;
            public Variable(string name) { this.name = name; }
            internal override double Evaluate(Func<string, double> resolve) { return Finite(resolve(name)); }
        }
        private sealed class Unary : Node
        {
            private readonly Node value;
            public Unary(Node value) { this.value = value; }
            internal override double Evaluate(Func<string, double> resolve) { return Finite(-value.Evaluate(resolve)); }
        }
        private sealed class Binary : Node
        {
            private readonly char operation;
            private readonly Node left, right;
            public Binary(char operation, Node left, Node right) { this.operation = operation; this.left = left; this.right = right; }
            internal override double Evaluate(Func<string, double> resolve)
            {
                double a = left.Evaluate(resolve), b = right.Evaluate(resolve);
                if ((operation == '/' || operation == '%') && b == 0) throw new InvalidOperationException("公式除数或余数除数不能为 0。");
                switch (operation)
                {
                    case '+': return Finite(a + b);
                    case '-': return Finite(a - b);
                    case '*': return Finite(a * b);
                    case '/': return Finite(a / b);
                    case '%': return Finite(a % b);
                    default: return Finite(Math.Pow(a, b));
                }
            }
        }
        private sealed class Function : Node
        {
            private readonly string name;
            private readonly Node[] arguments;
            public Function(string name, Node[] arguments) { this.name = name; this.arguments = arguments; }
            internal override double Evaluate(Func<string, double> resolve)
            {
                double[] a = arguments.Select(argument => argument.Evaluate(resolve)).ToArray();
                double result;
                switch (name)
                {
                    case "abs": result = Math.Abs(a[0]); break;
                    case "sqrt": result = Math.Sqrt(a[0]); break;
                    case "sin": result = Math.Sin(a[0]); break;
                    case "cos": result = Math.Cos(a[0]); break;
                    case "tan": result = Math.Tan(a[0]); break;
                    case "log": result = Math.Log(a[0]); break;
                    case "exp": result = Math.Exp(a[0]); break;
                    case "floor": result = Math.Floor(a[0]); break;
                    case "ceil": result = Math.Ceiling(a[0]); break;
                    case "min": result = Math.Min(a[0], a[1]); break;
                    case "max": result = Math.Max(a[0], a[1]); break;
                    case "pow": result = Math.Pow(a[0], a[1]); break;
                    case "round":
                        if (a[1] < 0 || a[1] > 15 || a[1] != Math.Truncate(a[1])) throw new InvalidOperationException("round 的小数位必须为 0–15 的整数。");
                        result = Math.Round(a[0], (int)a[1], MidpointRounding.AwayFromZero); break;
                    case "clamp":
                        if (a[1] > a[2]) throw new InvalidOperationException("clamp 下限不能大于上限。");
                        result = Math.Max(a[1], Math.Min(a[2], a[0])); break;
                    default: throw new InvalidOperationException("公式包含不支持的函数。");
                }
                return Finite(result);
            }
        }

        private static double Finite(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new InvalidOperationException("公式结果不是有限数值，请检查数值范围、开方、对数或幂的定义域。");
            return value;
        }

        public static Program Compile(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new InvalidOperationException("请输入数值公式。");
            if (text.Length > 1024) throw new InvalidOperationException("公式最多 1024 个字符。");
            var parser = new Parser(text);
            Node root = parser.Expression();
            parser.RequireEnd();
            return new Program(root, parser.Variables);
        }

        private sealed class Parser
        {
            private readonly string text;
            private int position, depth, nodes;
            public readonly HashSet<string> Variables = new HashSet<string>(StringComparer.Ordinal);
            public Parser(string text) { this.text = text; }
            private Exception Error(string message) { return new InvalidOperationException("公式第 " + (position + 1) + " 位：" + message); }
            private void Space() { while (position < text.Length && char.IsWhiteSpace(text[position])) position++; }
            private bool Take(char c) { Space(); if (position < text.Length && text[position] == c) { position++; return true; } return false; }
            private Node Count(Node node) { if (++nodes > 256) throw Error("公式最多 256 个运算/值节点，请拆为两个公式模块。"); return node; }
            private void Enter() { if (++depth > 32) throw Error("公式嵌套不能超过 32 层。"); }
            public void RequireEnd() { Space(); if (position != text.Length) throw Error("存在未识别字符；只接受数值、A/B/C/D、函数和算术运算。"); }
            public Node Expression()
            {
                Enter();
                try
                {
                    Node node = Product();
                    while (true)
                    {
                        if (Take('+')) node = Count(new Binary('+', node, Product()));
                        else if (Take('-')) node = Count(new Binary('-', node, Product()));
                        else return node;
                    }
                }
                finally { depth--; }
            }
            private Node Product()
            {
                Node node = Signed();
                while (true)
                {
                    if (Take('*')) node = Count(new Binary('*', node, Signed()));
                    else if (Take('/')) node = Count(new Binary('/', node, Signed()));
                    else if (Take('%')) node = Count(new Binary('%', node, Signed()));
                    else return node;
                }
            }
            private Node Signed()
            {
                Enter();
                try
                {
                    if (Take('+')) return Signed();
                    if (Take('-')) return Count(new Unary(Signed()));
                    Node value = Primary();
                    // Power is right associative and binds tighter than unary minus: -2^2 = -4.
                    return Take('^') ? Count(new Binary('^', value, Signed())) : value;
                }
                finally { depth--; }
            }
            private Node Primary()
            {
                Space();
                if (Take('('))
                {
                    Node node = Expression();
                    if (!Take(')')) throw Error("缺少右括号 )。");
                    return node;
                }
                if (position >= text.Length) throw Error("缺少数值或变量。");
                int start = position;
                if (char.IsDigit(text[position]) || text[position] == '.')
                {
                    while (position < text.Length && (char.IsDigit(text[position]) || text[position] == '.')) position++;
                    if (position < text.Length && (text[position] == 'e' || text[position] == 'E'))
                    {
                        position++;
                        if (position < text.Length && (text[position] == '+' || text[position] == '-')) position++;
                        while (position < text.Length && char.IsDigit(text[position])) position++;
                    }
                    double value;
                    if (!double.TryParse(text.Substring(start, position - start), NumberStyles.Float, CultureInfo.InvariantCulture, out value) || double.IsNaN(value) || double.IsInfinity(value)) throw Error("数值格式无效或超出范围，使用英文小数点。");
                    return Count(new Constant(value));
                }
                if (!char.IsLetter(text[position])) throw Error("应为数值、变量、函数或左括号。");
                while (position < text.Length && char.IsLetterOrDigit(text[position])) position++;
                string name = text.Substring(start, position - start).ToLowerInvariant();
                if (name.Length == 1 && name[0] >= 'a' && name[0] <= 'd')
                {
                    string variable = name.ToUpperInvariant(); Variables.Add(variable); return Count(new Variable(variable));
                }
                if (name == "pi") return Count(new Constant(Math.PI));
                if (name == "e") return Count(new Constant(Math.E));
                int arity;
                switch (name)
                {
                    case "abs": case "sqrt": case "sin": case "cos": case "tan": case "log": case "exp": case "floor": case "ceil": arity = 1; break;
                    case "min": case "max": case "pow": case "round": arity = 2; break;
                    case "clamp": arity = 3; break;
                    default: throw Error("未知名称 " + name + "；变量仅支持 A/B/C/D。");
                }
                if (!Take('(')) throw Error("函数后需要左括号 (。");
                var arguments = new Node[arity];
                for (int i = 0; i < arity; i++)
                {
                    if (i > 0 && !Take(',')) throw Error(name + " 需要 " + arity + " 个逗号分隔参数。");
                    arguments[i] = Expression();
                }
                if (!Take(')')) throw Error(name + " 需要 " + arity + " 个参数及右括号 )。");
                return Count(new Function(name, arguments));
            }
        }
    }
}
