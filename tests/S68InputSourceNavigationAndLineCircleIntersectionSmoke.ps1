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
$lineType = $assembly.GetType('HalconWinFormsDemo.Models.VmLine2D', $true)
$circleType = $assembly.GetType('HalconWinFormsDemo.Models.VmCircle2D', $true)
$pointArrayType = $assembly.GetType('HalconWinFormsDemo.Models.VmPoint2DArray', $true)
$xldSnapshotType = $assembly.GetType('HalconWinFormsDemo.Models.VmXldSnapshot', $true)
$recipeType = $assembly.GetType('HalconWinFormsDemo.Models.VisionRecipe', $true)
$private = [Reflection.BindingFlags]'Instance,NonPublic'
$all = [Reflection.BindingFlags]'Instance,NonPublic,Public'
$static = [Reflection.BindingFlags]'Static,Public'
$temp = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s68-' + [Guid]::NewGuid().ToString('N'))
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
function Run($Tool) { Method 'ExecuteFlowTool' ([object[]]@($Tool, 'S68 smoke')) }
function Output($Tool, [string]$Port) { $arguments = [object[]]@($Port, $null); if (-not $Tool.GetType().GetMethod('TryGetOutputValue').Invoke($Tool, $arguments)) { throw "Missing output $Port" }; $arguments[1] }
function HasOutput($Tool, [string]$Port) { $arguments = [object[]]@($Port, $null); [bool]$Tool.GetType().GetMethod('TryGetOutputValue').Invoke($Tool, $arguments) }
function Catalog([string]$KindName) { foreach ($entry in (Field 'toolCatalog')) { if ($entry.Kind.ToString() -eq $KindName) { return $entry } }; return $null }
function SetXld($Tool, [HalconDotNet.HObject]$Object) { $snapshot = $xldSnapshotType.GetMethod('Create', $static).Invoke($null, [object[]]@($Object, $Tool.ToolId, $Tool.InstanceName, 'XLD')); $Tool.SetOutputValue('XLD', $snapshot); $Tool.ResultCode = 'OK'; $Tool.RunStatus = 'Completed' }

try {
    $catalogBaseline = [Enum]::GetValues($kindType).Count
    Assert ((Field 'toolCatalog').Count -eq $catalogBaseline -and $catalogBaseline -ge 63) 'Full catalog no longer matches the real tool enum.'
    Assert ((Field 'DeveloperToolbarPanel').Children.Count -eq 7) 'S68 added a permanent top toolbar command.'
    Assert ($null -ne (Catalog 'LineCircleIntersectionMeasurement')) 'Line-circle intersection tool is missing from the toolbox.'

    # B: real HALCON XLD fits publish Line2D/Circle2D, then HALCON intersection_line_circle returns 2/1/0 typed points.
    $flow = Field 'flowTools'; Method 'DisposeFlowTools' | Out-Null; $flow.Clear()
    $source = Tool 'ImageSource' 'Shared Image'
    $lineCarrier = Tool 'SubpixelEdgeExtraction' 'Line XLD Carrier'
    $circleCarrier = Tool 'SubpixelEdgeExtraction' 'Circle XLD Carrier'
    $lineFit = Tool 'XldLineFitting' 'Typed Main Line'
    $circleFit = Tool 'XldCircleFitting' 'Typed Main Circle'
    $intersection = Tool 'LineCircleIntersectionMeasurement' 'Line Circle Intersection'
    $judge = Tool 'NumericJudge' 'Chord Judge'
    foreach ($tool in @($source, $lineCarrier, $circleCarrier, $lineFit, $circleFit, $intersection, $judge)) { $flow.Add($tool) }
    $lineCarrier.SetInputBinding('Image', $source.ToolId, 'Image'); $circleCarrier.SetInputBinding('Image', $source.ToolId, 'Image')
    $lineFit.SetInputBinding('XLD', $lineCarrier.ToolId, 'XLD'); $circleFit.SetInputBinding('XLD', $circleCarrier.ToolId, 'XLD')
    $intersection.SetInputBinding('Line', $lineFit.ToolId, 'MainLine'); $intersection.SetInputBinding('Circle', $circleFit.ToolId, 'MainCircle')
    $judge.SetInputBinding('Value', $intersection.ToolId, 'ChordLength'); $judge.InputToolId = $intersection.ToolId; $judge.InputPortName = 'ChordLength'; $judge.NumericOperator = 'BetweenInclusive'; $judge.NumericLowerLimit = 99.8; $judge.NumericUpperLimit = 100.2
    [HalconDotNet.HObject]$lineXld = $null; [HalconDotNet.HObject]$circleXld = $null
    try {
        [HalconDotNet.HOperatorSet]::GenContourPolygonXld([ref]$lineXld, [HalconDotNet.HTuple]::new([double[]]@(100,100)), [HalconDotNet.HTuple]::new([double[]]@(100,300)))
        [HalconDotNet.HOperatorSet]::GenCircleContourXld([ref]$circleXld, 100.0, 200.0, 50.0, 0.0, [Math]::PI * 2.0, 'positive', 1.0)
        SetXld $lineCarrier $lineXld; SetXld $circleCarrier $circleXld
    } finally { if ($lineXld) { $lineXld.Dispose() }; if ($circleXld) { $circleXld.Dispose() } }
    Assert ((Run $lineFit).ResultCode -eq 'OK' -and (Run $circleFit).ResultCode -eq 'OK') 'HALCON typed line/circle fitting chain failed.'
    $mainLine = Output $lineFit 'MainLine'; $mainCircle = Output $circleFit 'MainCircle'; $context = $mainLine.CoordinateContextId
    Assert ($mainLine.GetType() -eq $lineType -and $mainCircle.GetType() -eq $circleType -and $mainCircle.CoordinateContextId -eq $context) 'Typed Line2D/Circle2D context chain is inconsistent.'
    $result = Run $intersection
    Assert ($result.ResultCode -eq 'OK' -and [int](Output $intersection 'IntersectionCount') -eq 2 -and (Output $intersection 'Relation') -eq 'Secant') 'HALCON secant did not return two intersections.'
    $points = Output $intersection 'IntersectionPoints'
    Assert ($points.GetType() -eq $pointArrayType -and $points.Count -eq 2 -and [Math]::Abs([double](Output $intersection 'ChordLength') - 100) -lt 0.2) 'Typed intersection points or chord length is wrong.'
    Assert ((Output $intersection 'XLD').ObjectCount -eq 4) 'Secant managed XLD must contain line, circle and two crosses.'
    Assert ((Run $judge).ResultCode -eq 'OK') 'ChordLength did not drive NumericJudge.'

    $firstA = (Output $intersection 'PointA').Column
    $oldSnapshot = Output $intersection 'XLD'
    $intersection.Parameters.LineCircleIntersectionOrder = 'ReverseAlongLine'; $intersection.Parameters.LineCircleOverlayCrossSize = 14
    Run $intersection | Out-Null
    Assert ([Math]::Abs((Output $intersection 'PointA').Column - $firstA) -gt 90) 'Reverse line order did not swap the primary intersections.'
    Assert ($oldSnapshot.IsDisposed) 'Rerun did not dispose the old managed XLD snapshot.'

    $exactCircle = $circleType.GetMethod('CreateImagePixel', $static).Invoke($null, [object[]]@(100.0, 200.0, 50.0, $context))
    $circleFit.SetOutputValue('MainCircle', $exactCircle); $circleFit.ResultCode = 'OK'
    $tangentRow = 150.0
    $lineFit.SetOutputValue('MainLine', $lineType.GetMethod('CreateImagePixel', $static).Invoke($null, [object[]]@($tangentRow, 100.0, $tangentRow, 300.0, $context))); $lineFit.ResultCode = 'OK'
    $result = Run $intersection
    Assert ($result.ResultCode -eq 'OK' -and [int](Output $intersection 'IntersectionCount') -eq 1 -and (Output $intersection 'Relation') -eq 'Tangent' -and [Math]::Abs([double](Output $intersection 'ChordLength')) -lt 0.001) 'HALCON tangent semantics are wrong.'
    $lineFit.SetOutputValue('MainLine', $lineType.GetMethod('CreateImagePixel', $static).Invoke($null, [object[]]@(160.0, 100.0, 160.0, 300.0, $context))); $lineFit.ResultCode = 'OK'
    $result = Run $intersection
    Assert ($result.ResultCode -eq 'NG' -and [int](Output $intersection 'IntersectionCount') -eq 0 -and (Output $intersection 'Relation') -eq 'Separated') 'HALCON separated semantics must return zero points and NG.'
    Assert ((Output $intersection 'IntersectionPoints').Count -eq 0 -and -not (HasOutput $intersection 'PointA') -and (Output $intersection 'XLD').ObjectCount -eq 2) 'Separated rerun retained stale points or wrong XLD.'

    $intersection.Parameters.LineCircleIntersectionOrder = 'AlongLine'; $intersection.Parameters.LineCircleOverlayCrossSize = 13
    $recipe = Method 'CaptureRecipe'; $serializer = [Runtime.Serialization.Json.DataContractJsonSerializer]::new($recipeType); $stream = New-Object IO.MemoryStream
    try { $serializer.WriteObject($stream, $recipe); $stream.Position = 0; $saved = $serializer.ReadObject($stream) } finally { $stream.Dispose() }
    $savedTool = $saved.ToolFlow | Where-Object ToolId -eq $intersection.ToolId | Select-Object -First 1
    Assert ($savedTool.Parameters.LineCircleIntersectionOrder -eq 'AlongLine' -and [Math]::Abs($savedTool.Parameters.LineCircleOverlayCrossSize - 13) -lt 0.001) 'Recipe lost line-circle parameters.'
    (Field 'FlowToolList').SelectedItems.Clear(); (Field 'FlowToolList').SelectedItems.Add($lineFit); (Field 'FlowToolList').SelectedItems.Add($circleFit); (Field 'FlowToolList').SelectedItems.Add($intersection)
    Assert ([int](Method 'CopySelectedFlowToolsToClipboard') -eq 3 -and [int](Method 'PasteFlowToolClipboard' ([object[]]@($flow.Count))) -eq 3) 'Line-circle block copy/paste failed.'
    $copies = @($flow | Select-Object -Last 3); $copyLine = $copies | Where-Object { $_.Kind.ToString() -eq 'XldLineFitting' } | Select-Object -First 1; $copyCircle = $copies | Where-Object { $_.Kind.ToString() -eq 'XldCircleFitting' } | Select-Object -First 1; $copyIntersection = $copies | Where-Object { $_.Kind.ToString() -eq 'LineCircleIntersectionMeasurement' } | Select-Object -First 1
    Assert ($copyIntersection.GetInputBinding('Line').SourceToolId -eq $copyLine.ToolId -and $copyIntersection.GetInputBinding('Circle').SourceToolId -eq $copyCircle.ToolId) 'Line-circle copy did not remap both typed sources.'

    $lineFit.SetOutputValue('MainLine', $lineType.GetMethod('CreateImagePixel', $static).Invoke($null, [object[]]@(0.0,0.0,0.0,0.0,$context))); $lineFit.ResultCode = 'OK'
    $failed = $false; try { Run $intersection | Out-Null } catch { $failed = $true }; Assert $failed 'Degenerate Line2D was accepted.'
    $lineFit.SetOutputValue('MainLine', $lineType.GetMethod('CreateImagePixel', $static).Invoke($null, [object[]]@(0.0,0.0,1.0,1.0,'Other.Image'))); $lineFit.ResultCode = 'OK'
    $failed = $false; try { Run $intersection | Out-Null } catch { $failed = $true }; Assert $failed 'Mixed coordinate contexts were accepted.'
    $lineFit.ResultCode = '--'; $failed = $false; try { Run $intersection | Out-Null } catch { $failed = $true }; Assert $failed 'Stale Line2D input was accepted.'

    # A1: selecting a missing IN shows valid and rejected existing sources, then writes a real binding with undo/redo.
    Method 'DisposeFlowTools' | Out-Null; $flow.Clear()
    $lineFit = Tool 'XldLineFitting' 'Existing Main Line'
    $circleFit = Tool 'XldCircleFitting' 'Existing Main Circle'
    $target = Tool 'LineCircleIntersectionMeasurement' 'Needs Line'
    $lateLine = Tool 'XldLineFitting' 'Late Main Line'
    foreach ($tool in @($lineFit,$circleFit,$target,$lateLine)) { $flow.Add($tool) }
    $target.SetInputBinding('Circle',$circleFit.ToolId,'MainCircle')
    Assert ([bool](Method 'EnterInputSourceContextS68' ([object[]]@($target.ToolId,'Line')))) 'Missing IN did not enter source-navigation context.'
    Assert ((Field 'ToolInputSourceContextBorder').Visibility.ToString() -eq 'Visible' -and (Field 'ToolCatalogModePanel').Visibility.ToString() -eq 'Collapsed') 'IN source context did not replace catalog modes on demand.'
    $suggestions = @(); foreach ($suggestion in (Field 'inputSourceSuggestionsS68')) { $suggestions += $suggestion }
    $valid = $suggestions | Where-Object { $_.IsValid } | Select-Object -First 1; $invalid = $suggestions | Where-Object { -not $_.IsValid } | Select-Object -First 1
    Assert ([string]$valid.SourceToolId -eq [string]$lineFit.ToolId) 'Nearest valid Line2D source was not prioritized.'
    Assert ([string]$invalid.SourceToolId -eq [string]$lateLine.ToolId) 'Later Line2D source was not retained for rejection diagnostics.'
    $rejectText = [Convert]::ToString($invalid.GetType().GetProperty('RejectionText').GetValue($invalid, $null))
    Assert (-not [string]::IsNullOrWhiteSpace($rejectText) -and $invalid.SourceIndex -gt $flow.IndexOf($target)) 'Later-source rejection reason is missing.'
    (Field 'ToolInputSourceSuggestionList').SelectedItem = $valid; Method 'BindSelectedInputSourceS68' | Out-Null
    Assert ($target.GetInputBinding('Line').SourceToolId -eq $lineFit.ToolId -and (Field 'selectedFlowConnectionKey') -match $target.ToolId) 'Existing source was not written/highlighted as a real binding.'
    Method 'UndoFlowEditButton_Click' ([object[]]@($null,[Windows.RoutedEventArgs]::new())) | Out-Null
    $target = $flow | Where-Object InstanceName -eq 'Needs Line' | Select-Object -First 1
    Assert ($null -eq $target.GetInputBinding('Line')) 'Undo did not restore the missing IN state.'
    Method 'RedoFlowEditButton_Click' ([object[]]@($null,[Windows.RoutedEventArgs]::new())) | Out-Null
    $target = $flow | Where-Object InstanceName -eq 'Needs Line' | Select-Object -First 1
    Assert ($target.GetInputBinding('Line').SourceToolId -eq ($flow | Where-Object InstanceName -eq 'Existing Main Line' | Select-Object -First 1).ToolId) 'Redo did not restore the source binding.'

    # A2: no existing Line2D source -> catalog shows only real producers, inserts before target, autobinds XLD, and connects the target.
    Method 'DisposeFlowTools' | Out-Null; $flow.Clear()
    $xldCarrier = Tool 'SubpixelEdgeExtraction' 'Available XLD'
    $circleFit = Tool 'XldCircleFitting' 'Available Circle'
    $target = Tool 'LineCircleIntersectionMeasurement' 'Needs Created Line'
    foreach ($tool in @($xldCarrier,$circleFit,$target)) { $flow.Add($tool) }
    $circleFit.SetInputBinding('XLD',$xldCarrier.ToolId,'XLD'); $target.SetInputBinding('Circle',$circleFit.ToolId,'MainCircle')
    Assert ([bool](Method 'EnterInputSourceContextS68' ([object[]]@($target.ToolId,'Line')))) 'Missing Line IN did not enter create-source context.'
    $availableSuggestions = @(); foreach ($suggestion in (Field 'inputSourceSuggestionsS68')) { if ($suggestion.IsValid) { $availableSuggestions += $suggestion } }
    Assert ($availableSuggestions.Count -eq 0) 'Create-source scenario unexpectedly found an existing Line2D source.'
    $visible = @(); foreach ($entry in (Field 'toolCatalogView')) { $visible += $entry }
    Assert ($visible.Count -ge 2 -and ($visible | Where-Object { $_.Kind.ToString() -eq 'XldLineFitting' }).Count -eq 1 -and ($visible | Where-Object { $_.Kind.ToString() -eq 'PointPairLineConstructionMeasurement' }).Count -eq 1) 'Create-source catalog did not expose every real Line2D producer.'
    (Field 'ToolCatalogList').SelectedItem = $visible | Where-Object { $_.Kind.ToString() -eq 'XldLineFitting' } | Select-Object -First 1; $beforeCount = $flow.Count; Method 'InsertSelectedInputSourceToolS68' | Out-Null
    Assert ($flow.Count -eq $beforeCount + 1) 'Create-source action did not add exactly one tool.'
    $target = $flow | Where-Object InstanceName -eq 'Needs Created Line' | Select-Object -First 1; $created = $flow[$flow.IndexOf($target)-1]
    $createdInput = $created.GetInputBinding('XLD'); $createdInputSource = $flow | Where-Object ToolId -eq $createdInput.SourceToolId | Select-Object -First 1
    Assert ($created.Kind.ToString() -eq 'XldLineFitting' -and $null -ne $createdInput -and $flow.IndexOf($createdInputSource) -lt $flow.IndexOf($created)) 'Created Line2D producer was not inserted before target or autobound to a real earlier XLD.'
    Assert ($target.GetInputBinding('Line').SourceToolId -eq $created.ToolId -and $target.GetInputBinding('Line').SourcePortName -eq 'MainLine') 'Created producer output was not bound to the target IN.'
    Assert ((Field 'ToolInputSourceContextBorder').Visibility.ToString() -eq 'Collapsed') 'Successful source creation did not return to the full catalog.'
    $restored = @(); foreach ($entry in (Field 'toolCatalogView')) { $restored += $entry }
    Assert ($restored.Count -eq $catalogBaseline) 'Return to full catalog did not restore every real tool.'

    Write-Output 'S68 IN-port existing-source reuse + rejection diagnostics + source-tool insertion/autobind + undo/redo/recipe + HALCON line-circle 2/1/0 intersections + typed Point2D[]/XLD/NumericJudge/failures passed; BSB not implemented.'
}
finally {
    $main.GetField('recipeCloseConfirmed', $private).SetValue($window, $true)
    $window.Close()
    if ($ownsApp) { $app.Shutdown() }
    if (Test-Path $temp) { Remove-Item -LiteralPath $temp -Recurse -Force }
}
