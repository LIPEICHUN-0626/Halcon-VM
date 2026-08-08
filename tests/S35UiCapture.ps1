param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$RoiOutputPath = (Join-Path ([IO.Path]::GetTempPath()) 'halcon-vm-s35-line-point-roi.png'),
    [string]$RoiHalconOutputPath = (Join-Path ([IO.Path]::GetTempPath()) 'halcon-vm-s35-line-point-roi-view.png'),
    [string]$RotateOutputPath = (Join-Path ([IO.Path]::GetTempPath()) 'halcon-vm-s35-image-rotate.png'),
    [string]$HalconOutputPath = (Join-Path ([IO.Path]::GetTempPath()) 'halcon-vm-s35-rotated-view.png')
)

$ErrorActionPreference='Stop'
if([Threading.Thread]::CurrentThread.ApartmentState -ne [Threading.ApartmentState]::STA){throw 'Run with powershell.exe -STA.'}
$halcon='C:\Program Files\MVTec\HALCON-20.11-Progress';$env:PATH=(Join-Path $halcon 'bin\x64-win64')+';'+$env:PATH
Add-Type -AssemblyName PresentationFramework;Add-Type -AssemblyName System.Drawing;Add-Type -AssemblyName System.Windows.Forms
[Reflection.Assembly]::LoadFrom((Join-Path $halcon 'bin\dotnet35\halcondotnet.dll'))|Out-Null
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $ProjectRoot 'bin\x64\Debug\HalconWinFormsDemo.exe'))

function Pump-Ui([int]$Milliseconds){$frame=New-Object Windows.Threading.DispatcherFrame;$timer=New-Object Windows.Threading.DispatcherTimer;$timer.Interval=[TimeSpan]::FromMilliseconds($Milliseconds);$timer.Add_Tick({$timer.Stop();$frame.Continue=$false});$timer.Start();[Windows.Threading.Dispatcher]::PushFrame($frame)}
function Save-Shot([string]$Path){
    $window.UpdateLayout();$topLeft=$window.PointToScreen([Windows.Point]::new(0,0));$bottomRight=$window.PointToScreen([Windows.Point]::new($window.ActualWidth,$window.ActualHeight))
    $width=[Math]::Max(1,[int][Math]::Ceiling($bottomRight.X-$topLeft.X));$height=[Math]::Max(1,[int][Math]::Ceiling($bottomRight.Y-$topLeft.Y))
    $capture=[Drawing.Bitmap]::new($width,$height);$graphics=[Drawing.Graphics]::FromImage($capture);$saved=$false
    try{$graphics.CopyFromScreen([int]$topLeft.X,[int]$topLeft.Y,0,0,[Drawing.Size]::new($width,$height));$capture.Save($Path,[Drawing.Imaging.ImageFormat]::Png);$saved=$true}catch{$saved=$false}finally{$graphics.Dispose();$capture.Dispose()}
    if(-not $saved){$render=[Activator]::CreateInstance([Windows.Media.Imaging.RenderTargetBitmap],[object[]]@($width,$height,96.0,96.0,[Windows.Media.PixelFormats]::Pbgra32));$render.Render($window);$encoder=New-Object Windows.Media.Imaging.PngBitmapEncoder;$encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($render));$stream=[IO.File]::Open($Path,[IO.FileMode]::Create,[IO.FileAccess]::Write);try{$encoder.Save($stream)}finally{$stream.Dispose()}}
    "${width}x${height}"
}
function Write-Pattern([string]$Path){
    $width=640;$height=440;$header=[Text.Encoding]::ASCII.GetBytes("P5`n$width $height`n255`n");$pixels=New-Object byte[] ($width*$height)
    for($row=0;$row -lt $height;$row++){for($column=0;$column -lt $width;$column++){$value=25+[int](45*$column/$width);if($row -gt 55 -and $row -lt 175 -and $column -gt 65 -and $column -lt 285){$value=215};if($row -gt 245 -and $row -lt 395 -and $column -gt 350 -and $column -lt 590){$value=140};if(($row-115)*($row-115)+($column-510)*($column-510)-lt 48*48){$value=250};$pixels[$row*$width+$column]=[byte]$value}}
    $bytes=New-Object byte[] ($header.Length+$pixels.Length);[Array]::Copy($header,0,$bytes,0,$header.Length);[Array]::Copy($pixels,0,$bytes,$header.Length,$pixels.Length);[IO.File]::WriteAllBytes($Path,$bytes)
}

$temp=Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s35-ui-'+[Guid]::NewGuid().ToString('N'));[IO.Directory]::CreateDirectory($temp)|Out-Null
$imagePath=Join-Path $temp 'asymmetric-pattern.pgm';Write-Pattern $imagePath
$app=[Windows.Application]::Current;if($null -eq $app){$app=New-Object Windows.Application;$app.ShutdownMode=[Windows.ShutdownMode]::OnExplicitShutdown}
$main=$assembly.GetType('HalconWinFormsDemo.MainWindow',$true);$serviceType=$assembly.GetType('HalconWinFormsDemo.Services.RecipeService',$true);$loggerType=$assembly.GetType('HalconWinFormsDemo.Services.AppLogger',$true);$kindType=$assembly.GetType('HalconWinFormsDemo.Models.VmToolKind',$true);$roiType=$assembly.GetType('HalconWinFormsDemo.Models.RoiData',$true)
$private=[Reflection.BindingFlags]'Instance,NonPublic';$all=[Reflection.BindingFlags]'Instance,NonPublic,Public';$staticPublic=[Reflection.BindingFlags]'Static,Public';$args=New-Object 'object[]' 1;$args[0]=$temp.ToString();$service=$serviceType.GetConstructor([Type[]]@([string])).Invoke($args);$logger=$loggerType.GetConstructor([Type[]]@([string])).Invoke($args)
$window=$main.GetConstructor($private,$null,[Type[]]@($serviceType,$loggerType),$null).Invoke([object[]]@($service,$logger));$flow=$main.GetField('flowTools',$private).GetValue($window)
$create=$main.GetMethod('CreateFlowTool',$private);$dispose=$main.GetMethod('DisposeFlowTools',$private);$execute=$main.GetMethod('ExecuteFlowTool',$private);$loadImage=$main.GetMethod('LoadImage',$private);$refresh=$main.GetMethod('RefreshUiState',$private);$schedule=$main.GetMethod('ScheduleRefreshDisplay',$private);$addRoi=$main.GetMethod('AddRoiLayer',$private);$selectRoi=$main.GetMethod('SelectRoiLayer',$private);$expand=$main.GetMethod('ImageWorkspaceExpandButton_Click',$private)
function Field([string]$Name){$main.GetField($Name,$all).GetValue($window)}
function New-Tool([string]$Kind,[string]$Name){$create.Invoke($window,[object[]]@([Enum]::Parse($kindType,$Kind),$Name,$true,$null))}
$dispose.Invoke($window,@())|Out-Null;$flow.Clear();$source=New-Tool ImageSource 'Station Image Input';$rotate=New-Tool ImageRotate 'HALCON Image Rotate';$threshold=New-Tool ImageThreshold 'Rotated Output Threshold';$blob=New-Tool Blob 'ROI Binding Inspection'
$source.Parameters.LocalImagePath=$imagePath;$source.Parameters.LocalImageSerialNumber='S35-ROTATE';$rotate.Parameters.ImageRotateAngleDegrees=90;$rotate.Parameters.ImageRotateInterpolation='constant';$rotate.SetInputBinding('Image',$source.ToolId,'Image');$threshold.Parameters.ImageThresholdMinGray=180;$threshold.Parameters.ImageThresholdMaxGray=255;$threshold.SetInputBinding('Image',$rotate.ToolId,'Image');$blob.SetInputBinding('Image',$source.ToolId,'Image');foreach($tool in @($source,$rotate,$threshold,$blob)){$flow.Add($tool)}
$window.WindowStartupLocation=[Windows.WindowStartupLocation]::Manual;$window.WindowState=[Windows.WindowState]::Normal;$window.Left=2;$window.Top=2;$window.Width=1900;$window.Height=1060;$window.Topmost=$true;$window.Show();$window.Activate()|Out-Null;Pump-Ui 850
$loadImage.Invoke($window,[object[]]@($imagePath.ToString(),$false))|Out-Null;foreach($tool in @($source,$rotate,$threshold)){try{$execute.Invoke($window,[object[]]@($tool,'S35 UI capture'))|Out-Null}catch [Reflection.TargetInvocationException]{throw $_.Exception.InnerException}}
$line=$roiType.GetMethod('CreateLine',$staticPublic).Invoke($null,[object[]]@([double]92,[double]85,[double]330,[double]545));$lineLayer=$addRoi.Invoke($window,[object[]]@($line,'Inspection Baseline',$blob));$line.Dispose()
$point=$roiType.GetMethod('CreatePoint',$staticPublic).Invoke($null,[object[]]@([double]115,[double]510));$pointLayer=$addRoi.Invoke($window,[object[]]@($point,'Reference Center',$blob));$point.Dispose()
$list=Field 'FlowToolList';$context=Field 'ImageContextComboBox';$rightTabs=Field 'RightTabs';$rightTabs.SelectedItem=Field 'RoiTab';$roiList=Field 'RoiLayerList';$roiList.SelectedItem=$lineLayer;$selectRoi.Invoke($window,[object[]]@($lineLayer))|Out-Null;$list.SelectedItem=$blob;$context.SelectedValue='GlobalInput';$expand.Invoke($window,[object[]]@($null,$null))|Out-Null;$refresh.Invoke($window,@())|Out-Null;$schedule.Invoke($window,@())|Out-Null;Pump-Ui 1200
$roiSize=Save-Shot $RoiOutputPath
$roiView=(Field 'imageWindow').HalconWindow.DumpWindowImage();try{$roiView.WriteImage('png',0,$RoiHalconOutputPath)}finally{$roiView.Dispose()}
$expand.Invoke($window,[object[]]@($null,$null))|Out-Null;$list.SelectedItem=$rotate;$context.SelectedValue='ModuleOutput';$rightTabs.SelectedItem=Field 'DockConfigurationTab';(Field 'DockConfigurationTabs').SelectedIndex=0;$main.GetMethod('LoadDockConfigurationDraft',$private).Invoke($window,[object[]]@($rotate))|Out-Null;$refresh.Invoke($window,@())|Out-Null;$schedule.Invoke($window,@())|Out-Null;Pump-Ui 900
$panel=Field 'DockImageRotatePanel';if($panel.Visibility.ToString() -ne 'Visible'){throw 'S35 image rotate dock panel is not visible.'};$panel.BringIntoView();Pump-Ui 250
$rotateSize=Save-Shot $RotateOutputPath;$view=(Field 'imageWindow').HalconWindow.DumpWindowImage();try{$view.WriteImage('png',0,$HalconOutputPath)}finally{$view.Dispose()}
$main.GetField('recipeCloseConfirmed',$private).SetValue($window,$true);$window.Close();$app.Shutdown();if(Test-Path $temp){Remove-Item -LiteralPath $temp -Recurse -Force}
Write-Output "S35_UI_CAPTURE=PASS; RoiPath=$RoiOutputPath; RoiHalconOutput=$RoiHalconOutputPath; RotatePath=$RotateOutputPath; HalconOutput=$HalconOutputPath; RoiWindow=$roiSize; RotateWindow=$rotateSize; A=VM_LINE_POINT_ROI; B=HALCON_ROTATE_IMAGE; BSB=NOT_IMPLEMENTED_BY_USER_DIRECTIVE"
