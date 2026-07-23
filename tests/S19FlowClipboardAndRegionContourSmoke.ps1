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

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s19-smoke-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($tempRoot) | Out-Null
$imagePath = Join-Path $tempRoot 'region-with-hole.pgm'
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
$roiDataType = $assembly.GetType('HalconWinFormsDemo.Models.RoiData', $true)
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
$runContour = $mainType.GetMethod('RunRegionContourMeasurementTool', $private)
$disposeTools = $mainType.GetMethod('DisposeFlowTools', $private)
$addRoiLayer = $mainType.GetMethod('AddRoiLayer', $private)
$copyClipboard = $mainType.GetMethod('CopySelectedFlowToolsToClipboard', $private)
$pasteClipboard = $mainType.GetMethod('PasteFlowToolClipboard', $private)
$captureRecipe = $mainType.GetMethod('CaptureRecipe', $private)
$flowToolList = $mainType.GetField('FlowToolList', $allFields).GetValue($window)
$dockPanel = $mainType.GetField('DockRegionContourPanel', $allFields).GetValue($window)
$openConfiguration = $mainType.GetMethod('OpenSelectedToolConfiguration', $private)

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
$source = New-Tool 'ImageSource' 'Contour_Source'
$threshold = New-Tool 'ImageThreshold' 'Bright_Region'
$contour = New-Tool 'RegionContourMeasurement' 'Region_Perimeter'
$judge = New-Tool 'NumericJudge' 'Perimeter_Judge'
$source.Parameters.LocalImagePath = $imagePath
$source.Parameters.LocalImageSerialNumber = 'S19-CONTOUR'
$threshold.Parameters.ImageThresholdMinGray = 150
$threshold.Parameters.ImageThresholdMaxGray = 255
$contour.Parameters.RegionContourMode = 'BorderHoles'
$judge.NumericOperator = 'GreaterOrEqual'
$judge.NumericLowerLimit = 100
foreach ($tool in @($source, $threshold, $contour, $judge)) { $flow.Add($tool) }
$threshold.SetInputBinding('Image', $source.ToolId, 'Image')
$contour.SetInputBinding('Region', $threshold.ToolId, 'Region')
$judge.InputToolId = $contour.ToolId
$judge.InputPortName = 'TotalLength'

$catalogItem = $catalog | Where-Object { $_.Kind.ToString() -eq 'RegionContourMeasurement' } | Select-Object -First 1
Assert-True ($null -ne $catalogItem) 'Region contour measurement is missing from the visible toolbox.'

$unrunRejected = $false
try { Invoke-Tool $contour 'S19 unrun upstream' | Out-Null } catch { $unrunRejected = $_.Exception.Message.Contains('OK') }
Assert-True $unrunRejected 'Region contour measurement must reject an unrun upstream Region.'

Invoke-Tool $source 'S19 source' | Out-Null
Invoke-Tool $threshold 'S19 threshold' | Out-Null
Invoke-Tool $contour 'S19 border holes' | Out-Null
Invoke-Tool $judge 'S19 numeric judge' | Out-Null
$xld = Get-Output $contour 'XLD'
$holeLength = [double](Get-Output $contour 'TotalLength')
$holeCount = [int][double](Get-Output $contour 'ContourCount')
Assert-True ($contour.ResultCode -eq 'OK' -and $holeCount -eq 2 -and $holeLength -gt 0) 'border_holes must output the outer and hole XLD contours with a positive total length.'
Assert-True ($xld.ObjectCount -eq 2 -and [Math]::Abs($xld.TotalLength - $holeLength) -lt 0.001) 'Managed XLD snapshot metadata is inconsistent.'
Assert-True ($judge.ResultCode -eq 'OK' -and [Math]::Abs([double](Get-Output $judge 'Value') - $holeLength) -lt 0.001) 'NumericJudge did not consume TotalLength.'
$xldCopy = $xld.CreateXldCopy()
try {
    $copyCount = $null
    [HalconDotNet.HOperatorSet]::CountObj($xldCopy, [ref]$copyCount)
    Assert-True ($copyCount.I -eq 2) 'The managed XLD copy is incomplete.'
}
finally { $xldCopy.Dispose() }

$contour.Parameters.RegionContourMode = 'Border'
Invoke-Tool $contour 'S19 outer border' | Out-Null
$borderLength = [double](Get-Output $contour 'TotalLength')
$borderCount = [int][double](Get-Output $contour 'ContourCount')
Assert-True ($borderCount -eq 1 -and $borderLength -gt 0 -and $borderLength -lt $holeLength) 'border must exclude hole contours.'
$contour.Parameters.RegionContourMode = 'Center'
Invoke-Tool $contour 'S19 center contour' | Out-Null
Assert-True ([int][double](Get-Output $contour 'ContourCount') -eq 1 -and [double](Get-Output $contour 'TotalLength') -gt 0) 'center contour mode did not execute.'

[HalconDotNet.HRegion]$emptyRegion = [HalconDotNet.HRegion]::new()
$emptyRegion.GenEmptyRegion()
$emptyTool = New-Tool 'RegionContourMeasurement' 'Empty_Region_Check'
try {
    $emptyRecord = $runContour.Invoke($window, [object[]]@($emptyTool, $emptyRegion))
    Assert-True ($emptyRecord.ResultCode -eq 'NG' -and [int][double](Get-Output $emptyTool 'ContourCount') -eq 0 -and [double](Get-Output $emptyTool 'TotalLength') -eq 0) 'An empty Region must produce an empty managed XLD and NG result.'
}
finally { $emptyRegion.Dispose(); $emptyTool.Dispose() }

$badType = New-Tool 'RegionContourMeasurement' 'Bad_Type'
$flow.Add($badType)
$badType.SetInputBinding('Region', $source.ToolId, 'Image')
$typeRejected = $false
try { Invoke-Tool $badType 'S19 wrong type' | Out-Null } catch { $typeRejected = -not [string]::IsNullOrWhiteSpace($_.Exception.Message) }
Assert-True $typeRejected 'Image-to-Region type mismatch must be rejected.'
$flow.Remove($badType) | Out-Null; $badType.Dispose()

$lateContour = New-Tool 'RegionContourMeasurement' 'Bad_Order'
$lateThreshold = New-Tool 'ImageThreshold' 'Late_Threshold'
$flow.Insert(1, $lateContour)
$flow.Add($lateThreshold)
$lateContour.SetInputBinding('Region', $lateThreshold.ToolId, 'Region')
$orderRejected = $false
try { Invoke-Tool $lateContour 'S19 bad order' | Out-Null } catch { $orderRejected = -not [string]::IsNullOrWhiteSpace($_.Exception.Message) }
Assert-True $orderRejected 'A Region source after the target must be rejected.'
$flow.Remove($lateContour) | Out-Null; $flow.Remove($lateThreshold) | Out-Null; $lateContour.Dispose(); $lateThreshold.Dispose()

Invoke-Tool $threshold 'S19 lifecycle source' | Out-Null
Invoke-Tool $contour 'S19 lifecycle contour' | Out-Null
$liveXld = Get-Output $contour 'XLD'
Invoke-Tool $threshold 'S19 invalidate contour' | Out-Null
Assert-True ($liveXld.IsDisposed -and $contour.ResultCode -eq '--' -and $judge.ResultCode -eq '--') 'Upstream rerun must dispose and invalidate the downstream XLD chain.'
Invoke-Tool $contour 'S19 released region baseline' | Out-Null
$regionSnapshot = Get-Output $threshold 'Region'
$regionSnapshot.Dispose()
$releasedRejected = $false
try { Invoke-Tool $contour 'S19 released region' | Out-Null } catch { $releasedRejected = -not [string]::IsNullOrWhiteSpace($_.Exception.Message) }
Assert-True $releasedRejected 'A released upstream Region snapshot must be rejected.'

Invoke-Tool $threshold 'S19 clipboard threshold' | Out-Null
$contour.Parameters.RegionContourMode = 'BorderHoles'
Invoke-Tool $contour 'S19 clipboard contour' | Out-Null
Invoke-Tool $judge 'S19 clipboard judge' | Out-Null
$roi = $roiDataType.GetMethod('CreateRectangle').Invoke($null, [object[]]@([double]8, [double]10, [double]60, [double]75))
$roiLayer = $addRoiLayer.Invoke($window, [object[]]@($roi, 'Clipboard_ROI', $threshold))
$threshold.BindRoi($roiLayer.RoiId)
$flowToolList.SelectedItems.Clear()
foreach ($tool in @($threshold, $contour, $judge)) { $flowToolList.SelectedItems.Add($tool) | Out-Null }
Assert-True ([int]$copyClipboard.Invoke($window, @()) -eq 3) 'Multi-module copy did not capture the selected sequence.'
$originalCount = $flow.Count
Assert-True ([int]$pasteClipboard.Invoke($window, [object[]]@($originalCount)) -eq 3) 'Multi-module paste did not create all copies.'
Assert-True ($flow.Count -eq $originalCount + 3) 'Pasted module count is incorrect.'
$thresholdCopy = $flow[$originalCount]
$contourCopy = $flow[$originalCount + 1]
$judgeCopy = $flow[$originalCount + 2]
Assert-True ($thresholdCopy.ToolId -ne $threshold.ToolId -and $contourCopy.ToolId -ne $contour.ToolId -and $judgeCopy.ToolId -ne $judge.ToolId) 'Pasted modules must receive new ToolIds.'
Assert-True ($thresholdCopy.InstanceName -ne $threshold.InstanceName -and $contourCopy.InstanceName -ne $contour.InstanceName) 'Pasted modules must receive unique instance names.'
Assert-True ($thresholdCopy.Parameters.ImageThresholdMinGray -eq $threshold.Parameters.ImageThresholdMinGray -and $contourCopy.Parameters.RegionContourMode -eq 'BorderHoles') 'Pasted parameters were not cloned.'
Assert-True ($thresholdCopy.BoundRoiIds.Contains($roiLayer.RoiId)) 'Pasted ROI binding was not preserved.'
Assert-True ($thresholdCopy.GetInputBinding('Image').SourceToolId -eq $source.ToolId) 'Legal external binding was not preserved.'
Assert-True ($contourCopy.GetInputBinding('Region').SourceToolId -eq $thresholdCopy.ToolId) 'Internal Region binding was not remapped to the pasted source.'
Assert-True ($judgeCopy.InputToolId -eq $contourCopy.ToolId -and $judgeCopy.InputPortName -eq 'TotalLength') 'Internal numeric binding was not remapped to the pasted contour.'
Assert-True ($thresholdCopy.ResultCode -eq '--' -and $contourCopy.ResultCode -eq '--' -and $judgeCopy.ResultCode -eq '--') 'Runtime result state must not be copied.'
$missingRuntime = $null
Assert-True (-not $contourCopy.TryGetOutputValue('XLD', [ref]$missingRuntime)) 'Managed XLD runtime snapshot must not be copied.'

Assert-True ([int]$pasteClipboard.Invoke($window, [object[]]@($flow.Count)) -eq 3) 'A second paste failed.'
$names = @($flow | ForEach-Object InstanceName)
Assert-True (($names | Select-Object -Unique).Count -eq $names.Count) 'Repeated paste must keep instance names unique.'

$flowToolList.SelectedItem = $contour
$openConfiguration.Invoke($window, @()) | Out-Null
Assert-True ($dockPanel.Visibility.ToString() -eq 'Visible') 'Region contour parameters do not have a real docked UI entry.'

$recipe = $captureRecipe.Invoke($window, @())
$recipePath = Join-Path $tempRoot 's19-roundtrip.json'
$recipeService.SaveRecipe($recipePath, $recipe)
$loaded = $recipeService.LoadRecipe($recipePath)
$contourRecipe = $loaded.ToolFlow | Where-Object ToolId -eq $contour.ToolId
Assert-True ($contourRecipe.Parameters.RegionContourMode -eq 'BorderHoles') 'Region contour mode did not round-trip through the recipe.'
Assert-True (($contourRecipe.InputBindings | Where-Object TargetPortName -eq 'Region').SourceToolId -eq $threshold.ToolId) 'Region contour binding did not round-trip.'

$contour.Parameters.RegionContourMode = 'Unsupported'
$parameterRejected = $false
try { Invoke-Tool $contour 'S19 invalid parameter' | Out-Null } catch { $parameterRejected = -not [string]::IsNullOrWhiteSpace($_.Exception.Message) }
Assert-True $parameterRejected 'Unsupported contour mode must fail with an actionable error.'

$disposeTools.Invoke($window, @()) | Out-Null
$flow.Clear()
$mainType.GetField('recipeCloseConfirmed', $private).SetValue($window, $true)
$window.Close()

Write-Output ('S19_FLOW_CLIPBOARD_REGION_CONTOUR=PASS; Clipboard=3_MODULES/NEW_IDS/UNIQUE_NAMES/PARAMS/ROI/EXTERNAL_BINDING/INTERNAL_REMAP/NO_RUNTIME/REPEAT; HALCON=GEN_CONTOUR_REGION_XLD/LENGTH_XLD; Modes=BORDER/CENTER/BORDER_HOLES; XLD=MANAGED; Downstream=NUMERIC_JUDGE; Errors=UNRUN/TYPE/ORDER/RELEASED/EMPTY/PARAMETER; Dock=PASS; Recipe=PASS; Lifecycle=PASS; Lengths=Border:{0:0.###}/Holes:{1:0.###}' -f $borderLength, $holeLength)
