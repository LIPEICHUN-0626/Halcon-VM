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
function Wait-UiTask($Task, [int]$TimeoutMilliseconds = 30000) {
    $watch = [Diagnostics.Stopwatch]::StartNew()
    while (-not $Task.IsCompleted -and $watch.ElapsedMilliseconds -lt $TimeoutMilliseconds) { Pump-Ui 20 }
    Assert-True $Task.IsCompleted "Async operation timed out after $TimeoutMilliseconds ms."
    if ($Task.IsFaulted) { throw $Task.Exception.Flatten().InnerExceptions[0] }
    $Task.GetAwaiter().GetResult() | Out-Null
}
function Write-TestImage([string]$Path, [int]$Seed) {
    $bitmap = [Drawing.Bitmap]::new(128, 88); $graphics = [Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.Clear([Drawing.Color]::FromArgb(20 + $Seed, 30 + $Seed, 50 + $Seed))
        $brush = [Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(225, 150 + $Seed, 50 + $Seed))
        try { $graphics.FillEllipse($brush, 12 + $Seed, 14, 50, 44); $graphics.FillRectangle($brush, 72, 24, 32, 30) } finally { $brush.Dispose() }
        $bitmap.Save($Path, [Drawing.Imaging.ImageFormat]::Png)
    } finally { $graphics.Dispose(); $bitmap.Dispose() }
}
function New-RectangleContour() {
    [HalconDotNet.HObject]$contour = $null
    [HalconDotNet.HOperatorSet]::GenContourPolygonXld([ref]$contour, [HalconDotNet.HTuple]@([double]0,0,10,10,0), [HalconDotNet.HTuple]@([double]0,20,20,0,0))
    $contour
}
function New-OpenContour() {
    [HalconDotNet.HObject]$contour = $null
    [HalconDotNet.HOperatorSet]::GenContourPolygonXld([ref]$contour, [HalconDotNet.HTuple]@([double]5,8,13,19), [HalconDotNet.HTuple]@([double]2,9,15,22))
    $contour
}

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s41-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($tempRoot) | Out-Null
[IO.File]::WriteAllBytes((Join-Path $tempRoot '000_corrupt.png'), [byte[]](1,2,3,4,5,6))
for ($i = 1; $i -le 10; $i++) { Write-TestImage (Join-Path $tempRoot (('{0:000}.png' -f $i))) $i }

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
$runAffine = $mainType.GetMethod('RunXldAffineTransformTool', $private); $captureRecipe = $mainType.GetMethod('CaptureRecipe', $private)
$loadQueue = $mainType.GetMethods($private) | Where-Object { $_.Name -eq 'LoadImageQueue' -and $_.GetParameters().Count -eq 1 } | Select-Object -First 1
$getTargets = $mainType.GetMethod('GetImageQueueRunTargets', $private); $runBatch = $mainType.GetMethod('RunImageQueueBatchAsync', $private)
$firstQueue = $mainType.GetMethod('FirstImageQueueButton_Click', $private); $lastQueue = $mainType.GetMethod('LastImageQueueButton_Click', $private)
$selectAll = $mainType.GetMethod('SelectAllImageQueueButton_Click', $private); $clearSelection = $mainType.GetMethod('ClearImageQueueSelectionButton_Click', $private); $stopBatch = $mainType.GetMethod('StopImageQueueBatchButton_Click', $private)
function Field([string]$Name) { $field = $mainType.GetField($Name, $all); if ($null -eq $field) { throw "Field missing: $Name" }; $field.GetValue($window) }
function Set-PrivateField([string]$Name, $Value) { $mainType.GetField($Name, $private).SetValue($window, $Value) }
function New-Tool([string]$Kind, [string]$Name) { $create.Invoke($window, [object[]]@([Enum]::Parse($kindType, $Kind), $Name, $true, $null)) }
function Invoke-Execute($Tool, [string]$Source) { try { $execute.Invoke($window, [object[]]@($Tool, $Source)) } catch [Reflection.TargetInvocationException] { throw $_.Exception.InnerException } }
function Invoke-Affine($Tool, $Xld) { try { $runAffine.Invoke($window, [object[]]@($Tool, $Xld)) } catch [Reflection.TargetInvocationException] { throw $_.Exception.InnerException } }
function Get-Out($Tool, [string]$Port) { $value = $null; if (-not $Tool.TryGetOutputValue($Port, [ref]$value)) { throw "$($Tool.InstanceName).$Port missing" }; $value }
function New-Snapshot($Xld, $Tool) { $snapshotType.GetMethod('Create', $staticPublic).Invoke($null, [object[]]@($Xld,$Tool.ToolId,$Tool.InstanceName,'XLD')) }
function Reset-QueueRunState {
    foreach ($item in (Field 'imageQueueItems')) { $item.RunStateKey='NotRun'; $item.RunResultCode='--'; $item.RunElapsedMilliseconds=0; $item.RunMessage='waiting'; $item.IsRunSelected=$false }
}

foreach ($name in @('ImageQueueFirstButton','ImageQueueLastButton','ImageQueueSelectAllButton','ImageQueueClearSelectionButton','ImageQueueRunScopeComboBox','ImageQueueRunButton','ImageQueueStopButton','ImageQueueBatchSummaryText','DockXldAffineTransformPanel','DockXldAffineCenterModeComboBox','DockXldAffineCenterRowTextBox','DockXldAffineCenterColumnTextBox','DockXldAffineTranslateRowTextBox','DockXldAffineTranslateColumnTextBox','DockXldAffineScaleRowTextBox','DockXldAffineScaleColumnTextBox','DockXldAffineRotationTextBox','DockXldAffineSlantTextBox','DockXldAffineOrderComboBox','DockXldAffineCoordinatePolicyComboBox')) { Assert-True($null -ne (Field $name)) "$name is missing." }
Assert-True($null -ne ($catalog | Where-Object { $_.Kind.ToString() -eq 'XldAffineTransform' } | Select-Object -First 1)) 'XLD affine transform is missing from the toolbox.'
$affineKind = [Enum]::Parse($kindType, 'XldAffineTransform'); $inputs = $metadataType.GetMethod('GetInputPorts', $staticPublic).Invoke($null, [object[]]@($affineKind)); $outputs = $metadataType.GetMethod('GetOutputPorts', $staticPublic).Invoke($null, [object[]]@($affineKind))
Assert-True(($inputs | Where-Object { $_.PortName -eq 'XLD' -and $_.DataType -eq 'XLD' -and -not $_.IsOptional }).Count -eq 1) 'Affine required XLD input is missing.'
foreach ($port in @('XLD','ContourCount','InputTotalLength','TotalLength','MinRow','MinColumn','MaxRow','MaxColumn','CenterRow','CenterColumn','HomMat2D','TransformOrder','CoordinatePolicy','AttributePolicy','Parameters','ResultCode')) { Assert-True(($outputs | Where-Object PortName -eq $port).Count -eq 1) "Affine output $port is missing." }

$app = [Windows.Application]::Current; if ($null -eq $app) { $app = New-Object Windows.Application; $app.ShutdownMode = [Windows.ShutdownMode]::OnExplicitShutdown }
$window.WindowStartupLocation = [Windows.WindowStartupLocation]::Manual; $window.Left=2; $window.Top=2; $window.Width=1760; $window.Height=980; $window.Topmost=$true; $window.Show(); $window.Activate() | Out-Null; Pump-Ui 300

# A line: real current/selected/all execution, first/last, per-image states, summaries, stop, NG-stop and timeout.
$paths = Get-ChildItem -LiteralPath $tempRoot -Filter '*.png' -File | Sort-Object Name | ForEach-Object FullName
try { $loadQueue.Invoke($window, [object[]]@(,[string[]]$paths)) | Out-Null } catch [Reflection.TargetInvocationException] { throw $_.Exception.InnerException }
Assert-True((Field 'imageQueueItems').Count -eq 11) 'Expected one corrupt and ten readable queue items.'
$firstQueue.Invoke($window,[object[]]@($null,$null)) | Out-Null; Assert-True([int](Field 'imageIndex') -eq 1) 'First button did not select first readable image.'
$lastQueue.Invoke($window,[object[]]@($null,$null)) | Out-Null; Assert-True([int](Field 'imageIndex') -eq 10) 'Last button did not select last readable image.'
$selectAll.Invoke($window,[object[]]@($null,$null)) | Out-Null; Assert-True(((Field 'imageQueueItems') | Where-Object IsRunSelected).Count -eq 11) 'Select-all did not select every queue item.'
$clearSelection.Invoke($window,[object[]]@($null,$null)) | Out-Null; Assert-True(((Field 'imageQueueItems') | Where-Object IsRunSelected).Count -eq 0) 'Clear-selection did not clear every queue item.'
Assert-True(($getTargets.Invoke($window,[object[]]@('All'))).Count -eq 11) 'All scope is incomplete.'
Assert-True(($getTargets.Invoke($window,[object[]]@('Current'))).Count -eq 1) 'Current scope is not singular.'

$disposeFlow.Invoke($window,@()) | Out-Null; $flow.Clear(); $threshold=New-Tool ImageThreshold 'S41 Queue Threshold'; $threshold.Parameters.ImageThresholdMinGray=0; $threshold.Parameters.ImageThresholdMaxGray=255; $flow.Add($threshold)
(Field 'ContinuousIntervalTextBox').Text='50'; (Field 'FlowTimeoutTextBox').Text='0'; (Field 'StopOnNgCheckBox').IsChecked=$false
(Field 'ImageQueueRunScopeComboBox').SelectedIndex=2
Wait-UiTask ($runBatch.Invoke($window,@())) 30000
$items = Field 'imageQueueItems'; Assert-True(($items | Where-Object RunStateKey -eq 'OK').Count -eq 10) 'All-scope did not run every readable image through the real flow.'
Assert-True(($items | Where-Object RunStateKey -eq 'Error').Count -eq 1) 'Corrupt image did not remain an isolated per-image error.'
Assert-True((Field 'imageQueueBatchSummary').Contains('OK 10')) 'All-scope summary counts are wrong.'

Reset-QueueRunState; $items[2].IsRunSelected=$true; $items[4].IsRunSelected=$true; (Field 'ImageQueueRunScopeComboBox').SelectedIndex=1
Assert-True(($getTargets.Invoke($window,[object[]]@('Selected'))).Count -eq 2) 'Selected scope did not use checkbox selections.'
Wait-UiTask ($runBatch.Invoke($window,@())) 30000
Assert-True($items[2].RunStateKey -eq 'OK' -and $items[4].RunStateKey -eq 'OK' -and ($items | Where-Object RunStateKey -eq 'NotRun').Count -eq 9) 'Selected-scope run touched the wrong queue items.'

Reset-QueueRunState; $firstQueue.Invoke($window,[object[]]@($null,$null)) | Out-Null; (Field 'ImageQueueRunScopeComboBox').SelectedIndex=0
Wait-UiTask ($runBatch.Invoke($window,@())) 30000
Assert-True($items[1].RunStateKey -eq 'OK' -and ($items | Where-Object RunStateKey -eq 'NotRun').Count -eq 10) 'Current-scope run was not restricted to the viewed image.'

Reset-QueueRunState; (Field 'ImageQueueRunScopeComboBox').SelectedIndex=2; (Field 'ContinuousIntervalTextBox').Text='500'
$stopTask=$runBatch.Invoke($window,@()); $watch=[Diagnostics.Stopwatch]::StartNew(); while([int](Field 'imageQueueBatchProcessed') -lt 1 -and -not $stopTask.IsCompleted -and $watch.ElapsedMilliseconds -lt 10000){Pump-Ui 20}; $stopBatch.Invoke($window,[object[]]@($null,$null))|Out-Null; Wait-UiTask $stopTask 10000
Assert-True(($items | Where-Object RunStateKey -eq 'Skipped').Count -gt 0 -and -not [bool](Field 'isImageQueueBatchRunning')) 'Batch stop did not respond during the inter-image interval.'

Reset-QueueRunState; $items[1].IsRunSelected=$true; $items[2].IsRunSelected=$true; (Field 'ImageQueueRunScopeComboBox').SelectedIndex=1; (Field 'ContinuousIntervalTextBox').Text='50'; (Field 'StopOnNgCheckBox').IsChecked=$true
$judge=New-Tool NumericJudge 'S41 Forced NG'; $judge.SetInputBinding('Value',$threshold.ToolId,'Area'); $judge.InputToolId=$threshold.ToolId; $judge.InputPortName='Area'; $judge.NumericLowerLimit=999999999; $judge.NumericUpperLimit=1000000000; $flow.Add($judge)
Wait-UiTask ($runBatch.Invoke($window,@())) 30000
Assert-True($items[1].RunStateKey -eq 'NG' -and $items[2].RunStateKey -eq 'Skipped') 'Stop-on-NG did not stop after the first real NG image.'

$disposeFlow.Invoke($window,@()) | Out-Null; $flow.Clear(); for($i=0;$i-lt 100;$i++){ $tool=New-Tool ImageThreshold ('S41 Timeout '+$i); $tool.Parameters.ImageThresholdMinGray=0; $tool.Parameters.ImageThresholdMaxGray=255; $flow.Add($tool) }
Reset-QueueRunState; $firstQueue.Invoke($window,[object[]]@($null,$null))|Out-Null; (Field 'ImageQueueRunScopeComboBox').SelectedIndex=0; (Field 'StopOnNgCheckBox').IsChecked=$false; (Field 'FlowTimeoutTextBox').Text='50'
Wait-UiTask ($runBatch.Invoke($window,@())) 30000
Assert-True($items[1].RunResultCode -eq 'TIMEOUT' -and $items[1].RunStateKey -eq 'Error') 'Flow timeout was not propagated to the per-image batch state.'

# B line: HALCON affine_trans_contour_xld, coordinate policies, order, empty/multiple, validation and lifetime.
$direct=New-Tool XldAffineTransform 'S41 Direct Affine'; $rectangle=New-RectangleContour
try {
    $direct.Parameters.XldAffineScaleRow=1; $direct.Parameters.XldAffineScaleColumn=1; $direct.Parameters.XldAffineCenterMode='InputBounds'; $direct.Parameters.XldAffineCoordinatePolicy='StandardPixelCenter'
    Invoke-Affine $direct $rectangle | Out-Null
    Assert-True([Math]::Abs([double](Get-Out $direct MinRow)-0)-lt .0001 -and [Math]::Abs([double](Get-Out $direct MaxColumn)-20)-lt .0001) 'Identity affine changed contour bounds.'
    Assert-True([Math]::Abs([double](Get-Out $direct CenterRow)-5)-lt .0001 -and [Math]::Abs([double](Get-Out $direct CenterColumn)-10)-lt .0001) 'Input-bounds center is wrong.'
    $old=Get-Out $direct XLD; $direct.Parameters.XldAffineTranslateRow=5; $direct.Parameters.XldAffineTranslateColumn=-3; Invoke-Affine $direct $rectangle | Out-Null
    Assert-True($old.IsDisposed) 'Affine rerun did not dispose the previous managed XLD snapshot.'
    Assert-True([Math]::Abs([double](Get-Out $direct MinRow)-5)-lt .0001 -and [Math]::Abs([double](Get-Out $direct MinColumn)+3)-lt .0001) 'Positive Row/negative Column translation mapping is wrong.'
    [HalconDotNet.HTuple]$callerCount=$null; [HalconDotNet.HOperatorSet]::CountObj($rectangle,[ref]$callerCount); Assert-True($callerCount.I-eq1) 'Caller-owned input contour was mutated or disposed.'

    $direct.Parameters.XldAffineTranslateRow=0; $direct.Parameters.XldAffineTranslateColumn=0; $direct.Parameters.XldAffineRotationDegrees=90; Invoke-Affine $direct $rectangle|Out-Null
    Assert-True([Math]::Abs(([double](Get-Out $direct MaxRow)-[double](Get-Out $direct MinRow))-20)-lt .001 -and [Math]::Abs(([double](Get-Out $direct MaxColumn)-[double](Get-Out $direct MinColumn))-10)-lt .001) 'Center rotation did not swap the rectangular extents.'
    $standardMatrix=[string](Get-Out $direct HomMat2D); $direct.Parameters.XldAffineCoordinatePolicy='AffineTransPixel'; Invoke-Affine $direct $rectangle|Out-Null; $rawMatrix=[string](Get-Out $direct HomMat2D)
    Assert-True($standardMatrix-ne$rawMatrix -and (Get-Out $direct CoordinatePolicy).Contains('affine_trans_pixel')) 'Coordinate-policy switch did not expose the documented +/-0.5 distinction.'

    $direct.Parameters.XldAffineCoordinatePolicy='StandardPixelCenter'; $direct.Parameters.XldAffineScaleRow=1.7; $direct.Parameters.XldAffineScaleColumn=.6; $direct.Parameters.XldAffineRotationDegrees=23; $direct.Parameters.XldAffineSlantDegrees=11; $direct.Parameters.XldAffineTransformOrder='ScaleSlantRotateTranslate'; Invoke-Affine $direct $rectangle|Out-Null; $matrixA=[string](Get-Out $direct HomMat2D)
    $direct.Parameters.XldAffineTransformOrder='RotateSlantScaleTranslate'; Invoke-Affine $direct $rectangle|Out-Null; $matrixB=[string](Get-Out $direct HomMat2D)
    Assert-True($matrixA-ne$matrixB -and ([string](Get-Out $direct TransformOrder)).Length-gt0) 'Affine operation order is not real or not visible in outputs.'
    Assert-True((Get-Out $direct AttributePolicy).Contains('HALCON affine_trans_contour_xld')) 'Attribute ownership policy is not explicit.'
} finally { $rectangle.Dispose(); $direct.Dispose() }

$open=New-OpenContour; $rect=New-RectangleContour; [HalconDotNet.HObject]$multi=$null; $multiTool=New-Tool XldAffineTransform 'S41 Multiple'
try { [HalconDotNet.HOperatorSet]::ConcatObj($open,$rect,[ref]$multi); Invoke-Affine $multiTool $multi|Out-Null; Assert-True([int](Get-Out $multiTool ContourCount)-eq2) 'Multiple open/closed contours were not preserved.' } finally { $open.Dispose();$rect.Dispose();if($null-ne$multi){$multi.Dispose()};$multiTool.Dispose() }
[HalconDotNet.HObject]$empty=$null; [HalconDotNet.HOperatorSet]::GenEmptyObj([ref]$empty); $emptyTool=New-Tool XldAffineTransform 'S41 Empty'; try { $record=Invoke-Affine $emptyTool $empty; Assert-True($record.ResultCode-eq'NG'-and[int](Get-Out $emptyTool ContourCount)-eq0) 'Empty XLD affine semantics are wrong.' } finally { $empty.Dispose();$emptyTool.Dispose() }
foreach($invalid in @(@{sr=0;sc=1;rot=0;slant=0;mode='InputBounds'},@{sr=1;sc=1;rot=4000;slant=0;mode='InputBounds'},@{sr=1;sc=1;rot=0;slant=80;mode='InputBounds'},@{sr=1;sc=1;rot=0;slant=0;mode='Bogus'})) { $tool=New-Tool XldAffineTransform 'S41 Invalid';$input=New-OpenContour;try{$tool.Parameters.XldAffineScaleRow=$invalid.sr;$tool.Parameters.XldAffineScaleColumn=$invalid.sc;$tool.Parameters.XldAffineRotationDegrees=$invalid.rot;$tool.Parameters.XldAffineSlantDegrees=$invalid.slant;$tool.Parameters.XldAffineCenterMode=$invalid.mode;$rejected=$false;try{Invoke-Affine $tool $input|Out-Null}catch{$rejected=$true};Assert-True $rejected 'Invalid affine parameters were accepted.'}finally{$input.Dispose();$tool.Dispose()} }

$disposeFlow.Invoke($window,@())|Out-Null;$flow.Clear();$producer=New-Tool SubpixelEdgeExtraction 'S41 XLD Producer';$affine=New-Tool XldAffineTransform 'S41 Flow Affine';$fit=New-Tool XldLineFitting 'S41 Fit';$countJudge=New-Tool NumericJudge 'S41 Count Judge';$affine.SetInputBinding('XLD',$producer.ToolId,'XLD');$fit.SetInputBinding('XLD',$affine.ToolId,'XLD');$countJudge.SetInputBinding('Value',$affine.ToolId,'ContourCount');$countJudge.InputToolId=$affine.ToolId;$countJudge.InputPortName='ContourCount';$countJudge.NumericLowerLimit=1;$countJudge.NumericUpperLimit=1;$affine.Parameters.XldAffineTranslateRow=4;$affine.Parameters.XldAffineTranslateColumn=6;foreach($tool in @($producer,$affine,$fit,$countJudge)){$flow.Add($tool)}
$flowXld=New-OpenContour;try{$producer.SetOutputValue('XLD',(New-Snapshot $flowXld $producer));$producer.SetOutputValue('ContourCount',[double]1);$producer.ResultCode='OK'}finally{$flowXld.Dispose()};Invoke-Execute $affine 'S41 flow'|Out-Null;Invoke-Execute $fit 'S41 flow'|Out-Null;Invoke-Execute $countJudge 'S41 flow'|Out-Null;Assert-True($affine.ResultCode-eq'OK'-and$fit.ResultCode-eq'OK'-and$countJudge.ResultCode-eq'OK') 'Affine XLD/ContourCount did not drive fitting and NumericJudge.'
(Field 'FlowToolList').SelectedItem=$affine;$mainType.GetMethod('LoadDockConfigurationDraft',$private).Invoke($window,[object[]]@($affine))|Out-Null;Assert-True((Field 'DockXldAffineTransformPanel').Visibility.ToString()-eq'Visible'-and(Field 'DockXldAffineTranslateRowTextBox').Text-eq'4') 'Affine dock panel is not visible/synchronized.'
$recipe=$captureRecipe.Invoke($window,@());$saved=$recipe.ToolFlow|Where-Object ToolType -eq 'XldAffineTransform'|Select-Object -First 1;Assert-True($saved.Parameters.XldAffineTranslateRow-eq4-and$saved.Parameters.XldAffineCoordinatePolicy-eq'StandardPixelCenter') 'Affine parameters were not captured in the recipe.'
$legacy=$assembly.GetType('HalconWinFormsDemo.Models.VmToolParameterData',$true).GetConstructor([Type[]]@()).Invoke(@());$legacy.XldAffineScaleRow=0;$legacy.XldAffineScaleColumn=0;$legacy.XldAffineCenterMode=$null;$legacy.XldAffineCoordinatePolicy=$null;$legacy.XldAffineTransformOrder=$null;$legacy.Normalize();Assert-True($legacy.XldAffineScaleRow-eq1-and$legacy.XldAffineScaleColumn-eq1-and$legacy.XldAffineCoordinatePolicy-eq'StandardPixelCenter') 'Missing affine fields do not normalize to identity defaults.'
$upstream=Get-Out $producer XLD;$upstream.Dispose();$rejected=$false;try{Invoke-Execute $affine 'S41 disposed upstream'|Out-Null}catch{$rejected=$true};Assert-True $rejected 'Disposed upstream XLD was not rejected.'

$mainType.GetField('recipeCloseConfirmed',$private).SetValue($window,$true);$window.Close();$app.Shutdown();if(Test-Path $tempRoot){Remove-Item -LiteralPath $tempRoot -Recurse -Force}
Write-Output 'S41 image queue run range and HALCON XLD affine transform smoke tests passed.'
