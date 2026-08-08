param([string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot))

$ErrorActionPreference = 'Stop'
if ([Threading.Thread]::CurrentThread.ApartmentState -ne [Threading.ApartmentState]::STA) { throw 'Run with powershell.exe -STA.' }
$halconRoot = 'C:\Program Files\MVTec\HALCON-20.11-Progress'
$env:PATH = (Join-Path $halconRoot 'bin\x64-win64') + ';' + $env:PATH
Add-Type -AssemblyName PresentationFramework
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
[Reflection.Assembly]::LoadFrom((Join-Path $halconRoot 'bin\dotnet35\halcondotnet.dll')) | Out-Null
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path $ProjectRoot 'bin\x64\Debug\HalconWinFormsDemo.exe'))

function Assert-True([bool]$Condition, [string]$Message) { if (-not $Condition) { throw "Assertion failed: $Message" } }
function Pump-Ui([int]$Milliseconds) {
    $frame = New-Object Windows.Threading.DispatcherFrame
    $timer = New-Object Windows.Threading.DispatcherTimer
    $timer.Interval = [TimeSpan]::FromMilliseconds($Milliseconds)
    $timer.Add_Tick({ $timer.Stop(); $frame.Continue = $false })
    $timer.Start(); [Windows.Threading.Dispatcher]::PushFrame($frame)
}
function Write-GradientPgm([string]$Path, [int]$Width, [int]$Height) {
    $header = [Text.Encoding]::ASCII.GetBytes("P5`n$Width $Height`n255`n")
    $pixels = New-Object byte[] ($Width * $Height)
    for ($row = 0; $row -lt $Height; $row++) {
        for ($column = 0; $column -lt $Width; $column++) { $pixels[$row * $Width + $column] = [byte](($row + $column) % 256) }
    }
    $bytes = New-Object byte[] ($header.Length + $pixels.Length)
    [Array]::Copy($header,0,$bytes,0,$header.Length); [Array]::Copy($pixels,0,$bytes,$header.Length,$pixels.Length)
    [IO.File]::WriteAllBytes($Path,$bytes)
}
function Write-MarkerPgm([string]$Path) {
    $header = [Text.Encoding]::ASCII.GetBytes("P5`n20 20`n255`n")
    $pixels = New-Object byte[] 400; $pixels[5 * 20 + 7] = 255
    $bytes = New-Object byte[] ($header.Length + $pixels.Length)
    [Array]::Copy($header,0,$bytes,0,$header.Length); [Array]::Copy($pixels,0,$bytes,$header.Length,$pixels.Length)
    [IO.File]::WriteAllBytes($Path,$bytes)
}
function Write-TestPpm([string]$Path, [int]$Width, [int]$Height) {
    $header = [Text.Encoding]::ASCII.GetBytes("P6`n$Width $Height`n255`n")
    $pixels = New-Object byte[] ($Width * $Height * 3)
    for ($row = 0; $row -lt $Height; $row++) {
        for ($column = 0; $column -lt $Width; $column++) {
            $offset = ($row * $Width + $column) * 3
            $pixels[$offset] = [byte](($row * 3) % 256); $pixels[$offset + 1] = [byte](($column * 2) % 256); $pixels[$offset + 2] = [byte]90
        }
    }
    $bytes = New-Object byte[] ($header.Length + $pixels.Length)
    [Array]::Copy($header,0,$bytes,0,$header.Length); [Array]::Copy($pixels,0,$bytes,$header.Length,$pixels.Length)
    [IO.File]::WriteAllBytes($Path,$bytes)
}

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s33-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($tempRoot) | Out-Null
$grayPath = Join-Path $tempRoot 'gradient.pgm'; Write-GradientPgm $grayPath 100 80
$markerPath = Join-Path $tempRoot 'marker.pgm'; Write-MarkerPgm $markerPath
$colorPath = Join-Path $tempRoot 'color.ppm'; Write-TestPpm $colorPath 60 40

$mainType = $assembly.GetType('HalconWinFormsDemo.MainWindow',$true)
$recipeServiceType = $assembly.GetType('HalconWinFormsDemo.Services.RecipeService',$true)
$loggerType = $assembly.GetType('HalconWinFormsDemo.Services.AppLogger',$true)
$toolKindType = $assembly.GetType('HalconWinFormsDemo.Models.VmToolKind',$true)
$roiType = $assembly.GetType('HalconWinFormsDemo.Models.RoiData',$true)
$metadataType = $assembly.GetType('HalconWinFormsDemo.Models.ToolMetadata',$true)
$viewportType = $assembly.GetType('HalconWinFormsDemo.Services.ImageViewportController',$true)
$private = [Reflection.BindingFlags]'Instance,NonPublic'
$all = [Reflection.BindingFlags]'Instance,NonPublic,Public'
$staticPublic = [Reflection.BindingFlags]'Static,Public'
$constructorArgs = New-Object 'object[]' 1; $constructorArgs[0] = $tempRoot.ToString()
$service = $recipeServiceType.GetConstructor([Type[]]@([string])).Invoke($constructorArgs)
$logger = $loggerType.GetConstructor([Type[]]@([string])).Invoke($constructorArgs)
$window = $mainType.GetConstructor($private,$null,[Type[]]@($recipeServiceType,$loggerType),$null).Invoke([object[]]@($service,$logger))
$flow = $mainType.GetField('flowTools',$private).GetValue($window)
$catalog = $mainType.GetField('toolCatalog',$private).GetValue($window)
$create = $mainType.GetMethod('CreateFlowTool',$private)
$execute = $mainType.GetMethod('ExecuteFlowTool',$private)
$runResize = $mainType.GetMethod('RunImageResizeTool',$private)
$disposeFlow = $mainType.GetMethod('DisposeFlowTools',$private)
$loadImage = $mainType.GetMethod('LoadImage',$private)
$setDualMode = $mainType.GetMethod('SetDualImageMode',$private)
$resolveLeft = $mainType.GetMethod('ResolveDualInputImageContext',$private)
$resolveRight = $mainType.GetMethod('ResolveImageContext',$private)

function Field([string]$Name) { $mainType.GetField($Name,$all).GetValue($window) }
function New-Tool([string]$Kind,[string]$Name) { $create.Invoke($window,[object[]]@([Enum]::Parse($toolKindType,$Kind),$Name,$true,$null)) }
function Invoke-Execute($Tool,[string]$Source) {
    try { $execute.Invoke($window,[object[]]@($Tool,$Source)) }
    catch [Reflection.TargetInvocationException] { throw $_.Exception.InnerException }
}
function Invoke-Resize($Tool,$Image) {
    try { $runResize.Invoke($window,[object[]]@($Tool,$Image)) }
    catch [Reflection.TargetInvocationException] { throw $_.Exception.InnerException }
}
function Get-Out($Tool,[string]$Port) {
    $value = $null
    if (-not $Tool.TryGetOutputValue($Port,[ref]$value)) { throw "$($Tool.InstanceName).$Port missing" }
    $value
}

foreach ($fieldName in @(
    'ImageLayoutToggleButton','LinkDualImageViewsCheckBox','DualInputPortComboBox','DualLeftImagePanel','DualRightImagePanel',
    'DualLeftSourceText','DualLeftDetailText','DualRightSourceText','DualLeftOverlayText','DualRightOverlayText',
    'DualLeftPixelStatusText','DualRightPixelStatusText','DualLeftMaximizeButton','DualRightMaximizeButton',
    'DockImageResizePanel','DockImageResizeWidthTextBox','DockImageResizeHeightTextBox','DockImageResizeInterpolationComboBox')) {
    Assert-True ($null -ne (Field $fieldName)) "$fieldName is missing."
}
Assert-True ($null -ne ($catalog | Where-Object { $_.Kind.ToString() -eq 'ImageResize' } | Select-Object -First 1)) 'Image resize is missing from the toolbox.'
$resizeKind = [Enum]::Parse($toolKindType,'ImageResize')
$inputs = $metadataType.GetMethod('GetInputPorts',$staticPublic).Invoke($null,[object[]]@($resizeKind))
$outputs = $metadataType.GetMethod('GetOutputPorts',$staticPublic).Invoke($null,[object[]]@($resizeKind))
Assert-True (($inputs | Where-Object { $_.PortName -eq 'Image' -and $_.DataType -eq 'Image' -and -not $_.IsOptional }).Count -eq 1) 'Typed required Image input is missing.'
foreach ($port in @('Image','Width','Height','Channels','ScaleRow','ScaleColumn','Interpolation','Parameters','ResultCode')) {
    Assert-True (($outputs | Where-Object PortName -eq $port).Count -eq 1) "Image resize output $port is missing."
}

$app = [Windows.Application]::Current
if ($null -eq $app) { $app = New-Object Windows.Application; $app.ShutdownMode = [Windows.ShutdownMode]::OnExplicitShutdown }
$disposeFlow.Invoke($window,@()) | Out-Null; $flow.Clear()
$sourceA = New-Tool ImageSource 'S33 Source A'
$sourceB = New-Tool ImageSource 'S33 Source B'
$resize = New-Tool ImageResize 'S33 Resize'
$threshold = New-Tool ImageThreshold 'S33 Threshold'
$difference = New-Tool ImageDifference 'S33 Dual Input'
$sourceA.Parameters.LocalImagePath = $grayPath; $sourceA.Parameters.LocalImageSerialNumber = 'S33-A'
$sourceB.Parameters.LocalImagePath = $markerPath; $sourceB.Parameters.LocalImageSerialNumber = 'S33-B'
$resize.Parameters.ImageResizeWidth = 50; $resize.Parameters.ImageResizeHeight = 40; $resize.Parameters.ImageResizeInterpolation = 'bilinear'
$resize.SetInputBinding('Image',$sourceA.ToolId,'Image')
$threshold.Parameters.ImageThresholdMinGray = 0; $threshold.Parameters.ImageThresholdMaxGray = 255
$threshold.SetInputBinding('Image',$resize.ToolId,'Image')
$difference.SetInputBinding('ImageA',$sourceA.ToolId,'Image'); $difference.SetInputBinding('ImageB',$sourceB.ToolId,'Image')
foreach ($tool in @($sourceA,$sourceB,$resize,$threshold,$difference)) { $flow.Add($tool) }

$window.WindowStartupLocation = [Windows.WindowStartupLocation]::Manual
$window.Left = 2; $window.Top = 2; $window.Width = 1550; $window.Height = 970; $window.Topmost = $true
$window.Show(); $window.Activate() | Out-Null; Pump-Ui 500
$loadImage.Invoke($window,[object[]]@($grayPath.ToString(),$false)) | Out-Null

Invoke-Execute $sourceA 'S33 source A' | Out-Null
Invoke-Execute $sourceB 'S33 source B' | Out-Null
$resizeRecord = Invoke-Execute $resize 'S33 shrink bilinear'
Assert-True ($resizeRecord.ResultCode -eq 'OK') 'Bilinear shrink failed.'
Assert-True ([int][double](Get-Out $resize Width) -eq 50 -and [int][double](Get-Out $resize Height) -eq 40) 'Shrink output dimensions are wrong.'
Assert-True ([Math]::Abs([double](Get-Out $resize ScaleRow) - .5) -lt 1e-9) 'ScaleRow is wrong.'
Assert-True ([Math]::Abs([double](Get-Out $resize ScaleColumn) - .5) -lt 1e-9) 'ScaleColumn is wrong.'
$oldSnapshot = Get-Out $resize Image
$resize.Parameters.ImageResizeWidth = 200; $resize.Parameters.ImageResizeHeight = 160; $resize.Parameters.ImageResizeInterpolation = 'nearest_neighbor'
Invoke-Execute $resize 'S33 enlarge nearest' | Out-Null
Assert-True $oldSnapshot.IsDisposed 'Rerun did not release the previous managed Image snapshot.'
Assert-True ([double](Get-Out $resize ScaleRow) -eq 2 -and [double](Get-Out $resize ScaleColumn) -eq 2) 'Enlarge scale output is wrong.'
Invoke-Execute $threshold 'S33 downstream threshold' | Out-Null
Assert-True ($threshold.ResultCode -eq 'OK' -and $null -ne (Get-Out $threshold Region)) 'Resized Image did not drive downstream threshold.'

# A bright source pixel must become a 2x2 block under nearest-neighbor scaling.
$markerTool = New-Tool ImageResize 'S33 Marker Resize'
$markerTool.Parameters.ImageResizeWidth = 40; $markerTool.Parameters.ImageResizeHeight = 40; $markerTool.Parameters.ImageResizeInterpolation = 'nearest_neighbor'
$markerImage = [HalconDotNet.HImage]::new($markerPath)
try {
    Invoke-Resize $markerTool $markerImage | Out-Null
    $markerOutput = (Get-Out $markerTool Image).CreateImageCopy()
    try {
        foreach ($point in @(@(10,14),@(10,15),@(11,14),@(11,15))) { Assert-True ([double]$markerOutput.GetGrayval($point[0],$point[1]).D -eq 255) 'Nearest-neighbor marker mapping is wrong.' }
        Assert-True ([double]$markerOutput.GetGrayval(9,14).D -eq 0 -and [double]$markerOutput.GetGrayval(12,15).D -eq 0) 'Nearest-neighbor marker spread exceeded the expected 2x2 block.'
    }
    finally { $markerOutput.Dispose() }
}
finally { $markerImage.Dispose(); $markerTool.Dispose() }

# HALCON zoom_image_size ignores a reduced input domain and returns a full rectangular output domain.
$domainTool = New-Tool ImageResize 'S33 Domain Resize'
$domainTool.Parameters.ImageResizeWidth = 50; $domainTool.Parameters.ImageResizeHeight = 40; $domainTool.Parameters.ImageResizeInterpolation = 'constant'
$domainImage = [HalconDotNet.HImage]::new($grayPath)
$domainRoi = $roiType.GetMethod('CreateRectangle',$staticPublic).Invoke($null,[object[]]@([double]10,[double]20,[double]20,[double]30))
$reduced = $domainImage.ReduceDomain($domainRoi.Region)
try {
    Invoke-Resize $domainTool $reduced | Out-Null
    $domainOutput = (Get-Out $domainTool Image).CreateImageCopy(); $outputDomain = $domainOutput.GetDomain()
    try {
        $area = $null; $row = $null; $column = $null
        [HalconDotNet.HOperatorSet]::AreaCenter($outputDomain,[ref]$area,[ref]$row,[ref]$column)
        Assert-True ([Math]::Abs([double]$area.D - 2000) -lt .001) 'zoom_image_size did not expose the documented full output domain.'
    }
    finally { $outputDomain.Dispose(); $domainOutput.Dispose() }
}
finally { $reduced.Dispose(); $domainRoi.Dispose(); $domainImage.Dispose(); $domainTool.Dispose() }

$rgbTool = New-Tool ImageResize 'S33 RGB Resize'
$rgbTool.Parameters.ImageResizeWidth = 90; $rgbTool.Parameters.ImageResizeHeight = 60; $rgbTool.Parameters.ImageResizeInterpolation = 'bicubic'
$rgbImage = [HalconDotNet.HImage]::new($colorPath)
try {
    Invoke-Resize $rgbTool $rgbImage | Out-Null
    Assert-True ([int][double](Get-Out $rgbTool Channels) -eq 3) 'RGB resize did not preserve channels.'
    Assert-True ([int][double](Get-Out $rgbTool Width) -eq 90 -and [int][double](Get-Out $rgbTool Height) -eq 60) 'RGB bicubic resize dimensions are wrong.'
}
finally { $rgbImage.Dispose(); $rgbTool.Dispose() }

$invalidTool = New-Tool ImageResize 'S33 Invalid Resize'; $invalidImage = [HalconDotNet.HImage]::new($grayPath)
try {
    $invalidTool.Parameters.ImageResizeWidth = 1; $invalidTool.Parameters.ImageResizeHeight = 40
    $rejected = $false; try { Invoke-Resize $invalidTool $invalidImage | Out-Null } catch { $rejected = $true }
    Assert-True $rejected 'Invalid resize dimensions were accepted.'
    $invalidTool.Parameters.ImageResizeWidth = 50; $invalidTool.Parameters.ImageResizeInterpolation = 'invented'
    $rejected = $false; try { Invoke-Resize $invalidTool $invalidImage | Out-Null } catch { $rejected = $true }
    Assert-True $rejected 'Invalid interpolation was accepted.'
}
finally { $invalidImage.Dispose(); $invalidTool.Dispose() }

# Dual mode: left is strict current-module input, right defaults to current module output.
$list = Field 'FlowToolList'; $list.SelectedItem = $resize; Pump-Ui 80
$setDualMode.Invoke($window,[object[]]@($true)) | Out-Null; Pump-Ui 120
Assert-True ($mainType.GetField('dualImageMode',$private).GetValue($window)) 'Dual-image mode did not activate.'
Assert-True ((Field 'DualLeftImagePanel').Visibility.ToString() -eq 'Visible' -and (Field 'DualRightImagePanel').Visibility.ToString() -eq 'Visible') 'Dual-image panels are not both visible.'
Assert-True ((Field 'DualInputPortComboBox').Items.Count -eq 1 -and (Field 'DualInputPortComboBox').SelectedValue -eq 'Image') 'Left input selector does not expose the real Image port.'
$leftContext = $resolveLeft.Invoke($window,@()); $rightContext = $resolveRight.Invoke($window,@())
Assert-True ($leftContext.HasImage -and $leftContext.Width -eq 100 -and $leftContext.Height -eq 80 -and $leftContext.SourceText.Contains('S33 Source A.Image')) 'Left view is not the current module real input.'
Assert-True ($rightContext.HasImage -and $rightContext.Width -eq 200 -and $rightContext.Height -eq 160 -and $rightContext.SourceText.Contains('S33 Resize.Image')) 'Right view is not the current module output.'
Assert-True ((Field 'DualLeftSourceText').Text.Contains('S33 Source A.Image') -and (Field 'DualRightSourceText').Text.Contains('S33 Resize.Image')) 'Per-view source status is not visible.'

# A two-image module must let the left view choose only ImageA/ImageB, never arbitrary global/output images.
$list.SelectedItem = $difference; Pump-Ui 100
$portCombo = Field 'DualInputPortComboBox'
Assert-True ($portCombo.Items.Count -eq 2) 'Dual-input module did not expose both real Image input ports.'
$portCombo.SelectedValue = 'ImageB'; Pump-Ui 80
$leftB = $resolveLeft.Invoke($window,@())
Assert-True ($leftB.HasImage -and $leftB.SourceText.Contains('S33 Source B.Image')) 'Left view did not switch to the selected real ImageB input.'

# Selecting an image source proves that the left view does not silently fall back to global/output context.
$list.SelectedItem = $sourceA; Pump-Ui 80
$leftSourceTool = $resolveLeft.Invoke($window,@())
Assert-True (-not $leftSourceTool.HasImage -and (Field 'DualInputPortComboBox').Items.Count -eq 0) 'Left view fell back despite the current module having no Image input.'

# Link/independent state and per-view maximize/restore must be functional UI state, not decorative controls.
$list.SelectedItem = $resize; Pump-Ui 80
$link = Field 'LinkDualImageViewsCheckBox'; $link.IsChecked = $false; Pump-Ui 40
Assert-True (-not $mainType.GetMethod('AreDualImageViewsLinked',$private).Invoke($window,@())) 'Independent-view state was not applied.'
$link.IsChecked = $true; Pump-Ui 40
Assert-True ($mainType.GetMethod('AreDualImageViewsLinked',$private).Invoke($window,@())) 'Linked-view state was not applied.'
$mainType.GetMethod('DualLeftMaximizeButton_Click',$private).Invoke($window,[object[]]@($null,$null)) | Out-Null; Pump-Ui 30
Assert-True ((Field 'DualRightImagePanel').Visibility.ToString() -eq 'Collapsed') 'Left-view maximize did not hide the right view.'
$mainType.GetMethod('DualLeftMaximizeButton_Click',$private).Invoke($window,[object[]]@($null,$null)) | Out-Null; Pump-Ui 30
Assert-True ((Field 'DualRightImagePanel').Visibility.ToString() -eq 'Visible') 'Dual-view restore did not restore the right view.'

# Normalized linking must preserve the same scene fraction even when image sizes differ.
$leftViewport = $mainType.GetField('comparisonViewport',$private).GetValue($window)
$rightViewport = $mainType.GetField('viewport',$private).GetValue($window)
$rightViewport.SetImageSize(200,160); $leftViewport.SetImageSize(100,80)
$fakeControl = New-Object Windows.Forms.Panel; $fakeControl.Width = 100; $fakeControl.Height = 80
$rightViewport.ZoomAt((New-Object Drawing.Point(50,40)),$fakeControl,120)
$leftViewport.SynchronizeNormalizedViewFrom($rightViewport)
Assert-True ($leftViewport.ZoomText -eq $rightViewport.ZoomText) 'Normalized linked view did not preserve zoom fraction across different image dimensions.'
$fakeControl.Dispose()

# Recipe persistence and disposed-upstream rejection.
$resize.Parameters.ImageResizeWidth = 320; $resize.Parameters.ImageResizeHeight = 240; $resize.Parameters.ImageResizeInterpolation = 'weighted'
$recipe = [Activator]::CreateInstance($assembly.GetType('HalconWinFormsDemo.Models.VisionRecipe',$true))
$recipe.ToolFlow = $mainType.GetMethod('CaptureFlowRecipe',$private).Invoke($window,@())
$recipePath = Join-Path $tempRoot 's33-recipe.json'; $service.SaveRecipe($recipePath,$recipe)
$loaded = $service.LoadRecipe($recipePath)
$loadedResize = $loaded.ToolFlow | Where-Object ToolType -eq 'ImageResize' | Select-Object -First 1
Assert-True ($null -ne $loadedResize -and $loadedResize.Parameters.ImageResizeWidth -eq 320 -and $loadedResize.Parameters.ImageResizeHeight -eq 240 -and $loadedResize.Parameters.ImageResizeInterpolation -eq 'weighted') 'Resize parameters did not survive recipe save/load.'

Invoke-Execute $sourceA 'S33 source before disposal' | Out-Null
$releasedSource = Get-Out $sourceA Image; $releasedSource.Dispose()
$releasedRejected = $false; try { Invoke-Execute $resize 'S33 disposed upstream' | Out-Null } catch { $releasedRejected = $true }
Assert-True $releasedRejected 'Disposed upstream Image was not rejected.'

$setDualMode.Invoke($window,[object[]]@($false)) | Out-Null
Assert-True ((Field 'DualLeftImagePanel').Visibility.ToString() -eq 'Collapsed' -and (Field 'DualRightImagePanel').Visibility.ToString() -eq 'Visible') 'Single-image mode did not restore the original preview layout.'

$mainType.GetField('recipeCloseConfirmed',$private).SetValue($window,$true)
$window.Close(); $app.Shutdown()
if (Test-Path $tempRoot) { Remove-Item -LiteralPath $tempRoot -Recurse -Force }
Write-Output 'S33 VM-style dual image comparison and HALCON image resize smoke tests passed.'
