param(
    [string]$ProjectRoot=(Split-Path -Parent $PSScriptRoot),
    [string]$ResultCapture=(Join-Path $ProjectRoot 'artifacts\s88\s88-result-summary-1366x768.png'),
    [string]$StabilityCapture=(Join-Path $ProjectRoot 'artifacts\s88\s88-trial-stability-1938x1158.png')
)
$ErrorActionPreference='Stop'
if([Threading.Thread]::CurrentThread.ApartmentState-ne[Threading.ApartmentState]::STA){throw 'Run with powershell.exe -STA.'}
$halcon='C:\Program Files\MVTec\HALCON-20.11-Progress';$env:PATH=(Join-Path $halcon 'bin\x64-win64')+';'+$env:PATH
Add-Type -AssemblyName PresentationFramework
[Reflection.Assembly]::LoadFrom((Join-Path $halcon 'bin\dotnet35\halcondotnet.dll'))|Out-Null
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $ProjectRoot 'bin\x64\Debug\HalconWinFormsDemo.exe'))
function Assert([bool]$ok,[string]$message){if(-not$ok){throw "Assertion failed: $message"}}
function Pump([int]$ms){$frame=New-Object Windows.Threading.DispatcherFrame;$timer=New-Object Windows.Threading.DispatcherTimer;$timer.Interval=[TimeSpan]::FromMilliseconds($ms);$timer.Add_Tick({$timer.Stop();$frame.Continue=$false});$timer.Start();[Windows.Threading.Dispatcher]::PushFrame($frame)}
function Shot([string]$path){[IO.Directory]::CreateDirectory((Split-Path -Parent $path))|Out-Null;$window.UpdateLayout();$w=[Math]::Max(1,[int][Math]::Ceiling($window.ActualWidth));$h=[Math]::Max(1,[int][Math]::Ceiling($window.ActualHeight));$render=[Windows.Media.Imaging.RenderTargetBitmap]::new($w,$h,96,96,[Windows.Media.PixelFormats]::Pbgra32);$render.Render($window);$encoder=New-Object Windows.Media.Imaging.PngBitmapEncoder;$encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($render));$stream=[IO.File]::Open($path,[IO.FileMode]::Create,[IO.FileAccess]::Write);try{$encoder.Save($stream)}finally{$stream.Dispose()}}
$mainType=$assembly.GetType('HalconWinFormsDemo.MainWindow',$true);$serviceType=$assembly.GetType('HalconWinFormsDemo.Services.RecipeService',$true);$loggerType=$assembly.GetType('HalconWinFormsDemo.Services.AppLogger',$true);$kindType=$assembly.GetType('HalconWinFormsDemo.Models.VmToolKind',$true)
$private=[Reflection.BindingFlags]'Instance,NonPublic';$all=[Reflection.BindingFlags]'Instance,NonPublic,Public';$temp=Join-Path([IO.Path]::GetTempPath())('halcon-s88-'+[Guid]::NewGuid().ToString('N'));[IO.Directory]::CreateDirectory($temp)|Out-Null
$service=$serviceType.GetConstructor([Type[]]@([string])).Invoke([object[]]@($temp.ToString()));$logger=$loggerType.GetConstructor([Type[]]@([string])).Invoke([object[]]@($temp.ToString()));$window=$mainType.GetConstructor($private,$null,[Type[]]@($serviceType,$loggerType),$null).Invoke([object[]]@($service,$logger))
function F([string]$name){$field=$mainType.GetField($name,$all);if($null-eq$field){throw "Missing field $name"};$value=$field.GetValue($window);Write-Output -NoEnumerate $value}
function M([string]$name,[object[]]$invokeArgs=@()){try{$method=$mainType.GetMethods($private)|Where-Object Name -eq $name|Where-Object{$_.GetParameters().Count-eq$invokeArgs.Count}|Select-Object -First 1;if($null-eq$method){throw "Missing $name/$($invokeArgs.Count)"};$actual=@($invokeArgs|ForEach-Object{if($null-eq$_){$null}else{$_.PSObject.BaseObject}});$value=$method.Invoke($window,$actual);Write-Output -NoEnumerate $value}catch [Reflection.TargetInvocationException]{throw $_.Exception.InnerException}}
function Tool([string]$kind,[string]$name,[string]$toolId=$null){M 'CreateFlowTool' @([Enum]::Parse($kindType,$kind),$name,$true,$toolId)}
function Execute($tool){M 'ExecuteFlowTool' @($tool,'S88 smoke')}
$app=[Windows.Application]::Current;if($null-eq$app){$app=New-Object Windows.Application;$app.ShutdownMode=[Windows.ShutdownMode]::OnExplicitShutdown}
try{
 $window.Topmost=$true;$window.WindowState=[Windows.WindowState]::Normal;$window.MinWidth=0;$window.MinHeight=0;$window.Left=0;$window.Top=0;$window.Width=1366;$window.Height=768;$window.Show();Pump 450
 $flow=F 'flowTools';M 'DisposeFlowTools'|Out-Null;$flow.Clear();$source=Tool ImageSource '输入图像';$auto=Tool AutomaticThreshold '自动分割';$gray=Tool GrayStat '灰度检测';$morph=Tool RegionMorphology '后序形态学';foreach($tool in @($source,$auto,$gray,$morph)){$flow.Add($tool)}
 $source.Parameters.LocalImagePath=Join-Path $env:PUBLIC 'Documents\MVTec\HALCON-20.11-Progress\examples\images\barcode\code128\code12801.png';$source.Parameters.LocalImageSerialNumber='S88-INPUT';$auto.SetInputBinding('Image',$source.ToolId,'Image');$gray.SetInputBinding('Image',$source.ToolId,'Image');$gray.SetInputBinding('ROI',$auto.ToolId,'Region');$gray.Parameters.RoiSourceMode='Inherited';$gray.Parameters.GrayMin=0;$gray.Parameters.GrayMax=255;$morph.SetInputBinding('Region',$auto.ToolId,'Region')
 Execute $source|Out-Null;Execute $auto|Out-Null;Execute $gray|Out-Null;M 'RefreshFlowGraphLayout'|Out-Null
 (F 'FlowToolList').SelectedItem=$auto;M 'OpenCentralModuleConfigurationS77' @($auto)|Out-Null;M 'SelectCentralConfigurationTaskS77' @((F 'DockResultTaskTab'),'Result')|Out-Null;M 'RefreshDockConfigurationRuntime' @($auto)|Out-Null;Pump 250
 $keyRows=F 'dockTaskResultRowsS88';$technical=F 'dockTechnicalResultRowsS88';$keyNames=@($keyRows|ForEach-Object PortName)
 Assert((F 'DockResultVerdictTextS88').Text-eq'OK') 'Result verdict is not task-first OK'
 Assert($keyRows.Count-ge2-and$keyRows.Count-le4) 'Result summary is not limited to 2-4 key outputs'
 Assert($keyNames-contains'Threshold'-and$keyNames-contains'RegionCount') 'Automatic-threshold task outputs were not prioritized'
 Assert((F 'DockResultGraphicTextS88').Text-match'对象\s+\d+') 'Graphic object summary is missing'
 Assert($technical.Count-eq7-and-not(F 'DockResultTechnicalExpanderS88').IsExpanded) 'Full technical outputs are not hidden under one More action'
 Assert((F 'DockResultTaskErrorBorderS88').Visibility-eq[Windows.Visibility]::Collapsed) 'Empty error panel still occupies the first screen'
 $resultKeyCount=$keyRows.Count;$resultTechnicalCount=$technical.Count
 Shot $ResultCapture
 $catalog=F 'toolCatalog';$resultAudit=0;$resultFailures=New-Object Collections.Generic.List[string];foreach($item in @($catalog)){$auditTool=Tool ([string]$item.Kind) ('结果审计-'+$item.Name);try{M 'RefreshDockTaskResultSummaryS88' @($auditTool)|Out-Null;$count=(F 'dockTaskResultRowsS88').Count;if($count-lt2-or$count-gt4){$resultFailures.Add($item.Name+'='+$count)};$resultAudit++}finally{$auditTool.Dispose()}};Assert($resultFailures.Count-eq0) ('Catalog result summaries outside 2-4: '+($resultFailures-join','))

 (F 'FlowToolList').SelectedItem=$gray;M 'LoadDockConfigurationDraft' @($gray)|Out-Null;M 'SelectCentralConfigurationTaskS77' @((F 'DockRunSettingsTaskTab'),'More')|Out-Null;(F 'DockCurrentToolTrialIntervalComboBoxS87').SelectedItem=2000;(F 'DockCurrentToolTrialMetricComboBoxS88').SelectedValue='Mean';(F 'DockCurrentToolTrialWindowComboBoxS88').SelectedItem=10
 M 'DockContinuousTrialToggleButton_ClickS87' @($null,$null)|Out-Null;Assert($null-ne(F 'currentToolTrialSessionS87')) ('Stability session did not start: '+(F 'currentToolTrialLastUiTextS87')+' / '+(F 'DockValidationText').Text);for($i=0;$i-lt12;$i++){M 'RunCurrentToolTrialCycleS87'|Out-Null}
 $history=F 'currentToolTrialStabilityHistoryS88';$samples=$history.Samples
 Assert($samples.Count-eq10) 'Bounded stability window did not retain exactly the latest 10 cycles'
 Assert($samples[$samples.Count-1].Cycle-ge13) 'Stability cycles were not recorded continuously'
 $metricCount=@($samples|Where-Object{$null-ne$_.MetricValue}).Count;Assert($metricCount-eq10) ("Selected Number output was not captured for every valid cycle: Port=$($history.MetricPortName) Values=$metricCount First=$($samples[0].MetricText)")
 Assert((F 'DockCurrentToolTrialStabilitySummaryTextS88').Text-match'窗口 10/10.*Mean.*波动') 'Min/max/mean/range summary is missing'
 Assert($morph.ResultCode-eq'--') 'Stability monitoring executed a downstream tool'
 M 'DockContinuousTrialToggleButton_ClickS87' @($null,$null)|Out-Null;Assert($null-ne(F 'currentToolTrialStabilityHistoryS88')-and$history.Samples.Count-eq10) 'User stop discarded the review window'
 $csv=Join-Path $temp 'stability.csv';M 'ExportCurrentToolTrialStabilityCsvS88' @($csv)|Out-Null;$csvLines=[IO.File]::ReadAllLines($csv,[Text.Encoding]::UTF8);Assert($csvLines.Count-eq11-and$csvLines[0]-match'MetricValue') 'CSV export does not contain the bounded window'
 $window.Width=1938;$window.Height=1158;Pump 250;Shot $StabilityCapture

 Execute $source|Out-Null;Execute $auto|Out-Null;M 'DockContinuousTrialToggleButton_ClickS87' @($null,$null)|Out-Null;Assert($null-ne(F 'currentToolTrialSessionS87')) 'Error-session setup failed';$source.ClearRuntimeOutputs();M 'RunCurrentToolTrialCycleS87'|Out-Null;$errorHistory=F 'currentToolTrialStabilityHistoryS88';$last=$errorHistory.Samples[$errorHistory.Samples.Count-1]
 Assert($null-eq(F 'currentToolTrialSessionS87')-and$last.ResultCode-eq'ERROR'-and$last.ErrorMessage-ne'') 'Execution error was not retained in stability history'
 Assert((F 'DockCurrentToolTrialStabilityAnomalyTextS88').Text-match'最近异常：#.*ERROR') 'Latest anomaly is not visible'
 M 'ClearCurrentToolTrialStabilityS88' @('smoke clear',$true)|Out-Null;Assert((F 'currentToolTrialStabilityHistoryS88').Samples.Count-eq0) 'Clear did not empty the current debug window'

 Execute $source|Out-Null;Execute $auto|Out-Null;M 'DockContinuousTrialToggleButton_ClickS87' @($null,$null)|Out-Null;M 'DockContinuousTrialToggleButton_ClickS87' @($null,$null)|Out-Null;Assert((F 'currentToolTrialStabilityHistoryS88').Samples.Count-ge1) 'Isolation-session setup failed'
 $replacement=Tool GrayStat '新方案灰度检测' $gray.ToolId;M 'LoadDockConfigurationDraft' @($replacement)|Out-Null;Assert($null-eq(F 'currentToolTrialStabilityHistoryS88')) 'A replacement recipe instance reused stale stability samples';$replacement.Dispose()
 $xaml=[IO.File]::ReadAllText((Join-Path $ProjectRoot 'MainWindow.xaml'),[Text.Encoding]::UTF8);Assert($xaml.Contains('DockResultTechnicalExpanderS88')-and$xaml.Contains('更多 · 全部输出与明细')) 'Result task More surface is missing';Assert($xaml.Contains('不作为生产历史或 SPC')) 'Debug-only stability boundary is not visible';Assert(-not$xaml.Contains('CreateToolboxModeMenuItemS80')) 'Legacy nested toolbox category returned'
 Write-Output "S88_RESULT_STABILITY=PASS; KeyRows=$resultKeyCount; Technical=$resultTechnicalCount; ResultProfiles=$resultAudit; Window=10; LastCycle=$($samples[$samples.Count-1].Cycle); CsvRows=$($csvLines.Count); ErrorRetained=True; RecipeIsolation=True; Captures=$ResultCapture,$StabilityCapture"
}finally{try{M 'StopCurrentToolContinuousTrialS87' @('test cleanup',$false)|Out-Null;$mainType.GetField('recipeCloseConfirmed',$private).SetValue($window,$true);$window.Close();$app.Shutdown()}catch{};if(Test-Path $temp){Remove-Item -LiteralPath $temp -Recurse -Force}}
