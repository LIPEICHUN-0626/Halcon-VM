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

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s21-smoke-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($tempRoot) | Out-Null
$imagePath = Join-Path $tempRoot 'rectangle.pgm'
Write-Pgm $imagePath 100 80 {
    param($row, $column)
    if ($row -ge 12 -and $row -le 62 -and $column -ge 14 -and $column -le 84) { return 220 }
    return 30
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
$runSegmentation = $mainType.GetMethod('RunXldContourSegmentationTool', $private)
$refreshPorts = $mainType.GetMethod('RefreshFlowPortVisualization', $private)
$tryApplyBinding = $mainType.GetMethod('TryApplyFlowPortBinding', $private)
$evaluateBinding = $mainType.GetMethod('EvaluateFlowPortConnection', $private)
$beginDrag = $mainType.GetMethod('BeginFlowPortDrag', $private)
$resolveDrag = $mainType.GetMethod('TryResolveFlowPortDragEndpoints', $private)
$cancelDrag = $mainType.GetMethod('CancelFlowPortDrag', $private)
$clearConnection = $mainType.GetMethod('ClearFlowConnectionButton_Click', $private)
$captureRecipe = $mainType.GetMethod('CaptureRecipe', $private)
$openConfiguration = $mainType.GetMethod('OpenSelectedToolConfiguration', $private)
$disposeTools = $mainType.GetMethod('DisposeFlowTools', $private)
$flowToolList = $mainType.GetField('FlowToolList', $allFields).GetValue($window)
$dockPanel = $mainType.GetField('DockXldContourSegmentationPanel', $allFields).GetValue($window)
$previewPath = $mainType.GetField('FlowPortDragPreviewPath', $allFields).GetValue($window)
$dragStatus = $mainType.GetField('FlowPortDragStatusBorder', $allFields).GetValue($window)

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

function Get-Port($Tool, [string]$Direction, [string]$PortName) {
    $ports = if ($Direction -eq 'OUT') { $Tool.FlowOutputPorts } else { $Tool.FlowInputPorts }
    $port = $ports | Where-Object PortName -eq $PortName | Select-Object -First 1
    return $port.PSObject.BaseObject
}

function Test-Connection($SourcePort, $TargetPort) {
    $args = [object[]]@($SourcePort, $TargetPort, $null)
    $valid = [bool]$evaluateBinding.Invoke($window, $args)
    return [pscustomobject]@{ Valid = $valid; Error = [string]$args[2] }
}

function Apply-Connection($SourcePort, $TargetPort) {
    $args = [object[]]@($SourcePort, $TargetPort, $null)
    $applied = [bool]$tryApplyBinding.Invoke($window, $args)
    return [pscustomobject]@{ Applied = $applied; Error = [string]$args[2] }
}

$disposeTools.Invoke($window, @()) | Out-Null
$flow.Clear()
$sourceA = New-Tool 'ImageSource' 'S21_Source_A'
$sourceB = New-Tool 'ImageSource' 'S21_Source_B'
$difference = New-Tool 'ImageDifference' 'S21_Dual_Input'
$threshold = New-Tool 'ImageThreshold' 'S21_Rectangle_Region'
$contour = New-Tool 'RegionContourMeasurement' 'S21_Rectangle_XLD'
$segment = New-Tool 'XldContourSegmentation' 'S21_XLD_Segments'
$judge = New-Tool 'NumericJudge' 'S21_Segment_Judge'
$lateSource = New-Tool 'ImageSource' 'S21_Late_Source'
foreach ($source in @($sourceA, $sourceB, $lateSource)) {
    $source.Parameters.LocalImagePath = $imagePath
    $source.Parameters.LocalImageSerialNumber = $source.InstanceName
}
$threshold.Parameters.ImageThresholdMinGray = 150
$threshold.Parameters.ImageThresholdMaxGray = 255
$contour.Parameters.RegionContourMode = 'Border'
$segment.Parameters.XldSegmentMode = 'LinesCircles'
$segment.Parameters.XldSegmentSmooth = 5
$segment.Parameters.XldSegmentMaxLineDistance1 = 4
$segment.Parameters.XldSegmentMaxLineDistance2 = 2
$judge.NumericOperator = 'GreaterOrEqual'
$judge.NumericLowerLimit = 4
foreach ($tool in @($sourceA, $sourceB, $difference, $threshold, $contour, $segment, $judge, $lateSource)) { $flow.Add($tool) }
$difference.SetInputBinding('ImageA', $sourceA.ToolId, 'Image')
$difference.SetInputBinding('ImageB', $sourceB.ToolId, 'Image')
$threshold.SetInputBinding('Image', $sourceA.ToolId, 'Image')
$contour.SetInputBinding('Region', $threshold.ToolId, 'Region')
$segment.SetInputBinding('XLD', $contour.ToolId, 'XLD')
$judge.InputToolId = $segment.ToolId
$judge.InputPortName = 'SegmentCount'
$judge.SetInputBinding('Value', $segment.ToolId, 'SegmentCount')
$refreshPorts.Invoke($window, @()) | Out-Null

$catalogItem = $catalog | Where-Object { $_.Kind.ToString() -eq 'XldContourSegmentation' } | Select-Object -First 1
Assert-True ($null -ne $catalogItem) 'XLD contour segmentation is missing from the visible toolbox.'
Assert-True ($null -ne $previewPath -and $null -ne $dragStatus) 'Visible port-drag preview controls are missing.'

$sourceAImage = Get-Port $sourceA 'OUT' 'Image'
$sourceBImage = Get-Port $sourceB 'OUT' 'Image'
$lateImage = Get-Port $lateSource 'OUT' 'Image'
$thresholdInput = Get-Port $threshold 'IN' 'Image'
$segmentInput = Get-Port $segment 'IN' 'XLD'
$thresholdRegion = Get-Port $threshold 'OUT' 'Region'
$differenceImageB = Get-Port $difference 'IN' 'ImageB'

$validCandidate = Test-Connection $sourceBImage $thresholdInput
Assert-True ($validCandidate.Valid -and [string]::IsNullOrWhiteSpace($validCandidate.Error)) 'A compatible prior OUT-to-IN connection was rejected.'
$typeCandidate = Test-Connection $thresholdRegion $segmentInput
Assert-True (-not $typeCandidate.Valid -and -not [string]::IsNullOrWhiteSpace($typeCandidate.Error)) 'Type mismatch was not rejected with an actionable reason.'
$orderCandidate = Test-Connection $lateImage $thresholdInput
Assert-True (-not $orderCandidate.Valid -and -not [string]::IsNullOrWhiteSpace($orderCandidate.Error)) 'A source after the target was not rejected.'
$duplicateCandidate = Test-Connection $sourceAImage $differenceImageB
Assert-True (-not $duplicateCandidate.Valid -and -not [string]::IsNullOrWhiteSpace($duplicateCandidate.Error)) 'A duplicate dual-input endpoint was not rejected.'

$originalBinding = $threshold.GetInputBinding('Image').Clone()
$failedApply = Apply-Connection $lateImage $thresholdInput
Assert-True (-not $failedApply.Applied -and $threshold.GetInputBinding('Image').SourceToolId -eq $originalBinding.SourceToolId) 'Rejected connection changed the original binding.'
$successfulApply = Apply-Connection $sourceBImage $thresholdInput
Assert-True ($successfulApply.Applied -and $threshold.GetInputBinding('Image').SourceToolId -eq $sourceB.ToolId) 'Compatible drag application did not replace InputBindings.'

$refreshPorts.Invoke($window, @()) | Out-Null
$boundInput = Get-Port $threshold 'IN' 'Image'
$dummyElement = New-Object Windows.Controls.Border
$started = [bool]$beginDrag.Invoke($window, [object[]]@($boundInput, $dummyElement.PSObject.BaseObject, [Windows.Point]::new(20, 20)))
Assert-True $started 'Dragging out from an already-bound IN port did not start reconnect mode.'
$dragSession = $mainType.GetField('flowPortDragSession', $private).GetValue($window)
$resolveArgs = [object[]]@($dragSession, (Get-Port $sourceA 'OUT' 'Image'), $null, $null, $null)
$resolved = [bool]$resolveDrag.Invoke($window, $resolveArgs)
Assert-True ($resolved -and $resolveArgs[2].OwnerToolId -eq $sourceA.ToolId -and $resolveArgs[3].OwnerToolId -eq $threshold.ToolId) 'IN-origin reconnect did not resolve a new OUT source and fixed IN target.'
$cancelDrag.Invoke($window, [object[]]@('S21 cancel reconnect', $false)) | Out-Null
Assert-True ($threshold.GetInputBinding('Image').SourceToolId -eq $sourceB.ToolId -and $previewPath.Visibility.ToString() -eq 'Collapsed') 'Cancel did not preserve the original binding or hide the preview.'

$reconnectApply = Apply-Connection $sourceAImage $thresholdInput
Assert-True ($reconnectApply.Applied -and $threshold.GetInputBinding('Image').SourceToolId -eq $sourceA.ToolId) 'Reconnect did not restore source A.'

$segmentConnectionKey = "$($contour.ToolId)|XLD|$($segment.ToolId)|XLD"
$mainType.GetField('selectedFlowConnectionKey', $private).SetValue($window, $segmentConnectionKey)
$refreshPorts.Invoke($window, @()) | Out-Null
$clearConnection.Invoke($window, [object[]]@($null, $null)) | Out-Null
Assert-True ($null -eq $segment.GetInputBinding('XLD')) 'Selected connection delete did not clear the real InputBindings entry.'
$refreshPorts.Invoke($window, @()) | Out-Null
$recreate = Apply-Connection (Get-Port $contour 'OUT' 'XLD') (Get-Port $segment 'IN' 'XLD')
Assert-True $recreate.Applied 'Deleted XLD connection could not be recreated through the shared drag binding path.'

$unrunRejected = $false
try { Invoke-Tool $segment 'S21 unrun upstream' | Out-Null } catch { $unrunRejected = -not [string]::IsNullOrWhiteSpace($_.Exception.Message) }
Assert-True $unrunRejected 'XLD segmentation must reject an unrun upstream XLD.'

Invoke-Tool $sourceA 'S21 source A' | Out-Null
Invoke-Tool $threshold 'S21 threshold' | Out-Null
Invoke-Tool $contour 'S21 contour' | Out-Null
Invoke-Tool $segment 'S21 lines circles' | Out-Null
Invoke-Tool $judge 'S21 judge' | Out-Null

$segmentCount = [int][double](Get-Output $segment 'SegmentCount')
$lineCount = [int][double](Get-Output $segment 'LineCount')
$arcCount = [int][double](Get-Output $segment 'ArcCount')
$totalLength = [double](Get-Output $segment 'TotalLength')
$segmentedXld = Get-Output $segment 'XLD'
Assert-True ($segment.ResultCode -eq 'OK' -and $segmentCount -eq 4 -and $lineCount -eq 4 -and $arcCount -eq 0) 'Rectangle XLD did not segment into four classified line segments.'
Assert-True ($totalLength -gt 240 -and $totalLength -lt 250 -and $segmentedXld.ObjectCount -eq 4 -and [Math]::Abs($segmentedXld.TotalLength - $totalLength) -lt 0.001) 'Segmented XLD length/snapshot metadata is inconsistent.'
Assert-True (-not [string]::IsNullOrWhiteSpace([string](Get-Output $segment 'Mode')) -and (Get-Output $segment 'Parameters').Contains('Smooth=5')) 'Mode/parameter outputs are missing.'
Assert-True ($judge.ResultCode -eq 'OK' -and [int][double](Get-Output $judge 'Value') -eq 4) 'NumericJudge did not consume SegmentCount.'

foreach ($mode in @('Lines', 'LinesEllipses')) {
    $segment.Parameters.XldSegmentMode = $mode
    Invoke-Tool $segment "S21 mode $mode" | Out-Null
    Assert-True ([int][double](Get-Output $segment 'SegmentCount') -eq 4) "$mode did not preserve stable rectangle segmentation."
}
$segment.Parameters.XldSegmentMode = 'LinesCircles'

[HalconDotNet.HObject]$emptyXld = $null
[HalconDotNet.HOperatorSet]::GenEmptyObj([ref]$emptyXld)
$emptyTool = New-Tool 'XldContourSegmentation' 'S21_Empty_XLD'
try {
    $emptyRecord = $runSegmentation.Invoke($window, [object[]]@($emptyTool, $emptyXld))
    Assert-True ($emptyRecord.ResultCode -eq 'NG' -and [int][double](Get-Output $emptyTool 'SegmentCount') -eq 0 -and [double](Get-Output $emptyTool 'TotalLength') -eq 0) 'Empty XLD did not produce a managed empty NG result.'
}
finally { $emptyXld.Dispose(); $emptyTool.Dispose() }

[HalconDotNet.HObject]$shortXld = $null
[HalconDotNet.HOperatorSet]::GenContourPolygonXld([ref]$shortXld, [HalconDotNet.HTuple]@([double]10, [double]10.5), [HalconDotNet.HTuple]@([double]10, [double]10.5))
$shortTool = New-Tool 'XldContourSegmentation' 'S21_Short_XLD'
$shortTool.Parameters.XldSegmentSmooth = 0
try {
    $shortRecord = $runSegmentation.Invoke($window, [object[]]@($shortTool, $shortXld))
    Assert-True ($shortRecord.ResultCode -eq 'OK' -and [int][double](Get-Output $shortTool 'SegmentCount') -eq 1 -and [double](Get-Output $shortTool 'TotalLength') -gt 0) 'Short XLD was not safely copied/classified by HALCON.'
}
finally { $shortXld.Dispose(); $shortTool.Dispose() }

$segment.Parameters.XldSegmentMode = 'Unsupported'
$invalidModeRejected = $false
try { Invoke-Tool $segment 'S21 invalid mode' | Out-Null } catch { $invalidModeRejected = -not [string]::IsNullOrWhiteSpace($_.Exception.Message) }
Assert-True $invalidModeRejected 'Unsupported segmentation mode was not rejected.'
$segment.Parameters.XldSegmentMode = 'LinesCircles'
$segment.Parameters.XldSegmentSmooth = 2
$invalidSmoothRejected = $false
try { Invoke-Tool $segment 'S21 invalid smooth' | Out-Null } catch { $invalidSmoothRejected = -not [string]::IsNullOrWhiteSpace($_.Exception.Message) }
Assert-True $invalidSmoothRejected 'Invalid smoothing count was not rejected.'
$segment.Parameters.XldSegmentSmooth = 5
$segment.Parameters.XldSegmentMaxLineDistance1 = -1
$invalidDistanceRejected = $false
try { Invoke-Tool $segment 'S21 invalid distance' | Out-Null } catch { $invalidDistanceRejected = -not [string]::IsNullOrWhiteSpace($_.Exception.Message) }
Assert-True $invalidDistanceRejected 'Negative segmentation distance was not rejected.'
$segment.Parameters.XldSegmentMaxLineDistance1 = 4
Invoke-Tool $segment 'S21 restore after invalid parameters' | Out-Null

$badType = New-Tool 'XldContourSegmentation' 'S21_Bad_Type'
$flow.Insert(5, $badType)
$badType.SetInputBinding('XLD', $threshold.ToolId, 'Region')
$typeRunRejected = $false
try { Invoke-Tool $badType 'S21 wrong type' | Out-Null } catch { $typeRunRejected = -not [string]::IsNullOrWhiteSpace($_.Exception.Message) }
Assert-True $typeRunRejected 'Region-to-XLD runtime mismatch was not rejected.'
$flow.Remove($badType) | Out-Null; $badType.Dispose()

$badOrder = New-Tool 'XldContourSegmentation' 'S21_Bad_Order'
$flow.Insert(4, $badOrder)
$badOrder.SetInputBinding('XLD', $contour.ToolId, 'XLD')
$orderRunRejected = $false
try { Invoke-Tool $badOrder 'S21 source after target' | Out-Null } catch { $orderRunRejected = -not [string]::IsNullOrWhiteSpace($_.Exception.Message) }
Assert-True $orderRunRejected 'An XLD source after the segmentation target was not rejected at runtime.'
$flow.Remove($badOrder) | Out-Null; $badOrder.Dispose()

$oldSegmentSnapshot = Get-Output $segment 'XLD'
Invoke-Tool $contour 'S21 lifecycle rerun' | Out-Null
Assert-True ($oldSegmentSnapshot.IsDisposed -and $segment.ResultCode -eq '--' -and $judge.ResultCode -eq '--') 'Upstream rerun did not release segmented XLD and invalidate downstream consumers.'
Invoke-Tool $segment 'S21 restore segmentation' | Out-Null
$upstreamSnapshot = Get-Output $contour 'XLD'
$upstreamSnapshot.Dispose()
$releasedRejected = $false
try { Invoke-Tool $segment 'S21 released upstream' | Out-Null } catch { $releasedRejected = -not [string]::IsNullOrWhiteSpace($_.Exception.Message) }
Assert-True $releasedRejected 'Released upstream XLD snapshot was not rejected.'
Invoke-Tool $contour 'S21 restore upstream' | Out-Null
Invoke-Tool $segment 'S21 final segmentation' | Out-Null

$flowToolList.SelectedItem = $segment
$openConfiguration.Invoke($window, @()) | Out-Null
Assert-True ($dockPanel.Visibility.ToString() -eq 'Visible') 'XLD segmentation parameters do not have a real docked UI entry.'

$recipe = $captureRecipe.Invoke($window, @())
$recipePath = Join-Path $tempRoot 's21-roundtrip.json'
$recipeService.SaveRecipe($recipePath, $recipe)
$loaded = $recipeService.LoadRecipe($recipePath)
$segmentRecipe = $loaded.ToolFlow | Where-Object ToolId -eq $segment.ToolId
Assert-True ($segmentRecipe.Parameters.XldSegmentMode -eq 'LinesCircles' -and $segmentRecipe.Parameters.XldSegmentSmooth -eq 5 -and $segmentRecipe.Parameters.XldSegmentMaxLineDistance1 -eq 4 -and $segmentRecipe.Parameters.XldSegmentMaxLineDistance2 -eq 2) 'XLD segmentation parameters did not round-trip.'
Assert-True (($segmentRecipe.InputBindings | Where-Object TargetPortName -eq 'XLD').SourceToolId -eq $contour.ToolId) 'XLD segmentation binding did not round-trip.'

$window.Close()
try { [IO.Directory]::Delete($tempRoot, $true) } catch { }
Write-Host 'S21 port drag and XLD segmentation smoke test passed.'
