param([string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot))
$ErrorActionPreference = 'Stop'
if ([Threading.Thread]::CurrentThread.ApartmentState -ne [Threading.ApartmentState]::STA) { throw 'Run with powershell.exe -STA.' }
$halcon = 'C:\Program Files\MVTec\HALCON-20.11-Progress'
$env:PATH = (Join-Path $halcon 'bin\x64-win64') + ';' + $env:PATH
Add-Type -AssemblyName PresentationFramework
[Reflection.Assembly]::LoadFrom((Join-Path $halcon 'bin\dotnet35\halcondotnet.dll')) | Out-Null
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path $ProjectRoot 'bin\x64\Debug\HalconWinFormsDemo.exe'))

function Assert([bool]$ok, [string]$message) { if (-not $ok) { throw "Assertion failed: $message" } }
function Image([byte]$gray = 96) { [HalconDotNet.HObject]$raw = $null; [HalconDotNet.HOperatorSet]::GenImageConst([ref]$raw, 'byte', 180, 120); $image = New-Object HalconDotNet.HImage($raw); $raw.Dispose(); $image }
function Contour([double]$length) { [HalconDotNet.HObject]$x = $null; [HalconDotNet.HOperatorSet]::GenContourPolygonXld([ref]$x, [HalconDotNet.HTuple]@([double]0,0), [HalconDotNet.HTuple]@([double]0,$length)); $x }
function ClosedContour() { [HalconDotNet.HObject]$x = $null; [HalconDotNet.HOperatorSet]::GenContourPolygonXld([ref]$x, [HalconDotNet.HTuple]@([double]0,0,10,0), [HalconDotNet.HTuple]@([double]0,10,0,0)); $x }
function MultiXld([double[]]$lengths) { [HalconDotNet.HObject]$all = $null; [HalconDotNet.HOperatorSet]::GenEmptyObj([ref]$all); foreach ($length in $lengths) { $one = Contour $length; [HalconDotNet.HObject]$combined = $null; try { [HalconDotNet.HOperatorSet]::ConcatObj($all,$one,[ref]$combined); $all.Dispose(); $all=$combined; $combined=$null } finally { $one.Dispose(); if($null-ne$combined){$combined.Dispose()} } }; $all }

$temp = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s45-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($temp) | Out-Null
$mainType = $assembly.GetType('HalconWinFormsDemo.MainWindow',$true)
$serviceType = $assembly.GetType('HalconWinFormsDemo.Services.RecipeService',$true)
$loggerType = $assembly.GetType('HalconWinFormsDemo.Services.AppLogger',$true)
$storeType = $assembly.GetType('HalconWinFormsDemo.Services.BatchTraceStore',$true)
$recordType = $assembly.GetType('HalconWinFormsDemo.Models.VmImageBatchTraceRecord',$true)
$itemType = $assembly.GetType('HalconWinFormsDemo.Models.VmImageBatchTraceItem',$true)
$layerType = $assembly.GetType('HalconWinFormsDemo.Models.VmBatchTraceLayerSnapshotData',$true)
$captureType = $assembly.GetType('HalconWinFormsDemo.Services.VmBatchTraceLayerCapture',$true)
$settingsType = $assembly.GetType('HalconWinFormsDemo.Models.VmBatchTraceStorageSettings',$true)
$kindType = $assembly.GetType('HalconWinFormsDemo.Models.VmToolKind',$true)
$metadataType = $assembly.GetType('HalconWinFormsDemo.Models.ToolMetadata',$true)
$snapshotType = $assembly.GetType('HalconWinFormsDemo.Models.VmXldSnapshot',$true)
$private = [Reflection.BindingFlags]'Instance,NonPublic'
$all = [Reflection.BindingFlags]'Instance,NonPublic,Public'
$static = [Reflection.BindingFlags]'Static,Public'
$tempPath = $temp.ToString()
$service = $serviceType.GetConstructor([Type[]]@([string])).Invoke([object[]]@($tempPath))
$logger = $loggerType.GetConstructor([Type[]]@([string])).Invoke([object[]]@($tempPath))
$window = $mainType.GetConstructor($private,$null,[Type[]]@($serviceType,$loggerType),$null).Invoke([object[]]@($service,$logger))
function Field([string]$name) { $field=$mainType.GetField($name,$all); if($null-eq$field){throw "Missing field $name"}; $field.GetValue($window) }

# A: persisted source/result/layer assets, safe paths, restart audit, corruption degradation, isolation, retention and deletion safety.
foreach($name in @('HistoricalImageContextBar','HistoricalImageTitleText','HistoricalImageMetadataText','HistoricalSourceImageButton','HistoricalResultImageButton','ReturnLiveImageButton','BatchTraceSaveImagesCheckBox','BatchTraceSaveLayersCheckBox','BatchTraceMaximumBatchCountTextBox','BatchTraceMaximumMegabytesTextBox')) { Assert($null-ne(Field $name)) "$name missing" }
$store = Field 'batchTraceStore'
$sourcePath = Join-Path $temp 'original\same.png'; [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($sourcePath))|Out-Null; [IO.File]::WriteAllText($sourcePath,'source-owner-marker')
$record = [Activator]::CreateInstance($recordType); $record.BatchId='S45-ASSET-001'; $record.RecipeName='S45 Recipe'; $record.RecipeFingerprint=('a'*64); $record.StartedAtUtc=[DateTime]::UtcNow.AddMinutes(-5).ToString('O'); $record.CompletedAtUtc=[DateTime]::UtcNow.ToString('O'); $record.Scope='All'; $record.Outcome='OK'; $record.TotalCount=2; $record.AssetsEnabled=$true
$item0 = [Activator]::CreateInstance($itemType); $item0.Sequence=0; $item0.FilePath=$sourcePath; $item0.FileName='same.png'; $item0.State='OK'; $item0.ResultCode='OK'; $item0.ElapsedMilliseconds=12.3
$item1 = [Activator]::CreateInstance($itemType); $item1.Sequence=1; $item1.FilePath=(Join-Path $temp 'duplicate\same.png'); $item1.FileName='same.png'; $item1.State='NG'; $item1.ResultCode='NG'; $item1.ElapsedMilliseconds=13.4
$record.Items = New-Object "System.Collections.Generic.List[$itemType]"; $record.Items.Add($item0); $record.Items.Add($item1)
$captures = New-Object "System.Collections.Generic.List[$captureType]"
[HalconDotNet.HObject]$region=$null; [HalconDotNet.HOperatorSet]::GenRectangle1([ref]$region,10,12,80,100)
$xld = MultiXld ([double[]]@(15,25))
foreach($pair in @(@($region,'Region','Threshold','Region','green'),@($xld,'XLD','Edges','XLD','yellow'))) { $meta=[Activator]::CreateInstance($layerType); $meta.ToolId=[Guid]::NewGuid().ToString('N'); $meta.ToolName=$pair[2]; $meta.PortName=$pair[3]; $meta.DataType=$pair[1]; $meta.Color=$pair[4]; $meta.LineWidth=2; $meta.Opacity=80; $meta.IsVisible=$true; $meta.ObjectCount=1; $capture=[Activator]::CreateInstance($captureType); $capture.Object=$pair[0]; $capture.Metadata=$meta; $captures.Add($capture) }
$sourceImage=Image; $resultImage=Image
try { $store.SaveItemAssets($record,$item0,$sourceImage,$resultImage,$captures); $emptyCaptures=New-Object "System.Collections.Generic.List[$captureType]"; $store.SaveItemAssets($record,$item1,$sourceImage,$resultImage,$emptyCaptures); $store.Save($record)|Out-Null } finally { $sourceImage.Dispose(); $resultImage.Dispose(); $region.Dispose(); $xld.Dispose() }
Assert($item0.Layers.Count-eq2-and$item1.Layers.Count-eq0) 'Multiple/empty overlay persistence mismatch'
Assert(-not[IO.Path]::IsPathRooted($item0.SourceImageRelativePath)-and-not[IO.Path]::IsPathRooted($item0.ResultImageRelativePath)) 'Asset path escaped relative-path contract'
Assert($item0.SourceImageRelativePath-ne$item1.SourceImageRelativePath) 'Duplicate filenames collided in managed storage'
$loaded=$store.LoadAll(); Assert($loaded.Records.Count-eq1-and$loaded.Records[0].Items[0].AssetState-eq'Ready') 'Restart-style load did not audit ready snapshots'
[IO.File]::Delete($sourcePath); $loaded=$store.LoadAll(); Assert($loaded.Records[0].Items[0].AssetState-eq'Ready'-and-not$loaded.Records[0].Items[0].OriginalSourceExists) 'Deleted original source made managed snapshot unavailable'
$corruptItem=$loaded.Records[0].Items[0]; $resultAsset=Join-Path $store.DirectoryPath $corruptItem.ResultImageRelativePath; [IO.File]::WriteAllText($resultAsset,'corrupt-result')
$loaded=$store.LoadAll(); $corruptRecord=$loaded.Records[0]; $corruptItem=$corruptRecord.Items[0]; Assert($corruptItem.AssetState-eq'Corrupt') 'Corrupt result image was not detected'
$liveImage=Image; $mainType.GetField('currentImage',$private).SetValue($window,$liveImage); $beforeLive=$mainType.GetField('currentImage',$private).GetValue($window); $beforeFlowCount=(Field 'flowTools').Count
$mainType.GetMethod('EnterHistoricalImageMode',$private).Invoke($window,[object[]]@($corruptRecord,$corruptItem))|Out-Null
Assert([bool]$mainType.GetField('historicalImageMode',$private).GetValue($window)) 'Corrupt result did not degrade to readable source snapshot'
Assert([object]::ReferenceEquals($beforeLive,$mainType.GetField('currentImage',$private).GetValue($window))-and(Field 'flowTools').Count-eq$beforeFlowCount) 'Historical replay mutated live image or flow'
$blocked=$mainType.GetMethod('RunInspectionRangeAsync',$private).Invoke($window,[object[]]@([Enum]::Parse($assembly.GetType('HalconWinFormsDemo.Models.VmFlowRunRequestMode',$true),'Full'),'history isolation')).Result
Assert($blocked.ResultCode-eq'READ_ONLY') 'Historical read-only mode did not block central execution'
$mainType.GetMethod('ExitHistoricalImageMode',$private).Invoke($window,[object[]]@($false))|Out-Null; Assert([object]::ReferenceEquals($beforeLive,$mainType.GetField('currentImage',$private).GetValue($window))) 'Returning live replaced live image'
$settings=[Activator]::CreateInstance($settingsType); $settings.SaveImages=$true; $settings.SaveLayers=$true; $settings.MaximumBatchCount=2; $settings.MaximumMegabytes=64; $store.SaveSettings($settings); $settings2=$store.LoadSettings(); Assert($settings2.MaximumBatchCount-eq2-and$settings2.SaveLayers) 'Storage settings did not persist'
foreach($n in 2..3){$r=[Activator]::CreateInstance($recordType);$r.BatchId="S45-ASSET-00$n";$r.RecipeName='Retention';$r.StartedAtUtc=[DateTime]::UtcNow.AddMinutes($n).ToString('O');$r.TotalCount=0;$r.Items=New-Object "System.Collections.Generic.List[$itemType]";$store.Save($r)|Out-Null};$store.EnforceRetention($settings)|Out-Null; Assert($store.LoadAll().Records.Count-eq2) 'Batch-count retention failed'
$orphan=Join-Path $store.DirectoryPath 'assets\ORPHAN';[IO.Directory]::CreateDirectory($orphan)|Out-Null;[IO.File]::WriteAllText((Join-Path $orphan 'x.bin'),'x');Assert($store.CleanupOrphanAssets()-eq1) 'Orphan cleanup failed'
$retained=$store.LoadAll().Records[0];$sourceOwner=Join-Path $temp 'owner-do-not-delete.txt';[IO.File]::WriteAllText($sourceOwner,'keep');$retained.Items.Add([Activator]::CreateInstance($itemType));$retained.Items[$retained.Items.Count-1].FilePath=$sourceOwner;$store.DeleteBatch($retained);Assert([IO.File]::Exists($sourceOwner)) 'Batch deletion removed an external source file'

# B: typed port, stable sort, Top/Bottom/range selection, errors, managed lifetime, recipe and downstream.
$sortKind=[Enum]::Parse($kindType,'XldLengthSortSelection');$inputs=$metadataType.GetMethod('GetInputPorts',$static).Invoke($null,[object[]]@($sortKind));$outputs=$metadataType.GetMethod('GetOutputPorts',$static).Invoke($null,[object[]]@($sortKind));Assert(($inputs|Where-Object{$_.PortName-eq'XLD'-and$_.DataType-eq'XLD'}).Count-eq1) 'Typed XLD input missing';Assert(($outputs|Where-Object{$_.PortName-eq'OriginalIndices'-and$_.DataType-eq'String'}).Count-eq1) 'Original indices must remain an explicit String output'
$create=$mainType.GetMethod('CreateFlowTool',$private);$runSort=$mainType.GetMethod('RunXldLengthSortSelectionTool',$private);$execute=$mainType.GetMethod('ExecuteFlowTool',$private);$disposeFlow=$mainType.GetMethod('DisposeFlowTools',$private);$captureRecipe=$mainType.GetMethod('CaptureRecipe',$private);$flow=$mainType.GetField('flowTools',$private).GetValue($window)
function Tool([string]$kind,[string]$name){$create.Invoke($window,[object[]]@([Enum]::Parse($kindType,$kind),$name,$true,$null))}
function RunSort($tool,$xldInput){try{$runSort.Invoke($window,[object[]]@($tool,$xldInput))}catch [Reflection.TargetInvocationException]{throw $_.Exception.InnerException}}
function Out($tool,[string]$port){$value=$null;if(-not$tool.TryGetOutputValue($port,[ref]$value)){throw "Missing output $port"};$value}
$sort=Tool XldLengthSortSelection 'S45 Sort';$input=MultiXld ([double[]]@(5,10,10,20,30));try{$sort.Parameters.XldLengthSortDirection='Ascending';$sort.Parameters.XldLengthSelectionMode='TopN';$sort.Parameters.XldLengthStartRank=3;$sort.Parameters.XldLengthEndRank=3;$result=RunSort $sort $input;Assert($result.ResultCode-eq'OK'-and(Out $sort OriginalIndices)-eq'[1,2,3]') 'Ascending Top-N or equal-length stable order failed';$old=Out $sort XLD;$sort.Parameters.XldLengthSortDirection='Descending';$sort.Parameters.XldLengthSelectionMode='TopN';$sort.Parameters.XldLengthStartRank=4;RunSort $sort $input|Out-Null;Assert((Out $sort OriginalIndices)-eq'[5,4,2,3]') 'Descending stable order failed';Assert($old.IsDisposed) 'Prior managed selected XLD was not released';$sort.Parameters.XldLengthSortDirection='Ascending';$sort.Parameters.XldLengthSelectionMode='BottomN';$sort.Parameters.XldLengthStartRank=2;RunSort $sort $input|Out-Null;Assert((Out $sort OriginalIndices)-eq'[4,5]') 'Bottom-N selection failed';$sort.Parameters.XldLengthSelectionMode='RankRange';$sort.Parameters.XldLengthStartRank=2;$sort.Parameters.XldLengthEndRank=4;RunSort $sort $input|Out-Null;Assert((Out $sort OriginalIndices)-eq'[2,3,4]'-and[int](Out $sort ContourCount)-eq3-and[Math]::Abs([double](Out $sort MeanLength)-13.333333)-lt.001) 'Inclusive rank range/statistics failed';[HalconDotNet.HTuple]$count=$null;[HalconDotNet.HOperatorSet]::CountObj($input,[ref]$count);Assert($count.I-eq5) 'Input XLD ownership changed';$sort.Parameters.XldLengthStartRank=2;$sort.Parameters.XldLengthEndRank=6;$rejected=$false;try{RunSort $sort $input|Out-Null}catch{$rejected=$true};Assert $rejected 'Out-of-input rank was silently clamped'}finally{$input.Dispose()}
$sort.Parameters.XldLengthSelectionMode='RankRange';$sort.Parameters.XldLengthStartRank=4;$sort.Parameters.XldLengthEndRank=2;Assert(-not[string]::IsNullOrWhiteSpace($sort.Parameters.Validate($sortKind))) 'Reverse rank range was accepted'
[HalconDotNet.HObject]$empty=$null;[HalconDotNet.HOperatorSet]::GenEmptyObj([ref]$empty);try{$sort.Parameters.XldLengthSelectionMode='TopN';$sort.Parameters.XldLengthStartRank=1;$sort.Parameters.XldLengthEndRank=1;$emptyResult=RunSort $sort $empty;Assert($emptyResult.ResultCode-eq'NG'-and[int](Out $sort ContourCount)-eq0) 'Empty XLD semantics failed'}finally{$empty.Dispose()}
$closed=ClosedContour;try{$sort.Parameters.XldLengthStartRank=1;Assert((RunSort $sort $closed).ResultCode-eq'OK') 'Closed contour was rejected'}finally{$closed.Dispose()}
$disposeFlow.Invoke($window,@())|Out-Null;$flow.Clear();$producer=Tool SubpixelEdgeExtraction 'Producer';$selection=Tool XldLengthSortSelection 'Length Pick';$fit=Tool XldLineFitting 'Fit Pick';$judge=Tool NumericJudge 'Judge Count';$selection.Parameters.XldLengthSortDirection='Descending';$selection.Parameters.XldLengthSelectionMode='TopN';$selection.Parameters.XldLengthStartRank=1;$selection.Parameters.XldLengthEndRank=1;$selection.SetInputBinding('XLD',$producer.ToolId,'XLD');$fit.SetInputBinding('XLD',$selection.ToolId,'XLD');$judge.SetInputBinding('Value',$selection.ToolId,'ContourCount');$judge.InputToolId=$selection.ToolId;$judge.InputPortName='ContourCount';$judge.NumericLowerLimit=1;$judge.NumericUpperLimit=1;foreach($tool in @($producer,$selection,$fit,$judge)){$flow.Add($tool)}
$source=MultiXld ([double[]]@(20,40));try{$producer.SetOutputValue('XLD',$snapshotType.GetMethod('Create',$static).Invoke($null,[object[]]@($source,$producer.ToolId,$producer.InstanceName,'XLD')));$producer.ResultCode='OK'}finally{$source.Dispose()};foreach($tool in @($selection,$fit,$judge)){$execute.Invoke($window,[object[]]@($tool,'S45 chain'))|Out-Null};Assert($selection.ResultCode-eq'OK'-and$fit.ResultCode-eq'OK'-and$judge.ResultCode-eq'OK') 'XLD/Number downstream chain failed'
(Field 'FlowToolList').SelectedItem=$selection;$mainType.GetMethod('LoadDockConfigurationDraft',$private).Invoke($window,[object[]]@($selection))|Out-Null;Assert((Field 'DockXldLengthSortSelectionPanel').Visibility.ToString()-eq'Visible') 'Dock editor did not synchronize';$recipe=$captureRecipe.Invoke($window,@());$saved=$recipe.ToolFlow|Where-Object ToolType -eq 'XldLengthSortSelection'|Select-Object -First 1;Assert($saved.Parameters.XldLengthStartRank-eq1-and$saved.Parameters.XldLengthSortDirection-eq'Descending') 'Sort parameters were not persisted in recipe'
$upstream=Out $producer XLD;$upstream.Dispose();$failed=$false;try{$execute.Invoke($window,[object[]]@($selection,'released upstream'))|Out-Null}catch{$failed=$true};Assert $failed 'Released upstream XLD was accepted'

$mainType.GetField('recipeCloseConfirmed',$private).SetValue($window,$true);$window.Close();if(Test-Path $temp){Remove-Item -LiteralPath $temp -Recurse -Force}
Write-Output 'S45 historical result replay and HALCON XLD length sort/index selection smoke tests passed.'
