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

function New-FixtureImage([string]$Path, [int]$Left, [int]$Top, [int]$Right, [int]$Bottom) {
    $bitmap = New-Object Drawing.Bitmap 100, 100
    try {
        for ($row = 0; $row -lt 100; $row++) {
            for ($column = 0; $column -lt 100; $column++) {
                $gray = if ($column -ge $Left -and $column -le $Right -and $row -ge $Top -and $row -le $Bottom) { 230 } else { 10 }
                $bitmap.SetPixel($column, $row, [Drawing.Color]::FromArgb($gray, $gray, $gray))
            }
        }
        $bitmap.Save($Path, [Drawing.Imaging.ImageFormat]::Png)
    }
    finally { $bitmap.Dispose() }
}

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s15-smoke-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($tempRoot) | Out-Null
$imageA = Join-Path $tempRoot 'region-a.png'
$imageB = Join-Path $tempRoot 'region-b.png'
$imageC = Join-Path $tempRoot 'region-c.png'
New-FixtureImage $imageA 10 10 49 49
New-FixtureImage $imageB 30 30 69 69
New-FixtureImage $imageC 75 75 89 89

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
$refresh = $mainType.GetMethod('RefreshUiState', $private)
$openConfiguration = $mainType.GetMethod('OpenSelectedToolConfiguration', $private)
$applyDock = $mainType.GetMethod('ApplyDockConfigurationDraft', $private)
$captureRecipe = $mainType.GetMethod('CaptureRecipe', $private)
$deleteTool = $mainType.GetMethod('DeleteToolButton_Click', $private)
$flowToolList = $mainType.GetField('FlowToolList', $allFields).GetValue($window)
$rightTabs = $mainType.GetField('RightTabs', $allFields).GetValue($window)
$dockTab = $mainType.GetField('DockConfigurationTab', $allFields).GetValue($window)
$dockSetPanel = $mainType.GetField('DockRegionSetOperationPanel', $allFields).GetValue($window)
$dockSetCombo = $mainType.GetField('DockRegionSetOperationComboBox', $allFields).GetValue($window)
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
$sourceA = New-Tool 'ImageSource' 'Source_A'
$thresholdA = New-Tool 'ImageThreshold' 'Threshold_A'
$sourceB = New-Tool 'ImageSource' 'Source_B'
$thresholdB = New-Tool 'ImageThreshold' 'Threshold_B'
$sourceC = New-Tool 'ImageSource' 'Source_C'
$thresholdC = New-Tool 'ImageThreshold' 'Threshold_C'
$setTool = New-Tool 'RegionSetOperation' 'Region_Set'
$feature = New-Tool 'RegionFeatureFilter' 'Set_Feature'
$gray = New-Tool 'GrayStat' 'Set_Gray'
$edge = New-Tool 'EdgeMeasure' 'Set_Edge'

$sourceA.Parameters.LocalImagePath = $imageA
$sourceB.Parameters.LocalImagePath = $imageB
$sourceC.Parameters.LocalImagePath = $imageC
foreach ($threshold in @($thresholdA, $thresholdB, $thresholdC)) {
    $threshold.Parameters.ImageThresholdMinGray = 150
    $threshold.Parameters.ImageThresholdMaxGray = 255
}
$feature.Parameters.RegionFeature = 'Area'
$feature.Parameters.RegionFeatureMin = 1
$feature.Parameters.RegionFeatureMax = 10000
$gray.Parameters.GrayMin = 0
$gray.Parameters.GrayMax = 255
$edge.Parameters.EdgeThreshold = 5

foreach ($tool in @($sourceA, $thresholdA, $sourceB, $thresholdB, $sourceC, $thresholdC, $setTool, $feature, $gray, $edge)) { $flow.Add($tool) }
$thresholdA.SetInputBinding('Image', $sourceA.ToolId, 'Image')
$thresholdB.SetInputBinding('Image', $sourceB.ToolId, 'Image')
$thresholdC.SetInputBinding('Image', $sourceC.ToolId, 'Image')
$setTool.SetInputBinding('RegionA', $thresholdA.ToolId, 'Region')
$setTool.SetInputBinding('RegionB', $thresholdB.ToolId, 'Region')
$feature.SetInputBinding('Region', $setTool.ToolId, 'Region')
$gray.SetInputBinding('Image', $sourceA.ToolId, 'Image')
$gray.SetInputBinding('ROI', $setTool.ToolId, 'Region')
$edge.SetInputBinding('Image', $sourceA.ToolId, 'Image')
$edge.SetInputBinding('ROI', $setTool.ToolId, 'Region')

$catalogItem = $catalog | Where-Object Kind -eq ([Enum]::Parse($toolKindType, 'RegionSetOperation')) | Select-Object -First 1
Assert-True ($null -ne $catalogItem -and -not [string]::IsNullOrWhiteSpace($catalogItem.Category)) 'Region set operation is missing from the visible toolbox.'

$flowToolList.SelectedItem = $setTool
$refresh.Invoke($window, @()) | Out-Null
Assert-True ($setTool.FlowInputPorts.Count -eq 2) 'The flow card must expose two typed Region input chips.'
Assert-True (($setTool.FlowInputPorts | Where-Object PortName -eq 'RegionA').EndpointText.Contains('Threshold_A.Region')) 'RegionA endpoint is not visible on the flow card.'
Assert-True (($setTool.FlowInputPorts | Where-Object PortName -eq 'RegionB').EndpointText.Contains('Threshold_B.Region')) 'RegionB endpoint is not visible on the flow card.'
Assert-True (($setTool.FlowInputPorts | Where-Object StateKey -eq 'Waiting').Count -eq 2) 'Unrun Region inputs must be visibly waiting.'
Assert-True ($thresholdA.DependencyState -eq 'Upstream' -and $thresholdB.DependencyState -eq 'Upstream') 'Direct upstream dependency highlighting is incorrect.'
Assert-True ($feature.DependencyState -eq 'Downstream' -and $gray.DependencyState -eq 'Downstream' -and $edge.DependencyState -eq 'Downstream') 'Direct downstream dependency highlighting is incorrect.'

$upstreamRejected = $false
try { Invoke-Tool $setTool 'S15 upstream not run' | Out-Null } catch { $upstreamRejected = $_.Exception.Message.Contains('OK') }
Assert-True $upstreamRejected 'Region set operation must reject unrun upstream Regions.'

foreach ($tool in @($sourceA, $thresholdA, $sourceB, $thresholdB, $sourceC, $thresholdC)) { Invoke-Tool $tool ('S15 ' + $tool.InstanceName) | Out-Null }
$expectedAreas = @{ Union = 2800.0; Intersection = 400.0; Difference = 1200.0; SymmetricDifference = 2400.0 }
$actualAreas = @{}
foreach ($mode in @('Union', 'Intersection', 'Difference', 'SymmetricDifference')) {
    $setTool.Parameters.RegionSetOperationMode = $mode
    Invoke-Tool $setTool ('S15 ' + $mode) | Out-Null
    $area = [double](Get-Output $setTool 'Area')
    Assert-True ([Math]::Abs($area - $expectedAreas[$mode]) -lt 0.01) ("$mode area mismatch: $area")
    Assert-True ($setTool.ResultCode -eq 'OK' -and [int][double](Get-Output $setTool 'RegionCount') -ge 1) ("$mode did not publish a valid Region.")
    Assert-True (-not [string]::IsNullOrWhiteSpace([string](Get-Output $setTool 'Operation'))) ("$mode operation output is missing.")
    $actualAreas[$mode] = $area
}

$setTool.Parameters.RegionSetOperationMode = 'Union'
Invoke-Tool $setTool 'S15 downstream union' | Out-Null
Invoke-Tool $feature 'S15 feature consumer' | Out-Null
Invoke-Tool $gray 'S15 gray consumer' | Out-Null
Invoke-Tool $edge 'S15 edge consumer' | Out-Null
Assert-True ($feature.ResultCode -eq 'OK' -and $gray.ResultCode -eq 'OK' -and $edge.ResultCode -eq 'OK') 'Feature/Gray/Edge did not consume RegionSetOperation.Region.'
$refresh.Invoke($window, @()) | Out-Null
$regionOutputChip = $setTool.FlowOutputPorts | Where-Object PortName -eq 'Region'
Assert-True ($regionOutputChip.IsConnected -and $regionOutputChip.EndpointText.Contains('3')) 'The Region output chip does not show its three real consumers.'
Assert-True (($setTool.FlowInputPorts | Where-Object StateKey -eq 'Ready').Count -eq 2) 'Both Region inputs must be visibly ready after execution.'

$flowToolList.SelectedItem = $setTool
$openConfiguration.Invoke($window, @()) | Out-Null
Assert-True ([object]::ReferenceEquals($rightTabs.SelectedItem, $dockTab)) 'Region set operation did not open in the docked configuration workbench.'
Assert-True ($dockSetPanel.Visibility.ToString() -eq 'Visible' -and $dockInputRows.Count -eq 2) 'Set-operation parameter panel or typed dual inputs are not visible.'
$rowA = $dockInputRows | Where-Object PortName -eq 'RegionA'
$rowB = $dockInputRows | Where-Object PortName -eq 'RegionB'
$savedB = $rowB.SelectedSourceKey
$rowB.SelectedSourceKey = $rowA.SelectedSourceKey
Assert-True (-not [bool]$applyDock.Invoke($window, @($false))) 'The dock must reject two identical Region sources.'
Assert-True (-not [string]::IsNullOrWhiteSpace($dockValidation.Text)) 'Same-source validation is not actionable.'
$rowB.SelectedSourceKey = $savedB
$dockSetCombo.SelectedValue = 'Difference'
Assert-True ([bool]$applyDock.Invoke($window, @($false))) 'Applying a valid dual-Region draft failed.'

$recipe = $captureRecipe.Invoke($window, @())
$recipePath = Join-Path $tempRoot 's15-roundtrip.json'
$recipeService.SaveRecipe($recipePath, $recipe)
$loaded = $recipeService.LoadRecipe($recipePath)
$setRecipe = $loaded.ToolFlow | Where-Object ToolId -eq $setTool.ToolId
Assert-True ($setRecipe.Parameters.RegionSetOperationMode -eq 'Difference') 'Region set operation mode did not round-trip.'
Assert-True (($setRecipe.InputBindings | Where-Object TargetPortName -eq 'RegionA').SourceToolId -eq $thresholdA.ToolId) 'RegionA binding did not round-trip.'
Assert-True (($setRecipe.InputBindings | Where-Object TargetPortName -eq 'RegionB').SourceToolId -eq $thresholdB.ToolId) 'RegionB binding did not round-trip.'

$setTool.Parameters.RegionSetOperationMode = 'Intersection'
$setTool.SetInputBinding('RegionB', $thresholdC.ToolId, 'Region')
Invoke-Tool $setTool 'S15 empty intersection' | Out-Null
$emptyRegion = Get-Output $setTool 'Region'
Assert-True ($setTool.ResultCode -eq 'NG' -and [int][double](Get-Output $setTool 'RegionCount') -eq 0 -and $emptyRegion.ObjectCount -eq 0) 'An empty intersection must publish a managed empty Region and NG.'

$setTool.SetInputBinding('RegionB', $thresholdB.ToolId, 'Region')
$setTool.Parameters.RegionSetOperationMode = 'Union'
Invoke-Tool $setTool 'S15 lifecycle restore' | Out-Null
$liveRegion = Get-Output $setTool 'Region'
Invoke-Tool $thresholdA 'S15 upstream rerun' | Out-Null
Assert-True ($liveRegion.IsDisposed -and $setTool.ResultCode -eq '--' -and $feature.ResultCode -eq '--' -and $gray.ResultCode -eq '--') 'Upstream rerun must invalidate and dispose the entire downstream set chain.'
$refresh.Invoke($window, @()) | Out-Null
Assert-True (($setTool.FlowOutputPorts | Where-Object PortName -eq 'Region').StateKey -eq 'Waiting') 'Stale Region output was not visibly reset.'

$thresholdA.Parameters.ImageThresholdMinGray = 250
$thresholdA.Parameters.ImageThresholdMaxGray = 255
Invoke-Tool $thresholdA 'S15 NG upstream threshold' | Out-Null
Assert-True ($thresholdA.ResultCode -eq 'NG') 'The upstream NG fixture did not produce NG.'
$ngRejected = $false
try { Invoke-Tool $setTool 'S15 NG upstream' | Out-Null } catch { $ngRejected = -not [string]::IsNullOrWhiteSpace($_.Exception.Message) }
Assert-True $ngRejected 'Region set operation must reject an NG upstream Region.'
$thresholdA.Parameters.ImageThresholdMinGray = 150
Invoke-Tool $thresholdA 'S15 restore upstream threshold' | Out-Null
$releasedInput = Get-Output $thresholdB 'Region'
$releasedInput.Dispose()
$releasedRejected = $false
try { Invoke-Tool $setTool 'S15 released upstream' | Out-Null } catch { $releasedRejected = -not [string]::IsNullOrWhiteSpace($_.Exception.Message) }
Assert-True $releasedRejected 'Region set operation must reject a released upstream snapshot.'
Invoke-Tool $thresholdB 'S15 restore released upstream' | Out-Null

$setTool.SetInputBinding('RegionB', $thresholdA.ToolId, 'Region')
$sameSourceRejected = $false
try { Invoke-Tool $setTool 'S15 same source' | Out-Null } catch { $sameSourceRejected = -not [string]::IsNullOrWhiteSpace($_.Exception.Message) }
Assert-True $sameSourceRejected 'Programmatic execution must reject identical Region inputs.'

$setTool.SetInputBinding('RegionB', $sourceB.ToolId, 'Image')
$typeRejected = $false
try { Invoke-Tool $setTool 'S15 bad type' | Out-Null } catch { $typeRejected = -not [string]::IsNullOrWhiteSpace($_.Exception.Message) }
Assert-True $typeRejected 'Image-to-Region set binding must be rejected.'

$setTool.SetInputBinding('RegionB', $thresholdB.ToolId, 'Region')
$flowToolList.SelectedItem = $thresholdB
$deleteTool.Invoke($window, @($null, $null)) | Out-Null
$refresh.Invoke($window, @()) | Out-Null
Assert-True ($null -eq $setTool.GetInputBinding('RegionB')) 'Deleting an upstream module must remove its RegionB binding.'
Assert-True (($setTool.FlowInputPorts | Where-Object PortName -eq 'RegionB').StateKey -eq 'Missing') 'Deleted RegionB dependency is not visibly missing.'

$disposeTools.Invoke($window, @()) | Out-Null
$flow.Clear()
$badOrderSet = New-Tool 'RegionSetOperation' 'Bad_Order_Set'
$lateA = New-Tool 'ImageThreshold' 'Late_A'
$lateB = New-Tool 'ImageThreshold' 'Late_B'
$badOrderSet.SetInputBinding('RegionA', $lateA.ToolId, 'Region')
$badOrderSet.SetInputBinding('RegionB', $lateB.ToolId, 'Region')
$flow.Add($badOrderSet); $flow.Add($lateA); $flow.Add($lateB)
$orderRejected = $false
try { Invoke-Tool $badOrderSet 'S15 bad order' | Out-Null } catch { $orderRejected = -not [string]::IsNullOrWhiteSpace($_.Exception.Message) }
Assert-True $orderRejected 'A Region source after the set operation must be rejected.'

$disposeTools.Invoke($window, @()) | Out-Null
$flow.Clear()
$mainType.GetField('recipeCloseConfirmed', $private).SetValue($window, $true)
$window.Close()

Write-Output ('S15_FLOW_PORTS_REGION_SET=PASS; Ports=IN/OUT/CONSUMERS/HIGHLIGHT/WAITING/STALE/DELETE; Union={0:F0}; Intersection={1:F0}; Difference={2:F0}; Symmetric={3:F0}; Downstream=FEATURE/GRAY/EDGE; Invalid=UNRUN/NG/RELEASED/SAME/TYPE/ORDER; Empty=MANAGED_NG; Recipe=PASS; Lifecycle=PASS' -f $actualAreas.Union, $actualAreas.Intersection, $actualAreas.Difference, $actualAreas.SymmetricDifference)
