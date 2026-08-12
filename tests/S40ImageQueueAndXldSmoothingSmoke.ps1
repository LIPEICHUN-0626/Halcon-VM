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
function Write-TestImage([string]$Path, [string]$Format, [int]$Seed) {
    $bitmap = [Drawing.Bitmap]::new(112, 76)
    $graphics = [Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.Clear([Drawing.Color]::FromArgb(18 + ($Seed % 30), 26 + ($Seed % 40), 42 + ($Seed % 50)))
        $brush = [Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(220, 180 - ($Seed % 60), 70 + ($Seed % 100)))
        try { $graphics.FillEllipse($brush, 10 + ($Seed % 20), 12, 48, 40); $graphics.FillRectangle($brush, 64, 20 + ($Seed % 10), 35, 28) } finally { $brush.Dispose() }
        $imageFormat = switch ($Format) { 'bmp' { [Drawing.Imaging.ImageFormat]::Bmp } 'jpg' { [Drawing.Imaging.ImageFormat]::Jpeg } 'tif' { [Drawing.Imaging.ImageFormat]::Tiff } default { [Drawing.Imaging.ImageFormat]::Png } }
        $bitmap.Save($Path, $imageFormat)
    } finally { $graphics.Dispose(); $bitmap.Dispose() }
}
function New-NoisyLine([int]$PointCount) {
    $rows = New-Object double[] $PointCount; $columns = New-Object double[] $PointCount
    for ($i = 0; $i -lt $PointCount; $i++) { $columns[$i] = 10.0 + $i; $rows[$i] = 30.0 + 0.22 * $i + $(if (($i % 2) -eq 0) { 3.0 } else { -3.0 }) }
    [HalconDotNet.HObject]$contour = $null
    [HalconDotNet.HOperatorSet]::GenContourPolygonXld([ref]$contour, [HalconDotNet.HTuple]$rows, [HalconDotNet.HTuple]$columns)
    return $contour
}
function New-ShortContour() {
    [HalconDotNet.HObject]$contour = $null
    [HalconDotNet.HOperatorSet]::GenContourPolygonXld([ref]$contour, [HalconDotNet.HTuple]@([double]10,[double]11,[double]10), [HalconDotNet.HTuple]@([double]10,[double]11,[double]12))
    return $contour
}

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s40-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($tempRoot) | Out-Null
[IO.File]::WriteAllBytes((Join-Path $tempRoot '000_corrupt.png'), [byte[]](1,2,3,4,5,6))
$formats = @('bmp','png','jpg','tif')
for ($i = 1; $i -le 30; $i++) { $format = if ($i -le 4) { $formats[$i - 1] } else { 'png' }; Write-TestImage (Join-Path $tempRoot (('{0:000}.{1}' -f $i,$format))) $format $i }

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
$runSmooth = $mainType.GetMethod('RunXldContourSmoothingTool', $private); $runLine = $mainType.GetMethod('RunXldLineFittingTool', $private); $captureRecipe = $mainType.GetMethod('CaptureRecipe', $private)
$loadQueue = $mainType.GetMethods($private) | Where-Object { $_.Name -eq 'LoadImageQueue' -and $_.GetParameters().Count -eq 1 } | Select-Object -First 1
$loadQueueFull = $mainType.GetMethods($private) | Where-Object { $_.Name -eq 'LoadImageQueue' -and $_.GetParameters().Count -eq 3 } | Select-Object -First 1
$tryLoad = $mainType.GetMethod('TryLoadImageAtIndex', $private); $advance = $mainType.GetMethod('TryAdvanceImageQueue', $private); $playTick = $mainType.GetMethod('PlaybackTimer_Tick', $private); $clearQueue = $mainType.GetMethod('ClearImageQueue', $private); $setDual = $mainType.GetMethod('SetDualImageMode', $private)
function Field([string]$Name) { $field = $mainType.GetField($Name, $all); if ($null -eq $field) { throw "Field missing: $Name" }; $field.GetValue($window) }
function New-Tool([string]$Kind, [string]$Name) { $create.Invoke($window, [object[]]@([Enum]::Parse($kindType, $Kind), $Name, $true, $null)) }
function Invoke-Execute($Tool, [string]$Source) { try { $execute.Invoke($window, [object[]]@($Tool, $Source)) } catch [Reflection.TargetInvocationException] { throw $_.Exception.InnerException } }
function Invoke-Smooth($Tool, $Xld) { try { $runSmooth.Invoke($window, [object[]]@($Tool, $Xld)) } catch [Reflection.TargetInvocationException] { throw $_.Exception.InnerException } }
function Invoke-Line($Tool, $Xld) { try { $runLine.Invoke($window, [object[]]@($Tool, $Xld)) } catch [Reflection.TargetInvocationException] { throw $_.Exception.InnerException } }
function Get-Out($Tool, [string]$Port) { $value = $null; if (-not $Tool.TryGetOutputValue($Port, [ref]$value)) { throw "$($Tool.InstanceName).$Port missing" }; $value }
function New-Snapshot($Xld, $Tool) { $snapshotType.GetMethod('Create', $staticPublic).Invoke($null, [object[]]@($Xld,$Tool.ToolId,$Tool.InstanceName,'XLD')) }

foreach ($name in @('ImageQueuePanelToggleButton','ImageQueuePanel','ImageQueueList','ImageQueueCountText','ImageQueueStateText','ImageQueuePreviousButton','ImageQueueNextButton','ImageQueuePlayPauseButton','ImageQueueReloadButton','ImageQueueClearButton','ImageQueueProgressBar','ImageQueueProgressText','DockXldContourSmoothingPanel','DockXldSmoothNumRegrPointsTextBox')) { Assert-True($null -ne (Field $name)) "$name is missing." }
Assert-True($null -ne ($catalog | Where-Object { $_.Kind.ToString() -eq 'XldContourSmoothing' } | Select-Object -First 1)) 'XLD contour smoothing is missing from the toolbox.'
$smoothKind = [Enum]::Parse($kindType, 'XldContourSmoothing'); $inputs = $metadataType.GetMethod('GetInputPorts', $staticPublic).Invoke($null, [object[]]@($smoothKind)); $outputs = $metadataType.GetMethod('GetOutputPorts', $staticPublic).Invoke($null, [object[]]@($smoothKind))
Assert-True(($inputs | Where-Object { $_.PortName -eq 'XLD' -and $_.DataType -eq 'XLD' -and -not $_.IsOptional }).Count -eq 1) 'Smoothing required XLD input is missing.'
foreach ($port in @('XLD','InputContourCount','ContourCount','InputTotalLength','TotalLength','LengthDelta','NumRegrPoints','AttributePolicy','Parameters','ResultCode')) { Assert-True(($outputs | Where-Object PortName -eq $port).Count -eq 1) "Smoothing output $port is missing." }
foreach ($kindName in @('XldContourFilter','XldAdjacentContourUnion','XldContourSegmentation','XldLineFitting','XldCircleFitting','XldEllipseFitting','XldRectangleFitting')) { $ports = $metadataType.GetMethod('GetInputPorts', $staticPublic).Invoke($null, [object[]]@([Enum]::Parse($kindType, $kindName))); Assert-True(($ports | Where-Object { $_.PortName -eq 'XLD' -and $_.DataType -eq 'XLD' }).Count -eq 1) "$kindName cannot consume smoothed XLD." }

$app = [Windows.Application]::Current; if ($null -eq $app) { $app = New-Object Windows.Application; $app.ShutdownMode = [Windows.ShutdownMode]::OnExplicitShutdown }
$window.WindowStartupLocation = [Windows.WindowStartupLocation]::Manual; $window.Left = 2; $window.Top = 2; $window.Width = 1760; $window.Height = 980; $window.Topmost = $true; $window.Show(); $window.Activate() | Out-Null; Pump-Ui 300

# A line: one persistent mixed-format queue, thumbnails, click/load, playback, failures, reload, clear and dual-view independence.
$paths = Get-ChildItem -LiteralPath $tempRoot -File | Where-Object Extension -in @('.bmp','.png','.jpg','.tif') | ForEach-Object FullName
try { $loadQueue.Invoke($window, [object[]]@(,[string[]]$paths)) | Out-Null } catch [Reflection.TargetInvocationException] { throw $_.Exception.InnerException }
Assert-True((Field 'imageFiles').Count -eq 31 -and (Field 'imageQueueItems').Count -eq 31) 'The directory did not become one 31-item queue.'
Assert-True((Field 'imageIndex') -eq 1) 'The corrupt first item was not skipped in favor of the first readable image.'
$corruptItem = (Field 'imageQueueItems')[0]; Assert-True($corruptItem.IsFailed -and $corruptItem.FailureMessage.Length -gt 0) 'Corrupt image failure was not retained in the queue.'
Assert-True((Field 'imageQueueThumbnailCache').Count -le 24) 'Thumbnail cache exceeded the bounded limit.'
foreach ($extension in @('.bmp','.png','.jpg','.tif')) { Assert-True(($paths | Where-Object { [IO.Path]::GetExtension($_) -eq $extension }).Count -gt 0) "Mixed format $extension fixture is missing." }
Assert-True([bool]$tryLoad.Invoke($window, [object[]]@(4,$false))) 'Direct thumbnail/index image switching failed.'
$beforeAdvance = [int](Field 'imageIndex'); Assert-True([bool]$advance.Invoke($window, [object[]]@(1,$false,$false))) 'Next-image queue advance failed.'; Assert-True([int](Field 'imageIndex') -gt $beforeAdvance) 'Queue index did not move forward.'
$setDual.Invoke($window, [object[]]@($true)) | Out-Null; Assert-True((Field 'dualImageMode')) 'Queue usage changed or blocked dual-image mode.'
$playTimer = Field 'playbackTimer'; $playTimer.Start(); $playBefore = [int](Field 'imageIndex'); $playTick.Invoke($window, [object[]]@($null,[EventArgs]::Empty)) | Out-Null; Assert-True([int](Field 'imageIndex') -ne $playBefore) 'Playback did not advance to another readable image.'; $playTimer.Stop()
$lastIndex = (Field 'imageFiles').Count - 1; Assert-True([bool]$tryLoad.Invoke($window, [object[]]@($lastIndex,$false))) 'Last queue image could not be loaded.'; $playTimer.Start(); $playTick.Invoke($window, [object[]]@($null,[EventArgs]::Empty)) | Out-Null; Assert-True(-not $playTimer.IsEnabled -and [int](Field 'imageIndex') -eq $lastIndex) 'VM-style playback did not stop at the last readable image.'
$deletedIndex = 25; $deletedPath = (Field 'imageFiles')[$deletedIndex]; Remove-Item -LiteralPath $deletedPath -Force; Assert-True(-not [bool]$tryLoad.Invoke($window, [object[]]@($deletedIndex,$false))) 'Deleted image was incorrectly reported as readable.'; Assert-True((Field 'imageQueueItems')[$deletedIndex].IsFailed) 'Deleted image failure was not retained.'
$addedPath = Join-Path $tempRoot '999_added.png'; Write-TestImage $addedPath 'png' 99; $reloadPaths = Get-ChildItem -LiteralPath $tempRoot -File | Where-Object Extension -in @('.bmp','.png','.jpg','.tif') | ForEach-Object FullName
$reloadArgs = New-Object object[] 3; $reloadArgs[0] = [string[]]$reloadPaths; $reloadArgs[1] = $tempRoot.ToString(); $reloadArgs[2] = [string](Field 'currentImagePath')
try { $loadQueueFull.Invoke($window, $reloadArgs) | Out-Null } catch [Reflection.TargetInvocationException] { throw $_.Exception.InnerException }
Assert-True(((Field 'imageFiles') -contains $addedPath) -and -not ((Field 'imageFiles') -contains $deletedPath)) 'Directory reload did not reflect added/deleted files.'
Assert-True((Field 'imageQueueThumbnailCache').Count -le 24) 'Reload exceeded the thumbnail cache limit.'
$clearQueue.Invoke($window, [object[]]@($true)) | Out-Null; Assert-True((Field 'imageFiles').Count -eq 0 -and (Field 'imageQueueItems').Count -eq 0 -and (Field 'imageQueueThumbnailCache').Count -eq 0) 'Clear queue did not release queue state and thumbnails.'

# B line: real HALCON smoothing, odd-window boundaries, empty/short/multi input, attributes, lifecycle, downstream and recipe.
$direct = New-Tool XldContourSmoothing 'S40 Direct Smoothing'; $noisy = New-NoisyLine 120
try {
    foreach ($points in @(3,5,7,9)) { $direct.Parameters.XldSmoothNumRegrPoints = $points; $record = Invoke-Smooth $direct $noisy; Assert-True($record.ResultCode -eq 'OK' -and [int](Get-Out $direct ContourCount) -eq 1 -and [int](Get-Out $direct NumRegrPoints) -eq $points) "NumRegrPoints=$points failed." }
    Assert-True((Get-Out $direct AttributePolicy).Contains('smooth_contours_xld')) 'Point-attribute policy is not exposed.'
    $old = Get-Out $direct XLD; $direct.Parameters.XldSmoothNumRegrPoints = 5; Invoke-Smooth $direct $noisy | Out-Null; Assert-True($old.IsDisposed) 'Rerun did not release the old managed XLD snapshot.'
    Assert-True([double](Get-Out $direct TotalLength) -lt [double](Get-Out $direct InputTotalLength)) 'Smoothing did not reduce the high-frequency contour path length.'
    [HalconDotNet.HTuple]$callerCount = $null; [HalconDotNet.HOperatorSet]::CountObj($noisy, [ref]$callerCount); Assert-True($callerCount.I -eq 1) 'Caller-owned input XLD was mutated or released.'
} finally { $noisy.Dispose(); $direct.Dispose() }
foreach ($invalidPoints in @(1,2,4,1000)) { $invalid = New-Tool XldContourSmoothing ('S40 Invalid ' + $invalidPoints); $input = New-NoisyLine 20; try { $invalid.Parameters.XldSmoothNumRegrPoints=$invalidPoints; $rejected=$false; try { Invoke-Smooth $invalid $input | Out-Null } catch { $rejected=$true }; Assert-True $rejected "Invalid NumRegrPoints=$invalidPoints was accepted." } finally { $input.Dispose(); $invalid.Dispose() } }
$short = New-ShortContour; $shortTool=New-Tool XldContourSmoothing 'S40 Short'; try { $shortTool.Parameters.XldSmoothNumRegrPoints=5; $shortRecord=Invoke-Smooth $shortTool $short; Assert-True($shortRecord.ResultCode-eq'OK'-and[int](Get-Out $shortTool ContourCount)-eq1) 'Short contour handling failed.' } finally { $short.Dispose();$shortTool.Dispose() }
$multiA=New-NoisyLine 30;$multiB=New-ShortContour;[HalconDotNet.HObject]$multi=$null;$multiTool=New-Tool XldContourSmoothing 'S40 Multiple';try{[HalconDotNet.HOperatorSet]::ConcatObj($multiA,$multiB,[ref]$multi);$multiTool.Parameters.XldSmoothNumRegrPoints=5;Invoke-Smooth $multiTool $multi|Out-Null;Assert-True([int](Get-Out $multiTool InputContourCount)-eq2-and[int](Get-Out $multiTool ContourCount)-eq2) 'Multiple XLD objects were not preserved as a collection.'}finally{$multiA.Dispose();$multiB.Dispose();if($null-ne$multi){$multi.Dispose()};$multiTool.Dispose()}
[HalconDotNet.HObject]$empty=$null;[HalconDotNet.HOperatorSet]::GenEmptyObj([ref]$empty);$emptyTool=New-Tool XldContourSmoothing 'S40 Empty';try{$emptyRecord=Invoke-Smooth $emptyTool $empty;Assert-True($emptyRecord.ResultCode-eq'NG'-and[int](Get-Out $emptyTool ContourCount)-eq0) 'Empty XLD semantics are wrong.'}finally{$empty.Dispose();$emptyTool.Dispose()}

$attributeImagePath = Join-Path $tempRoot 'attribute.png'; Write-TestImage $attributeImagePath 'png' 7; $attributeImage=[HalconDotNet.HImage]::new($attributeImagePath);[HalconDotNet.HObject]$attributed=$null;$attributeTool=New-Tool XldContourSmoothing 'S40 Attribute'
try { [HalconDotNet.HOperatorSet]::EdgesSubPix($attributeImage,[ref]$attributed,'canny',1.0,10.0,20.0);[HalconDotNet.HTuple]$attrs=$null;[HalconDotNet.HOperatorSet]::QueryContourAttribsXld($attributed,[ref]$attrs);Assert-True(($attrs.S -contains 'angle')) 'Attributed fixture has no angle attribute.';Invoke-Smooth $attributeTool $attributed|Out-Null;$copy=(Get-Out $attributeTool XLD).CreateXldCopy();try{[HalconDotNet.HOperatorSet]::QueryContourAttribsXld($copy,[ref]$attrs);Assert-True($attrs.Length-eq0) 'smooth_contours_xld unexpectedly retained point attributes.'}finally{$copy.Dispose()} } finally { $attributeImage.Dispose();if($null-ne$attributed){$attributed.Dispose()};$attributeTool.Dispose() }

$disposeFlow.Invoke($window, @()) | Out-Null; $flow.Clear(); $producer=New-Tool SubpixelEdgeExtraction 'S40 XLD Producer';$smooth=New-Tool XldContourSmoothing 'S40 Smooth';$line=New-Tool XldLineFitting 'S40 Fit';$judge=New-Tool NumericJudge 'S40 Judge';$smooth.SetInputBinding('XLD',$producer.ToolId,'XLD');$line.SetInputBinding('XLD',$smooth.ToolId,'XLD');$judge.SetInputBinding('Value',$smooth.ToolId,'ContourCount');$judge.InputToolId=$smooth.ToolId;$judge.InputPortName='ContourCount';$smooth.Parameters.XldSmoothNumRegrPoints=7;$line.Parameters.XldLineFitAlgorithm='regression';$judge.NumericLowerLimit=1;$judge.NumericUpperLimit=1;foreach($tool in @($producer,$smooth,$line,$judge)){$flow.Add($tool)}
$flowXld=New-NoisyLine 120;try{$producer.SetOutputValue('XLD',(New-Snapshot $flowXld $producer));$producer.SetOutputValue('ContourCount',[double]1);$producer.ResultCode='OK'}finally{$flowXld.Dispose()};Invoke-Execute $smooth 'S40 flow'|Out-Null;Invoke-Execute $line 'S40 flow'|Out-Null;Invoke-Execute $judge 'S40 flow'|Out-Null;Assert-True($smooth.ResultCode-eq'OK'-and$line.ResultCode-eq'OK'-and$judge.ResultCode-eq'OK') 'Smoothed XLD/ContourCount did not drive fitting and NumericJudge.'
$rawLine=New-NoisyLine 120;$baselineTool=New-Tool XldLineFitting 'S40 Baseline Fit';try{Invoke-Line $baselineTool $rawLine|Out-Null;$rawResidual=[double](Get-Out $baselineTool MaxResidual);$smoothedCopy=(Get-Out $smooth XLD).CreateXldCopy();try{$smoothedFit=New-Tool XldLineFitting 'S40 Smoothed Fit';try{Invoke-Line $smoothedFit $smoothedCopy|Out-Null;$smoothResidual=[double](Get-Out $smoothedFit MaxResidual);Assert-True(-not [double]::IsNaN($smoothResidual) -and -not [double]::IsInfinity($smoothResidual) -and $smoothResidual -ge 0 -and $rawResidual -ge 0) "Downstream fitting residual was not produced ($rawResidual -> $smoothResidual)."}finally{$smoothedFit.Dispose()}}finally{$smoothedCopy.Dispose()}}finally{$rawLine.Dispose();$baselineTool.Dispose()}
$recipe=$captureRecipe.Invoke($window,@());$saved=$recipe.ToolFlow|Where-Object ToolType -eq 'XldContourSmoothing'|Select-Object -First 1;Assert-True($saved.Parameters.XldSmoothNumRegrPoints-eq7) 'Smoothing parameters were not captured in the recipe.'

$mainType.GetField('recipeCloseConfirmed',$private).SetValue($window,$true);$window.Close();$app.Shutdown();if(Test-Path $tempRoot){Remove-Item -LiteralPath $tempRoot -Recurse -Force}
Write-Output 'S40 image thumbnail queue and HALCON XLD contour smoothing smoke tests passed.'
