param([string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot))

$ErrorActionPreference = 'Stop'
if ([Threading.Thread]::CurrentThread.ApartmentState -ne [Threading.ApartmentState]::STA) { throw 'Run with powershell.exe -STA.' }
$halconRoot = 'C:\Program Files\MVTec\HALCON-20.11-Progress'
$env:PATH = (Join-Path $halconRoot 'bin\x64-win64') + ';' + $env:PATH
Add-Type -AssemblyName PresentationFramework
Add-Type -AssemblyName System.Drawing
[Reflection.Assembly]::LoadFrom((Join-Path $halconRoot 'bin\dotnet35\halcondotnet.dll')) | Out-Null
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path $ProjectRoot 'bin\x64\Debug\HalconWinFormsDemo.exe'))

function Assert-True([bool]$Condition, [string]$Message) { if (-not $Condition) { throw "Assertion failed: $Message" } }
function Pump-Ui([int]$Milliseconds) {
    $frame = New-Object Windows.Threading.DispatcherFrame
    $timer = New-Object Windows.Threading.DispatcherTimer
    $timer.Interval = [TimeSpan]::FromMilliseconds($Milliseconds)
    $timer.Add_Tick({ $timer.Stop(); $frame.Continue = $false })
    $timer.Start(); [Windows.Threading.Dispatcher]::PushFrame($frame)
}
function Write-PatternPgm([string]$Path, [int]$Width, [int]$Height) {
    $header = [Text.Encoding]::ASCII.GetBytes("P5`n$Width $Height`n255`n")
    $pixels = New-Object byte[] ($Width * $Height)
    for ($row = 0; $row -lt $Height; $row++) {
        for ($column = 0; $column -lt $Width; $column++) {
            $value = if ($row -ge 14 -and $row -le 76 -and $column -ge 18 -and $column -le 108) { 220 } else { 25 }
            $pixels[$row * $Width + $column] = [byte]$value
        }
    }
    $bytes = New-Object byte[] ($header.Length + $pixels.Length)
    [Array]::Copy($header, 0, $bytes, 0, $header.Length); [Array]::Copy($pixels, 0, $bytes, $header.Length, $pixels.Length)
    [IO.File]::WriteAllBytes($Path, $bytes)
}
function New-SegmentSet([double]$Gap, [bool]$ReverseSecond) {
    [HalconDotNet.HObject]$first = $null; [HalconDotNet.HObject]$second = $null; [HalconDotNet.HObject]$result = $null
    try {
        [HalconDotNet.HOperatorSet]::GenContourPolygonXld([ref]$first, [HalconDotNet.HTuple]@([double]20,[double]20,[double]20), [HalconDotNet.HTuple]@([double]10,[double]20,[double]30))
        $start = 30.0 + $Gap; $end = 60.0
        $columns = if ($ReverseSecond) { [HalconDotNet.HTuple]@($end,$start) } else { [HalconDotNet.HTuple]@($start,$end) }
        [HalconDotNet.HOperatorSet]::GenContourPolygonXld([ref]$second, [HalconDotNet.HTuple]@([double]20,[double]20), $columns)
        [HalconDotNet.HOperatorSet]::ConcatObj($first, $second, [ref]$result)
        return $result
    } finally {
        if ($null -ne $first) { $first.Dispose() }; if ($null -ne $second) { $second.Dispose() }
    }
}

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s39-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($tempRoot) | Out-Null
$imagePath = Join-Path $tempRoot 'pattern.pgm'; Write-PatternPgm $imagePath 128 96

$mainType = $assembly.GetType('HalconWinFormsDemo.MainWindow', $true)
$recipeServiceType = $assembly.GetType('HalconWinFormsDemo.Services.RecipeService', $true)
$loggerType = $assembly.GetType('HalconWinFormsDemo.Services.AppLogger', $true)
$kindType = $assembly.GetType('HalconWinFormsDemo.Models.VmToolKind', $true)
$metadataType = $assembly.GetType('HalconWinFormsDemo.Models.ToolMetadata', $true)
$snapshotType = $assembly.GetType('HalconWinFormsDemo.Models.VmXldSnapshot', $true)
$private = [Reflection.BindingFlags]'Instance,NonPublic'; $all = [Reflection.BindingFlags]'Instance,NonPublic,Public'; $staticPublic = [Reflection.BindingFlags]'Static,Public'
$constructorArgs = New-Object 'object[]' 1; $constructorArgs[0] = $tempRoot.ToString()
$service = $recipeServiceType.GetConstructor([Type[]]@([string])).Invoke($constructorArgs)
$logger = $loggerType.GetConstructor([Type[]]@([string])).Invoke($constructorArgs)
$window = $mainType.GetConstructor($private, $null, [Type[]]@($recipeServiceType, $loggerType), $null).Invoke([object[]]@($service, $logger))
$flow = $mainType.GetField('flowTools', $private).GetValue($window); $catalog = $mainType.GetField('toolCatalog', $private).GetValue($window)
$create = $mainType.GetMethod('CreateFlowTool', $private); $execute = $mainType.GetMethod('ExecuteFlowTool', $private); $disposeFlow = $mainType.GetMethod('DisposeFlowTools', $private)
$runUnion = $mainType.GetMethod('RunXldAdjacentContourUnionTool', $private); $refreshLayers = $mainType.GetMethod('RefreshResultOverlayLayers', $private)
$setSelectionMode = $mainType.GetMethod('SetXldContourSelectionMode', $private); $tryPick = $mainType.GetMethod('TryPickXldContour', $private); $navigate = $mainType.GetMethod('NavigateXldContour', $private)
$ensureSelection = $mainType.GetMethod('EnsureXldContourSelectionValid', $private); $setDual = $mainType.GetMethod('SetDualImageMode', $private)
$setContext = $mainType.GetMethod('SetImageContextSelection', $private); $loadImage = $mainType.GetMethod('LoadImage', $private); $captureRecipe = $mainType.GetMethod('CaptureRecipe', $private)
function Field([string]$Name) { $mainType.GetField($Name, $all).GetValue($window) }
function New-Tool([string]$Kind, [string]$Name) { $create.Invoke($window, [object[]]@([Enum]::Parse($kindType, $Kind), $Name, $true, $null)) }
function Invoke-Execute($Tool, [string]$Source) { try { $execute.Invoke($window, [object[]]@($Tool, $Source)) } catch [Reflection.TargetInvocationException] { throw $_.Exception.InnerException } }
function Invoke-Union($Tool, $Xld) { try { $runUnion.Invoke($window, [object[]]@($Tool, $Xld)) } catch [Reflection.TargetInvocationException] { throw $_.Exception.InnerException } }
function Get-Out($Tool, [string]$Port) { $value = $null; if (-not $Tool.TryGetOutputValue($Port, [ref]$value)) { throw "$($Tool.InstanceName).$Port missing" }; $value }
function New-Snapshot($Xld, $Tool) { $snapshotType.GetMethod('Create', $staticPublic).Invoke($null, [object[]]@($Xld,$Tool.ToolId,$Tool.InstanceName,'XLD')) }
function Selection-Value([string]$Name) { $selection = $mainType.GetField('xldContourSelection', $private).GetValue($window); if ($null -eq $selection) { return $null }; $selection.GetType().GetProperty($Name).GetValue($selection, $null) }

foreach ($name in @('XldContourSelectionToggleButton','XldContourInspectorPanel','XldContourInspectorSourceText','XldContourInspectorContextText','XldContourInspectorIndexText','XldContourInspectorGeometryText','XldContourInspectorNearestText','XldContourPreviousButton','XldContourNextButton','XldContourIsolateToggleButton','XldContourLocateButton','XldContourOpenConfigurationButton','DockXldAdjacentContourUnionPanel','DockXldAdjacentMaxDistAbsTextBox','DockXldAdjacentMaxDistRelTextBox','DockXldAdjacentAttributeModeComboBox')) { Assert-True($null -ne (Field $name)) "$name is missing." }
Assert-True($null -ne ($catalog | Where-Object { $_.Kind.ToString() -eq 'XldAdjacentContourUnion' } | Select-Object -First 1)) 'Adjacent XLD union is missing from toolbox.'
$unionKind = [Enum]::Parse($kindType, 'XldAdjacentContourUnion'); $inputs = $metadataType.GetMethod('GetInputPorts', $staticPublic).Invoke($null, [object[]]@($unionKind)); $outputs = $metadataType.GetMethod('GetOutputPorts', $staticPublic).Invoke($null, [object[]]@($unionKind))
Assert-True(($inputs | Where-Object { $_.PortName -eq 'XLD' -and $_.DataType -eq 'XLD' -and -not $_.IsOptional }).Count -eq 1) 'Adjacent union required XLD input is missing.'
foreach ($port in @('XLD','InputContourCount','ContourCount','JoinedCount','TotalLength','Mode','Parameters','ResultCode')) { Assert-True(($outputs | Where-Object PortName -eq $port).Count -eq 1) "Adjacent union output $port is missing." }
foreach ($port in @('InputContourCount','ContourCount','JoinedCount','TotalLength')) { Assert-True(($outputs | Where-Object { $_.PortName -eq $port -and $_.DataType -eq 'Number' }).Count -eq 1) "$port is not a typed Number output." }
foreach ($kindName in @('XldContourFilter','XldContourSegmentation','XldLineFitting','XldCircleFitting','XldEllipseFitting','XldRectangleFitting')) { $ports = $metadataType.GetMethod('GetInputPorts', $staticPublic).Invoke($null, [object[]]@([Enum]::Parse($kindType, $kindName))); Assert-True(($ports | Where-Object { $_.PortName -eq 'XLD' -and $_.DataType -eq 'XLD' }).Count -eq 1) "$kindName cannot consume the union XLD." }

$app = [Windows.Application]::Current; if ($null -eq $app) { $app = New-Object Windows.Application; $app.ShutdownMode = [Windows.ShutdownMode]::OnExplicitShutdown }
$window.WindowStartupLocation = [Windows.WindowStartupLocation]::Manual; $window.Left = 2; $window.Top = 2; $window.Width = 1760; $window.Height = 980; $window.Topmost = $true; $window.Show(); $window.Activate() | Out-Null; Pump-Ui 400

# B line: exact HALCON endpoint union, thresholds, reversed direction, mode, empty/invalid paths, lifecycle, downstream, and recipe.
$direct = New-Tool XldAdjacentContourUnion 'S39 Direct Union'; $segments = New-SegmentSet 4 $false
try {
    $direct.Parameters.XldAdjacentMaxDistAbs = 10; $direct.Parameters.XldAdjacentMaxDistRel = 1; $direct.Parameters.XldAdjacentAttributeMode = 'AttrKeep'
    $record = Invoke-Union $direct $segments
    Assert-True($record.ResultCode -eq 'OK' -and [int](Get-Out $direct InputContourCount) -eq 2 -and [int](Get-Out $direct ContourCount) -eq 1 -and [int](Get-Out $direct JoinedCount) -eq 1) 'Nearby contours were not joined exactly once.'
    Assert-True((Get-Out $direct Mode) -eq 'attr_keep' -and (Get-Out $direct Parameters).Contains('MaxDistAbs=10')) 'attr_keep or parameter reporting is wrong.'
    $old = Get-Out $direct XLD; $direct.Parameters.XldAdjacentMaxDistAbs = 10; $direct.Parameters.XldAdjacentMaxDistRel = 0.01; Invoke-Union $direct $segments | Out-Null
    Assert-True($old.IsDisposed) 'Rerun did not release the previous managed XLD snapshot.'
    Assert-True([int](Get-Out $direct ContourCount) -eq 2 -and [int](Get-Out $direct JoinedCount) -eq 0) 'Relative-distance threshold did not independently reject the gap.'
    $relativeSnapshot = Get-Out $direct XLD; $direct.Parameters.XldAdjacentMaxDistAbs = 2; $direct.Parameters.XldAdjacentMaxDistRel = 1; Invoke-Union $direct $segments | Out-Null
    Assert-True($relativeSnapshot.IsDisposed -and [int](Get-Out $direct ContourCount) -eq 2 -and [int](Get-Out $direct JoinedCount) -eq 0) 'Absolute-distance threshold did not independently reject the gap.'
    $direct.Parameters.XldAdjacentAttributeMode = 'AttrForget'; Invoke-Union $direct $segments | Out-Null; Assert-True((Get-Out $direct Mode) -eq 'attr_forget') 'attr_forget did not reach HALCON/reporting.'
    [HalconDotNet.HTuple]$inputCount = $null; [HalconDotNet.HOperatorSet]::CountObj($segments, [ref]$inputCount); Assert-True($inputCount.I -eq 2) 'The caller-owned input XLD was mutated or released.'
} finally { $segments.Dispose(); $direct.Dispose() }
$reversed = New-SegmentSet 4 $true; $reverseTool = New-Tool XldAdjacentContourUnion 'S39 Reverse Union'; try { $reverseTool.Parameters.XldAdjacentMaxDistAbs=10;$reverseTool.Parameters.XldAdjacentMaxDistRel=1;Invoke-Union $reverseTool $reversed|Out-Null;Assert-True([int](Get-Out $reverseTool ContourCount)-eq1) 'Reversed endpoint order was not joined.' } finally { $reversed.Dispose();$reverseTool.Dispose() }
[HalconDotNet.HObject]$attributedEdges=$null;$attributeImage=[HalconDotNet.HImage]::new($imagePath.ToString());$attributeTool=New-Tool XldAdjacentContourUnion 'S39 Attribute Union'
try {
    [HalconDotNet.HOperatorSet]::EdgesSubPix($attributeImage,[ref]$attributedEdges,'canny',1.0,10.0,20.0);[HalconDotNet.HTuple]$attributes=$null;[HalconDotNet.HOperatorSet]::QueryContourAttribsXld($attributedEdges,[ref]$attributes);Assert-True(($attributes.S -contains 'angle')) 'Attributed edge fixture has no angle attribute.'
    $attributeTool.Parameters.XldAdjacentMaxDistAbs=0;$attributeTool.Parameters.XldAdjacentMaxDistRel=0;$attributeTool.Parameters.XldAdjacentAttributeMode='AttrKeep';Invoke-Union $attributeTool $attributedEdges|Out-Null;$keepSnapshot=Get-Out $attributeTool XLD;$keepCopy=$keepSnapshot.CreateXldCopy();try{[HalconDotNet.HOperatorSet]::QueryContourAttribsXld($keepCopy,[ref]$attributes);Assert-True(($attributes.S -contains 'angle')) 'attr_keep did not preserve the XLD point attribute.'}finally{$keepCopy.Dispose()}
    $attributeTool.Parameters.XldAdjacentAttributeMode='AttrForget';Invoke-Union $attributeTool $attributedEdges|Out-Null;Assert-True($keepSnapshot.IsDisposed) 'Attribute-mode rerun did not release the old snapshot.';$forgetCopy=(Get-Out $attributeTool XLD).CreateXldCopy();try{[HalconDotNet.HOperatorSet]::QueryContourAttribsXld($forgetCopy,[ref]$attributes);Assert-True($attributes.Length-eq0) 'attr_forget did not drop XLD point attributes.'}finally{$forgetCopy.Dispose()}
} finally { $attributeImage.Dispose();if($null-ne$attributedEdges){$attributedEdges.Dispose()};$attributeTool.Dispose() }
[HalconDotNet.HObject]$empty = $null; [HalconDotNet.HOperatorSet]::GenEmptyObj([ref]$empty); $emptyTool=New-Tool XldAdjacentContourUnion 'S39 Empty Union'; try { $emptyRecord=Invoke-Union $emptyTool $empty;Assert-True($emptyRecord.ResultCode-eq'NG'-and[int](Get-Out $emptyTool ContourCount)-eq0) 'Empty XLD semantics are wrong.' } finally { $empty.Dispose();$emptyTool.Dispose() }
$invalid=New-Tool XldAdjacentContourUnion 'S39 Invalid Union';$invalidInput=New-SegmentSet 4 $false;try{$invalid.Parameters.XldAdjacentMaxDistAbs=-1;$rejected=$false;try{Invoke-Union $invalid $invalidInput|Out-Null}catch{$rejected=$true};Assert-True $rejected 'Negative absolute distance was accepted.';$invalid.Parameters.XldAdjacentMaxDistAbs=10;$invalid.Parameters.XldAdjacentMaxDistRel=1001;$rejected=$false;try{Invoke-Union $invalid $invalidInput|Out-Null}catch{$rejected=$true};Assert-True $rejected 'Out-of-range relative distance was accepted.'}finally{$invalidInput.Dispose();$invalid.Dispose()}

$disposeFlow.Invoke($window, @()) | Out-Null; $flow.Clear()
$source=New-Tool ImageSource 'S39 Source';$producer=New-Tool SubpixelEdgeExtraction 'S39 XLD Producer';$union=New-Tool XldAdjacentContourUnion 'S39 Adjacent Union';$filter=New-Tool XldContourFilter 'S39 Filter';$judge=New-Tool NumericJudge 'S39 Judge'
$source.Parameters.LocalImagePath=$imagePath;$producer.SetInputBinding('Image',$source.ToolId,'Image');$union.SetInputBinding('XLD',$producer.ToolId,'XLD');$filter.SetInputBinding('XLD',$union.ToolId,'XLD');$judge.SetInputBinding('Value',$union.ToolId,'JoinedCount');$judge.InputToolId=$union.ToolId;$judge.InputPortName='JoinedCount';$filter.Parameters.XldContourMin=1;$filter.Parameters.XldContourMax=100000;$judge.NumericLowerLimit=1;$judge.NumericUpperLimit=1
foreach($tool in @($source,$producer,$union,$filter,$judge)){$flow.Add($tool)}
Invoke-Execute $source 'S39 flow'|Out-Null
$flowSegments=New-SegmentSet 4 $false;try{$producer.SetOutputValue('XLD',(New-Snapshot $flowSegments $producer));$producer.SetOutputValue('ContourCount',[double]2);$producer.ResultCode='OK'}finally{$flowSegments.Dispose()}
Invoke-Execute $union 'S39 flow'|Out-Null;Invoke-Execute $filter 'S39 flow'|Out-Null;Invoke-Execute $judge 'S39 flow'|Out-Null
Assert-True($union.ResultCode-eq'OK'-and$filter.ResultCode-eq'OK'-and$judge.ResultCode-eq'OK') 'Union XLD/JoinedCount did not drive the real filter and NumericJudge chain.'
$recipe=$captureRecipe.Invoke($window,@());$saved=$recipe.ToolFlow|Where-Object ToolType -eq 'XldAdjacentContourUnion'|Select-Object -First 1;Assert-True($saved.Parameters.XldAdjacentMaxDistAbs-eq10-and$saved.Parameters.XldAdjacentMaxDistRel-eq1-and$saved.Parameters.XldAdjacentAttributeMode-eq'AttrKeep') 'Union parameters were not captured in the recipe.'

# A line: nearest subpixel segment hit, geometry, navigation, isolate, viewport isolation, rerun and source invalidation.
$loadImage.Invoke($window,[object[]]@($imagePath.ToString(),$false))|Out-Null;$setContext.Invoke($window,[object[]]@('GlobalInput'))|Out-Null
$refreshLayers.Invoke($window,@())|Out-Null;$layers=Field 'resultOverlayLayers';$producerLayer=$layers|Where-Object{$_.ToolId-eq$producer.ToolId-and$_.PortName-eq'XLD'}|Select-Object -First 1;Assert-True($null-ne$producerLayer) 'Producer XLD result layer was not created.';$producerLayer.ViewportScope='Both';(Field 'ResultLayerList').SelectedItem=$producerLayer
$setSelectionMode.Invoke($window,[object[]]@($true,$false))|Out-Null
$picked=[bool]$tryPick.Invoke($window,[object[]]@($false,[Drawing.PointF]::new(15,20.8),[double]3));Assert-True $picked 'Right viewport click was not consumed.';Assert-True((Selection-Value 'ViewportKey')-eq'Right') 'Right viewport selection was assigned to the wrong context.';Assert-True([int](Selection-Value 'ObjectIndex')-eq1-and[int](Selection-Value 'ObjectCount')-eq2-and[int](Selection-Value 'PointCount')-eq3) 'Selected contour identity/geometry is wrong.';Assert-True([Math]::Abs([double](Selection-Value 'NearestRow')-20)-lt0.01-and[Math]::Abs([double](Selection-Value 'NearestColumn')-15)-lt0.01) 'Nearest subpixel point was not projected onto the contour segment.'
$navigate.Invoke($window,[object[]]@([int]1))|Out-Null;Assert-True([int](Selection-Value 'ObjectIndex')-eq2) 'Next contour navigation failed.';(Field 'XldContourIsolateToggleButton').IsChecked=$true;Assert-True([bool]$ensureSelection.Invoke($window,[object[]]@($true))) 'Isolated selection unexpectedly became invalid.'
(Field 'FlowToolList').SelectedItem=$producer;$setDual.Invoke($window,[object[]]@($true))|Out-Null;Pump-Ui 100;$producerLayer.ViewportScope='Both';(Field 'ResultLayerList').SelectedItem=$producerLayer
$pickedLeft=[bool]$tryPick.Invoke($window,[object[]]@($true,[Drawing.PointF]::new(15,20.5),[double]3));Assert-True($pickedLeft-and(Selection-Value 'ViewportKey')-eq'Left') 'Left viewport did not keep an isolated input-coordinate selection.';Pump-Ui 50;$contextText=(Field 'XldContourInspectorContextText').Text.ToString();Assert-True($contextText.Contains('S39 XLD Producer.Image')) "Inspector does not expose the concrete left input source: $contextText"
$selectedSnapshot=$mainType.GetField('xldContourSelection',$private).GetValue($window).GetType().GetProperty('Snapshot').GetValue($mainType.GetField('xldContourSelection',$private).GetValue($window),$null)
$replacementSegments=New-SegmentSet 6 $false;try{$producer.SetOutputValue('XLD',(New-Snapshot $replacementSegments $producer))}finally{$replacementSegments.Dispose()};$refreshLayers.Invoke($window,@())|Out-Null;Assert-True($selectedSnapshot.IsDisposed-and$null-eq$mainType.GetField('xldContourSelection',$private).GetValue($window)) 'Rerun did not safely invalidate the selected old XLD snapshot.'
$producerLayer=(Field 'resultOverlayLayers')|Where-Object{$_.ToolId-eq$producer.ToolId-and$_.PortName-eq'XLD'}|Select-Object -First 1;$producerLayer.ViewportScope='Both';(Field 'ResultLayerList').SelectedItem=$producerLayer;$setDual.Invoke($window,[object[]]@($false))|Out-Null;$setContext.Invoke($window,[object[]]@('GlobalInput'))|Out-Null;$tryPick.Invoke($window,[object[]]@($false,[Drawing.PointF]::new(15,20.3),[double]3))|Out-Null;Assert-True($null-ne$mainType.GetField('xldContourSelection',$private).GetValue($window)) 'Selection could not be recreated before source-change validation.';$loadImage.Invoke($window,[object[]]@($imagePath.ToString(),$false))|Out-Null;Assert-True($null-eq$mainType.GetField('xldContourSelection',$private).GetValue($window)) 'Image source identity change did not invalidate the contour selection.'

$mainType.GetField('recipeCloseConfirmed',$private).SetValue($window,$true);$window.Close();$app.Shutdown();if(Test-Path $tempRoot){Remove-Item -LiteralPath $tempRoot -Recurse -Force}
Write-Output 'S39 XLD contour inspector and HALCON adjacent contour union smoke tests passed.'
