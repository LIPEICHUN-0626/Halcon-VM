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
function Write-TestPgm([string]$Path, [int]$Width, [int]$Height) {
    $header = [Text.Encoding]::ASCII.GetBytes("P5`n$Width $Height`n255`n")
    $pixels = New-Object byte[] ($Width * $Height)
    for ($row = 0; $row -lt $Height; $row++) {
        for ($column = 0; $column -lt $Width; $column++) { $pixels[$row * $Width + $column] = [byte](($row + $column) % 256) }
    }
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
            $pixels[$offset] = [byte]$row; $pixels[$offset + 1] = [byte]$column; $pixels[$offset + 2] = [byte]90
        }
    }
    $bytes = New-Object byte[] ($header.Length + $pixels.Length)
    [Array]::Copy($header,0,$bytes,0,$header.Length); [Array]::Copy($pixels,0,$bytes,$header.Length,$pixels.Length)
    [IO.File]::WriteAllBytes($Path,$bytes)
}

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s32-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($tempRoot) | Out-Null
$grayPath = Join-Path $tempRoot 'gradient.pgm'; Write-TestPgm $grayPath 100 80
$colorPath = Join-Path $tempRoot 'color.ppm'; Write-TestPpm $colorPath 100 80

$mainType = $assembly.GetType('HalconWinFormsDemo.MainWindow',$true)
$recipeServiceType = $assembly.GetType('HalconWinFormsDemo.Services.RecipeService',$true)
$loggerType = $assembly.GetType('HalconWinFormsDemo.Services.AppLogger',$true)
$toolKindType = $assembly.GetType('HalconWinFormsDemo.Models.VmToolKind',$true)
$roiType = $assembly.GetType('HalconWinFormsDemo.Models.RoiData',$true)
$metadataType = $assembly.GetType('HalconWinFormsDemo.Models.ToolMetadata',$true)
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
$runExtraction = $mainType.GetMethod('RunRoiImageExtractionTool',$private)
$addRoi = $mainType.GetMethod('AddRoiLayer',$private)
$selectRoi = $mainType.GetMethod('SelectRoiLayer',$private)
$loadImage = $mainType.GetMethod('LoadImage',$private)
$disposeFlow = $mainType.GetMethod('DisposeFlowTools',$private)
$applyRoiDraft = $mainType.GetMethod('ApplyRoiGeometryDraftButton_Click',$private)
$cancelRoiDraft = $mainType.GetMethod('CancelRoiGeometryDraftButton_Click',$private)
$undo = $mainType.GetMethod('UndoFlowEditButton_Click',$private)
$redo = $mainType.GetMethod('RedoFlowEditButton_Click',$private)

function Field([string]$Name) { $mainType.GetField($Name,$all).GetValue($window) }
function New-Tool([string]$Kind,[string]$Name) { $create.Invoke($window,[object[]]@([Enum]::Parse($toolKindType,$Kind),$Name,$true,$null)) }
function Invoke-Execute($Tool,[string]$Source) {
    try { $execute.Invoke($window,[object[]]@($Tool,$Source)) }
    catch [Reflection.TargetInvocationException] { throw $_.Exception.InnerException }
}
function Invoke-Extraction($Tool,$Image,$Region) {
    try { $runExtraction.Invoke($window,[object[]]@($Tool,$Image,$Region)) }
    catch [Reflection.TargetInvocationException] { throw $_.Exception.InnerException }
}
function Get-Out($Tool,[string]$Port) {
    $value = $null
    if (-not $Tool.TryGetOutputValue($Port,[ref]$value)) { throw "$($Tool.InstanceName).$Port missing" }
    $value
}

foreach ($fieldName in @(
    'RoiGeometryDraftShapeText','RoiRectangleGeometryPanel','RoiRectangleCenterXTextBox','RoiRectangleCenterYTextBox','RoiRectangleWidthTextBox','RoiRectangleHeightTextBox',
    'RoiCircleGeometryPanel','RoiCircleCenterXTextBox','RoiCircleCenterYTextBox','RoiCircleDiameterTextBox',
    'RoiOrientedGeometryPanel','RoiOrientedCenterXTextBox','RoiOrientedCenterYTextBox','RoiOrientedLength1TextBox','RoiOrientedLength2TextBox','RoiOrientedAngleTextBox',
    'RoiAnnulusGeometryPanel','RoiAnnulusCenterXTextBox','RoiAnnulusCenterYTextBox','RoiAnnulusInnerDiameterTextBox','RoiAnnulusOuterDiameterTextBox',
    'RoiPolygonGeometryPanel','RoiPolygonVerticesTextBox','ApplyRoiGeometryDraftButton','CancelRoiGeometryDraftButton','ReloadRoiGeometryDraftButton',
    'DockRoiImageExtractionPanel','DockRoiImageExtractionModeComboBox')) {
    Assert-True ($null -ne (Field $fieldName)) "$fieldName is missing."
}
Assert-True ($null -ne ($catalog | Where-Object { $_.Kind.ToString() -eq 'RoiImageExtraction' } | Select-Object -First 1)) 'ROI image extraction is missing from the toolbox.'
$extractKind = [Enum]::Parse($toolKindType,'RoiImageExtraction')
$inputs = $metadataType.GetMethod('GetInputPorts',$staticPublic).Invoke($null,[object[]]@($extractKind))
$outputs = $metadataType.GetMethod('GetOutputPorts',$staticPublic).Invoke($null,[object[]]@($extractKind))
Assert-True (($inputs | Where-Object { $_.PortName -eq 'Image' -and $_.DataType -eq 'Image' -and -not $_.IsOptional }).Count -eq 1) 'Typed required Image input is missing.'
Assert-True (($inputs | Where-Object { $_.PortName -eq 'ROI' -and $_.DataType -eq 'Region' }).Count -eq 1) 'Typed optional ROI/Region input is missing.'
foreach ($port in @('Image','Width','Height','Channels','OffsetRow','OffsetColumn','Mode','Parameters','ResultCode')) { Assert-True (($outputs | Where-Object PortName -eq $port).Count -eq 1) "ROI extraction output $port is missing." }

$app = [Windows.Application]::Current
if ($null -eq $app) { $app = New-Object Windows.Application; $app.ShutdownMode = [Windows.ShutdownMode]::OnExplicitShutdown }
$disposeFlow.Invoke($window,@()) | Out-Null; $flow.Clear()
$source = New-Tool ImageSource 'S32 Image Source'
$extract = New-Tool RoiImageExtraction 'S32 ROI Extract'
$threshold = New-Tool ImageThreshold 'S32 Downstream Threshold'
$source.Parameters.LocalImagePath = $grayPath; $source.Parameters.LocalImageSerialNumber = 'S32-GRAY'
$extract.SetInputBinding('Image',$source.ToolId,'Image')
$threshold.Parameters.ImageThresholdMinGray = 0; $threshold.Parameters.ImageThresholdMaxGray = 255
$threshold.SetInputBinding('Image',$extract.ToolId,'Image')
foreach ($tool in @($source,$extract,$threshold)) { $flow.Add($tool) }

$window.WindowStartupLocation = [Windows.WindowStartupLocation]::Manual
$window.Left = 2; $window.Top = 2; $window.Width = 1550; $window.Height = 970; $window.Topmost = $true
$window.Show(); $window.Activate() | Out-Null; Pump-Ui 500
$loadImage.Invoke($window,[object[]]@($grayPath.ToString(),$false)) | Out-Null

$rectangle = $roiType.GetMethod('CreateRectangle',$staticPublic).Invoke($null,[object[]]@([double]10,[double]20,[double]40,[double]60))
$rectangleLayer = $addRoi.Invoke($window,[object[]]@($rectangle,'S32 Precision Rectangle',$null)); $rectangle.Dispose()
$extract.BindRoi($rectangleLayer.RoiId)
Invoke-Execute $source 'S32 source' | Out-Null
$extract.Parameters.RoiImageExtractionMode = 'ReduceDomain'
$reduceRecord = Invoke-Execute $extract 'S32 reduce domain'
Assert-True ($reduceRecord.ResultCode -eq 'OK') 'reduce_domain execution failed.'
Assert-True ([int][double](Get-Out $extract Width) -eq 100 -and [int][double](Get-Out $extract Height) -eq 80) 'reduce_domain did not preserve image matrix dimensions.'
Assert-True ([double](Get-Out $extract OffsetRow) -eq 0 -and [double](Get-Out $extract OffsetColumn) -eq 0) 'reduce_domain returned a false coordinate offset.'
$oldSnapshot = Get-Out $extract Image
$extract.Parameters.RoiImageExtractionMode = 'CropDomain'
$cropRecord = Invoke-Execute $extract 'S32 crop domain'
Assert-True $oldSnapshot.IsDisposed 'Rerun did not release the previous managed Image snapshot.'
Assert-True ($cropRecord.ResultCode -eq 'OK') 'crop_domain execution failed.'
Assert-True ([int][double](Get-Out $extract Width) -eq 41 -and [int][double](Get-Out $extract Height) -eq 31) 'crop_domain bounding-box dimensions are wrong.'
Assert-True ([double](Get-Out $extract OffsetRow) -eq 10 -and [double](Get-Out $extract OffsetColumn) -eq 20) 'crop_domain coordinate offset is wrong.'
$croppedImage = (Get-Out $extract Image).CreateImageCopy()
try {
    Assert-True ([double]$croppedImage.GetGrayval(0,0).D -eq 30) 'Cropped origin does not map to original Row=10, Column=20.'
    Assert-True ([double]$croppedImage.GetGrayval(30,40).D -eq 100) 'Cropped lower-right point does not preserve source coordinates.'
}
finally { $croppedImage.Dispose() }
Invoke-Execute $threshold 'S32 downstream threshold' | Out-Null
Assert-True ($threshold.ResultCode -eq 'OK' -and $null -ne (Get-Out $threshold Region)) 'Extracted Image did not drive downstream threshold processing.'

$regionThreshold = New-Tool ImageThreshold 'S32 Region Source'
$regionThreshold.Parameters.ImageThresholdMinGray = 0; $regionThreshold.Parameters.ImageThresholdMaxGray = 60
$regionThreshold.SetInputBinding('Image',$source.ToolId,'Image'); $flow.Insert(1,$regionThreshold)
$extract.ReplaceRoiBindings($null); $extract.SetInputBinding('ROI',$regionThreshold.ToolId,'Region')
Invoke-Execute $regionThreshold 'S32 upstream region' | Out-Null
$extract.Parameters.RoiImageExtractionMode = 'ReduceDomain'
Assert-True ((Invoke-Execute $extract 'S32 subscribed region').ResultCode -eq 'OK') 'Subscribed Region input did not drive extraction.'

$colorTool = New-Tool RoiImageExtraction 'S32 RGB Extract'
$colorTool.Parameters.RoiImageExtractionMode = 'CropDomain'
$colorImage = [HalconDotNet.HImage]::new($colorPath)
$colorRegion = $roiType.GetMethod('CreateCircle',$staticPublic).Invoke($null,[object[]]@([double]40,[double]50,[double]20))
try {
    $colorRecord = Invoke-Extraction $colorTool $colorImage $colorRegion.Region
    Assert-True ($colorRecord.ResultCode -eq 'OK' -and [int][double](Get-Out $colorTool Channels) -eq 3) 'RGB ROI extraction did not preserve channels.'
}
finally { $colorRegion.Dispose(); $colorImage.Dispose(); $colorTool.Dispose() }

$outsideTool = New-Tool RoiImageExtraction 'S32 Empty Domain'
$outsideImage = [HalconDotNet.HImage]::new($grayPath)
$outsideRegion = $roiType.GetMethod('CreateCircle',$staticPublic).Invoke($null,[object[]]@([double]500,[double]500,[double]10))
try {
    $rejected = $false
    try { Invoke-Extraction $outsideTool $outsideImage $outsideRegion.Region | Out-Null } catch { $rejected = $true }
    Assert-True $rejected 'Empty ROI/image intersection was not rejected.'
}
finally { $outsideRegion.Dispose(); $outsideImage.Dispose(); $outsideTool.Dispose() }

# Six ROI types must expose the correct persistent numeric workbench, and a rectangle edit must preview/apply/undo/redo/cancel.
$circle = $roiType.GetMethod('CreateCircle',$staticPublic).Invoke($null,[object[]]@([double]40,[double]40,[double]12))
$ellipse = $roiType.GetMethod('CreateEllipse',$staticPublic).Invoke($null,[object[]]@([double]40,[double]45,[double].2,[double]18,[double]9))
$annulus = $roiType.GetMethod('CreateAnnulus',$staticPublic).Invoke($null,[object[]]@([double]40,[double]45,[double]8,[double]18))
$rotated = $roiType.GetMethod('CreateRotatedRectangle',$staticPublic).Invoke($null,[object[]]@([double]40,[double]45,[double].2,[double]18,[double]9))
$polygon = $roiType.GetMethod('CreatePolygon',[Type[]]@([double[]],[double[]])).Invoke($null,[object[]]@([double[]]@(10,10,30,30),[double[]]@(10,30,30,10)))
$layers = @()
foreach ($entry in @(@($circle,'Circle'),@($ellipse,'Ellipse'),@($annulus,'Annulus'),@($rotated,'Rotated'),@($polygon,'Polygon'))) {
    $layers += $addRoi.Invoke($window,[object[]]@($entry[0],('S32 ' + $entry[1]),$null)); $entry[0].Dispose()
}
$roiList = Field 'RoiLayerList'
$expectedPanels = @('RoiCircleGeometryPanel','RoiOrientedGeometryPanel','RoiAnnulusGeometryPanel','RoiOrientedGeometryPanel','RoiPolygonGeometryPanel')
for ($index = 0; $index -lt $layers.Count; $index++) {
    $roiList.SelectedItem = $layers[$index]; $selectRoi.Invoke($window,[object[]]@($layers[$index])) | Out-Null; Pump-Ui 20
    Assert-True ((Field $expectedPanels[$index]).Visibility.ToString() -eq 'Visible') "$($layers[$index].ShapeText) numeric panel is not visible."
}

$roiList.SelectedItem = $rectangleLayer; $selectRoi.Invoke($window,[object[]]@($rectangleLayer)) | Out-Null
Assert-True ((Field 'RoiRectangleGeometryPanel').Visibility.ToString() -eq 'Visible') 'Rectangle numeric panel is not visible.'
(Field 'RoiRectangleWidthTextBox').Text = '50'; Pump-Ui 40
Assert-True ([Math]::Abs(($rectangleLayer.Geometry.Column2 - $rectangleLayer.Geometry.Column1) - 50) -lt .001) 'Numeric edit did not live-preview in ROI geometry.'
Assert-True ((Field 'ApplyRoiGeometryDraftButton').IsEnabled) 'Valid numeric draft cannot be applied.'
$applyRoiDraft.Invoke($window,[object[]]@($null,$null)) | Out-Null
Assert-True ([Math]::Abs(($rectangleLayer.Geometry.Column2 - $rectangleLayer.Geometry.Column1) - 50) -lt .001) 'Applied numeric geometry is wrong.'
$rectangleId = $rectangleLayer.RoiId
$undo.Invoke($window,[object[]]@($null,$null)) | Out-Null
$rectangleAfterUndo = $null; foreach ($candidate in $roiLayers) { if ($candidate.RoiId -eq $rectangleId) { $rectangleAfterUndo = $candidate; break } }
Assert-True ([Math]::Abs(($rectangleAfterUndo.Geometry.Column2 - $rectangleAfterUndo.Geometry.Column1) - 40) -lt .001) 'ROI numeric edit undo did not restore geometry.'
$redo.Invoke($window,[object[]]@($null,$null)) | Out-Null
$rectangleAfterRedo = $null; foreach ($candidate in $roiLayers) { if ($candidate.RoiId -eq $rectangleId) { $rectangleAfterRedo = $candidate; break } }
Assert-True ([Math]::Abs(($rectangleAfterRedo.Geometry.Column2 - $rectangleAfterRedo.Geometry.Column1) - 50) -lt .001) 'ROI numeric edit redo did not restore geometry.'
$roiList.SelectedItem = $rectangleAfterRedo; $selectRoi.Invoke($window,[object[]]@($rectangleAfterRedo)) | Out-Null
(Field 'RoiRectangleCenterXTextBox').Text = '500'; Pump-Ui 20
Assert-True (-not (Field 'ApplyRoiGeometryDraftButton').IsEnabled) 'Out-of-bounds ROI draft was accepted.'
$cancelRoiDraft.Invoke($window,[object[]]@($null,$null)) | Out-Null
Assert-True ([Math]::Abs(($rectangleAfterRedo.Geometry.Column2 - $rectangleAfterRedo.Geometry.Column1) - 50) -lt .001) 'Cancel did not restore the last applied ROI geometry.'

$roiList.SelectedItem = $layers[4]; $selectRoi.Invoke($window,[object[]]@($layers[4])) | Out-Null
(Field 'RoiPolygonVerticesTextBox').Text = "10,10`n30,30`n10,30`n30,10"; Pump-Ui 20
Assert-True (-not (Field 'ApplyRoiGeometryDraftButton').IsEnabled) 'Self-intersecting polygon draft was accepted.'
$cancelRoiDraft.Invoke($window,[object[]]@($null,$null)) | Out-Null

$extractCurrent = $null; $sourceCurrent = $null
foreach ($candidate in $flow) {
    if ($candidate.Kind.ToString() -eq 'RoiImageExtraction' -and $null -eq $extractCurrent) { $extractCurrent = $candidate }
    if ($candidate.Kind.ToString() -eq 'ImageSource' -and $null -eq $sourceCurrent) { $sourceCurrent = $candidate }
}
$list = Field 'FlowToolList'; $list.SelectedItem = $extractCurrent
$mainType.GetMethod('LoadDockConfigurationDraft',$private).Invoke($window,[object[]]@($extractCurrent)) | Out-Null
Assert-True ((Field 'DockRoiImageExtractionPanel').Visibility.ToString() -eq 'Visible') 'ROI extraction dock panel is not visible.'
$extractCurrent.Parameters.RoiImageExtractionMode = 'CropDomain'
$recipe = [Activator]::CreateInstance($assembly.GetType('HalconWinFormsDemo.Models.VisionRecipe',$true))
$recipe.ToolFlow = $mainType.GetMethod('CaptureFlowRecipe',$private).Invoke($window,@())
$recipe.RoiLayers = $mainType.GetMethod('CaptureRoiLayers',$private).Invoke($window,@())
$recipePath = Join-Path $tempRoot 's32-recipe.json'; $service.SaveRecipe($recipePath,$recipe)
$loaded = $service.LoadRecipe($recipePath)
$loadedExtract = $loaded.ToolFlow | Where-Object ToolType -eq 'RoiImageExtraction' | Select-Object -First 1
Assert-True ($null -ne $loadedExtract -and $loadedExtract.Parameters.RoiImageExtractionMode -eq 'CropDomain') 'ROI extraction mode did not survive recipe save/load.'
Assert-True (($loaded.RoiLayers | Where-Object RoiId -eq $rectangleAfterRedo.RoiId).Geometry.Column2 -gt 0) 'Edited ROI geometry did not survive recipe save/load.'

Invoke-Execute $sourceCurrent 'S32 source after history restore' | Out-Null
$sourceSnapshot = Get-Out $sourceCurrent Image; $sourceSnapshot.Dispose()
$releasedRejected = $false
try { Invoke-Execute $extractCurrent 'S32 disposed upstream image' | Out-Null } catch { $releasedRejected = $true }
Assert-True $releasedRejected 'Disposed upstream Image was not rejected.'

$mainType.GetField('recipeCloseConfirmed',$private).SetValue($window,$true)
$window.Close(); $app.Shutdown()
if (Test-Path $tempRoot) { Remove-Item -LiteralPath $tempRoot -Recurse -Force }
Write-Output 'S32 VM-style ROI geometry workbench and HALCON ROI image extraction smoke tests passed.'
