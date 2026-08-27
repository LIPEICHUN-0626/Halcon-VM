param(
  [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot),
  [string]$WidePath = (Join-Path $ProjectRoot 'artifacts\s67\s67-port-recommendation-1938x1158.png'),
  [string]$CompactPath = (Join-Path $ProjectRoot 'artifacts\s67\s67-point-circle-workbench-1366x768.png')
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
$private = [Reflection.BindingFlags]'Instance,NonPublic'; $all = [Reflection.BindingFlags]'Instance,NonPublic,Public'; $temp = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s67-ui-' + [Guid]::NewGuid().ToString('N')); [IO.Directory]::CreateDirectory($temp) | Out-Null
$service = $serviceType.GetConstructor([Type[]]@([string])).Invoke([object[]]@($temp.ToString())); $logger = $loggerType.GetConstructor([Type[]]@([string])).Invoke([object[]]@($temp.ToString())); $window = $main.GetConstructor($private, $null, [Type[]]@($serviceType, $loggerType), $null).Invoke([object[]]@($service, $logger))
function Field([string]$Name) { $value = $main.GetField($Name, $all).GetValue($window); Write-Output -NoEnumerate $value }
function Method([string]$Name, [object[]]$Parameters = @()) { try { $main.GetMethod($Name, $private).Invoke($window, $Parameters) } catch { $error = $_.Exception; while ($error.InnerException) { $error = $error.InnerException }; throw ($Name + ': ' + $error.Message) } }
function Kind([string]$Name) { [Enum]::Parse($kindType, $Name) }
function Tool([string]$KindName, [string]$Name) { Method 'CreateFlowTool' ([object[]]@((Kind $KindName), $Name, $true, $null)) }
$app = [Windows.Application]::Current; if ($null -eq $app) { $app = New-Object Windows.Application; $app.ShutdownMode = [Windows.ShutdownMode]::OnExplicitShutdown }
try {
  $window.Topmost = $true; $window.Show(); $window.WindowState = [Windows.WindowState]::Normal; $window.MinWidth = 0; $window.MinHeight = 0; $window.Left = 2; $window.Top = 2
  $flow = Field 'flowTools'; Method 'DisposeFlowTools' | Out-Null; $flow.Clear()
  $source = Tool 'ImageSource' 'Input Image'; $points = Tool 'PointSetGeometryStatistics' 'Typed Point'; $edge = Tool 'SubpixelEdgeExtraction' 'Circle Edge'; $segment = Tool 'XldContourSegmentation' 'Arc Segments'; $fit = Tool 'XldCircleFitting' 'Typed Main Circle'; $radial = Tool 'PointToCircleRadialDeviationMeasurement' 'Radial Deviation'; $judge = Tool 'NumericJudge' 'Deviation Judge'
  foreach ($tool in @($source,$points,$edge,$segment,$fit,$radial,$judge)) { $flow.Add($tool) }
  $edge.SetInputBinding('Image',$source.ToolId,'Image'); $segment.SetInputBinding('XLD',$edge.ToolId,'XLD'); $fit.SetInputBinding('XLD',$segment.ToolId,'XLD'); $radial.SetInputBinding('Point',$points.ToolId,'Centroid'); $radial.SetInputBinding('Circle',$fit.ToolId,'MainCircle'); $judge.SetInputBinding('Value',$radial.ToolId,'AbsoluteDeviation'); $judge.InputToolId=$radial.ToolId; $judge.InputPortName='AbsoluteDeviation'
  Method 'RefreshUiState' | Out-Null
  Method 'EnterToolRecommendationContextS67' ([object[]]@($fit.ToolId,'MainCircle')) | Out-Null
  $candidate = $null; foreach ($entry in (Field 'toolCatalogView')) { $candidate = $entry; break }
  (Field 'ToolCatalogList').SelectedItem = $candidate; (Field 'ToolCapabilityExpander').IsExpanded = $true; Method 'RefreshToolCapabilityPreviewS65' ([object[]]@($candidate)) | Out-Null
  $window.Width = 1938; $window.Height = 1158; Pump 800
  Assert ((Field 'ToolRecommendationContextBorder').Visibility.ToString() -eq 'Visible' -and (Field 'ToolCatalogModePanel').Visibility.ToString() -eq 'Collapsed') 'Port recommendation context is not visible.'
  Shot $WidePath 1938 1158

  Method 'ExitToolRecommendationContextS67' ([object[]]@($false)) | Out-Null
  (Field 'ToolCapabilityExpander').IsExpanded = $false; (Field 'FlowToolList').SelectedItem = $radial; (Field 'RightTabs').SelectedItem = (Field 'DockConfigurationTab'); (Field 'DockConfigurationTabs').SelectedItem = (Field 'DockParametersTaskTab'); Method 'LoadDockConfigurationDraft' ([object[]]@($radial)) | Out-Null; (Field 'DockBasicParametersScrollViewer').ScrollToTop()
  $window.Width = 1366; $window.Height = 768; Pump 800
  Assert ((Field 'DockPointToCircleRadialDeviationPanel').Visibility.ToString() -eq 'Visible') 'Point-circle dock configuration is not visible.'
  Shot $CompactPath 1366 768
  Write-Output "S67_UI_CAPTURE=PASS; Wide=$WidePath; Compact=$CompactPath; Catalog=62; Recommendation=Circle2D; Dock=Point2D+Circle2D; BSB=NOT_IMPLEMENTED"
}
finally { $main.GetField('recipeCloseConfirmed', $private).SetValue($window, $true); $window.Close(); $app.Shutdown(); if (Test-Path $temp) { Remove-Item -LiteralPath $temp -Recurse -Force } }
