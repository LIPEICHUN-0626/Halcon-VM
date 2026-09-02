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
$circleType = $assembly.GetType('HalconWinFormsDemo.Models.VmCircle2D', $true)
$pointArrayType = $assembly.GetType('HalconWinFormsDemo.Models.VmPoint2DArray', $true)
$xldSnapshotType = $assembly.GetType('HalconWinFormsDemo.Models.VmXldSnapshot', $true)
$recipeType = $assembly.GetType('HalconWinFormsDemo.Models.VisionRecipe', $true)
$private = [Reflection.BindingFlags]'Instance,NonPublic'
$all = [Reflection.BindingFlags]'Instance,NonPublic,Public'
$static = [Reflection.BindingFlags]'Static,Public'
$temp = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s69-' + [Guid]::NewGuid().ToString('N'))
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
function Run($Tool) { Method 'ExecuteFlowTool' ([object[]]@($Tool, 'S69 smoke')) }
function Output($Tool, [string]$Port) { $arguments = [object[]]@($Port, $null); if (-not $Tool.GetType().GetMethod('TryGetOutputValue').Invoke($Tool, $arguments)) { throw "Missing output $Port" }; $arguments[1] }
function HasOutput($Tool, [string]$Port) { $arguments = [object[]]@($Port, $null); [bool]$Tool.GetType().GetMethod('TryGetOutputValue').Invoke($Tool, $arguments) }
function Catalog([string]$KindName) { foreach ($entry in (Field 'toolCatalog')) { if ($entry.Kind.ToString() -eq $KindName) { return $entry } }; return $null }
function Circle([double]$Row,[double]$Column,[double]$Radius,[string]$Context) { $circleType.GetMethod('CreateImagePixel', $static).Invoke($null, [object[]]@($Row,$Column,$Radius,$Context)) }
function SetXld($Tool, [HalconDotNet.HObject]$Object) { $snapshot = $xldSnapshotType.GetMethod('Create', $static).Invoke($null, [object[]]@($Object, $Tool.ToolId, $Tool.InstanceName, 'XLD')); $Tool.SetOutputValue('XLD', $snapshot); $Tool.ResultCode = 'OK'; $Tool.RunStatus = 'Completed' }

try {
    Assert ((Field 'toolCatalog').Count -eq [Enum]::GetValues($kindType).Count -and (Field 'toolCatalog').Count -ge 64) 'S69 catalog baseline dropped below its full 64-tool implementation.'
    Assert ((Field 'DeveloperToolbarPanel').Children.Count -eq 7) 'S69 added a permanent top toolbar command.'
    Assert ($null -ne (Catalog 'CircleCircleIntersectionMeasurement')) 'Circle-circle tool is missing from the catalog.'

    # A: scan multiple required-input failures without mutating the flow, navigate, locate, open I/O, and hand off to S68.
    $flow = Field 'flowTools'; Method 'DisposeFlowTools' | Out-Null; $flow.Clear()
    $carrierA = Tool 'SubpixelEdgeExtraction' 'Circle Carrier A'
    $fitA = Tool 'XldCircleFitting' 'Circle Source A'
    $carrierB = Tool 'SubpixelEdgeExtraction' 'Circle Carrier B'
    $fitB = Tool 'XldCircleFitting' 'Circle Source B'
    $missingCircle = Tool 'CircleCircleIntersectionMeasurement' 'Missing Circle B'
    $missingPoint = Tool 'PointToCircleRadialDeviationMeasurement' 'Missing Point'
    $lateBound = Tool 'CircleCircleIntersectionMeasurement' 'Late Bound Circle'
    $lateFit = Tool 'XldCircleFitting' 'Late Circle Source'
    $staleFit = Tool 'XldCircleFitting' 'Stale Circle Source'
    $staleTarget = Tool 'CircleCircleIntersectionMeasurement' 'Stale Circle Target'
    foreach ($tool in @($carrierA,$fitA,$carrierB,$fitB,$missingCircle,$missingPoint,$lateBound,$lateFit,$staleFit,$staleTarget)) { $flow.Add($tool) }
    $fitA.SetInputBinding('XLD',$carrierA.ToolId,'XLD'); $fitB.SetInputBinding('XLD',$carrierB.ToolId,'XLD'); $lateFit.SetInputBinding('XLD',$carrierA.ToolId,'XLD'); $staleFit.SetInputBinding('XLD',$carrierB.ToolId,'XLD')
    [HalconDotNet.HObject]$rawA=$null; [HalconDotNet.HObject]$rawB=$null
    try {
        [HalconDotNet.HOperatorSet]::GenCircleContourXld([ref]$rawA,0.0,0.0,5.0,0.0,[Math]::PI*2.0,'positive',1.0)
        [HalconDotNet.HOperatorSet]::GenCircleContourXld([ref]$rawB,0.0,8.0,5.0,0.0,[Math]::PI*2.0,'positive',1.0)
        SetXld $carrierA $rawA; SetXld $carrierB $rawB
    } finally { if($rawA){$rawA.Dispose()};if($rawB){$rawB.Dispose()} }
    $context='Audit.Image'
    $fitA.SetOutputValue('MainCircle',(Circle 0 0 5 $context));$fitA.ResultCode='OK';$fitA.RunStatus='Completed'
    $fitB.SetOutputValue('MainCircle',(Circle 0 8 5 $context));$fitB.ResultCode='OK';$fitB.RunStatus='Completed'
    $lateFit.SetOutputValue('MainCircle',(Circle 0 10 5 $context));$lateFit.ResultCode='OK';$lateFit.RunStatus='Completed'
    $staleFit.SetOutputValue('MainCircle',(Circle 0 3 2 $context));$staleFit.ResultCode='--';$staleFit.RunStatus='Waiting'
    $missingCircle.SetInputBinding('CircleA',$fitA.ToolId,'MainCircle')
    $missingPoint.SetInputBinding('Circle',$fitB.ToolId,'MainCircle')
    $lateBound.SetInputBinding('CircleA',$fitA.ToolId,'MainCircle');$lateBound.SetInputBinding('CircleB',$lateFit.ToolId,'MainCircle')
    $staleTarget.SetInputBinding('CircleA',$fitA.ToolId,'MainCircle');$staleTarget.SetInputBinding('CircleB',$staleFit.ToolId,'MainCircle')
    $bindingCountBefore = [int](($flow | ForEach-Object { $_.InputBindings.Count } | Measure-Object -Sum).Sum)
    Method 'OpenConnectionIssuesButton_Click' ([object[]]@($null,[Windows.RoutedEventArgs]::new())) | Out-Null
    Assert ((Field 'FlowConnectionIssuePanel').Visibility.ToString() -eq 'Visible') 'Connection issue task panel did not open from the existing flow context.'
    $issues=@();foreach($issue in (Field 'connectionIssuesS69')){$issues+=$issue}
    $issues | ForEach-Object { Write-Output ("S69_AUDIT=" + $_.SeverityText + "|" + $_.TargetText + "|" + $_.DataType + "|" + $_.ReasonText) }
    Assert ($issues.Count -eq 4 -and ($issues|Where-Object IsBlocking).Count -eq 3 -and ($issues|Where-Object {-not $_.IsBlocking}).Count -eq 1) 'Connection audit did not classify the four deterministic blocking/waiting tasks.'
    $missingCircleIssues = @($issues|Where-Object {($_.TargetToolId -eq $missingCircle.ToolId) -and ($_.TargetPortName -eq 'CircleB') -and $_.IsBlocking})
    $missingPointIssues = @($issues|Where-Object {($_.TargetToolId -eq $missingPoint.ToolId) -and ($_.TargetPortName -eq 'Point') -and ($_.DataType -eq 'Point2D')})
    $lateIssues = @($issues|Where-Object {($_.TargetToolId -eq $lateBound.ToolId) -and ($_.TargetPortName -eq 'CircleB') -and $_.IsBlocking})
    $staleIssues = @($issues|Where-Object {($_.TargetToolId -eq $staleTarget.ToolId) -and ($_.TargetPortName -eq 'CircleB') -and (-not $_.IsBlocking)})
    Assert ($missingCircleIssues.Count -eq 1) 'Missing required CircleB was not reported.'
    Assert ($missingPointIssues.Count -eq 1) 'Missing required Point2D was not reported.'
    Assert ($lateIssues.Count -eq 1) 'Later-source order violation was not reported.'
    Assert ($staleIssues.Count -eq 1) 'Stale connected source was not retained as a non-mutating waiting task.'
    $bindingCountAfter = [int](($flow | ForEach-Object { $_.InputBindings.Count } | Measure-Object -Sum).Sum)
    Assert ($bindingCountAfter -eq $bindingCountBefore) 'Opening/scanning the task list mutated InputBindings.'
    $first = (Field 'FlowConnectionIssueListBox').SelectedItem; Method 'NextConnectionIssueButton_Click' ([object[]]@($null,[Windows.RoutedEventArgs]::new())) | Out-Null
    Assert (-not [object]::ReferenceEquals($first,(Field 'FlowConnectionIssueListBox').SelectedItem) -and (Field 'FlowConnectionIssuePositionText').Text -match '2 / 4') 'Next-task navigation did not advance deterministically.'
    Method 'LocateConnectionIssueButton_Click' ([object[]]@($null,[Windows.RoutedEventArgs]::new())) | Out-Null
    Assert ((Field 'FlowToolList').SelectedItem.ToolId -eq (Field 'FlowConnectionIssueListBox').SelectedItem.TargetToolId) 'Task locate did not select the real target node.'
    Method 'OpenConnectionIssueIoButton_Click' ([object[]]@($null,[Windows.RoutedEventArgs]::new())) | Out-Null
    Assert ([object]::ReferenceEquals((Field 'RightTabs').SelectedItem,(Field 'IoTab'))) 'Task did not open the target real I/O page.'
    (Field 'FlowConnectionIssueListBox').SelectedItem = $missingCircleIssues[0]
    Method 'OpenConnectionIssueSourceButton_Click' ([object[]]@($null,[Windows.RoutedEventArgs]::new())) | Out-Null
    Assert ((Field 'FlowConnectionIssuePanel').Visibility.ToString() -eq 'Collapsed' -and (Field 'ToolInputSourceContextBorder').Visibility.ToString() -eq 'Visible') 'Blocking task did not hand off to S68 source navigation.'
    Assert ((Field 'inputSourceTargetToolIdS68') -eq $missingCircle.ToolId -and (Field 'inputSourceTargetPortNameS68') -eq 'CircleB') 'S68 handoff targeted the wrong real IN port.'
    Assert ([int](($flow | ForEach-Object { $_.InputBindings.Count } | Measure-Object -Sum).Sum) -eq $bindingCountBefore) 'Opening S68 from the task list mutated the flow before user selection.'
    Method 'ExitInputSourceContextS68' ([object[]]@($false)) | Out-Null

    # B: real HALCON intersection_circles and explicit relation semantics.
    Method 'DisposeFlowTools' | Out-Null; $flow.Clear()
    $sourceA = Tool 'XldCircleFitting' 'Typed Circle A'
    $sourceB = Tool 'XldCircleFitting' 'Typed Circle B'
    $intersection = Tool 'CircleCircleIntersectionMeasurement' 'Circle Circle Relation'
    $judge = Tool 'NumericJudge' 'Center Distance Judge'
    foreach($tool in @($sourceA,$sourceB,$intersection,$judge)){$flow.Add($tool)}
    $intersection.SetInputBinding('CircleA',$sourceA.ToolId,'MainCircle');$intersection.SetInputBinding('CircleB',$sourceB.ToolId,'MainCircle')
    $judge.SetInputBinding('Value',$intersection.ToolId,'CenterDistance');$judge.InputToolId=$intersection.ToolId;$judge.InputPortName='CenterDistance';$judge.NumericOperator='BetweenInclusive';$judge.NumericLowerLimit=7.9;$judge.NumericUpperLimit=8.1
    $sourceA.SetOutputValue('MainCircle',(Circle 0 0 5 $context));$sourceA.ResultCode='OK'
    $sourceB.SetOutputValue('MainCircle',(Circle 0 8 5 $context));$sourceB.ResultCode='OK'
    $result=Run $intersection
    Assert ($result.ResultCode -eq 'OK' -and (Output $intersection 'Relation') -eq 'Secant' -and [int](Output $intersection 'IntersectionCount') -eq 2) 'HALCON secant relation/count failed.'
    $points=Output $intersection 'IntersectionPoints'
    Assert ($points.GetType() -eq $pointArrayType -and $points.Count -eq 2 -and [Math]::Abs([double](Output $intersection 'CenterDistance')-8)-lt 0.001 -and [Math]::Abs([double](Output $intersection 'CommonChordLength')-6)-lt 0.001) 'Typed secant points, center distance, or chord length is wrong.'
    Assert ((Output $intersection 'XLD').ObjectCount -eq 5 -and (Run $judge).ResultCode -eq 'OK') 'Managed secant XLD or NumericJudge downstream failed.'
    $firstRow=(Output $intersection 'PointA').Row;$oldSnapshot=Output $intersection 'XLD'
    $intersection.Parameters.CircleCircleIntersectionOrder='RowColumnDescending';$intersection.Parameters.CircleCircleOverlayCrossSize=14;Run $intersection|Out-Null
    Assert ([Math]::Abs((Output $intersection 'PointA').Row-$firstRow)-gt 5.9 -and $oldSnapshot.IsDisposed) 'Reverse point order or old XLD disposal failed.'

    $sourceB.SetOutputValue('MainCircle',(Circle 0 10 5 $context));$sourceB.ResultCode='OK';$result=Run $intersection
    Assert ($result.ResultCode -eq 'OK' -and (Output $intersection 'Relation') -eq 'ExternalTangent' -and [int](Output $intersection 'IntersectionCount') -eq 1 -and (Output $intersection 'XLD').ObjectCount -eq 4) 'External tangent semantics failed.'
    $sourceB.SetOutputValue('MainCircle',(Circle 0 3 2 $context));$sourceB.ResultCode='OK';$result=Run $intersection
    Assert ($result.ResultCode -eq 'OK' -and (Output $intersection 'Relation') -eq 'InternalTangent' -and [int](Output $intersection 'IntersectionCount') -eq 1) 'Internal tangent semantics failed.'
    $sourceB.SetOutputValue('MainCircle',(Circle 0 12 5 $context));$sourceB.ResultCode='OK';$result=Run $intersection
    Assert ($result.ResultCode -eq 'NG' -and (Output $intersection 'Relation') -eq 'Separated' -and [int](Output $intersection 'IntersectionCount') -eq 0 -and -not (HasOutput $intersection 'PointA') -and (Output $intersection 'XLD').ObjectCount -eq 3) 'Separated circles retained stale points or wrong relation/XLD.'
    $sourceB.SetOutputValue('MainCircle',(Circle 0 1 2 $context));$sourceB.ResultCode='OK';$result=Run $intersection
    Assert ($result.ResultCode -eq 'NG' -and (Output $intersection 'Relation') -eq 'Contained') 'Contained relation failed.'
    $sourceB.SetOutputValue('MainCircle',(Circle 0 0 2 $context));$sourceB.ResultCode='OK';$result=Run $intersection
    Assert ($result.ResultCode -eq 'NG' -and (Output $intersection 'Relation') -eq 'ConcentricContained') 'Concentric-contained relation failed.'
    $sourceB.SetOutputValue('MainCircle',(Circle 0 0 5 $context));$sourceB.ResultCode='OK';$result=Run $intersection
    Assert ($result.ResultCode -eq 'NG' -and (Output $intersection 'Relation') -eq 'Overlapping' -and [bool](Output $intersection 'IsOverlapping') -and [int](Output $intersection 'IntersectionCount') -eq 0 -and -not (HasOutput $intersection 'PointA')) 'Overlapping circles were misrepresented as two finite points.'

    Method 'RefreshPortDataMonitorS64' ([object[]]@($intersection)) | Out-Null
    Assert (@((Field 'portDataMonitorRows')|Where-Object DataType -eq 'Circle2D').Count -ge 2) 'Port monitor did not expose both Circle2D inputs.'
    $intersection.Parameters.CircleCircleIntersectionOrder='RowColumnAscending';$intersection.Parameters.CircleCircleOverlayCrossSize=13
    $recipe=Method 'CaptureRecipe';$serializer=[Runtime.Serialization.Json.DataContractJsonSerializer]::new($recipeType);$stream=New-Object IO.MemoryStream
    try{$serializer.WriteObject($stream,$recipe);$stream.Position=0;$saved=$serializer.ReadObject($stream)}finally{$stream.Dispose()}
    $savedTool=$saved.ToolFlow|Where-Object ToolId -eq $intersection.ToolId|Select-Object -First 1
    Assert ($savedTool.Parameters.CircleCircleIntersectionOrder -eq 'RowColumnAscending' -and [Math]::Abs($savedTool.Parameters.CircleCircleOverlayCrossSize-13)-lt 0.001) 'Recipe lost circle-circle parameters.'
    (Field 'FlowToolList').SelectedItems.Clear();(Field 'FlowToolList').SelectedItems.Add($sourceA);(Field 'FlowToolList').SelectedItems.Add($sourceB);(Field 'FlowToolList').SelectedItems.Add($intersection)
    Assert ([int](Method 'CopySelectedFlowToolsToClipboard') -eq 3 -and [int](Method 'PasteFlowToolClipboard' ([object[]]@($flow.Count))) -eq 3) 'Circle-circle block copy/paste failed.'
    $copies=@($flow|Select-Object -Last 3);$copyA=$copies|Where-Object InstanceName -match 'Typed Circle A'|Select-Object -First 1;$copyB=$copies|Where-Object InstanceName -match 'Typed Circle B'|Select-Object -First 1;$copyIntersection=$copies|Where-Object {$_.Kind.ToString()-eq'CircleCircleIntersectionMeasurement'}|Select-Object -First 1
    Assert ($copyIntersection.GetInputBinding('CircleA').SourceToolId -eq $copyA.ToolId -and $copyIntersection.GetInputBinding('CircleB').SourceToolId -eq $copyB.ToolId) 'Circle-circle block copy did not remap both typed sources.'

    $intersection.SetInputBinding('CircleB',$sourceA.ToolId,'MainCircle');$failed=$false;try{Run $intersection|Out-Null}catch{$failed=$true};Assert $failed 'Duplicate circle endpoint was accepted.'
    $intersection.SetInputBinding('CircleB',$sourceB.ToolId,'MainCircle');$sourceB.SetOutputValue('MainCircle',(Circle 0 8 5 'Other.Image'));$sourceB.ResultCode='OK';$failed=$false;try{Run $intersection|Out-Null}catch{$failed=$true};Assert $failed 'Mixed circle coordinate contexts were accepted.'
    $sourceB.SetOutputValue('MainCircle',(Circle 0 8 0 $context));$sourceB.ResultCode='OK';$failed=$false;try{Run $intersection|Out-Null}catch{$failed=$true};Assert $failed 'Degenerate circle was accepted.'
    $sourceB.SetOutputValue('MainCircle',(Circle ([double]::NaN) 8 5 $context));$sourceB.ResultCode='OK';$failed=$false;try{Run $intersection|Out-Null}catch{$failed=$true};Assert $failed 'Nonfinite circle was accepted.'
    $sourceB.SetOutputValue('MainCircle',(Circle 0 8 5 $context));$sourceB.ResultCode='--';$failed=$false;try{Run $intersection|Out-Null}catch{$failed=$true};Assert $failed 'Stale Circle2D source was accepted.'

    Write-Output 'S69 connection-issue scan/navigation/read-only/S68 handoff + HALCON circle-circle secant/tangents/separated/contained/concentric/overlap + typed Point2D[]/XLD/NumericJudge/recipe/copy/failures passed'
}
finally {
    $main.GetField('recipeCloseConfirmed',$private).SetValue($window,$true)
    $window.Close()
    if($ownsApp){$app.Shutdown()}
    if(Test-Path $temp){Remove-Item -LiteralPath $temp -Recurse -Force}
}
