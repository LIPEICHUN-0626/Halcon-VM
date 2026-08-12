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
function Write-PatternPgm([string]$Path, [int]$Width, [int]$Height, [bool]$Flat) {
    $header = [Text.Encoding]::ASCII.GetBytes("P5`n$Width $Height`n255`n")
    $pixels = New-Object byte[] ($Width * $Height)
    for ($row = 0; $row -lt $Height; $row++) {
        for ($column = 0; $column -lt $Width; $column++) {
            $value = if ($Flat) { 80 } else { 20 }
            if (-not $Flat -and $row -ge 18 -and $row -le 74 -and $column -ge 22 -and $column -le 104) { $value = 225 }
            if (-not $Flat -and (($row - 48) * ($row - 48) + ($column - 70) * ($column - 70)) -lt 14 * 14) { $value = 55 }
            $pixels[$row * $Width + $column] = [byte]$value
        }
    }
    $bytes = New-Object byte[] ($header.Length + $pixels.Length)
    [Array]::Copy($header, 0, $bytes, 0, $header.Length); [Array]::Copy($pixels, 0, $bytes, $header.Length, $pixels.Length)
    [IO.File]::WriteAllBytes($Path, $bytes)
}
function Write-ColorPpm([string]$Path, [int]$Width, [int]$Height) {
    $header = [Text.Encoding]::ASCII.GetBytes("P6`n$Width $Height`n255`n")
    $pixels = New-Object byte[] ($Width * $Height * 3)
    for ($row = 0; $row -lt $Height; $row++) {
        for ($column = 0; $column -lt $Width; $column++) {
            $inside = $row -ge 18 -and $row -le 74 -and $column -ge 22 -and $column -le 104
            $offset = ($row * $Width + $column) * 3
            $red = if ($inside) { 240 } else { 20 }; $green = if ($inside) { 180 } else { 30 }; $blue = if ($inside) { 80 } else { 40 }
            $pixels[$offset] = [byte]$red
            $pixels[$offset + 1] = [byte]$green
            $pixels[$offset + 2] = [byte]$blue
        }
    }
    $bytes = New-Object byte[] ($header.Length + $pixels.Length)
    [Array]::Copy($header, 0, $bytes, 0, $header.Length); [Array]::Copy($pixels, 0, $bytes, $header.Length, $pixels.Length)
    [IO.File]::WriteAllBytes($Path, $bytes)
}

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s38-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($tempRoot) | Out-Null
$grayPath = Join-Path $tempRoot 'pattern.pgm'; Write-PatternPgm $grayPath 128 96 $false
$flatPath = Join-Path $tempRoot 'flat.pgm'; Write-PatternPgm $flatPath 128 96 $true
$colorPath = Join-Path $tempRoot 'color.ppm'; Write-ColorPpm $colorPath 128 96

$mainType = $assembly.GetType('HalconWinFormsDemo.MainWindow', $true)
$recipeServiceType = $assembly.GetType('HalconWinFormsDemo.Services.RecipeService', $true)
$loggerType = $assembly.GetType('HalconWinFormsDemo.Services.AppLogger', $true)
$kindType = $assembly.GetType('HalconWinFormsDemo.Models.VmToolKind', $true)
$metadataType = $assembly.GetType('HalconWinFormsDemo.Models.ToolMetadata', $true)
$roiType = $assembly.GetType('HalconWinFormsDemo.Models.RoiData', $true)
$private = [Reflection.BindingFlags]'Instance,NonPublic'; $all = [Reflection.BindingFlags]'Instance,NonPublic,Public'; $staticPublic = [Reflection.BindingFlags]'Static,Public'
$constructorArgs = New-Object 'object[]' 1; $constructorArgs[0] = $tempRoot.ToString()
$service = $recipeServiceType.GetConstructor([Type[]]@([string])).Invoke($constructorArgs)
$logger = $loggerType.GetConstructor([Type[]]@([string])).Invoke($constructorArgs)
$window = $mainType.GetConstructor($private, $null, [Type[]]@($recipeServiceType, $loggerType), $null).Invoke([object[]]@($service, $logger))
$flow = $mainType.GetField('flowTools', $private).GetValue($window); $catalog = $mainType.GetField('toolCatalog', $private).GetValue($window)
$create = $mainType.GetMethod('CreateFlowTool', $private); $execute = $mainType.GetMethod('ExecuteFlowTool', $private); $disposeFlow = $mainType.GetMethod('DisposeFlowTools', $private)
$runSubpixel = $mainType.GetMethod('RunSubpixelEdgeExtractionTool', $private); $loadDock = $mainType.GetMethod('LoadDockConfigurationDraft', $private)
$applyDock = $mainType.GetMethod('ApplyDockConfigurationDraft', $private); $trialDock = $mainType.GetMethod('DockTrialRunButton_Click', $private)
$resetCorners = $mainType.GetMethod('DockProjectiveResetCornersButton_Click', $private); $copyCorners = $mainType.GetMethod('DockProjectiveCopySourceToTargetButton_Click', $private)
$refreshProjective = $mainType.GetMethod('RefreshProjectiveGeometryPreviewFromDock', $private); $updateProjectiveDrag = $mainType.GetMethod('UpdateProjectiveGeometryDrag', $private)
$finishProjectiveDrag = $mainType.GetMethod('FinishProjectiveGeometryDrag', $private); $undo = $mainType.GetMethod('UndoFlowEditButton_Click', $private); $redo = $mainType.GetMethod('RedoFlowEditButton_Click', $private)
$captureRecipe = $mainType.GetMethod('CaptureRecipe', $private)
function Field([string]$Name) { $mainType.GetField($Name, $all).GetValue($window) }
function New-Tool([string]$Kind, [string]$Name) { $create.Invoke($window, [object[]]@([Enum]::Parse($kindType, $Kind), $Name, $true, $null)) }
function Invoke-Execute($Tool, [string]$Source) { try { $execute.Invoke($window, [object[]]@($Tool, $Source)) } catch [Reflection.TargetInvocationException] { throw $_.Exception.InnerException } }
function Invoke-Subpixel($Tool, $Image, $Region) { try { $runSubpixel.Invoke($window, [object[]]@($Tool, $Image, $Region)) } catch [Reflection.TargetInvocationException] { throw $_.Exception.InnerException } }
function Get-Out($Tool, [string]$Port) { $value = $null; if (-not $Tool.TryGetOutputValue($Port, [ref]$value)) { throw "$($Tool.InstanceName).$Port missing" }; $value }

foreach ($name in @('DockProjectiveImageEditToggleButton','DockProjectiveGeometryStatusText','DockSubpixelEdgeExtractionPanel','DockSubpixelEdgeFilterComboBox','DockSubpixelEdgeAlphaTextBox','DockSubpixelEdgeLowTextBox','DockSubpixelEdgeHighTextBox','DockSubpixelEdgeColorPolicyComboBox')) { Assert-True($null -ne (Field $name)) "$name is missing." }
Assert-True($null -ne ($catalog | Where-Object { $_.Kind.ToString() -eq 'SubpixelEdgeExtraction' } | Select-Object -First 1)) 'Subpixel edge tool is missing from toolbox.'
$edgeKind = [Enum]::Parse($kindType, 'SubpixelEdgeExtraction'); $inputs = $metadataType.GetMethod('GetInputPorts', $staticPublic).Invoke($null, [object[]]@($edgeKind)); $outputs = $metadataType.GetMethod('GetOutputPorts', $staticPublic).Invoke($null, [object[]]@($edgeKind))
Assert-True(($inputs | Where-Object { $_.PortName -eq 'Image' -and $_.DataType -eq 'Image' -and -not $_.IsOptional }).Count -eq 1) 'Subpixel edge required Image input is missing.'
Assert-True(($inputs | Where-Object { $_.PortName -eq 'ROI' -and $_.DataType -eq 'Region' -and $_.IsOptional }).Count -eq 1) 'Subpixel edge optional ROI input is missing.'
foreach ($port in @('XLD','TotalLength','ContourCount','Filter','ColorPolicy','Parameters','ResultCode')) { Assert-True(($outputs | Where-Object PortName -eq $port).Count -eq 1) "Subpixel edge output $port is missing." }
Assert-True(($outputs | Where-Object { $_.PortName -eq 'TotalLength' -and $_.DataType -eq 'Number' }).Count -eq 1) 'Subpixel edge TotalLength is not a typed Number output.'
Assert-True(($outputs | Where-Object { $_.PortName -eq 'ContourCount' -and $_.DataType -eq 'Number' }).Count -eq 1) 'Subpixel edge ContourCount is not a typed Number output.'
foreach ($kindName in @('XldContourFilter','XldContourSegmentation','XldLineFitting','XldCircleFitting','XldEllipseFitting','XldRectangleFitting')) { $ports = $metadataType.GetMethod('GetInputPorts', $staticPublic).Invoke($null, [object[]]@([Enum]::Parse($kindType, $kindName))); Assert-True(($ports | Where-Object { $_.PortName -eq 'XLD' -and $_.DataType -eq 'XLD' }).Count -eq 1) "$kindName is not compatible with XLD." }
$lineRelationPorts = $metadataType.GetMethod('GetInputPorts', $staticPublic).Invoke($null, [object[]]@([Enum]::Parse($kindType, 'XldLineRelationMeasurement')))
foreach ($portName in @('LineA','LineB')) { Assert-True(($lineRelationPorts | Where-Object { $_.PortName -eq $portName -and $_.DataType -eq 'XLD' }).Count -eq 1) "Line relation $portName is not compatible with XLD." }
$numericJudgePorts = $metadataType.GetMethod('GetInputPorts', $staticPublic).Invoke($null, [object[]]@([Enum]::Parse($kindType, 'NumericJudge')))
Assert-True(($numericJudgePorts | Where-Object { $_.PortName -eq 'Value' -and $_.DataType -eq 'Number' }).Count -eq 1) 'NumericJudge is not compatible with the subpixel numeric outputs.'

$app = [Windows.Application]::Current; if ($null -eq $app) { $app = New-Object Windows.Application; $app.ShutdownMode = [Windows.ShutdownMode]::OnExplicitShutdown }
$disposeFlow.Invoke($window, @()) | Out-Null; $flow.Clear()
$source = New-Tool ImageSource 'S38 Source'; $projective = New-Tool ImageProjectiveTransform 'S38 Projective'; $edge = New-Tool SubpixelEdgeExtraction 'S38 Subpixel'; $filter = New-Tool XldContourFilter 'S38 XLD Filter'; $segment = New-Tool XldContourSegmentation 'S38 XLD Segment'
$lineFit = New-Tool XldLineFitting 'S38 Line Fit'; $circleFit = New-Tool XldCircleFitting 'S38 Circle Fit'; $ellipseFit = New-Tool XldEllipseFitting 'S38 Ellipse Fit'; $rectangleFit = New-Tool XldRectangleFitting 'S38 Rectangle Fit'; $lineRelation = New-Tool XldLineRelationMeasurement 'S38 Line Relation'; $numericJudge = New-Tool NumericJudge 'S38 Numeric Judge'
$source.Parameters.LocalImagePath = $grayPath; $projective.SetInputBinding('Image', $source.ToolId, 'Image'); $edge.SetInputBinding('Image', $projective.ToolId, 'Image'); $filter.SetInputBinding('XLD', $edge.ToolId, 'XLD'); $segment.SetInputBinding('XLD', $filter.ToolId, 'XLD')
foreach ($fit in @($lineFit,$circleFit,$ellipseFit,$rectangleFit)) { $fit.SetInputBinding('XLD', $edge.ToolId, 'XLD') }
$lineRelation.SetInputBinding('LineA', $edge.ToolId, 'XLD'); $lineRelation.SetInputBinding('LineB', $filter.ToolId, 'XLD'); $numericJudge.SetInputBinding('Value', $edge.ToolId, 'TotalLength')
$projective.Parameters.ImageProjectiveSourceColumn2 = 127; $projective.Parameters.ImageProjectiveSourceRow3 = 95; $projective.Parameters.ImageProjectiveSourceColumn3 = 127; $projective.Parameters.ImageProjectiveSourceRow4 = 95
$projective.Parameters.ImageProjectiveTargetColumn2 = 127; $projective.Parameters.ImageProjectiveTargetRow3 = 95; $projective.Parameters.ImageProjectiveTargetColumn3 = 127; $projective.Parameters.ImageProjectiveTargetRow4 = 95
$filter.Parameters.XldContourMin = 2; $filter.Parameters.XldContourMax = 100000; $segment.Parameters.XldSegmentSmooth = 3
foreach ($tool in @($source,$projective,$edge,$filter,$segment,$lineFit,$circleFit,$ellipseFit,$rectangleFit,$lineRelation,$numericJudge)) { $flow.Add($tool) }
foreach ($bindingCheck in @(
    @($lineFit,'XLD',$edge.ToolId,'XLD'), @($circleFit,'XLD',$edge.ToolId,'XLD'), @($ellipseFit,'XLD',$edge.ToolId,'XLD'), @($rectangleFit,'XLD',$edge.ToolId,'XLD'),
    @($lineRelation,'LineA',$edge.ToolId,'XLD'), @($lineRelation,'LineB',$filter.ToolId,'XLD'), @($numericJudge,'Value',$edge.ToolId,'TotalLength')
)) {
    $binding = $bindingCheck[0].GetInputBinding($bindingCheck[1])
    Assert-True($null -ne $binding -and $binding.SourceToolId -eq $bindingCheck[2] -and $binding.SourcePortName -eq $bindingCheck[3]) "Typed downstream binding $($bindingCheck[0].InstanceName).$($bindingCheck[1]) was not persisted."
}
$window.WindowStartupLocation = [Windows.WindowStartupLocation]::Manual; $window.Left = 2; $window.Top = 2; $window.Width = 1760; $window.Height = 980; $window.Topmost = $true; $window.Show(); $window.Activate() | Out-Null; Pump-Ui 500

# B line: every installed HALCON 20.11 filter, RGB policy, reduced domain, empty result, lifecycle, and typed downstream.
$gray = [HalconDotNet.HImage]::new($grayPath); $direct = New-Tool SubpixelEdgeExtraction 'S38 Direct Edge'
try {
    foreach ($edgeFilter in @('canny','deriche1','lanser1','deriche2','lanser2','shen','mshen','sobel','sobel_fast')) {
        $direct.Parameters.SubpixelEdgeFilter = $edgeFilter; $direct.Parameters.SubpixelEdgeAlpha = 1.0; $direct.Parameters.SubpixelEdgeLow = 5; $direct.Parameters.SubpixelEdgeHigh = 10
        Invoke-Subpixel $direct $gray $null | Out-Null
        Assert-True([int](Get-Out $direct ContourCount) -gt 0) "$edgeFilter returned no XLD on a strong edge pattern."
        Assert-True((Get-Out $direct Filter) -eq $edgeFilter) "$edgeFilter was not reported exactly."
    }
    $old = Get-Out $direct XLD; Invoke-Subpixel $direct $gray $null | Out-Null; Assert-True $old.IsDisposed 'Rerun did not release the previous managed XLD snapshot.'
    $roiData = $roiType.GetMethod('CreateRectangle', $staticPublic).Invoke($null, [object[]]@([double]10,[double]10,[double]60,[double]82)); try { Invoke-Subpixel $direct $gray $roiData.Region | Out-Null; Assert-True((Get-Out $direct Parameters).Contains('ReducedDomain(ROI-Mask)')) 'Reduced-domain ROI execution was not reported.' } finally { $roiData.Dispose() }
    $direct.Parameters.SubpixelEdgeLow = 0; $rejected = $false; try { Invoke-Subpixel $direct $gray $null | Out-Null } catch { $rejected = $true }; Assert-True $rejected 'Low=0 was accepted.'; $direct.Parameters.SubpixelEdgeLow = 5; $direct.Parameters.SubpixelEdgeHigh = 4; $rejected = $false; try { Invoke-Subpixel $direct $gray $null | Out-Null } catch { $rejected = $true }; Assert-True $rejected 'High<Low was accepted.'; $direct.Parameters.SubpixelEdgeHigh = 10
} finally { $gray.Dispose(); $direct.Dispose() }
$rgb = [HalconDotNet.HImage]::new($colorPath); $rgbTool = New-Tool SubpixelEdgeExtraction 'S38 RGB Edge'; try { $rgbTool.Parameters.SubpixelEdgeLow=5;$rgbTool.Parameters.SubpixelEdgeHigh=10;$rgbTool.Parameters.SubpixelEdgeColorPolicy='AutoGray';Invoke-Subpixel $rgbTool $rgb $null|Out-Null;Assert-True([int](Get-Out $rgbTool ContourCount)-gt0) 'RGB AutoGray produced no XLD.';$rgbTool.Parameters.SubpixelEdgeColorPolicy='RequireSingleChannel';$rejected=$false;try{Invoke-Subpixel $rgbTool $rgb $null|Out-Null}catch{$rejected=$true};Assert-True $rejected 'RequireSingleChannel accepted RGB.' } finally { $rgb.Dispose(); $rgbTool.Dispose() }
$flat = [HalconDotNet.HImage]::new($flatPath); $flatTool = New-Tool SubpixelEdgeExtraction 'S38 Empty Edge'; try { $flatTool.Parameters.SubpixelEdgeLow=5;$flatTool.Parameters.SubpixelEdgeHigh=10;$record=Invoke-Subpixel $flatTool $flat $null;Assert-True($record.ResultCode-eq'NG'-and[int](Get-Out $flatTool ContourCount)-eq0) 'Flat image empty-edge semantics are wrong.' } finally { $flat.Dispose(); $flatTool.Dispose() }
foreach ($tool in @($source,$projective,$edge,$filter,$segment)) { Invoke-Execute $tool 'S38 flow' | Out-Null }; Assert-True($edge.ResultCode-eq'OK'-and$filter.ResultCode-eq'OK'-and$segment.ResultCode-eq'OK') 'Subpixel XLD did not drive the real downstream filter/segment chain.'

# A line: separate source/target dimensions, drag-only draft, trial run, apply, undo/redo, reset/copy, invalid status, and recipe.
(Field 'FlowToolList').SelectedItem = $projective; $loadDock.Invoke($window, [object[]]@($projective)) | Out-Null; (Field 'DockProjectiveOutputModeComboBox').SelectedValue='ExplicitSize';(Field 'DockProjectiveOutputWidthTextBox').Text='80';(Field 'DockProjectiveOutputHeightTextBox').Text='60';$resetCorners.Invoke($window,[object[]]@($null,$null))|Out-Null
Assert-True((Field 'DockProjectiveSourceColumn2TextBox').Text-eq'127') 'Source reset did not use input coordinate width.';Assert-True((Field 'DockProjectiveTargetColumn2TextBox').Text-eq'79') 'Target reset did not use output coordinate width.'
$appliedSourceRow1=$projective.Parameters.ImageProjectiveSourceRow1;$mainType.GetField('projectiveGeometryEditActive',$private).SetValue($window,$true);$refreshProjective.Invoke($window,@())|Out-Null;$mainType.GetField('projectiveGeometryDragging',$private).SetValue($window,$true);$mainType.GetField('projectiveGeometryDragSide',$private).SetValue($window,'Source');$mainType.GetField('projectiveGeometryDragPointIndex',$private).SetValue($window,0)
$moved=[Drawing.PointF]::new(13,11);Assert-True([bool]$updateProjectiveDrag.Invoke($window,[object[]]@($true,$moved))) 'Source point drag helper did not update.';$finishProjectiveDrag.Invoke($window,[object[]]@($true,$null))|Out-Null;Assert-True([double](Field 'DockProjectiveSourceRow1TextBox').Text-eq11-and[double](Field 'DockProjectiveSourceColumn1TextBox').Text-eq13) 'Image drag did not synchronize numeric point fields.';Assert-True($projective.Parameters.ImageProjectiveSourceRow1-eq$appliedSourceRow1) 'Image drag changed applied parameters before Apply.'
$beforeTrial=$projective.Parameters.ImageProjectiveSourceRow1;$trialDock.Invoke($window,[object[]]@($null,$null))|Out-Null;Assert-True($projective.Parameters.ImageProjectiveSourceRow1-eq$beforeTrial) 'Trial run applied the projective draft.'
Assert-True([bool]$applyDock.Invoke($window,[object[]]@($true))) 'Projective draft Apply failed.';Assert-True($projective.Parameters.ImageProjectiveSourceRow1-eq11) 'Applied projective draft did not enter the tool instance.';$undo.Invoke($window,[object[]]@($null,$null))|Out-Null;$restoredProjective=@($mainType.GetField('flowTools',$private).GetValue($window))|Where-Object ToolId -eq $projective.ToolId|Select-Object -First 1;Assert-True($restoredProjective.Parameters.ImageProjectiveSourceRow1-eq$appliedSourceRow1) 'Projective Apply undo failed.';$redo.Invoke($window,[object[]]@($null,$null))|Out-Null;$restoredProjective=@($mainType.GetField('flowTools',$private).GetValue($window))|Where-Object ToolId -eq $projective.ToolId|Select-Object -First 1;Assert-True($restoredProjective.Parameters.ImageProjectiveSourceRow1-eq11) 'Projective Apply redo failed.'
$restoredProjective=$restoredProjective.PSObject.BaseObject;(Field 'FlowToolList').SelectedItem=$restoredProjective;$loadDock.Invoke($window,[object[]]@($restoredProjective))|Out-Null;(Field 'DockProjectiveSourceRow2TextBox').Text=(Field 'DockProjectiveSourceRow1TextBox').Text;(Field 'DockProjectiveSourceColumn2TextBox').Text=(Field 'DockProjectiveSourceColumn1TextBox').Text;$refreshProjective.Invoke($window,@())|Out-Null;Assert-True((Field 'DockProjectiveGeometryStatusText').Foreground.ToString()-eq'#FFFF0000') 'Duplicate-point geometry was not shown as an error in the parameter panel.';$resetCorners.Invoke($window,[object[]]@($null,$null))|Out-Null;$copyCorners.Invoke($window,[object[]]@($null,$null))|Out-Null
$recipe=$captureRecipe.Invoke($window,@());$saved=$recipe.ToolFlow|Where-Object ToolType -eq 'SubpixelEdgeExtraction'|Select-Object -First 1;Assert-True($saved.Parameters.SubpixelEdgeFilter-eq'canny'-and$saved.InputBindings.Count-ge1) 'Subpixel edge parameters/binding did not enter recipe.'

$mainType.GetField('recipeCloseConfirmed',$private).SetValue($window,$true);$window.Close();$app.Shutdown();if(Test-Path $tempRoot){Remove-Item -LiteralPath $tempRoot -Recurse -Force}
Write-Output 'S38 projective image editor and HALCON subpixel edge extraction smoke tests passed.'
