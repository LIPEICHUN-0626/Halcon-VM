param([string]$ProjectRoot=(Split-Path -Parent $PSScriptRoot))
$ErrorActionPreference='Stop'
if([Threading.Thread]::CurrentThread.ApartmentState-ne'STA'){throw 'Run with powershell.exe -STA'}
$halcon='C:\Program Files\MVTec\HALCON-20.11-Progress';$env:PATH=(Join-Path $halcon 'bin\x64-win64')+';'+$env:PATH
Add-Type -AssemblyName PresentationFramework
[Reflection.Assembly]::LoadFrom((Join-Path $halcon 'bin\dotnet35\halcondotnet.dll'))|Out-Null
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $ProjectRoot 'bin\x64\Debug\HalconWinFormsDemo.exe'))
$main=$assembly.GetType('HalconWinFormsDemo.MainWindow',$true);$serviceType=$assembly.GetType('HalconWinFormsDemo.Services.RecipeService',$true);$loggerType=$assembly.GetType('HalconWinFormsDemo.Services.AppLogger',$true)
$private=[Reflection.BindingFlags]'Instance,NonPublic';$all=[Reflection.BindingFlags]'Instance,NonPublic,Public'
$temp=Join-Path ([IO.Path]::GetTempPath()) ('halcon-s91-'+[Guid]::NewGuid().ToString('N'));[IO.Directory]::CreateDirectory($temp)|Out-Null
$service=$serviceType.GetConstructor([Type[]]@([string])).Invoke([object[]]@($temp.ToString()));$logger=$loggerType.GetConstructor([Type[]]@([string])).Invoke([object[]]@($temp.ToString()))
$app=New-Object Windows.Application;$app.ShutdownMode=[Windows.ShutdownMode]::OnExplicitShutdown
$window=$main.GetConstructor($private,$null,[Type[]]@($serviceType,$loggerType),$null).Invoke([object[]]@($service,$logger))
function Assert([bool]$ok,[string]$message){if(-not$ok){throw $message}}
function F([string]$name){Write-Output -NoEnumerate ($main.GetField($name,$all).GetValue($window))}
function SetF([string]$name,$value){$main.GetField($name,$all).SetValue($window,$value)}
function M([string]$name,[object[]]$arguments=@()){
 $method=$main.GetMethods($private)|Where-Object{$_.Name-eq$name-and$_.GetParameters().Count-eq$arguments.Count}|Select-Object -First 1
 $actual=@($arguments|ForEach-Object{if($null-eq$_){$null}else{$_.PSObject.BaseObject}})
 try{Write-Output -NoEnumerate ($method.Invoke($window,$actual))}catch [Reflection.TargetInvocationException]{throw $_.Exception.InnerException}
}
function Tool([string]$kind,[string]$name){M CreateFlowTool @([Enum]::Parse($assembly.GetType('HalconWinFormsDemo.Models.VmToolKind'),$kind),$name,$true,$null)}
function Run($tool){M ExecuteFlowTool @($tool,'S91 automated')|Out-Null}
function Out($tool,[string]$port){$value=$null;Assert($tool.TryGetOutputValue($port,[ref]$value)) "Missing output $port";Write-Output -NoEnumerate $value}
function Pump([int]$ms){$frame=New-Object Windows.Threading.DispatcherFrame;$timer=New-Object Windows.Threading.DispatcherTimer;$timer.Interval=[TimeSpan]::FromMilliseconds($ms);$timer.Add_Tick({$timer.Stop();$frame.Continue=$false});$timer.Start();[Windows.Threading.Dispatcher]::PushFrame($frame)}
function Image([string]$type,[double]$offset=0){
 $im=New-Object HalconDotNet.HImage;$im.GenImageConst($type,10,10)
 $rows=[HalconDotNet.HTuple]::new([int[]]@(0..99|ForEach-Object{[int][Math]::Floor($_/10)}));$cols=[HalconDotNet.HTuple]::new([int[]]@(0..99|ForEach-Object{$_%10}));$values=[HalconDotNet.HTuple]::new([double[]]@(0..99|ForEach-Object{$_+$offset}))
 try{[HalconDotNet.HOperatorSet]::SetGrayval($im,$rows,$cols,$values)}finally{$rows.Dispose();$cols.Dispose();$values.Dispose()}
 Write-Output -NoEnumerate $im
}
function InstallImage($im){
 $snapshotType=$assembly.GetType('HalconWinFormsDemo.Models.VmImageSnapshot')
 $snapshot=$snapshotType.GetMethod('Create').Invoke($null,[object[]]@($im.PSObject.BaseObject,$source.ToolId,$source.InstanceName,'Image','','S91'))
 $source.ClearRuntimeOutputs();$source.SetOutputValue('Image',$snapshot);$source.ResultCode='OK'
 M ReplaceCurrentImage @($im.CopyImage(),$true)|Out-Null
}
function Roi([double]$r1,[double]$c1,[double]$r2,[double]$c2,[string]$name){
 $geometry=$assembly.GetType('HalconWinFormsDemo.Models.RoiData').GetMethod('CreateRectangle').Invoke($null,[object[]]@($r1,$c1,$r2,$c2))
 try{M AddRoiLayer @($geometry,$name,$hist)}finally{$geometry.Dispose()}
}
function Capture($visual,[string]$name){
 $visual.UpdateLayout();$folder=Join-Path $ProjectRoot 'artifacts\s91';[IO.Directory]::CreateDirectory($folder)|Out-Null
 $bitmap=[Windows.Media.Imaging.RenderTargetBitmap]::new([int][Math]::Ceiling($visual.ActualWidth),[int][Math]::Ceiling($visual.ActualHeight),96,96,[Windows.Media.PixelFormats]::Pbgra32);$bitmap.Render($visual)
 $encoder=New-Object Windows.Media.Imaging.PngBitmapEncoder;$encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap));$stream=[IO.File]::Open((Join-Path $folder $name),[IO.FileMode]::Create);try{$encoder.Save($stream)}finally{$stream.Dispose()}
}
try{
 $window.WindowState=[Windows.WindowState]::Normal;$window.ShowActivated=$false;$window.Width=1366;$window.Height=768;$window.Show();Pump 200
 $source=Tool ImageSource '测试图像';$regionSource=Tool ImageThreshold '前序区域';$hist=Tool GrayHistogram '灰度分布';$judge=Tool NumericJudge '中位数判定'
 foreach($tool in @($source,$regionSource,$hist,$judge)){(F flowTools).Add($tool)}
 $hist.SetInputBinding('Image',$source.ToolId,'Image');$regionSource.SetInputBinding('Image',$source.ToolId,'Image')
 $judge.SetInputBinding('Value',$hist.ToolId,'P50');$judge.InputToolId=$hist.ToolId;$judge.InputPortName='P50';$judge.NumericLowerLimit=48;$judge.NumericUpperLimit=50
 Assert((F toolCatalog).Count-eq78) 'Catalog must contain 78 real tools after S92'
 $profiles=$main.GetField('TaskCommonParameterProfilesS83',[Reflection.BindingFlags]'Static,NonPublic').GetValue($null);Assert($profiles.Count-eq78) 'Task profiles incomplete'
 $im=Image byte;try{$path=Join-Path $temp 'ramp.png';$im.WriteImage('png',0,$path)}finally{$im.Dispose()}
 $source.Parameters.LocalImagePath=$path;Run $source;Run $hist;Run $judge
 Assert($hist.ResultCode-eq'OK'-and$judge.ResultCode-eq'OK') 'ImageSource -> Histogram -> NumericJudge failed'
 Assert([Math]::Abs((Out $hist Mean)-49.5)-lt1e-9-and[Math]::Abs((Out $hist Deviation)-[Math]::Sqrt(833.25))-lt1e-8) 'Mean/deviation incorrect'
 Assert((Out $hist SampleCount)-eq100-and(Out $hist P10)-eq9-and(Out $hist P50)-eq49-and(Out $hist P90)-eq89-and(Out $hist Percentile)-eq94) 'Quantiles incorrect'
 Assert((Out $hist PeakGray)-eq0-and(Out $hist PeakCount)-eq1-and(Out $hist DynamicRange)-eq99) 'Peak tie/range incorrect'
 $hist.Parameters.HistogramPercentile=0;Run $hist;Assert((Out $hist Percentile)-eq0) 'P0 incorrect';$hist.Parameters.HistogramPercentile=100;Run $hist;Assert((Out $hist Percentile)-eq99) 'P100 incorrect';$hist.Parameters.HistogramPercentile=95
 $oldRegion=Out $hist Region;Run $hist;Assert($oldRegion.IsDisposed) 'Old Region leaked'
 # Reduced domains must not be ignored by HALCON statistics.
 $im=Image byte;$domain=[HalconDotNet.HRegion]::new(0.0,0.0,4.0,9.0);$reduced=$im.ReduceDomain($domain)
 try{InstallImage $reduced;Run $hist;Assert((Out $hist SampleCount)-eq50-and(Out $hist Mean)-eq24.5) 'Input domain ignored'}finally{$reduced.Dispose();$domain.Dispose();$im.Dispose()}
 # Local ROI, inherited ROI and merge use one effective pixel set.
 Run $source;$hist.Parameters.RoiSourceMode='Local';$local=Roi 0 0 4 9 '本地检测';Run $hist;Assert((Out $hist SampleCount)-eq50) 'Local ROI failed'
 $mask=Roi 0 0 0 9 '屏蔽首行';$mask.IsMask=$true;Run $hist;Assert((Out $hist SampleCount)-eq40-and(Out $hist Mean)-eq29.5) 'Mask failed'
 $regionSource.Parameters.ImageThresholdMinGray=50;$regionSource.Parameters.ImageThresholdMaxGray=99;Run $regionSource
 $hist.SetInputBinding('ROI',$regionSource.ToolId,'Region');$hist.Parameters.RoiSourceMode='Inherited';Run $hist;Assert((Out $hist SampleCount)-eq50-and(Out $hist Mean)-eq74.5) 'Inherited ROI failed'
 $hist.Parameters.RoiSourceMode='Merge';Run $hist;Assert((Out $hist SampleCount)-eq90-and(Out $hist Mean)-eq54.5) 'Merged ROI double counted or lost mask'
 $hist.SetInputBinding('ROI',$null,$null);$hist.Parameters.RoiSourceMode='Local';$hist.ReplaceRoiBindings([string[]]@())
 # Signed/16-bit data stays exact; no hidden normalization.
 foreach($case in @(@('uint2',1000),@('int2',-50),@('int1',-100))){$im=Image $case[0] $case[1];try{InstallImage $im;Run $hist;Assert((Out $hist Mean)-eq(49.5+$case[1])-and(Out $hist P50)-eq(49+$case[1])) "Raw $($case[0]) gray changed"}finally{$im.Dispose()}}
 $im=Image byte;$rgb=$im.Compose3($im,$im);try{InstallImage $rgb;Run $hist;Assert((Out $hist P50)-eq49) 'RGB conversion failed'}finally{$rgb.Dispose();$im.Dispose()}
 $constant=New-Object HalconDotNet.HImage;$constant.GenImageConst('byte',10,10);try{InstallImage $constant;Run $hist;Assert((Out $hist Deviation)-eq0-and(Out $hist P90)-eq0-and(Out $hist SampleCount)-eq100) 'Constant image failed'}finally{$constant.Dispose()}
 # Empty domain and unsupported precision reject without stale values.
 $im=Image byte;$empty=New-Object HalconDotNet.HRegion;$empty.GenEmptyRegion();$reduced=$im.ReduceDomain($empty)
 try{InstallImage $reduced;$rejected=$false;try{Run $hist}catch{$rejected=$true};Assert($rejected-and$null-eq$hist.GrayHistogramResult) 'Empty input accepted or stale distribution retained'}finally{$reduced.Dispose();$empty.Dispose();$im.Dispose()}
 $im=Image real;try{InstallImage $im;$rejected=$false;try{Run $hist}catch{$rejected=$true};Assert($rejected) 'Real image was silently quantized'}finally{$im.Dispose()}
 Run $source;foreach($bad in @([double]::NaN,[double]::PositiveInfinity,-1,101)){$hist.Parameters.HistogramPercentile=$bad;$rejected=$false;try{Run $hist}catch{$rejected=$true};Assert($rejected) 'Invalid percentile accepted'};$hist.Parameters.HistogramPercentile=95;Run $hist
 $fullMask=Roi 0 0 9 9 '全屏蔽';$fullMask.IsMask=$true;$rejected=$false;try{Run $hist}catch{$rejected=$true};Assert($rejected-and$null-eq$hist.GrayHistogramResult) 'Fully masked image retained statistics';$hist.ReplaceRoiBindings([string[]]@());Run $hist
 # Parameter UI reuses the real draft and persistence.
 M OpenCentralModuleConfigurationS77 @($hist)|Out-Null;Assert((F DockKeyParameterSummaryText).Text-match'1') 'Common page should have one decision'
 (F DockHistogramPercentileTextBoxS91).Text='60';M DockTrialRunButton_Click @($null,$null)|Out-Null
 Assert($hist.Parameters.HistogramPercentile-eq95-and(Out $hist Percentile)-eq59-and$null-ne$hist.GrayHistogramResult) 'Trial did not use draft or changed applied parameters'
 (F DockHistogramPercentileTextBoxS91).Text='75';Assert((M ApplyDockConfigurationDraft @($true))) 'Histogram UI draft failed'
 Assert($hist.Parameters.HistogramPercentile-eq75) 'UI did not update real parameters';Run $hist;Assert((Out $hist Percentile)-eq74) 'Applied percentile did not execute'
 $recipe=M CaptureRecipe;$recipePath=Join-Path $temp 'histogram.json';$service.SaveRecipe($recipePath,$recipe);$loaded=$service.LoadRecipe($recipePath);$saved=@($loaded.ToolFlow|Where-Object ToolId -eq $hist.ToolId)[0]
 Assert($saved.Parameters.HistogramPercentile-eq75-and$hist.Parameters.Clone().HistogramPercentile-eq75) 'JSON/clone lost percentile'
 Assert(-not([IO.File]::ReadAllText($recipePath).Contains('GrayHistogramResult'))) 'Runtime histogram leaked into recipe'
 $histId=$hist.ToolId;$sourceId=$source.ToolId
 M UndoFlowEditButton_Click @($null,$null)|Out-Null;$hist=@((F flowTools)|Where-Object ToolId -eq $histId)[0];Assert($hist.Parameters.HistogramPercentile-eq95-and$null-eq$hist.GrayHistogramResult) 'Undo did not restore parameter-only state'
 M RedoFlowEditButton_Click @($null,$null)|Out-Null;$hist=@((F flowTools)|Where-Object ToolId -eq $histId)[0];$source=@((F flowTools)|Where-Object ToolId -eq $sourceId)[0]
 Assert($hist.Parameters.HistogramPercentile-eq75-and$null-eq$hist.GrayHistogramResult) 'Redo retained runtime histogram or lost percentile';Run $source;Run $hist
 Assert((F DockResultVerdictTextS88).Text-eq'OK'-and@((F dockTaskResultRowsS88)|Where-Object{$_.PortName-eq'Mean'-and$_.ValueText-eq'49.5'}).Count-eq1) 'Pinned editor summary stayed stale without flow selection'
 M SelectCentralConfigurationTaskS77 @((F DockResultTaskTab),'Result')|Out-Null;M RefreshHistogramResultS91 @($hist)|Out-Null
 Assert(-not(F DockHistogramExpanderS91).IsExpanded) 'Distribution must default to collapsed'
 (F DockHistogramExpanderS91).IsExpanded=$true;Pump 150;Assert((F DockHistogramChartS91).Children.Count-gt3) 'Real distribution chart missing'
 Capture (F moduleParameterWindowS89) 'histogram-parameters-1366.png';Capture $window 'workspace-1366.png'
 $window.Width=1938;$window.Height=1158;Pump 100;Capture $window 'workspace-1938.png';Capture (F moduleParameterWindowS89) 'histogram-parameters-1938.png'
 $currentSnapshot=Out $source Image;$currentSnapshot.Dispose();$rejected=$false;try{Run $hist}catch{$rejected=$true};Assert($rejected-and$null-eq$hist.GrayHistogramResult) 'Released upstream snapshot accepted'
 $source.ClearRuntimeOutputs();$source.ResultCode='--';$rejected=$false;try{Run $hist}catch{$rejected=$true};M RefreshHistogramResultS91 @($hist)|Out-Null;$value=$null
 Assert($rejected-and-not$hist.TryGetOutputValue('Mean',[ref]$value)-and$null-eq$hist.GrayHistogramResult-and(F DockHistogramChartS91).Children.Count-eq0) 'Stale input retained statistics/chart'
 Write-Output 'S91_HISTOGRAM=PASS; Catalog=78; Mean=49.5; P10/P50/P90=9/49/89; Domain=50; Local/Inherited/Merge=50/50/90; RawInteger=True; RGB=True; Constant=True; Invalid=True; UI=True; JSON=True; Stale=True'
}finally{
 SetF dockDraftDirty $false;SetF recipeCloseConfirmed $true;$window.Close();$app.Shutdown()
 $resolved=[IO.Path]::GetFullPath($temp);$parent=[IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\')+'\'
 if(-not$resolved.StartsWith($parent,[StringComparison]::OrdinalIgnoreCase)-or-not[IO.Path]::GetFileName($resolved).StartsWith('halcon-s91-')){throw 'Unsafe cleanup target'}
 Remove-Item -LiteralPath $resolved -Recurse -Force
}
