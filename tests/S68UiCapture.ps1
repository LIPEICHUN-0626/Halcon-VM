param(
  [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot),
  [string]$WidePath = (Join-Path $ProjectRoot 'artifacts\s68\s68-existing-source-navigation-1938x1158.png'),
  [string]$CompactPath = (Join-Path $ProjectRoot 'artifacts\s68\s68-create-source-line-circle-1366x768.png')
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
$private = [Reflection.BindingFlags]'Instance,NonPublic'; $all = [Reflection.BindingFlags]'Instance,NonPublic,Public'; $temp = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s68-ui-' + [Guid]::NewGuid().ToString('N')); [IO.Directory]::CreateDirectory($temp) | Out-Null
$service = $serviceType.GetConstructor([Type[]]@([string])).Invoke([object[]]@($temp.ToString())); $logger = $loggerType.GetConstructor([Type[]]@([string])).Invoke([object[]]@($temp.ToString())); $window = $main.GetConstructor($private, $null, [Type[]]@($serviceType, $loggerType), $null).Invoke([object[]]@($service, $logger))
function Field([string]$Name) { $value = $main.GetField($Name, $all).GetValue($window); Write-Output -NoEnumerate $value }
function Method([string]$Name, [object[]]$Parameters = @()) { try { $main.GetMethod($Name, $private).Invoke($window, $Parameters) } catch { $error = $_.Exception; while ($error.InnerException) { $error = $error.InnerException }; throw ($Name + ': ' + $error.Message) } }
function Kind([string]$Name) { [Enum]::Parse($kindType, $Name) }
function Tool([string]$KindName, [string]$Name) { Method 'CreateFlowTool' ([object[]]@((Kind $KindName), $Name, $true, $null)) }
$app = [Windows.Application]::Current; if ($null -eq $app) { $app = New-Object Windows.Application; $app.ShutdownMode = [Windows.ShutdownMode]::OnExplicitShutdown }
try {
  $window.Topmost = $true; $window.Show(); $window.WindowState = [Windows.WindowState]::Normal; $window.MinWidth = 0; $window.MinHeight = 0; $window.Left = 2; $window.Top = 2
  $flow = Field 'flowTools'; Method 'DisposeFlowTools' | Out-Null; $flow.Clear()
  $image = Tool 'ImageSource' 'Input Image'; $lineXld = Tool 'SubpixelEdgeExtraction' 'Line Edge'; $lineFit = Tool 'XldLineFitting' 'Existing Main Line'; $circleXld = Tool 'XldContourSegmentation' 'Circle Arc'; $circleFit = Tool 'XldCircleFitting' 'Existing Main Circle'; $target = Tool 'LineCircleIntersectionMeasurement' 'Line Circle Intersection'; $judge = Tool 'NumericJudge' 'Chord Judge'; $lateLine = Tool 'XldLineFitting' 'Late Line (Rejected)'
  foreach ($tool in @($image,$lineXld,$lineFit,$circleXld,$circleFit,$target,$judge,$lateLine)) { $flow.Add($tool) }
  $lineXld.SetInputBinding('Image',$image.ToolId,'Image'); $lineFit.SetInputBinding('XLD',$lineXld.ToolId,'XLD'); $circleXld.SetInputBinding('XLD',$lineXld.ToolId,'XLD'); $circleFit.SetInputBinding('XLD',$circleXld.ToolId,'XLD'); $target.SetInputBinding('Circle',$circleFit.ToolId,'MainCircle'); $judge.SetInputBinding('Value',$target.ToolId,'ChordLength'); $judge.InputToolId=$target.ToolId; $judge.InputPortName='ChordLength'
  (Field 'FlowToolList').SelectedItem = $target; (Field 'RightTabs').SelectedItem = (Field 'DockConfigurationTab'); (Field 'DockConfigurationTabs').SelectedItem = (Field 'DockParametersTaskTab'); Method 'LoadDockConfigurationDraft' ([object[]]@($target)) | Out-Null
  Method 'EnterInputSourceContextS68' ([object[]]@($target.ToolId,'Line')) | Out-Null
  $suggestions = Field 'inputSourceSuggestionsS68'; foreach ($entry in $suggestions) { if ($entry.IsValid) { (Field 'ToolInputSourceSuggestionList').SelectedItem = $entry; break } }
  Method 'RefreshUiState' | Out-Null
  $window.Width = 1938; $window.Height = 1158; Pump 900
  Assert ((Field 'ToolInputSourceContextBorder').Visibility.ToString() -eq 'Visible' -and (Field 'ToolInputSourceSuggestionList').Items.Count -ge 2) 'Existing-source navigation context is not visible.'
  Assert ((Field 'DockLineCircleIntersectionPanel').Visibility.ToString() -eq 'Visible') 'Line-circle configuration panel is not visible.'
  Shot $WidePath 1938 1158

  Method 'ExitInputSourceContextS68' ([object[]]@($false)) | Out-Null; Method 'DisposeFlowTools' | Out-Null; $flow.Clear()
  $xld = Tool 'SubpixelEdgeExtraction' 'Available XLD'; $circleFit = Tool 'XldCircleFitting' 'Available Circle'; $target = Tool 'LineCircleIntersectionMeasurement' 'Needs Created Line'
  foreach ($tool in @($xld,$circleFit,$target)) { $flow.Add($tool) }
  $circleFit.SetInputBinding('XLD',$xld.ToolId,'XLD'); $target.SetInputBinding('Circle',$circleFit.ToolId,'MainCircle')
  (Field 'FlowToolList').SelectedItem = $target; (Field 'RightTabs').SelectedItem = (Field 'DockConfigurationTab'); (Field 'DockConfigurationTabs').SelectedItem = (Field 'DockParametersTaskTab'); Method 'LoadDockConfigurationDraft' ([object[]]@($target)) | Out-Null
  Method 'EnterInputSourceContextS68' ([object[]]@($target.ToolId,'Line')) | Out-Null
  $candidate = $null; foreach ($entry in (Field 'toolCatalogView')) { $candidate = $entry; break }; (Field 'ToolCatalogList').SelectedItem = $candidate
  Method 'RefreshUiState' | Out-Null
  $window.Width = 1366; $window.Height = 768; Pump 900
  Assert ((Field 'ToolInputSourceCreateHintText').Visibility.ToString() -eq 'Visible' -and $candidate.Kind.ToString() -eq 'XldLineFitting') 'Create-source context did not show the real Line2D producer.'
  Assert ((Field 'DockLineCircleIntersectionPanel').Visibility.ToString() -eq 'Visible') 'Compact line-circle configuration panel is not visible.'
  Shot $CompactPath 1366 768
  Write-Output "S68_UI_CAPTURE=PASS; Wide=$WidePath; Compact=$CompactPath; Catalog=63; ExistingSource=Visible; CreateSource=XldLineFitting; Dock=Line2D+Circle2D"
}
finally { $main.GetField('recipeCloseConfirmed', $private).SetValue($window, $true); $window.Close(); $app.Shutdown(); if (Test-Path $temp) { Remove-Item -LiteralPath $temp -Recurse -Force } }
