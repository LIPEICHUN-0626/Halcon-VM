param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$RoiOutputPath = (Join-Path ([IO.Path]::GetTempPath()) 'halcon-vm-s31-ellipse-annulus-roi.png'),
    [string]$PolarOutputPath = (Join-Path ([IO.Path]::GetTempPath()) 'halcon-vm-s31-polar-unwrap.png'),
    [string]$RoiImageOutputPath = (Join-Path ([IO.Path]::GetTempPath()) 'halcon-vm-s31-roi-image-view.png'),
    [string]$PolarImageOutputPath = (Join-Path ([IO.Path]::GetTempPath()) 'halcon-vm-s31-polar-image-view.png')
)

$ErrorActionPreference = 'Stop'
if ([Threading.Thread]::CurrentThread.ApartmentState -ne [Threading.ApartmentState]::STA) { throw 'Run with powershell.exe -STA.' }
$halcon = 'C:\Program Files\MVTec\HALCON-20.11-Progress'
$env:PATH = (Join-Path $halcon 'bin\x64-win64') + ';' + $env:PATH
Add-Type -AssemblyName PresentationFramework
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
[Reflection.Assembly]::LoadFrom((Join-Path $halcon 'bin\dotnet35\halcondotnet.dll')) | Out-Null
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path $ProjectRoot 'bin\x64\Debug\HalconWinFormsDemo.exe'))

function Pump-Ui([int]$Milliseconds) {
    $frame = New-Object Windows.Threading.DispatcherFrame
    $timer = New-Object Windows.Threading.DispatcherTimer
    $timer.Interval = [TimeSpan]::FromMilliseconds($Milliseconds)
    $timer.Add_Tick({ $timer.Stop(); $frame.Continue = $false })
    $timer.Start()
    [Windows.Threading.Dispatcher]::PushFrame($frame)
}
function Save-Shot([string]$Path) {
    $window.UpdateLayout()
    $topLeft = $window.PointToScreen([Windows.Point]::new(0,0))
    $bottomRight = $window.PointToScreen([Windows.Point]::new($window.ActualWidth,$window.ActualHeight))
    $width = [Math]::Max(1,[int][Math]::Ceiling($bottomRight.X-$topLeft.X))
    $height = [Math]::Max(1,[int][Math]::Ceiling($bottomRight.Y-$topLeft.Y))
    $capture = [Drawing.Bitmap]::new($width,$height)
    $graphics = [Drawing.Graphics]::FromImage($capture)
    $screenSaved=$false
    try { $graphics.CopyFromScreen([int]$topLeft.X,[int]$topLeft.Y,0,0,[Drawing.Size]::new($width,$height)); $capture.Save($Path,[Drawing.Imaging.ImageFormat]::Png); $screenSaved=$true }
    catch { $screenSaved=$false }
    finally { $graphics.Dispose(); $capture.Dispose() }
    if(-not $screenSaved){
        $width=[Math]::Max(1,[int][Math]::Ceiling($window.ActualWidth)); $height=[Math]::Max(1,[int][Math]::Ceiling($window.ActualHeight))
        $render=[Activator]::CreateInstance([Windows.Media.Imaging.RenderTargetBitmap],[object[]]@($width,$height,96.0,96.0,[Windows.Media.PixelFormats]::Pbgra32))
        $render.Render($window)
        $encoder=New-Object Windows.Media.Imaging.PngBitmapEncoder
        $encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($render))
        $stream=[IO.File]::Open($Path,[IO.FileMode]::Create,[IO.FileAccess]::Write)
        try{$encoder.Save($stream)}finally{$stream.Dispose()}
    }
    "${width}x${height}"
}
function Save-HalconView([string]$Path) {
    $control = $main.GetField('imageWindow',$private).GetValue($window)
    $view = $control.HalconWindow.DumpWindowImage()
    try { $view.WriteImage('png',0,$Path) } finally { $view.Dispose() }
}
function Write-PolarPattern([string]$Path) {
    $width = 640; $height = 440; $centerRow = 220; $centerColumn = 330
    $header = [Text.Encoding]::ASCII.GetBytes("P5`n$width $height`n255`n")
    $pixels = New-Object byte[] ($width*$height)
    for ($row=0; $row -lt $height; $row++) {
        for ($column=0; $column -lt $width; $column++) {
            $dr=$row-$centerRow; $dc=$column-$centerColumn
            $radius=[Math]::Sqrt($dr*$dr+$dc*$dc)
            $angle=[Math]::Atan2(-$dr,$dc)*180/[Math]::PI
            if ($angle -lt 0) { $angle += 360 }
            $sector=[int]($angle/30)
            $value=30+[int](25*$column/$width)
            if ($radius -ge 60 -and $radius -le 165) {
                $value = if (($sector % 2) -eq 0) { 205 } else { 90 }
                if (([int]$radius % 32) -lt 5) { $value=245 }
            }
            $pixels[$row*$width+$column]=[byte]$value
        }
    }
    $bytes=New-Object byte[] ($header.Length+$pixels.Length)
    [Array]::Copy($header,0,$bytes,0,$header.Length)
    [Array]::Copy($pixels,0,$bytes,$header.Length,$pixels.Length)
    [IO.File]::WriteAllBytes($Path,$bytes)
}

$temp=Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s31-ui-'+[Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($temp)|Out-Null
$imagePath=Join-Path $temp 'polar-pattern.pgm'
Write-PolarPattern $imagePath
$app=[Windows.Application]::Current
if ($null -eq $app) { $app=New-Object Windows.Application; $app.ShutdownMode=[Windows.ShutdownMode]::OnExplicitShutdown }

$main=$assembly.GetType('HalconWinFormsDemo.MainWindow',$true)
$serviceType=$assembly.GetType('HalconWinFormsDemo.Services.RecipeService',$true)
$loggerType=$assembly.GetType('HalconWinFormsDemo.Services.AppLogger',$true)
$kindType=$assembly.GetType('HalconWinFormsDemo.Models.VmToolKind',$true)
$roiType=$assembly.GetType('HalconWinFormsDemo.Models.RoiData',$true)
$private=[Reflection.BindingFlags]'Instance,NonPublic'
$all=[Reflection.BindingFlags]'Instance,NonPublic,Public'
$args=New-Object 'object[]' 1; $args[0]=$temp.ToString()
$service=$serviceType.GetConstructor([Type[]]@([string])).Invoke($args)
$logger=$loggerType.GetConstructor([Type[]]@([string])).Invoke($args)
$window=$main.GetConstructor($private,$null,[Type[]]@($serviceType,$loggerType),$null).Invoke([object[]]@($service,$logger))
$flow=$main.GetField('flowTools',$private).GetValue($window)
$create=$main.GetMethod('CreateFlowTool',$private)
$dispose=$main.GetMethod('DisposeFlowTools',$private)
$execute=$main.GetMethod('ExecuteFlowTool',$private)
$loadImage=$main.GetMethod('LoadImage',$private)
$addRoi=$main.GetMethod('AddRoiLayer',$private)
$schedule=$main.GetMethod('ScheduleRefreshDisplay',$private)
$refresh=$main.GetMethod('RefreshUiState',$private)
function New-Tool([string]$Kind,[string]$Name) { $create.Invoke($window,[object[]]@([Enum]::Parse($kindType,$Kind),$Name,$true,$null)) }

$dispose.Invoke($window,@())|Out-Null
$flow.Clear()
$source=New-Tool ImageSource 'Station Image Input'
$polar=New-Tool PolarImageUnwrap 'HALCON Polar Image Unwrap'
$threshold=New-Tool ImageThreshold 'Unwrapped Surface Threshold'
$source.Parameters.LocalImagePath=$imagePath
$source.Parameters.LocalImageSerialNumber='S31-POLAR'
$p=$polar.Parameters
$p.PolarCenterRow=220; $p.PolarCenterColumn=330; $p.PolarStartAngleDegrees=0; $p.PolarEndAngleDegrees=360
$p.PolarInnerRadius=60; $p.PolarOuterRadius=165; $p.PolarOutputWidth=720; $p.PolarOutputHeight=160; $p.PolarInterpolation='bilinear'
$polar.SetInputBinding('Image',$source.ToolId,'Image')
$threshold.Parameters.ImageThresholdMinGray=170; $threshold.Parameters.ImageThresholdMaxGray=255
$threshold.SetInputBinding('Image',$polar.ToolId,'Image')
foreach($tool in @($source,$polar,$threshold)){ $flow.Add($tool) }

$window.WindowStartupLocation=[Windows.WindowStartupLocation]::Manual
$window.WindowState=[Windows.WindowState]::Normal
$window.Left=2; $window.Top=2; $window.Width=1900; $window.Height=1060; $window.Topmost=$true
$window.Show(); $window.Activate()|Out-Null; Pump-Ui 900
$loadImage.Invoke($window,[object[]]@($imagePath.ToString(),$false))|Out-Null
foreach($tool in @($source,$polar,$threshold)) {
    try { $execute.Invoke($window,[object[]]@($tool,'S31 UI capture'))|Out-Null }
    catch [Reflection.TargetInvocationException] { throw $_.Exception.InnerException }
}

$ellipse=$roiType.GetMethod('CreateEllipse').Invoke($null,[object[]]@([double]155,[double]210,[double].35,[double]105,[double]58))
$annulus=$roiType.GetMethod('CreateAnnulus').Invoke($null,[object[]]@([double]255,[double]445,[double]48,[double]98))
$ellipseLayer=$addRoi.Invoke($window,[object[]]@($ellipse,'Inspection Ellipse',$null))
$annulusLayer=$addRoi.Invoke($window,[object[]]@($annulus,'Seal Annulus',$null))
$ellipse.Dispose(); $annulus.Dispose()

$list=$main.GetField('FlowToolList',$all).GetValue($window)
$roiList=$main.GetField('RoiLayerList',$all).GetValue($window)
$context=$main.GetField('ImageContextComboBox',$all).GetValue($window)
$rightTabs=$main.GetField('RightTabs',$all).GetValue($window)
$rightTabs.SelectedItem=$main.GetField('RoiTab',$all).GetValue($window)
$main.GetField('ImageLayerTabs',$all).GetValue($window).SelectedIndex=0
$context.SelectedValue='GlobalInput'
$roiList.SelectedItem=$annulusLayer
$main.GetField('RoiLayerPanel',$all).GetValue($window).Visibility=[Windows.Visibility]::Visible
$main.GetField('RoiLayerPanelColumn',$all).GetValue($window).Width=[Windows.GridLength]::new(220)
$refresh.Invoke($window,@())|Out-Null; $schedule.Invoke($window,@())|Out-Null; Pump-Ui 800
$roiSize=Save-Shot $RoiOutputPath
Save-HalconView $RoiImageOutputPath

$list.SelectedItem=$polar
$rightTabs.SelectedItem=$main.GetField('DockConfigurationTab',$all).GetValue($window)
$main.GetField('DockConfigurationTabs',$all).GetValue($window).SelectedIndex=0
$context.SelectedValue='ModuleOutput'
$refresh.Invoke($window,@())|Out-Null; $schedule.Invoke($window,@())|Out-Null; Pump-Ui 900
$panel=$main.GetField('DockPolarImageUnwrapPanel',$all).GetValue($window)
if ($panel.Visibility.ToString() -ne 'Visible') { throw 'S31 polar dock panel is not visible.' }
$panel.BringIntoView(); Pump-Ui 250
$polarSize=Save-Shot $PolarOutputPath
Save-HalconView $PolarImageOutputPath

$imageSnapshot=$null
if (-not $polar.TryGetOutputValue('Image',[ref]$imageSnapshot) -or $null -eq $imageSnapshot) { throw 'S31 polar output image is missing.' }
$angleRange=$null; $polar.TryGetOutputValue('AngleRange',[ref]$angleRange)|Out-Null
$main.GetField('recipeCloseConfirmed',$private).SetValue($window,$true)
$window.Close(); $app.Shutdown()
if(Test-Path $temp){Remove-Item -LiteralPath $temp -Recurse -Force}
Write-Output "S31_UI_CAPTURE=PASS; RoiPath=$RoiOutputPath; PolarPath=$PolarOutputPath; RoiImage=$RoiImageOutputPath; PolarImage=$PolarImageOutputPath; RoiWindow=$roiSize; PolarWindow=$polarSize; A=ELLIPSE_ANNULUS_IMAGE_ROI; B=HALCON_POLAR_UNWRAP; AngleRange=$angleRange"
