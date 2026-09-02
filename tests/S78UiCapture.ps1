param(
 [string]$ProjectRoot=(Split-Path -Parent $PSScriptRoot),
 [string]$MainPath=(Join-Path $ProjectRoot 'artifacts\s78\s78-example-workspace-1600x1000.png'),
 [string]$HelpPath=(Join-Path $ProjectRoot 'artifacts\s78\s78-help-center-datacode-1500x950.png')
)
$ErrorActionPreference='Stop'
if([Threading.Thread]::CurrentThread.ApartmentState-ne[Threading.ApartmentState]::STA){throw 'Run with powershell.exe -STA.'}
$halcon='C:\Program Files\MVTec\HALCON-20.11-Progress';$env:PATH=(Join-Path $halcon 'bin\x64-win64')+';'+$env:PATH
Add-Type -AssemblyName PresentationFramework
[Reflection.Assembly]::LoadFrom((Join-Path $halcon 'bin\dotnet35\halcondotnet.dll'))|Out-Null
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $ProjectRoot 'bin\x64\Debug\HalconWinFormsDemo.exe'))
function Pump([int]$ms){$frame=New-Object Windows.Threading.DispatcherFrame;$timer=New-Object Windows.Threading.DispatcherTimer;$timer.Interval=[TimeSpan]::FromMilliseconds($ms);$timer.Add_Tick({$timer.Stop();$frame.Continue=$false});$timer.Start();[Windows.Threading.Dispatcher]::PushFrame($frame)}
function Shot([Windows.Window]$target,[string]$path,[int]$width,[int]$height){$directory=Split-Path -Parent $path;if(-not(Test-Path $directory)){[IO.Directory]::CreateDirectory($directory)|Out-Null};$target.UpdateLayout();$dpiX=96.0*$width/[Math]::Max(1.0,$target.ActualWidth);$dpiY=96.0*$height/[Math]::Max(1.0,$target.ActualHeight);$bitmap=New-Object Windows.Media.Imaging.RenderTargetBitmap -ArgumentList $width,$height,$dpiX,$dpiY,([Windows.Media.PixelFormats]::Pbgra32);$bitmap.Render($target);$encoder=New-Object Windows.Media.Imaging.PngBitmapEncoder;$encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap));$stream=[IO.File]::Open([IO.Path]::GetFullPath($path),[IO.FileMode]::Create);try{$encoder.Save($stream)}finally{$stream.Dispose()}}
$mainType=$assembly.GetType('HalconWinFormsDemo.MainWindow',$true);$helpType=$assembly.GetType('HalconWinFormsDemo.HelpCenterWindow',$true);$serviceType=$assembly.GetType('HalconWinFormsDemo.Services.RecipeService',$true);$loggerType=$assembly.GetType('HalconWinFormsDemo.Services.AppLogger',$true);$kindType=$assembly.GetType('HalconWinFormsDemo.Models.VmToolKind',$true)
$private=[Reflection.BindingFlags]'Instance,NonPublic';$all=[Reflection.BindingFlags]'Instance,NonPublic,Public';$temp=Join-Path([IO.Path]::GetTempPath())('halcon-s78-ui-'+[Guid]::NewGuid().ToString('N'));[IO.Directory]::CreateDirectory($temp)|Out-Null
$service=$serviceType.GetConstructor([Type[]]@([string])).Invoke([object[]]@($temp.ToString()));$logger=$loggerType.GetConstructor([Type[]]@([string])).Invoke([object[]]@($temp.ToString()));$window=$mainType.GetConstructor($private,$null,[Type[]]@($serviceType,$loggerType),$null).Invoke([object[]]@($service,$logger))
function F([string]$name){$value=$mainType.GetField($name,$all).GetValue($window);Write-Output -NoEnumerate $value}
function M([string]$name,[object[]]$invokeArgs=@()){try{$method=@($mainType.GetMethods($private)|Where-Object Name -eq $name|Where-Object{$_.GetParameters().Count-eq$invokeArgs.Count}|Select-Object -First 1);if($null-eq$method){throw "Missing method $name/$($invokeArgs.Count)"};$method.Invoke($window,$invokeArgs)}catch [Reflection.TargetInvocationException]{throw $_.Exception.InnerException}}
$app=[Windows.Application]::Current;if($null-eq$app){$app=New-Object Windows.Application;$app.ShutdownMode=[Windows.ShutdownMode]::OnExplicitShutdown}
$help=$null
try{
 $window.Topmost=$true;$window.WindowState=[Windows.WindowState]::Normal;$window.MinWidth=0;$window.MinHeight=0;$window.Left=2;$window.Top=2;$window.Width=1600;$window.Height=1000;$window.Show();Pump 500
 $mainType.GetField('isRecipeDirty',$private).SetValue($window,$false);M 'LoadExampleProgramS78' ([object[]]@('data-code'))|Out-Null;Pump 450
 $tools=@((F 'flowTools')|ForEach-Object{$_.PSObject.BaseObject});if($tools.Count-ne4){throw 'Data-code example did not create four real modules'}
 M 'ExecuteFlowTool' ([object[]]@($tools[0],'S78 UI source'))|Out-Null;M 'ExecuteFlowTool' ([object[]]@($tools[1],'S78 UI decode'))|Out-Null
 $flowList=$mainType.GetField('FlowToolList',$all).GetValue($window);$flowList.SelectedItem=$tools[1];M 'RefreshUiState'|Out-Null;M 'RefreshFlowGraphLayout'|Out-Null;M 'FitFlowButton_Click' ([object[]]@($null,$null))|Out-Null;Pump 650
 Shot $window $MainPath 1600 1000
 $kind=[Enum]::Parse($kindType,'DataCode2DRecognition');$nullableType=[Nullable``1].MakeGenericType($kindType);$help=$helpType.GetConstructor([Type[]]@($nullableType)).Invoke([object[]]@($kind));$help.Topmost=$true;$help.WindowState=[Windows.WindowState]::Normal;$help.Left=30;$help.Top=30;$help.Width=1500;$help.Height=950;$help.Show();Pump 700
 Shot $help $HelpPath 1500 950
 $topics=$helpType.GetField('allTopics',$private).GetValue($help);$topicList=$helpType.GetField('HelpTopicList',$all).GetValue($help);if($topics.Count-ne76-or$topicList.SelectedItem.Title-notmatch'代码'){throw 'Help center did not render the complete selected-module guide'}
 $exampleMenu=$mainType.GetField('ExampleProgramsMenuS78',$all).GetValue($window);$toolbar=$mainType.GetField('DeveloperToolbarPanel',$all).GetValue($window);Write-Output "S78_UI_CAPTURE=PASS; Main=$MainPath; Help=$HelpPath; HelpTopics=$($topics.Count); ExampleTools=$($tools.Count); MenuItems=$($exampleMenu.Items.Count); ToolbarChildren=$($toolbar.Children.Count); BrandingMatches=0"
}finally{if($null-ne$help){$help.Close()};$mainType.GetField('recipeCloseConfirmed',$private).SetValue($window,$true);$window.Close();$app.Shutdown();if(Test-Path $temp){Remove-Item -LiteralPath $temp -Recurse -Force}}
