param(
  [string]$ProjectRoot=(Split-Path -Parent $PSScriptRoot),
  [string]$DefaultOutputPath=(Join-Path $ProjectRoot 'artifacts\s59\s59-default-first-screen-1938x1158.png'),
  [string]$WideOutputPath=(Join-Path $ProjectRoot 'artifacts\s59\s59-global-variables-1938x1158.png'),
  [string]$CompactOutputPath=(Join-Path $ProjectRoot 'artifacts\s59\s59-global-variables-1366x768.png')
)
$ErrorActionPreference='Stop'
if([Threading.Thread]::CurrentThread.ApartmentState-ne[Threading.ApartmentState]::STA){throw 'Run with powershell.exe -STA.'}
$halcon='C:\Program Files\MVTec\HALCON-20.11-Progress';$env:PATH=(Join-Path $halcon 'bin\x64-win64')+';'+$env:PATH
Add-Type -AssemblyName PresentationFramework
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $ProjectRoot 'bin\x64\Debug\HalconWinFormsDemo.exe'))
function Assert([bool]$condition,[string]$message){if(-not$condition){throw $message}}
function Pump([int]$ms){$frame=New-Object Windows.Threading.DispatcherFrame;$timer=New-Object Windows.Threading.DispatcherTimer;$timer.Interval=[TimeSpan]::FromMilliseconds($ms);$timer.Add_Tick({$timer.Stop();$frame.Continue=$false});$timer.Start();[Windows.Threading.Dispatcher]::PushFrame($frame)}
function Shot([string]$path,[int]$targetWidth,[int]$targetHeight){$dir=Split-Path -Parent $path;if(-not(Test-Path $dir)){[IO.Directory]::CreateDirectory($dir)|Out-Null};$window.UpdateLayout();$dpiX=96*$targetWidth/[Math]::Max(1,$window.ActualWidth);$dpiY=96*$targetHeight/[Math]::Max(1,$window.ActualHeight);$render=New-Object Windows.Media.Imaging.RenderTargetBitmap($targetWidth,$targetHeight,$dpiX,$dpiY,[Windows.Media.PixelFormats]::Pbgra32);$render.Render($window);$encoder=New-Object Windows.Media.Imaging.PngBitmapEncoder;$encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($render));$stream=[IO.File]::Open([IO.Path]::GetFullPath($path),[IO.FileMode]::Create);try{$encoder.Save($stream)}finally{$stream.Dispose()};"${targetWidth}x${targetHeight} (WPF render)"}
$temp=Join-Path([IO.Path]::GetTempPath())('halcon-vm-s59-ui-'+[Guid]::NewGuid().ToString('N'));[IO.Directory]::CreateDirectory($temp)|Out-Null
$main=$assembly.GetType('HalconWinFormsDemo.MainWindow',$true);$serviceType=$assembly.GetType('HalconWinFormsDemo.Services.RecipeService',$true);$loggerType=$assembly.GetType('HalconWinFormsDemo.Services.AppLogger',$true);$variableType=$assembly.GetType('HalconWinFormsDemo.Models.VmGlobalVariableData',$true);$private=[Reflection.BindingFlags]'Instance,NonPublic';$all=[Reflection.BindingFlags]'Instance,NonPublic,Public'
$service=$serviceType.GetConstructor([Type[]]@([string])).Invoke([object[]]@($temp.ToString()));$logger=$loggerType.GetConstructor([Type[]]@([string])).Invoke([object[]]@($temp.ToString()));$window=$main.GetConstructor($private,$null,[Type[]]@($serviceType,$loggerType),$null).Invoke([object[]]@($service,$logger))
function F([string]$name){$field=$main.GetField($name,$all);if($null-eq$field){return $null};$value=$field.GetValue($window);Write-Output -NoEnumerate $value}
function NewVar([string]$name,[string]$type,[object]$default,[object]$current){$v=[Activator]::CreateInstance($variableType);$v.Name=$name;$v.ValueType=$type;if($type-eq'Number'){$v.DefaultNumberValue=[double]$default}elseif($type-eq'String'){$v.DefaultTextValue=[string]$default}elseif($type-eq'Boolean'){$v.DefaultBooleanValue=[bool]$default}else{$v.DefaultResultCodeValue=[string]$default};$v.SetCurrentValue($current);$v}
$app=[Windows.Application]::Current;if($null-eq$app){$app=New-Object Windows.Application;$app.ShutdownMode=[Windows.ShutdownMode]::OnExplicitShutdown}
$window.Topmost=$true;$window.Show();$window.WindowState=[Windows.WindowState]::Normal;$window.MinWidth=0;$window.MinHeight=0;$window.Left=2;$window.Top=2;$window.Width=1938;$window.Height=1158;Pump 550
Assert((F 'GlobalVariableWorkspace').Visibility.ToString()-eq'Collapsed') 'Variable workspace is visible on the default first screen';Assert((F 'DeveloperToolbarPanel').Children.Count-eq7) 'A new permanent toolbar command was exposed';$default=Shot $DefaultOutputPath 1938 1158
$globals=F 'globalVariables';$number=NewVar 'InspectionThreshold' 'Number' ([double]15) ([double]18.5);$text=NewVar 'ProductCode' 'String' 'P-100' 'P-2048';$boolean=NewVar 'EnableInspection' 'Boolean' $true $true;$result=NewVar 'PreviousResult' 'ResultCode' 'OK' 'NG';foreach($v in @($number,$text,$boolean,$result)){$globals.Add($v)}
$number.SetSubscriptionCounts(2,1);$text.SetSubscriptionCounts(1,0);$boolean.SetSubscriptionCounts(0,1);$result.SetSubscriptionCounts(1,0)
$main.GetMethod('OpenGlobalVariablesButton_Click',$private).Invoke($window,[object[]]@($null,$null))|Out-Null;(F 'GlobalVariableListBox').SelectedItem=$number;Pump 550
Assert((F 'GlobalVariableWorkspace').Visibility.ToString()-eq'Visible'-and(F 'MainWorkspaceGrid').Visibility.ToString()-eq'Collapsed') 'On-demand workspace is not active';Assert((F 'GlobalVariableListBox').ActualWidth-gt250-and(F 'GlobalVariableNameTextBox').ActualWidth-gt200) 'Wide variable workbench did not lay out correctly';$wide=Shot $WideOutputPath 1938 1158
$window.Width=1366;$window.Height=768;$window.Left=2;$window.Top=2;Pump 600;Assert((F 'GlobalVariableListBox').ActualHeight-gt250-and(F 'GlobalVariableCurrentValueTextBox').ActualWidth-gt200) 'Compact variable workbench is clipped';$compact=Shot $CompactOutputPath 1366 768
$main.GetField('recipeCloseConfirmed',$private).SetValue($window,$true);$window.Close();$app.Shutdown();if(Test-Path $temp){Remove-Item -LiteralPath $temp -Recurse -Force}
Write-Output "S59_UI_CAPTURE=PASS; Default=$DefaultOutputPath; Wide=$WideOutputPath; Compact=$CompactOutputPath; DefaultSize=$default; WideSize=$wide; CompactSize=$compact; PermanentToolbarChildren=7; WorkspaceDefaultVisible=0; BSB=NOT_IMPLEMENTED_BY_USER_DIRECTIVE"
