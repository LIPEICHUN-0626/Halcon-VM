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

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s12-smoke-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($tempRoot) | Out-Null
$imagePath = Join-Path $tempRoot 'threshold-rgb.png'
$bitmap = New-Object Drawing.Bitmap 120, 80
try {
    for ($row = 0; $row -lt $bitmap.Height; $row++) {
        for ($column = 0; $column -lt $bitmap.Width; $column++) {
            $insideFirst = $column -ge 15 -and $column -le 44 -and $row -ge 15 -and $row -le 44
            $insideSecond = $column -ge 75 -and $column -le 104 -and $row -ge 25 -and $row -le 54
            $gray = if ($insideFirst -or $insideSecond) { 220 } else { 20 }
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
$constructorTypes = [Type[]]@([string])
$constructorArgs = New-Object 'object[]' 1
$constructorArgs[0] = $tempRoot.ToString()
$recipeService = $recipeServiceType.GetConstructor($constructorTypes).Invoke($constructorArgs)
$logger = $loggerType.GetConstructor($constructorTypes).Invoke($constructorArgs)
$windowConstructor = $mainType.GetConstructor($private, $null, [Type[]]@($recipeServiceType, $loggerType), $null)
$window = $windowConstructor.Invoke([object[]]@($recipeService, $logger))

$flow = $mainType.GetField('flowTools', $private).GetValue($window)
$catalog = $mainType.GetField('toolCatalog', $private).GetValue($window)
$favorites = $mainType.GetField('favoriteToolKinds', $private).GetValue($window)
$createTool = $mainType.GetMethod('CreateFlowTool', $private)
$insertCatalog = $mainType.GetMethod('InsertCatalogTool', $private)
$executeTool = $mainType.GetMethod('ExecuteFlowTool', $private)
$disposeTools = $mainType.GetMethod('DisposeFlowTools', $private)
$openConfiguration = $mainType.GetMethod('OpenSelectedToolConfiguration', $private)
$applyDock = $mainType.GetMethod('ApplyDockConfigurationDraft', $private)
$captureRecipe = $mainType.GetMethod('CaptureRecipe', $private)
$saveLayout = $mainType.GetMethod('SaveLayoutState', $private)
$flowToolList = $mainType.GetField('FlowToolList', $allFields).GetValue($window)
$rightTabs = $mainType.GetField('RightTabs', $allFields).GetValue($window)
$dockTab = $mainType.GetField('DockConfigurationTab', $allFields).GetValue($window)
$dockThresholdPanel = $mainType.GetField('DockImageThresholdPanel', $allFields).GetValue($window)
$dockThresholdMin = $mainType.GetField('DockImageThresholdMinTextBox', $allFields).GetValue($window)
$dockThresholdMax = $mainType.GetField('DockImageThresholdMaxTextBox', $allFields).GetValue($window)
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

# A line: deterministic insertion plus local favorite/recent persistence.
$sourceForInsert = New-Tool 'ImageSource' 'Insert_Source'
$grayForInsert = New-Tool 'GrayStat' 'Insert_Gray'
$flow.Add($sourceForInsert)
$flow.Add($grayForInsert)
$thresholdCatalog = $catalog | Where-Object Kind -eq ([Enum]::Parse($toolKindType, 'ImageThreshold')) | Select-Object -First 1
Assert-True ($null -ne $thresholdCatalog) 'The threshold tool is missing from the categorized toolbox.'
$thresholdCatalog = $thresholdCatalog.PSObject.BaseObject
$inserted = $insertCatalog.Invoke($window, [object[]]@($thresholdCatalog, 1, '测试插入'))
Assert-True ([object]::ReferenceEquals($flow[1], $inserted)) 'Toolbox insertion did not use the requested deterministic linear position.'
Assert-True ($flow[0].Sequence -eq 1 -and $flow[1].Sequence -eq 2 -and $flow[2].Sequence -eq 3) 'Flow sequence labels did not refresh after insertion.'
$favorites.Add([Enum]::Parse($toolKindType, 'ImageThreshold')) | Out-Null
$saveLayout.Invoke($window, @()) | Out-Null
$layout = $recipeService.LoadLayout()
Assert-True ($layout.FavoriteToolKinds -contains 'ImageThreshold') 'Tool favorites were not saved in local UI state.'
Assert-True ($layout.RecentToolKinds[0] -eq 'ImageThreshold') 'Recent tool usage was not saved in local UI state.'

$disposeTools.Invoke($window, @()) | Out-Null
$flow.Clear()

# B line: source -> filter -> threshold.Region -> Gray/Edge ROI.
$source = New-Tool 'ImageSource' 'S12_RGB_Source'
$filter = New-Tool 'ImageFilter' 'S12_Mean_Filter'
$threshold = New-Tool 'ImageThreshold' 'S12_Threshold'
$gray = New-Tool 'GrayStat' 'S12_Gray_In_Region'
$edge = New-Tool 'EdgeMeasure' 'S12_Edge_In_Region'
$source.Parameters.LocalImagePath = $imagePath
$source.Parameters.LocalImageSerialNumber = 'S12-THRESHOLD-001'
$filter.Parameters.ImageFilterMaskWidth = 3
$filter.Parameters.ImageFilterMaskHeight = 3
$threshold.Parameters.ImageThresholdMinGray = 150
$threshold.Parameters.ImageThresholdMaxGray = 255
$edge.Parameters.EdgeThreshold = 5
$flow.Add($source); $flow.Add($filter); $flow.Add($threshold); $flow.Add($gray); $flow.Add($edge)
$filter.SetInputBinding('Image', $source.ToolId, 'Image')
$threshold.SetInputBinding('Image', $filter.ToolId, 'Image')
$gray.SetInputBinding('Image', $filter.ToolId, 'Image')
$gray.SetInputBinding('ROI', $threshold.ToolId, 'Region')
$edge.SetInputBinding('Image', $filter.ToolId, 'Image')
$edge.SetInputBinding('ROI', $threshold.ToolId, 'Region')

$upstreamRejected = $false
$upstreamDetails = '--'
try { Invoke-Tool $threshold 'S12 upstream not run' | Out-Null }
catch { $upstreamDetails = $_.Exception.Message; $upstreamRejected = $upstreamDetails.Contains('OK') -or $upstreamDetails.Contains('上游') }
Assert-True $upstreamRejected "Threshold must reject an Image source that has not run successfully. $upstreamDetails"

Invoke-Tool $source 'S12 source' | Out-Null
$releasedSource = Get-Output $source 'Image'
$releasedSource.Dispose()
$releasedRejected = $false
$releasedDetails = '--'
try { Invoke-Tool $filter 'S12 released source snapshot' | Out-Null }
catch { $releasedDetails = $_.Exception.Message; $releasedRejected = $true }
Assert-True ($releasedRejected -and -not [string]::IsNullOrWhiteSpace($filter.ErrorMessage) -and $filter.ErrorMessage.Contains('Image')) "A released upstream Image snapshot must be rejected clearly. $releasedDetails / $($filter.ErrorMessage)"

Invoke-Tool $source 'S12 source restore' | Out-Null
Invoke-Tool $filter 'S12 filter' | Out-Null

$flowToolList.SelectedItem = $threshold
$openConfiguration.Invoke($window, @()) | Out-Null
Assert-True ([object]::ReferenceEquals($rightTabs.SelectedItem, $dockTab)) 'Threshold must open the docked configuration workbench.'
Assert-True ($dockThresholdPanel.Visibility.ToString() -eq 'Visible') 'Threshold parameter panel is not visible.'
Assert-True ($dockInputRows.Count -eq 1) 'Threshold must expose one typed Image input row.'
$dockThresholdMin.Text = '200'
$dockThresholdMax.Text = '100'
Assert-True (-not [bool]$applyDock.Invoke($window, @($false))) 'An inverted threshold range must be rejected in the dock.'
Assert-True ($dockValidation.Text.Contains('0') -and $dockValidation.Text.Contains('255')) 'Threshold validation did not provide an actionable range.'
$dockThresholdMin.Text = '150'
$dockThresholdMax.Text = '255'
Assert-True ([bool]$applyDock.Invoke($window, @($false))) 'Applying a valid threshold dock draft failed.'

Invoke-Tool $threshold 'S12 threshold RGB' | Out-Null
Assert-True ($threshold.ResultCode -eq 'OK') 'The normal RGB threshold path should produce OK.'
$regionSnapshot = Get-Output $threshold 'Region'
$area = [double](Get-Output $threshold 'Area')
$regionCount = [double](Get-Output $threshold 'RegionCount')
$thresholdText = [string](Get-Output $threshold 'Threshold')
Assert-True ($area -gt 1500 -and $regionCount -eq 2) 'Threshold area/connected-region outputs are not plausible for the two-object image.'
Assert-True ($regionSnapshot.ObjectCount -eq 2) 'The managed Region snapshot must expose the same connected-object count as RegionCount.'
Assert-True ($thresholdText.Contains('rgb1_to_gray')) 'The reproducible RGB-to-gray convention is missing from the output.'

Invoke-Tool $gray 'S12 gray subscribed region' | Out-Null
Invoke-Tool $edge 'S12 edge subscribed region' | Out-Null
Assert-True ($gray.ResultCode -eq 'OK') 'GrayStat did not consume ImageThreshold.Region.'
Assert-True ($edge.ResultCode -eq 'OK') 'EdgeMeasure did not consume ImageThreshold.Region.'

$recipe = $captureRecipe.Invoke($window, @())
$recipePath = Join-Path $tempRoot 's12-roundtrip.json'
$recipeService.SaveRecipe($recipePath, $recipe)
$loaded = $recipeService.LoadRecipe($recipePath)
$thresholdRecipe = $loaded.ToolFlow | Where-Object ToolId -eq $threshold.ToolId
$grayRecipe = $loaded.ToolFlow | Where-Object ToolId -eq $gray.ToolId
Assert-True ($thresholdRecipe.Parameters.ImageThresholdMinGray -eq 150 -and $thresholdRecipe.Parameters.ImageThresholdMaxGray -eq 255) 'Threshold parameters did not round-trip.'
Assert-True (($thresholdRecipe.InputBindings | Where-Object TargetPortName -eq 'Image').SourceToolId -eq $filter.ToolId) 'Threshold Image binding did not round-trip.'
Assert-True (($grayRecipe.InputBindings | Where-Object TargetPortName -eq 'ROI').SourceToolId -eq $threshold.ToolId) 'Downstream Region binding did not round-trip.'

$threshold.Parameters.ImageThresholdMinGray = 250
$threshold.Parameters.ImageThresholdMaxGray = 255
Invoke-Tool $threshold 'S12 empty region' | Out-Null
$emptySnapshot = Get-Output $threshold 'Region'
Assert-True ($threshold.ResultCode -eq 'NG' -and [double](Get-Output $threshold 'Area') -eq 0 -and [double](Get-Output $threshold 'RegionCount') -eq 0) 'Empty threshold output must be a managed NG Region with zero metrics.'
Assert-True ($emptySnapshot.ObjectCount -eq 0) 'An empty managed Region snapshot must not report a phantom object.'
Assert-True $regionSnapshot.IsDisposed 'Rerunning threshold must dispose the previous Region snapshot.'
Assert-True ($gray.ResultCode -eq '--' -and $edge.ResultCode -eq '--') 'Rerunning threshold must invalidate Region subscribers.'

$threshold.Parameters.ImageThresholdMinGray = 240
$threshold.Parameters.ImageThresholdMaxGray = 100
$invalidRejected = $false
try { Invoke-Tool $threshold 'S12 invalid range' | Out-Null }
catch { $invalidRejected = $_.Exception.Message.Contains('0') -and $_.Exception.Message.Contains('255') }
Assert-True $invalidRejected 'Invalid threshold parameters must be rejected clearly.'
Assert-True $emptySnapshot.IsDisposed 'A failed rerun must still dispose the previous Region snapshot safely.'

$threshold.Parameters.ImageThresholdMinGray = 150
$threshold.Parameters.ImageThresholdMaxGray = 255
Invoke-Tool $threshold 'S12 lifecycle restore' | Out-Null
$restoredRegion = Get-Output $threshold 'Region'
Invoke-Tool $source 'S12 source invalidation' | Out-Null
Assert-True $restoredRegion.IsDisposed 'Source rerun must dispose downstream threshold Region snapshots.'
Assert-True ($filter.ResultCode -eq '--' -and $threshold.ResultCode -eq '--' -and $gray.ResultCode -eq '--') 'Source rerun must invalidate the complete Image/Region chain.'

# Type and order validation remain real rather than UI-only.
$disposeTools.Invoke($window, @()) | Out-Null
$flow.Clear()
$badThreshold = New-Tool 'ImageThreshold' 'Bad_Order_Threshold'
$lateSource = New-Tool 'ImageSource' 'Late_Source'
$badThreshold.SetInputBinding('Image', $lateSource.ToolId, 'Image')
$flow.Add($badThreshold); $flow.Add($lateSource)
$orderRejected = $false
$orderDetails = '--'
try { Invoke-Tool $badThreshold 'S12 bad order' | Out-Null }
catch { $orderDetails = $_.Exception.Message; $orderRejected = $true }
Assert-True ($orderRejected -and -not [string]::IsNullOrWhiteSpace($badThreshold.ErrorMessage)) "A source after the target must be rejected. $orderDetails / $($badThreshold.ErrorMessage)"

$disposeTools.Invoke($window, @()) | Out-Null
$flow.Clear()
$regionProducer = New-Tool 'ImageThreshold' 'Region_Producer'
$typeTarget = New-Tool 'ImageThreshold' 'Image_Type_Target'
$typeTarget.SetInputBinding('Image', $regionProducer.ToolId, 'Region')
$flow.Add($regionProducer); $flow.Add($typeTarget)
$typeRejected = $false
try { Invoke-Tool $typeTarget 'S12 bad type' | Out-Null }
catch { $typeRejected = $true }
Assert-True ($typeRejected -and -not [string]::IsNullOrWhiteSpace($typeTarget.ErrorMessage)) 'A Region-to-Image binding must be rejected.'

$disposeTools.Invoke($window, @()) | Out-Null
$closeConfirmed = $mainType.GetField('recipeCloseConfirmed', $private)
$closeConfirmed.SetValue($window, $true)
$window.Close()

Write-Output ('S12_TOOLBOX_THRESHOLD=PASS; Insert=2; Favorite=PASS; Recent=PASS; Area={0:F0}; Regions={1}; RGB=rgb1_to_gray; Gray/Edge=PASS; Empty=NG; Invalid=PASS; Recipe=PASS; Lifecycle=PASS' -f $area, $regionCount)
