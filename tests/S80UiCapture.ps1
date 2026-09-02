param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$LargePath = (Join-Path $ProjectRoot 'artifacts\s80\s80-single-category-barcode-1938x1158.png'),
    [string]$CompactPath = (Join-Path $ProjectRoot 'artifacts\s80\s80-single-category-barcode-1366x768.png')
)
$ErrorActionPreference='Stop'
if([Threading.Thread]::CurrentThread.ApartmentState-ne[Threading.ApartmentState]::STA){throw 'Run with powershell.exe -STA.'}
$halcon='C:\Program Files\MVTec\HALCON-20.11-Progress';$env:PATH=(Join-Path $halcon 'bin\x64-win64')+';'+$env:PATH
Add-Type -AssemblyName PresentationFramework
[Reflection.Assembly]::LoadFrom((Join-Path $halcon 'bin\dotnet35\halcondotnet.dll'))|Out-Null
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $ProjectRoot 'bin\x64\Debug\HalconWinFormsDemo.exe'))
function Pump([int]$ms){$frame=New-Object Windows.Threading.DispatcherFrame;$timer=New-Object Windows.Threading.DispatcherTimer;$timer.Interval=[TimeSpan]::FromMilliseconds($ms);$timer.Add_Tick({$timer.Stop();$frame.Continue=$false});$timer.Start();[Windows.Threading.Dispatcher]::PushFrame($frame)}
function Shot([string]$path,[int]$width,[int]$height){$dir=Split-Path -Parent $path;if(-not(Test-Path $dir)){[IO.Directory]::CreateDirectory($dir)|Out-Null};$window.UpdateLayout();$bmp=New-Object Windows.Media.Imaging.RenderTargetBitmap -ArgumentList $width,$height,96,96,([Windows.Media.PixelFormats]::Pbgra32);$bmp.Render($window);$encoder=New-Object Windows.Media.Imaging.PngBitmapEncoder;$encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bmp));$stream=[IO.File]::Open([IO.Path]::GetFullPath($path),[IO.FileMode]::Create);try{$encoder.Save($stream)}finally{$stream.Dispose()}}
$mainType=$assembly.GetType('HalconWinFormsDemo.MainWindow',$true);$serviceType=$assembly.GetType('HalconWinFormsDemo.Services.RecipeService',$true);$loggerType=$assembly.GetType('HalconWinFormsDemo.Services.AppLogger',$true);$kindType=$assembly.GetType('HalconWinFormsDemo.Models.VmToolKind',$true)
$private=[Reflection.BindingFlags]'Instance,NonPublic';$all=[Reflection.BindingFlags]'Instance,NonPublic,Public';$temp=Join-Path([IO.Path]::GetTempPath())('halcon-s80-ui-'+[Guid]::NewGuid().ToString('N'));[IO.Directory]::CreateDirectory($temp)|Out-Null
$service=$serviceType.GetConstructor([Type[]]@([string])).Invoke([object[]]@($temp.ToString()));$logger=$loggerType.GetConstructor([Type[]]@([string])).Invoke([object[]]@($temp.ToString()));$window=$mainType.GetConstructor($private,$null,[Type[]]@($serviceType,$loggerType),$null).Invoke([object[]]@($service,$logger))
function F([string]$name){$v=$mainType.GetField($name,$all).GetValue($window);Write-Output -NoEnumerate $v}
function M([string]$name,[object[]]$invokeArgs=@()){try{$m=$mainType.GetMethods($private)|Where-Object Name -eq $name|Where-Object{$_.GetParameters().Count-eq$invokeArgs.Count}|Select-Object -First 1;if($null-eq$m){throw "Missing $name/$($invokeArgs.Count)"};$m.Invoke($window,$invokeArgs)}catch [Reflection.TargetInvocationException]{throw $_.Exception.InnerException}}
function Tool([string]$kind,[string]$name){$mainType.GetMethod('CreateFlowTool',$private).Invoke($window,[object[]]@([Enum]::Parse($kindType,$kind),$name,$true,$null))}
$app=[Windows.Application]::Current;if($null-eq$app){$app=New-Object Windows.Application;$app.ShutdownMode=[Windows.ShutdownMode]::OnExplicitShutdown}
try{
  $window.Topmost=$true;$window.Show();$window.WindowState=[Windows.WindowState]::Normal;$window.MinWidth=0;$window.MinHeight=0;$window.Left=0;$window.Top=0;$window.Width=1938;$window.Height=1158;Pump 700
  $flow=F 'flowTools';M 'DisposeFlowTools'|Out-Null;$flow.Clear();$source=Tool ImageSource '01 Barcode Image';$reader=Tool BarCode1DRecognition '02 Barcode Reader';$judge=Tool NumericJudge '03 Count Judge';$flow.Add($source);$flow.Add($reader);$flow.Add($judge);$reader.SetInputBinding('Image',$source.ToolId,'Image');$judge.SetInputBinding('Value',$reader.ToolId,'CodeCount');$judge.InputToolId=$reader.ToolId;$judge.InputPortName='CodeCount';$judge.NumericLowerLimit=1;$judge.NumericUpperLimit=8
  $source.Parameters.LocalImagePath=Join-Path $env:PUBLIC 'Documents\MVTec\HALCON-20.11-Progress\examples\images\barcode\code128\code12801.png';$source.Parameters.LocalImageSerialNumber='S80-UI';$reader.Parameters.BarCodeType='Code 128';$reader.Parameters.BarCodeMaxResults=8
  M 'ExecuteFlowTool' @($source,'S80 UI')|Out-Null;M 'ExecuteFlowTool' @($reader,'S80 UI')|Out-Null;M 'ExecuteFlowTool' @($judge,'S80 UI')|Out-Null
  $catalog=F 'toolCatalog';$barcodeKind=[Enum]::Parse($kindType,'BarCode1DRecognition');$barcodeItem=@($catalog|Where-Object{$_.Kind-eq$barcodeKind})[0];$mainType.GetField('toolboxSelectedCategoryS80',$private).SetValue($window,$barcodeItem.Category);$mainType.GetField('toolCatalogMode',$private).SetValue($window,'Category');M 'RefreshToolCatalogView'|Out-Null
  (F 'FlowToolList').SelectedItem=$reader;M 'RefreshUiState'|Out-Null;M 'RefreshFlowGraphLayout'|Out-Null;M 'OpenCentralModuleConfigurationS77' @($reader)|Out-Null;Pump 900
  if(@((F 'toolCatalogView')|Where-Object{$_.Category-ne$barcodeItem.Category}).Count-ne0){throw 'Toolbox renders more than one category'}
  Shot $LargePath 1938 1158
  $window.Width=1366;$window.Height=768;Pump 900;Shot $CompactPath 1366 768
  if((F 'CentralModuleConfigurationOverlayS77').Visibility.ToString()-ne'Visible'){throw 'Central barcode configuration is not visible'}
  Write-Output "S80_UI_CAPTURE=PASS; Category=$($barcodeItem.Category); Visible=$(@((F 'toolCatalogView')).Count); Large=$LargePath; Compact=$CompactPath"
}
finally{try{$mainType.GetField('recipeCloseConfirmed',$private).SetValue($window,$true);$window.Close();$app.Shutdown()}catch{};if(Test-Path $temp){Remove-Item -LiteralPath $temp -Recurse -Force}}
