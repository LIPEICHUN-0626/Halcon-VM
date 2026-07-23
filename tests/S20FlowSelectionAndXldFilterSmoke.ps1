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
[Reflection.Assembly]::LoadFrom((Join-Path $halconRoot 'bin\dotnet35\halcondotnet.dll')) | Out-Null
$exePath = Join-Path $ProjectRoot 'bin\x64\Debug\HalconWinFormsDemo.exe'
if (-not (Test-Path -LiteralPath $exePath)) { throw "Build Debug x64 first: $exePath" }
$assembly = [Reflection.Assembly]::LoadFrom($exePath)

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw "Assertion failed: $Message" }
}

function Write-Pgm([string]$Path, [int]$Width, [int]$Height, [scriptblock]$PixelFactory) {
    $header = [Text.Encoding]::ASCII.GetBytes("P5`n$Width $Height`n255`n")
    $pixels = New-Object byte[] ($Width * $Height)
    for ($row = 0; $row -lt $Height; $row++) {
        for ($column = 0; $column -lt $Width; $column++) {
            $pixels[$row * $Width + $column] = [byte](& $PixelFactory $row $column)
        }
    }
    $bytes = New-Object byte[] ($header.Length + $pixels.Length)
    [Array]::Copy($header, 0, $bytes, 0, $header.Length)
    [Array]::Copy($pixels, 0, $bytes, $header.Length, $pixels.Length)
    [IO.File]::WriteAllBytes($Path, $bytes)
}

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s20-smoke-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($tempRoot) | Out-Null
$imagePath = Join-Path $tempRoot 'two-contours.pgm'
Write-Pgm $imagePath 90 70 {
    param($row, $column)
    if ($row -ge 10 -and $row -le 54 -and $column -ge 12 -and $column -le 69) {
        if ($row -ge 25 -and $row -le 39 -and $column -ge 30 -and $column -le 49) { return 60 }
        return 210
    }
    return 60
}

$mainType = $assembly.GetType('HalconWinFormsDemo.MainWindow', $true)
$recipeServiceType = $assembly.GetType('HalconWinFormsDemo.Services.RecipeService', $true)
$loggerType = $assembly.GetType('HalconWinFormsDemo.Services.AppLogger', $true)
$toolKindType = $assembly.GetType('HalconWinFormsDemo.Models.VmToolKind', $true)
$toolInstanceType = $assembly.GetType('HalconWinFormsDemo.Models.VmToolInstance', $true)
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
$runFilter = $mainType.GetMethod('RunXldContourFilterTool', $private)
$createXldInput = $mainType.GetMethod('CreateToolInputXld', $private)
$disposeTools = $mainType.GetMethod('DisposeFlowTools', $private)
$captureRecipe = $mainType.GetMethod('CaptureRecipe', $private)
$refreshLayout = $mainType.GetMethod('RefreshFlowGraphLayout', $private)
$resolveSelection = $mainType.GetMethod('ResolveFlowGraphSelection', $private)
$applySelection = $mainType.GetMethod('ApplyFlowToolSelection', $private)
$setEnabled = $mainType.GetMethod('SetSelectedFlowToolsEnabled', $private)
$copyClipboard = $mainType.GetMethod('CopySelectedFlowToolsToClipboard', $private)
$duplicate = $mainType.GetMethod('DuplicateFlowToolButton_Click', $private)
$deleteSelected = $mainType.GetMethod('DeleteSelectedFlowTools', $private)
$openConfiguration = $mainType.GetMethod('OpenSelectedToolConfiguration', $private)
$flowToolList = $mainType.GetField('FlowToolList', $allFields).GetValue($window)
$selectionText = $mainType.GetField('FlowSelectionStatusText', $allFields).GetValue($window)
$dockPanel = $mainType.GetField('DockXldContourFilterPanel', $allFields).GetValue($window)
$batchEnableButton = $mainType.GetField('BatchEnableFlowToolsButton', $allFields).GetValue($window)
$batchDisableButton = $mainType.GetField('BatchDisableFlowToolsButton', $allFields).GetValue($window)
$batchDeleteButton = $mainType.GetField('BatchDeleteFlowToolsButton', $allFields).GetValue($window)
$selectionBaseline = $mainType.GetField('flowGraphSelectionBaseline', $private).GetValue($window)

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

function Select-Tools([object[]]$Tools) {
    $primary = if ($Tools.Count -gt 0) { $Tools[$Tools.Count - 1] } else { $null }
    $typedTools = [Array]::CreateInstance($toolInstanceType, $Tools.Count)
    for ($index = 0; $index -lt $Tools.Count; $index++) { $typedTools.SetValue($Tools[$index], $index) }
    $applySelection.Invoke($window, [object[]]@($typedTools, $primary, $false)) | Out-Null
}

$disposeTools.Invoke($window, @()) | Out-Null
$flow.Clear()
$source = New-Tool 'ImageSource' 'S20_Image'
$threshold = New-Tool 'ImageThreshold' 'S20_Region'
$grayBranch = New-Tool 'GrayStat' 'S20_Gray_Branch'
$contour = New-Tool 'RegionContourMeasurement' 'S20_Two_Contours'
$filter = New-Tool 'XldContourFilter' 'S20_Contour_Filter'
$judge = New-Tool 'NumericJudge' 'S20_Length_Judge'
$source.Parameters.LocalImagePath = $imagePath
$source.Parameters.LocalImageSerialNumber = 'S20-XLD'
$threshold.Parameters.ImageThresholdMinGray = 150
$threshold.Parameters.ImageThresholdMaxGray = 255
$contour.Parameters.RegionContourMode = 'BorderHoles'
$filter.Parameters.XldContourFeature = 'ContourLength'
$filter.Parameters.XldContourMin = 100
$filter.Parameters.XldContourMax = 300
$judge.NumericOperator = 'GreaterOrEqual'
$judge.NumericLowerLimit = 100
foreach ($tool in @($source, $threshold, $grayBranch, $contour, $filter, $judge)) { $flow.Add($tool) }
$threshold.SetInputBinding('Image', $source.ToolId, 'Image')
$grayBranch.SetInputBinding('Image', $source.ToolId, 'Image')
$contour.SetInputBinding('Region', $threshold.ToolId, 'Region')
$filter.SetInputBinding('XLD', $contour.ToolId, 'XLD')
$judge.InputToolId = $filter.ToolId
$judge.InputPortName = 'TotalLength'

$catalogItem = $catalog | Where-Object { $_.Kind.ToString() -eq 'XldContourFilter' } | Select-Object -First 1
Assert-True ($null -ne $catalogItem) 'XLD contour filter is missing from the visible toolbox.'
Assert-True ($null -ne $batchEnableButton -and $null -ne $batchDisableButton -and $null -ne $batchDeleteButton) 'Visible batch action controls are missing.'

$unrunRejected = $false
try { Invoke-Tool $filter 'S20 unrun upstream' | Out-Null } catch { $unrunRejected = -not [string]::IsNullOrWhiteSpace($_.Exception.Message) }
Assert-True $unrunRejected 'XLD contour filter must reject an unrun upstream XLD.'

Invoke-Tool $source 'S20 source' | Out-Null
Invoke-Tool $threshold 'S20 threshold' | Out-Null
Invoke-Tool $grayBranch 'S20 gray branch' | Out-Null
Invoke-Tool $contour 'S20 contours' | Out-Null
$xldInputCopy = $createXldInput.Invoke($window, [object[]]@($filter, 'XLD'))
try {
    $inputCount = $null
    [HalconDotNet.HOperatorSet]::CountObj($xldInputCopy, [ref]$inputCount)
    Assert-True ($inputCount.I -eq 2) 'CreateToolInputXld did not return a safe copy of both upstream contours.'
}
finally { $xldInputCopy.Dispose() }
Invoke-Tool $filter 'S20 contour length filter' | Out-Null
Invoke-Tool $judge 'S20 judge' | Out-Null

$filteredXld = Get-Output $filter 'XLD'
$filteredLength = [double](Get-Output $filter 'TotalLength')
$filteredCount = [int][double](Get-Output $filter 'ContourCount')
Assert-True ($filter.ResultCode -eq 'OK' -and $filteredCount -eq 1 -and $filteredLength -gt 100) 'contour_length must reduce the synthetic two-contour input to one contour.'
Assert-True ($filteredXld.ObjectCount -eq 1 -and [Math]::Abs($filteredXld.TotalLength - $filteredLength) -lt 0.001) 'Managed filtered XLD metadata is inconsistent.'
Assert-True (-not [string]::IsNullOrWhiteSpace([string](Get-Output $filter 'Feature')) -and (Get-Output $filter 'Range').Contains('100')) 'Feature/range outputs are missing.'
Assert-True ($judge.ResultCode -eq 'OK' -and [Math]::Abs([double](Get-Output $judge 'Value') - $filteredLength) -lt 0.001) 'NumericJudge did not consume the filtered TotalLength.'

$filter.Parameters.XldContourFeature = 'MaximumExtent'
$filter.Parameters.XldContourMin = 50
$filter.Parameters.XldContourMax = 100
Invoke-Tool $filter 'S20 maximum extent filter' | Out-Null
$extentLength = [double](Get-Output $filter 'TotalLength')
Assert-True ([int][double](Get-Output $filter 'ContourCount') -eq 1 -and $extentLength -gt 100) 'maximum_extent must reduce the synthetic two-contour input to one contour.'
Assert-True (-not [string]::IsNullOrWhiteSpace([string](Get-Output $filter 'Feature'))) 'maximum_extent feature output is incorrect.'

$oldFilteredSnapshot = Get-Output $filter 'XLD'
Invoke-Tool $contour 'S20 lifecycle rerun' | Out-Null
Assert-True ($oldFilteredSnapshot.IsDisposed -and $filter.ResultCode -eq '--' -and $judge.ResultCode -eq '--') 'Upstream rerun must dispose and invalidate the filtered XLD and numeric consumer.'
Invoke-Tool $filter 'S20 released snapshot baseline' | Out-Null
$upstreamXld = Get-Output $contour 'XLD'
$upstreamXld.Dispose()
$releasedRejected = $false
try { Invoke-Tool $filter 'S20 released upstream XLD' | Out-Null } catch { $releasedRejected = -not [string]::IsNullOrWhiteSpace($_.Exception.Message) }
Assert-True $releasedRejected 'A released upstream XLD snapshot must be rejected.'

Invoke-Tool $contour 'S20 restore upstream XLD' | Out-Null
[HalconDotNet.HObject]$emptyXld = $null
[HalconDotNet.HOperatorSet]::GenEmptyObj([ref]$emptyXld)
$emptyTool = New-Tool 'XldContourFilter' 'S20_Empty_XLD'
try {
    $emptyRecord = $runFilter.Invoke($window, [object[]]@($emptyTool, $emptyXld))
    Assert-True ($emptyRecord.ResultCode -eq 'NG' -and [int][double](Get-Output $emptyTool 'ContourCount') -eq 0 -and [double](Get-Output $emptyTool 'TotalLength') -eq 0) 'An empty XLD must produce a managed empty XLD and NG result.'
}
finally { $emptyXld.Dispose(); $emptyTool.Dispose() }

$badType = New-Tool 'XldContourFilter' 'S20_Bad_Type'
$flow.Add($badType)
$badType.SetInputBinding('XLD', $threshold.ToolId, 'Region')
$typeRejected = $false
try { Invoke-Tool $badType 'S20 wrong type' | Out-Null } catch { $typeRejected = -not [string]::IsNullOrWhiteSpace($_.Exception.Message) }
Assert-True $typeRejected 'Region-to-XLD type mismatch must be rejected.'
$flow.Remove($badType) | Out-Null; $badType.Dispose()

$badOrderFilter = New-Tool 'XldContourFilter' 'S20_Bad_Order'
$lateContour = New-Tool 'RegionContourMeasurement' 'S20_Late_Contour'
$flow.Insert(1, $badOrderFilter)
$flow.Add($lateContour)
$badOrderFilter.SetInputBinding('XLD', $lateContour.ToolId, 'XLD')
$orderRejected = $false
try { Invoke-Tool $badOrderFilter 'S20 bad order' | Out-Null } catch { $orderRejected = -not [string]::IsNullOrWhiteSpace($_.Exception.Message) }
Assert-True $orderRejected 'An XLD source after the target must be rejected.'
$flow.Remove($badOrderFilter) | Out-Null; $flow.Remove($lateContour) | Out-Null; $badOrderFilter.Dispose(); $lateContour.Dispose()

$filter.Parameters.XldContourFeature = 'Unsupported'
$invalidFeatureRejected = $false
try { Invoke-Tool $filter 'S20 invalid feature' | Out-Null } catch { $invalidFeatureRejected = -not [string]::IsNullOrWhiteSpace($_.Exception.Message) }
Assert-True $invalidFeatureRejected 'Unsupported XLD feature must fail with an actionable error.'
$filter.Parameters.XldContourFeature = 'ContourLength'
$filter.Parameters.XldContourMin = 300
$filter.Parameters.XldContourMax = 100
$invalidRangeRejected = $false
try { Invoke-Tool $filter 'S20 invalid range' | Out-Null } catch { $invalidRangeRejected = -not [string]::IsNullOrWhiteSpace($_.Exception.Message) }
Assert-True $invalidRangeRejected 'Reversed XLD range must fail with an actionable error.'
$filter.Parameters.XldContourMin = 100
$filter.Parameters.XldContourMax = 300
Invoke-Tool $filter 'S20 restore filter' | Out-Null
Invoke-Tool $judge 'S20 restore judge' | Out-Null

$flowToolList.SelectedItem = $filter
$openConfiguration.Invoke($window, @()) | Out-Null
Assert-True ($dockPanel.Visibility.ToString() -eq 'Visible') 'XLD filter parameters do not have a real docked UI entry.'

$recipe = $captureRecipe.Invoke($window, @())
$recipePath = Join-Path $tempRoot 's20-roundtrip.json'
$recipeService.SaveRecipe($recipePath, $recipe)
$loaded = $recipeService.LoadRecipe($recipePath)
$filterRecipe = $loaded.ToolFlow | Where-Object ToolId -eq $filter.ToolId
Assert-True ($filterRecipe.Parameters.XldContourFeature -eq 'ContourLength' -and $filterRecipe.Parameters.XldContourMin -eq 100 -and $filterRecipe.Parameters.XldContourMax -eq 300) 'XLD filter parameters did not round-trip through the recipe.'
Assert-True (($filterRecipe.InputBindings | Where-Object TargetPortName -eq 'XLD').SourceToolId -eq $contour.ToolId) 'XLD input binding did not round-trip through the recipe.'

$refreshLayout.Invoke($window, @()) | Out-Null
$branchLeft = [Math]::Min($threshold.GraphX, $grayBranch.GraphX) - 1
$branchTop = [Math]::Min($threshold.GraphY, $grayBranch.GraphY) - 1
$branchRight = [Math]::Max($threshold.GraphX + 360, $grayBranch.GraphX + 360) + 1
$branchBottom = [Math]::Max($threshold.GraphY + $threshold.GraphHeight, $grayBranch.GraphY + $grayBranch.GraphHeight) + 1
$branchRect = [Windows.Rect]::new($branchLeft, $branchTop, $branchRight - $branchLeft, $branchBottom - $branchTop)
$selectionBaseline.Clear()
$selectionBaseline.Add($contour.ToolId) | Out-Null
$resolved = $resolveSelection.Invoke($window, [object[]]@($branchRect, $true))
Assert-True ($resolved.Count -eq 3 -and $resolved.Contains($threshold) -and $resolved.Contains($grayBranch) -and $resolved.Contains($contour)) 'Ctrl rubber-band selection did not combine the baseline with a cross-branch rectangle.'
Select-Tools @($threshold, $grayBranch, $contour)
Assert-True ($flowToolList.SelectedItems.Count -eq 3 -and $threshold.IsFlowSelected -and $grayBranch.IsFlowSelected -and $contour.IsFlowSelected) 'Canvas/list multi-selection state is not synchronized.'
Assert-True ([object]::ReferenceEquals($flowToolList.SelectedItem, $contour)) 'The last selected module must remain the primary configuration target.'
Assert-True ($selectionText.Text.Contains('3/') -and $selectionText.Text.Contains('Ctrl')) 'Visible selection count/instruction is missing.'

$liveContourBeforeDisable = Get-Output $contour 'XLD'
$setEnabled.Invoke($window, [object[]]@($false)) | Out-Null
Assert-True (-not $threshold.IsEnabled -and -not $grayBranch.IsEnabled -and -not $contour.IsEnabled) 'Batch disable did not update all selected real flow tools.'
Assert-True ($liveContourBeforeDisable.IsDisposed -and $filter.ResultCode -eq '--' -and $judge.ResultCode -eq '--') 'Batch disable did not invalidate downstream runtime state.'
$setEnabled.Invoke($window, [object[]]@($true)) | Out-Null
Assert-True ($threshold.IsEnabled -and $grayBranch.IsEnabled -and $contour.IsEnabled) 'Batch enable did not update all selected real flow tools.'
Invoke-Tool $threshold 'S20 after batch enable' | Out-Null
Invoke-Tool $contour 'S20 contour after batch enable' | Out-Null
Invoke-Tool $filter 'S20 filter after batch enable' | Out-Null
Invoke-Tool $judge 'S20 judge after batch enable' | Out-Null

Select-Tools @($contour, $filter, $judge)
Assert-True ([int]$copyClipboard.Invoke($window, @()) -eq 3) 'Batch copy did not capture all selected modules.'
$countBeforeDuplicate = $flow.Count
$duplicate.Invoke($window, [object[]]@($null, $null)) | Out-Null
Assert-True ($flow.Count -eq $countBeforeDuplicate + 3) 'Batch duplicate did not create one copy per selected module.'
$duplicates = @($flow | Select-Object -Last 3)
Assert-True (($duplicates | Select-Object -ExpandProperty ToolId | Select-Object -Unique).Count -eq 3) 'Duplicated modules must have unique ToolIds.'
Assert-True ($duplicates[1].GetInputBinding('XLD').SourceToolId -eq $duplicates[0].ToolId -and $duplicates[2].InputToolId -eq $duplicates[1].ToolId) 'Batch duplicate did not remap internal XLD/numeric dependencies.'
Assert-True ([int]$deleteSelected.Invoke($window, @()) -eq 3) 'Batch delete could not remove the newly duplicated selection.'
Assert-True ($flow.Count -eq $countBeforeDuplicate) 'Duplicate cleanup left extra modules in the real flow.'

$filterSnapshotBeforeDelete = Get-Output $filter 'XLD'
Select-Tools @($threshold, $grayBranch, $contour)
Assert-True ([int]$deleteSelected.Invoke($window, @()) -eq 3) 'Batch delete did not remove the selected cross-branch modules.'
Assert-True (-not $flow.Contains($threshold) -and -not $flow.Contains($grayBranch) -and -not $flow.Contains($contour)) 'Deleted modules remain in the real flow collection.'
Assert-True ($null -eq $filter.GetInputBinding('XLD') -and $filter.ResultCode -eq '--' -and $judge.ResultCode -eq '--') 'Batch delete did not clear the consumer binding and invalidate downstream tools.'
Assert-True ($filterSnapshotBeforeDelete.IsDisposed) 'Batch delete did not release the downstream managed XLD snapshot.'

$disposeTools.Invoke($window, @()) | Out-Null
$flow.Clear()
$mainType.GetField('recipeCloseConfirmed', $private).SetValue($window, $true)
$window.Close()

Write-Output ('S20_FLOW_SELECTION_XLD_FILTER=PASS; Selection=RUBBER_BAND/CTRL_TOGGLE/COUNT/LIST_SYNC/CROSS_BRANCH; Batch=COPY/DUPLICATE/ENABLE/DISABLE/DELETE/CONSUMER_CLEANUP; HALCON=SELECT_CONTOURS_XLD; Features=CONTOUR_LENGTH/MAXIMUM_EXTENT; Synthetic=2_TO_1; XLD=MANAGED; Downstream=NUMERIC_JUDGE; Errors=UNRUN/TYPE/ORDER/RELEASED/EMPTY/FEATURE/RANGE; Dock=PASS; Recipe=PASS; Lifecycle=PASS; Lengths=Contour:{0:0.###}/Extent:{1:0.###}' -f $filteredLength, $extentLength)
