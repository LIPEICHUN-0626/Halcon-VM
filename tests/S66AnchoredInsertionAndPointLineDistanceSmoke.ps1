param([string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot))

$ErrorActionPreference = 'Stop'
if ([Threading.Thread]::CurrentThread.ApartmentState -ne [Threading.ApartmentState]::STA) { throw 'Run with powershell.exe -STA.' }
$halcon = 'C:\Program Files\MVTec\HALCON-20.11-Progress'
$env:PATH = (Join-Path $halcon 'bin\x64-win64') + ';' + $env:PATH
Add-Type -AssemblyName PresentationFramework
Add-Type -AssemblyName System.Runtime.Serialization
[Reflection.Assembly]::LoadFrom((Join-Path $halcon 'bin\dotnet35\halcondotnet.dll')) | Out-Null
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path $ProjectRoot 'bin\x64\Debug\HalconWinFormsDemo.exe'))

function Assert([bool]$Condition, [string]$Message) { if (-not $Condition) { throw $Message } }
function Pump([int]$Milliseconds) { $frame = New-Object Windows.Threading.DispatcherFrame; $timer = New-Object Windows.Threading.DispatcherTimer; $timer.Interval = [TimeSpan]::FromMilliseconds($Milliseconds); $timer.Add_Tick({ $timer.Stop(); $frame.Continue = $false }); $timer.Start(); [Windows.Threading.Dispatcher]::PushFrame($frame) }

$main = $assembly.GetType('HalconWinFormsDemo.MainWindow', $true)
$serviceType = $assembly.GetType('HalconWinFormsDemo.Services.RecipeService', $true)
$loggerType = $assembly.GetType('HalconWinFormsDemo.Services.AppLogger', $true)
$kindType = $assembly.GetType('HalconWinFormsDemo.Models.VmToolKind', $true)
$metadataType = $assembly.GetType('HalconWinFormsDemo.Models.ToolMetadata', $true)
$pointType = $assembly.GetType('HalconWinFormsDemo.Models.VmPoint2D', $true)
$lineType = $assembly.GetType('HalconWinFormsDemo.Models.VmLine2D', $true)
$xldSnapshotType = $assembly.GetType('HalconWinFormsDemo.Models.VmXldSnapshot', $true)
$recipeType = $assembly.GetType('HalconWinFormsDemo.Models.VisionRecipe', $true)
$private = [Reflection.BindingFlags]'Instance,NonPublic'
$all = [Reflection.BindingFlags]'Instance,NonPublic,Public'
$static = [Reflection.BindingFlags]'Static,Public'
$temp = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s66-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($temp) | Out-Null
$service = $serviceType.GetConstructor([Type[]]@([string])).Invoke([object[]]@($temp.ToString()))
$logger = $loggerType.GetConstructor([Type[]]@([string])).Invoke([object[]]@($temp.ToString()))
$window = $main.GetConstructor($private, $null, [Type[]]@($serviceType, $loggerType), $null).Invoke([object[]]@($service, $logger))
$app = [Windows.Application]::Current
$ownsApp = $null -eq $app
if ($ownsApp) { $app = New-Object Windows.Application; $app.ShutdownMode = [Windows.ShutdownMode]::OnExplicitShutdown }
$window.ShowInTaskbar = $false; $window.Left = -10000; $window.Show(); Pump 180

function Field([string]$Name) { $value = $main.GetField($Name, $all).GetValue($window); Write-Output -NoEnumerate $value }
function Method([string]$Name, [object[]]$Parameters = @()) { try { $main.GetMethod($Name, $private).Invoke($window, $Parameters) } catch { $error = $_.Exception; while ($error.InnerException) { $error = $error.InnerException }; throw ($Name + ': ' + $error.Message) } }
function Kind([string]$Name) { [Enum]::Parse($kindType, $Name) }
function Tool([string]$KindName, [string]$Name) { Method 'CreateFlowTool' ([object[]]@((Kind $KindName), $Name, $true, $null)) }
function Run($Tool) { Method 'ExecuteFlowTool' ([object[]]@($Tool, 'S66 smoke')) }
function Output($Tool, [string]$Port) { $arguments = [object[]]@($Port, $null); if (-not $Tool.GetType().GetMethod('TryGetOutputValue').Invoke($Tool, $arguments)) { throw "Missing output $Port" }; $arguments[1] }
function Catalog([string]$KindName) { foreach ($entry in (Field 'toolCatalog')) { if ($entry.Kind.ToString() -eq $KindName) { return $entry } }; return $null }

try {
    # A: 61 real tools, no new permanent top command, selected-tool insertion preview and refusal reason.
    Assert ((Field 'toolCatalog').Count -ge 61) 'S66 catalog baseline dropped below 61 real tools.'
    Assert ((Field 'DeveloperToolbarPanel').Children.Count -eq 7) 'S66 added a permanent top toolbar command.'
    $distanceCatalog = Catalog 'PointToLineDistanceMeasurement'
    Assert ($null -ne $distanceCatalog) 'Point-to-line tool is missing from the toolbox.'
    (Field 'ToolCatalogList').SelectedItem = $distanceCatalog
    Method 'RefreshToolCapabilityPreviewS65' ([object[]]@($distanceCatalog)) | Out-Null
    Assert ((Field 'ToolCapabilityInputText').Text -match 'Point:Point2D.*Line:Line2D') 'Capability preview lost typed geometry inputs.'
    Assert ((Field 'ToolCapabilityInsertStatusText').Text.Length -gt 10 -and (Field 'ToolCapabilityInsertAnchorText').Text.Length -gt 8) 'No-anchor refusal reason is not visible.'

    # Formal Line2D clone/display/validation/JSON contract.
    $line = $lineType.GetMethod('CreateImagePixel', $static).Invoke($null, [object[]]@(10.0, 0.0, 10.0, 100.0, 'Fixture.Image'))
    Assert ([string]::IsNullOrEmpty($line.GetValidationError()) -and [Math]::Abs($line.Length - 100) -lt 0.0001) 'Valid Line2D was rejected.'
    $clone = $line.Clone()
    Assert (-not [object]::ReferenceEquals($line, $clone) -and $clone.ToDisplayString() -eq $line.ToDisplayString()) 'Line2D clone/display is not deterministic.'
    $serializer = [Runtime.Serialization.Json.DataContractJsonSerializer]::new($lineType)
    $stream = New-Object IO.MemoryStream
    try { $serializer.WriteObject($stream, $line); $stream.Position = 0; $round = $serializer.ReadObject($stream) } finally { $stream.Dispose() }
    Assert ($round.CoordinateContextId -eq 'Fixture.Image' -and [Math]::Abs($round.Length - 100) -lt 0.0001) 'Line2D JSON roundtrip failed.'
    $degenerate = $lineType.GetMethod('CreateImagePixel', $static).Invoke($null, [object[]]@(1.0, 2.0, 1.0, 2.0, 'Fixture.Image'))
    Assert (-not [string]::IsNullOrEmpty($degenerate.GetValidationError())) 'Degenerate Line2D was accepted.'

    # Real HALCON XLD -> line fitting -> typed MainLine and Point2D + Line2D -> HALCON point-line graphics -> NumericJudge.
    $flow = Field 'flowTools'; Method 'DisposeFlowTools' | Out-Null; $flow.Clear()
    $source = Tool 'ImageSource' 'Shared Image'
    $xldCarrier = Tool 'SubpixelEdgeExtraction' 'Line XLD Carrier'
    $fit = Tool 'XldLineFitting' 'Typed Main Line'
    $pointCarrier = Tool 'PointSetGeometryStatistics' 'Typed Point Carrier'
    $distance = Tool 'PointToLineDistanceMeasurement' 'Point Line Distance'
    $judge = Tool 'NumericJudge' 'Distance Judge'
    foreach ($tool in @($source, $xldCarrier, $fit, $pointCarrier, $distance, $judge)) { $flow.Add($tool) }
    $xldCarrier.SetInputBinding('Image', $source.ToolId, 'Image')
    $fit.SetInputBinding('XLD', $xldCarrier.ToolId, 'XLD')
    $context = $source.ToolId + '.Image'
    $pointCarrier.SetOutputValue('Centroid', $pointType.GetMethod('CreateImagePixel', $static).Invoke($null, [object[]]@(25.0, 40.0, $context)))
    $pointCarrier.ResultCode = 'OK'; $pointCarrier.RunStatus = 'Completed'
    $distance.SetInputBinding('Point', $pointCarrier.ToolId, 'Centroid')
    $distance.SetInputBinding('Line', $fit.ToolId, 'MainLine')
    $judge.SetInputBinding('Value', $distance.ToolId, 'Distance'); $judge.InputToolId = $distance.ToolId; $judge.InputPortName = 'Distance'; $judge.NumericOperator = 'BetweenInclusive'; $judge.NumericLowerLimit = 14.9; $judge.NumericUpperLimit = 15.1
    [HalconDotNet.HObject]$rawXld = $null
    try {
        [HalconDotNet.HOperatorSet]::GenContourPolygonXld([ref]$rawXld, [HalconDotNet.HTuple]@([double]10,[double]10,[double]10), [HalconDotNet.HTuple]@([double]0,[double]50,[double]100))
        $snapshot = $xldSnapshotType.GetMethod('Create', $static).Invoke($null, [object[]]@($rawXld, $xldCarrier.ToolId, $xldCarrier.InstanceName, 'XLD'))
        $xldCarrier.SetOutputValue('XLD', $snapshot); $xldCarrier.ResultCode = 'OK'; $xldCarrier.RunStatus = 'Completed'
    } finally { if ($rawXld) { $rawXld.Dispose() } }
    Assert ((Run $fit).ResultCode -eq 'OK') 'HALCON line fitting failed.'
    $mainLine = Output $fit 'MainLine'
    Assert ($mainLine.GetType() -eq $lineType -and $mainLine.CoordinateContextId -eq $context) 'Line fitting did not publish contextual MainLine:Line2D.'
    foreach ($legacy in @('StartRow','StartColumn','EndRow','EndColumn','MeanAngle','MaxResidual')) { Assert ($null -ne (Output $fit $legacy)) "Legacy line output $legacy was removed." }
    Assert ((Run $distance).ResultCode -eq 'OK') 'Point-to-line execution failed.'
    Assert ([Math]::Abs([double](Output $distance 'Distance') - 15) -lt 0.001) 'Unsigned point-line distance is wrong.'
    Assert ([Math]::Abs((Output $distance 'FootPoint').Row - 10) -lt 0.001 -and [Math]::Abs((Output $distance 'FootPoint').Column - 40) -lt 0.001) 'Foot point is wrong.'
    Assert ([Math]::Abs([double](Output $distance 'SignedSide') + 15) -lt 0.001 -and (Output $distance 'XLD').ObjectCount -eq 4) 'Signed side or managed XLD is wrong.'
    Assert ((Run $judge).ResultCode -eq 'OK') 'Distance did not drive NumericJudge.'

    # Right-positive parameter, monitor, recipe and stale/mixed/type failures.
    $distance.Parameters.PointLineSideConvention = 'RightPositive'; $distance.Parameters.PointLineOverlayCrossSize = 12
    Run $distance | Out-Null
    Assert ([Math]::Abs([double](Output $distance 'SignedSide') - 15) -lt 0.001) 'Right-positive convention did not invert the side value.'
    Method 'RefreshPortDataMonitorS64' ([object[]]@($distance)) | Out-Null
    $lineRows = @((Field 'portDataMonitorRows') | Where-Object DataType -eq 'Line2D')
    Assert ($lineRows.Count -ge 1 -and ($lineRows | Where-Object { $_.ContextText -match 'ImagePixel.*L=' }).Count -ge 1) 'Port monitor lost Line2D context.'
    $recipe = Method 'CaptureRecipe'
    $recipeSerializer = [Runtime.Serialization.Json.DataContractJsonSerializer]::new($recipeType)
    $stream = New-Object IO.MemoryStream
    try { $recipeSerializer.WriteObject($stream, $recipe); $stream.Position = 0; $saved = $recipeSerializer.ReadObject($stream) } finally { $stream.Dispose() }
    $savedDistance = $saved.ToolFlow | Where-Object ToolId -eq $distance.ToolId | Select-Object -First 1
    Assert ($savedDistance.Parameters.PointLineSideConvention -eq 'RightPositive' -and [Math]::Abs($savedDistance.Parameters.PointLineOverlayCrossSize - 12) -lt 0.001) 'Recipe lost point-line parameters.'
    $mixed = $pointType.GetMethod('CreateImagePixel', $static).Invoke($null, [object[]]@(25.0, 40.0, 'Other.Image')); $pointCarrier.SetOutputValue('Centroid', $mixed); $pointCarrier.ResultCode = 'OK'
    $failed = $false; try { Run $distance | Out-Null } catch { $failed = $true }; Assert $failed 'Mixed coordinate contexts were accepted.'
    $pointCarrier.SetOutputValue('Centroid', $pointType.GetMethod('CreateImagePixel', $static).Invoke($null, [object[]]@(25.0, 40.0, $context))); $pointCarrier.ResultCode = '--'
    $failed = $false; try { Run $distance | Out-Null } catch { $failed = $true }; Assert $failed 'Stale point input was accepted.'
    $pointCarrier.ResultCode = 'OK'; $distance.SetInputBinding('Point', $pointCarrier.ToolId, 'PointCount')
    $failed = $false; try { Run $distance | Out-Null } catch { $failed = $true }; Assert $failed 'Wrong point port type was accepted.'
    $distance.SetInputBinding('Point', $pointCarrier.ToolId, 'Centroid')

    # Anchored after insertion finds both branches; undo restores. Before insertion bridges a real XLD connection.
    (Field 'ToolCatalogList').SelectedItem = $distanceCatalog
    (Field 'FlowToolList').SelectedItems.Clear(); (Field 'FlowToolList').SelectedItem = $pointCarrier
    Method 'RefreshToolCapabilityPreviewS65' ([object[]]@($distanceCatalog)) | Out-Null
    Assert ((Field 'ToolCapabilityCompatibilityText').Text -match 'Point.*Centroid' -and (Field 'ToolCapabilityCompatibilityText').Text -match 'Line.*MainLine') 'Insertion preview did not find both typed branches.'
    $beforeCount = $flow.Count; Method 'InsertSelectedCatalogToolAtAnchorS66' ([object[]]@($true)) | Out-Null
    Assert ($flow.Count -eq $beforeCount + 1) 'Anchored after insertion did not add exactly one tool.'
    $inserted = $flow[$flow.IndexOf($pointCarrier) + 1]
    Assert ($inserted.Kind.ToString() -eq 'PointToLineDistanceMeasurement' -and $inserted.GetInputBinding('Point').SourceToolId -eq $pointCarrier.ToolId -and $inserted.GetInputBinding('Line').SourceToolId -eq $fit.ToolId) 'Anchored insertion did not write both real typed bindings.'
    Method 'UndoFlowEditButton_Click' ([object[]]@($null, $null)) | Out-Null
    Assert ($flow.Count -eq $beforeCount) 'Undo did not restore anchored insertion.'
    $fit = $flow | Where-Object ToolId -eq $fit.ToolId | Select-Object -First 1
    $xldCarrier = $flow | Where-Object ToolId -eq $xldCarrier.ToolId | Select-Object -First 1
    $pointCarrier = $flow | Where-Object ToolId -eq $pointCarrier.ToolId | Select-Object -First 1
    $distance = $flow | Where-Object ToolId -eq $distance.ToolId | Select-Object -First 1
    $smoothingCatalog = Catalog 'XldContourSmoothing'; (Field 'ToolCatalogList').SelectedItem = $smoothingCatalog; (Field 'FlowToolList').SelectedItem = $fit
    Method 'InsertSelectedCatalogToolAtAnchorS66' ([object[]]@($false)) | Out-Null
    $bridge = $flow[$flow.IndexOf($fit) - 1]
    Assert ($bridge.Kind.ToString() -eq 'XldContourSmoothing' -and $bridge.GetInputBinding('XLD').SourceToolId -eq $xldCarrier.ToolId -and $fit.GetInputBinding('XLD').SourceToolId -eq $bridge.ToolId) 'Before insertion did not bridge the existing real XLD connection.'

    # Copy remap keeps Point2D + Line2D subscriptions.
    (Field 'FlowToolList').SelectedItems.Clear(); (Field 'FlowToolList').SelectedItems.Add($fit); (Field 'FlowToolList').SelectedItems.Add($pointCarrier); (Field 'FlowToolList').SelectedItems.Add($distance)
    Assert ([int](Method 'CopySelectedFlowToolsToClipboard') -eq 3) 'Typed geometry block copy failed.'
    Assert ([int](Method 'PasteFlowToolClipboard' ([object[]]@($flow.Count))) -eq 3) 'Typed geometry block paste failed.'
    $copies = @($flow | Select-Object -Last 3); $copyFit = $copies | Where-Object { $_.Kind.ToString() -eq 'XldLineFitting' } | Select-Object -First 1; $copyPoint = $copies | Where-Object { $_.Kind.ToString() -eq 'PointSetGeometryStatistics' } | Select-Object -First 1; $copyDistance = $copies | Where-Object { $_.Kind.ToString() -eq 'PointToLineDistanceMeasurement' } | Select-Object -First 1
    Assert ($copyDistance.GetInputBinding('Point').SourceToolId -eq $copyPoint.ToolId -and $copyDistance.GetInputBinding('Line').SourceToolId -eq $copyFit.ToolId) 'Typed geometry block copy did not remap both sources.'
    Write-Output 'S66 anchored insertion + compatibility preview + Line2D + typed MainLine + HALCON point-to-line + monitor/recipe/copy/failures passed'
}
finally {
    $main.GetField('recipeCloseConfirmed', $private).SetValue($window, $true)
    $window.Close()
    if ($ownsApp) { $app.Shutdown() }
    if (Test-Path $temp) { Remove-Item -LiteralPath $temp -Recurse -Force }
}
