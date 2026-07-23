param([string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot))

$ErrorActionPreference = 'Stop'
if ([Threading.Thread]::CurrentThread.ApartmentState -ne [Threading.ApartmentState]::STA) { throw 'Run with powershell.exe -STA.' }
$halconRoot = 'C:\Program Files\MVTec\HALCON-20.11-Progress'
$env:PATH = (Join-Path $halconRoot 'bin\x64-win64') + ';' + $env:PATH
Add-Type -AssemblyName PresentationFramework
[Reflection.Assembly]::LoadFrom((Join-Path $halconRoot 'bin\dotnet35\halcondotnet.dll')) | Out-Null
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path $ProjectRoot 'bin\x64\Debug\HalconWinFormsDemo.exe'))

function Assert-True([bool]$Condition, [string]$Message) { if (-not $Condition) { throw "Assertion failed: $Message" } }
function Write-Pgm([string]$Path) {
    $w=100; $h=80; $header=[Text.Encoding]::ASCII.GetBytes("P5`n$w $h`n255`n"); $pixels=New-Object byte[] ($w*$h)
    for($r=0;$r -lt $h;$r++){ for($c=0;$c -lt $w;$c++){ $pixels[$r*$w+$c]=if($r -ge 12 -and $r -le 62 -and $c -ge 14 -and $c -le 84){220}else{30} } }
    $bytes=New-Object byte[] ($header.Length+$pixels.Length); [Array]::Copy($header,0,$bytes,0,$header.Length); [Array]::Copy($pixels,0,$bytes,$header.Length,$pixels.Length); [IO.File]::WriteAllBytes($Path,$bytes)
}

$tempRoot=Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s22-'+[Guid]::NewGuid().ToString('N')); [IO.Directory]::CreateDirectory($tempRoot)|Out-Null
$imagePath=Join-Path $tempRoot 'rectangle.pgm'; Write-Pgm $imagePath
$mainType=$assembly.GetType('HalconWinFormsDemo.MainWindow',$true); $recipeServiceType=$assembly.GetType('HalconWinFormsDemo.Services.RecipeService',$true); $loggerType=$assembly.GetType('HalconWinFormsDemo.Services.AppLogger',$true); $toolKindType=$assembly.GetType('HalconWinFormsDemo.Models.VmToolKind',$true)
$private=[Reflection.BindingFlags]'Instance,NonPublic'; $all=[Reflection.BindingFlags]'Instance,NonPublic,Public'; $args=New-Object 'object[]' 1; $args[0]=$tempRoot.ToString()
$service=$recipeServiceType.GetConstructor([Type[]]@([string])).Invoke($args); $logger=$loggerType.GetConstructor([Type[]]@([string])).Invoke($args)
$window=$mainType.GetConstructor($private,$null,[Type[]]@($recipeServiceType,$loggerType),$null).Invoke([object[]]@($service,$logger))
$flow=$mainType.GetField('flowTools',$private).GetValue($window); $catalog=$mainType.GetField('toolCatalog',$private).GetValue($window); $create=$mainType.GetMethod('CreateFlowTool',$private); $execute=$mainType.GetMethod('ExecuteFlowTool',$private); $runFit=$mainType.GetMethod('RunXldLineFittingTool',$private); $dispose=$mainType.GetMethod('DisposeFlowTools',$private)
function New-Tool([string]$Kind,[string]$Name){ $create.Invoke($window,[object[]]@([Enum]::Parse($toolKindType,$Kind),$Name,$true,$null)) }
function Run-Tool($Tool,[string]$Label){ try{$execute.Invoke($window,[object[]]@($Tool,$Label))}catch [Reflection.TargetInvocationException]{throw $_.Exception.InnerException} }
function Get-Out($Tool,[string]$Port){$v=$null;if(-not $Tool.TryGetOutputValue($Port,[ref]$v)){throw "$($Tool.InstanceName).$Port missing"};$v}
function Invoke-Fit($Tool,$Xld){try{$runFit.Invoke($window,[object[]]@($Tool,$Xld))}catch [Reflection.TargetInvocationException]{throw $_.Exception.InnerException}}

$dispose.Invoke($window,@())|Out-Null; $flow.Clear()
$source=New-Tool ImageSource S22_Source; $threshold=New-Tool ImageThreshold S22_Region; $contour=New-Tool RegionContourMeasurement S22_Contour; $segment=New-Tool XldContourSegmentation S22_Segments; $fit=New-Tool XldLineFitting S22_Line_Fit; $judge=New-Tool NumericJudge S22_Length_Judge
$source.Parameters.LocalImagePath=$imagePath; $threshold.Parameters.ImageThresholdMinGray=150; $threshold.Parameters.ImageThresholdMaxGray=255; $contour.Parameters.RegionContourMode='Border'; $segment.Parameters.XldSegmentMode='Lines'; $fit.Parameters.XldLineFitAlgorithm='Tukey'; $judge.NumericOperator='GreaterOrEqual'; $judge.NumericLowerLimit=240
foreach($tool in @($source,$threshold,$contour,$segment,$fit,$judge)){$flow.Add($tool)}
$threshold.SetInputBinding('Image',$source.ToolId,'Image'); $contour.SetInputBinding('Region',$threshold.ToolId,'Region'); $segment.SetInputBinding('XLD',$contour.ToolId,'XLD'); $fit.SetInputBinding('XLD',$segment.ToolId,'XLD'); $judge.SetInputBinding('Value',$fit.ToolId,'TotalLength'); $judge.InputToolId=$fit.ToolId; $judge.InputPortName='TotalLength'
Assert-True ($null -ne ($catalog|Where-Object{$_.Kind.ToString() -eq 'XldLineFitting'}|Select-Object -First 1)) 'Line-fitting tool missing from toolbox.'
$unrun=$false; try{Run-Tool $fit unrun|Out-Null}catch{$unrun=$true}; Assert-True $unrun 'Unrun upstream XLD was not rejected.'
foreach($tool in @($source,$threshold,$contour,$segment,$fit,$judge)){Run-Tool $tool chain|Out-Null}
$lineCount=[int][double](Get-Out $fit LineCount); $total=[double](Get-Out $fit TotalLength); $snapshot=Get-Out $fit XLD
Assert-True ($fit.ResultCode -eq 'OK' -and $lineCount -eq 4 -and $total -gt 240 -and $total -lt 250) 'Real Region -> XLD -> segments -> fitted lines chain is wrong.'
Assert-True ($snapshot.ObjectCount -eq 4 -and [Math]::Abs($snapshot.TotalLength-$total) -lt .01 -and $judge.ResultCode -eq 'OK') 'Managed fitted XLD or NumericJudge output is inconsistent.'
Assert-True ((Get-Out $fit Parameters).Contains('ClipFactor=2') -and -not [string]::IsNullOrWhiteSpace([string](Get-Out $fit Algorithm))) 'Algorithm/parameter outputs missing.'

[HalconDotNet.HObject]$parallel=$null; [HalconDotNet.HObject]$line1=$null; [HalconDotNet.HObject]$line2=$null
try{
    [HalconDotNet.HOperatorSet]::GenContourPolygonXld([ref]$line1,[HalconDotNet.HTuple]@([double]10,[double]10.2,[double]9.8,[double]10.1,[double]35),[HalconDotNet.HTuple]@([double]5,[double]45,[double]85,[double]125,[double]65))
    [HalconDotNet.HOperatorSet]::GenContourPolygonXld([ref]$line2,[HalconDotNet.HTuple]@([double]30,[double]30.1,[double]29.9,[double]30),[HalconDotNet.HTuple]@([double]5,[double]45,[double]85,[double]125))
    [HalconDotNet.HOperatorSet]::ConcatObj($line1,$line2,[ref]$parallel)
    $direct=New-Tool XldLineFitting S22_Direct; Invoke-Fit $direct $parallel|Out-Null
    $directLineCount=[int][double](Get-Out $direct LineCount); $directTotal=[double](Get-Out $direct TotalLength)
    Assert-True ($directLineCount -eq 2 -and $directTotal -gt 170) "Synthetic parallel lines were not fitted (count=$directLineCount total=$directTotal)."
    Assert-True ([Math]::Abs([double](Get-Out $direct MeanAngle)) -lt 2 -and [double](Get-Out $direct MaxResidual) -gt 20) 'Robust angle or point-to-line residual is wrong for an outlier.'
    foreach($algorithm in @('Regression','Huber','Tukey','Drop','Gauss')){$direct.Parameters.XldLineFitAlgorithm=$algorithm; $algorithmRecord=Invoke-Fit $direct $parallel; Assert-True ($algorithmRecord.ResultCode -eq 'OK') "$algorithm failed."}
    $direct.Dispose()
}finally{if($parallel){$parallel.Dispose()};if($line1){$line1.Dispose()};if($line2){$line2.Dispose()}}

[HalconDotNet.HObject]$empty=$null; [HalconDotNet.HOperatorSet]::GenEmptyObj([ref]$empty); $emptyTool=New-Tool XldLineFitting S22_Empty
try{$record=Invoke-Fit $emptyTool $empty; Assert-True ($record.ResultCode -eq 'NG' -and [int][double](Get-Out $emptyTool LineCount) -eq 0) 'Empty XLD did not return managed NG.'}finally{$empty.Dispose();$emptyTool.Dispose()}
[HalconDotNet.HObject]$onePoint=$null; [HalconDotNet.HOperatorSet]::GenContourPolygonXld([ref]$onePoint,[HalconDotNet.HTuple]@([double]10),[HalconDotNet.HTuple]@([double]20)); $shortTool=New-Tool XldLineFitting S22_Short
try{$rejected=$false;try{$shortRecord=Invoke-Fit $shortTool $onePoint;$rejected=$shortRecord.ResultCode -eq 'NG'}catch{$rejected=$true};Assert-True $rejected 'One-point XLD was not rejected or returned as managed NG.'}finally{$onePoint.Dispose();$shortTool.Dispose()}

[HalconDotNet.HObject]$circle=$null; [HalconDotNet.HObject]$arcSegments=$null; $arcTool=New-Tool XldLineFitting S22_Arc
try{
    [HalconDotNet.HOperatorSet]::GenCircleContourXld([ref]$circle,40,50,20,0,6.283185307179586,'positive',1)
    [HalconDotNet.HOperatorSet]::SegmentContoursXld($circle,[ref]$arcSegments,'lines_circles',5,4,2)
    $rejected=$false;try{Invoke-Fit $arcTool $arcSegments|Out-Null}catch{$rejected=$true};Assert-True $rejected 'Arc segment was not rejected with guidance.'
}finally{if($circle){$circle.Dispose()};if($arcSegments){$arcSegments.Dispose()};$arcTool.Dispose()}

$invalid=New-Tool XldLineFitting S22_Invalid; [HalconDotNet.HObject]$validLine=$null
try{
    [HalconDotNet.HOperatorSet]::GenContourPolygonXld([ref]$validLine,[HalconDotNet.HTuple]@([double]1,[double]2),[HalconDotNet.HTuple]@([double]1,[double]20))
    foreach($case in @(@('XldLineFitAlgorithm','Bad'),@('XldLineFitMaxNumPoints',1),@('XldLineFitClippingEndPoints',-1),@('XldLineFitIterations',-1),@('XldLineFitClippingFactor',0))){$invalid.Parameters=$invalid.Parameters.Clone();$invalid.Parameters.$($case[0])=$case[1];$bad=$false;try{Invoke-Fit $invalid $validLine|Out-Null}catch{$bad=$true};Assert-True $bad "Invalid $($case[0]) was accepted.";$invalid.Parameters=[Activator]::CreateInstance($assembly.GetType('HalconWinFormsDemo.Models.VmToolParameterData',$true))}
}finally{if($validLine){$validLine.Dispose()};$invalid.Dispose()}

# A line: visible manual layout controls, alignment/distribution, optional recipe positions and legacy auto fallback.
$refreshLayout=$mainType.GetMethod('RefreshFlowGraphLayout',$private); $enableManual=$mainType.GetMethod('EnableManualFlowLayout',$private); $captureFlow=$mainType.GetMethod('CaptureFlowRecipe',$private); $applyFlow=$mainType.GetMethod('ApplyFlowFromRecipe',$private); $alignH=$mainType.GetMethod('AlignFlowToolsHorizontalButton_Click',$private); $alignV=$mainType.GetMethod('AlignFlowToolsVerticalButton_Click',$private); $distH=$mainType.GetMethod('DistributeFlowToolsHorizontalButton_Click',$private); $distV=$mainType.GetMethod('DistributeFlowToolsVerticalButton_Click',$private); $auto=$mainType.GetMethod('AutoLayoutFlowButton_Click',$private)
$refreshLayout.Invoke($window,@())|Out-Null; $enableManual.Invoke($window,@())|Out-Null
$source.GraphX=24;$source.GraphY=48;$threshold.GraphX=120;$threshold.GraphY=96;$contour.GraphX=312;$contour.GraphY=192
$beginNodeDrag=$mainType.GetMethod('BeginFlowNodeDrag',$private);$cancelNodeDrag=$mainType.GetMethod('CancelFlowNodeDrag',$private);$beforeDragX=$source.GraphX;$beforeDragY=$source.GraphY;$beginNodeDrag.Invoke($window,[object[]]@($source,[Windows.Point]::new($source.GraphX,$source.GraphY)))|Out-Null;Assert-True ($null -ne $mainType.GetField('flowNodeDragSession',$private).GetValue($window)) 'Node drag session did not start.';$source.GraphX=456;$source.GraphY=480;$cancelNodeDrag.Invoke($window,@())|Out-Null;Assert-True ($source.GraphX -eq $beforeDragX -and $source.GraphY -eq $beforeDragY) 'Escape/cancel path did not restore the original node position.'
$list=$mainType.GetField('FlowToolList',$all).GetValue($window); $list.SelectedItems.Clear(); foreach($tool in @($source,$threshold,$contour)){$list.SelectedItems.Add($tool)|Out-Null}
$alignH.Invoke($window,[object[]]@($null,$null))|Out-Null; Assert-True ($source.GraphY -eq $threshold.GraphY -and $threshold.GraphY -eq $contour.GraphY) 'Horizontal alignment failed.'
$source.GraphX=24;$threshold.GraphX=120;$contour.GraphX=312; $distH.Invoke($window,[object[]]@($null,$null))|Out-Null; Assert-True ([Math]::Abs(($threshold.GraphX-$source.GraphX)-($contour.GraphX-$threshold.GraphX)) -lt .1) 'Horizontal distribution failed.'
$source.GraphY=48;$threshold.GraphY=96;$contour.GraphY=240; $distV.Invoke($window,[object[]]@($null,$null))|Out-Null; Assert-True ([Math]::Abs(($threshold.GraphY-$source.GraphY)-($contour.GraphY-$threshold.GraphY)) -lt .1) 'Vertical distribution failed.'
$alignV.Invoke($window,[object[]]@($null,$null))|Out-Null; Assert-True ($source.GraphX -eq $threshold.GraphX -and $threshold.GraphX -eq $contour.GraphX) 'Vertical alignment failed.'
$recipeItems=$captureFlow.Invoke($window,@()); Assert-True (($recipeItems|Where-Object{$null -eq $_.GraphX -or $null -eq $_.GraphY}).Count -eq 0) 'Manual positions were not captured in recipe items.'
$visionRecipeType=$assembly.GetType('HalconWinFormsDemo.Models.VisionRecipe',$true); $manualRecipe=[Activator]::CreateInstance($visionRecipeType); $manualRecipe.ToolFlow=$recipeItems; $applyFlow.Invoke($window,[object[]]@($manualRecipe))|Out-Null; $restored=$flow[0]; Assert-True ($restored.HasManualGraphPosition -and $restored.GraphX -eq [double]$recipeItems[0].GraphX) 'Manual layout did not survive recipe reload.'
$auto.Invoke($window,[object[]]@($null,$null))|Out-Null; $autoItems=$captureFlow.Invoke($window,@()); Assert-True (($autoItems|Where-Object{$null -ne $_.GraphX -or $null -ne $_.GraphY}).Count -eq 0) 'Auto layout did not clear optional recipe positions.'
$legacy=[Activator]::CreateInstance($visionRecipeType); $legacy.ToolFlow=$autoItems; $applyFlow.Invoke($window,[object[]]@($legacy))|Out-Null; $refreshLayout.Invoke($window,@())|Out-Null; Assert-True (-not $mainType.GetField('isManualFlowLayout',$private).GetValue($window)) 'Legacy recipe without positions did not fall back to auto layout.'
foreach($name in @('FlowLayoutModeText','FlowSnapGridCheckBox','DockXldLineFittingPanel')){Assert-True ($null -ne $mainType.GetField($name,$all).GetValue($window)) "$name is missing."}

$dispose.Invoke($window,@())|Out-Null; $window.Close(); if(Test-Path $tempRoot){Remove-Item -LiteralPath $tempRoot -Recurse -Force}
Write-Output 'S22 node layout and HALCON XLD line fitting smoke passed.'
