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
$pointType = $assembly.GetType('HalconWinFormsDemo.Models.VmPoint2D', $true)
$circleType = $assembly.GetType('HalconWinFormsDemo.Models.VmCircle2D', $true)
$xldSnapshotType = $assembly.GetType('HalconWinFormsDemo.Models.VmXldSnapshot', $true)
$recipeType = $assembly.GetType('HalconWinFormsDemo.Models.VisionRecipe', $true)
$private = [Reflection.BindingFlags]'Instance,NonPublic'
$all = [Reflection.BindingFlags]'Instance,NonPublic,Public'
$static = [Reflection.BindingFlags]'Static,Public'
$temp = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s67-' + [Guid]::NewGuid().ToString('N'))
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
function Run($Tool) { Method 'ExecuteFlowTool' ([object[]]@($Tool, 'S67 smoke')) }
function Output($Tool, [string]$Port) { $arguments = [object[]]@($Port, $null); if (-not $Tool.GetType().GetMethod('TryGetOutputValue').Invoke($Tool, $arguments)) { throw "Missing output $Port" }; $arguments[1] }
function Catalog([string]$KindName) { foreach ($entry in (Field 'toolCatalog')) { if ($entry.Kind.ToString() -eq $KindName) { return $entry } }; return $null }

try {
    Assert ((Field 'toolCatalog').Count -eq [Enum]::GetValues($kindType).Count -and (Field 'toolCatalog').Count -ge 62) 'S67 catalog baseline or full implemented-tool coverage regressed.'
    Assert ((Field 'DeveloperToolbarPanel').Children.Count -eq 7) 'S67 added a permanent top toolbar command.'
    Assert ($null -ne (Catalog 'PointToCircleRadialDeviationMeasurement')) 'Point-to-circle tool is missing from the toolbox.'

    # Formal Circle2D clone/display/validation/JSON contract.
    $circle = $circleType.GetMethod('CreateImagePixel', $static).Invoke($null, [object[]]@(100.0, 200.0, 50.0, 'Fixture.Image'))
    Assert ([string]::IsNullOrEmpty($circle.GetValidationError())) 'Valid Circle2D was rejected.'
    $clone = $circle.Clone()
    Assert (-not [object]::ReferenceEquals($circle, $clone) -and $clone.ToDisplayString() -eq $circle.ToDisplayString()) 'Circle2D clone/display is not deterministic.'
    $serializer = [Runtime.Serialization.Json.DataContractJsonSerializer]::new($circleType)
    $stream = New-Object IO.MemoryStream
    try { $serializer.WriteObject($stream, $circle); $stream.Position = 0; $round = $serializer.ReadObject($stream) } finally { $stream.Dispose() }
    Assert ($round.CoordinateContextId -eq 'Fixture.Image' -and [Math]::Abs($round.Radius - 50) -lt 0.0001) 'Circle2D JSON roundtrip failed.'
    $badCircle = $circleType.GetMethod('CreateImagePixel', $static).Invoke($null, [object[]]@(0.0, 0.0, 0.0, 'Fixture.Image'))
    Assert (-not [string]::IsNullOrEmpty($badCircle.GetValidationError())) 'Degenerate Circle2D was accepted.'

    # Real HALCON circle XLD -> fit -> MainCircle and Point2D + Circle2D -> radial deviation -> NumericJudge.
    $flow = Field 'flowTools'; Method 'DisposeFlowTools' | Out-Null; $flow.Clear()
    $source = Tool 'ImageSource' 'Shared Image'
    $xldCarrier = Tool 'SubpixelEdgeExtraction' 'Circle XLD Carrier'
    $fit = Tool 'XldCircleFitting' 'Typed Main Circle'
    $pointCarrier = Tool 'PointSetGeometryStatistics' 'Typed Point Carrier'
    $radial = Tool 'PointToCircleRadialDeviationMeasurement' 'Radial Deviation'
    $judge = Tool 'NumericJudge' 'Radial Judge'
    foreach ($tool in @($source, $xldCarrier, $fit, $pointCarrier, $radial, $judge)) { $flow.Add($tool) }
    $xldCarrier.SetInputBinding('Image', $source.ToolId, 'Image')
    $fit.SetInputBinding('XLD', $xldCarrier.ToolId, 'XLD')
    $context = $source.ToolId + '.Image'
    $pointCarrier.SetOutputValue('Centroid', $pointType.GetMethod('CreateImagePixel', $static).Invoke($null, [object[]]@(100.0, 260.0, $context)))
    $pointCarrier.ResultCode = 'OK'; $pointCarrier.RunStatus = 'Completed'
    $radial.SetInputBinding('Point', $pointCarrier.ToolId, 'Centroid')
    $radial.SetInputBinding('Circle', $fit.ToolId, 'MainCircle')
    $judge.SetInputBinding('Value', $radial.ToolId, 'AbsoluteDeviation'); $judge.InputToolId = $radial.ToolId; $judge.InputPortName = 'AbsoluteDeviation'; $judge.NumericOperator = 'BetweenInclusive'; $judge.NumericLowerLimit = 9.9; $judge.NumericUpperLimit = 10.1
    [HalconDotNet.HObject]$rawXld = $null
    try {
        [HalconDotNet.HOperatorSet]::GenCircleContourXld([ref]$rawXld, 100.0, 200.0, 50.0, 0.0, [Math]::PI * 2.0, 'positive', 1.0)
        $snapshot = $xldSnapshotType.GetMethod('Create', $static).Invoke($null, [object[]]@($rawXld, $xldCarrier.ToolId, $xldCarrier.InstanceName, 'XLD'))
        $xldCarrier.SetOutputValue('XLD', $snapshot); $xldCarrier.ResultCode = 'OK'; $xldCarrier.RunStatus = 'Completed'
    } finally { if ($rawXld) { $rawXld.Dispose() } }
    Assert ((Run $fit).ResultCode -eq 'OK') 'HALCON circle fitting failed.'
    $mainCircle = Output $fit 'MainCircle'
    Assert ($mainCircle.GetType() -eq $circleType -and $mainCircle.CoordinateContextId -eq $context -and [Math]::Abs($mainCircle.Radius - 50) -lt 0.1) 'Circle fitting did not publish contextual MainCircle:Circle2D.'
    foreach ($legacy in @('CenterRow','CenterColumn','Radius','StartPhi','EndPhi','MaxResidual')) { Assert ($null -ne (Output $fit $legacy)) "Legacy circle output $legacy was removed." }
    Assert ((Run $radial).ResultCode -eq 'OK') 'Point-to-circle radial execution failed.'
    Assert ([Math]::Abs([double](Output $radial 'CenterDistance') - 60) -lt 0.1) 'Center distance is wrong.'
    Assert ([Math]::Abs([double](Output $radial 'AbsoluteDeviation') - 10) -lt 0.1 -and [Math]::Abs([double](Output $radial 'SignedDeviation') - 10) -lt 0.1) 'Radial deviation is wrong.'
    Assert ([Math]::Abs((Output $radial 'ClosestPoint').Column - 250) -lt 0.1 -and (Output $radial 'XLD').ObjectCount -eq 5) 'Closest point or managed XLD is wrong.'
    Assert ((Run $judge).ResultCode -eq 'OK') 'Radial deviation did not drive NumericJudge.'

    $radial.Parameters.PointCircleSignConvention = 'InsidePositive'; $radial.Parameters.PointCircleOverlayCrossSize = 12
    Run $radial | Out-Null
    Assert ([Math]::Abs([double](Output $radial 'SignedDeviation') + 10) -lt 0.1) 'Inside-positive convention did not invert the deviation.'
    Method 'RefreshPortDataMonitorS64' ([object[]]@($radial)) | Out-Null
    $circleRows = @((Field 'portDataMonitorRows') | Where-Object DataType -eq 'Circle2D')
    Assert ($circleRows.Count -ge 1 -and ($circleRows | Where-Object { $_.ContextText -match 'ImagePixel.*R=' }).Count -ge 1) 'Port monitor lost Circle2D context.'
    $recipe = Method 'CaptureRecipe'
    $recipeSerializer = [Runtime.Serialization.Json.DataContractJsonSerializer]::new($recipeType)
    $stream = New-Object IO.MemoryStream
    try { $recipeSerializer.WriteObject($stream, $recipe); $stream.Position = 0; $saved = $recipeSerializer.ReadObject($stream) } finally { $stream.Dispose() }
    $savedRadial = $saved.ToolFlow | Where-Object ToolId -eq $radial.ToolId | Select-Object -First 1
    Assert ($savedRadial.Parameters.PointCircleSignConvention -eq 'InsidePositive' -and [Math]::Abs($savedRadial.Parameters.PointCircleOverlayCrossSize - 12) -lt 0.001) 'Recipe lost point-circle parameters.'

    $pointCarrier.SetOutputValue('Centroid', $pointType.GetMethod('CreateImagePixel', $static).Invoke($null, [object[]]@(100.0, 260.0, 'Other.Image'))); $pointCarrier.ResultCode = 'OK'
    $failed = $false; try { Run $radial | Out-Null } catch { $failed = $true }; Assert $failed 'Mixed coordinate contexts were accepted.'
    $pointCarrier.SetOutputValue('Centroid', $pointType.GetMethod('CreateImagePixel', $static).Invoke($null, [object[]]@($mainCircle.CenterRow, $mainCircle.CenterColumn, $context))); $pointCarrier.ResultCode = 'OK'
    $failed = $false; try { Run $radial | Out-Null } catch { $failed = $true }; Assert $failed 'Point at circle center was accepted.'
    $pointCarrier.SetOutputValue('Centroid', $pointType.GetMethod('CreateImagePixel', $static).Invoke($null, [object[]]@(100.0, 260.0, $context))); $pointCarrier.ResultCode = '--'
    $failed = $false; try { Run $radial | Out-Null } catch { $failed = $true }; Assert $failed 'Stale point input was accepted.'

    # OUT-port recommendation context: only compatible required inputs, full-catalog return, real insert/autobind, undo and copy remap.
    Method 'DisposeFlowTools' | Out-Null; $flow.Clear()
    $source = Tool 'ImageSource' 'Recommend Image'
    $pointCarrier = Tool 'PointSetGeometryStatistics' 'Recommend Point'
    $xldCarrier = Tool 'SubpixelEdgeExtraction' 'Recommend Circle XLD'
    $fit = Tool 'XldCircleFitting' 'Recommend Circle'
    foreach ($tool in @($source, $pointCarrier, $xldCarrier, $fit)) { $flow.Add($tool) }
    Method 'EnterToolRecommendationContextS67' ([object[]]@($fit.ToolId, 'MainCircle')) | Out-Null
    Assert ((Field 'ToolRecommendationContextBorder').Visibility.ToString() -eq 'Visible' -and (Field 'ToolCatalogModePanel').Visibility.ToString() -eq 'Collapsed') 'Recommendation context did not replace catalog modes on demand.'
    $visible = @(); foreach ($entry in (Field 'toolCatalogView')) { $visible += $entry }
    $visibleKinds = @($visible | ForEach-Object { $_.Kind.ToString() })
    Assert ($visible.Count -eq 3 -and $visibleKinds -contains 'PointToCircleRadialDeviationMeasurement' -and $visibleKinds -contains 'LineCircleIntersectionMeasurement' -and $visibleKinds -contains 'CircleCircleIntersectionMeasurement') 'Recommendation list does not match the three tools with a compatible required Circle2D input.'
    Assert ((Field 'ToolRecommendationSourceText').Text -match 'Recommend Circle.*MainCircle.*Circle2D' -and (Field 'ToolRecommendationRejectText').Text.Length -gt 10) 'Recommendation source/type/rejection summary is missing.'
    (Field 'ToolCatalogList').SelectedItem = ($visible | Where-Object { $_.Kind.ToString() -eq 'PointToCircleRadialDeviationMeasurement' } | Select-Object -First 1)
    $beforeCount = $flow.Count
    Method 'InsertSelectedRecommendedToolS67' | Out-Null
    Assert ($flow.Count -eq $beforeCount + 1) 'Recommended insertion did not add exactly one tool.'
    $inserted = $flow[$flow.IndexOf($fit) + 1]
    Assert ($inserted.Kind.ToString() -eq 'PointToCircleRadialDeviationMeasurement') 'Recommended tool was not inserted immediately after the OUT source.'
    Assert ($inserted.GetInputBinding('Circle').SourceToolId -eq $fit.ToolId -and $inserted.GetInputBinding('Circle').SourcePortName -eq 'MainCircle') 'Selected OUT was not written to the compatible target input.'
    Assert ($inserted.GetInputBinding('Point').SourceToolId -eq $pointCarrier.ToolId) 'Remaining required Point2D input was not filled from the nearest real source.'
    Assert ((Field 'selectedFlowConnectionKey') -match $inserted.ToolId -and (Field 'FlowToolList').SelectedItem.ToolId -eq $inserted.ToolId) 'Inserted connection/tool was not highlighted.'
    (Field 'FlowToolList').SelectedItems.Clear(); (Field 'FlowToolList').SelectedItems.Add($pointCarrier); (Field 'FlowToolList').SelectedItems.Add($fit); (Field 'FlowToolList').SelectedItems.Add($inserted)
    Assert ([int](Method 'CopySelectedFlowToolsToClipboard') -eq 3) 'Circle geometry block copy failed.'
    Assert ([int](Method 'PasteFlowToolClipboard' ([object[]]@($flow.Count))) -eq 3) 'Circle geometry block paste failed.'
    $copies = @($flow | Select-Object -Last 3); $copyPoint = $copies | Where-Object { $_.Kind.ToString() -eq 'PointSetGeometryStatistics' } | Select-Object -First 1; $copyFit = $copies | Where-Object { $_.Kind.ToString() -eq 'XldCircleFitting' } | Select-Object -First 1; $copyRadial = $copies | Where-Object { $_.Kind.ToString() -eq 'PointToCircleRadialDeviationMeasurement' } | Select-Object -First 1
    Assert ($copyRadial.GetInputBinding('Point').SourceToolId -eq $copyPoint.ToolId -and $copyRadial.GetInputBinding('Circle').SourceToolId -eq $copyFit.ToolId) 'Circle geometry block copy did not remap both typed sources.'
    Method 'ToolRecommendationBackButton_Click' ([object[]]@($null, $null)) | Out-Null
    $restored = @(); foreach ($entry in (Field 'toolCatalogView')) { $restored += $entry }
    Assert ((Field 'ToolRecommendationContextBorder').Visibility.ToString() -eq 'Collapsed' -and $restored.Count -eq [Enum]::GetValues($kindType).Count -and $restored.Count -ge 62) 'Return to full catalog did not restore the full implemented-tool set.'

    Write-Output 'S67 OUT-port recommendations + real insertion/autobind + full-catalog return + Circle2D + HALCON point-circle radial deviation + monitor/recipe/copy/failures passed; BSB not implemented.'
}
finally {
    $main.GetField('recipeCloseConfirmed', $private).SetValue($window, $true)
    $window.Close()
    if ($ownsApp) { $app.Shutdown() }
    if (Test-Path $temp) { Remove-Item -LiteralPath $temp -Recurse -Force }
}
