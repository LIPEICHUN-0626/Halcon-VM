param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$CapturePath = (Join-Path $ProjectRoot 'artifacts\s82\s82-module-config-1366x768.png')
)
$ErrorActionPreference = 'Stop'
if ([Threading.Thread]::CurrentThread.ApartmentState -ne [Threading.ApartmentState]::STA) { throw 'Run with powershell.exe -STA.' }
$halcon = 'C:\Program Files\MVTec\HALCON-20.11-Progress'
$env:PATH = (Join-Path $halcon 'bin\x64-win64') + ';' + $env:PATH
Add-Type -AssemblyName PresentationFramework
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class S82Mouse {
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);
    public const uint LeftDown = 0x0002;
    public const uint LeftUp = 0x0004;
}
'@
[Reflection.Assembly]::LoadFrom((Join-Path $halcon 'bin\dotnet35\halcondotnet.dll')) | Out-Null
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path $ProjectRoot 'bin\x64\Debug\HalconWinFormsDemo.exe'))
function Pump([int]$ms) { $frame = New-Object Windows.Threading.DispatcherFrame; $timer = New-Object Windows.Threading.DispatcherTimer; $timer.Interval = [TimeSpan]::FromMilliseconds($ms); $timer.Add_Tick({ $timer.Stop(); $frame.Continue = $false }); $timer.Start(); [Windows.Threading.Dispatcher]::PushFrame($frame) }
function Assert([bool]$ok, [string]$message) { if (-not $ok) { throw "Assertion failed: $message" } }
function Shot([string]$path) {
    [IO.Directory]::CreateDirectory((Split-Path -Parent $path)) | Out-Null
    $window.UpdateLayout(); $root = $window.Content; $top = $root.PointToScreen([Windows.Point]::new(0, 0)); $end = $root.PointToScreen([Windows.Point]::new($root.ActualWidth, $root.ActualHeight))
    $screen = [Windows.Forms.Screen]::FromPoint([Drawing.Point]::new([int]$top.X, [int]$top.Y)).Bounds
    $width = [int][Math]::Min($end.X - $top.X, $screen.Right - $top.X); $height = [int][Math]::Min($end.Y - $top.Y, $screen.Bottom - $top.Y)
    $bmp = New-Object Drawing.Bitmap($width, $height); $graphics = [Drawing.Graphics]::FromImage($bmp)
    try { $graphics.CopyFromScreen([int]$top.X, [int]$top.Y, 0, 0, $bmp.Size); $bmp.Save($path, [Drawing.Imaging.ImageFormat]::Png) } finally { $graphics.Dispose(); $bmp.Dispose() }
}

$mainType = $assembly.GetType('HalconWinFormsDemo.MainWindow', $true)
$serviceType = $assembly.GetType('HalconWinFormsDemo.Services.RecipeService', $true)
$loggerType = $assembly.GetType('HalconWinFormsDemo.Services.AppLogger', $true)
$kindType = $assembly.GetType('HalconWinFormsDemo.Models.VmToolKind', $true)
$private = [Reflection.BindingFlags]'Instance,NonPublic'; $all = [Reflection.BindingFlags]'Instance,NonPublic,Public'
$temp = Join-Path ([IO.Path]::GetTempPath()) ('halcon-s82-ui-' + [Guid]::NewGuid().ToString('N')); [IO.Directory]::CreateDirectory($temp) | Out-Null
$service = $serviceType.GetConstructor([Type[]]@([string])).Invoke([object[]]@($temp.ToString()))
$logger = $loggerType.GetConstructor([Type[]]@([string])).Invoke([object[]]@($temp.ToString()))
$window = $mainType.GetConstructor($private, $null, [Type[]]@($serviceType, $loggerType), $null).Invoke([object[]]@($service, $logger))
function F([string]$name) { $field = $mainType.GetField($name, $all); if ($null -eq $field) { throw "Missing field $name" }; $value = $field.GetValue($window); Write-Output -NoEnumerate $value }
function M([string]$name, [object[]]$invokeArgs = @()) { try { $method = $mainType.GetMethods($private) | Where-Object Name -eq $name | Where-Object { $_.GetParameters().Count -eq $invokeArgs.Count } | Select-Object -First 1; if ($null -eq $method) { throw "Missing $name/$($invokeArgs.Count)" }; $method.Invoke($window, $invokeArgs) } catch [Reflection.TargetInvocationException] { throw $_.Exception.InnerException } }
function Tool([string]$kind, [string]$name) { $mainType.GetMethod('CreateFlowTool', $private).Invoke($window, [object[]]@([Enum]::Parse($kindType, $kind), $name, $true, $null)) }
function DoubleClickCard($tool) {
    $viewport = F 'FlowGraphViewport'; $viewport.ScrollToVerticalOffset([Math]::Max(0, $tool.GraphY - 24)); Pump 250
    $items = F 'FlowGraphNodeItemsControl'; $presenter = $items.ItemContainerGenerator.ContainerFromItem($tool)
    Assert($null -ne $presenter) 'Card presenter is unavailable'
    $card = $presenter.ContentTemplate.FindName('GraphToolCard', $presenter); Assert($null -ne $card) 'Card is unavailable'
    $card.BringIntoView(); Pump 250
    $point = $card.PointToScreen([Windows.Point]::new(150, 24)); [S82Mouse]::SetCursorPos([int]$point.X, [int]$point.Y) | Out-Null
    [S82Mouse]::mouse_event([S82Mouse]::LeftDown, 0, 0, 0, [UIntPtr]::Zero); [S82Mouse]::mouse_event([S82Mouse]::LeftUp, 0, 0, 0, [UIntPtr]::Zero); Pump 90
    [S82Mouse]::mouse_event([S82Mouse]::LeftDown, 0, 0, 0, [UIntPtr]::Zero); [S82Mouse]::mouse_event([S82Mouse]::LeftUp, 0, 0, 0, [UIntPtr]::Zero); Pump 650
}

$app = [Windows.Application]::Current; if ($null -eq $app) { $app = New-Object Windows.Application; $app.ShutdownMode = [Windows.ShutdownMode]::OnExplicitShutdown }
try {
    $window.Topmost = $true; $window.WindowState = [Windows.WindowState]::Normal; $window.MinWidth = 0; $window.MinHeight = 0; $window.Left = 0; $window.Top = 0; $window.Width = 1366; $window.Height = 768; $window.Show(); Pump 700
    $handle = New-Object Windows.Interop.WindowInteropHelper -ArgumentList $window; [S82Mouse]::SetForegroundWindow($handle.Handle) | Out-Null
    $flow = F 'flowTools'; M 'DisposeFlowTools' | Out-Null; $flow.Clear()
    $source = Tool ImageSource 'Input Image'; $ocr = Tool OcrRecognition 'OCR Reader'; $judge = Tool NumericJudge 'Character Count'
    $flow.Add($source); $flow.Add($ocr); $flow.Add($judge); $ocr.SetInputBinding('Image', $source.ToolId, 'Image'); $judge.SetInputBinding('Value', $ocr.ToolId, 'CharacterCount')
    $source.Parameters.LocalImagePath = Join-Path $env:PUBLIC 'Documents\MVTec\HALCON-20.11-Progress\examples\images\barcode\code128\code12801.png'
    M 'RefreshUiState' | Out-Null; M 'RefreshFlowGraphLayout' | Out-Null; Pump 700

    DoubleClickCard $ocr
    Assert((F 'CentralModuleConfigurationOverlayS77').Visibility.ToString() -eq 'Visible') 'A card double-click did not open central configuration'
    Assert([object]::ReferenceEquals((F 'dockEditingTool'), $ocr)) 'A card double-click opened the wrong tool'
    Assert($null -eq (F 'flowNodeDragSession') -and -not (F 'isManualFlowLayout')) 'A double-click unexpectedly left graph dragging active'
    Assert((F 'DockIdentityHeaderRowS82').Height.Value -eq 0) 'Duplicated inner identity header is still visible in central mode'
    Assert((F 'CentralModuleConfigurationTitleS77').Text -eq '#02  OCR Reader') 'Central title is not a single concise tool identity'
    Assert((F 'DockKeyParameterSummaryText').Text -eq '本工具常用 4 项 · 已按任务排序') 'OCR task profile does not limit the first decision to four relevant fields'
    Assert((F 'DockAdvancedParameterToggleButton').Content.ToString() -match '^更多参数') 'Advanced fields are not presented as an on-demand action'
    Assert((F 'DockTrialRunButton').Content.ToString() -match '试运行当前模块') 'Trial-run action is ambiguous'

    DoubleClickCard $judge
    Assert([object]::ReferenceEquals((F 'dockEditingTool'), $judge)) 'A second double-click did not switch the central task'
    Assert((F 'CentralModuleConfigurationTitleS77').Text -eq '#03  Character Count') 'Central title did not follow the clicked tool'
    DoubleClickCard $ocr; Shot $CapturePath

    M 'OpenCentralModuleConfigurationS77' @($source) | Out-Null
    Assert([object]::ReferenceEquals((F 'dockEditingTool'), $source)) 'Source task was not active before F5'
    $key = New-Object Windows.Input.KeyEventArgs([Windows.Input.Keyboard]::PrimaryDevice, [Windows.PresentationSource]::FromVisual($window), 0, [Windows.Input.Key]::F5)
    $key = $key.PSObject.BaseObject
    $key.RoutedEvent = [Windows.Input.Keyboard]::PreviewKeyDownEvent
    M 'MainWindow_PreviewKeyDown' @($window, $key) | Out-Null; Pump 500
    Assert($source.ResultCode -eq 'OK' -and $ocr.ResultCode -eq '--' -and $judge.ResultCode -eq '--') ("F5 current-module contract failed: source="+$source.ResultCode+" ocr="+$ocr.ResultCode+" judge="+$judge.ResultCode)
    Assert($key.Handled) 'Central F5 was not consumed by the current-module task'

    Assert((M 'CloseCentralModuleConfigurationS77')) 'Clean central configuration close failed'
    Assert((F 'DockIdentityHeaderRowS82').Height.IsAuto) 'Right-side identity header was not restored after closing'
    $xaml = Get-Content (Join-Path $ProjectRoot 'MainWindow.xaml') -Raw
    Assert($xaml -match 'GraphToolCard"[^>]*Cursor="SizeAll"' -and $xaml -match 'FlowGraphNode_MouseLeftButtonDown') 'Double-click/config and single-drag XAML contract regressed'
    Write-Output "S82_MODULE_CONFIGURATION_UI=PASS; DoubleClick=Central; SingleDrag=Enabled; Common=4; F5=CurrentOnly; Capture=$CapturePath"
}
finally {
    try { $mainType.GetField('recipeCloseConfirmed', $private).SetValue($window, $true); $window.Close(); $app.Shutdown() } catch {}
    if (Test-Path $temp) { Remove-Item -LiteralPath $temp -Recurse -Force }
}
