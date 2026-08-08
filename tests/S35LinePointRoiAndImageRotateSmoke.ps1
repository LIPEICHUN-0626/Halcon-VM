param([string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot))

$ErrorActionPreference = 'Stop'
if ([Threading.Thread]::CurrentThread.ApartmentState -ne [Threading.ApartmentState]::STA) { throw 'Run with powershell.exe -STA.' }
$halconRoot = 'C:\Program Files\MVTec\HALCON-20.11-Progress'
$env:PATH = (Join-Path $halconRoot 'bin\x64-win64') + ';' + $env:PATH
Add-Type -AssemblyName PresentationFramework
Add-Type -AssemblyName System.Drawing
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
function Write-MarkerPgm([string]$Path) {
    $header = [Text.Encoding]::ASCII.GetBytes("P5`n4 3`n255`n")
    $pixels = [byte[]](0,1,2,3,10,11,12,13,20,21,22,23)
    $bytes = New-Object byte[] ($header.Length + $pixels.Length)
    [Array]::Copy($header,0,$bytes,0,$header.Length); [Array]::Copy($pixels,0,$bytes,$header.Length,$pixels.Length)
    [IO.File]::WriteAllBytes($Path,$bytes)
}
function Write-GrayPgm([string]$Path, [int]$Width, [int]$Height) {
    $header = [Text.Encoding]::ASCII.GetBytes("P5`n$Width $Height`n255`n")
    $pixels = New-Object byte[] ($Width * $Height)
    for ($row = 0; $row -lt $Height; $row++) { for ($column = 0; $column -lt $Width; $column++) { $pixels[$row * $Width + $column] = [byte](($row * 3 + $column * 2) % 256) } }
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

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s35-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($tempRoot) | Out-Null
$grayPath = Join-Path $tempRoot 'gray.pgm'; Write-GrayPgm $grayPath 100 80
$markerPath = Join-Path $tempRoot 'marker.pgm'; Write-MarkerPgm $markerPath
$colorPath = Join-Path $tempRoot 'color.ppm'; Write-ColorPpm $colorPath

$mainType = $assembly.GetType('HalconWinFormsDemo.MainWindow',$true)
$recipeServiceType = $assembly.GetType('HalconWinFormsDemo.Services.RecipeService',$true)
$loggerType = $assembly.GetType('HalconWinFormsDemo.Services.AppLogger',$true)
$toolKindType = $assembly.GetType('HalconWinFormsDemo.Models.VmToolKind',$true)
$metadataType = $assembly.GetType('HalconWinFormsDemo.Models.ToolMetadata',$true)
$roiType = $assembly.GetType('HalconWinFormsDemo.Models.RoiData',$true)
$roiGeometryType = $assembly.GetType('HalconWinFormsDemo.Services.RoiGeometryEditor',$true)
$roiHandleType = $assembly.GetType('HalconWinFormsDemo.Services.RoiEditHandle',$true)
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
$runRotate = $mainType.GetMethod('RunImageRotateTool',$private)
$loadImage = $mainType.GetMethod('LoadImage',$private)
$disposeFlow = $mainType.GetMethod('DisposeFlowTools',$private)
$addRoi = $mainType.GetMethod('AddRoiLayer',$private)
$selectRoi = $mainType.GetMethod('SelectRoiLayer',$private)
$applyRoiDraft = $mainType.GetMethod('ApplyRoiGeometryDraftButton_Click',$private)
$cancelRoiDraft = $mainType.GetMethod('CancelRoiGeometryDraftButton_Click',$private)
$copyRoi = $mainType.GetMethod('CopySelectedRoiButton_Click',$private)
$toggleRoiLock = $mainType.GetMethod('ToggleSelectedRoiLockButton_Click',$private)
$deleteRoi = $mainType.GetMethod('ClearRoiButton_Click',$private)
$undo = $mainType.GetMethod('UndoFlowEditButton_Click',$private)
$redo = $mainType.GetMethod('RedoFlowEditButton_Click',$private)
$captureRecipe = $mainType.GetMethod('CaptureRecipe',$private)
$hitTest = $roiGeometryType.GetMethod('HitTestDetailed',$staticPublic)
$transform = $roiGeometryType.GetMethod('TransformDetailed',$staticPublic)

function Field([string]$Name) { $mainType.GetField($Name,$all).GetValue($window) }
function New-Tool([string]$Kind,[string]$Name) { $create.Invoke($window,[object[]]@([Enum]::Parse($toolKindType,$Kind),$Name,$true,$null)) }
function Invoke-Execute($Tool,[string]$Source) {
    try { $execute.Invoke($window,[object[]]@($Tool,$Source)) }
    catch [Reflection.TargetInvocationException] { throw $_.Exception.InnerException }
}
function Invoke-Rotate($Tool,$Image) {
    try { $runRotate.Invoke($window,[object[]]@($Tool,$Image)) }
    catch [Reflection.TargetInvocationException] { throw $_.Exception.InnerException }
}
function Get-Out($Tool,[string]$Port) {
    $value = $null
    if (-not $Tool.TryGetOutputValue($Port,[ref]$value)) { throw "$($Tool.InstanceName).$Port missing" }
    $value
}

foreach ($fieldName in @(
    'LineRoiButton','PointRoiButton','RoiLineGeometryPanel','RoiLineStartXTextBox','RoiLineStartYTextBox','RoiLineEndXTextBox','RoiLineEndYTextBox','RoiLineLengthTextBox','RoiLineAngleTextBox',
    'RoiPointGeometryPanel','RoiPointXTextBox','RoiPointYTextBox','ApplyRoiGeometryDraftButton','CancelRoiGeometryDraftButton',
    'DockImageRotatePanel','DockImageRotateAngleTextBox','DockImageRotateInterpolationComboBox')) {
    Assert-True ($null -ne (Field $fieldName)) "$fieldName is missing."
}
Assert-True ($null -ne ($catalog | Where-Object { $_.Kind.ToString() -eq 'ImageRotate' } | Select-Object -First 1)) 'Image rotate is missing from the toolbox.'
$rotateKind = [Enum]::Parse($toolKindType,'ImageRotate')
$inputs = $metadataType.GetMethod('GetInputPorts',$staticPublic).Invoke($null,[object[]]@($rotateKind))
$outputs = $metadataType.GetMethod('GetOutputPorts',$staticPublic).Invoke($null,[object[]]@($rotateKind))
Assert-True (($inputs | Where-Object { $_.PortName -eq 'Image' -and $_.DataType -eq 'Image' -and -not $_.IsOptional }).Count -eq 1) 'Image rotate required typed input is missing.'
foreach ($port in @('Image','Width','Height','Channels','Angle','Interpolation','Parameters','ResultCode')) { Assert-True (($outputs | Where-Object PortName -eq $port).Count -eq 1) "Image rotate output $port is missing." }

# Line/point geometry factories, regions, hit testing, endpoint edits, moves and bounds.
$line = $roiType.GetMethod('CreateLine',$staticPublic).Invoke($null,[object[]]@([double]10,[double]20,[double]30,[double]60))
$point = $roiType.GetMethod('CreatePoint',$staticPublic).Invoke($null,[object[]]@([double]25,[double]35))
try {
    Assert-True ($line.ShapeType.ToString() -eq 'Line' -and $line.ShapeDisplayText -eq '直线' -and $line.DisplayText.Contains('长度')) 'Line ROI identity/geometry text is wrong.'
    Assert-True ($point.ShapeType.ToString() -eq 'Point' -and $point.ShapeDisplayText -eq '点' -and $point.DisplayText.Contains('X=35.0')) 'Point ROI identity/geometry text is wrong.'
    $lineHit = $hitTest.Invoke($null,[object[]]@($line,[Drawing.PointF]::new(20,10),[double]6))
    $segmentHit = $hitTest.Invoke($null,[object[]]@($line,[Drawing.PointF]::new(40,20),[double]4))
    $pointHit = $hitTest.Invoke($null,[object[]]@($point,[Drawing.PointF]::new(35,25),[double]6))
    Assert-True ($lineHit.Handle.ToString() -eq 'LineStart' -and $segmentHit.Handle.ToString() -eq 'Move' -and $pointHit.Handle.ToString() -eq 'Point') 'Line/point control-point hit testing is wrong.'
    $movedLine = $transform.Invoke($null,[object[]]@($line,[Enum]::Parse($roiHandleType,'Move'),-1,[Drawing.PointF]::new(40,20),[Drawing.PointF]::new(50,25),100,80))
    $editedLine = $transform.Invoke($null,[object[]]@($line,[Enum]::Parse($roiHandleType,'LineEnd'),-1,[Drawing.PointF]::new(60,30),[Drawing.PointF]::new(95,70),100,80))
    $movedPoint = $transform.Invoke($null,[object[]]@($point,[Enum]::Parse($roiHandleType,'Point'),-1,[Drawing.PointF]::new(35,25),[Drawing.PointF]::new(150,100),100,80))
    try {
        Assert-True ($movedLine.Row1 -eq 15 -and $movedLine.Column1 -eq 30 -and $movedLine.Row2 -eq 35 -and $movedLine.Column2 -eq 70) 'Line whole-segment move is wrong.'
        Assert-True ($editedLine.Row2 -eq 70 -and $editedLine.Column2 -eq 95) 'Line endpoint edit is wrong.'
        Assert-True ($movedPoint.Row -eq 79 -and $movedPoint.Column -eq 99) 'Point drag did not clamp to image bounds.'
    }
    finally { $movedLine.Dispose(); $editedLine.Dispose(); $movedPoint.Dispose() }
}
finally { $line.Dispose(); $point.Dispose() }

$app = [Windows.Application]::Current
if ($null -eq $app) { $app = New-Object Windows.Application; $app.ShutdownMode = [Windows.ShutdownMode]::OnExplicitShutdown }
$disposeFlow.Invoke($window,@()) | Out-Null; $flow.Clear()
$source = New-Tool ImageSource 'S35 Source'
$rotate = New-Tool ImageRotate 'S35 HALCON Rotate'
$threshold = New-Tool ImageThreshold 'S35 Downstream Threshold'
$blob = New-Tool Blob 'S35 ROI Binding Consumer'
$source.Parameters.LocalImagePath = $grayPath; $source.Parameters.LocalImageSerialNumber = 'S35-GRAY'
$rotate.Parameters.ImageRotateAngleDegrees = 90; $rotate.Parameters.ImageRotateInterpolation = 'constant'; $rotate.SetInputBinding('Image',$source.ToolId,'Image')
$threshold.Parameters.ImageThresholdMinGray = 0; $threshold.Parameters.ImageThresholdMaxGray = 255; $threshold.SetInputBinding('Image',$rotate.ToolId,'Image')
$blob.SetInputBinding('Image',$rotate.ToolId,'Image')
foreach ($tool in @($source,$rotate,$threshold,$blob)) { $flow.Add($tool) }
$sourceId = $source.ToolId; $rotateId = $rotate.ToolId; $thresholdId = $threshold.ToolId; $blobId = $blob.ToolId

$window.WindowStartupLocation = [Windows.WindowStartupLocation]::Manual
$window.Left = 2; $window.Top = 2; $window.Width = 1550; $window.Height = 970; $window.Topmost = $true
$window.Show(); $window.Activate() | Out-Null; Pump-Ui 500
$loadImage.Invoke($window,[object[]]@($grayPath.ToString(),$false)) | Out-Null

# The actual toolbar handlers must select the new drawing tools.
$mainType.GetMethod('LineRoiButton_Click',$private).Invoke($window,[object[]]@($null,$null)) | Out-Null
Assert-True ($mainType.GetField('roiEditor',$private).GetValue($window).Tool.ToString() -eq 'LineRoi') 'Line ROI toolbar entry did not select the line tool.'
$mainType.GetMethod('PointRoiButton_Click',$private).Invoke($window,[object[]]@($null,$null)) | Out-Null
Assert-True ($mainType.GetField('roiEditor',$private).GetValue($window).Tool.ToString() -eq 'PointRoi') 'Point ROI toolbar entry did not select the point tool.'

$line = $roiType.GetMethod('CreateLine',$staticPublic).Invoke($null,[object[]]@([double]10,[double]20,[double]30,[double]60))
$point = $roiType.GetMethod('CreatePoint',$staticPublic).Invoke($null,[object[]]@([double]25,[double]35))
$lineLayer = $addRoi.Invoke($window,[object[]]@($line,'S35 Inspection Line',$blob)); $line.Dispose()
$pointLayer = $addRoi.Invoke($window,[object[]]@($point,'S35 Reference Point',$blob)); $point.Dispose()
$pointId = $pointLayer.RoiId
Assert-True ($blob.BoundRoiIds.Contains($lineLayer.RoiId) -and $blob.BoundRoiIds.Contains($pointLayer.RoiId)) 'Line/point ROI did not use the real tool binding path.'
$roiList = Field 'RoiLayerList'
$roiList.SelectedItem = $lineLayer; $selectRoi.Invoke($window,[object[]]@($lineLayer)) | Out-Null; Pump-Ui 30
Assert-True ((Field 'RoiLineGeometryPanel').Visibility.ToString() -eq 'Visible' -and (Field 'RoiLineLengthTextBox').Text -eq '44.721') 'Line numeric geometry panel is not synchronized.'
(Field 'RoiLineEndXTextBox').Text = '72'; Pump-Ui 40
Assert-True ([Math]::Abs($lineLayer.Geometry.Column2 - 72) -lt .001 -and (Field 'ApplyRoiGeometryDraftButton').IsEnabled) 'Line numeric edit did not live-preview.'
$applyRoiDraft.Invoke($window,[object[]]@($null,$null)) | Out-Null
$lineId = $lineLayer.RoiId
$undo.Invoke($window,[object[]]@($null,$null)) | Out-Null
$lineAfterUndo = $roiLayers | Where-Object RoiId -eq $lineId | Select-Object -First 1
Assert-True ([Math]::Abs($lineAfterUndo.Geometry.Column2 - 60) -lt .001) 'Line numeric edit undo did not restore the endpoint.'
$redo.Invoke($window,[object[]]@($null,$null)) | Out-Null
$lineAfterRedo = $roiLayers | Where-Object RoiId -eq $lineId | Select-Object -First 1
Assert-True ([Math]::Abs($lineAfterRedo.Geometry.Column2 - 72) -lt .001) 'Line numeric edit redo did not restore the endpoint.'
$pointLayer = $null; foreach ($candidate in $roiLayers) { if ($candidate.RoiId -eq $pointId) { $pointLayer = $candidate; break } }
$roiList.SelectedItem = $pointLayer; $selectRoi.Invoke($window,[object[]]@($pointLayer)) | Out-Null; Pump-Ui 30
Assert-True ((Field 'RoiPointGeometryPanel').Visibility.ToString() -eq 'Visible' -and (Field 'RoiPointXTextBox').Text -eq '35') 'Point numeric geometry panel is not synchronized.'
(Field 'RoiPointXTextBox').Text = '999'; Pump-Ui 25
Assert-True (-not (Field 'ApplyRoiGeometryDraftButton').IsEnabled) 'Out-of-bounds point draft was accepted.'
$cancelRoiDraft.Invoke($window,[object[]]@($null,$null)) | Out-Null
Assert-True ([Math]::Abs($pointLayer.Geometry.Column - 35) -lt .001) 'Cancel did not restore point geometry.'

# Undo/redo rebuilds managed flow instances; continue with the authoritative current instances.
$source = $null; $rotate = $null; $threshold = $null; $blob = $null
foreach ($candidate in $flow) {
    if ($candidate.ToolId -eq $sourceId) { $source = $candidate }
    elseif ($candidate.ToolId -eq $rotateId) { $rotate = $candidate }
    elseif ($candidate.ToolId -eq $thresholdId) { $threshold = $candidate }
    elseif ($candidate.ToolId -eq $blobId) { $blob = $candidate }
}
Assert-True ($null -ne $source -and $null -ne $rotate -and $null -ne $threshold -and $null -ne $blob) 'Flow instances were not restored after ROI undo/redo.'

# Generic visibility/enable/name/copy/lock/delete behavior must work for both new geometry types.
$lineLayer = $null; $pointLayer = $null
foreach ($candidate in $roiLayers) { if ($candidate.RoiId -eq $lineId) { $lineLayer = $candidate }; if ($candidate.RoiId -eq $pointId) { $pointLayer = $candidate } }
$lineLayer.IsEnabled = $false; $lineLayer.IsVisible = $false
$roiList.SelectedItem = $lineLayer; $selectRoi.Invoke($window,[object[]]@($lineLayer)) | Out-Null
$beforeCopy = $roiLayers.Count; $copyRoi.Invoke($window,[object[]]@($null,$null)) | Out-Null
$lineCopy = $roiList.SelectedItem
Assert-True ($roiLayers.Count -eq $beforeCopy + 1 -and $lineCopy.Geometry.ShapeType.ToString() -eq 'Line' -and $lineCopy.Name.Contains('副本') -and -not $lineCopy.IsEnabled -and -not $lineCopy.IsVisible -and $blob.BoundRoiIds.Contains($lineCopy.RoiId)) 'Line copy did not preserve shape/name/state/binding.'
$deleteRoi.Invoke($window,[object[]]@($null,$null)) | Out-Null
$lineLayer.IsEnabled = $true; $lineLayer.IsVisible = $true
$roiList.SelectedItem = $pointLayer; $selectRoi.Invoke($window,[object[]]@($pointLayer)) | Out-Null
$beforeCopy = $roiLayers.Count; $copyRoi.Invoke($window,[object[]]@($null,$null)) | Out-Null
$pointCopy = $roiList.SelectedItem
Assert-True ($roiLayers.Count -eq $beforeCopy + 1 -and $pointCopy.Geometry.ShapeType.ToString() -eq 'Point' -and $blob.BoundRoiIds.Contains($pointCopy.RoiId)) 'Point copy did not preserve shape/binding.'
$toggleRoiLock.Invoke($window,[object[]]@($null,$null)) | Out-Null
Assert-True $pointCopy.IsLocked 'Point copy did not enter locked state.'
$lockedCount = $roiLayers.Count; $deleteRoi.Invoke($window,[object[]]@($null,$null)) | Out-Null
Assert-True ($roiLayers.Count -eq $lockedCount) 'Locked point ROI was deleted.'
$toggleRoiLock.Invoke($window,[object[]]@($null,$null)) | Out-Null; $deleteRoi.Invoke($window,[object[]]@($null,$null)) | Out-Null
Assert-True ($roiLayers.Count -eq $lockedCount - 1) 'Unlocked point ROI copy was not deleted.'

# Real flow execution, downstream subscription and managed snapshot replacement.
Invoke-Execute $source 'S35 source' | Out-Null
Invoke-Execute $rotate 'S35 rotate flow' | Out-Null
Invoke-Execute $threshold 'S35 downstream threshold' | Out-Null
Assert-True ($rotate.ResultCode -eq 'OK' -and $threshold.ResultCode -eq 'OK' -and $null -ne (Get-Out $threshold Region)) 'Rotated Image did not drive downstream threshold.'
$firstSnapshot = Get-Out $rotate Image
$rotate.Parameters.ImageRotateAngleDegrees = 180; Invoke-Execute $rotate 'S35 rerun rotate' | Out-Null
Assert-True $firstSnapshot.IsDisposed 'Image rotate rerun did not release the previous managed snapshot.'

# Canonical 4x3 grayscale mapping for 0/90/180/270 and dimensions for arbitrary angles.
$markerImage = [HalconDotNet.HImage]::new($markerPath)
try {
    foreach ($angle in @(0,90,180,270,45)) {
        $tool = New-Tool ImageRotate ("S35 Rotate " + $angle); $tool.Parameters.ImageRotateAngleDegrees = $angle; $tool.Parameters.ImageRotateInterpolation = 'constant'
        try {
            Invoke-Rotate $tool $markerImage | Out-Null
            $copy = (Get-Out $tool Image).CreateImageCopy()
            try {
                $expectedWidth = if ($angle -eq 90 -or $angle -eq 270) { 3 } else { 4 }
                $expectedHeight = if ($angle -eq 90 -or $angle -eq 270) { 4 } else { 3 }
                Assert-True ([int](Get-Out $tool Width) -eq $expectedWidth -and [int](Get-Out $tool Height) -eq $expectedHeight) "Rotate $angle dimensions are wrong."
                if ($angle -eq 0) { Assert-True ([double]$copy.GetGrayval(0,0).D -eq 0 -and [double]$copy.GetGrayval(2,3).D -eq 23) '0-degree mapping is wrong.' }
                elseif ($angle -eq 90) { Assert-True ([double]$copy.GetGrayval(0,0).D -eq 3 -and [double]$copy.GetGrayval(3,2).D -eq 20) '90-degree CCW mapping is wrong.' }
                elseif ($angle -eq 180) { Assert-True ([double]$copy.GetGrayval(0,0).D -eq 23 -and [double]$copy.GetGrayval(2,3).D -eq 0) '180-degree mapping is wrong.' }
                elseif ($angle -eq 270) { Assert-True ([double]$copy.GetGrayval(0,0).D -eq 20 -and [double]$copy.GetGrayval(3,2).D -eq 3) '270-degree CCW mapping is wrong.' }
                Assert-True ([double](Get-Out $tool Angle) -eq $angle -and (Get-Out $tool Interpolation) -eq 'constant') 'Angle/interpolation outputs are wrong.'
            }
            finally { $copy.Dispose() }
        }
        finally { $tool.Dispose() }
    }
}
finally { $markerImage.Dispose() }

# All official HALCON 20.11 interpolation modes, RGB preservation and ignored input domain.
$interpolationImage = [HalconDotNet.HImage]::new($grayPath)
try {
    foreach ($mode in @('nearest_neighbor','constant','bilinear','bicubic','weighted')) {
        $tool = New-Tool ImageRotate ("S35 " + $mode); $tool.Parameters.ImageRotateAngleDegrees = 17; $tool.Parameters.ImageRotateInterpolation = $mode
        try { Assert-True ((Invoke-Rotate $tool $interpolationImage).ResultCode -eq 'OK') "HALCON interpolation $mode failed." }
        finally { $tool.Dispose() }
    }
}
finally { $interpolationImage.Dispose() }

$rgbTool = New-Tool ImageRotate 'S35 RGB'; $rgbTool.Parameters.ImageRotateAngleDegrees = 90; $rgbTool.Parameters.ImageRotateInterpolation = 'bilinear'; $rgbImage = [HalconDotNet.HImage]::new($colorPath)
try { Invoke-Rotate $rgbTool $rgbImage | Out-Null; Assert-True ([int](Get-Out $rgbTool Channels) -eq 3 -and [int](Get-Out $rgbTool Width) -eq 3 -and [int](Get-Out $rgbTool Height) -eq 4) 'RGB rotate did not preserve channels/swap dimensions.' }
finally { $rgbImage.Dispose(); $rgbTool.Dispose() }

$domainTool = New-Tool ImageRotate 'S35 Domain'; $domainTool.Parameters.ImageRotateAngleDegrees = 0; $domainTool.Parameters.ImageRotateInterpolation = 'constant'; $domainImage = [HalconDotNet.HImage]::new($markerPath)
$domainRoi = $roiType.GetMethod('CreatePoint',$staticPublic).Invoke($null,[object[]]@([double]1,[double]1)); $reduced = $domainImage.ReduceDomain($domainRoi.Region)
try {
    Invoke-Rotate $domainTool $reduced | Out-Null
    $domainOutput = (Get-Out $domainTool Image).CreateImageCopy(); $outputDomain = $domainOutput.GetDomain()
    try {
        $area = $null; $row = $null; $column = $null; [HalconDotNet.HOperatorSet]::AreaCenter($outputDomain,[ref]$area,[ref]$row,[ref]$column)
        Assert-True ([double]$area.D -eq 12) 'rotate_image did not ignore the reduced input domain as documented.'
    }
    finally { $outputDomain.Dispose(); $domainOutput.Dispose() }
}
finally { $reduced.Dispose(); $domainRoi.Dispose(); $domainImage.Dispose(); $domainTool.Dispose() }

foreach ($invalid in @(@(-1,'constant'),@(361,'constant'),@(90,'invented'))) {
    $tool = New-Tool ImageRotate 'S35 Invalid'; $tool.Parameters.ImageRotateAngleDegrees = $invalid[0]; $tool.Parameters.ImageRotateInterpolation = $invalid[1]; $image = [HalconDotNet.HImage]::new($markerPath)
    try { $rejected = $false; try { Invoke-Rotate $tool $image | Out-Null } catch { $rejected = $true }; Assert-True $rejected "Invalid rotate parameters $($invalid -join ',') were accepted." }
    finally { $image.Dispose(); $tool.Dispose() }
}

# Persistent dock values and both new ROI shapes must survive recipe save/load.
$list = Field 'FlowToolList'; $list.SelectedItem = $rotate; $mainType.GetMethod('LoadDockConfigurationDraft',$private).Invoke($window,[object[]]@($rotate)) | Out-Null
Assert-True ((Field 'DockImageRotatePanel').Visibility.ToString() -eq 'Visible') 'Image rotate dock panel is not visible.'
$rotate.Parameters.ImageRotateAngleDegrees = 270; $rotate.Parameters.ImageRotateInterpolation = 'weighted'
$recipe = $captureRecipe.Invoke($window,@()); $recipePath = Join-Path $tempRoot 's35-recipe.json'; $service.SaveRecipe($recipePath,$recipe); $loaded = $service.LoadRecipe($recipePath)
$loadedRotate = $loaded.ToolFlow | Where-Object ToolType -eq 'ImageRotate' | Select-Object -First 1
Assert-True ($null -ne $loadedRotate -and $loadedRotate.Parameters.ImageRotateAngleDegrees -eq 270 -and $loadedRotate.Parameters.ImageRotateInterpolation -eq 'weighted') 'Image rotate parameters did not survive recipe save/load.'
Assert-True (($loaded.RoiLayers | Where-Object { $_.Geometry.ShapeType -eq 'Line' }).Count -ge 1 -and ($loaded.RoiLayers | Where-Object { $_.Geometry.ShapeType -eq 'Point' }).Count -ge 1) 'Line/point ROI did not survive recipe save/load.'

$sourceSnapshot = Get-Out $source Image; $sourceSnapshot.Dispose()
$releasedRejected = $false; try { Invoke-Execute $rotate 'S35 disposed upstream image' | Out-Null } catch { $releasedRejected = $true }
Assert-True $releasedRejected 'Disposed upstream Image was not rejected by image rotate.'

$mainType.GetField('recipeCloseConfirmed',$private).SetValue($window,$true)
$window.Close(); $app.Shutdown()
if (Test-Path $tempRoot) { Remove-Item -LiteralPath $tempRoot -Recurse -Force }
Write-Output 'S35 VM-style line/point ROI and HALCON image rotate smoke tests passed.'
