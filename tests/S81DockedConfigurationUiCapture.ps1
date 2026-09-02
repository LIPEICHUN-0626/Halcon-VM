param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$LargePath = (Join-Path $ProjectRoot 'artifacts\s81\s81-docked-config-large.png'),
    [string]$CompactPath = (Join-Path $ProjectRoot 'artifacts\s81\s81-docked-config-1366x768.png')
)
$ErrorActionPreference='Stop'
if([Threading.Thread]::CurrentThread.ApartmentState-ne[Threading.ApartmentState]::STA){throw 'Run with powershell.exe -STA.'}
$halcon='C:\Program Files\MVTec\HALCON-20.11-Progress';$env:PATH=(Join-Path $halcon 'bin\x64-win64')+';'+$env:PATH
Add-Type -AssemblyName PresentationFramework
[Reflection.Assembly]::LoadFrom((Join-Path $halcon 'bin\dotnet35\halcondotnet.dll'))|Out-Null
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $ProjectRoot 'bin\x64\Debug\HalconWinFormsDemo.exe'))
function Pump([int]$ms){$frame=New-Object Windows.Threading.DispatcherFrame;$timer=New-Object Windows.Threading.DispatcherTimer;$timer.Interval=[TimeSpan]::FromMilliseconds($ms);$timer.Add_Tick({$timer.Stop();$frame.Continue=$false});$timer.Start();[Windows.Threading.Dispatcher]::PushFrame($frame)}
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
function Assert([bool]$ok,[string]$message){if(-not$ok){throw "Assertion failed: $message"}}
function Bounds($element){$point=$element.TransformToAncestor($window).Transform((New-Object Windows.Point(0,0)));New-Object Windows.Rect($point.X,$point.Y,$element.ActualWidth,$element.ActualHeight)}
function ValidateLayout([string]$label){
  $config=Bounds (F 'CentralModuleConfigurationOverlayS77');$image=Bounds (F 'ImageWorkspacePanel');$flowBounds=Bounds (F 'FlowWorkspacePanel')
  Assert(-not$config.IntersectsWith($image)-and-not$config.IntersectsWith($flowBounds)) "$label configuration overlaps image or flow"
  Assert($config.Width-ge359-and$image.Width-ge389-and$flowBounds.Width-ge309) "$label unusable panel width"
  Assert($config.Right-le$window.ActualWidth-and$image.Right-le$window.ActualWidth) "$label panels exceed visible window: image=$image config=$config window=$($window.ActualWidth) workspace=$((F 'MainWorkspaceGrid').ActualWidth)"
  Assert((F 'FlowGraphViewport').ScrollableWidth-lt1) "$label automatic flow has horizontal scrolling"
  Assert((F 'ToolCatalogPanel').Visibility.ToString()-eq'Collapsed') "$label toolbox duplicates flow while editing"
  Assert([object]::ReferenceEquals((F 'CentralModuleConfigurationHostS77').Content,(F 'DockConfigurationWorkspaceContent'))) "$label duplicate configuration state"
  Write-Output ("S81_LAYOUT; Size="+$label+"; Actual="+$window.ActualWidth+"x"+$window.ActualHeight+"; Config="+$config.Width+"; Image="+$image.Width+"; Flow="+$flowBounds.Width+"; Overlap=0")
}
function Shot([string]$path){
  $dir=Split-Path -Parent $path;[IO.Directory]::CreateDirectory($dir)|Out-Null
  $window.UpdateLayout();$root=$window.Content;$top=$root.PointToScreen((New-Object Windows.Point(0,0)));$end=$root.PointToScreen((New-Object Windows.Point($root.ActualWidth,$root.ActualHeight)))
  $screen=[Windows.Forms.Screen]::FromPoint((New-Object Drawing.Point([int]$top.X,[int]$top.Y))).Bounds
  $width=[int][Math]::Min($end.X-$top.X,$screen.Right-$top.X);$height=[int][Math]::Min($end.Y-$top.Y,$screen.Bottom-$top.Y)
  $bmp=New-Object Drawing.Bitmap($width,$height);$graphics=[Drawing.Graphics]::FromImage($bmp)
  try{$graphics.CopyFromScreen([int]$top.X,[int]$top.Y,0,0,$bmp.Size);$bmp.Save($path,[Drawing.Imaging.ImageFormat]::Png)}finally{$graphics.Dispose();$bmp.Dispose()}
}

$mainType=$assembly.GetType('HalconWinFormsDemo.MainWindow',$true);$serviceType=$assembly.GetType('HalconWinFormsDemo.Services.RecipeService',$true);$loggerType=$assembly.GetType('HalconWinFormsDemo.Services.AppLogger',$true);$kindType=$assembly.GetType('HalconWinFormsDemo.Models.VmToolKind',$true)
$private=[Reflection.BindingFlags]'Instance,NonPublic';$all=[Reflection.BindingFlags]'Instance,NonPublic,Public';$temp=Join-Path([IO.Path]::GetTempPath())('halcon-s81-ui-'+[Guid]::NewGuid().ToString('N'));[IO.Directory]::CreateDirectory($temp)|Out-Null
$service=$serviceType.GetConstructor([Type[]]@([string])).Invoke([object[]]@($temp.ToString()));$logger=$loggerType.GetConstructor([Type[]]@([string])).Invoke([object[]]@($temp.ToString()));$window=$mainType.GetConstructor($private,$null,[Type[]]@($serviceType,$loggerType),$null).Invoke([object[]]@($service,$logger))
function F([string]$name){$v=$mainType.GetField($name,$all).GetValue($window);Write-Output -NoEnumerate $v}
function M([string]$name,[object[]]$invokeArgs=@()){try{$m=$mainType.GetMethods($private)|Where-Object Name -eq $name|Where-Object{$_.GetParameters().Count-eq$invokeArgs.Count}|Select-Object -First 1;if($null-eq$m){throw "Missing $name/$($invokeArgs.Count)"};$m.Invoke($window,$invokeArgs)}catch [Reflection.TargetInvocationException]{throw $_.Exception.InnerException}}
function Tool([string]$kind,[string]$name){$mainType.GetMethod('CreateFlowTool',$private).Invoke($window,[object[]]@([Enum]::Parse($kindType,$kind),$name,$true,$null))}
$app=[Windows.Application]::Current;if($null-eq$app){$app=New-Object Windows.Application;$app.ShutdownMode=[Windows.ShutdownMode]::OnExplicitShutdown}
try{
  $window.Topmost=$true;$window.Show();$window.WindowState=[Windows.WindowState]::Normal;$window.MinWidth=0;$window.MinHeight=0;$window.Left=0;$window.Top=0;$window.Width=1938;$window.Height=1158;Pump 700
  $flow=F 'flowTools';M 'DisposeFlowTools'|Out-Null;$flow.Clear();$source=Tool ImageSource '01 Barcode Image';$reader=Tool BarCode1DRecognition '02 Barcode Reader';$judge=Tool NumericJudge '03 Count Judge';$flow.Add($source);$flow.Add($reader);$flow.Add($judge);$reader.SetInputBinding('Image',$source.ToolId,'Image');$judge.SetInputBinding('Value',$reader.ToolId,'CodeCount');$judge.InputToolId=$reader.ToolId;$judge.InputPortName='CodeCount';$judge.NumericLowerLimit=1;$judge.NumericUpperLimit=8
  $source.Parameters.LocalImagePath=Join-Path $env:PUBLIC 'Documents\MVTec\HALCON-20.11-Progress\examples\images\barcode\code128\code12801.png';$source.Parameters.LocalImageSerialNumber='S81-UI';$reader.Parameters.BarCodeType='Code 128';$reader.Parameters.BarCodeMaxResults=8
  M 'ExecuteFlowTool' @($source,'S81 UI')|Out-Null;M 'ExecuteFlowTool' @($reader,'S81 UI')|Out-Null;M 'ExecuteFlowTool' @($judge,'S81 UI')|Out-Null
  $catalog=F 'toolCatalog';$barcodeKind=[Enum]::Parse($kindType,'BarCode1DRecognition');$barcodeItem=@($catalog|Where-Object{$_.Kind-eq$barcodeKind})[0];$mainType.GetField('toolboxSelectedCategoryS80',$private).SetValue($window,$barcodeItem.Category);$mainType.GetField('toolCatalogMode',$private).SetValue($window,'Category');M 'RefreshToolCatalogView'|Out-Null
  (F 'FlowToolList').SelectedItem=$reader;M 'RefreshUiState'|Out-Null;M 'RefreshFlowGraphLayout'|Out-Null;M 'OpenCentralModuleConfigurationS77' @($reader)|Out-Null;Pump 900
  if(@((F 'toolCatalogView')|Where-Object{$_.Category-ne$barcodeItem.Category}).Count-ne0){throw 'Toolbox renders more than one category'}
  ValidateLayout 'requested-1938x1158';Shot $LargePath
  $window.Width=1366;$window.Height=768;Pump 900;ValidateLayout '1366x768';Shot $CompactPath
  $window.Width=1120;Pump 500;ValidateLayout '1120x768'
  if((F 'CentralModuleConfigurationOverlayS77').Visibility.ToString()-ne'Visible'){throw 'Central barcode configuration is not visible'}
  M 'CentralInputsButton_ClickS77' @($null,$null)|Out-Null;Assert([object]::ReferenceEquals((F 'DockConfigurationTabs').SelectedItem,(F 'DockIoTaskTab'))) 'Input tab did not open'
  M 'CentralRoiButton_ClickS77' @($null,$null)|Out-Null;M 'CentralResultsButton_ClickS77' @($null,$null)|Out-Null;M 'CentralParametersButton_ClickS77' @($null,$null)|Out-Null
  Assert((M 'CloseCentralModuleConfigurationS77')) 'Clean configuration close failed'
  Assert((F 'ToolCatalogPanel').Visibility.ToString()-eq'Visible'-and[Windows.Controls.Grid]::GetColumn((F 'FlowWorkspacePanel'))-eq2) 'Toolbox and flow not restored'
  M 'OpenCentralModuleConfigurationS77' @($reader)|Out-Null;M 'ImageWorkspaceExpandButton_Click' @($null,$null)|Out-Null
  Assert(-not(F 'centralModuleConfigurationOpenS77')-and(F 'imageWorkspaceExpanded')) 'Image expansion did not safely leave configuration'
  M 'OpenCentralModuleConfigurationS77' @($reader)|Out-Null;Pump 400;ValidateLayout 'restore-from-expanded'
  M 'SetProductionRunMode' @($true)|Out-Null;Assert(-not(F 'centralModuleConfigurationOpenS77')) 'Production kept development configuration'
  M 'SetProductionRunMode' @($false)|Out-Null;Assert((F 'ToolCatalogPanel').Visibility.ToString()-eq'Visible') 'Production did not restore normal toolbox'
  Write-Output "S81_UI_LAYOUT=PASS; Category=$($barcodeItem.Category); Visible=$(@((F 'toolCatalogView')).Count); Large=$LargePath; Compact=$CompactPath"
}
finally{try{$mainType.GetField('recipeCloseConfirmed',$private).SetValue($window,$true);$window.Close();$app.Shutdown()}catch{};if(Test-Path $temp){Remove-Item -LiteralPath $temp -Recurse -Force}}
