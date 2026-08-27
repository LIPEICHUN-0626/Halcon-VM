param([string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot))

$ErrorActionPreference = 'Stop'
if ([Threading.Thread]::CurrentThread.ApartmentState -ne [Threading.ApartmentState]::STA) { throw 'Run with powershell.exe -STA.' }
$halcon = 'C:\Program Files\MVTec\HALCON-20.11-Progress'
$env:PATH = (Join-Path $halcon 'bin\x64-win64') + ';' + $env:PATH
Add-Type -AssemblyName PresentationFramework
[Reflection.Assembly]::LoadFrom((Join-Path $halcon 'bin\dotnet35\halcondotnet.dll')) | Out-Null
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path $ProjectRoot 'bin\x64\Debug\HalconWinFormsDemo.exe'))

function Assert([bool]$Condition,[string]$Message){if(-not $Condition){throw $Message}}
function Pump([int]$Milliseconds){$frame=New-Object Windows.Threading.DispatcherFrame;$timer=New-Object Windows.Threading.DispatcherTimer;$timer.Interval=[TimeSpan]::FromMilliseconds($Milliseconds);$timer.Add_Tick({$timer.Stop();$frame.Continue=$false});$timer.Start();[Windows.Threading.Dispatcher]::PushFrame($frame)}

$main=$assembly.GetType('HalconWinFormsDemo.MainWindow',$true)
$serviceType=$assembly.GetType('HalconWinFormsDemo.Services.RecipeService',$true)
$loggerType=$assembly.GetType('HalconWinFormsDemo.Services.AppLogger',$true)
$kindType=$assembly.GetType('HalconWinFormsDemo.Models.VmToolKind',$true)
$pointType=$assembly.GetType('HalconWinFormsDemo.Models.VmPoint2D',$true)
$lineType=$assembly.GetType('HalconWinFormsDemo.Models.VmLine2D',$true)
$private=[Reflection.BindingFlags]'Instance,NonPublic';$all=[Reflection.BindingFlags]'Instance,NonPublic,Public';$static=[Reflection.BindingFlags]'Static,Public'
$temp=Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s70-'+[Guid]::NewGuid().ToString('N'));[IO.Directory]::CreateDirectory($temp)|Out-Null
$service=$serviceType.GetConstructor([Type[]]@([string])).Invoke([object[]]@($temp.ToString()))
$logger=$loggerType.GetConstructor([Type[]]@([string])).Invoke([object[]]@($temp.ToString()))
$window=$main.GetConstructor($private,$null,[Type[]]@($serviceType,$loggerType),$null).Invoke([object[]]@($service,$logger))
$app=[Windows.Application]::Current;$ownsApp=$null-eq$app;if($ownsApp){$app=New-Object Windows.Application;$app.ShutdownMode=[Windows.ShutdownMode]::OnExplicitShutdown}
$window.ShowInTaskbar=$false;$window.Left=-10000;$window.Show();Pump 180

function Field([string]$Name){$value=$main.GetField($Name,$all).GetValue($window);Write-Output -NoEnumerate $value}
function Method([string]$Name,[object[]]$Parameters=@()){try{$main.GetMethod($Name,$private).Invoke($window,$Parameters)}catch{$error=$_.Exception;while($error.InnerException){$error=$error.InnerException};throw($Name+': '+$error.Message)}}
function Kind([string]$Name){[Enum]::Parse($kindType,$Name)}
function Tool([string]$KindName,[string]$Name){Method 'CreateFlowTool' ([object[]]@((Kind $KindName),$Name,$true,$null))}
function Run($Tool){Method 'ExecuteFlowTool' ([object[]]@($Tool,'S70 smoke'))}
function Output($Tool,[string]$Port){$args=[object[]]@($Port,$null);if(-not $Tool.GetType().GetMethod('TryGetOutputValue').Invoke($Tool,$args)){throw "Missing output $Port"};$args[1]}
function Point([double]$Row,[double]$Column,[string]$Context){$pointType.GetMethod('CreateImagePixel',$static).Invoke($null,[object[]]@($Row,$Column,$Context))}
function SelectTools([object[]]$Tools){(Field 'FlowToolList').SelectedItems.Clear();foreach($tool in $Tools){(Field 'FlowToolList').SelectedItems.Add($tool)}}

try {
    Assert ((Field 'toolCatalog').Count -eq [Enum]::GetValues($kindType).Count -and (Field 'toolCatalog').Count -eq 70) 'S70 compatibility catalog is not the full implementation after S75.'
    Assert ((Field 'DeveloperToolbarPanel').Children.Count -eq 7) 'S70 added a permanent top toolbar command.'
    Assert ([object]::ReferenceEquals((Field 'OpenFlowSnippetLibraryButton').Parent,(Field 'OpenConnectionIssuesButton').Parent)) 'Flow snippet entry is not inside the existing flow More context.'
    Assert ((Field 'FlowSnippetLibraryPanel').Visibility.ToString()-eq'Collapsed') 'Flow snippet panel is visible on the default first screen.'

    $flow=Field 'flowTools';Method 'DisposeFlowTools'|Out-Null;$flow.Clear()
    $sourceA=Tool 'PointSetGeometryStatistics' 'Endpoint A';$sourceB=Tool 'PointSetGeometryStatistics' 'Endpoint B';$probe=Tool 'PointSetGeometryStatistics' 'Probe Point'
    $pair=Tool 'PointPairLineConstructionMeasurement' 'Construct Main Line';$distance=Tool 'PointToLineDistanceMeasurement' 'Probe To Constructed Line';$judge=Tool 'NumericJudge' 'Constructed Distance Judge'
    foreach($tool in @($sourceA,$sourceB,$probe,$pair,$distance,$judge)){$flow.Add($tool)}
    $pair.SetInputBinding('PointA',$sourceA.ToolId,'Centroid');$pair.SetInputBinding('PointB',$sourceB.ToolId,'Centroid')
    $distance.SetInputBinding('Point',$probe.ToolId,'Centroid');$distance.SetInputBinding('Line',$pair.ToolId,'MainLine')
    $judge.SetInputBinding('Value',$pair.ToolId,'Distance');$judge.InputToolId=$pair.ToolId;$judge.InputPortName='Distance';$judge.NumericOperator='BetweenInclusive';$judge.NumericLowerLimit=4.9;$judge.NumericUpperLimit=5.1
    $pair.IsBreakpoint=$true;$pair.SetOutputValue('Distance',999.0);$pair.ResultCode='OK';$pair.RunStatus='Completed';$pair.ErrorMessage='must not persist'

    Method 'OpenFlowSnippetLibraryButton_Click' ([object[]]@($null,[Windows.RoutedEventArgs]::new()))|Out-Null
    Assert ((Field 'FlowSnippetLibraryPanel').Visibility.ToString()-eq'Visible') 'On-demand snippet panel did not open.'
    SelectTools @($pair,$judge)
    (Field 'FlowSnippetNameTextBox').Text='Non Contiguous';Method 'SaveSelectedFlowSnippetButton_Click' ([object[]]@($null,[Windows.RoutedEventArgs]::new()))|Out-Null
    Assert ((Field 'flowSnippetLibraryS70').Snippets.Count-eq0) 'Non-contiguous selection was not rejected.'
    SelectTools @($pair,$distance,$judge)
    (Field 'FlowSnippetNameTextBox').Text='Geometry Inspection Chain';Method 'SaveSelectedFlowSnippetButton_Click' ([object[]]@($null,[Windows.RoutedEventArgs]::new()))|Out-Null
    $library=Field 'flowSnippetLibraryS70';Assert ($library.Snippets.Count-eq1) 'Snippet was not saved.'
    $snippet=$library.Snippets[0]
    Assert ($snippet.ToolFlow.Count-eq3 -and $snippet.ExternalInputs.Count-eq3) 'Snippet tool count or external input contract is wrong.'
    $snippetIds=@($snippet.ToolFlow|ForEach-Object ToolId);$internal=0;foreach($item in $snippet.ToolFlow){foreach($binding in $item.InputBindings){if($snippetIds-contains$binding.SourceToolId){$internal++}}};Assert ($internal-eq2) 'Snippet internal connection count is wrong.'
    Assert (($snippet.ToolFlow|Where-Object IsBreakpoint).Count-eq0 -and ($snippet.ToolFlow|Where-Object {$null-ne$_.GraphX-or$null-ne$_.GraphY}).Count-eq0) 'Snippet retained breakpoint or graph coordinates.'
    Assert (Test-Path $service.FlowSnippetPath) 'Atomic local snippet file was not written.'
    Assert ((Field 'FlowSnippetExternalInputsText').Text-match'PointA:Point2D' -and (Field 'FlowSnippetExternalInputsText').Text-match'Probe To Constructed Line.Point:Point2D') 'Snippet detail does not expose its external input contract.'

    (Field 'FlowSnippetNameTextBox').Text='Geometry QA Chain';Method 'RenameFlowSnippetButton_Click' ([object[]]@($null,[Windows.RoutedEventArgs]::new()))|Out-Null
    Assert ($library.Snippets[0].Name-eq'Geometry QA Chain' -and (Test-Path ($service.FlowSnippetPath+'.bak'))) 'Snippet rename or backup failed.'
    (Field 'FlowToolList').SelectedItem=$judge;$before=$flow.Count;Method 'InsertFlowSnippetAfterButton_Click' ([object[]]@($null,[Windows.RoutedEventArgs]::new()))|Out-Null
    Assert ($flow.Count-eq$before+3) 'Snippet insertion did not add exactly three tools.'
    $copies=@($flow|Select-Object -Last 3);$copyPair=$copies|Where-Object {$_.Kind.ToString()-eq'PointPairLineConstructionMeasurement'}|Select-Object -First 1;$copyDistance=$copies|Where-Object {$_.Kind.ToString()-eq'PointToLineDistanceMeasurement'}|Select-Object -First 1;$copyJudge=$copies|Where-Object {$_.Kind.ToString()-eq'NumericJudge'}|Select-Object -First 1
    Assert ($copyPair.ToolId-ne$pair.ToolId -and $copyDistance.GetInputBinding('Line').SourceToolId-eq$copyPair.ToolId -and $copyJudge.GetInputBinding('Value').SourceToolId-eq$copyPair.ToolId) 'Snippet internal ToolIds/InputBindings were not remapped.'
    Assert ($copyPair.GetInputBinding('PointA').SourceToolId-eq$sourceA.ToolId -and $copyPair.GetInputBinding('PointB').SourceToolId-eq$sourceB.ToolId -and $copyDistance.GetInputBinding('Point').SourceToolId-eq$probe.ToolId) 'Compatible external context was not safely rebound.'
    Assert ($copyPair.ResultCode-eq'--' -and $copyPair.RunStatus-ne'Completed' -and -not $copyPair.TryGetOutputValue('Distance',[ref]$null)) 'Snippet copied runtime results.'
    Method 'UndoFlowEditButton_Click' ([object[]]@($null,$null))|Out-Null;Assert ($flow.Count-eq$before) 'One undo did not remove the entire inserted snippet.'
    Method 'RedoFlowEditButton_Click' ([object[]]@($null,$null))|Out-Null;Assert ($flow.Count-eq$before+3) 'Redo did not restore the inserted snippet.'
    $correction=Tool 'PositionCorrection2D' 'Correction Contract';$emptyJudge=Tool 'NumericJudge' 'Contract Consumer';$flow.Add($correction);$flow.Add($emptyJudge);SelectTools @($correction,$emptyJudge);(Field 'FlowSnippetNameTextBox').Text='Correction Contract Chain';Method 'SaveSelectedFlowSnippetButton_Click' ([object[]]@($null,[Windows.RoutedEventArgs]::new()))|Out-Null
    $correctionSnippet=$library.Snippets|Where-Object Name -eq 'Correction Contract Chain'|Select-Object -First 1;Assert ($null-ne$correctionSnippet -and @($correctionSnippet.ExternalInputs|Where-Object {$_.TargetPortName-eq'Pose2D'-and$_.IsRequired}).Count-eq1) 'PositionCorrection2D snippet did not expose required Pose2D when neither input mode was bound.'
    Method 'DeleteFlowSnippetButton_Click' ([object[]]@($null,[Windows.RoutedEventArgs]::new()))|Out-Null;Method 'DeleteFlowSnippetButton_Click' ([object[]]@($null,[Windows.RoutedEventArgs]::new()))|Out-Null;Assert ($library.Snippets.Count-eq1) 'Correction contract fixture cleanup failed.'
    (Field 'FlowToolList').SelectedItems.Clear();(Field 'FlowToolList').SelectedItem=$judge;$validType=$snippet.ToolFlow[0].ToolType;$snippet.ToolFlow[0].ToolType='MissingFutureTool';$invalidBefore=$flow.Count;Method 'InsertFlowSnippetAfterButton_Click' ([object[]]@($null,[Windows.RoutedEventArgs]::new()))|Out-Null;$snippet.ToolFlow[0].ToolType=$validType;Assert ($flow.Count-eq$invalidBefore -and (Field 'flowToolClipboard').Count-eq0 -and (Field 'FlowSnippetStatusText').Text.Length-gt10) 'Unsupported snippet type did not fail visibly and restore clipboard state.'
    Method 'DeleteFlowSnippetButton_Click' ([object[]]@($null,[Windows.RoutedEventArgs]::new()))|Out-Null;Assert ((Field 'pendingDeleteSnippetIdS70')-eq$snippet.SnippetId -and $library.Snippets.Count-eq1) 'Snippet delete did not require inline confirmation.'
    Method 'DeleteFlowSnippetButton_Click' ([object[]]@($null,[Windows.RoutedEventArgs]::new()))|Out-Null;Assert ($library.Snippets.Count-eq0) 'Confirmed snippet delete failed.'
    [IO.File]::WriteAllText($service.FlowSnippetPath,'{ broken json',[Text.Encoding]::UTF8);Method 'LoadFlowSnippetLibraryS70' ([object[]]@($false))|Out-Null
    Assert ((Field 'FlowSnippetStatusText').Text.Length-gt10 -and (Field 'flowSnippetLibraryS70').Snippets.Count-eq0 -and [IO.File]::ReadAllText($service.FlowSnippetPath)-match'broken json') 'Corrupt snippet diagnostics overwrote or hid the original file.'

    # B: HALCON distance_pp, typed Line2D/MidPoint and real downstream chain.
    Method 'DisposeFlowTools'|Out-Null;$flow.Clear();foreach($tool in @($sourceA,$sourceB,$probe,$pair,$distance,$judge)){$tool= $null}
    $sourceA=Tool 'PointSetGeometryStatistics' 'Point A';$sourceB=Tool 'PointSetGeometryStatistics' 'Point B';$probe=Tool 'PointSetGeometryStatistics' 'Probe';$pair=Tool 'PointPairLineConstructionMeasurement' 'Two Point Main Line';$distance=Tool 'PointToLineDistanceMeasurement' 'Typed Line Consumer';$judge=Tool 'NumericJudge' 'Distance Judge'
    foreach($tool in @($sourceA,$sourceB,$probe,$pair,$distance,$judge)){$flow.Add($tool)}
    $context='S70.Image';$sourceA.SetOutputValue('Centroid',(Point 0 0 $context));$sourceB.SetOutputValue('Centroid',(Point 3 4 $context));$probe.SetOutputValue('Centroid',(Point 0 4 $context));foreach($source in @($sourceA,$sourceB,$probe)){$source.ResultCode='OK';$source.RunStatus='Completed'}
    $pair.SetInputBinding('PointA',$sourceA.ToolId,'Centroid');$pair.SetInputBinding('PointB',$sourceB.ToolId,'Centroid')
    $distance.SetInputBinding('Point',$probe.ToolId,'Centroid');$distance.SetInputBinding('Line',$pair.ToolId,'MainLine')
    $judge.SetInputBinding('Value',$pair.ToolId,'Distance');$judge.InputToolId=$pair.ToolId;$judge.InputPortName='Distance';$judge.NumericOperator='BetweenInclusive';$judge.NumericLowerLimit=4.9;$judge.NumericUpperLimit=5.1
    $result=Run $pair
    Assert ($result.ResultCode-eq'OK' -and [Math]::Abs([double](Output $pair 'Distance')-5)-lt0.0001) 'HALCON distance_pp result is wrong.'
    Assert ([Math]::Abs([double](Output $pair 'DeltaRow')-3)-lt0.0001 -and [Math]::Abs([double](Output $pair 'DeltaColumn')-4)-lt0.0001 -and [Math]::Abs([double](Output $pair 'Angle')-36.86989765)-lt0.0001) 'Point-pair deltas or direction angle are wrong.'
    $line=Output $pair 'MainLine';$mid=Output $pair 'MidPoint';Assert ($line.GetType()-eq$lineType -and $line.CoordinateContextId-eq$context -and [Math]::Abs($mid.Row-1.5)-lt0.0001 -and [Math]::Abs($mid.Column-2)-lt0.0001) 'Typed Line2D or MidPoint is wrong.'
    Assert ((Output $pair 'XLD').ObjectCount-eq4 -and (Run $distance).ResultCode-eq'OK' -and [Math]::Abs([double](Output $distance 'Distance')-2.4)-lt0.0001 -and (Run $judge).ResultCode-eq'OK') 'Managed XLD, Line2D downstream, or NumericJudge downstream failed.'
    $old=Output $pair 'XLD';$pair.Parameters.PointPairOverlayCrossSize=15;Run $pair|Out-Null;Assert ($old.IsDisposed) 'Rerun did not dispose the old managed XLD snapshot.'
    (Field 'FlowToolList').SelectedItem=$pair;Method 'LoadDockConfigurationDraft' ([object[]]@($pair))|Out-Null
    Assert ((Field 'DockPointPairLineConstructionPanel').Visibility.ToString()-eq'Visible' -and (Field 'DockPointPairMinimumDistanceTextBox').Text.Length-gt0) 'Point-pair parameters are not available in the right persistent dock.'
    Method 'RefreshPortDataMonitorS64' ([object[]]@($pair))|Out-Null;Assert (@((Field 'portDataMonitorRows')|Where-Object DataType -eq 'Point2D').Count -ge 2) 'Port monitor did not expose both Point2D inputs.'
    $recipe=Method 'CaptureRecipe';$saved=$recipe.ToolFlow|Where-Object ToolId -eq $pair.ToolId|Select-Object -First 1;Assert ([Math]::Abs($saved.Parameters.PointPairOverlayCrossSize-15)-lt0.0001) 'Recipe lost point-pair parameters.'
    SelectTools @($sourceA,$sourceB,$pair);Assert ([int](Method 'CopySelectedFlowToolsToClipboard')-eq3 -and [int](Method 'PasteFlowToolClipboard' ([object[]]@($flow.Count)))-eq3) 'Point-pair block copy/paste failed.'
    $block=@($flow|Select-Object -Last 3);$copyA=$block[0];$copyB=$block[1];$copyPair=$block[2];Assert ($copyPair.GetInputBinding('PointA').SourceToolId-eq$copyA.ToolId -and $copyPair.GetInputBinding('PointB').SourceToolId-eq$copyB.ToolId) 'Point-pair copy did not remap both sources.'

    $pair.SetInputBinding('PointB',$sourceA.ToolId,'Centroid');$failed=$false;try{Run $pair|Out-Null}catch{$failed=$true};Assert $failed 'Duplicate endpoint was accepted.'
    $pair.SetInputBinding('PointB',$sourceB.ToolId,'Centroid');$sourceB.SetOutputValue('Centroid',(Point 0 0 $context));$sourceB.ResultCode='OK';$failed=$false;try{Run $pair|Out-Null}catch{$failed=$true};Assert $failed 'Coincident points were accepted.'
    $sourceB.SetOutputValue('Centroid',(Point 0 0.0000005 $context));$sourceB.ResultCode='OK';$failed=$false;try{Run $pair|Out-Null}catch{$failed=$true};Assert $failed 'Near-coincident points below the configured threshold were accepted.'
    $sourceB.SetOutputValue('Centroid',(Point 3 4 'Other.Image'));$sourceB.ResultCode='OK';$failed=$false;try{Run $pair|Out-Null}catch{$failed=$true};Assert $failed 'Cross-context points were accepted.'
    $sourceB.SetOutputValue('Centroid',(Point ([double]::NaN) 4 $context));$sourceB.ResultCode='OK';$failed=$false;try{Run $pair|Out-Null}catch{$failed=$true};Assert $failed 'Nonfinite point was accepted.'
    $sourceB.SetOutputValue('Centroid',(Point 3 4 $context));$sourceB.ResultCode='--';$failed=$false;try{Run $pair|Out-Null}catch{$failed=$true};Assert $failed 'Stale/unrun Point2D source was accepted.'
    Write-Output 'S70 VM-style on-demand flow snippets/save-preview-rename-delete/remap-external binding/undo/runtime isolation/corrupt diagnostics + HALCON distance_pp Point2D pair to Line2D/MidPoint/XLD/downstreams/recipe/copy/failures passed; BSB not implemented.'
}
finally{
    $main.GetField('recipeCloseConfirmed',$private).SetValue($window,$true);$window.Close();if($ownsApp){$app.Shutdown()};if(Test-Path $temp){Remove-Item -LiteralPath $temp -Recurse -Force}
}
