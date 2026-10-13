param([string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot))

$ErrorActionPreference = 'Stop'
if ([Threading.Thread]::CurrentThread.ApartmentState -ne [Threading.ApartmentState]::STA) { throw 'Run with powershell.exe -STA.' }
$halconRoot = 'C:\Program Files\MVTec\HALCON-20.11-Progress'
$env:PATH = (Join-Path $halconRoot 'bin\x64-win64') + ';' + $env:PATH
Add-Type -AssemblyName PresentationFramework
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
function Write-GrayPgm([string]$Path, [int]$Width, [int]$Height) {
    $header = [Text.Encoding]::ASCII.GetBytes("P5`n$Width $Height`n255`n")
    $pixels = New-Object byte[] ($Width * $Height)
    for ($row = 0; $row -lt $Height; $row++) { for ($column = 0; $column -lt $Width; $column++) { $pixels[$row * $Width + $column] = [byte](($row * 3 + $column * 2) % 256) } }
    $bytes = New-Object byte[] ($header.Length + $pixels.Length)
    [Array]::Copy($header,0,$bytes,0,$header.Length); [Array]::Copy($pixels,0,$bytes,$header.Length,$pixels.Length)
    [IO.File]::WriteAllBytes($Path,$bytes)
}
function Write-MarkerPgm([string]$Path) {
    $header = [Text.Encoding]::ASCII.GetBytes("P5`n4 3`n255`n")
    $pixels = [byte[]](0,1,2,3,10,11,12,13,20,21,22,23)
    $bytes = New-Object byte[] ($header.Length + $pixels.Length)
    [Array]::Copy($header,0,$bytes,0,$header.Length); [Array]::Copy($pixels,0,$bytes,$header.Length,$pixels.Length)
    [IO.File]::WriteAllBytes($Path,$bytes)
}
function Write-SquareMarkerPgm([string]$Path) {
    $header = [Text.Encoding]::ASCII.GetBytes("P5`n3 3`n255`n")
    $pixels = [byte[]](0,1,2,10,11,12,20,21,22)
    $bytes = New-Object byte[] ($header.Length + $pixels.Length)
    [Array]::Copy($header,0,$bytes,0,$header.Length); [Array]::Copy($pixels,0,$bytes,$header.Length,$pixels.Length)
    [IO.File]::WriteAllBytes($Path,$bytes)
}
function Write-ColorPpm([string]$Path) {
    $header = [Text.Encoding]::ASCII.GetBytes("P6`n4 3`n255`n")
    $pixels = New-Object byte[] 36
    for ($index = 0; $index -lt 12; $index++) { $pixels[$index * 3] = [byte]($index + 1); $pixels[$index * 3 + 1] = [byte]($index + 31); $pixels[$index * 3 + 2] = [byte]($index + 61) }
    $bytes = New-Object byte[] ($header.Length + $pixels.Length)
    [Array]::Copy($header,0,$bytes,0,$header.Length); [Array]::Copy($pixels,0,$bytes,$header.Length,$pixels.Length)
    [IO.File]::WriteAllBytes($Path,$bytes)
}

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s36-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($tempRoot) | Out-Null
$grayPath = Join-Path $tempRoot 'gray.pgm'; Write-GrayPgm $grayPath 100 80
$markerPath = Join-Path $tempRoot 'marker.pgm'; Write-MarkerPgm $markerPath
$squareMarkerPath = Join-Path $tempRoot 'square-marker.pgm'; Write-SquareMarkerPgm $squareMarkerPath
$colorPath = Join-Path $tempRoot 'color.ppm'; Write-ColorPpm $colorPath

$mainType = $assembly.GetType('HalconWinFormsDemo.MainWindow',$true)
$recipeServiceType = $assembly.GetType('HalconWinFormsDemo.Services.RecipeService',$true)
$loggerType = $assembly.GetType('HalconWinFormsDemo.Services.AppLogger',$true)
$toolKindType = $assembly.GetType('HalconWinFormsDemo.Models.VmToolKind',$true)
$metadataType = $assembly.GetType('HalconWinFormsDemo.Models.ToolMetadata',$true)
$roiType = $assembly.GetType('HalconWinFormsDemo.Models.RoiData',$true)
$private = [Reflection.BindingFlags]'Instance,NonPublic'
$all = [Reflection.BindingFlags]'Instance,NonPublic,Public'
$staticPublic = [Reflection.BindingFlags]'Static,Public'
$args = New-Object 'object[]' 1; $args[0] = $tempRoot.ToString()
$service = $recipeServiceType.GetConstructor([Type[]]@([string])).Invoke($args)
$logger = $loggerType.GetConstructor([Type[]]@([string])).Invoke($args)
$window = $mainType.GetConstructor($private,$null,[Type[]]@($recipeServiceType,$loggerType),$null).Invoke([object[]]@($service,$logger))
$flow = $mainType.GetField('flowTools',$private).GetValue($window)
$roiLayers = $mainType.GetField('roiLayers',$private).GetValue($window)
$catalog = $mainType.GetField('toolCatalog',$private).GetValue($window)
$create = $mainType.GetMethod('CreateFlowTool',$private)
$execute = $mainType.GetMethod('ExecuteFlowTool',$private)
$runAffine = $mainType.GetMethod('RunImageAffineTransformTool',$private)
$loadImage = $mainType.GetMethod('LoadImage',$private)
$disposeFlow = $mainType.GetMethod('DisposeFlowTools',$private)
$addRoi = $mainType.GetMethod('AddRoiLayer',$private)
$selectRoi = $mainType.GetMethod('SelectRoiLayer',$private)
$toggleRole = $mainType.GetMethod('ToggleSelectedRoiRoleButton_Click',$private)
$moveRoiUp = $mainType.GetMethod('MoveSelectedRoiUpButton_Click',$private)
$copyRoi = $mainType.GetMethod('CopySelectedRoiButton_Click',$private)
$deleteRoi = $mainType.GetMethod('ClearRoiButton_Click',$private)
$undo = $mainType.GetMethod('UndoFlowEditButton_Click',$private)
$redo = $mainType.GetMethod('RedoFlowEditButton_Click',$private)
$createBound = $mainType.GetMethod('CreateBoundRoiRegion',$private)
$createEffective = $mainType.GetMethod('CreateEffectiveRoiRegion',$private)
$captureRecipe = $mainType.GetMethod('CaptureRecipe',$private)

function Field([string]$Name) { $mainType.GetField($Name,$all).GetValue($window) }
function New-Tool([string]$Kind,[string]$Name) { $create.Invoke($window,[object[]]@([Enum]::Parse($toolKindType,$Kind),$Name,$true,$null)) }
function Invoke-Execute($Tool,[string]$Source) {
    try { $execute.Invoke($window,[object[]]@($Tool,$Source)) }
    catch [Reflection.TargetInvocationException] { throw $_.Exception.InnerException }
}
function Invoke-Affine($Tool,$Image) {
    try { $runAffine.Invoke($window,[object[]]@($Tool,$Image)) }
    catch [Reflection.TargetInvocationException] { throw $_.Exception.InnerException }
}
function Invoke-Region($Method,$Tool,[bool]$Required) {
    try { $Method.Invoke($window,[object[]]@($Tool,$Required)) }
    catch [Reflection.TargetInvocationException] { throw $_.Exception.InnerException }
}
function Get-Out($Tool,[string]$Port) {
    $value = $null
    if (-not $Tool.TryGetOutputValue($Port,[ref]$value)) { throw "$($Tool.InstanceName).$Port missing" }
    $value
}
function Get-Area($Region) {
    $area = $null; $row = $null; $column = $null
    [HalconDotNet.HOperatorSet]::AreaCenter($Region,[ref]$area,[ref]$row,[ref]$column)
    $sum = 0.0; foreach ($value in $area.ToDArr()) { $sum += $value }; $sum
}
function Find-Tool([string]$Id) { foreach ($candidate in $flow) { if ($candidate.ToolId -eq $Id) { return $candidate } }; $null }
function Find-Roi([string]$Id) { foreach ($candidate in $roiLayers) { if ($candidate.RoiId -eq $Id) { return $candidate } }; $null }

foreach ($fieldName in @(
    'ToggleSelectedRoiRoleButton','MoveSelectedRoiUpButton','MoveSelectedRoiDownButton','SelectedRoiRoleText',
    'DockImageAffineTransformPanel','DockImageAffineTranslateRowTextBox','DockImageAffineTranslateColumnTextBox',
    'DockImageAffineScaleRowTextBox','DockImageAffineScaleColumnTextBox','DockImageAffineRotationTextBox',
    'DockImageAffineSlantTextBox','DockImageAffineInterpolationComboBox','DockImageAffineAdaptSizeCheckBox')) {
    Assert-True ($null -ne (Field $fieldName)) "$fieldName is missing."
}
Assert-True ($null -ne ($catalog | Where-Object { $_.Kind.ToString() -eq 'ImageAffineTransform' } | Select-Object -First 1)) 'Affine image transform is missing from the toolbox.'
$affineKind = [Enum]::Parse($toolKindType,'ImageAffineTransform')
$inputs = $metadataType.GetMethod('GetInputPorts',$staticPublic).Invoke($null,[object[]]@($affineKind))
$outputs = $metadataType.GetMethod('GetOutputPorts',$staticPublic).Invoke($null,[object[]]@($affineKind))
Assert-True (($inputs | Where-Object { $_.PortName -eq 'Image' -and $_.DataType -eq 'Image' -and -not $_.IsOptional }).Count -eq 1) 'Affine transform required typed Image input is missing.'
foreach ($port in @('Image','Width','Height','Channels','HomMat2D','Interpolation','AdaptImageSize','Parameters','ResultCode')) { Assert-True (($outputs | Where-Object PortName -eq $port).Count -eq 1) "Affine output $port is missing." }

$app = [Windows.Application]::Current
if ($null -eq $app) { $app = New-Object Windows.Application; $app.ShutdownMode = [Windows.ShutdownMode]::OnExplicitShutdown }
$disposeFlow.Invoke($window,@()) | Out-Null; $flow.Clear()
$source = New-Tool ImageSource 'S36 Source'
$affine = New-Tool ImageAffineTransform 'S36 HALCON Affine'
$threshold = New-Tool ImageThreshold 'S36 Downstream Threshold'
$blob = New-Tool Blob 'S36 Masked Blob'
$gray = New-Tool GrayStat 'S36 Region Subscriber'
$source.Parameters.LocalImagePath = $grayPath; $source.Parameters.LocalImageSerialNumber = 'S36-GRAY'
$affine.SetInputBinding('Image',$source.ToolId,'Image')
$threshold.Parameters.ImageThresholdMinGray = 0; $threshold.Parameters.ImageThresholdMaxGray = 255; $threshold.SetInputBinding('Image',$affine.ToolId,'Image')
$gray.SetInputBinding('Image',$affine.ToolId,'Image'); $gray.SetInputBinding('ROI',$threshold.ToolId,'Region')
foreach ($tool in @($source,$affine,$threshold,$blob,$gray)) { $flow.Add($tool) }
$sourceId=$source.ToolId; $affineId=$affine.ToolId; $thresholdId=$threshold.ToolId; $blobId=$blob.ToolId; $grayId=$gray.ToolId

$window.WindowStartupLocation = [Windows.WindowStartupLocation]::Manual
$window.Left = 2; $window.Top = 2; $window.Width = 1550; $window.Height = 970; $window.Topmost = $true
$window.Show(); $window.Activate() | Out-Null; Pump-Ui 350
$loadImage.Invoke($window,[object[]]@($grayPath.ToString(),$false)) | Out-Null

# A-line: turn a real area ROI into a mask through the visible handler, then verify recipe-aware undo/redo.
$detectionGeometry = $roiType.GetMethod('CreateRectangle',$staticPublic).Invoke($null,[object[]]@([double]0,[double]0,[double]79,[double]99))
$maskGeometry = $roiType.GetMethod('CreateRectangle',$staticPublic).Invoke($null,[object[]]@([double]20,[double]20,[double]39,[double]39))
$detection = $addRoi.Invoke($window,[object[]]@($detectionGeometry,'S36 Detection',$blob)); $detectionGeometry.Dispose()
$mask = $addRoi.Invoke($window,[object[]]@($maskGeometry,'S36 Mask',$blob)); $maskGeometry.Dispose()
$detectionId=$detection.RoiId; $maskId=$mask.RoiId
$roiList = Field 'RoiLayerList'; $roiList.SelectedItem=$mask; $selectRoi.Invoke($window,[object[]]@($mask)) | Out-Null
$toggleRole.Invoke($window,[object[]]@($null,$null)) | Out-Null
Assert-True ($mask.IsMask -and $mask.RoleText -eq '屏蔽区' -and (Field 'SelectedRoiRoleText').Text.Contains('差集')) 'Visible ROI role handler did not create a mask.'
$undo.Invoke($window,[object[]]@($null,$null)) | Out-Null
Assert-True (-not (Find-Roi $maskId).IsMask) 'ROI role undo did not restore detection role.'
$redo.Invoke($window,[object[]]@($null,$null)) | Out-Null
$source=Find-Tool $sourceId; $affine=Find-Tool $affineId; $threshold=Find-Tool $thresholdId; $blob=Find-Tool $blobId; $gray=Find-Tool $grayId
$detection=Find-Roi $detectionId; $mask=Find-Roi $maskId
Assert-True $mask.IsMask 'ROI role redo did not restore mask role.'

$region = Invoke-Region $createBound $blob $true
try { Assert-True ([Math]::Abs((Get-Area $region)-7600) -lt .001) 'Detection union minus mask area is wrong.' } finally { $region.Dispose() }
$mask.IsVisible=$false; $region=Invoke-Region $createBound $blob $true
try { Assert-True ([Math]::Abs((Get-Area $region)-7600) -lt .001) 'Mask visibility incorrectly changed the algorithm domain.' } finally { $region.Dispose() }
$mask.IsEnabled=$false; $region=Invoke-Region $createBound $blob $true
try { Assert-True ([Math]::Abs((Get-Area $region)-8000) -lt .001) 'Disabled mask still changed the algorithm domain.' } finally { $region.Dispose() }
$mask.IsEnabled=$true; $mask.IsVisible=$true

# Copy preserves role/bindings; layer ordering is visible and undoable.
$roiList.SelectedItem=$mask; $selectRoi.Invoke($window,[object[]]@($mask)) | Out-Null
$beforeCopy=$roiLayers.Count; $copyRoi.Invoke($window,[object[]]@($null,$null)) | Out-Null
$maskCopy=$roiList.SelectedItem
Assert-True ($roiLayers.Count -eq $beforeCopy+1 -and $maskCopy.IsMask -and $blob.IsRoiBound($maskCopy.RoiId)) 'Mask copy did not preserve role and tool binding.'
$deleteRoi.Invoke($window,[object[]]@($null,$null)) | Out-Null
$mask=Find-Roi $maskId; $roiList.SelectedItem=$mask; $selectRoi.Invoke($window,[object[]]@($mask)) | Out-Null
$oldIndex=$roiLayers.IndexOf($mask); $moveRoiUp.Invoke($window,[object[]]@($null,$null)) | Out-Null
Assert-True ($roiLayers.IndexOf($mask) -eq $oldIndex-1) 'Mask layer did not move upward.'
$undo.Invoke($window,[object[]]@($null,$null)) | Out-Null
Assert-True ($roiLayers.IndexOf((Find-Roi $maskId)) -eq $oldIndex) 'Mask layer order undo failed.'
$redo.Invoke($window,[object[]]@($null,$null)) | Out-Null
$source=Find-Tool $sourceId; $affine=Find-Tool $affineId; $threshold=Find-Tool $thresholdId; $blob=Find-Tool $blobId; $gray=Find-Tool $grayId
$detection=Find-Roi $detectionId; $mask=Find-Roi $maskId

# Line/point are intentionally rejected as masks.
$lineGeometry = $roiType.GetMethod('CreateLine',$staticPublic).Invoke($null,[object[]]@([double]5,[double]5,[double]30,[double]30))
$lineLayer = $addRoi.Invoke($window,[object[]]@($lineGeometry,'S36 Line',$null)); $lineGeometry.Dispose()
$roiList.SelectedItem=$lineLayer; $selectRoi.Invoke($window,[object[]]@($lineLayer)) | Out-Null
$toggleRole.Invoke($window,[object[]]@($null,$null)) | Out-Null
Assert-True (-not $lineLayer.IsMask) 'Line ROI was incorrectly accepted as a mask.'

# Real Blob merged/per-ROI paths both subtract the same enabled mask.
$blob.Parameters.BlobMinGray=0; $blob.Parameters.BlobMaxGray=255; $blob.Parameters.BlobMinArea=1; $blob.Parameters.RoiExecutionMode='Union'
Invoke-Execute $blob 'S36 merged masked Blob' | Out-Null
Assert-True ($blob.ResultCode -eq 'OK' -and [Math]::Abs([double](Get-Out $blob Area)-7600) -lt .001) 'Merged Blob did not execute on detection minus mask.'
$blob.Parameters.RoiExecutionMode='PerRoi'; Invoke-Execute $blob 'S36 per-ROI masked Blob' | Out-Null
Assert-True ($blob.RoiResults.Count -eq 1 -and [Math]::Abs($blob.RoiResults[0].Value-7600) -lt .001) 'Per-ROI Blob did not subtract the mask from each detection area.'

# A subscribed Region can be used as the base while local masks are still applied.
Invoke-Execute $source 'S36 source' | Out-Null
Invoke-Execute $affine 'S36 identity affine' | Out-Null
Invoke-Execute $threshold 'S36 threshold' | Out-Null
$gray.BindRoi($mask.RoiId)
$region = Invoke-Region $createEffective $gray $true
try { Assert-True ([Math]::Abs((Get-Area $region)-7600) -lt .001) 'Subscribed Region did not subtract the local mask.' } finally { $region.Dispose() }
$gray.Parameters.GrayMin=0; $gray.Parameters.GrayMax=255; Invoke-Execute $gray 'S36 masked subscribed Region' | Out-Null
Assert-True ($gray.ResultCode -eq 'OK') 'GrayStat did not run with subscribed Region minus local mask.'

$maskOnly = New-Tool Blob 'S36 Mask Only'; $maskOnly.BindRoi($mask.RoiId)
$rejected=$false; try { $invalidRegion=Invoke-Region $createBound $maskOnly $false; if($null-ne$invalidRegion){$invalidRegion.Dispose()} } catch { $rejected=$_.Exception.Message.Contains('只绑定了屏蔽区') }
Assert-True $rejected 'Mask-only local domain was not rejected with an actionable error.'
$maskOnly.Dispose()
$fullMaskGeometry=$roiType.GetMethod('CreateRectangle',$staticPublic).Invoke($null,[object[]]@([double]0,[double]0,[double]79,[double]99))
$fullMask=$addRoi.Invoke($window,[object[]]@($fullMaskGeometry,'S36 Empty Domain Mask',$blob)); $fullMaskGeometry.Dispose(); $fullMask.IsMask=$true
$rejected=$false; try { $invalidRegion=Invoke-Region $createBound $blob $true; if($null-ne$invalidRegion){$invalidRegion.Dispose()} } catch { $rejected=$_.Exception.Message.Contains('有效运行域为空') }
Assert-True $rejected 'Fully masked empty domain was not rejected.'
$blob.UnbindRoi($fullMask.RoiId)

# B-line: direct HALCON affine execution, mapping, interpolation, channels and lifetime.
$markerImage=[HalconDotNet.HImage]::new($markerPath)
try {
    $direct=New-Tool ImageAffineTransform 'S36 Direct Affine'
    try {
        $direct.Parameters.ImageAffineTranslateRow=0; $direct.Parameters.ImageAffineTranslateColumn=0
        $direct.Parameters.ImageAffineScaleRow=1; $direct.Parameters.ImageAffineScaleColumn=1
        $direct.Parameters.ImageAffineRotationDegrees=0; $direct.Parameters.ImageAffineSlantDegrees=0
        $direct.Parameters.ImageAffineInterpolation='nearest_neighbor'; $direct.Parameters.ImageAffineAdaptImageSize=$false
        Invoke-Affine $direct $markerImage | Out-Null
        $copy=(Get-Out $direct Image).CreateImageCopy()
        try {
            Assert-True ([int](Get-Out $direct Width)-eq 4 -and [int](Get-Out $direct Height)-eq 3) 'Identity affine dimensions are wrong.'
            Assert-True ([double]$copy.GetGrayval(0,0).D-eq 0 -and [double]$copy.GetGrayval(2,3).D-eq 23) 'Identity affine pixel mapping is wrong.'
            Assert-True ((Get-Out $direct HomMat2D).StartsWith('[') -and (Get-Out $direct Parameters).Contains('PixelCenterCorrection')) 'Affine matrix/coordinate contract output is missing.'
        } finally { $copy.Dispose() }

        $direct.Parameters.ImageAffineTranslateRow=1; $direct.Parameters.ImageAffineTranslateColumn=2; $direct.Parameters.ImageAffineAdaptImageSize=$false
        Invoke-Affine $direct $markerImage | Out-Null; $copy=(Get-Out $direct Image).CreateImageCopy()
        try { Assert-True ([double]$copy.GetGrayval(1,2).D-eq 0 -and [double]$copy.GetGrayval(2,3).D-eq 11) 'Positive Row/Column affine translation mapping is wrong.' } finally { $copy.Dispose() }

        $direct.Parameters.ImageAffineAdaptImageSize=$true; Invoke-Affine $direct $markerImage | Out-Null
        Assert-True ([int](Get-Out $direct Width)-eq 6 -and [int](Get-Out $direct Height)-eq 4) 'AdaptImageSize did not expand right/lower output bounds for translation.'

        $direct.Parameters.ImageAffineTranslateRow=0; $direct.Parameters.ImageAffineTranslateColumn=0; $direct.Parameters.ImageAffineScaleRow=1.2; $direct.Parameters.ImageAffineScaleColumn=.8
        $direct.Parameters.ImageAffineRotationDegrees=17; $direct.Parameters.ImageAffineSlantDegrees=8; $direct.Parameters.ImageAffineAdaptImageSize=$false
        foreach($mode in @('nearest_neighbor','constant','bilinear','bicubic','weighted')) { $direct.Parameters.ImageAffineInterpolation=$mode; Assert-True ((Invoke-Affine $direct $markerImage).ResultCode-eq 'OK') "HALCON affine interpolation $mode failed." }
    } finally { $direct.Dispose() }
} finally { $markerImage.Dispose() }

$squareImage=[HalconDotNet.HImage]::new($squareMarkerPath); $rotationTool=New-Tool ImageAffineTransform 'S36 Rotation Mapping'
try {
    $rotationTool.Parameters.ImageAffineScaleRow=1; $rotationTool.Parameters.ImageAffineScaleColumn=1; $rotationTool.Parameters.ImageAffineRotationDegrees=90; $rotationTool.Parameters.ImageAffineInterpolation='nearest_neighbor'; $rotationTool.Parameters.ImageAffineAdaptImageSize=$false
    Invoke-Affine $rotationTool $squareImage | Out-Null; $copy=(Get-Out $rotationTool Image).CreateImageCopy()
    try { Assert-True ([double]$copy.GetGrayval(0,0).D-eq 2 -and [double]$copy.GetGrayval(2,2).D-eq 20) 'Positive affine rotation is not the expected counterclockwise image mapping.' } finally { $copy.Dispose() }
} finally { $squareImage.Dispose(); $rotationTool.Dispose() }

$rgbImage=[HalconDotNet.HImage]::new($colorPath); $rgbTool=New-Tool ImageAffineTransform 'S36 RGB Affine'
try { $rgbTool.Parameters.ImageAffineScaleRow=1; $rgbTool.Parameters.ImageAffineScaleColumn=1; Invoke-Affine $rgbTool $rgbImage | Out-Null; Assert-True ([int](Get-Out $rgbTool Channels)-eq 3) 'Affine transform did not preserve RGB channels.' }
finally { $rgbImage.Dispose(); $rgbTool.Dispose() }

$domainImage=[HalconDotNet.HImage]::new($markerPath); $domainTool=New-Tool ImageAffineTransform 'S36 Domain Affine'; $point=$roiType.GetMethod('CreatePoint',$staticPublic).Invoke($null,[object[]]@([double]1,[double]1)); $reduced=$domainImage.ReduceDomain($point.Region)
try {
    $domainTool.Parameters.ImageAffineScaleRow=1; $domainTool.Parameters.ImageAffineScaleColumn=1; Invoke-Affine $domainTool $reduced | Out-Null
    $output=(Get-Out $domainTool Image).CreateImageCopy(); $domain=$output.GetDomain()
    try { Assert-True ([Math]::Abs((Get-Area $domain)-12)-lt .001) 'affine_trans_image did not ignore the reduced input domain.' } finally { $domain.Dispose(); $output.Dispose() }
} finally { $reduced.Dispose(); $point.Dispose(); $domainImage.Dispose(); $domainTool.Dispose() }

foreach($invalid in @(
    @{sr=0;sc=1;rot=0;slant=0;tr=0;tc=0;adapt=$false},
    @{sr=1;sc=1;rot=361;slant=0;tr=0;tc=0;adapt=$false},
    @{sr=1;sc=1;rot=0;slant=80;tr=0;tc=0;adapt=$false},
    @{sr=1;sc=1;rot=0;slant=0;tr=100000;tc=100000;adapt=$true})) {
    $tool=New-Tool ImageAffineTransform 'S36 Invalid Affine'; $image=[HalconDotNet.HImage]::new($markerPath)
    try {
        $tool.Parameters.ImageAffineScaleRow=$invalid.sr; $tool.Parameters.ImageAffineScaleColumn=$invalid.sc; $tool.Parameters.ImageAffineRotationDegrees=$invalid.rot
        $tool.Parameters.ImageAffineSlantDegrees=$invalid.slant; $tool.Parameters.ImageAffineTranslateRow=$invalid.tr; $tool.Parameters.ImageAffineTranslateColumn=$invalid.tc; $tool.Parameters.ImageAffineAdaptImageSize=$invalid.adapt
        $rejected=$false; try { Invoke-Affine $tool $image | Out-Null } catch { $rejected=$true }; Assert-True $rejected 'Invalid/unsafe affine parameters were accepted.'
    } finally { $image.Dispose(); $tool.Dispose() }
}

# Real upstream/downstream snapshots, dock visibility, persistence and old-field defaults.
$source=Find-Tool $sourceId; $affine=Find-Tool $affineId; $threshold=Find-Tool $thresholdId
$affine.Parameters.ImageAffineTranslateRow=2; $affine.Parameters.ImageAffineTranslateColumn=3; $affine.Parameters.ImageAffineScaleRow=1; $affine.Parameters.ImageAffineScaleColumn=1; $affine.Parameters.ImageAffineAdaptImageSize=$false
Invoke-Execute $source 'S36 source rerun' | Out-Null; Invoke-Execute $affine 'S36 affine flow' | Out-Null; Invoke-Execute $threshold 'S36 downstream threshold' | Out-Null
Assert-True ($affine.ResultCode-eq 'OK' -and $threshold.ResultCode-eq 'OK' -and $null-ne(Get-Out $threshold Region)) 'Affine Image did not drive downstream threshold.'
$oldSnapshot=Get-Out $affine Image; $affine.Parameters.ImageAffineTranslateRow=3; Invoke-Execute $affine 'S36 snapshot replacement' | Out-Null
Assert-True $oldSnapshot.IsDisposed 'Affine rerun did not dispose the previous managed Image snapshot.'

$list=Field 'FlowToolList'; $list.SelectedItem=$affine; $mainType.GetMethod('LoadDockConfigurationDraft',$private).Invoke($window,[object[]]@($affine)) | Out-Null
Assert-True ((Field 'DockImageAffineTransformPanel').Visibility.ToString()-eq 'Visible' -and (Field 'DockImageAffineAdaptSizeCheckBox').IsChecked-eq $false) 'Affine dock panel is not visible/synchronized.'
$recipe=$captureRecipe.Invoke($window,@()); $recipePath=Join-Path $tempRoot 's36-recipe.json'; $service.SaveRecipe($recipePath,$recipe); $loaded=$service.LoadRecipe($recipePath)
$loadedAffine=$loaded.ToolFlow | Where-Object ToolType -eq 'ImageAffineTransform' | Select-Object -First 1
Assert-True ($null-ne$loadedAffine -and $loadedAffine.Parameters.ImageAffineTranslateRow-eq 3 -and $loadedAffine.Parameters.ImageAffineScaleRow-eq 1) 'Affine parameters did not survive recipe save/load.'
Assert-True (($loaded.RoiLayers | Where-Object { $_.RoiId-eq$maskId -and $_.IsMask }).Count-eq 1) 'Mask role did not survive recipe save/load.'
$legacyParameters=$assembly.GetType('HalconWinFormsDemo.Models.VmToolParameterData',$true).GetConstructor([Type[]]@()).Invoke(@()); $legacyParameters.ImageAffineScaleRow=0; $legacyParameters.ImageAffineScaleColumn=0; $legacyParameters.ImageAffineInterpolation=$null; $legacyParameters.Normalize()
Assert-True ($legacyParameters.ImageAffineScaleRow-eq 1 -and $legacyParameters.ImageAffineScaleColumn-eq 1 -and $legacyParameters.ImageAffineInterpolation-eq 'bilinear') 'Missing affine fields do not normalize to identity defaults.'

$sourceSnapshot=Get-Out $source Image; $sourceSnapshot.Dispose(); $rejected=$false
try { Invoke-Execute $affine 'S36 disposed upstream image' | Out-Null } catch { $rejected=$true }
Assert-True $rejected 'Disposed upstream Image was not rejected by affine transform.'

$mainType.GetField('recipeCloseConfirmed',$private).SetValue($window,$true)
$window.Close(); $app.Shutdown()
if (Test-Path $tempRoot) { Remove-Item -LiteralPath $tempRoot -Recurse -Force }
Write-Output 'S36 VM-style ROI mask and HALCON affine image transform smoke tests passed.'
