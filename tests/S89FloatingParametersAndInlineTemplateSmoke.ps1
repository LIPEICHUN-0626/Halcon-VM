param([string]$ProjectRoot=(Split-Path -Parent $PSScriptRoot), [switch]$KeepOpen)
$ErrorActionPreference='Stop'
if([Threading.Thread]::CurrentThread.ApartmentState -ne 'STA'){throw 'Run with powershell.exe -STA'}
$halcon='C:\Program Files\MVTec\HALCON-20.11-Progress'
$env:PATH=(Join-Path $halcon 'bin\x64-win64')+';'+$env:PATH
Add-Type -AssemblyName PresentationFramework
Add-Type -AssemblyName System.Runtime.Serialization
[Reflection.Assembly]::LoadFrom((Join-Path $halcon 'bin\dotnet35\halcondotnet.dll'))|Out-Null
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $ProjectRoot 'bin\x64\Debug\HalconWinFormsDemo.exe'))
$main=$assembly.GetType('HalconWinFormsDemo.MainWindow',$true)
$private=[Reflection.BindingFlags]'Instance,NonPublic';$all=[Reflection.BindingFlags]'Instance,NonPublic,Public'
$temp=Join-Path ([IO.Path]::GetTempPath()) ('halcon-s89-'+[Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($temp)|Out-Null
$serviceType=$assembly.GetType('HalconWinFormsDemo.Services.RecipeService',$true)
$loggerType=$assembly.GetType('HalconWinFormsDemo.Services.AppLogger',$true)
$service=$serviceType.GetConstructor([Type[]]@([string])).Invoke([object[]]@($temp.ToString()))
$logger=$loggerType.GetConstructor([Type[]]@([string])).Invoke([object[]]@($temp.ToString()))
$app=New-Object Windows.Application;$app.ShutdownMode=[Windows.ShutdownMode]::OnExplicitShutdown
$window=$main.GetConstructor($private,$null,[Type[]]@($serviceType,$loggerType),$null).Invoke([object[]]@($service,$logger))
function Assert([bool]$ok,[string]$message){if(-not $ok){throw $message}}
function F([string]$name){$field=$main.GetField($name,$all);if($null-eq$field){throw "Missing field: $name"};Write-Output -NoEnumerate ($field.GetValue($window))}
function SetF([string]$name,$value){$main.GetField($name,$all).SetValue($window,$value)}
function M([string]$name,[object[]]$arguments=@()){
    $method=$main.GetMethods($private)|Where-Object{$_.Name-eq$name-and$_.GetParameters().Count-eq$arguments.Count}|Select-Object -First 1
    $actual=@($arguments|ForEach-Object{if($null-eq$_){$null}else{$_.PSObject.BaseObject}})
    try{Write-Output -NoEnumerate ($method.Invoke($window,$actual))}catch [Reflection.TargetInvocationException]{throw $_.Exception.InnerException}
}
function Tool([string]$kind,[string]$name){M CreateFlowTool @([Enum]::Parse($assembly.GetType('HalconWinFormsDemo.Models.VmToolKind'),$kind),$name,$true,$null)}
function Pump([int]$ms){$frame=New-Object Windows.Threading.DispatcherFrame;$timer=New-Object Windows.Threading.DispatcherTimer;$timer.Interval=[TimeSpan]::FromMilliseconds($ms);$timer.Add_Tick({$timer.Stop();$frame.Continue=$false});$timer.Start();[Windows.Threading.Dispatcher]::PushFrame($frame)}
function Out($tool,[string]$name){$value=$null;Assert($tool.TryGetOutputValue($name,[ref]$value)) ('Missing output '+$name);Write-Output -NoEnumerate $value}
function Shot($target,[string]$name){
    $path=Join-Path $ProjectRoot ('artifacts\s89\'+$name+'.png');[IO.Directory]::CreateDirectory((Split-Path $path))|Out-Null
    $target.UpdateLayout();$render=[Windows.Media.Imaging.RenderTargetBitmap]::new([int][Math]::Ceiling($target.ActualWidth),[int][Math]::Ceiling($target.ActualHeight),96,96,[Windows.Media.PixelFormats]::Pbgra32)
    $render.Render($target);$encoder=New-Object Windows.Media.Imaging.PngBitmapEncoder;$encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($render))
    $stream=[IO.File]::Open($path,[IO.FileMode]::Create);try{$encoder.Save($stream)}finally{$stream.Dispose()}
}
try{
    # Real HALCON synthetic L target; image source is a real file-backed flow module.
    [HalconDotNet.HObject]$base=$null;[HalconDotNet.HObject]$vertical=$null;[HalconDotNet.HObject]$horizontal=$null;[HalconDotNet.HObject]$region=$null;[HalconDotNet.HObject]$paint=$null
    $imagePath=Join-Path $temp 'target.png';$rgbPath=Join-Path $temp 'target-rgb.png';$translatedPath=Join-Path $temp 'target-translated.png'
    try{
        [HalconDotNet.HOperatorSet]::GenImageConst([ref]$base,'byte',320,240)
        [HalconDotNet.HOperatorSet]::GenRectangle1([ref]$vertical,40,45,75,54)
        [HalconDotNet.HOperatorSet]::GenRectangle1([ref]$horizontal,66,45,75,85)
        [HalconDotNet.HOperatorSet]::Union2($vertical,$horizontal,[ref]$region)
        [HalconDotNet.HOperatorSet]::PaintRegion($region,$base,[ref]$paint,255,'fill')
        [HalconDotNet.HOperatorSet]::WriteImage($paint,'png',0,$imagePath)
        [HalconDotNet.HObject]$movedRegion=$null;[HalconDotNet.HObject]$movedImage=$null
        try{
            [HalconDotNet.HOperatorSet]::MoveRegion($region,[ref]$movedRegion,100,160)
            [HalconDotNet.HOperatorSet]::PaintRegion($movedRegion,$base,[ref]$movedImage,255,'fill')
            [HalconDotNet.HOperatorSet]::WriteImage($movedImage,'png',0,$translatedPath)
        }finally{foreach($o in @($movedImage,$movedRegion)){if($null-ne$o){$o.Dispose()}}}
        [HalconDotNet.HObject]$rgb=$null
        try{[HalconDotNet.HOperatorSet]::Compose3($paint,$paint,$paint,[ref]$rgb);[HalconDotNet.HOperatorSet]::WriteImage($rgb,'png',0,$rgbPath)}finally{if($null-ne$rgb){$rgb.Dispose()}}
    }finally{foreach($o in @($paint,$region,$horizontal,$vertical,$base)){if($null-ne$o){$o.Dispose()}}}
    $window.Title='S89 自动化验证';$window.WindowState=[Windows.WindowState]::Normal;$window.ShowActivated=$false;$window.Left=0;$window.Top=0;$window.Width=1366;$window.Height=768;$window.Show();Pump 250
    M DisposeFlowTools|Out-Null;$flow=F flowTools;$flow.Clear()
    $source=Tool ImageSource '图像源';$shape=Tool ShapeMatch '模板匹配';$judge=Tool NumericJudge '匹配分数判定'
    foreach($tool in @($source,$shape,$judge)){$flow.Add($tool)}
    $source.Parameters.LocalImagePath=$imagePath
    $shape.SetInputBinding('Image',$source.ToolId,'Image')
    $shape.Parameters.ShapeMinScore=.45;$shape.Parameters.ShapeAngleStartDegrees=-10;$shape.Parameters.ShapeAngleExtentDegrees=20
    $shape.Parameters.ShapeScaleMin=.9;$shape.Parameters.ShapeScaleMax=1.1
    M RefreshFlowGraphLayout|Out-Null;(F FlowToolList).SelectedItem=$shape;Pump 150
    $zoom=(F FlowZoomSlider).Value;$width=(F FlowGraphViewport).ActualWidth;$columns=@((F MainWorkspaceGrid).ColumnDefinitions|ForEach-Object{$_.Width.ToString()})
    M OpenCentralModuleConfigurationS77 @($shape)|Out-Null;Pump 150
    $popup=F moduleParameterWindowS89
    Assert($popup.IsVisible -and $popup.Width-le560) 'Compact parameter window missing'
    Assert((F FlowZoomSlider).Value-eq$zoom-and[Math]::Abs((F FlowGraphViewport).ActualWidth-$width)-lt.01) 'Opening parameters changed flow width/zoom'
    Assert(($columns-join',')-eq(@((F MainWorkspaceGrid).ColumnDefinitions|ForEach-Object{$_.Width.ToString()})-join',')) 'Column layout changed'
    Assert((F ToolCatalogPanel).Visibility-eq[Windows.Visibility]::Visible) 'Toolbox disappeared'
    (F FlowToolList).SelectedItem=$source;Pump 50
    Assert([object]::ReferenceEquals((F dockEditingTool),$shape)) 'Single selection silently switched parameter target'
    M ShapeReadInputButton_ClickS89 @($null,$null)|Out-Null
    Assert($null-ne(F ShapeTrainingImageS89).Source) ('Input preview missing: '+(F ShapeTrainingStatusS89).Text)
    Assert($source.ResultCode-eq'OK'-and$judge.ResultCode-eq'--') 'Read input unexpectedly ran full flow'
    M SetImageContextSelection @('ModuleInput')|Out-Null
    M RefreshDualImageInputOptions @($source)|Out-Null
    $context=M ResolveDualInputImageContext
    Assert($context.HasImage-and(M GetImageTaskToolS89).ToolId-eq$shape.ToolId) 'Image task followed the selected source instead of the edited matcher'
    Assert((F DualInputPortComboBox).Items.Count-ge1) 'Pinned matcher lost its Image input selector'
    SetF shapeTrainingDraftS89 ([Windows.Rect]::new(38,34,54,48));M DrawShapeTrainingRectangleS89|Out-Null
    M OpenCentralModuleConfigurationS77 @($shape)|Out-Null
    Assert($null-ne(F shapeTrainingDraftS89)) 'Reopening the same module discarded its unfinished template rectangle'
    Shot $window 'flow-unchanged-1366x768';Shot $popup 'template-parameter-window'
    M TrainShapeFromPreviewS89|Out-Null
    Assert((Test-Path $shape.Parameters.ShapeModelPath)) 'Inline training did not save a real model'
    Assert($shape.BoundRoiIds.Count-eq0) 'Training region was incorrectly bound as search restriction'
    $record=M ExecuteFlowTool @($shape,'S89 full-image match')
    Assert($record.ResultCode-eq'OK'-and[double](Out $shape Score)-ge.45) ('Full-image matching failed: '+$shape.ErrorMessage)
    $score=[double](Out $shape Score);$pose=Out $shape Pose2D
    Assert($null-ne$pose-and(Out $shape XLD).ObjectCount-ge1) 'Match did not publish real pose/XLD'
    $match=(F shapeToolMatches)[$shape.ToolId][0]
    $picked=M TrySelectShapeCandidateAt @([Drawing.PointF]::new([single]$match.Column,[single]$match.Row),[double]5)
    Assert($picked-and(F selectedShapeCandidateIndex)-eq0) 'Image candidate selection followed another flow node instead of the edited matcher'
    # The search must find a target wholly outside the training rectangle.
    $source.Parameters.LocalImagePath=$translatedPath
    M ExecuteFlowTool @($source,'S89 translated input')|Out-Null
    M ExecuteFlowTool @($shape,'S89 translated full-image match')|Out-Null
    Assert($shape.ResultCode-eq'OK') ('Default search did not find translated target: '+$shape.ErrorMessage)
    $translated=(F shapeToolMatches)[$shape.ToolId][0]
    $rowShift=$translated.Row-$match.Row;$columnShift=$translated.Column-$match.Column
    Assert([Math]::Abs($rowShift-100)-lt.5-and[Math]::Abs($columnShift-160)-lt.5) 'Full-image match reported the wrong translated position'
    $source.Parameters.LocalImagePath=$imagePath
    M ExecuteFlowTool @($source,'S89 restore original input')|Out-Null
    M ExecuteFlowTool @($shape,'S89 restore original match')|Out-Null
    # Keyboard dispatch stays pinned to the editing module, not the selected source.
    $sourceVersion=$source.RuntimeOutputVersion;$judgeVersion=$judge.RuntimeOutputVersion
    $event=[Windows.Input.KeyEventArgs]::new([Windows.Input.Keyboard]::PrimaryDevice,[Windows.PresentationSource]::FromVisual($popup),0,[Windows.Input.Key]::F5)
    $event.RoutedEvent=[Windows.Input.Keyboard]::PreviewKeyDownEvent
    M ModuleParameterWindow_PreviewKeyDownS89 @($popup,$event)|Out-Null
    Assert($event.Handled-and$shape.ResultCode-eq'OK'-and$source.RuntimeOutputVersion-eq$sourceVersion-and$judge.RuntimeOutputVersion-eq$judgeVersion) 'Parameter-window F5 escaped to another module or flow'
    $breakpoint=$source.IsBreakpoint
    $event=[Windows.Input.KeyEventArgs]::new([Windows.Input.Keyboard]::PrimaryDevice,[Windows.PresentationSource]::FromVisual($popup),0,[Windows.Input.Key]::F9)
    $event.RoutedEvent=[Windows.Input.Keyboard]::PreviewKeyDownEvent
    M ModuleParameterWindow_PreviewKeyDownS89 @($popup,$event)|Out-Null
    Assert($source.IsBreakpoint-eq$breakpoint) 'Parameter-window F9 modified the selected flow module'
    $judge.SetInputBinding('Value',$shape.ToolId,'Score');$judge.InputToolId=$shape.ToolId;$judge.InputPortName='Score';$judge.NumericLowerLimit=.4;$judge.NumericUpperLimit=1
    M ExecuteFlowTool @($judge,'S89 score judge')|Out-Null;Assert($judge.ResultCode-eq'OK') 'Match score did not drive downstream judge'
    $recipe=M CaptureRecipe;$saved=@($recipe.ToolFlow|Where-Object ToolId -eq $shape.ToolId)[0]
    Assert($saved.Parameters.ShapeModelPath-eq$shape.Parameters.ShapeModelPath-and$saved.InputBindings.Count-ge1) 'Recipe lost template/input'
    $serializer=[Runtime.Serialization.Json.DataContractJsonSerializer]::new($recipe.GetType());$stream=New-Object IO.MemoryStream
    try{$serializer.WriteObject($stream,$recipe);$stream.Position=0;$restored=$serializer.ReadObject($stream)}finally{$stream.Dispose()}
    $restoredShape=@($restored.ToolFlow|Where-Object ToolId -eq $shape.ToolId)[0]
    Assert($restoredShape.Parameters.ShapeTrainingRoiId-eq$shape.Parameters.ShapeTrainingRoiId-and$restoredShape.Parameters.ShapeModelPath-eq$shape.Parameters.ShapeModelPath) 'JSON roundtrip lost model or training region'
    $clonedParameters=$shape.Parameters.Clone();Assert($clonedParameters.ShapeModelPath-eq$shape.Parameters.ShapeModelPath) 'Parameter clone lost model reference'
    $firstModelPath=$shape.Parameters.ShapeModelPath;$firstModelHash=(Get-FileHash -LiteralPath $firstModelPath).Hash
    M RefreshShapeTrainingPreviewS89 @($false)|Out-Null;SetF shapeTrainingDraftS89 ([Windows.Rect]::new(37,33,56,50))
    M TrainShapeFromPreviewS89|Out-Null
    Assert($shape.Parameters.ShapeModelPath-ne$firstModelPath-and(Get-FileHash -LiteralPath $firstModelPath).Hash-eq$firstModelHash) 'Retraining overwrote the previous model version'
    $secondModelPath=$shape.Parameters.ShapeModelPath;$shapeId=$shape.ToolId
    M UndoFlowEditButton_Click @($null,$null)|Out-Null
    $shape=@($flow|Where-Object ToolId -eq $shapeId)[0]
    Assert($shape.Parameters.ShapeModelPath-eq$firstModelPath-and(F dockEditingTool).ToolId-eq$shapeId) 'Undo lost model version or switched the pinned editor'
    M RedoFlowEditButton_Click @($null,$null)|Out-Null
    $shape=@($flow|Where-Object ToolId -eq $shapeId)[0];$source=@($flow|Where-Object Kind -eq ImageSource)[0]
    Assert($shape.Parameters.ShapeModelPath-eq$secondModelPath-and(F dockEditingTool).ToolId-eq$shapeId) 'Redo lost model version or switched the pinned editor'
    $source.Parameters.LocalImagePath=$rgbPath
    M ShapeReadInputButton_ClickS89 @($null,$null)|Out-Null;SetF shapeTrainingDraftS89 ([Windows.Rect]::new(38,34,54,48))
    M TrainShapeFromPreviewS89|Out-Null;M ExecuteFlowTool @($shape,'S89 RGB match')|Out-Null
    Assert($shape.ResultCode-eq'OK') ('RGB training/match failed: '+$shape.ErrorMessage)
    $oldPath=$shape.Parameters.ShapeModelPath;$oldHash=(Get-FileHash -LiteralPath $oldPath).Hash
    M RefreshShapeTrainingPreviewS89 @($false)|Out-Null;SetF shapeTrainingDraftS89 ([Windows.Rect]::new(180,150,40,40))
    $rejected=$false;try{M TrainShapeFromPreviewS89|Out-Null}catch{$rejected=$true}
    Assert($rejected-and$shape.Parameters.ShapeModelPath-eq$oldPath-and(Get-FileHash -LiteralPath $oldPath).Hash-eq$oldHash) 'Failed training replaced the previous model'
    M ExecuteFlowTool @($shape,'S89 previous model survives')|Out-Null;Assert($shape.ResultCode-eq'OK') 'Failed training damaged the active model resource'
    SetF shapeTrainingDraftS89 $null
    M DockShapeTrainButton_Click @($null,$null)|Out-Null
    Assert($shape.Parameters.ShapeModelPath-ne$oldPath-and(Get-FileHash -LiteralPath $oldPath).Hash-eq$oldHash) 'Existing-ROI training overwrote the previous model'
    # Reject stale input rather than applying coordinates to a different image generation.
    M RefreshShapeTrainingPreviewS89 @($false)|Out-Null;SetF shapeTrainingDraftS89 ([Windows.Rect]::new(38,34,54,48))
    M ExecuteFlowTool @($source,'S89 input changed')|Out-Null;$oldPath=$shape.Parameters.ShapeModelPath
    $rejected=$false;try{M TrainShapeFromPreviewS89|Out-Null}catch{$rejected=$_.Exception.Message-match'重新读取'}
    Assert($rejected-and$shape.Parameters.ShapeModelPath-eq$oldPath) 'Stale preview overwrote the existing model'
    M RefreshShapeTrainingPreviewS89 @($false)|Out-Null;SetF shapeTrainingDraftS89 ([Windows.Rect]::new(1,1,2,2))
    $rejected=$false;try{M TrainShapeFromPreviewS89|Out-Null}catch{$rejected=$_.Exception.Message-match'太小'};Assert($rejected) 'Tiny training region accepted'
    M RefreshShapeTrainingPreviewS89 @($false)|Out-Null;SetF shapeTrainingDraftS89 ([Windows.Rect]::new(38,34,54,48))
    $oldPath=$shape.Parameters.ShapeModelPath;$source.ClearRuntimeOutputs()
    $rejected=$false;try{M TrainShapeFromPreviewS89|Out-Null}catch{$rejected=$true}
    Assert($rejected-and$shape.Parameters.ShapeModelPath-eq$oldPath) 'Released upstream image was accepted for training'
    SetF shapeTrainingDraftS89 $null
    M CloseCentralModuleConfigurationS77|Out-Null;Pump 80
    Assert($null-eq(F moduleParameterWindowS89)-and(F FlowZoomSlider).Value-eq$zoom) 'Parameter window cleanup changed flow'
    Assert((M GetImageTaskToolS89).ToolId-eq$source.ToolId) 'Closing parameters did not restore selected-node image context'
    $window.Width=1938;$window.Height=1158;Pump 100;$largeWidth=(F FlowGraphViewport).ActualWidth
    M OpenCentralModuleConfigurationS77 @($shape)|Out-Null;Pump 100
    Assert([Math]::Abs((F FlowGraphViewport).ActualWidth-$largeWidth)-lt.01) 'Large-layout flow changed'
    Shot $window 'flow-unchanged-1938x1158'
    (F moduleParameterWindowS89).Close();Pump 100
    Assert($null-eq(F moduleParameterWindowS89)-and$null-eq(F ShapeTrainingImageS89).Source) 'Native window close left editor or preview alive'
    M OpenCentralModuleConfigurationS77 @($shape)|Out-Null
    M ApplyRecipe @($restored)|Out-Null;Pump 100
    Assert($null-eq(F moduleParameterWindowS89)-and$null-eq(F ShapeTrainingImageS89).Source) 'Recipe switch left old editor or preview alive'
    Write-Output "S89 PASS: unchanged flow at two sizes; pinned editor/image/candidate; inline HALCON template; Score=$score; full-image shift=$rowShift/$columnShift; Pose/XLD; downstream judge; JSON recipe; model undo/redo; RGB; failed-model preservation; versioned ROI retraining; released/stale/tiny rejection; title close; recipe switch."
    if($KeepOpen){Write-Output 'READY_FOR_UI';for($i=0;$i-lt120;$i++){Pump 1000}}
}finally{
    SetF dockDraftDirty $false;M CloseCentralModuleConfigurationS77|Out-Null;SetF recipeCloseConfirmed $true;$window.Close();$app.Shutdown()
    $resolved=[IO.Path]::GetFullPath($temp);$parent=[IO.Path]::GetFullPath([IO.Path]::GetTempPath())
    if($resolved.StartsWith($parent,[StringComparison]::OrdinalIgnoreCase)-and[IO.Path]::GetFileName($resolved).StartsWith('halcon-s89-')){Remove-Item -LiteralPath $resolved -Recurse -Force}
}
