param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'
if ([Threading.Thread]::CurrentThread.ApartmentState -ne [Threading.ApartmentState]::STA) {
    throw 'Run this smoke test with powershell.exe -STA.'
}

$halconRoot = 'C:\Program Files\MVTec\HALCON-20.11-Progress'
$env:PATH = (Join-Path $halconRoot 'bin\x64-win64') + ';' + $env:PATH
Add-Type -AssemblyName PresentationFramework
Add-Type -AssemblyName System.Drawing
[Reflection.Assembly]::LoadFrom((Join-Path $halconRoot 'bin\dotnet35\halcondotnet.dll')) | Out-Null
$exePath = Join-Path $ProjectRoot 'bin\x64\Debug\HalconWinFormsDemo.exe'
if (-not (Test-Path -LiteralPath $exePath)) { throw "Build Debug x64 first: $exePath" }
$assembly = [Reflection.Assembly]::LoadFrom($exePath)

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw "Assertion failed: $Message" }
}

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s14-smoke-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($tempRoot) | Out-Null
$imagePath = Join-Path $tempRoot 'feature-input.png'
$bitmap = New-Object Drawing.Bitmap 160, 110
try {
    for ($row = 0; $row -lt $bitmap.Height; $row++) {
        for ($column = 0; $column -lt $bitmap.Width; $column++) {
            $square = $column -ge 10 -and $column -le 29 -and $row -ge 12 -and $row -le 31
            $wide = $column -ge 50 -and $column -le 89 -and $row -ge 15 -and $row -le 34
            $tall = $column -ge 110 -and $column -le 139 -and $row -ge 50 -and $row -le 89
            $gray = if ($square -or $wide -or $tall) { 230 } else { 10 }
            $bitmap.SetPixel($column, $row, [Drawing.Color]::FromArgb($gray, $gray, $gray))
        }
    }
    $bitmap.Save($imagePath, [Drawing.Imaging.ImageFormat]::Png)
}
finally {
    $bitmap.Dispose()
}

$mainType = $assembly.GetType('HalconWinFormsDemo.MainWindow', $true)
$recipeServiceType = $assembly.GetType('HalconWinFormsDemo.Services.RecipeService', $true)
$loggerType = $assembly.GetType('HalconWinFormsDemo.Services.AppLogger', $true)
$toolKindType = $assembly.GetType('HalconWinFormsDemo.Models.VmToolKind', $true)
$roiType = $assembly.GetType('HalconWinFormsDemo.Models.RoiData', $true)
$roiEditorType = $assembly.GetType('HalconWinFormsDemo.Services.RoiGeometryEditor', $true)
$roiHandleType = $assembly.GetType('HalconWinFormsDemo.Services.RoiEditHandle', $true)
$private = [Reflection.BindingFlags]'Instance,NonPublic'
$publicStatic = [Reflection.BindingFlags]'Static,Public'
$allFields = [Reflection.BindingFlags]'Instance,NonPublic,Public'
$constructorArgs = New-Object 'object[]' 1
$constructorArgs[0] = $tempRoot.ToString()
$recipeService = $recipeServiceType.GetConstructor([Type[]]@([string])).Invoke($constructorArgs)
$logger = $loggerType.GetConstructor([Type[]]@([string])).Invoke($constructorArgs)
$windowConstructor = $mainType.GetConstructor($private, $null, [Type[]]@($recipeServiceType, $loggerType), $null)
$window = $windowConstructor.Invoke([object[]]@($recipeService, $logger))

$flow = $mainType.GetField('flowTools', $private).GetValue($window)
$roiLayers = $mainType.GetField('roiLayers', $private).GetValue($window)
$catalog = $mainType.GetField('toolCatalog', $private).GetValue($window)
$createTool = $mainType.GetMethod('CreateFlowTool', $private)
$executeTool = $mainType.GetMethod('ExecuteFlowTool', $private)
$disposeTools = $mainType.GetMethod('DisposeFlowTools', $private)
$openConfiguration = $mainType.GetMethod('OpenSelectedToolConfiguration', $private)
$applyDock = $mainType.GetMethod('ApplyDockConfigurationDraft', $private)
$captureRecipe = $mainType.GetMethod('CaptureRecipe', $private)
$addRoiLayer = $mainType.GetMethod('AddRoiLayer', $private)
$copyRoi = $mainType.GetMethod('CopySelectedRoiButton_Click', $private)
$toggleLock = $mainType.GetMethod('ToggleSelectedRoiLockButton_Click', $private)

$flowToolList = $mainType.GetField('FlowToolList', $allFields).GetValue($window)
$roiLayerList = $mainType.GetField('RoiLayerList', $allFields).GetValue($window)
$rightTabs = $mainType.GetField('RightTabs', $allFields).GetValue($window)
$dockTab = $mainType.GetField('DockConfigurationTab', $allFields).GetValue($window)
$dockFeaturePanel = $mainType.GetField('DockRegionFeaturePanel', $allFields).GetValue($window)
$dockFeatureCombo = $mainType.GetField('DockRegionFeatureComboBox', $allFields).GetValue($window)
$dockFeatureMin = $mainType.GetField('DockRegionFeatureMinTextBox', $allFields).GetValue($window)
$dockFeatureMax = $mainType.GetField('DockRegionFeatureMaxTextBox', $allFields).GetValue($window)
$dockValidation = $mainType.GetField('DockValidationText', $allFields).GetValue($window)
$dockInputRows = $mainType.GetField('dockInputPortRows', $private).GetValue($window)

function New-Tool([string]$Kind, [string]$Name) {
    return $createTool.Invoke($window, [object[]]@([Enum]::Parse($toolKindType, $Kind), $Name, $true, $null))
}

function Invoke-Tool($Tool, [string]$Label) {
    try { return $executeTool.Invoke($window, [object[]]@($Tool, $Label)) }
    catch [Reflection.TargetInvocationException] { throw $_.Exception.InnerException }
}

function Get-Output($Tool, [string]$Port) {
    $value = $null
    if (-not $Tool.TryGetOutputValue($Port, [ref]$value)) { throw "$($Tool.InstanceName).$Port has no output." }
    return $value
}

$disposeTools.Invoke($window, @()) | Out-Null
$flow.Clear()

# A line: rotated rectangle handles, bounds, polygon vertices and invalid-geometry rejection.
$createRotated = $roiType.GetMethod('CreateRotatedRectangle', $publicStatic)
$createPolygon = $roiType.GetMethod('CreatePolygon', $publicStatic, $null, [Type[]]@([double[]], [double[]]), $null)
$hitDetailed = $roiEditorType.GetMethod('HitTestDetailed', $publicStatic)
$transformDetailed = $roiEditorType.GetMethod('TransformDetailed', $publicStatic)
$offsetRoi = $roiEditorType.GetMethod('Offset', $publicStatic)
$rotated = $createRotated.Invoke($null, [object[]]@([double]50, [double]60, [double]0.3, [double]20, [double]10))
$rotatePoint = [Drawing.PointF]::new([float](60 + 36 * [Math]::Cos(0.3)), [float](50 - 36 * [Math]::Sin(0.3)))
$rotateHit = $hitDetailed.Invoke($null, [object[]]@($rotated, $rotatePoint, [double]3))
Assert-True ($rotateHit.Handle.ToString() -eq 'Rotate') 'Rotated rectangle rotation handle hit testing failed.'
$axisPoint = [Drawing.PointF]::new([float](60 + 20 * [Math]::Cos(0.3)), [float](50 - 20 * [Math]::Sin(0.3)))
$axisHit = $hitDetailed.Invoke($null, [object[]]@($rotated, $axisPoint, [double]3))
Assert-True ($axisHit.Handle.ToString() -eq 'Length1End') 'Rotated rectangle length handle hit testing failed.'
$rotateHandle = [Enum]::Parse($roiHandleType, 'Rotate')
$rotated90 = $transformDetailed.Invoke($null, [object[]]@($rotated, $rotateHandle, [int]-1, $rotatePoint, [Drawing.PointF]::new(60, 15), [int]160, [int]110))
Assert-True ([Math]::Abs($rotated90.Phi - ([Math]::PI / 2)) -lt 0.001) 'Rotated rectangle angle transform returned the wrong phi.'
$lengthHandle = [Enum]::Parse($roiHandleType, 'Length1End')
$longerPoint = [Drawing.PointF]::new([float](60 + 35 * [Math]::Cos(0.3)), [float](50 - 35 * [Math]::Sin(0.3)))
$longer = $transformDetailed.Invoke($null, [object[]]@($rotated, $lengthHandle, [int]-1, $axisPoint, $longerPoint, [int]160, [int]110))
Assert-True ([Math]::Abs($longer.Length1 - 35) -lt 0.01) 'Rotated rectangle width adjustment failed.'
$bounded = $offsetRoi.Invoke($null, [object[]]@($longer, [double]500, [double]500, [int]160, [int]110))
foreach ($corner in $bounded.GetRotatedRectangleCorners()) {
    Assert-True ($corner.X -ge -0.01 -and $corner.X -le 159.01 -and $corner.Y -ge -0.01 -and $corner.Y -le 109.01) 'Rotated rectangle move escaped image bounds.'
}

$rows = [double[]]@(10, 10, 40, 40)
$columns = [double[]]@(10, 40, 40, 10)
$polygon = $createPolygon.Invoke($null, [object[]]@($rows, $columns))
$vertexHit = $hitDetailed.Invoke($null, [object[]]@($polygon, [Drawing.PointF]::new(10, 10), [double]3))
Assert-True ($vertexHit.Handle.ToString() -eq 'PolygonVertex' -and $vertexHit.VertexIndex -eq 0) 'Polygon vertex handle did not expose its stable index.'
$vertexHandle = [Enum]::Parse($roiHandleType, 'PolygonVertex')
$editedPolygon = $transformDetailed.Invoke($null, [object[]]@($polygon, $vertexHandle, [int]0, [Drawing.PointF]::new(10, 10), [Drawing.PointF]::new(12, 12), [int]160, [int]110))
Assert-True ($editedPolygon.PolygonRows[0] -eq 12 -and $editedPolygon.PolygonColumns[0] -eq 12) 'Polygon vertex adjustment failed.'
$invalidPolygonRejected = $false
try {
    $transformDetailed.Invoke($null, [object[]]@($polygon, $vertexHandle, [int]1, [Drawing.PointF]::new(40, 10), [Drawing.PointF]::new(30, 50), [int]160, [int]110)) | Out-Null
}
catch {
    $invalidPolygonRejected = -not [string]::IsNullOrWhiteSpace($_.Exception.Message)
}
Assert-True $invalidPolygonRejected 'Self-intersecting polygon edit was not rejected clearly.'

# B line: Image -> Filter -> Threshold -> Morphology -> RegionFeatureFilter -> Gray/Edge.
$featureCatalog = $catalog | Where-Object Kind -eq ([Enum]::Parse($toolKindType, 'RegionFeatureFilter')) | Select-Object -First 1
Assert-True ($null -ne $featureCatalog -and -not [string]::IsNullOrWhiteSpace($featureCatalog.Category)) 'Region feature filter is missing from the visible image-processing toolbox.'
$source = New-Tool 'ImageSource' 'S14_Source'
$filter = New-Tool 'ImageFilter' 'S14_Filter'
$threshold = New-Tool 'ImageThreshold' 'S14_Threshold'
$morphology = New-Tool 'RegionMorphology' 'S14_Morphology'
$feature = New-Tool 'RegionFeatureFilter' 'S14_FeatureFilter'
$gray = New-Tool 'GrayStat' 'S14_Gray'
$edge = New-Tool 'EdgeMeasure' 'S14_Edge'
$source.Parameters.LocalImagePath = $imagePath
$source.Parameters.LocalImageSerialNumber = 'S14-FEATURE-001'
$filter.Parameters.ImageFilterMode = 'Mean'
$filter.Parameters.ImageFilterMaskWidth = 3
$filter.Parameters.ImageFilterMaskHeight = 3
$threshold.Parameters.ImageThresholdMinGray = 150
$threshold.Parameters.ImageThresholdMaxGray = 255
$morphology.Parameters.RegionMorphologyMode = 'OpeningCircle'
$morphology.Parameters.RegionMorphologyRadius = 1.0
$feature.Parameters.RegionFeature = 'Area'
$feature.Parameters.RegionFeatureMin = 300
$feature.Parameters.RegionFeatureMax = 550
$edge.Parameters.EdgeThreshold = 5
$flow.Add($source); $flow.Add($filter); $flow.Add($threshold); $flow.Add($morphology); $flow.Add($feature); $flow.Add($gray); $flow.Add($edge)
$filter.SetInputBinding('Image', $source.ToolId, 'Image')
$threshold.SetInputBinding('Image', $filter.ToolId, 'Image')
$morphology.SetInputBinding('Region', $threshold.ToolId, 'Region')
$feature.SetInputBinding('Region', $morphology.ToolId, 'Region')
$gray.SetInputBinding('Image', $filter.ToolId, 'Image')
$gray.SetInputBinding('ROI', $feature.ToolId, 'Region')
$edge.SetInputBinding('Image', $filter.ToolId, 'Image')
$edge.SetInputBinding('ROI', $feature.ToolId, 'Region')

$upstreamRejected = $false
try { Invoke-Tool $feature 'S14 upstream not run' | Out-Null }
catch { $upstreamRejected = -not [string]::IsNullOrWhiteSpace($_.Exception.Message) }
Assert-True $upstreamRejected 'Region feature filter must reject an upstream Region that has not run.'
Invoke-Tool $source 'S14 source' | Out-Null
Invoke-Tool $filter 'S14 filter' | Out-Null
Invoke-Tool $threshold 'S14 threshold' | Out-Null
Invoke-Tool $morphology 'S14 morphology' | Out-Null

$flowToolList.SelectedItem = $feature
$openConfiguration.Invoke($window, @()) | Out-Null
Assert-True ([object]::ReferenceEquals($rightTabs.SelectedItem, $dockTab)) 'Region feature filter did not open in the docked configuration workbench.'
Assert-True ($dockFeaturePanel.Visibility.ToString() -eq 'Visible' -and $dockInputRows.Count -eq 1) 'Feature parameter panel or typed Region input is not visible.'
$dockFeatureCombo.SelectedValue = 'Circularity'
$dockFeatureMin.Text = '0.5'
$dockFeatureMax.Text = '1.2'
Assert-True (-not [bool]$applyDock.Invoke($window, @($false))) 'Circularity above 1 must be rejected in the dock.'
Assert-True ($dockValidation.Text.Contains('0') -and $dockValidation.Text.Contains('1')) 'Circularity validation is not actionable.'
$dockFeatureCombo.SelectedValue = 'Area'
$dockFeatureMin.Text = '300'
$dockFeatureMax.Text = '550'
Assert-True ([bool]$applyDock.Invoke($window, @($false))) 'Applying a valid Region feature draft failed.'

$results = @{}
$ranges = @{
    Area = @(300.0, 550.0)
    Width = @(35.0, 45.0)
    Height = @(35.0, 45.0)
    Circularity = @(0.70, 1.0)
}
foreach ($mode in @('Area', 'Width', 'Height', 'Circularity')) {
    $feature.Parameters.RegionFeature = $mode
    $feature.Parameters.RegionFeatureMin = [double]$ranges[$mode][0]
    $feature.Parameters.RegionFeatureMax = [double]$ranges[$mode][1]
    Invoke-Tool $feature ('S14 ' + $mode) | Out-Null
    Assert-True ($feature.ResultCode -eq 'OK') ('Region feature mode failed: ' + $mode)
    $count = [int][double](Get-Output $feature 'RegionCount')
    Assert-True ($count -eq 1) ('Expected exactly one selected component for ' + $mode + ', got ' + $count)
    Assert-True ([string](Get-Output $feature 'Feature') -ne '' -and [string](Get-Output $feature 'Range') -match '\.\.') ('Feature outputs are incomplete for ' + $mode)
    $results[$mode] = [double](Get-Output $feature 'Area')
}

$feature.Parameters.RegionFeature = 'Area'
$feature.Parameters.RegionFeatureMin = 5000
$feature.Parameters.RegionFeatureMax = 6000
Invoke-Tool $feature 'S14 empty feature selection' | Out-Null
$emptyRegion = Get-Output $feature 'Region'
Assert-True ($feature.ResultCode -eq 'NG' -and [int][double](Get-Output $feature 'RegionCount') -eq 0 -and $emptyRegion.ObjectCount -eq 0) 'An empty feature selection must publish a managed empty Region and NG.'

$feature.Parameters.RegionFeature = 'Area'
$feature.Parameters.RegionFeatureMin = 300
$feature.Parameters.RegionFeatureMax = 550
Invoke-Tool $feature 'S14 downstream Region' | Out-Null
$liveRegion = Get-Output $feature 'Region'
Invoke-Tool $gray 'S14 gray selected Region' | Out-Null
Invoke-Tool $edge 'S14 edge selected Region' | Out-Null
Assert-True ($gray.ResultCode -eq 'OK' -and $edge.ResultCode -eq 'OK') 'Gray/Edge did not consume RegionFeatureFilter.Region.'

$rotatedLayer = $addRoiLayer.Invoke($window, [object[]]@($rotated, 'Rotated_Search', $gray))
$polygonLayer = $addRoiLayer.Invoke($window, [object[]]@($polygon, 'Polygon_Search', $edge))
$roiLayerList.SelectedItem = $rotatedLayer
$beforeCopy = $roiLayers.Count
$copyRoi.Invoke($window, @($null, $null)) | Out-Null
Assert-True ($roiLayers.Count -eq $beforeCopy + 1) 'Copying a rotated ROI did not create an independent layer.'
$rotatedCopy = $roiLayerList.SelectedItem
Assert-True ($rotatedCopy.Geometry.ShapeType.ToString() -eq 'RotatedRectangle' -and $rotatedCopy.RoiId -ne $rotatedLayer.RoiId) 'Rotated ROI copy lost geometry or identity.'
$roiLayerList.SelectedItem = $polygonLayer
$toggleLock.Invoke($window, @($null, $null)) | Out-Null
Assert-True $polygonLayer.IsLocked 'Polygon lock action failed.'

$recipe = $captureRecipe.Invoke($window, @())
$recipePath = Join-Path $tempRoot 's14-roundtrip.json'
$recipeService.SaveRecipe($recipePath, $recipe)
$loaded = $recipeService.LoadRecipe($recipePath)
$featureRecipe = $loaded.ToolFlow | Where-Object ToolId -eq $feature.ToolId
$rotatedRecipe = $loaded.RoiLayers | Where-Object RoiId -eq $rotatedLayer.RoiId
$polygonRecipe = $loaded.RoiLayers | Where-Object RoiId -eq $polygonLayer.RoiId
Assert-True ($featureRecipe.Parameters.RegionFeature -eq 'Area' -and $featureRecipe.Parameters.RegionFeatureMin -eq 300 -and $featureRecipe.Parameters.RegionFeatureMax -eq 550) 'Region feature parameters did not round-trip.'
Assert-True (($featureRecipe.InputBindings | Where-Object TargetPortName -eq 'Region').SourceToolId -eq $morphology.ToolId) 'Region feature binding did not round-trip.'
Assert-True ($rotatedRecipe.Geometry.ShapeType -eq 'RotatedRectangle' -and [Math]::Abs($rotatedRecipe.Geometry.Phi - 0.3) -lt 0.001 -and $rotatedRecipe.Geometry.Length1 -eq 20 -and $rotatedRecipe.Geometry.Length2 -eq 10) 'Rotated ROI geometry did not round-trip.'
Assert-True ($polygonRecipe.Geometry.ShapeType -eq 'Polygon' -and $polygonRecipe.IsLocked -and $polygonRecipe.Geometry.PolygonRows.Count -eq 4) 'Polygon geometry or lock state did not round-trip.'

$feature.Parameters.RegionFeatureMin = 600
$feature.Parameters.RegionFeatureMax = 300
$invalidRangeRejected = $false
try { Invoke-Tool $feature 'S14 invalid range' | Out-Null }
catch { $invalidRangeRejected = -not [string]::IsNullOrWhiteSpace($_.Exception.Message) }
Assert-True $invalidRangeRejected 'Invalid Region feature range was not rejected clearly.'
Assert-True $liveRegion.IsDisposed 'A Region feature rerun must dispose its previous managed Region output.'

$feature.Parameters.RegionFeatureMin = 300
$feature.Parameters.RegionFeatureMax = 550
Invoke-Tool $feature 'S14 lifecycle restore' | Out-Null
$restoredRegion = Get-Output $feature 'Region'
Invoke-Tool $threshold 'S14 upstream rerun invalidation' | Out-Null
Assert-True ($restoredRegion.IsDisposed -and $feature.ResultCode -eq '--' -and $gray.ResultCode -eq '--') 'Threshold rerun must invalidate the downstream feature/Gray/Edge chain.'
Invoke-Tool $morphology 'S14 morphology restore' | Out-Null
$releasedMorphology = Get-Output $morphology 'Region'
$releasedMorphology.Dispose()
$releasedRejected = $false
try { Invoke-Tool $feature 'S14 released upstream Region' | Out-Null }
catch { $releasedRejected = $_.Exception.Message.Contains('Region') }
Assert-True $releasedRejected 'A released upstream Region snapshot must be rejected clearly.'

$threshold.Parameters.ImageThresholdMinGray = 250
$threshold.Parameters.ImageThresholdMaxGray = 255
Invoke-Tool $threshold 'S14 NG threshold' | Out-Null
Assert-True ($threshold.ResultCode -eq 'NG') 'Empty threshold fixture should produce NG.'
$feature.SetInputBinding('Region', $threshold.ToolId, 'Region')
$ngRejected = $false
try { Invoke-Tool $feature 'S14 NG upstream' | Out-Null }
catch { $ngRejected = $_.Exception.Message.Contains('OK') }
Assert-True $ngRejected 'Region feature filter must reject an NG upstream Region.'

$disposeTools.Invoke($window, @()) | Out-Null
$flow.Clear()
$badFeature = New-Tool 'RegionFeatureFilter' 'Bad_Order_Feature'
$lateThreshold = New-Tool 'ImageThreshold' 'Late_Threshold'
$badFeature.SetInputBinding('Region', $lateThreshold.ToolId, 'Region')
$flow.Add($badFeature); $flow.Add($lateThreshold)
$orderRejected = $false
try { Invoke-Tool $badFeature 'S14 bad order' | Out-Null } catch { $orderRejected = $true }
Assert-True ($orderRejected -and -not [string]::IsNullOrWhiteSpace($badFeature.ErrorMessage)) 'A Region source after the feature filter must be rejected.'

$disposeTools.Invoke($window, @()) | Out-Null
$flow.Clear()
$imageSource = New-Tool 'ImageSource' 'Image_Type_Source'
$badType = New-Tool 'RegionFeatureFilter' 'Bad_Type_Feature'
$badType.SetInputBinding('Region', $imageSource.ToolId, 'Image')
$flow.Add($imageSource); $flow.Add($badType)
$typeRejected = $false
try { Invoke-Tool $badType 'S14 bad type' | Out-Null } catch { $typeRejected = $true }
Assert-True ($typeRejected -and -not [string]::IsNullOrWhiteSpace($badType.ErrorMessage)) 'An Image-to-Region feature binding must be rejected.'

$rotated.Dispose(); $rotated90.Dispose(); $longer.Dispose(); $bounded.Dispose(); $polygon.Dispose(); $editedPolygon.Dispose()
$disposeTools.Invoke($window, @()) | Out-Null
$flow.Clear()
$mainType.GetField('recipeCloseConfirmed', $private).SetValue($window, $true)
$window.Close()

Write-Output ('S14_ADVANCED_ROI_FEATURE=PASS; Rotated=HANDLE/ROTATE/SIZE/BOUNDS/COPY/RECIPE; Polygon=VERTEX/INVALID/LOCK/RECIPE; Area={0:F0}; Width={1:F0}; Height={2:F0}; Circularity={3:F0}; Gray/Edge=PASS; Invalid=PASS; Recipe=PASS; Lifecycle=PASS' -f $results.Area, $results.Width, $results.Height, $results.Circularity)
