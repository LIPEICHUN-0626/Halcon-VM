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

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s13-smoke-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($tempRoot) | Out-Null
$imagePath = Join-Path $tempRoot 'morphology-input.png'
$bitmap = New-Object Drawing.Bitmap 120, 90
try {
    for ($row = 0; $row -lt $bitmap.Height; $row++) {
        for ($column = 0; $column -lt $bitmap.Width; $column++) {
            $insideFirst = $column -ge 12 -and $column -le 42 -and $row -ge 15 -and $row -le 52
            $insideSecond = $column -ge 70 -and $column -le 105 -and $row -ge 25 -and $row -le 67
            $hole = ($column -ge 25 -and $column -le 29 -and $row -ge 31 -and $row -le 35) -or
                    ($column -ge 86 -and $column -le 91 -and $row -ge 43 -and $row -le 48)
            $speck = $column -ge 55 -and $column -le 56 -and $row -ge 12 -and $row -le 13
            $gray = if (($insideFirst -or $insideSecond -or $speck) -and -not $hole) { 230 } else { 15 }
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
$private = [Reflection.BindingFlags]'Instance,NonPublic'
$allFields = [Reflection.BindingFlags]'Instance,NonPublic,Public'
$constructorArgs = New-Object 'object[]' 1
$constructorArgs[0] = $tempRoot.ToString()
$recipeService = $recipeServiceType.GetConstructor([Type[]]@([string])).Invoke($constructorArgs)
$logger = $loggerType.GetConstructor([Type[]]@([string])).Invoke($constructorArgs)
$windowConstructor = $mainType.GetConstructor($private, $null, [Type[]]@($recipeServiceType, $loggerType), $null)
$window = $windowConstructor.Invoke([object[]]@($recipeService, $logger))

$flow = $mainType.GetField('flowTools', $private).GetValue($window)
$catalog = $mainType.GetField('toolCatalog', $private).GetValue($window)
$createTool = $mainType.GetMethod('CreateFlowTool', $private)
$executeTool = $mainType.GetMethod('ExecuteFlowTool', $private)
$disposeTools = $mainType.GetMethod('DisposeFlowTools', $private)
$openConfiguration = $mainType.GetMethod('OpenSelectedToolConfiguration', $private)
$applyDock = $mainType.GetMethod('ApplyDockConfigurationDraft', $private)
$captureRecipe = $mainType.GetMethod('CaptureRecipe', $private)
$refreshUi = $mainType.GetMethod('RefreshUiState', $private)
$expandImage = $mainType.GetMethod('ImageWorkspaceExpandButton_Click', $private)
$overlayChanged = $mainType.GetMethod('ImageOverlayVisibility_Changed', $private)

$flowToolList = $mainType.GetField('FlowToolList', $allFields).GetValue($window)
$imagePanel = $mainType.GetField('ImageWorkspacePanel', $allFields).GetValue($window)
$rightPanelColumn = $mainType.GetField('RightPanelColumn', $allFields).GetValue($window)
$expandButton = $mainType.GetField('ImageWorkspaceExpandButton', $allFields).GetValue($window)
$showRoi = $mainType.GetField('ShowRoiOverlayCheckBox', $allFields).GetValue($window)
$showResult = $mainType.GetField('ShowResultOverlayCheckBox', $allFields).GetValue($window)
$resultLegend = $mainType.GetField('OverlayStatusText', $allFields).GetValue($window)
$roiLegend = $mainType.GetField('RoiOverlayLegendText', $allFields).GetValue($window)
$emptyState = $mainType.GetField('ImageEmptyStateBorder', $allFields).GetValue($window)
$halconHost = $mainType.GetField('HalconHost', $allFields).GetValue($window)
$rightTabs = $mainType.GetField('RightTabs', $allFields).GetValue($window)
$dockTab = $mainType.GetField('DockConfigurationTab', $allFields).GetValue($window)
$dockMorphPanel = $mainType.GetField('DockRegionMorphologyPanel', $allFields).GetValue($window)
$dockMorphMode = $mainType.GetField('DockRegionMorphologyModeComboBox', $allFields).GetValue($window)
$dockMorphRadius = $mainType.GetField('DockRegionMorphologyRadiusTextBox', $allFields).GetValue($window)
$dockValidation = $mainType.GetField('DockValidationText', $allFields).GetValue($window)
$dockInputRows = $mainType.GetField('dockInputPortRows', $private).GetValue($window)
$showRoiField = $mainType.GetField('showRoiOverlay', $private)
$showResultField = $mainType.GetField('showResultOverlay', $private)
$overlayObjectCountField = $mainType.GetField('toolOverlayObjectCount', $private)

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

# A line: workspace expansion restores the exact grid placement and overlay switches keep data alive.
$initialRightWidth = $rightPanelColumn.Width.Value
$expandImage.Invoke($window, [object[]]@($expandButton, [Windows.RoutedEventArgs]::new())) | Out-Null
Assert-True ([Windows.Controls.Grid]::GetColumn($imagePanel) -eq 0) 'Image maximize did not move the image panel to the full workspace origin.'
Assert-True ([Windows.Controls.Grid]::GetColumnSpan($imagePanel) -eq 5 -and [Windows.Controls.Grid]::GetRowSpan($imagePanel) -eq 3) 'Image maximize did not span the complete main workspace.'
$expandImage.Invoke($window, [object[]]@($expandButton, [Windows.RoutedEventArgs]::new())) | Out-Null
Assert-True ([Windows.Controls.Grid]::GetColumn($imagePanel) -eq 4 -and [Windows.Controls.Grid]::GetColumnSpan($imagePanel) -eq 1) 'Image workspace did not restore its original grid column.'
Assert-True ($rightPanelColumn.Width.Value -eq $initialRightWidth) 'Image maximize/restore changed the saved right-panel width.'

$morphCatalog = $catalog | Where-Object Kind -eq ([Enum]::Parse($toolKindType, 'RegionMorphology')) | Select-Object -First 1
Assert-True ($null -ne $morphCatalog -and -not [string]::IsNullOrWhiteSpace($morphCatalog.Category)) 'Region morphology is missing from the visible image-processing toolbox category.'

# B line: source -> filter -> threshold.Region -> morphology.Region -> Gray/Edge ROI.
$source = New-Tool 'ImageSource' 'S13_Source'
$filter = New-Tool 'ImageFilter' 'S13_Filter'
$threshold = New-Tool 'ImageThreshold' 'S13_Threshold'
$morphology = New-Tool 'RegionMorphology' 'S13_Morphology'
$gray = New-Tool 'GrayStat' 'S13_Gray'
$edge = New-Tool 'EdgeMeasure' 'S13_Edge'
$source.Parameters.LocalImagePath = $imagePath
$source.Parameters.LocalImageSerialNumber = 'S13-MORPH-001'
$filter.Parameters.ImageFilterMaskWidth = 3
$filter.Parameters.ImageFilterMaskHeight = 3
$threshold.Parameters.ImageThresholdMinGray = 150
$threshold.Parameters.ImageThresholdMaxGray = 255
$morphology.Parameters.RegionMorphologyMode = 'OpeningCircle'
$morphology.Parameters.RegionMorphologyRadius = 2.5
$edge.Parameters.EdgeThreshold = 5
$flow.Add($source); $flow.Add($filter); $flow.Add($threshold); $flow.Add($morphology); $flow.Add($gray); $flow.Add($edge)
$filter.SetInputBinding('Image', $source.ToolId, 'Image')
$threshold.SetInputBinding('Image', $filter.ToolId, 'Image')
$morphology.SetInputBinding('Region', $threshold.ToolId, 'Region')
$gray.SetInputBinding('Image', $filter.ToolId, 'Image')
$gray.SetInputBinding('ROI', $morphology.ToolId, 'Region')
$edge.SetInputBinding('Image', $filter.ToolId, 'Image')
$edge.SetInputBinding('ROI', $morphology.ToolId, 'Region')

$upstreamRejected = $false
try { Invoke-Tool $morphology 'S13 upstream not run' | Out-Null }
catch { $upstreamRejected = $_.Exception.Message.Contains('OK') -or -not [string]::IsNullOrWhiteSpace($_.Exception.Message) }
Assert-True $upstreamRejected 'Morphology must reject an upstream Region that has not run successfully.'

Invoke-Tool $source 'S13 source' | Out-Null
Invoke-Tool $filter 'S13 filter' | Out-Null
Invoke-Tool $threshold 'S13 threshold' | Out-Null
$thresholdArea = [double](Get-Output $threshold 'Area')
Assert-True ($threshold.ResultCode -eq 'OK' -and $thresholdArea -gt 2000) 'Threshold did not produce the expected two-object Region input.'

$flowToolList.SelectedItem = $morphology
$openConfiguration.Invoke($window, @()) | Out-Null
Assert-True ([object]::ReferenceEquals($rightTabs.SelectedItem, $dockTab)) 'Morphology did not open in the docked configuration workbench.'
Assert-True ($dockMorphPanel.Visibility.ToString() -eq 'Visible' -and $dockInputRows.Count -eq 1) 'Morphology parameter panel or typed Region input is not visible.'
$dockMorphMode.SelectedValue = 'DilationCircle'
$dockMorphRadius.Text = '0.1'
Assert-True (-not [bool]$applyDock.Invoke($window, @($false))) 'An invalid morphology radius must be rejected in the dock.'
Assert-True ($dockValidation.Text.Contains('0.5') -and $dockValidation.Text.Contains('100')) 'Morphology radius validation is not actionable.'
$dockMorphRadius.Text = '2.5'
Assert-True ([bool]$applyDock.Invoke($window, @($false))) 'Applying a valid morphology draft failed.'

$areas = @{}
foreach ($mode in @('OpeningCircle', 'ClosingCircle', 'DilationCircle', 'ErosionCircle')) {
    $morphology.Parameters.RegionMorphologyMode = $mode
    $morphology.Parameters.RegionMorphologyRadius = 2.5
    Invoke-Tool $morphology ("S13 " + $mode) | Out-Null
    Assert-True ($morphology.ResultCode -eq 'OK') ("Morphology mode failed: " + $mode)
    $areas[$mode] = [double](Get-Output $morphology 'Area')
    $snapshot = Get-Output $morphology 'Region'
    Assert-True ($snapshot.ObjectCount -eq [int][double](Get-Output $morphology 'RegionCount')) ("RegionCount mismatch: " + $mode)
    Assert-True ([string](Get-Output $morphology 'Operation') -match 'R=2.5px') ("Operation summary is incomplete: " + $mode)
}
Assert-True ($areas.DilationCircle -gt $thresholdArea) 'Dilation should increase the segmented area.'
Assert-True ($areas.ErosionCircle -lt $thresholdArea) 'Erosion should decrease the segmented area.'
Assert-True ($areas.ClosingCircle -gt $areas.OpeningCircle) 'Closing should preserve/fill more area than opening for the hole-and-speck fixture.'

$morphology.Parameters.RegionMorphologyMode = 'OpeningCircle'
Invoke-Tool $morphology 'S13 downstream region' | Out-Null
$liveRegion = Get-Output $morphology 'Region'
Invoke-Tool $gray 'S13 gray region' | Out-Null
Invoke-Tool $edge 'S13 edge region' | Out-Null
Assert-True ($gray.ResultCode -eq 'OK' -and $edge.ResultCode -eq 'OK') 'Gray/Edge did not consume RegionMorphology.Region.'

$refreshUi.Invoke($window, @()) | Out-Null
Assert-True ($resultLegend.Text.Contains('S13_Edge.Contours') -and [int]$overlayObjectCountField.GetValue($window) -gt 0) 'Result legend does not expose the actual latest result source and object count.'
$visibleLegend = $resultLegend.Text
$showResult.IsChecked = $false
$overlayChanged.Invoke($window, [object[]]@($showResult, [Windows.RoutedEventArgs]::new())) | Out-Null
Assert-True (-not [bool]$showResultField.GetValue($window) -and $resultLegend.Text -ne $visibleLegend -and -not $liveRegion.IsDisposed) 'Hiding the result overlay must retain the managed Region output.'
$showRoi.IsChecked = $false
$overlayChanged.Invoke($window, [object[]]@($showRoi, [Windows.RoutedEventArgs]::new())) | Out-Null
Assert-True (-not [bool]$showRoiField.GetValue($window) -and -not $liveRegion.IsDisposed) 'Hiding ROI overlays must not release algorithm output.'
$showResult.IsChecked = $true
$showRoi.IsChecked = $true
$overlayChanged.Invoke($window, [object[]]@($showResult, [Windows.RoutedEventArgs]::new())) | Out-Null

$recipe = $captureRecipe.Invoke($window, @())
$recipePath = Join-Path $tempRoot 's13-roundtrip.json'
$recipeService.SaveRecipe($recipePath, $recipe)
$loaded = $recipeService.LoadRecipe($recipePath)
$morphRecipe = $loaded.ToolFlow | Where-Object ToolId -eq $morphology.ToolId
Assert-True ($morphRecipe.Parameters.RegionMorphologyMode -eq 'OpeningCircle' -and $morphRecipe.Parameters.RegionMorphologyRadius -eq 2.5) 'Morphology parameters did not round-trip.'
Assert-True (($morphRecipe.InputBindings | Where-Object TargetPortName -eq 'Region').SourceToolId -eq $threshold.ToolId) 'Morphology Region binding did not round-trip.'

$morphology.Parameters.RegionMorphologyRadius = 0.1
$invalidRadiusRejected = $false
try { Invoke-Tool $morphology 'S13 invalid radius' | Out-Null }
catch { $invalidRadiusRejected = $_.Exception.Message.Contains('0.5') -and $_.Exception.Message.Contains('100') }
Assert-True $invalidRadiusRejected 'Invalid morphology radius was not rejected clearly.'
Assert-True $liveRegion.IsDisposed 'A morphology rerun must dispose its previous managed Region output.'

$morphology.Parameters.RegionMorphologyRadius = 2.5
Invoke-Tool $morphology 'S13 lifecycle restore' | Out-Null
$restoredRegion = Get-Output $morphology 'Region'
Invoke-Tool $threshold 'S13 upstream rerun invalidation' | Out-Null
Assert-True ($restoredRegion.IsDisposed -and $morphology.ResultCode -eq '--' -and $gray.ResultCode -eq '--') 'Threshold rerun must release and invalidate the downstream morphology chain.'
$refreshUi.Invoke($window, @()) | Out-Null
Assert-True ($morphology.ResultCode -eq '--' -and $resultLegend.Text -ne $visibleLegend) 'The image result legend must identify a stale overlay after upstream rerun.'

$releasedThreshold = Get-Output $threshold 'Region'
$releasedThreshold.Dispose()
$releasedRejected = $false
try { Invoke-Tool $morphology 'S13 released upstream Region' | Out-Null }
catch { $releasedRejected = $_.Exception.Message.Contains('Region') }
Assert-True $releasedRejected 'A released upstream Region snapshot must be rejected clearly.'

$threshold.Parameters.ImageThresholdMinGray = 250
$threshold.Parameters.ImageThresholdMaxGray = 255
Invoke-Tool $threshold 'S13 empty threshold' | Out-Null
Assert-True ($threshold.ResultCode -eq 'NG') 'Empty threshold fixture should produce NG.'
$ngRejected = $false
try { Invoke-Tool $morphology 'S13 NG upstream' | Out-Null }
catch { $ngRejected = $_.Exception.Message.Contains('OK') }
Assert-True $ngRejected 'Morphology must reject an NG upstream Region.'

# Real order/type validation.
$disposeTools.Invoke($window, @()) | Out-Null
$flow.Clear()
$badMorph = New-Tool 'RegionMorphology' 'Bad_Order_Morph'
$lateThreshold = New-Tool 'ImageThreshold' 'Late_Threshold'
$badMorph.SetInputBinding('Region', $lateThreshold.ToolId, 'Region')
$flow.Add($badMorph); $flow.Add($lateThreshold)
$orderRejected = $false
try { Invoke-Tool $badMorph 'S13 bad order' | Out-Null } catch { $orderRejected = $true }
Assert-True ($orderRejected -and -not [string]::IsNullOrWhiteSpace($badMorph.ErrorMessage)) 'A Region source after morphology must be rejected.'

$disposeTools.Invoke($window, @()) | Out-Null
$flow.Clear()
$sourceType = New-Tool 'ImageSource' 'Image_Type_Source'
$badType = New-Tool 'RegionMorphology' 'Bad_Type_Morph'
$badType.SetInputBinding('Region', $sourceType.ToolId, 'Image')
$flow.Add($sourceType); $flow.Add($badType)
$typeRejected = $false
try { Invoke-Tool $badType 'S13 bad type' | Out-Null } catch { $typeRejected = $true }
Assert-True ($typeRejected -and -not [string]::IsNullOrWhiteSpace($badType.ErrorMessage)) 'An Image-to-Region binding must be rejected.'

$disposeTools.Invoke($window, @()) | Out-Null
$flow.Clear()
$flowToolList.SelectedItem = $null
$refreshUi.Invoke($window, @()) | Out-Null
Assert-True ($emptyState.Visibility.ToString() -eq 'Visible' -and $halconHost.Visibility.ToString() -eq 'Collapsed') 'No-image state must replace the empty HALCON host with an actionable message.'

$mainType.GetField('recipeCloseConfirmed', $private).SetValue($window, $true)
$window.Close()

Write-Output ('S13_IMAGE_MORPHOLOGY=PASS; ExpandRestore=PASS; OverlayControls=PASS; Legend=PASS; ThresholdArea={0:F0}; Open={1:F0}; Close={2:F0}; Dilate={3:F0}; Erode={4:F0}; Gray/Edge=PASS; Invalid=PASS; NG=PASS; Recipe=PASS; Lifecycle=PASS' -f $thresholdArea, $areas.OpeningCircle, $areas.ClosingCircle, $areas.DilationCircle, $areas.ErosionCircle)
