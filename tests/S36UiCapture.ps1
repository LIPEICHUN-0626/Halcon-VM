param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$MaskOutputPath = (Join-Path ([IO.Path]::GetTempPath()) 'halcon-vm-s36-roi-mask.png'),
    [string]$MaskHalconOutputPath = (Join-Path ([IO.Path]::GetTempPath()) 'halcon-vm-s36-roi-mask-view.png'),
    [string]$AffineOutputPath = (Join-Path ([IO.Path]::GetTempPath()) 'halcon-vm-s36-image-affine.png'),
    [string]$AffineHalconOutputPath = (Join-Path ([IO.Path]::GetTempPath()) 'halcon-vm-s36-affine-view.png')
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
    for($row=0;$row -lt $height;$row++){for($column=0;$column -lt $width;$column++){$value=28+[int](50*$column/$width);if($row-gt 55-and$row-lt 185-and$column-gt 65-and$column-lt 300){$value=220};if($row-gt 245-and$row-lt 400-and$column-gt 345-and$column-lt 595){$value=145};if(($row-118)*($row-118)+($column-510)*($column-510)-lt 50*50){$value=252};$pixels[$row*$width+$column]=[byte]$value}}
    $bytes=New-Object byte[] ($header.Length+$pixels.Length);[Array]::Copy($header,0,$bytes,0,$header.Length);[Array]::Copy($pixels,0,$bytes,$header.Length,$pixels.Length);[IO.File]::WriteAllBytes($Path,$bytes)
}

$temp=Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s36-ui-'+[Guid]::NewGuid().ToString('N'));[IO.Directory]::CreateDirectory($temp)|Out-Null
$imagePath=Join-Path $temp 'pattern.pgm';Write-Pattern $imagePath
$app=[Windows.Application]::Current;if($null-eq$app){$app=New-Object Windows.Application;$app.ShutdownMode=[Windows.ShutdownMode]::OnExplicitShutdown}
$main=$assembly.GetType('HalconWinFormsDemo.MainWindow',$true);$serviceType=$assembly.GetType('HalconWinFormsDemo.Services.RecipeService',$true);$loggerType=$assembly.GetType('HalconWinFormsDemo.Services.AppLogger',$true);$kindType=$assembly.GetType('HalconWinFormsDemo.Models.VmToolKind',$true);$roiType=$assembly.GetType('HalconWinFormsDemo.Models.RoiData',$true)
$private=[Reflection.BindingFlags]'Instance,NonPublic';$all=[Reflection.BindingFlags]'Instance,NonPublic,Public';$staticPublic=[Reflection.BindingFlags]'Static,Public';$args=New-Object 'object[]' 1;$args[0]=$temp.ToString();$service=$serviceType.GetConstructor([Type[]]@([string])).Invoke($args);$logger=$loggerType.GetConstructor([Type[]]@([string])).Invoke($args)
$window=$main.GetConstructor($private,$null,[Type[]]@($serviceType,$loggerType),$null).Invoke([object[]]@($service,$logger));$flow=$main.GetField('flowTools',$private).GetValue($window)
$create=$main.GetMethod('CreateFlowTool',$private);$dispose=$main.GetMethod('DisposeFlowTools',$private);$execute=$main.GetMethod('ExecuteFlowTool',$private);$loadImage=$main.GetMethod('LoadImage',$private);$refresh=$main.GetMethod('RefreshUiState',$private);$schedule=$main.GetMethod('ScheduleRefreshDisplay',$private);$addRoi=$main.GetMethod('AddRoiLayer',$private);$selectRoi=$main.GetMethod('SelectRoiLayer',$private);$toggleRole=$main.GetMethod('ToggleSelectedRoiRoleButton_Click',$private);$expand=$main.GetMethod('ImageWorkspaceExpandButton_Click',$private)
function Field([string]$Name){$main.GetField($Name,$all).GetValue($window)}
function New-Tool([string]$Kind,[string]$Name){$create.Invoke($window,[object[]]@([Enum]::Parse($kindType,$Kind),$Name,$true,$null))}
$dispose.Invoke($window,@())|Out-Null;$flow.Clear();$source=New-Tool ImageSource 'Station Image Input';$affine=New-Tool ImageAffineTransform 'HALCON Affine Transform';$threshold=New-Tool ImageThreshold 'Affine Output Threshold';$blob=New-Tool Blob 'Masked Defect Inspection'
$source.Parameters.LocalImagePath=$imagePath;$source.Parameters.LocalImageSerialNumber='S36-AFFINE';$affine.Parameters.ImageAffineTranslateRow=30;$affine.Parameters.ImageAffineTranslateColumn=45;$affine.Parameters.ImageAffineScaleRow=.86;$affine.Parameters.ImageAffineScaleColumn=1.08;$affine.Parameters.ImageAffineRotationDegrees=12;$affine.Parameters.ImageAffineSlantDegrees=7;$affine.Parameters.ImageAffineInterpolation='bilinear';$affine.Parameters.ImageAffineAdaptImageSize=$false;$affine.SetInputBinding('Image',$source.ToolId,'Image');$threshold.Parameters.ImageThresholdMinGray=180;$threshold.Parameters.ImageThresholdMaxGray=255;$threshold.SetInputBinding('Image',$affine.ToolId,'Image');$blob.SetInputBinding('Image',$source.ToolId,'Image');foreach($tool in @($source,$affine,$threshold,$blob)){$flow.Add($tool)}
$window.WindowStartupLocation=[Windows.WindowStartupLocation]::Manual;$window.WindowState=[Windows.WindowState]::Normal;$window.Left=2;$window.Top=2;$window.Width=1900;$window.Height=1060;$window.Topmost=$true;$window.Show();$window.Activate()|Out-Null;Pump-Ui 800
$loadImage.Invoke($window,[object[]]@($imagePath.ToString(),$false))|Out-Null;foreach($tool in @($source,$affine,$threshold)){try{$execute.Invoke($window,[object[]]@($tool,'S36 UI capture'))|Out-Null}catch [Reflection.TargetInvocationException]{throw $_.Exception.InnerException}}
$detectGeometry=$roiType.GetMethod('CreateRectangle',$staticPublic).Invoke($null,[object[]]@([double]38,[double]42,[double]410,[double]610));$detection=$addRoi.Invoke($window,[object[]]@($detectGeometry,'Inspection Area',$blob));$detectGeometry.Dispose()
$maskGeometry=$roiType.GetMethod('CreateCircle',$staticPublic).Invoke($null,[object[]]@([double]118,[double]510,[double]72));$mask=$addRoi.Invoke($window,[object[]]@($maskGeometry,'Ignore Fixture Hole',$blob));$maskGeometry.Dispose();$roiList=Field 'RoiLayerList';$roiList.SelectedItem=$mask;$selectRoi.Invoke($window,[object[]]@($mask))|Out-Null;$toggleRole.Invoke($window,[object[]]@($null,$null))|Out-Null
$list=Field 'FlowToolList';$context=Field 'ImageContextComboBox';$rightTabs=Field 'RightTabs';$rightTabs.SelectedItem=Field 'RoiTab';(Field 'ImageLayerTabs').SelectedIndex=0;$list.SelectedItem=$blob;$context.SelectedValue='GlobalInput';$expand.Invoke($window,[object[]]@($null,$null))|Out-Null;$refresh.Invoke($window,@())|Out-Null;$schedule.Invoke($window,@())|Out-Null;Pump-Ui 1100
$maskSize=Save-Shot $MaskOutputPath;$maskView=(Field 'imageWindow').HalconWindow.DumpWindowImage();try{$maskView.WriteImage('png',0,$MaskHalconOutputPath)}finally{$maskView.Dispose()}
$expand.Invoke($window,[object[]]@($null,$null))|Out-Null;$list.SelectedItem=$affine;$context.SelectedValue='ModuleOutput';$rightTabs.SelectedItem=Field 'DockConfigurationTab';(Field 'DockConfigurationTabs').SelectedIndex=0;$main.GetMethod('LoadDockConfigurationDraft',$private).Invoke($window,[object[]]@($affine))|Out-Null;$refresh.Invoke($window,@())|Out-Null;$schedule.Invoke($window,@())|Out-Null;Pump-Ui 900
$panel=Field 'DockImageAffineTransformPanel';if($panel.Visibility.ToString()-ne'Visible'){throw 'S36 affine dock panel is not visible.'};$panel.BringIntoView();Pump-Ui 250
$affineSize=Save-Shot $AffineOutputPath;$affineView=(Field 'imageWindow').HalconWindow.DumpWindowImage();try{$affineView.WriteImage('png',0,$AffineHalconOutputPath)}finally{$affineView.Dispose()}
$main.GetField('recipeCloseConfirmed',$private).SetValue($window,$true);$window.Close();$app.Shutdown();if(Test-Path $temp){Remove-Item -LiteralPath $temp -Recurse -Force}
Write-Output "S36_UI_CAPTURE=PASS; MaskPath=$MaskOutputPath; MaskHalconOutput=$MaskHalconOutputPath; AffinePath=$AffineOutputPath; AffineHalconOutput=$AffineHalconOutputPath; MaskWindow=$maskSize; AffineWindow=$affineSize; A=VM_ROI_MASK; B=HALCON_AFFINE_IMAGE"
