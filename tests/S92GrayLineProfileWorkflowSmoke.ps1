param([string]$ProjectRoot=(Split-Path -Parent $PSScriptRoot))
$ErrorActionPreference='Stop'
if([Threading.Thread]::CurrentThread.ApartmentState-ne'STA'){throw 'Run with powershell.exe -STA'}
$halcon='C:\Program Files\MVTec\HALCON-20.11-Progress';$env:PATH=(Join-Path $halcon 'bin\x64-win64')+';'+$env:PATH
Add-Type -AssemblyName PresentationFramework
[Reflection.Assembly]::LoadFrom((Join-Path $halcon 'bin\dotnet35\halcondotnet.dll'))|Out-Null
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $ProjectRoot 'bin\x64\Debug\HalconWinFormsDemo.exe'))
$main=$assembly.GetType('HalconWinFormsDemo.MainWindow',$true);$serviceType=$assembly.GetType('HalconWinFormsDemo.Services.RecipeService',$true);$loggerType=$assembly.GetType('HalconWinFormsDemo.Services.AppLogger',$true)
$private=[Reflection.BindingFlags]'Instance,NonPublic';$all=[Reflection.BindingFlags]'Instance,NonPublic,Public'
$temp=Join-Path ([IO.Path]::GetTempPath()) ('halcon-s92-'+[Guid]::NewGuid().ToString('N'));[IO.Directory]::CreateDirectory($temp)|Out-Null
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
function Run($tool){M ExecuteFlowTool @($tool,'S92 automated')|Out-Null}
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
 $snapshot=$snapshotType.GetMethod('Create').Invoke($null,[object[]]@($im.PSObject.BaseObject,$source.ToolId,$source.InstanceName,'Image','','S92'))
 $source.ClearRuntimeOutputs();$source.SetOutputValue('Image',$snapshot);$source.ResultCode='OK'
 M ReplaceCurrentImage @($im.CopyImage(),$true)|Out-Null
}
function Roi([double]$r1,[double]$c1,[double]$r2,[double]$c2,[string]$name){
 $geometry=$assembly.GetType('HalconWinFormsDemo.Models.RoiData').GetMethod('CreateRectangle').Invoke($null,[object[]]@($r1,$c1,$r2,$c2))
 try{M AddRoiLayer @($geometry,$name,$hist)}finally{$geometry.Dispose()}
}
function Capture($visual,[string]$name){
 $visual.UpdateLayout();$folder=Join-Path $ProjectRoot 'artifacts\s92';[IO.Directory]::CreateDirectory($folder)|Out-Null
 $bitmap=[Windows.Media.Imaging.RenderTargetBitmap]::new([int][Math]::Ceiling($visual.ActualWidth),[int][Math]::Ceiling($visual.ActualHeight),96,96,[Windows.Media.PixelFormats]::Pbgra32);$bitmap.Render($visual)
 $encoder=New-Object Windows.Media.Imaging.PngBitmapEncoder;$encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap));$stream=[IO.File]::Open((Join-Path $folder $name),[IO.FileMode]::Create);try{$encoder.Save($stream)}finally{$stream.Dispose()}
}
try{
 $window.WindowState=[Windows.WindowState]::Normal;$window.ShowActivated=$false;$window.Width=1366;$window.Height=768;$window.Show();Pump 200
 $source=Tool ImageSource '图像源';$profile=Tool GrayLineProfile '沿线灰度';$judge=Tool NumericJudge '平均灰度判定'
 foreach($tool in @($source,$profile,$judge)){(F flowTools).Add($tool)}
 $profile.SetInputBinding('Image',$source.ToolId,'Image');$judge.SetInputBinding('Value',$profile.ToolId,'Mean');$judge.InputToolId=$profile.ToolId;$judge.InputPortName='Mean';$judge.NumericLowerLimit=27;$judge.NumericUpperLimit=28
 $profile.Parameters.ProfileStartRow=0.5;$profile.Parameters.ProfileStartColumn=0.5;$profile.Parameters.ProfileEndRow=4.5;$profile.Parameters.ProfileEndColumn=4.5;$profile.Parameters.ProfileSampleCount=5
 Assert((F toolCatalog).Count-eq78) 'Catalog should contain 78 tools'
 $im=Image byte;try{$path=Join-Path $temp 'ramp.png';$im.WriteImage('png',0,$path)}finally{$im.Dispose()}
 $source.Parameters.LocalImagePath=$path;Run $source;Run $profile;Run $judge
 Assert((Out $profile Mean)-eq27.5-and(Out $profile Minimum)-eq5.5-and(Out $profile Maximum)-eq49.5-and$judge.ResultCode-eq'OK') 'Source/profile/Number chain failed'
 Assert((Out $profile MinimumRow)-eq0.5-and(Out $profile MaximumColumn)-eq4.5-and(Out $profile SampleCount)-eq5) 'Extremum coordinates/count failed'
 $firstXld=Out $profile XLD;Assert(-not$firstXld.IsDisposed) 'Managed sampling line missing'
 Run $profile;Assert($firstXld.IsDisposed) 'Rerun leaked old XLD'
 $widthBefore=(F FlowGraphViewport).ActualWidth
 M OpenCentralModuleConfigurationS77 @($profile)|Out-Null
 Assert((F DockKeyParameterSummaryText).Text-match'4') 'Common task does not expose exactly four endpoint coordinates'
 Assert((F DockAdvancedParameterToggleButton).Content-match'1') 'Sample count should be the one extra parameter'
 (F DockProfileEndRowS92).Text='3.5';(F DockProfileEndColumnS92).Text='3.5'
 M DockTrialRunButton_Click @($null,$null)|Out-Null
 Assert($profile.Parameters.ProfileEndRow-eq4.5-and(Out $profile Mean)-eq22) 'Trial changed formal parameters or did not sample draft'
 Assert((M ApplyDockConfigurationDraft @($true))) 'Profile draft apply failed';Run $profile
 Assert($profile.Parameters.ProfileEndRow-eq3.5-and(Out $profile Mean)-eq22) 'Applied profile incorrect'
 Assert([Math]::Abs((F FlowGraphViewport).ActualWidth-$widthBefore)-lt1) 'Parameter editor narrowed flow'
 $id=$profile.ToolId;$sourceId=$source.ToolId
 $recipe=M CaptureRecipe;$path=Join-Path $temp 'profile.json';$service.SaveRecipe($path,$recipe);$loaded=$service.LoadRecipe($path);$item=@($loaded.ToolFlow|Where-Object ToolId -eq $id)[0]
 Assert($item.Parameters.ProfileEndRow-eq3.5-and$item.Parameters.ProfileSampleCount-eq5-and$profile.Parameters.Clone().ProfileEndColumn-eq3.5) 'Recipe/clone lost geometry'
 Assert(-not([IO.File]::ReadAllText($path).Contains('GrayLineProfileResult'))) 'Recipe contains runtime sample values'
 M UndoFlowEditButton_Click @($null,$null)|Out-Null;$profile=@((F flowTools)|Where-Object ToolId -eq $id)[0];Assert($profile.Parameters.ProfileEndRow-eq4.5-and$null-eq$profile.GrayLineProfileResult) 'Undo wrong'
 M RedoFlowEditButton_Click @($null,$null)|Out-Null;$profile=@((F flowTools)|Where-Object ToolId -eq $id)[0];$source=@((F flowTools)|Where-Object ToolId -eq $sourceId)[0]
 Assert($profile.Parameters.ProfileEndRow-eq3.5-and$null-eq$profile.GrayLineProfileResult) 'Redo wrong';Run $source;Run $profile
 Assert((F DockResultVerdictTextS88).Text-eq'OK') 'Pinned result summary stale'
 M SelectCentralConfigurationTaskS77 @((F DockResultTaskTab),'Result')|Out-Null
 Assert(-not(F DockProfileExpanderS92).IsExpanded) 'Curve defaults to expanded';(F DockProfileExpanderS92).IsExpanded=$true;Pump 100
 Assert((F DockProfileChartS92).Children.Count-eq5) 'Real curve and axes missing'
 Capture (F moduleParameterWindowS89) 'profile-parameters-1366.png';Capture $window 'workspace-1366.png'
 $window.Width=1938;$window.Height=1158;Pump 100;Capture (F moduleParameterWindowS89) 'profile-parameters-1938.png';Capture $window 'workspace-1938.png'
 # RGB and original precision.
 $im=Image byte;$rgb=$im.Compose3($im,$im);try{InstallImage $rgb;Run $profile;Assert((Out $profile Mean)-eq22) 'RGB failed'}finally{$rgb.Dispose();$im.Dispose()}
 foreach($case in @(@('uint2',1000),@('int2',-50),@('real',0.125))){$im=Image $case[0] $case[1];try{InstallImage $im;Run $profile;Assert([Math]::Abs((Out $profile Mean)-(22+$case[1]))-lt1e-5) 'Original gray precision changed'}finally{$im.Dispose()}}
 $constant=New-Object HalconDotNet.HImage;$constant.GenImageConst('byte',10,10);try{InstallImage $constant;Run $profile;Assert((Out $profile Range)-eq0-and(Out $profile MinimumDistance)-eq0-and(Out $profile Deviation)-eq0) 'Constant/tie failed'}finally{$constant.Dispose()}
 Run $source;$profile.Parameters.ProfileEndRow=100;$rejected=$false;try{Run $profile}catch{$rejected=$true};Assert($rejected-and$null-eq$profile.GrayLineProfileResult) 'Bounds not rejected';$profile.Parameters.ProfileEndRow=3.5
 $im=Image byte;$domain=[HalconDotNet.HRegion]::new(0.0,0.0,2.0,9.0);$reduced=$im.ReduceDomain($domain)
 try{InstallImage $reduced;$rejected=$false;try{Run $profile}catch{$rejected=$true};Assert($rejected) 'Invalid domain crossed silently'}finally{$reduced.Dispose();$domain.Dispose();$im.Dispose()}
 Run $source;Run $profile;$old=Out $profile XLD;$snapshot=Out $source Image;$snapshot.Dispose();$rejected=$false;try{Run $profile}catch{$rejected=$true}
 Assert($rejected-and$old.IsDisposed-and$null-eq$profile.GrayLineProfileResult-and(F DockProfileChartS92).Children.Count-eq0) 'Stale curve or sampling XLD retained'
 Write-Output 'S92_PROFILE=PASS; Catalog=78; Mean=27.5; DraftMean=22; XLD=True; Number=True; UI=True; JSON=True; UndoRedo=True; RGB=True; Precision=True; Domain=True; Released=True'
}finally{
 SetF dockDraftDirty $false;SetF recipeCloseConfirmed $true;$window.Close();$app.Shutdown()
 $resolved=[IO.Path]::GetFullPath($temp);$parent=[IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\')+'\'
 if(-not$resolved.StartsWith($parent,[StringComparison]::OrdinalIgnoreCase)-or-not[IO.Path]::GetFileName($resolved).StartsWith('halcon-s92-')){throw 'Unsafe cleanup target'}
 Remove-Item -LiteralPath $resolved -Recurse -Force
}
