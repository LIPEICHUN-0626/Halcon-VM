param([string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot))

$ErrorActionPreference = 'Stop'
if ([Threading.Thread]::CurrentThread.ApartmentState -ne [Threading.ApartmentState]::STA) { throw 'Run with powershell.exe -STA.' }
$halcon = 'C:\Program Files\MVTec\HALCON-20.11-Progress'
$env:PATH = (Join-Path $halcon 'bin\x64-win64') + ';' + $env:PATH
Add-Type -AssemblyName PresentationFramework
Add-Type -AssemblyName System.Runtime.Serialization
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path $ProjectRoot 'bin\x64\Debug\HalconWinFormsDemo.exe'))

function Assert([bool]$Condition, [string]$Message) { if (-not $Condition) { throw $Message } }
function Pump([int]$Milliseconds) {
    $frame = New-Object Windows.Threading.DispatcherFrame
    $timer = New-Object Windows.Threading.DispatcherTimer
    $timer.Interval = [TimeSpan]::FromMilliseconds($Milliseconds)
    $timer.Add_Tick({ $timer.Stop(); $frame.Continue = $false })
    $timer.Start()
    [Windows.Threading.Dispatcher]::PushFrame($frame)
}
function Write-CirclePgm([string]$Path) {
    $width = 360; $height = 300
    $header = [Text.Encoding]::ASCII.GetBytes("P5`n$width $height`n255`n")
    $pixels = New-Object byte[] ($width * $height)
    for ($row = 0; $row -lt $height; $row++) {
        for ($column = 0; $column -lt $width; $column++) {
            $dr = $row - 150; $dc = $column - 180
            $pixels[$row * $width + $column] = if ([Math]::Sqrt($dr * $dr + $dc * $dc) -le 90) { [byte]230 } else { [byte]20 }
        }
    }
    $bytes = New-Object byte[] ($header.Length + $pixels.Length)
    [Array]::Copy($header, 0, $bytes, 0, $header.Length)
    [Array]::Copy($pixels, 0, $bytes, $header.Length, $pixels.Length)
    [IO.File]::WriteAllBytes($Path, $bytes)
}

$main = $assembly.GetType('HalconWinFormsDemo.MainWindow', $true)
$serviceType = $assembly.GetType('HalconWinFormsDemo.Services.RecipeService', $true)
$loggerType = $assembly.GetType('HalconWinFormsDemo.Services.AppLogger', $true)
$kindType = $assembly.GetType('HalconWinFormsDemo.Models.VmToolKind', $true)
$metadataType = $assembly.GetType('HalconWinFormsDemo.Models.ToolMetadata', $true)
$pointType = $assembly.GetType('HalconWinFormsDemo.Models.VmPoint2D', $true)
$arrayType = $assembly.GetType('HalconWinFormsDemo.Models.VmPoint2DArray', $true)
$recipeType = $assembly.GetType('HalconWinFormsDemo.Models.VisionRecipe', $true)
$private = [Reflection.BindingFlags]'Instance,NonPublic'
$all = [Reflection.BindingFlags]'Instance,NonPublic,Public'
$static = [Reflection.BindingFlags]'Static,Public'
$temp = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s65-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($temp) | Out-Null
$imagePath = Join-Path $temp 'circle.pgm'
Write-CirclePgm $imagePath
$service = $serviceType.GetConstructor([Type[]]@([string])).Invoke([object[]]@($temp.ToString()))
$logger = $loggerType.GetConstructor([Type[]]@([string])).Invoke([object[]]@($temp.ToString()))
$window = $main.GetConstructor($private, $null, [Type[]]@($serviceType, $loggerType), $null).Invoke([object[]]@($service, $logger))
$app = [Windows.Application]::Current
$ownsApp = $null -eq $app
if ($ownsApp) { $app = New-Object Windows.Application; $app.ShutdownMode = [Windows.ShutdownMode]::OnExplicitShutdown }
$window.ShowInTaskbar = $false
$window.Left = -10000
$window.Show()
Pump 180

function Field([string]$Name) {
    $value = $main.GetField($Name, $all).GetValue($window)
    Write-Output -NoEnumerate $value
}
function Method([string]$Name, [object[]]$Parameters = @()) {
    try { $main.GetMethod($Name, $private).Invoke($window, $Parameters) }
    catch { $error = $_.Exception; while ($error.InnerException) { $error = $error.InnerException }; throw ($Name + ': ' + $error.Message) }
}
function Kind([string]$Name) { [Enum]::Parse($kindType, $Name) }
function Tool([string]$KindName, [string]$Name) { Method 'CreateFlowTool' ([object[]]@((Kind $KindName), $Name, $true, $null)) }
function Run($Tool) { Method 'ExecuteFlowTool' ([object[]]@($Tool, 'S65 smoke')) }
function Output($Tool, [string]$Port) {
    $arguments = [object[]]@($Port, $null)
    if (-not $Tool.GetType().GetMethod('TryGetOutputValue').Invoke($Tool, $arguments)) { throw "Missing output $Port" }
    $arguments[1]
}
function Point([double]$Row, [double]$Column, [string]$Context) {
    $pointType.GetMethod('CreateImagePixel', $static).Invoke($null, [object[]]@($Row, $Column, $Context))
}

try {
    # A: the full catalog stays compact; detail is selected-tool metadata, not a new permanent command.
    $catalog = Field 'toolCatalog'
    Assert ($catalog.Count -eq [Enum]::GetValues($kindType).Count -and $catalog.Count -ge 60) 'S65 catalog baseline or full implemented-tool coverage regressed.'
    Assert (-not (Field 'ToolCapabilityExpander').IsExpanded) 'Capability preview is not collapsed by default.'
    Assert ((Field 'DeveloperToolbarPanel').Children.Count -eq 7) 'S65 added a permanent toolbar command.'
    $item = $catalog | Where-Object { $_.Kind.ToString() -eq 'PointSetGeometryStatistics' } | Select-Object -First 1
    Assert ($null -ne $item -and $item.ImplementedCategory -match '^已实现') 'Implemented grouping is missing.'
    $itemBase = $item.PSObject.BaseObject
    (Field 'ToolCatalogList').SelectedItem = $itemBase
    Method 'RefreshToolCapabilityPreviewS65' ([object[]]@($itemBase)) | Out-Null
    Assert ((Field 'ToolCapabilityInputText').Text -match 'Points:Point2D\[\].*必需') 'Preview did not derive the required typed input.'
    Assert ((Field 'ToolCapabilityOutputText').Text -match 'Centroid:Point2D') 'Preview did not derive typed outputs.'
    Assert ((Field 'ToolCapabilityPurposeText').Text -match 'HALCON') 'Preview purpose is not real metadata.'

    # Deterministic Point2D/Point2D[] contract and JSON roundtrip.
    $array = $arrayType.GetMethod('CreateImagePixel', $static).Invoke($null, [object[]]@([double[]](10, 20, 30), [double[]](5, 15, 25), 'Fixture.Image'))
    Assert ([string]::IsNullOrEmpty($array.GetValidationError($false))) 'Valid Point2D[] was rejected.'
    $clone = $array.Clone()
    Assert (-not [object]::ReferenceEquals($array, $clone) -and $clone.ToDisplayString() -eq $array.ToDisplayString()) 'Point2D[] clone/display is not deterministic.'
    $serializer = [Runtime.Serialization.Json.DataContractJsonSerializer]::new($arrayType)
    $stream = New-Object IO.MemoryStream
    try { $serializer.WriteObject($stream, $array); $stream.Position = 0; $round = $serializer.ReadObject($stream) } finally { $stream.Dispose() }
    Assert ($round.Count -eq 3 -and $round.CoordinateContextId -eq 'Fixture.Image') 'Point2D[] JSON roundtrip failed.'
    $badPoint = Point ([double]::NaN) 2 'Fixture.Image'
    Assert ($badPoint.GetValidationError() -match '有限') 'Point2D accepted NaN.'

    # Old coordinate strings remain; a parallel typed output and strict typed consumer are real metadata.
    $circleOutputs = $metadataType.GetMethod('GetOutputPorts', $static).Invoke($null, [object[]]@((Kind 'CircularCaliperCircleFinder')))
    Assert (($circleOutputs | Where-Object { $_.PortName -eq 'EdgePoints' -and $_.DataType -eq 'Point2D[]' }).Count -eq 1) 'Circular caliper EdgePoints is missing.'
    foreach ($legacy in @('EdgeRows', 'EdgeColumns')) { Assert (($circleOutputs | Where-Object PortName -eq $legacy).Count -eq 1) "Legacy $legacy was removed." }
    $pointInputs = $metadataType.GetMethod('GetInputPorts', $static).Invoke($null, [object[]]@((Kind 'PointSetGeometryStatistics')))
    Assert (($pointInputs | Where-Object { $_.PortName -eq 'Points' -and $_.DataType -eq 'Point2D[]' -and -not $_.IsOptional }).Count -eq 1) 'Point-set input metadata is wrong.'

    # Real Image -> HALCON circular caliper -> typed points -> HALCON point geometry -> NumericJudge.
    $flow = Field 'flowTools'
    Method 'DisposeFlowTools' | Out-Null
    $flow.Clear()
    $source = Tool 'ImageSource' 'Circle Image'
    $circle = Tool 'CircularCaliperCircleFinder' 'Circle Edge Points'
    $stats = Tool 'PointSetGeometryStatistics' 'Point Geometry'
    $judge = Tool 'NumericJudge' 'Span Judge'
    foreach ($tool in @($source, $circle, $stats, $judge)) { $flow.Add($tool) }
    $source.Parameters.LocalImagePath = $imagePath
    $circle.SetInputBinding('Image', $source.ToolId, 'Image')
    $circle.Parameters.CircularCaliperCenterRow = 150; $circle.Parameters.CircularCaliperCenterColumn = 180
    $circle.Parameters.CircularCaliperSearchRadius = 90; $circle.Parameters.CircularCaliperStartAngleDegrees = 0; $circle.Parameters.CircularCaliperEndAngleDegrees = 360
    $circle.Parameters.CircularCaliperCount = 24; $circle.Parameters.CircularCaliperLength1 = 25; $circle.Parameters.CircularCaliperLength2 = 4
    $circle.Parameters.CircularCaliperSigma = 1; $circle.Parameters.CircularCaliperThreshold = 20; $circle.Parameters.CircularCaliperTransition = 'Negative'; $circle.Parameters.CircularCaliperSelect = 'All'
    $stats.SetInputBinding('Points', $circle.ToolId, 'EdgePoints'); $stats.Parameters.PointSetMinimumCount = 12; $stats.Parameters.PointSetAbnormalPolicy = 'Reject'
    $judge.SetInputBinding('Value', $stats.ToolId, 'MaxSpan'); $judge.InputToolId = $stats.ToolId; $judge.InputPortName = 'MaxSpan'; $judge.NumericOperator = 'BetweenInclusive'; $judge.NumericLowerLimit = 170; $judge.NumericUpperLimit = 190
    Assert ((Run $source).ResultCode -eq 'OK') 'Image source failed.'
    Assert ((Run $circle).ResultCode -eq 'OK') 'Real circular caliper failed.'
    $edgePoints = Output $circle 'EdgePoints'
    Assert ($edgePoints.Count -eq 24 -and $edgePoints.CoordinateContextId -eq ($source.ToolId + '.Image')) 'Real measurement did not publish contextual typed points.'
    Assert ((Run $stats).ResultCode -eq 'OK') 'Point-set geometry execution failed.'
    Assert ([int][double](Output $stats 'PointCount') -eq 24) 'Point count is wrong.'
    Assert ([Math]::Abs([double](Output $stats 'CentroidRow') - 150) -lt 1 -and [Math]::Abs([double](Output $stats 'CentroidColumn') - 180) -lt 1) 'Point centroid is wrong.'
    Assert ((Output $stats 'Centroid').GetType() -eq $pointType -and (Output $stats 'XLD').ObjectCount -ge 3) 'Typed centroid or managed XLD is missing.'
    Assert ((Run $judge).ResultCode -eq 'OK') 'Point-set Number did not drive NumericJudge.'

    # Connection canvas and S64 monitor see the exact Point2D[] type and context.
    Method 'RefreshFlowPortVisualization' | Out-Null
    $connections = @()
    foreach ($entry in (Field 'flowConnections')) { if ($entry.TargetToolId -eq $stats.ToolId -and $entry.TargetPortName -eq 'Points') { $connections += $entry } }
    Assert ($connections.Count -ge 1 -and ($connections | Where-Object DataType -eq 'Point2D[]').Count -ge 1) 'Connection canvas lost Point2D[] type.'
    $connection = $connections[0]
    $main.GetField('selectedFlowConnectionKey', $private).SetValue($window, $connection.ConnectionKey)
    Method 'RefreshPortDataMonitorS64' ([object[]]@($stats)) | Out-Null
    $monitorRows = @()
    foreach ($entry in (Field 'portDataMonitorRows')) { if ($entry.DataType -eq 'Point2D[]') { $monitorRows += $entry } }
    Assert ($monitorRows.Count -ge 2 -and ($monitorRows | Where-Object { $_.StateKey -eq 'Valid' -and $_.ContextText -match 'ImagePixel.*24 点' }).Count -ge 2) 'Port monitor lost typed points/context.'

    # Exact type, mixed context, NaN, skip policy and stale/not-run failure paths.
    $stats.SetInputBinding('Points', $circle.ToolId, 'Radius')
    $failed = $false; try { Run $stats | Out-Null } catch { $failed = $_.Exception.Message -match '类型不匹配' }
    Assert $failed 'Wrong port type was accepted.'
    $stats.SetInputBinding('Points', $circle.ToolId, 'EdgePoints')
    $invalid = $arrayType.GetMethod('CreateImagePixel', $static).Invoke($null, [object[]]@([double[]](1, 2, 3), [double[]](4, 5, 6), 'Fixture.Image'))
    $invalid.Points[1].CoordinateContextId = 'Other.Image'; $circle.SetOutputValue('EdgePoints', $invalid); $circle.ResultCode = 'OK'
    $failed = $false; try { Run $stats | Out-Null } catch { $failed = $_.Exception.Message -match '上下文' }
    Assert $failed 'Mixed contexts were accepted.'
    $nanArray = $arrayType.GetMethod('CreateImagePixel', $static).Invoke($null, [object[]]@([double[]](1, 2, 3, 4), [double[]](5, 6, 7, 8), 'Fixture.Image'))
    $nanArray.Points[1].Row = [double]::NaN; $circle.SetOutputValue('EdgePoints', $nanArray); $stats.Parameters.PointSetAbnormalPolicy = 'Reject'
    $failed = $false; try { Run $stats | Out-Null } catch { $failed = $_.Exception.Message -match '有限' }
    Assert $failed 'Reject policy accepted NaN.'
    $stats.Parameters.PointSetAbnormalPolicy = 'SkipInvalid'; $stats.Parameters.PointSetMinimumCount = 3
    Assert ((Run $stats).ResultCode -eq 'NG' -and [int][double](Output $stats 'SkippedCount') -eq 1) 'Skip-invalid did not retain statistics and mark NG.'
    $circle.ResultCode = '--'; $failed = $false; try { Run $stats | Out-Null } catch { $failed = $_.Exception.Message -match '本周期' }
    Assert $failed 'Stale/not-run source was accepted.'

    # Recipe, block-copy remap and invalidation retain the typed contract.
    $circle.ResultCode = 'OK'; $circle.SetOutputValue('EdgePoints', $edgePoints); $stats.Parameters.PointSetAbnormalPolicy = 'Reject'; $stats.Parameters.PointSetMinimumCount = 12
    $recipe = Method 'CaptureRecipe'
    $recipeSerializer = [Runtime.Serialization.Json.DataContractJsonSerializer]::new($recipeType)
    $stream = New-Object IO.MemoryStream
    try { $recipeSerializer.WriteObject($stream, $recipe); $stream.Position = 0; $saved = $recipeSerializer.ReadObject($stream) } finally { $stream.Dispose() }
    $savedStats = $saved.ToolFlow | Where-Object ToolId -eq $stats.ToolId | Select-Object -First 1
    Assert ($savedStats.Parameters.PointSetMinimumCount -eq 12 -and ($savedStats.InputBindings | Where-Object { $_.TargetPortName -eq 'Points' -and $_.SourcePortName -eq 'EdgePoints' }).Count -eq 1) 'Recipe lost typed point-set configuration.'
    (Field 'FlowToolList').SelectedItems.Clear(); (Field 'FlowToolList').SelectedItems.Add($circle); (Field 'FlowToolList').SelectedItems.Add($stats)
    Assert ([int](Method 'CopySelectedFlowToolsToClipboard') -eq 2) 'Point-set block copy failed.'
    Assert ([int](Method 'PasteFlowToolClipboard' ([object[]]@($flow.Count))) -eq 2) 'Point-set block paste failed.'
    $copies = @($flow | Select-Object -Last 2); $copyCircle = $copies | Where-Object { $_.Kind.ToString() -eq 'CircularCaliperCircleFinder' } | Select-Object -First 1; $copyStats = $copies | Where-Object { $_.Kind.ToString() -eq 'PointSetGeometryStatistics' } | Select-Object -First 1
    Assert ($copyStats.GetInputBinding('Points').SourceToolId -eq $copyCircle.ToolId) 'Point-set block copy did not remap ToolId.'
    $stats.SetOutputValue('MaxSpan', [double]180); $stats.ResultCode = 'OK'; Method 'InvalidateDownstreamResults' ([object[]]@($circle, 'S65 rerun')) | Out-Null
    Assert ($stats.ResultCode -eq '--') 'Upstream invalidation retained point-set results.'
    Write-Output 'S65 on-demand capability preview + 60-tool baseline + Point2D/Point2D[] + real circular EdgePoints + HALCON point geometry + connection/monitor/recipe/copy/failures passed'
}
finally {
    $main.GetField('recipeCloseConfirmed', $private).SetValue($window, $true)
    $window.Close()
    if ($ownsApp) { $app.Shutdown() }
    if (Test-Path $temp) { Remove-Item -LiteralPath $temp -Recurse -Force }
}
