param(
  [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot),
  [string]$WidePath = (Join-Path $ProjectRoot 'artifacts\s65\s65-full-toolbox-1938x1158.png'),
  [string]$CompactPath = (Join-Path $ProjectRoot 'artifacts\s65\s65-pointset-contract-1366x768.png')
)
$ErrorActionPreference = 'Stop'
if ([Threading.Thread]::CurrentThread.ApartmentState -ne [Threading.ApartmentState]::STA) { throw 'Run with powershell.exe -STA.' }
$halcon = 'C:\Program Files\MVTec\HALCON-20.11-Progress'; $env:PATH = (Join-Path $halcon 'bin\x64-win64') + ';' + $env:PATH
Add-Type -AssemblyName PresentationFramework
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path $ProjectRoot 'bin\x64\Debug\HalconWinFormsDemo.exe'))
function Assert([bool]$Condition, [string]$Message) { if (-not $Condition) { throw $Message } }
function Pump([int]$Milliseconds) { $frame = New-Object Windows.Threading.DispatcherFrame; $timer = New-Object Windows.Threading.DispatcherTimer; $timer.Interval = [TimeSpan]::FromMilliseconds($Milliseconds); $timer.Add_Tick({ $timer.Stop(); $frame.Continue = $false }); $timer.Start(); [Windows.Threading.Dispatcher]::PushFrame($frame) }
function Shot([string]$Path, [int]$Width, [int]$Height) { $directory = Split-Path -Parent $Path; if (-not (Test-Path $directory)) { [IO.Directory]::CreateDirectory($directory) | Out-Null }; $window.UpdateLayout(); [double]$dpiX = 96.0 * $Width / [Math]::Max(1.0, [double]$window.ActualWidth); [double]$dpiY = 96.0 * $Height / [Math]::Max(1.0, [double]$window.ActualHeight); $render = New-Object Windows.Media.Imaging.RenderTargetBitmap -ArgumentList $Width,$Height,$dpiX,$dpiY,([Windows.Media.PixelFormats]::Pbgra32); $render.Render($window); $encoder = New-Object Windows.Media.Imaging.PngBitmapEncoder; $encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($render)); $stream = [IO.File]::Open([IO.Path]::GetFullPath($Path), [IO.FileMode]::Create); try { $encoder.Save($stream) } finally { $stream.Dispose() } }
$main = $assembly.GetType('HalconWinFormsDemo.MainWindow', $true); $serviceType = $assembly.GetType('HalconWinFormsDemo.Services.RecipeService', $true); $loggerType = $assembly.GetType('HalconWinFormsDemo.Services.AppLogger', $true); $kindType = $assembly.GetType('HalconWinFormsDemo.Models.VmToolKind', $true)
$private = [Reflection.BindingFlags]'Instance,NonPublic'; $all = [Reflection.BindingFlags]'Instance,NonPublic,Public'; $temp = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s65-ui-' + [Guid]::NewGuid().ToString('N')); [IO.Directory]::CreateDirectory($temp) | Out-Null
$service = $serviceType.GetConstructor([Type[]]@([string])).Invoke([object[]]@($temp.ToString())); $logger = $loggerType.GetConstructor([Type[]]@([string])).Invoke([object[]]@($temp.ToString())); $window = $main.GetConstructor($private, $null, [Type[]]@($serviceType, $loggerType), $null).Invoke([object[]]@($service, $logger))
function Field([string]$Name) { $value = $main.GetField($Name, $all).GetValue($window); Write-Output -NoEnumerate $value }
function Method([string]$Name, [object[]]$Parameters = @()) { try { $main.GetMethod($Name, $private).Invoke($window, $Parameters) } catch { $error = $_.Exception; while ($error.InnerException) { $error = $error.InnerException }; throw ($Name + ': ' + $error.Message) } }
function Kind([string]$Name) { [Enum]::Parse($kindType, $Name) }
$app = [Windows.Application]::Current; if ($null -eq $app) { $app = New-Object Windows.Application; $app.ShutdownMode = [Windows.ShutdownMode]::OnExplicitShutdown }
try {
  $window.Topmost = $true; $window.Show(); $window.WindowState = [Windows.WindowState]::Normal; $window.MinWidth = 0; $window.MinHeight = 0; $window.Left = 2; $window.Top = 2; $window.Width = 1938; $window.Height = 1158; Pump 600
  Assert ((Field 'toolCatalog').Count -eq 60) 'Full catalog is not visible.'
  Assert (-not (Field 'ToolCapabilityExpander').IsExpanded) 'Capability panel is not collapsed on the default first screen.'
  (Field 'ToolSearchTextBox').Text = ''; Method 'RefreshToolCatalogView' | Out-Null; Pump 500; Shot $WidePath 1938 1158

  $flow = Field 'flowTools'; Method 'DisposeFlowTools' | Out-Null; $flow.Clear()
  $circle = Method 'CreateFlowTool' ([object[]]@((Kind 'CircularCaliperCircleFinder'), '圆周卡尺找圆', $true, $null))
  $stats = Method 'CreateFlowTool' ([object[]]@((Kind 'PointSetGeometryStatistics'), '点集几何统计', $true, $null))
  $flow.Add($circle); $flow.Add($stats); $stats.SetInputBinding('Points', $circle.ToolId, 'EdgePoints')
  (Field 'FlowToolList').SelectedItem = $stats; (Field 'RightTabs').SelectedItem = (Field 'DockConfigurationTab'); (Field 'DockConfigurationTabs').SelectedItem = (Field 'DockParametersTaskTab'); Method 'LoadDockConfigurationDraft' ([object[]]@($stats)) | Out-Null; (Field 'DockBasicParametersScrollViewer').ScrollToTop()
  $catalogItem = $null; foreach ($entry in (Field 'toolCatalog')) { if ($entry.Kind.ToString() -eq 'PointSetGeometryStatistics') { $catalogItem = $entry; break } }
  (Field 'ToolCatalogList').SelectedItem = $catalogItem; (Field 'ToolCapabilityExpander').IsExpanded = $true; Method 'RefreshToolCapabilityPreviewS65' ([object[]]@($catalogItem)) | Out-Null
  $window.Width = 1366; $window.Height = 768; Pump 800
  Assert ((Field 'ToolCapabilityInputText').Text -match 'Point2D\[\]') 'Typed capability contract is not visible.'
  Assert ((Field 'DockPointSetGeometryPanel').Visibility.ToString() -eq 'Visible') 'Point-set dock configuration is not visible.'
  Shot $CompactPath 1366 768
  Write-Output "S65_UI_CAPTURE=PASS; Wide=$WidePath; Compact=$CompactPath; Catalog=60; DefaultDetailCollapsed=True; SelectedContract=Point2D[]"
}
finally { $main.GetField('recipeCloseConfirmed', $private).SetValue($window, $true); $window.Close(); $app.Shutdown(); if (Test-Path $temp) { Remove-Item -LiteralPath $temp -Recurse -Force } }
