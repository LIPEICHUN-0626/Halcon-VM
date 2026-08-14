param([string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot))

$ErrorActionPreference = 'Stop'
if ([Threading.Thread]::CurrentThread.ApartmentState -ne [Threading.ApartmentState]::STA) { throw 'Run with powershell.exe -STA.' }
$halconRoot = 'C:\Program Files\MVTec\HALCON-20.11-Progress'
$env:PATH = (Join-Path $halconRoot 'bin\x64-win64') + ';' + $env:PATH
Add-Type -AssemblyName PresentationFramework
[Reflection.Assembly]::LoadFrom((Join-Path $halconRoot 'bin\dotnet35\halcondotnet.dll')) | Out-Null
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path $ProjectRoot 'bin\x64\Debug\HalconWinFormsDemo.exe'))

function Assert-True([bool]$Condition, [string]$Message) { if (-not $Condition) { throw "Assertion failed: $Message" } }
$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s24-' + [Guid]::NewGuid().ToString('N')); [IO.Directory]::CreateDirectory($tempRoot) | Out-Null
$mainType = $assembly.GetType('HalconWinFormsDemo.MainWindow', $true); $recipeServiceType = $assembly.GetType('HalconWinFormsDemo.Services.RecipeService', $true); $loggerType = $assembly.GetType('HalconWinFormsDemo.Services.AppLogger', $true); $toolKindType = $assembly.GetType('HalconWinFormsDemo.Models.VmToolKind', $true)
$private = [Reflection.BindingFlags]'Instance,NonPublic'; $all = [Reflection.BindingFlags]'Instance,NonPublic,Public'; $args = New-Object 'object[]' 1; $args[0] = $tempRoot.ToString()
$service = $recipeServiceType.GetConstructor([Type[]]@([string])).Invoke($args); $logger = $loggerType.GetConstructor([Type[]]@([string])).Invoke($args)
$window = $mainType.GetConstructor($private, $null, [Type[]]@($recipeServiceType, $loggerType), $null).Invoke([object[]]@($service, $logger))
$flow = $mainType.GetField('flowTools', $private).GetValue($window); $catalog = $mainType.GetField('toolCatalog', $private).GetValue($window); $create = $mainType.GetMethod('CreateFlowTool', $private); $runEllipse = $mainType.GetMethod('RunXldEllipseFittingTool', $private); $dispose = $mainType.GetMethod('DisposeFlowTools', $private)
function New-Tool([string]$Kind, [string]$Name) { $create.Invoke($window, [object[]]@([Enum]::Parse($toolKindType, $Kind), $Name, $true, $null)) }
function Get-Out($Tool, [string]$Port) { $value = $null; if (-not $Tool.TryGetOutputValue($Port, [ref]$value)) { throw "$($Tool.InstanceName).$Port missing" }; $value }
function Invoke-Ellipse($Tool, $Xld) { try { $runEllipse.Invoke($window, [object[]]@($Tool, $Xld)) } catch [Reflection.TargetInvocationException] { throw $_.Exception.InnerException } }
function Select-Tools([object[]]$Tools) { $list.SelectedItems.Clear(); foreach ($tool in $Tools) { $list.SelectedItems.Add($tool) | Out-Null } }

$dispose.Invoke($window, @()) | Out-Null; $flow.Clear()
$source = New-Tool ImageSource S24_Source; $threshold = New-Tool ImageThreshold S24_Threshold; $contour = New-Tool RegionContourMeasurement S24_Contour; $ellipseTool = New-Tool XldEllipseFitting S24_Ellipse_Fit; $judge = New-Tool NumericJudge S24_Radius1_Judge
foreach ($tool in @($source,$threshold,$contour,$ellipseTool,$judge)) { $flow.Add($tool) }
$threshold.SetInputBinding('Image',$source.ToolId,'Image'); $contour.SetInputBinding('Region',$threshold.ToolId,'Region'); $ellipseTool.SetInputBinding('XLD',$contour.ToolId,'XLD'); $judge.InputToolId=$ellipseTool.ToolId; $judge.InputPortName='Radius1'; $judge.SetInputBinding('Value',$ellipseTool.ToolId,'Radius1')
$list = $mainType.GetField('FlowToolList', $all).GetValue($window)
Assert-True ($null -ne ($catalog | Where-Object { $_.Kind.ToString() -eq 'XldEllipseFitting' } | Select-Object -First 1)) 'Ellipse-fitting tool missing from toolbox.'
foreach ($fieldName in @('DockXldEllipseFittingPanel','DockXldEllipseFitAlgorithmComboBox','CreateFlowGroupButton','FlowGroupNameTextBox','FlowGroupItemsControl')) { Assert-True ($null -ne $mainType.GetField($fieldName, $all).GetValue($window)) "$fieldName is missing." }
$metadata = $assembly.GetType('HalconWinFormsDemo.Models.ToolMetadata',$true); $getOutputs=$metadata.GetMethod('GetOutputPorts',[Reflection.BindingFlags]'Static,Public'); $ellipsePorts=$getOutputs.Invoke($null,[object[]]@([Enum]::Parse($toolKindType,'XldEllipseFitting')))
Assert-True (($ellipsePorts | Where-Object PortName -eq 'Radius1').Count -eq 1 -and ($ellipsePorts | Where-Object PortName -eq 'Radius2').Count -eq 1 -and ($ellipsePorts | Where-Object PortName -eq 'Phi').Count -eq 1) 'Ellipse Number outputs are missing.'

[HalconDotNet.HObject]$whole = $null; [HalconDotNet.HObject]$arc = $null; [HalconDotNet.HObject]$circle = $null; [HalconDotNet.HObject]$line = $null; [HalconDotNet.HObject]$empty = $null; [HalconDotNet.HObject]$short = $null; [HalconDotNet.HObject]$outliers = $null
try {
    [HalconDotNet.HOperatorSet]::GenEllipseContourXld([ref]$whole, 80, 90, 0.45, 40, 20, 0, 6.283185307179586, 'positive', 1)
    $wholeRecord = Invoke-Ellipse $ellipseTool $whole; $wholeSnapshot = Get-Out $ellipseTool XLD
    Assert-True ($wholeRecord.ResultCode -eq 'OK' -and [int][double](Get-Out $ellipseTool EllipseCount) -eq 1) 'Whole ellipse did not return one fitted ellipse.'
    Assert-True ([Math]::Abs([double](Get-Out $ellipseTool CenterRow)-80) -lt .05 -and [Math]::Abs([double](Get-Out $ellipseTool CenterColumn)-90) -lt .05) 'Whole ellipse center is wrong.'
    Assert-True ([Math]::Abs([double](Get-Out $ellipseTool Radius1)-40) -lt .05 -and [Math]::Abs([double](Get-Out $ellipseTool Radius2)-20) -lt .05) 'Whole ellipse axes are wrong.'
    Assert-True ([Math]::Abs([Math]::Sin([double](Get-Out $ellipseTool Phi)-0.45)) -lt .01 -and $wholeSnapshot.ObjectCount -eq 1) 'Ellipse orientation or managed fitted XLD is wrong.'

    [HalconDotNet.HOperatorSet]::GenEllipseContourXld([ref]$arc, 130, 170, -0.65, 55, 24, 0.2, 4.8, 'positive', 1)
    $arcRecord=Invoke-Ellipse $ellipseTool $arc
    Assert-True ($arcRecord.ResultCode -eq 'OK' -and [Math]::Abs([double](Get-Out $ellipseTool Radius1)-55) -lt .2 -and [Math]::Abs([double](Get-Out $ellipseTool Radius2)-24) -lt .2) 'Open rotated ellipse arc fitting is wrong.'
    foreach ($algorithm in @('Fitzgibbon','FHuber','FTukey','FocPoints','FPHuber','FPTukey','Geometric','GeoHuber','GeoTukey','Voss')) { $ellipseTool.Parameters.XldEllipseFitAlgorithm=$algorithm; Assert-True ((Invoke-Ellipse $ellipseTool $arc).ResultCode -eq 'OK') "$algorithm failed." }

    [HalconDotNet.HTuple]$baseRows=$null; [HalconDotNet.HTuple]$baseCols=$null; [HalconDotNet.HOperatorSet]::GetContourXld($whole,[ref]$baseRows,[ref]$baseCols)
    $rowValues = New-Object 'System.Collections.Generic.List[double]'; $colValues = New-Object 'System.Collections.Generic.List[double]'
    for($i=0;$i -lt $baseRows.Length;$i++){ $rowValues.Add($baseRows[$i].D);$colValues.Add($baseCols[$i].D); if($i -eq 40){$rowValues.Add(5);$colValues.Add(210)} }
    [HalconDotNet.HOperatorSet]::GenContourPolygonXld([ref]$outliers,[HalconDotNet.HTuple]$rowValues.ToArray(),[HalconDotNet.HTuple]$colValues.ToArray())
    $ellipseTool.Parameters.XldEllipseFitAlgorithm='FTukey'; $outlierRecord=Invoke-Ellipse $ellipseTool $outliers
    Assert-True ($outlierRecord.ResultCode -eq 'OK' -and [Math]::Abs([double](Get-Out $ellipseTool Radius1)-40) -lt 2.5) 'Robust ellipse fit did not tolerate an outlier.'

    [HalconDotNet.HOperatorSet]::GenCircleContourXld([ref]$circle,80,90,25,0,6.283185307179586,'positive',1)
    $rejected=$false;try{Invoke-Ellipse $ellipseTool $circle|Out-Null}catch{$rejected=$true};Assert-True $rejected 'Circle degeneration was not rejected.'
    [HalconDotNet.HOperatorSet]::GenContourPolygonXld([ref]$line,[HalconDotNet.HTuple]@([double]10,[double]10,[double]10,[double]10,[double]10),[HalconDotNet.HTuple]@([double]5,[double]35,[double]65,[double]95,[double]125))
    $rejected=$false;try{Invoke-Ellipse $ellipseTool $line|Out-Null}catch{$rejected=$true};Assert-True $rejected 'Non-elliptic line was not rejected.'
    [HalconDotNet.HOperatorSet]::GenEmptyObj([ref]$empty);$emptyRecord=Invoke-Ellipse $ellipseTool $empty;Assert-True ($emptyRecord.ResultCode -eq 'NG' -and [int][double](Get-Out $ellipseTool EllipseCount) -eq 0) 'Empty XLD did not return managed NG.'
    [HalconDotNet.HOperatorSet]::GenContourPolygonXld([ref]$short,[HalconDotNet.HTuple]@([double]1,[double]2,[double]3,[double]4),[HalconDotNet.HTuple]@([double]1,[double]2,[double]4,[double]8))
    $rejected=$false;try{Invoke-Ellipse $ellipseTool $short|Out-Null}catch{$rejected=$true};Assert-True $rejected 'Four-point XLD was not rejected.'
    foreach($case in @(@('XldEllipseFitAlgorithm','Bad'),@('XldEllipseFitMaxNumPoints',4),@('XldEllipseFitMaxClosureDistance',-1),@('XldEllipseFitClippingEndPoints',-1),@('XldEllipseFitVossTabSize',3),@('XldEllipseFitIterations',-1),@('XldEllipseFitClippingFactor',0))){$badTool=New-Tool XldEllipseFitting S24_Invalid;$badTool.Parameters.$($case[0])=$case[1];$bad=$false;try{Invoke-Ellipse $badTool $whole|Out-Null}catch{$bad=$true};Assert-True $bad "Invalid $($case[0]) was accepted.";$badTool.Dispose()}

    $oldSnapshot=Get-Out $ellipseTool XLD;Invoke-Ellipse $ellipseTool $whole|Out-Null;Assert-True $oldSnapshot.IsDisposed 'Rerun did not release the previous fitted XLD snapshot.'
    $ellipseTool.Parameters.XldEllipseFitAlgorithm='GeoTukey';$ellipseTool.Parameters.XldEllipseFitMaxNumPoints=321;$ellipseTool.Parameters.XldEllipseFitMaxClosureDistance=2.75;$ellipseTool.Parameters.XldEllipseFitClippingEndPoints=2;$ellipseTool.Parameters.XldEllipseFitVossTabSize=333;$ellipseTool.Parameters.XldEllipseFitIterations=7;$ellipseTool.Parameters.XldEllipseFitClippingFactor=1.8
    $captureFlow=$mainType.GetMethod('CaptureFlowRecipe',$private);$visionRecipeType=$assembly.GetType('HalconWinFormsDemo.Models.VisionRecipe',$true);$recipe=[Activator]::CreateInstance($visionRecipeType);$recipe.ToolFlow=$captureFlow.Invoke($window,@());$recipePath=Join-Path $tempRoot 's24-ellipse-recipe.json';$service.SaveRecipe($recipePath,$recipe);$loaded=$service.LoadRecipe($recipePath);$loadedEllipse=$loaded.ToolFlow|Where-Object ToolType -eq 'XldEllipseFitting'|Select-Object -First 1
    Assert-True ($loadedEllipse.Parameters.XldEllipseFitAlgorithm -eq 'GeoTukey' -and $loadedEllipse.Parameters.XldEllipseFitMaxNumPoints -eq 321 -and [Math]::Abs($loadedEllipse.Parameters.XldEllipseFitMaxClosureDistance-2.75) -lt .001 -and $loadedEllipse.Parameters.XldEllipseFitClippingEndPoints -eq 2 -and $loadedEllipse.Parameters.XldEllipseFitVossTabSize -eq 333 -and $loadedEllipse.Parameters.XldEllipseFitIterations -eq 7 -and [Math]::Abs($loadedEllipse.Parameters.XldEllipseFitClippingFactor-1.8) -lt .001) 'Ellipse parameters did not survive recipe save/load.'

    # A line: group membership is a recipe DTO, visible container, compact ports, copy/delete lifecycle and undo/redo.
    $resetHistory=$mainType.GetMethod('ResetFlowEditHistory',$private);$createGroup=$mainType.GetMethod('CreateFlowGroupButton_Click',$private);$renameGroup=$mainType.GetMethod('RenameFlowGroupButton_Click',$private);$collapseGroup=$mainType.GetMethod('CollapseFlowGroupButton_Click',$private);$expandGroup=$mainType.GetMethod('ExpandFlowGroupButton_Click',$private);$ungroup=$mainType.GetMethod('UngroupFlowGroupButton_Click',$private);$undo=$mainType.GetMethod('UndoFlowEditButton_Click',$private);$redo=$mainType.GetMethod('RedoFlowEditButton_Click',$private);$copy=$mainType.GetMethod('CopySelectedFlowToolsToClipboard',$private);$paste=$mainType.GetMethod('PasteFlowToolClipboard',$private);$delete=$mainType.GetMethod('DeleteSelectedFlowTools',$private)
    $resetHistory.Invoke($window,@())|Out-Null;Select-Tools @($threshold,$contour,$ellipseTool);$sequenceBefore=($flow|ForEach-Object ToolId)-join '|';$createGroup.Invoke($window,[object[]]@($null,$null))|Out-Null
    $groupId=$threshold.GroupId;Assert-True (-not [string]::IsNullOrWhiteSpace($groupId) -and $contour.GroupId -eq $groupId -and $ellipseTool.GroupId -eq $groupId) 'Selected tools were not grouped.'
    $groupVisuals=$mainType.GetField('flowGroupVisuals',$private).GetValue($window);Assert-True ($groupVisuals.Count -eq 1 -and $groupVisuals[0].BoundarySummary -match 'IN 1' -and $groupVisuals[0].BoundarySummary -match 'OUT 1') 'Group container did not expose real boundary ports.'
    Assert-True ((($flow|ForEach-Object ToolId)-join '|') -eq $sequenceBefore) 'Grouping changed deterministic flow order.'
    $groupNameBox=$mainType.GetField('FlowGroupNameTextBox',$all).GetValue($window);$groupNameBox.Text='Ellipse Inspection';$renameGroup.Invoke($window,[object[]]@($null,$null))|Out-Null;Assert-True ($threshold.GroupName -eq 'Ellipse Inspection' -and $contour.GroupName -eq 'Ellipse Inspection') 'Group rename did not update every member.'
    $ellipseTool.IsFlowDetailExpanded=$true;$mainType.GetMethod('RefreshFlowPortVisualization',$private).Invoke($window,@())|Out-Null;$expandedHeight=$ellipseTool.GraphHeight;$connectionCount=$mainType.GetField('flowConnections',$private).GetValue($window).Count;$collapseGroup.Invoke($window,[object[]]@($null,$null))|Out-Null
    Assert-True ($ellipseTool.IsGroupCollapsed -and $ellipseTool.GraphHeight -lt $expandedHeight -and $ellipseTool.FlowInputPorts.Count -eq 1 -and $ellipseTool.FlowOutputPorts.Count -ge 14) 'Safe collapse did not compact cards while retaining all ports.'
    Assert-True ($mainType.GetField('flowConnections',$private).GetValue($window).Count -eq $connectionCount -and (($flow|ForEach-Object ToolId)-join '|') -eq $sequenceBefore) 'Safe collapse hid a dependency or changed execution order.'
    $ellipseId=$ellipseTool.ToolId;$undo.Invoke($window,[object[]]@($null,$null))|Out-Null;$ellipseTool=$flow|Where-Object ToolId -eq $ellipseId|Select-Object -First 1;Assert-True (-not $ellipseTool.IsGroupCollapsed) 'Undo did not expand the group.'
    $redo.Invoke($window,[object[]]@($null,$null))|Out-Null;for($i=0;$i -lt $flow.Count;$i++){if($flow[$i].ToolId -eq $ellipseId){$ellipseTool=$flow[$i];break}};Assert-True $ellipseTool.IsGroupCollapsed 'Redo did not restore collapsed group state.'

    $groupRecipe=[Activator]::CreateInstance($visionRecipeType);$groupRecipe.ToolFlow=$captureFlow.Invoke($window,@());$groupPath=Join-Path $tempRoot 's24-group-recipe.json';$service.SaveRecipe($groupPath,$groupRecipe);$loadedGroup=$service.LoadRecipe($groupPath);$loadedMembers=@($loadedGroup.ToolFlow|Where-Object GroupId -eq $groupId)
    Assert-True ($loadedMembers.Count -eq 3 -and ($loadedMembers|Where-Object IsGroupCollapsed).Count -eq 3 -and ($loadedMembers|Where-Object GroupName -eq 'Ellipse Inspection').Count -eq 3) 'Group relationship did not survive recipe save/load.'
    Invoke-Ellipse $ellipseTool $whole|Out-Null;$preReloadSnapshot=Get-Out $ellipseTool XLD;$applyFlow=$mainType.GetMethod('ApplyFlowFromRecipe',$private);$applyFlow.Invoke($window,[object[]]@($loadedGroup))|Out-Null
    Assert-True $preReloadSnapshot.IsDisposed 'Group recipe reload did not release the old XLD runtime snapshot.'
    Assert-True (($flow|Where-Object GroupId -eq $groupId).Count -eq 3 -and $mainType.GetField('flowGroupVisuals',$private).GetValue($window).Count -eq 1) 'Applying the saved recipe did not rebuild the group container.'

    $originalMembers=@($flow|Where-Object GroupId -eq $groupId);Select-Tools $originalMembers;$copy.Invoke($window,@())|Out-Null;$paste.Invoke($window,[object[]]@($flow.Count-1))|Out-Null;$copiedGroups=@($flow|Where-Object { -not [string]::IsNullOrWhiteSpace($_.GroupId) -and $_.GroupId -ne $groupId }|Group-Object GroupId)
    Assert-True ($copiedGroups.Count -ge 1 -and $copiedGroups[-1].Count -eq 3) 'Complete group copy did not create a new complete GroupId.'
    $copiedGroupId=$copiedGroups[-1].Name;$partialSource=$flow|Where-Object GroupId -eq $copiedGroupId|Select-Object -First 1;Select-Tools @($partialSource);$copy.Invoke($window,@())|Out-Null;$paste.Invoke($window,[object[]]@($flow.Count))|Out-Null;$partialCopy=$flow[-1];Assert-True ([string]::IsNullOrWhiteSpace($partialCopy.GroupId)) 'Partial group copy created a broken one-member group.'

    $survivorMembers=@($flow|Where-Object GroupId -eq $groupId);Select-Tools @($survivorMembers[0],$survivorMembers[1]);$delete.Invoke($window,@())|Out-Null;$survivor=$flow|Where-Object ToolId -eq $survivorMembers[2].ToolId|Select-Object -First 1;Assert-True ([string]::IsNullOrWhiteSpace($survivor.GroupId)) 'Deleting members left a broken one-member group.'
    $copiedMember=$flow|Where-Object GroupId -eq $copiedGroupId|Select-Object -First 1;Select-Tools @($copiedMember);$ungroup.Invoke($window,[object[]]@($null,$null))|Out-Null;Assert-True (($flow|Where-Object GroupId -eq $copiedGroupId).Count -eq 0) 'Ungroup did not clear membership.'
    $undo.Invoke($window,[object[]]@($null,$null))|Out-Null;Assert-True (($flow|Where-Object GroupId -eq $copiedGroupId).Count -eq 3) 'Undo did not restore ungrouped members.'

    $fitMethods=[HalconDotNet.HOperatorSet].GetMethods()|Where-Object Name -eq 'FitEllipseContourXld';Assert-True ($fitMethods.Count -eq 1 -and $fitMethods[0].GetParameters().Count -eq 16) 'HALCON 20.11 FitEllipseContourXld .NET signature was not verified.'
}
finally {
    foreach ($obj in @($whole,$arc,$circle,$line,$empty,$short,$outliers)) { if ($null -ne $obj) { $obj.Dispose() } }
}

$dispose.Invoke($window,@())|Out-Null;$mainType.GetField('recipeCloseConfirmed',$private).SetValue($window,$true);$window.Close();if(Test-Path $tempRoot){Remove-Item -LiteralPath $tempRoot -Recurse -Force}
Write-Output 'S24 VM flow groups and HALCON XLD ellipse fitting smoke passed.'
