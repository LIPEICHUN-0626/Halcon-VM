param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$RoiOutputPath = (Join-Path ([IO.Path]::GetTempPath()) 'halcon-vm-s32-roi-geometry.png'),
    [string]$ExtractionOutputPath = (Join-Path ([IO.Path]::GetTempPath()) 'halcon-vm-s32-roi-extraction.png'),
    [string]$RoiImageOutputPath = (Join-Path ([IO.Path]::GetTempPath()) 'halcon-vm-s32-roi-view.png'),
    [string]$ExtractionImageOutputPath = (Join-Path ([IO.Path]::GetTempPath()) 'halcon-vm-s32-extraction-view.png')
)

$ErrorActionPreference='Stop'
if([Threading.Thread]::CurrentThread.ApartmentState -ne [Threading.ApartmentState]::STA){throw 'Run with powershell.exe -STA.'}
$halcon='C:\Program Files\MVTec\HALCON-20.11-Progress'; $env:PATH=(Join-Path $halcon 'bin\x64-win64')+';'+$env:PATH
Add-Type -AssemblyName PresentationFramework; Add-Type -AssemblyName System.Drawing; Add-Type -AssemblyName System.Windows.Forms
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
function Save-HalconView([string]$Path){$control=$main.GetField('imageWindow',$private).GetValue($window);$view=$control.HalconWindow.DumpWindowImage();try{$view.WriteImage('png',0,$Path)}finally{$view.Dispose()}}
function Write-Pattern([string]$Path){
    $width=640;$height=440;$header=[Text.Encoding]::ASCII.GetBytes("P5`n$width $height`n255`n");$pixels=New-Object byte[] ($width*$height)
    for($row=0;$row -lt $height;$row++){for($column=0;$column -lt $width;$column++){$value=25+[int](80*$column/$width);if($row -ge 85 -and $row -le 340 -and $column -ge 120 -and $column -le 510){$value=170;if((($row+$column)%55)-lt 8){$value=235}};$pixels[$row*$width+$column]=[byte]$value}}
    $bytes=New-Object byte[] ($header.Length+$pixels.Length);[Array]::Copy($header,0,$bytes,0,$header.Length);[Array]::Copy($pixels,0,$bytes,$header.Length,$pixels.Length);[IO.File]::WriteAllBytes($Path,$bytes)
}

$temp=Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s32-ui-'+[Guid]::NewGuid().ToString('N'));[IO.Directory]::CreateDirectory($temp)|Out-Null
$imagePath=Join-Path $temp 'roi-pattern.pgm';Write-Pattern $imagePath
$app=[Windows.Application]::Current;if($null -eq $app){$app=New-Object Windows.Application;$app.ShutdownMode=[Windows.ShutdownMode]::OnExplicitShutdown}
$main=$assembly.GetType('HalconWinFormsDemo.MainWindow',$true);$serviceType=$assembly.GetType('HalconWinFormsDemo.Services.RecipeService',$true);$loggerType=$assembly.GetType('HalconWinFormsDemo.Services.AppLogger',$true)
$kindType=$assembly.GetType('HalconWinFormsDemo.Models.VmToolKind',$true);$roiType=$assembly.GetType('HalconWinFormsDemo.Models.RoiData',$true);$private=[Reflection.BindingFlags]'Instance,NonPublic';$all=[Reflection.BindingFlags]'Instance,NonPublic,Public'
$args=New-Object 'object[]' 1;$args[0]=$temp.ToString();$service=$serviceType.GetConstructor([Type[]]@([string])).Invoke($args);$logger=$loggerType.GetConstructor([Type[]]@([string])).Invoke($args)
$window=$main.GetConstructor($private,$null,[Type[]]@($serviceType,$loggerType),$null).Invoke([object[]]@($service,$logger));$flow=$main.GetField('flowTools',$private).GetValue($window)
$create=$main.GetMethod('CreateFlowTool',$private);$dispose=$main.GetMethod('DisposeFlowTools',$private);$execute=$main.GetMethod('ExecuteFlowTool',$private);$loadImage=$main.GetMethod('LoadImage',$private);$addRoi=$main.GetMethod('AddRoiLayer',$private);$refresh=$main.GetMethod('RefreshUiState',$private);$schedule=$main.GetMethod('ScheduleRefreshDisplay',$private)
function New-Tool([string]$Kind,[string]$Name){$create.Invoke($window,[object[]]@([Enum]::Parse($kindType,$Kind),$Name,$true,$null))}
$dispose.Invoke($window,@())|Out-Null;$flow.Clear();$source=New-Tool ImageSource 'Station Image Input';$extract=New-Tool RoiImageExtraction 'HALCON ROI Image Extraction';$threshold=New-Tool ImageThreshold 'Extracted Image Threshold'
$source.Parameters.LocalImagePath=$imagePath;$source.Parameters.LocalImageSerialNumber='S32-ROI';$extract.Parameters.RoiImageExtractionMode='CropDomain';$extract.SetInputBinding('Image',$source.ToolId,'Image');$threshold.Parameters.ImageThresholdMinGray=150;$threshold.Parameters.ImageThresholdMaxGray=255;$threshold.SetInputBinding('Image',$extract.ToolId,'Image');foreach($tool in @($source,$extract,$threshold)){$flow.Add($tool)}
$window.WindowStartupLocation=[Windows.WindowStartupLocation]::Manual;$window.WindowState=[Windows.WindowState]::Normal;$window.Left=2;$window.Top=2;$window.Width=1900;$window.Height=1060;$window.Topmost=$true;$window.Show();$window.Activate()|Out-Null;Pump-Ui 900
$loadImage.Invoke($window,[object[]]@($imagePath.ToString(),$false))|Out-Null
$rectangle=$roiType.GetMethod('CreateRotatedRectangle').Invoke($null,[object[]]@([double]215,[double]315,[double].12,[double]185,[double]115));$layer=$addRoi.Invoke($window,[object[]]@($rectangle,'Inspection Work Area',$extract));$rectangle.Dispose();$extract.BindRoi($layer.RoiId)
foreach($tool in @($source,$extract,$threshold)){try{$execute.Invoke($window,[object[]]@($tool,'S32 UI capture'))|Out-Null}catch [Reflection.TargetInvocationException]{throw $_.Exception.InnerException}}
$list=$main.GetField('FlowToolList',$all).GetValue($window);$roiList=$main.GetField('RoiLayerList',$all).GetValue($window);$context=$main.GetField('ImageContextComboBox',$all).GetValue($window);$rightTabs=$main.GetField('RightTabs',$all).GetValue($window)
$rightTabs.SelectedItem=$main.GetField('RoiTab',$all).GetValue($window);$context.SelectedValue='GlobalInput';$roiList.SelectedItem=$layer;$main.GetField('RoiLayerPanel',$all).GetValue($window).Visibility=[Windows.Visibility]::Visible;$main.GetField('RoiLayerPanelColumn',$all).GetValue($window).Width=[Windows.GridLength]::new(220);$refresh.Invoke($window,@())|Out-Null;$schedule.Invoke($window,@())|Out-Null;Pump-Ui 500;$main.GetField('RoiOrientedGeometryPanel',$all).GetValue($window).BringIntoView();Pump-Ui 400
$roiSize=Save-Shot $RoiOutputPath;Save-HalconView $RoiImageOutputPath
$list.SelectedItem=$extract;$rightTabs.SelectedItem=$main.GetField('DockConfigurationTab',$all).GetValue($window);$main.GetField('DockConfigurationTabs',$all).GetValue($window).SelectedIndex=0;$context.SelectedValue='ModuleOutput';$refresh.Invoke($window,@())|Out-Null;$schedule.Invoke($window,@())|Out-Null;Pump-Ui 900
$panel=$main.GetField('DockRoiImageExtractionPanel',$all).GetValue($window);if($panel.Visibility.ToString() -ne 'Visible'){throw 'S32 ROI extraction dock panel is not visible.'};$panel.BringIntoView();Pump-Ui 250
$extractSize=Save-Shot $ExtractionOutputPath;Save-HalconView $ExtractionImageOutputPath
$offsetRow=$null;$offsetColumn=$null;$extract.TryGetOutputValue('OffsetRow',[ref]$offsetRow)|Out-Null;$extract.TryGetOutputValue('OffsetColumn',[ref]$offsetColumn)|Out-Null
$main.GetField('recipeCloseConfirmed',$private).SetValue($window,$true);$window.Close();$app.Shutdown();if(Test-Path $temp){Remove-Item -LiteralPath $temp -Recurse -Force}
Write-Output "S32_UI_CAPTURE=PASS; RoiPath=$RoiOutputPath; ExtractionPath=$ExtractionOutputPath; RoiImage=$RoiImageOutputPath; ExtractionImage=$ExtractionImageOutputPath; RoiWindow=$roiSize; ExtractionWindow=$extractSize; A=VM_ROI_NUMERIC_GEOMETRY; B=HALCON_ROI_IMAGE_EXTRACTION; Offset=($offsetRow,$offsetColumn)"
