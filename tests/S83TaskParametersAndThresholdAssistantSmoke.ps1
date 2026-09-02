param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$CapturePath = (Join-Path $ProjectRoot 'artifacts\s83\s83-threshold-task-1366x768.png'),
    [string]$ToolboxCapturePath = (Join-Path $ProjectRoot 'artifacts\s83\s83-direct-toolbox-drag-1366x768.png')
)
$ErrorActionPreference = 'Stop'
if ([Threading.Thread]::CurrentThread.ApartmentState -ne [Threading.ApartmentState]::STA) { throw 'Run with powershell.exe -STA.' }
$halcon = 'C:\Program Files\MVTec\HALCON-20.11-Progress'; $env:PATH = (Join-Path $halcon 'bin\x64-win64') + ';' + $env:PATH
Add-Type -AssemblyName PresentationFramework
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
Add-Type @'
using System;
using System.Runtime.InteropServices;
using System.Threading;
public static class S83Mouse {
 [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
 [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
 [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint x, uint y, uint d, UIntPtr e);
 public const uint LD=0x0002, LU=0x0004;
 public static void ClickAsync(int x,int y){var t=new Thread(()=>{SetCursorPos(x,y);Thread.Sleep(120);mouse_event(LD,0,0,0,UIntPtr.Zero);Thread.Sleep(80);mouse_event(LU,0,0,0,UIntPtr.Zero);});t.IsBackground=true;t.Start();}
 public static void DoubleClickAsync(int x,int y){var t=new Thread(()=>{SetCursorPos(x,y);Thread.Sleep(120);for(int i=0;i<2;i++){mouse_event(LD,0,0,0,UIntPtr.Zero);Thread.Sleep(55);mouse_event(LU,0,0,0,UIntPtr.Zero);Thread.Sleep(90);}});t.IsBackground=true;t.Start();}
 public static void DragAsync(int x1,int y1,int x2,int y2){var t=new Thread(()=>{SetCursorPos(x1,y1);Thread.Sleep(120);mouse_event(LD,0,0,0,UIntPtr.Zero);for(int i=1;i<=8;i++){Thread.Sleep(35);SetCursorPos(x1+(x2-x1)*i/8,y1+(y2-y1)*i/8);}Thread.Sleep(80);mouse_event(LU,0,0,0,UIntPtr.Zero);});t.IsBackground=true;t.Start();}
}
'@
[Reflection.Assembly]::LoadFrom((Join-Path $halcon 'bin\dotnet35\halcondotnet.dll')) | Out-Null
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path $ProjectRoot 'bin\x64\Debug\HalconWinFormsDemo.exe'))
function Pump([int]$ms){$frame=New-Object Windows.Threading.DispatcherFrame;$timer=New-Object Windows.Threading.DispatcherTimer;$timer.Interval=[TimeSpan]::FromMilliseconds($ms);$timer.Add_Tick({$timer.Stop();$frame.Continue=$false});$timer.Start();[Windows.Threading.Dispatcher]::PushFrame($frame)}
function Assert([bool]$ok,[string]$message){if (-not $ok){throw "Assertion failed: $message"}}
function Shot([string]$path){[IO.Directory]::CreateDirectory((Split-Path -Parent $path))|Out-Null;$window.UpdateLayout();$root=$window.Content;$top=$root.PointToScreen([Windows.Point]::new(0,0));$end=$root.PointToScreen([Windows.Point]::new($root.ActualWidth,$root.ActualHeight));$screen=[Windows.Forms.Screen]::FromPoint([Drawing.Point]::new([int]$top.X,[int]$top.Y)).Bounds;$bmp=New-Object Drawing.Bitmap([int][Math]::Min($end.X-$top.X,$screen.Right-$top.X),[int][Math]::Min($end.Y-$top.Y,$screen.Bottom-$top.Y));$g=[Drawing.Graphics]::FromImage($bmp);try{$g.CopyFromScreen([int]$top.X,[int]$top.Y,0,0,$bmp.Size);$bmp.Save($path,[Drawing.Imaging.ImageFormat]::Png)}finally{$g.Dispose();$bmp.Dispose()}}

$mainType=$assembly.GetType('HalconWinFormsDemo.MainWindow',$true);$serviceType=$assembly.GetType('HalconWinFormsDemo.Services.RecipeService',$true);$loggerType=$assembly.GetType('HalconWinFormsDemo.Services.AppLogger',$true);$kindType=$assembly.GetType('HalconWinFormsDemo.Models.VmToolKind',$true)
$private=[Reflection.BindingFlags]'Instance,NonPublic';$all=[Reflection.BindingFlags]'Instance,NonPublic,Public';$temp=Join-Path([IO.Path]::GetTempPath())('halcon-s83-'+[Guid]::NewGuid().ToString('N'));[IO.Directory]::CreateDirectory($temp)|Out-Null
$service=$serviceType.GetConstructor([Type[]]@([string])).Invoke([object[]]@($temp.ToString()));$logger=$loggerType.GetConstructor([Type[]]@([string])).Invoke([object[]]@($temp.ToString()));$window=$mainType.GetConstructor($private,$null,[Type[]]@($serviceType,$loggerType),$null).Invoke([object[]]@($service,$logger))
function F([string]$name){$field=$mainType.GetField($name,$all);if ($null -eq $field){throw "Missing field $name"};$value=$field.GetValue($window);Write-Output -NoEnumerate $value}
function FBool([string]$name){$field=$mainType.GetField($name,$all);if ($null -eq $field){throw "Missing field $name"};[Convert]::ToBoolean($field.GetValue($window),[Globalization.CultureInfo]::InvariantCulture)}
function M([string]$name,[object[]]$invokeArgs=@()){try{$method=$mainType.GetMethods($private)|Where-Object Name -eq $name|Where-Object{$_.GetParameters().Count -eq $invokeArgs.Count}|Select-Object -First 1;if ($null -eq $method){throw "Missing $name/$($invokeArgs.Count)"};$method.Invoke($window,$invokeArgs)}catch [Reflection.TargetInvocationException]{throw $_.Exception.InnerException}}
function Tool([string]$kind,[string]$name){$mainType.GetMethod('CreateFlowTool',$private).Invoke($window,[object[]]@([Enum]::Parse($kindType,$kind),$name,$true,$null))}
function Out($tool,[string]$port){$value=$null;if (-not $tool.TryGetOutputValue($port,[ref]$value)){throw "Missing output $port"};Write-Output -NoEnumerate $value}
function Execute($tool){M 'ExecuteFlowTool' @($tool,'S83 smoke')}
function RowShown([string]$controlName){$control=F $controlName;$current=$control;while($null -ne $current -and -not ($current -is [Windows.Controls.Grid])){$current=[Windows.LogicalTreeHelper]::GetParent($current)};if ($null -eq $current){throw "Grid not found for $controlName"};$row=[Windows.Controls.Grid]::GetRow($control);$current.RowDefinitions[$row].Height.Value -gt 0}
function CardPart($tool,[string]$name){$items=F 'FlowGraphNodeItemsControl';$presenter=$items.ItemContainerGenerator.ContainerFromItem($tool);Assert ($null -ne $presenter) 'Card presenter missing';$part=$presenter.ContentTemplate.FindName($name,$presenter);Assert ($null -ne $part) "Card part $name missing";Write-Output -NoEnumerate $part}
function DragCard($tool,[int]$dx,[int]$dy){$card=CardPart $tool 'GraphToolCard';$point=$card.PointToScreen([Windows.Point]::new(150,24));[S83Mouse]::DragAsync([int]$point.X,[int]$point.Y,[int]$point.X+$dx,[int]$point.Y+$dy);Pump 1000}
function DoubleClickCard($tool){$card=CardPart $tool 'GraphToolCard';$point=$card.PointToScreen([Windows.Point]::new(150,24));[S83Mouse]::DoubleClickAsync([int]$point.X,[int]$point.Y);Pump 900}

$app=[Windows.Application]::Current;if ($null -eq $app){$app=New-Object Windows.Application;$app.ShutdownMode=[Windows.ShutdownMode]::OnExplicitShutdown}
try {
 $window.Topmost=$true;$window.WindowState=[Windows.WindowState]::Normal;$window.MinWidth=0;$window.MinHeight=0;$window.Left=0;$window.Top=0;$window.Width=1366;$window.Height=768;$window.Show();Pump 700;$helper=New-Object Windows.Interop.WindowInteropHelper -ArgumentList $window;[S83Mouse]::SetForegroundWindow($helper.Handle)|Out-Null
 $flow=F 'flowTools';M 'DisposeFlowTools'|Out-Null;$flow.Clear();$source=Tool ImageSource 'Input Image';$threshold=Tool ImageThreshold 'Threshold';$judge=Tool NumericJudge 'Region Count';$ocr=Tool OcrRecognition 'OCR';$shape=Tool ShapeMatch 'Shape';$caliper=Tool CaliperEdgePairMeasurement 'Caliper'
 foreach($tool in @($source,$threshold,$judge,$ocr,$shape,$caliper)){$flow.Add($tool)}
 $source.Parameters.LocalImagePath=Join-Path $env:PUBLIC 'Documents\MVTec\HALCON-20.11-Progress\examples\images\barcode\code128\code12801.png';$source.Parameters.LocalImageSerialNumber='S83-AUTO'
 $threshold.SetInputBinding('Image',$source.ToolId,'Image');$judge.SetInputBinding('Value',$threshold.ToolId,'RegionCount');$judge.InputToolId=$threshold.ToolId;$judge.InputPortName='RegionCount';$judge.NumericLowerLimit=1;$judge.NumericUpperLimit=10000;$ocr.SetInputBinding('Image',$source.ToolId,'Image');$caliper.SetInputBinding('Image',$source.ToolId,'Image')
 Execute $source|Out-Null;M 'RefreshUiState'|Out-Null;M 'RefreshFlowGraphLayout'|Out-Null;Pump 650
 Assert(([string](F 'toolCatalogMode')) -eq 'All') 'Toolbox did not default to the direct all-category view'
 Assert((F 'ToolCatalogAllButton').Content.ToString() -eq '全部' -and (F 'ToolCatalogAllButton').ContextMenu -eq $null) 'Toolbox still uses a nested category menu'
 Assert((F 'ToolCatalogCommonButton').Visibility -eq [Windows.Visibility]::Visible -and (F 'ToolCatalogSnippetButton').Visibility -eq [Windows.Visibility]::Visible) 'Direct toolbox modes are not visible'
 Assert((F 'toolCatalogView').Groups.Count -ge 5 -and (F 'ToolCatalogList').Items.Count -ge 60) 'All categorized tools are not directly available'
 $inputAnchor=CardPart $threshold 'S75InputAnchor';$outputAnchor=CardPart $threshold 'S75OutputAnchor'
 Assert($inputAnchor.ActualWidth -le 18 -and $inputAnchor.ActualHeight -le 16 -and $outputAnchor.ActualWidth -le 18 -and $outputAnchor.ActualHeight -le 16) 'Connection anchors still cover too much of the card'
 $startX=$threshold.GraphX;$startY=$threshold.GraphY;DragCard $threshold 52 28
 Assert(([Math]::Abs($threshold.GraphX-$startX) -ge 24 -or [Math]::Abs($threshold.GraphY-$startY) -ge 24)) 'Single-click drag did not move the module'
 Assert(-not (FBool 'centralModuleConfigurationOpenS77')) 'Single-click drag unexpectedly opened configuration'
 Shot $ToolboxCapturePath
 DoubleClickCard $threshold
 Assert((FBool 'centralModuleConfigurationOpenS77')) 'Double-click did not open central configuration'
 Assert((F 'DockKeyParameterSummaryText').Text -eq '本工具常用 2 项 · 已按任务排序') 'Threshold task profile summary is wrong'
 Assert((RowShown 'DockImageThresholdMinTextBox') -and (RowShown 'DockImageThresholdMaxTextBox')) 'Threshold common fields are hidden'
 Assert((F 'DockAdvancedParameterToggleButton').Content.ToString() -eq '没有更多参数') 'Threshold exposes fake advanced parameters'
 Assert((M 'EstimateImageThresholdFromCurrentInputS83' @('dark'))) 'Dark-target threshold estimation failed'
 $darkMin=[double]::Parse((F 'DockImageThresholdMinTextBox').Text,[Globalization.CultureInfo]::InvariantCulture);$darkMax=[double]::Parse((F 'DockImageThresholdMaxTextBox').Text,[Globalization.CultureInfo]::InvariantCulture)
 Assert($darkMin -eq 0 -and $darkMax -gt 0 -and $darkMax -lt 255 -and (FBool 'dockDraftDirty')) 'Dark estimate did not write a valid draft range'
 Assert((M 'ApplyDockConfigurationDraft' @($false))) 'Estimated threshold draft did not apply'
 Execute $threshold|Out-Null;Execute $judge|Out-Null;$region=Out $threshold Region
 Assert($threshold.ResultCode -eq 'OK' -and $judge.ResultCode -eq 'OK' -and -not $region.IsDisposed -and [double](Out $threshold RegionCount) -ge 1) 'Estimated threshold did not run through Region and NumericJudge'
 $regionCount=[double](Out $threshold RegionCount);$recipe=M 'CaptureRecipe';$saved=@($recipe.ToolFlow|Where-Object ToolId -eq $threshold.ToolId)[0];Assert([math]::Abs($saved.Parameters.ImageThresholdMaxGray-$darkMax) -lt 0.001) 'Estimated threshold did not persist in recipe'

 Assert((M 'EstimateImageThresholdFromCurrentInputS83' @('light'))) 'Light-target threshold estimation failed'
 $lightMin=[double]::Parse((F 'DockImageThresholdMinTextBox').Text,[Globalization.CultureInfo]::InvariantCulture);$lightMax=[double]::Parse((F 'DockImageThresholdMaxTextBox').Text,[Globalization.CultureInfo]::InvariantCulture)
 Assert($lightMin -gt 0 -and $lightMin -lt 255 -and $lightMax -eq 255) 'Light estimate did not write a valid draft range'
 M 'LoadDockConfigurationDraft' @($threshold)|Out-Null

 M 'LoadDockConfigurationDraft' @($ocr)|Out-Null;Assert((F 'DockKeyParameterSummaryText').Text -eq '本工具常用 4 项 · 已按任务排序') 'OCR task profile missing';Assert((RowShown 'DockOcrModelTextBox') -and (RowShown 'DockOcrThresholdTextBox') -and -not (RowShown 'DockOcrSortComboBox')) 'OCR task priorities are wrong'
 M 'LoadDockConfigurationDraft' @($caliper)|Out-Null;Assert((F 'DockKeyParameterSummaryText').Text -eq '本工具常用 4 项 · 已按任务排序') 'Caliper task profile missing';Assert((RowShown 'DockCaliperLength2TextBox') -and (RowShown 'DockCaliperThresholdTextBox') -and -not (RowShown 'DockCaliperRowTextBox')) 'Caliper task priorities are wrong'
 M 'LoadDockConfigurationDraft' @($shape)|Out-Null;Assert((F 'DockKeyParameterSummaryText').Text -eq '本工具常用 4 项 · 已按任务排序') 'Shape task profile missing';Assert((RowShown 'DockShapeModelNameTextBox') -and (RowShown 'DockShapeMinScoreTextBox') -and -not (RowShown 'DockShapeAngleStartTextBox')) 'Shape task priorities are wrong'

 M 'LoadDockConfigurationDraft' @($threshold)|Out-Null;Assert((M 'EstimateImageThresholdFromCurrentInputS83' @('dark'))) 'Final estimate failed';Pump 300;Shot $CapturePath
 $beforeMin=(F 'DockImageThresholdMinTextBox').Text;$beforeMax=(F 'DockImageThresholdMaxTextBox').Text;$source.ClearRuntimeOutputs();$source.ResultCode='--';$source.RunStatus='未运行';Assert(-not (M 'EstimateImageThresholdFromCurrentInputS83' @('dark'))) 'Estimator accepted a stale source';Assert((F 'DockImageThresholdMinTextBox').Text -eq $beforeMin -and (F 'DockImageThresholdMaxTextBox').Text -eq $beforeMax) 'Failed estimate corrupted the existing draft'
 Assert((F 'DockImageThresholdEstimateStatusS83').Text.StartsWith('估算失败')) 'Failed estimate has no visible diagnosis'
 Write-Output "S83_USABILITY_THRESHOLD=PASS; DirectCategories=$((F 'toolCatalogView').Groups.Count); Anchor=18x16; DragDelta=$([Math]::Round($threshold.GraphX-$startX)),$([Math]::Round($threshold.GraphY-$startY)); DoubleClick=True; Profiles=15; Dark=$darkMin..$darkMax; Light=$lightMin..$lightMax; RegionCount=$regionCount; ToolboxCapture=$ToolboxCapturePath; ConfigCapture=$CapturePath"
}
finally{try{$mainType.GetField('recipeCloseConfirmed',$private).SetValue($window,$true);$window.Close();$app.Shutdown()}catch{};if(Test-Path $temp){Remove-Item -LiteralPath $temp -Recurse -Force}}
