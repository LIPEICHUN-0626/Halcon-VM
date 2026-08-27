param(
  [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot),
  [string]$WidePath = (Join-Path $ProjectRoot 'artifacts\s69\s69-connection-issues-1938x1158.png'),
  [string]$CompactPath = (Join-Path $ProjectRoot 'artifacts\s69\s69-circle-circle-workbench-1366x768.png')
)
$ErrorActionPreference = 'Stop'
if ([Threading.Thread]::CurrentThread.ApartmentState -ne [Threading.ApartmentState]::STA) { throw 'Run with powershell.exe -STA.' }
$halcon = 'C:\Program Files\MVTec\HALCON-20.11-Progress'; $env:PATH = (Join-Path $halcon 'bin\x64-win64') + ';' + $env:PATH
Add-Type -AssemblyName PresentationFramework
[Reflection.Assembly]::LoadFrom((Join-Path $halcon 'bin\dotnet35\halcondotnet.dll')) | Out-Null
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path $ProjectRoot 'bin\x64\Debug\HalconWinFormsDemo.exe'))
function Assert([bool]$Condition, [string]$Message) { if (-not $Condition) { throw $Message } }
function Pump([int]$Milliseconds) { $frame = New-Object Windows.Threading.DispatcherFrame; $timer = New-Object Windows.Threading.DispatcherTimer; $timer.Interval = [TimeSpan]::FromMilliseconds($Milliseconds); $timer.Add_Tick({ $timer.Stop(); $frame.Continue = $false }); $timer.Start(); [Windows.Threading.Dispatcher]::PushFrame($frame) }
function Shot([string]$Path, [int]$Width, [int]$Height) { $directory = Split-Path -Parent $Path; if (-not (Test-Path $directory)) { [IO.Directory]::CreateDirectory($directory) | Out-Null }; $window.UpdateLayout(); [double]$dpiX = 96.0 * $Width / [Math]::Max(1.0, [double]$window.ActualWidth); [double]$dpiY = 96.0 * $Height / [Math]::Max(1.0, [double]$window.ActualHeight); $render = New-Object Windows.Media.Imaging.RenderTargetBitmap -ArgumentList $Width,$Height,$dpiX,$dpiY,([Windows.Media.PixelFormats]::Pbgra32); $render.Render($window); $encoder = New-Object Windows.Media.Imaging.PngBitmapEncoder; $encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($render)); $stream = [IO.File]::Open([IO.Path]::GetFullPath($Path), [IO.FileMode]::Create); try { $encoder.Save($stream) } finally { $stream.Dispose() } }
$main = $assembly.GetType('HalconWinFormsDemo.MainWindow', $true); $serviceType = $assembly.GetType('HalconWinFormsDemo.Services.RecipeService', $true); $loggerType = $assembly.GetType('HalconWinFormsDemo.Services.AppLogger', $true); $kindType = $assembly.GetType('HalconWinFormsDemo.Models.VmToolKind', $true); $circleType = $assembly.GetType('HalconWinFormsDemo.Models.VmCircle2D', $true); $xldSnapshotType = $assembly.GetType('HalconWinFormsDemo.Models.VmXldSnapshot', $true)
$private = [Reflection.BindingFlags]'Instance,NonPublic'; $all = [Reflection.BindingFlags]'Instance,NonPublic,Public'; $static = [Reflection.BindingFlags]'Static,Public'; $temp = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s69-ui-' + [Guid]::NewGuid().ToString('N')); [IO.Directory]::CreateDirectory($temp) | Out-Null
$service = $serviceType.GetConstructor([Type[]]@([string])).Invoke([object[]]@($temp.ToString())); $logger = $loggerType.GetConstructor([Type[]]@([string])).Invoke([object[]]@($temp.ToString())); $window = $main.GetConstructor($private, $null, [Type[]]@($serviceType, $loggerType), $null).Invoke([object[]]@($service, $logger))
function Field([string]$Name) { $value = $main.GetField($Name, $all).GetValue($window); Write-Output -NoEnumerate $value }
function Method([string]$Name, [object[]]$Parameters = @()) { try { $main.GetMethod($Name, $private).Invoke($window, $Parameters) } catch { $error = $_.Exception; while ($error.InnerException) { $error = $error.InnerException }; throw ($Name + ': ' + $error.Message) } }
function Kind([string]$Name) { [Enum]::Parse($kindType, $Name) }
function Tool([string]$KindName, [string]$Name) { Method 'CreateFlowTool' ([object[]]@((Kind $KindName), $Name, $true, $null)) }
function Circle([double]$Row,[double]$Column,[double]$Radius,[string]$Context) { $circleType.GetMethod('CreateImagePixel',$static).Invoke($null,[object[]]@($Row,$Column,$Radius,$Context)) }
function PublishXld($Tool,[HalconDotNet.HObject]$Raw) { $snapshot = $xldSnapshotType.GetMethod('Create',$static).Invoke($null,[object[]]@($Raw,$Tool.ToolId,$Tool.InstanceName,'XLD')); $Tool.SetOutputValue('XLD',$snapshot); $Tool.ResultCode='OK'; $Tool.RunStatus='Completed' }
$app = [Windows.Application]::Current; if ($null -eq $app) { $app = New-Object Windows.Application; $app.ShutdownMode = [Windows.ShutdownMode]::OnExplicitShutdown }
try {
  $window.Topmost = $true; $window.Show(); $window.WindowState = [Windows.WindowState]::Normal; $window.MinWidth = 0; $window.MinHeight = 0; $window.Left = 2; $window.Top = 2
  Assert ((Field 'DeveloperToolbarPanel').Children.Count -eq 7) 'S69 added a permanent top command.'
  $flow = Field 'flowTools'; Method 'DisposeFlowTools' | Out-Null; $flow.Clear()
  $carrierA=Tool 'SubpixelEdgeExtraction' 'Circle Carrier A';$fitA=Tool 'XldCircleFitting' 'Circle Source A';$carrierB=Tool 'SubpixelEdgeExtraction' 'Circle Carrier B';$fitB=Tool 'XldCircleFitting' 'Circle Source B';$missing=Tool 'CircleCircleIntersectionMeasurement' 'Needs Circle B';$point=Tool 'PointToCircleRadialDeviationMeasurement' 'Needs Point';$late=Tool 'CircleCircleIntersectionMeasurement' 'Later Source Error';$lateFit=Tool 'XldCircleFitting' 'Later Circle Source';$staleFit=Tool 'XldCircleFitting' 'Waiting Circle Source';$stale=Tool 'CircleCircleIntersectionMeasurement' 'Waiting Current Value'
  foreach($tool in @($carrierA,$fitA,$carrierB,$fitB,$missing,$point,$late,$lateFit,$staleFit,$stale)){$flow.Add($tool)}
  $fitA.SetInputBinding('XLD',$carrierA.ToolId,'XLD');$fitB.SetInputBinding('XLD',$carrierB.ToolId,'XLD');$lateFit.SetInputBinding('XLD',$carrierA.ToolId,'XLD');$staleFit.SetInputBinding('XLD',$carrierB.ToolId,'XLD')
  [HalconDotNet.HObject]$rawA=$null;[HalconDotNet.HObject]$rawB=$null;try{[HalconDotNet.HOperatorSet]::GenCircleContourXld([ref]$rawA,120.0,130.0,45.0,0.0,[Math]::PI*2.0,'positive',1.0);[HalconDotNet.HOperatorSet]::GenCircleContourXld([ref]$rawB,120.0,195.0,45.0,0.0,[Math]::PI*2.0,'positive',1.0);PublishXld $carrierA $rawA;PublishXld $carrierB $rawB}finally{if($rawA){$rawA.Dispose()};if($rawB){$rawB.Dispose()}}
  $context='S69.UI';$fitA.SetOutputValue('MainCircle',(Circle 120 130 45 $context));$fitA.ResultCode='OK';$fitA.RunStatus='Completed';$fitB.SetOutputValue('MainCircle',(Circle 120 195 45 $context));$fitB.ResultCode='OK';$fitB.RunStatus='Completed';$lateFit.SetOutputValue('MainCircle',(Circle 120 230 35 $context));$lateFit.ResultCode='OK';$lateFit.RunStatus='Completed';$staleFit.SetOutputValue('MainCircle',(Circle 120 165 20 $context));$staleFit.ResultCode='--';$staleFit.RunStatus='Waiting'
  $missing.SetInputBinding('CircleA',$fitA.ToolId,'MainCircle');$point.SetInputBinding('Circle',$fitB.ToolId,'MainCircle');$late.SetInputBinding('CircleA',$fitA.ToolId,'MainCircle');$late.SetInputBinding('CircleB',$lateFit.ToolId,'MainCircle');$stale.SetInputBinding('CircleA',$fitA.ToolId,'MainCircle');$stale.SetInputBinding('CircleB',$staleFit.ToolId,'MainCircle')
  (Field 'FlowToolList').SelectedItem=$missing;Method 'RefreshUiState'|Out-Null;Method 'OpenConnectionIssuesButton_Click' ([object[]]@($null,[Windows.RoutedEventArgs]::new()))|Out-Null
  $window.Width=1938;$window.Height=1158;Pump 900
  Assert ((Field 'FlowConnectionIssuePanel').Visibility.ToString() -eq 'Visible' -and (Field 'FlowConnectionIssueListBox').Items.Count -eq 4) 'Connection issue task list is not visible.'
  Shot $WidePath 1938 1158

  (Field 'FlowConnectionIssuePanel').Visibility=[Windows.Visibility]::Collapsed;Method 'DisposeFlowTools'|Out-Null;$flow.Clear()
  $xldA=Tool 'SubpixelEdgeExtraction' 'Arc A';$circleA=Tool 'XldCircleFitting' 'Circle Fit A';$xldB=Tool 'SubpixelEdgeExtraction' 'Arc B';$circleB=Tool 'XldCircleFitting' 'Circle Fit B';$target=Tool 'CircleCircleIntersectionMeasurement' 'Circle Relation';$judge=Tool 'NumericJudge' 'Center Distance Judge'
  foreach($tool in @($xldA,$circleA,$xldB,$circleB,$target,$judge)){$flow.Add($tool)};$circleA.SetInputBinding('XLD',$xldA.ToolId,'XLD');$circleB.SetInputBinding('XLD',$xldB.ToolId,'XLD');$target.SetInputBinding('CircleA',$circleA.ToolId,'MainCircle');$target.SetInputBinding('CircleB',$circleB.ToolId,'MainCircle');$judge.SetInputBinding('Value',$target.ToolId,'CenterDistance');$judge.InputToolId=$target.ToolId;$judge.InputPortName='CenterDistance'
  (Field 'FlowToolList').SelectedItem=$target;(Field 'RightTabs').SelectedItem=(Field 'DockConfigurationTab');(Field 'DockConfigurationTabs').SelectedItem=(Field 'DockParametersTaskTab');Method 'LoadDockConfigurationDraft' ([object[]]@($target))|Out-Null;Method 'RefreshUiState'|Out-Null
  $window.Width=1366;$window.Height=768;Pump 900
  Assert ((Field 'DockCircleCircleIntersectionPanel').Visibility.ToString() -eq 'Visible') 'Circle-circle dock panel is not visible.'
  Shot $CompactPath 1366 768
  $catalogCount = (Field 'toolCatalog').Count
  Write-Output "S69_UI_CAPTURE=PASS; Wide=$WidePath; Compact=$CompactPath; Catalog=$catalogCount; Issues=4; Dock=Circle2D+Circle2D; PermanentToolbar=7; BSB=NOT_IMPLEMENTED"
}
finally { $main.GetField('recipeCloseConfirmed',$private).SetValue($window,$true);$window.Close();$app.Shutdown();if(Test-Path $temp){Remove-Item -LiteralPath $temp -Recurse -Force} }
