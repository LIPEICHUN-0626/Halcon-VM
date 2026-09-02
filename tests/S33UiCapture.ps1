param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$DualOutputPath = (Join-Path ([IO.Path]::GetTempPath()) 'halcon-vm-s33-dual-image.png'),
    [string]$ResizeOutputPath = (Join-Path ([IO.Path]::GetTempPath()) 'halcon-vm-s33-image-resize.png'),
    [string]$LeftImageOutputPath = (Join-Path ([IO.Path]::GetTempPath()) 'halcon-vm-s33-input-view.png'),
    [string]$RightImageOutputPath = (Join-Path ([IO.Path]::GetTempPath()) 'halcon-vm-s33-output-view.png')
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
function Save-HalconView([string]$Field,[string]$Path){$control=$main.GetField($Field,$private).GetValue($window);$view=$control.HalconWindow.DumpWindowImage();try{$view.WriteImage('png',0,$Path)}finally{$view.Dispose()}}
function Write-Pattern([string]$Path){
    $width=640;$height=440;$header=[Text.Encoding]::ASCII.GetBytes("P5`n$width $height`n255`n");$pixels=New-Object byte[] ($width*$height)
    for($row=0;$row -lt $height;$row++){for($column=0;$column -lt $width;$column++){$value=20+[int](75*$column/$width);if($row -ge 90 -and $row -le 350 -and $column -ge 100 -and $column -le 530){$value=155;if((($row*2+$column)%72)-lt 12){$value=235}};if(($row-220)*($row-220)+($column-320)*($column-320)-lt 75*75){$value=245};$pixels[$row*$width+$column]=[byte]$value}}
    $bytes=New-Object byte[] ($header.Length+$pixels.Length);[Array]::Copy($header,0,$bytes,0,$header.Length);[Array]::Copy($pixels,0,$bytes,$header.Length,$pixels.Length);[IO.File]::WriteAllBytes($Path,$bytes)
}

$temp=Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s33-ui-'+[Guid]::NewGuid().ToString('N'));[IO.Directory]::CreateDirectory($temp)|Out-Null
$imagePath=Join-Path $temp 'resize-pattern.pgm';Write-Pattern $imagePath
$app=[Windows.Application]::Current;if($null -eq $app){$app=New-Object Windows.Application;$app.ShutdownMode=[Windows.ShutdownMode]::OnExplicitShutdown}
$main=$assembly.GetType('HalconWinFormsDemo.MainWindow',$true);$serviceType=$assembly.GetType('HalconWinFormsDemo.Services.RecipeService',$true);$loggerType=$assembly.GetType('HalconWinFormsDemo.Services.AppLogger',$true)
$kindType=$assembly.GetType('HalconWinFormsDemo.Models.VmToolKind',$true);$roiType=$assembly.GetType('HalconWinFormsDemo.Models.RoiData',$true);$private=[Reflection.BindingFlags]'Instance,NonPublic';$all=[Reflection.BindingFlags]'Instance,NonPublic,Public'
$args=New-Object 'object[]' 1;$args[0]=$temp.ToString();$service=$serviceType.GetConstructor([Type[]]@([string])).Invoke($args);$logger=$loggerType.GetConstructor([Type[]]@([string])).Invoke($args)
$window=$main.GetConstructor($private,$null,[Type[]]@($serviceType,$loggerType),$null).Invoke([object[]]@($service,$logger));$flow=$main.GetField('flowTools',$private).GetValue($window)
$create=$main.GetMethod('CreateFlowTool',$private);$dispose=$main.GetMethod('DisposeFlowTools',$private);$execute=$main.GetMethod('ExecuteFlowTool',$private);$loadImage=$main.GetMethod('LoadImage',$private);$addRoi=$main.GetMethod('AddRoiLayer',$private);$refresh=$main.GetMethod('RefreshUiState',$private);$schedule=$main.GetMethod('ScheduleRefreshDisplay',$private);$setDual=$main.GetMethod('SetDualImageMode',$private);$expand=$main.GetMethod('ImageWorkspaceExpandButton_Click',$private)
function New-Tool([string]$Kind,[string]$Name){$create.Invoke($window,[object[]]@([Enum]::Parse($kindType,$Kind),$Name,$true,$null))}
$dispose.Invoke($window,@())|Out-Null;$flow.Clear();$source=New-Tool ImageSource 'Station Image Input';$resize=New-Tool ImageResize 'HALCON Image Resize';$threshold=New-Tool ImageThreshold 'Resized Image Threshold'
$source.Parameters.LocalImagePath=$imagePath;$source.Parameters.LocalImageSerialNumber='S33-RESIZE';$resize.Parameters.ImageResizeWidth=320;$resize.Parameters.ImageResizeHeight=220;$resize.Parameters.ImageResizeInterpolation='bicubic';$resize.SetInputBinding('Image',$source.ToolId,'Image');$threshold.Parameters.ImageThresholdMinGray=200;$threshold.Parameters.ImageThresholdMaxGray=255;$threshold.SetInputBinding('Image',$resize.ToolId,'Image');foreach($tool in @($source,$resize,$threshold)){$flow.Add($tool)}
$window.WindowStartupLocation=[Windows.WindowStartupLocation]::Manual;$window.WindowState=[Windows.WindowState]::Normal;$window.Left=2;$window.Top=2;$window.Width=1900;$window.Height=1060;$window.Topmost=$true;$window.Show();$window.Activate()|Out-Null;Pump-Ui 900
$loadImage.Invoke($window,[object[]]@($imagePath.ToString(),$false))|Out-Null
$rectangle=$roiType.GetMethod('CreateRotatedRectangle').Invoke($null,[object[]]@([double]220,[double]320,[double].08,[double]205,[double]130));$layer=$addRoi.Invoke($window,[object[]]@($rectangle,'Input Inspection ROI',$null));$rectangle.Dispose()
foreach($tool in @($source,$resize,$threshold)){try{$execute.Invoke($window,[object[]]@($tool,'S33 UI capture'))|Out-Null}catch [Reflection.TargetInvocationException]{throw $_.Exception.InnerException}}
$list=$main.GetField('FlowToolList',$all).GetValue($window);$context=$main.GetField('ImageContextComboBox',$all).GetValue($window);$list.SelectedItem=$resize;$setDual.Invoke($window,[object[]]@($true))|Out-Null;$context.SelectedValue='ModuleOutput'
$main.GetField('RoiLayerPanel',$all).GetValue($window).Visibility=[Windows.Visibility]::Collapsed;$main.GetField('RoiLayerPanelColumn',$all).GetValue($window).Width=[Windows.GridLength]::new(0)
$expand.Invoke($window,[object[]]@($null,$null))|Out-Null;$refresh.Invoke($window,@())|Out-Null;$schedule.Invoke($window,@())|Out-Null;Pump-Ui 1200
$dualSize=Save-Shot $DualOutputPath;Save-HalconView 'comparisonImageWindow' $LeftImageOutputPath;Save-HalconView 'imageWindow' $RightImageOutputPath
$expand.Invoke($window,[object[]]@($null,$null))|Out-Null;$rightTabs=$main.GetField('RightTabs',$all).GetValue($window);$rightTabs.SelectedItem=$main.GetField('DockConfigurationTab',$all).GetValue($window);$main.GetField('DockConfigurationTabs',$all).GetValue($window).SelectedIndex=0;$refresh.Invoke($window,@())|Out-Null;$schedule.Invoke($window,@())|Out-Null;Pump-Ui 900
$panel=$main.GetField('DockImageResizePanel',$all).GetValue($window);if($panel.Visibility.ToString() -ne 'Visible'){throw 'S33 image resize dock panel is not visible.'};$panel.BringIntoView();Pump-Ui 300
$resizeSize=Save-Shot $ResizeOutputPath;$scaleRow=$null;$scaleColumn=$null;$resize.TryGetOutputValue('ScaleRow',[ref]$scaleRow)|Out-Null;$resize.TryGetOutputValue('ScaleColumn',[ref]$scaleColumn)|Out-Null
$main.GetField('recipeCloseConfirmed',$private).SetValue($window,$true);$window.Close();$app.Shutdown();if(Test-Path $temp){Remove-Item -LiteralPath $temp -Recurse -Force}
Write-Output "S33_UI_CAPTURE=PASS; DualPath=$DualOutputPath; ResizePath=$ResizeOutputPath; LeftImage=$LeftImageOutputPath; RightImage=$RightImageOutputPath; DualWindow=$dualSize; ResizeWindow=$resizeSize; A=VM_DUAL_INPUT_OUTPUT_VIEW; B=HALCON_ZOOM_IMAGE_SIZE; Scale=($scaleRow,$scaleColumn)"
