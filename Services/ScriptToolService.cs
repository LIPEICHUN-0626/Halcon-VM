using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using HalconWinFormsDemo.Models;
using Microsoft.CSharp;

namespace HalconWinFormsDemo.Services
{
    public sealed class ScriptToolService
    {
        public ScriptToolExecutionReport Validate(ScriptToolExecutionRequest request)
        {
            if (request == null) throw new ArgumentNullException("request");
            string mode = VmScriptExecutionMode.Normalize(request.Mode);
            string assemblyPath = mode == VmScriptExecutionMode.InlineCSharp
                ? CompileInlineAssembly(request.InlineCode)
                : Path.GetFullPath(request.AssemblyPath ?? string.Empty);
            if (!File.Exists(assemblyPath)) throw new FileNotFoundException("脚本 DLL 不存在。", assemblyPath);
            string typeName = mode == VmScriptExecutionMode.InlineCSharp ? "HalconVmUserScript.ScriptEntry" : (request.TypeName ?? string.Empty).Trim();
            string methodName = mode == VmScriptExecutionMode.InlineCSharp ? "Execute" : (request.MethodName ?? string.Empty).Trim();
            string hash = ComputeSha256(assemblyPath);
            string version = ReadAssemblyVersion(assemblyPath);
            AppDomain domain = null;
            try
            {
                domain = CreateScriptAppDomain();
                ScriptToolAppDomainRunner runner = CreateRunner(domain);
                runner.ValidateEntry(assemblyPath, typeName, methodName);
                return new ScriptToolExecutionReport
                {
                    AssemblySha256 = hash,
                    AssemblyVersion = version,
                    ElapsedMilliseconds = 0,
                    Diagnostics = string.Format(CultureInfo.InvariantCulture, "编译/入口检查通过 · {0} · {1}.{2} · v{3} · SHA-256 {4}", mode, typeName, methodName, version, hash.Substring(0, 12))
                };
            }
            finally
            {
                if (domain != null)
                {
                    try { AppDomain.Unload(domain); }
                    catch (CannotUnloadAppDomainException) { }
                }
            }
        }

        public ScriptToolExecutionReport Execute(ScriptToolExecutionRequest request)
        {
            if (request == null) throw new ArgumentNullException("request");
            string mode = VmScriptExecutionMode.Normalize(request.Mode);
            string assemblyPath = mode == VmScriptExecutionMode.InlineCSharp
                ? CompileInlineAssembly(request.InlineCode)
                : Path.GetFullPath(request.AssemblyPath ?? string.Empty);
            if (!File.Exists(assemblyPath)) throw new FileNotFoundException("脚本 DLL 不存在。", assemblyPath);

            string typeName = mode == VmScriptExecutionMode.InlineCSharp
                ? "HalconVmUserScript.ScriptEntry"
                : (request.TypeName ?? string.Empty).Trim();
            string methodName = mode == VmScriptExecutionMode.InlineCSharp
                ? "Execute"
                : (request.MethodName ?? string.Empty).Trim();
            string hash = ComputeSha256(assemblyPath);
            string version = ReadAssemblyVersion(assemblyPath);
            int timeout = request.TimeoutMilliseconds;
            if (timeout < 50 || timeout > 30000) throw new InvalidOperationException("脚本超时必须在 50 到 30000 ms 之间。");

            AppDomain domain = null;
            bool unloaded = false;
            Stopwatch stopwatch = Stopwatch.StartNew();
            try
            {
                domain = CreateScriptAppDomain();
                ScriptToolAppDomainRunner runner = CreateRunner(domain);
                Task<ScriptToolResult> task = Task.Factory.StartNew(
                    delegate { return runner.Execute(assemblyPath, typeName, methodName, request.Context ?? new ScriptToolContext()); });
                if (!task.Wait(timeout))
                {
                    AppDomain.Unload(domain);
                    unloaded = true;
                    throw new TimeoutException("脚本执行超过 " + timeout.ToString(CultureInfo.InvariantCulture) + " ms，隔离 AppDomain 已卸载。");
                }

                ScriptToolResult result = task.Result;
                ValidateResult(result);
                stopwatch.Stop();
                return new ScriptToolExecutionReport
                {
                    Result = result,
                    AssemblySha256 = hash,
                    AssemblyVersion = version,
                    ElapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds,
                    Diagnostics = string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} · {1}.{2} · v{3} · SHA-256 {4}",
                        mode,
                        typeName,
                        methodName,
                        version,
                        hash.Substring(0, 12))
                };
            }
            catch (AggregateException ex)
            {
                Exception inner = ex.Flatten().InnerExceptions.FirstOrDefault() ?? ex;
                throw new InvalidOperationException("脚本执行失败：" + inner.Message, inner);
            }
            finally
            {
                stopwatch.Stop();
                if (domain != null && !unloaded)
                {
                    try { AppDomain.Unload(domain); }
                    catch (CannotUnloadAppDomainException) { }
                }
            }
        }

        private static AppDomain CreateScriptAppDomain()
        {
            AppDomainSetup setup = new AppDomainSetup
            {
                ApplicationBase = Path.GetDirectoryName(typeof(ScriptToolService).Assembly.Location),
                ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile,
                // The host is a WPF executable rather than a lightweight contract DLL.
                // Loading that executable through the CLR shadow-copy cache is unreliable
                // after repeated Debug/Release rebuilds (STG_E_ABNORMALAPIEXIT). User
                // assemblies are versioned by the resource service, so the host itself
                // does not need to be shadow-copied in this short-lived AppDomain.
                ShadowCopyFiles = "false"
            };
            return AppDomain.CreateDomain("HalconVmScript-" + Guid.NewGuid().ToString("N"), null, setup);
        }

        private static ScriptToolAppDomainRunner CreateRunner(AppDomain domain)
        {
            return (ScriptToolAppDomainRunner)domain.CreateInstanceFromAndUnwrap(
                typeof(ScriptToolAppDomainRunner).Assembly.Location,
                typeof(ScriptToolAppDomainRunner).FullName);
        }

        private static string CompileInlineAssembly(string body)
        {
            if (string.IsNullOrWhiteSpace(body)) throw new InvalidOperationException("内联 C# 代码不能为空。");
            string source =
                "using System;\r\n" +
                "using HalconWinFormsDemo.Models;\r\n" +
                "namespace HalconVmUserScript {\r\n" +
                "  public static class ScriptEntry {\r\n" +
                "    public static ScriptToolResult Execute(ScriptToolContext input) {\r\n" +
                body + "\r\n" +
                "    }\r\n" +
                "  }\r\n" +
                "}\r\n";
            string sourceHash = ComputeSha256(Encoding.UTF8.GetBytes(source));
            string directory = Path.Combine(Path.GetTempPath(), "HalconVmScriptCache");
            Directory.CreateDirectory(directory);
            string output = Path.Combine(directory, "script-" + sourceHash + ".dll");
            if (File.Exists(output)) return output;

            CompilerParameters options = new CompilerParameters
            {
                GenerateExecutable = false,
                GenerateInMemory = false,
                IncludeDebugInformation = false,
                TreatWarningsAsErrors = false,
                WarningLevel = 4,
                CompilerOptions = "/optimize+ /platform:x64",
                OutputAssembly = output
            };
            options.ReferencedAssemblies.Add("System.dll");
            options.ReferencedAssemblies.Add("System.Core.dll");
            options.ReferencedAssemblies.Add(typeof(ScriptToolContext).Assembly.Location);
            using (CSharpCodeProvider provider = new CSharpCodeProvider())
            {
                CompilerResults results = provider.CompileAssemblyFromSource(options, source);
                if (results.Errors.HasErrors)
                {
                    string diagnostics = string.Join(" | ", results.Errors.Cast<CompilerError>()
                        .Where(item => !item.IsWarning)
                        .Take(8)
                        .Select(item => "L" + Math.Max(1, item.Line - 5).ToString(CultureInfo.InvariantCulture) + ": " + item.ErrorText));
                    try { if (File.Exists(output)) File.Delete(output); } catch { }
                    throw new InvalidOperationException("内联 C# 编译失败：" + diagnostics);
                }
            }
            return output;
        }

        private static void ValidateResult(ScriptToolResult result)
        {
            if (result == null) throw new InvalidOperationException("脚本返回 null；必须返回 ScriptToolResult。");
            result.Text = result.Text ?? string.Empty;
            result.ResultCode = (result.ResultCode ?? string.Empty).Trim().ToUpperInvariant();
            if (result.ResultCode != "OK" && result.ResultCode != "NG")
                throw new InvalidOperationException("脚本 ResultCode 仅允许 OK 或 NG。");
            if (result.IsOk != (result.ResultCode == "OK"))
                throw new InvalidOperationException("脚本 IsOk 必须与 ResultCode 保持一致。");
            if (result.HasNumber && (double.IsNaN(result.Number) || double.IsInfinity(result.Number)))
                throw new InvalidOperationException("脚本 Number 输出必须是有限数值，不能为 NaN 或 Infinity。");
        }

        private static string ReadAssemblyVersion(string path)
        {
            try
            {
                Version version = AssemblyName.GetAssemblyName(path).Version;
                return version == null ? "0.0.0.0" : version.ToString();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("无法读取脚本程序集版本：" + ex.Message, ex);
            }
        }

        private static string ComputeSha256(string path)
        {
            using (FileStream stream = File.OpenRead(path))
            using (SHA256 sha = SHA256.Create())
            {
                return ToHex(sha.ComputeHash(stream));
            }
        }

        private static string ComputeSha256(byte[] value)
        {
            using (SHA256 sha = SHA256.Create())
            {
                return ToHex(sha.ComputeHash(value));
            }
        }

        private static string ToHex(byte[] value)
        {
            StringBuilder builder = new StringBuilder(value.Length * 2);
            foreach (byte item in value) builder.Append(item.ToString("x2", CultureInfo.InvariantCulture));
            return builder.ToString();
        }
    }

    public sealed class ScriptToolAppDomainRunner : MarshalByRefObject
    {
        public void ValidateEntry(string assemblyPath, string typeName, string methodName)
        {
            ResolveEntry(assemblyPath, typeName, methodName);
        }

        public ScriptToolResult Execute(string assemblyPath, string typeName, string methodName, ScriptToolContext context)
        {
            MethodInfo method = ResolveEntry(assemblyPath, typeName, methodName);
            try
            {
                return (ScriptToolResult)method.Invoke(null, new object[] { context });
            }
            catch (TargetInvocationException ex)
            {
                Exception inner = ex.InnerException ?? ex;
                throw new InvalidOperationException(inner.GetType().Name + ": " + inner.Message, inner);
            }
        }

        private static MethodInfo ResolveEntry(string assemblyPath, string typeName, string methodName)
        {
            Assembly assembly = Assembly.LoadFrom(assemblyPath);
            Type type = assembly.GetType(typeName, false, false);
            if (type == null) throw new InvalidOperationException("脚本类型不存在：" + typeName);
            MethodInfo method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(ScriptToolContext) }, null);
            if (method == null)
                throw new InvalidOperationException("脚本入口签名无效；需要 public static ScriptToolResult " + methodName + "(ScriptToolContext input)。");
            if (method.ReturnType != typeof(ScriptToolResult))
                throw new InvalidOperationException("脚本入口返回类型必须是 ScriptToolResult。");
            return method;
        }

        public override object InitializeLifetimeService()
        {
            return null;
        }
    }
}
