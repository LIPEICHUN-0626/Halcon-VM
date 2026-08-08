param([string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot))

$ErrorActionPreference = 'Stop'
if ([Threading.Thread]::CurrentThread.ApartmentState -ne [Threading.ApartmentState]::STA) { throw 'Run with powershell.exe -STA.' }
$halconRoot = 'C:\Program Files\MVTec\HALCON-20.11-Progress'
$env:PATH = (Join-Path $halconRoot 'bin\x64-win64') + ';' + $env:PATH
Add-Type -AssemblyName PresentationFramework
[Reflection.Assembly]::LoadFrom((Join-Path $halconRoot 'bin\dotnet35\halcondotnet.dll')) | Out-Null
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path $ProjectRoot 'bin\x64\Debug\HalconWinFormsDemo.exe'))

function Assert-True([bool]$Condition, [string]$Message) { if (-not $Condition) { throw "Assertion failed: $Message" } }
function Write-BandPgm([string]$Path, [int]$Width, [int]$Height, [string]$Mode, [int]$Delta = 200) {
    $header = [Text.Encoding]::ASCII.GetBytes("P5`n$Width $Height`n255`n")
    $pixels = New-Object byte[] ($Width * $Height)
    for ($row = 0; $row -lt $Height; $row++) {
        for ($column = 0; $column -lt $Width; $column++) {
            $value = 20
            if ($Mode -eq 'Vertical' -and (($column -ge 65 -and $column -le 95) -or ($column -ge 135 -and $column -le 170))) { $value = [Math]::Min(255, 20 + $Delta) }
            if ($Mode -eq 'Horizontal' -and (($row -ge 65 -and $row -le 95) -or ($row -ge 135 -and $row -le 170))) { $value = [Math]::Min(255, 20 + $Delta) }
            if ($Mode -eq 'Uniform') { $value = 100 }
            $pixels[$row * $Width + $column] = [byte]$value
        }
    }
    $bytes = New-Object byte[] ($header.Length + $pixels.Length)
    [Array]::Copy($header, 0, $bytes, 0, $header.Length)
    [Array]::Copy($pixels, 0, $bytes, $header.Length, $pixels.Length)
    [IO.File]::WriteAllBytes($Path, $bytes)
}

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s27-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($tempRoot) | Out-Null
$verticalPath = Join-Path $tempRoot 'vertical-pairs.pgm'
$horizontalPath = Join-Path $tempRoot 'horizontal-pairs.pgm'
$uniformPath = Join-Path $tempRoot 'uniform.pgm'
$lowContrastPath = Join-Path $tempRoot 'low-contrast.pgm'
Write-BandPgm $verticalPath 240 240 Vertical 200
Write-BandPgm $horizontalPath 240 240 Horizontal 200
Write-BandPgm $uniformPath 240 240 Uniform 0
Write-BandPgm $lowContrastPath 240 240 Vertical 5

$mainType = $assembly.GetType('HalconWinFormsDemo.MainWindow', $true)
$recipeServiceType = $assembly.GetType('HalconWinFormsDemo.Services.RecipeService', $true)
$loggerType = $assembly.GetType('HalconWinFormsDemo.Services.AppLogger', $true)
$toolKindType = $assembly.GetType('HalconWinFormsDemo.Models.VmToolKind', $true)
$visionRecipeType = $assembly.GetType('HalconWinFormsDemo.Models.VisionRecipe', $true)
$private = [Reflection.BindingFlags]'Instance,NonPublic'
$all = [Reflection.BindingFlags]'Instance,NonPublic,Public'
$staticPublic = [Reflection.BindingFlags]'Static,Public'
$constructorArgs = New-Object 'object[]' 1
$constructorArgs[0] = $tempRoot.ToString()
$service = $recipeServiceType.GetConstructor([Type[]]@([string])).Invoke($constructorArgs)
$logger = $loggerType.GetConstructor([Type[]]@([string])).Invoke($constructorArgs)
$window = $mainType.GetConstructor($private, $null, [Type[]]@($recipeServiceType, $loggerType), $null).Invoke([object[]]@($service, $logger))
$flow = $mainType.GetField('flowTools', $private).GetValue($window)
$catalog = $mainType.GetField('toolCatalog', $private).GetValue($window)
$layers = $mainType.GetField('resultOverlayLayers', $private).GetValue($window)
$create = $mainType.GetMethod('CreateFlowTool', $private)
$runCaliper = $mainType.GetMethod('RunCaliperEdgePairTool', $private)
$execute = $mainType.GetMethod('ExecuteFlowTool', $private)
$refreshLayers = $mainType.GetMethod('RefreshResultOverlayLayers', $private)
$setAllLayers = $mainType.GetMethod('SetAllResultLayerVisibility', $private)
$moveLayer = $mainType.GetMethod('MoveSelectedResultLayer', $private)
$isolateLayer = $mainType.GetMethod('IsolateResultLayerButton_Click', $private)
$clearOverlay = $mainType.GetMethod('ClearOverlayButton_Click', $private)
$captureFlow = $mainType.GetMethod('CaptureFlowRecipe', $private)
$loadDraft = $mainType.GetMethod('LoadDockConfigurationDraft', $private)
$disposeFlow = $mainType.GetMethod('DisposeFlowTools', $private)
$resultList = $mainType.GetField('ResultLayerList', $all).GetValue($window)

function New-Tool([string]$Kind, [string]$Name, [bool]$Enabled = $true) {
    $create.Invoke($window, [object[]]@([Enum]::Parse($toolKindType, $Kind), $Name, $Enabled, $null))
}
function Get-Out($Tool, [string]$Port) {
    $value = $null
    if (-not $Tool.TryGetOutputValue($Port, [ref]$value)) { throw "$($Tool.InstanceName).$Port missing" }
    $value
}
function Invoke-Caliper($Tool, $Image) {
    try { $runCaliper.Invoke($window, [object[]]@($Tool, $Image)) }
    catch [Reflection.TargetInvocationException] { throw $_.Exception.InnerException }
}
function Invoke-Execute($Tool, [string]$Source) {
    try { $execute.Invoke($window, [object[]]@($Tool, $Source)) }
    catch [Reflection.TargetInvocationException] { throw $_.Exception.InnerException }
}

foreach ($fieldName in @(
    'ImageLayerTabs','ResultLayerList','ResultLayerCountText','DockCaliperEdgePairPanel',
    'DockCaliperRowTextBox','DockCaliperColumnTextBox','DockCaliperPhiTextBox',
    'DockCaliperLength1TextBox','DockCaliperLength2TextBox','DockCaliperSigmaTextBox',
    'DockCaliperThresholdTextBox','DockCaliperTransitionComboBox','DockCaliperSelectComboBox'
)) {
    Assert-True ($null -ne $mainType.GetField($fieldName, $all).GetValue($window)) "$fieldName is missing."
}
Assert-True ($null -ne ($catalog | Where-Object { $_.Kind.ToString() -eq 'CaliperEdgePairMeasurement' } | Select-Object -First 1)) 'Caliper tool missing from toolbox.'

$metadata = $assembly.GetType('HalconWinFormsDemo.Models.ToolMetadata', $true)
$caliperKind = [Enum]::Parse($toolKindType, 'CaliperEdgePairMeasurement')
$caliperInputs = $metadata.GetMethod('GetInputPorts', $staticPublic).Invoke($null, [object[]]@($caliperKind))
$caliperOutputs = $metadata.GetMethod('GetOutputPorts', $staticPublic).Invoke($null, [object[]]@($caliperKind))
Assert-True (($caliperInputs | Where-Object { $_.PortName -eq 'Image' -and $_.DataType -eq 'Image' }).Count -eq 1) 'Typed Image input is missing.'
foreach ($port in @('XLD','PrimaryDistance','PairCount','FirstRows','FirstColumns','FirstAmplitudes','SecondRows','SecondColumns','SecondAmplitudes','IntraDistances','InterDistances','Parameters','ResultCode')) {
    Assert-True (($caliperOutputs | Where-Object PortName -eq $port).Count -eq 1) "Caliper output $port is missing."
}

$caliper = New-Tool CaliperEdgePairMeasurement 'S27 Caliper'
$flow.Add($caliper)
$loadDraft.Invoke($window, [object[]]@($caliper)) | Out-Null
$dock = $mainType.GetField('DockCaliperEdgePairPanel', $all).GetValue($window)
Assert-True ($dock.Visibility.ToString() -eq 'Visible') 'Caliper parameters are not visible in the persistent configuration dock.'

[HalconDotNet.HImage]$vertical = $null
[HalconDotNet.HImage]$horizontal = $null
[HalconDotNet.HImage]$uniform = $null
[HalconDotNet.HImage]$lowContrast = $null
try {
    $vertical = [HalconDotNet.HImage]::new($verticalPath)
    $horizontal = [HalconDotNet.HImage]::new($horizontalPath)
    $uniform = [HalconDotNet.HImage]::new($uniformPath)
    $lowContrast = [HalconDotNet.HImage]::new($lowContrastPath)

    $p = $caliper.Parameters
    $p.CaliperRow = 120; $p.CaliperColumn = 120; $p.CaliperPhiDegrees = 0
    $p.CaliperLength1 = 90; $p.CaliperLength2 = 18; $p.CaliperSigma = 1
    $p.CaliperThreshold = 20; $p.CaliperTransition = 'Positive'; $p.CaliperSelect = 'All'
    $record = Invoke-Caliper $caliper $vertical
    $caliper.ResultCode = $record.ResultCode
    Assert-True ($record.ResultCode -eq 'OK') 'Positive transition did not find edge pairs.'
    Assert-True ([double](Get-Out $caliper PairCount) -ge 2) 'Multiple positive edge pairs were not found.'
    Assert-True ([double](Get-Out $caliper PrimaryDistance) -gt 25) 'Primary pair distance is not plausible.'
    Assert-True ((Get-Out $caliper FirstRows).StartsWith('[') -and (Get-Out $caliper IntraDistances).StartsWith('[')) 'Per-pair arrays were not published.'
    Assert-True ((Get-Out $caliper XLD).ObjectCount -ge 5) 'Measurement rectangle, edge crosses, and pair lines were not published as XLD.'

    $oldXld = Get-Out $caliper XLD
    $p.CaliperTransition = 'Negative'; $p.CaliperSelect = 'All'
    $negative = Invoke-Caliper $caliper $vertical
    $caliper.ResultCode = $negative.ResultCode
    Assert-True $oldXld.IsDisposed 'Rerun did not release the previous managed caliper XLD.'
    Assert-True ($negative.ResultCode -eq 'OK' -and [double](Get-Out $caliper PairCount) -ge 1) 'Negative transition did not find a valid pair.'

    $p.CaliperTransition = 'All'; $p.CaliperSelect = 'First'
    $first = Invoke-Caliper $caliper $vertical
    Assert-True ($first.ResultCode -eq 'OK' -and [double](Get-Out $caliper PairCount) -eq 1) 'Select=first did not return one pair.'
    $p.CaliperSelect = 'Last'
    $last = Invoke-Caliper $caliper $vertical
    Assert-True ($last.ResultCode -eq 'OK' -and [double](Get-Out $caliper PairCount) -eq 1) 'Select=last did not return one pair.'

    $p.CaliperRow = 120; $p.CaliperColumn = 120; $p.CaliperPhiDegrees = 90
    $p.CaliperLength1 = 90; $p.CaliperLength2 = 18; $p.CaliperTransition = 'Positive'; $p.CaliperSelect = 'All'
    $rotated = Invoke-Caliper $caliper $horizontal
    Assert-True ($rotated.ResultCode -eq 'OK' -and [double](Get-Out $caliper PairCount) -ge 2) 'Rotated 90-degree caliper did not find horizontal edge pairs.'

    $p.CaliperPhiDegrees = 0; $p.CaliperTransition = 'All'; $p.CaliperSelect = 'All'
    $missing = Invoke-Caliper $caliper $uniform
    Assert-True ($missing.ResultCode -eq 'NG' -and [double](Get-Out $caliper PairCount) -eq 0) 'Uniform missing-edge case did not return a clean NG.'
    Assert-True ((Get-Out $caliper XLD).ObjectCount -eq 1) 'NG case must retain the measurement rectangle overlay.'
    $low = Invoke-Caliper $caliper $lowContrast
    Assert-True ($low.ResultCode -eq 'NG') 'Low-contrast case was not rejected by the threshold.'

    foreach ($case in @(
        @('CaliperLength1',0),
        @('CaliperLength2',0),
        @('CaliperSigma',0.2),
        @('CaliperThreshold',0),
        @('CaliperTransition','Bad'),
        @('CaliperSelect','Bad'),
        @('CaliperPhiDegrees',181)
    )) {
        $bad = New-Tool CaliperEdgePairMeasurement 'S27 Invalid'
        $bad.Parameters.CaliperRow = 120; $bad.Parameters.CaliperColumn = 120
        $bad.Parameters.CaliperLength1 = 60; $bad.Parameters.CaliperLength2 = 10
        $bad.Parameters.CaliperSigma = 1; $bad.Parameters.CaliperThreshold = 20
        $bad.Parameters.$($case[0]) = $case[1]
        $rejected = $false
        try { Invoke-Caliper $bad $vertical | Out-Null } catch { $rejected = $true }
        Assert-True $rejected "Invalid $($case[0]) was accepted."
        $bad.Dispose()
    }

    $p.CaliperRow = 2; $p.CaliperColumn = 2; $p.CaliperLength1 = 50; $p.CaliperLength2 = 10
    $rejected = $false
    $boundsMessage = ''
    try { Invoke-Caliper $caliper $vertical | Out-Null } catch {
        $rejected = $true
        $boundsMessage = $_.Exception.ToString()
        if ($null -ne $_.Exception.InnerException) { $boundsMessage += ' ' + $_.Exception.InnerException.ToString() }
    }
    Assert-True $rejected 'Out-of-bounds rotated rectangle was not rejected clearly.'
    Assert-True ($boundsMessage -match 'bilinear') ('Out-of-bounds error does not explain the bilinear safety boundary: ' + $boundsMessage)

    $p.CaliperRow = 120; $p.CaliperColumn = 120; $p.CaliperLength1 = 90; $p.CaliperLength2 = 18
    $p.CaliperSigma = 1; $p.CaliperThreshold = 20; $p.CaliperTransition = 'Positive'; $p.CaliperSelect = 'All'
    for ($index = 0; $index -lt 25; $index++) {
        $repeat = Invoke-Caliper $caliper $vertical
        Assert-True ($repeat.ResultCode -eq 'OK') "Repeated handle lifecycle run $index failed."
    }
}
finally {
    foreach ($image in @($vertical,$horizontal,$uniform,$lowContrast)) { if ($null -ne $image) { $image.Dispose() } }
}

$disposeFlow.Invoke($window, @()) | Out-Null
$flow.Clear()
$source = New-Tool ImageSource 'S27 Source'
$threshold = New-Tool ImageThreshold 'S27 Threshold'
$caliperFlow = New-Tool CaliperEdgePairMeasurement 'S27 Flow Caliper'
$judge = New-Tool NumericJudge 'S27 Distance Judge'
foreach ($tool in @($source,$threshold,$caliperFlow,$judge)) { $flow.Add($tool) }
$source.Parameters.LocalImagePath = $verticalPath
$source.Parameters.LocalImageSerialNumber = 'S27-PAIR'
$threshold.Parameters.ImageThresholdMinGray = 100
$threshold.Parameters.ImageThresholdMaxGray = 255
$threshold.SetInputBinding('Image',$source.ToolId,'Image')
$caliperFlow.Parameters.CaliperRow = 120; $caliperFlow.Parameters.CaliperColumn = 120
$caliperFlow.Parameters.CaliperPhiDegrees = 0; $caliperFlow.Parameters.CaliperLength1 = 90; $caliperFlow.Parameters.CaliperLength2 = 18
$caliperFlow.Parameters.CaliperSigma = 1; $caliperFlow.Parameters.CaliperThreshold = 20
$caliperFlow.Parameters.CaliperTransition = 'Positive'; $caliperFlow.Parameters.CaliperSelect = 'All'
$caliperFlow.SetInputBinding('Image',$source.ToolId,'Image')
$judge.InputToolId = $caliperFlow.ToolId
$judge.InputPortName = 'PrimaryDistance'
$judge.SetInputBinding('Value',$caliperFlow.ToolId,'PrimaryDistance')
$judge.NumericOperator = 'BetweenInclusive'
$judge.NumericLowerLimit = 25
$judge.NumericUpperLimit = 40

$orphan = New-Tool CaliperEdgePairMeasurement 'S27 Missing Image'
$missingImageRejected = $false
try { Invoke-Execute $orphan 'S27 missing image' | Out-Null } catch { $missingImageRejected = $true }
Assert-True $missingImageRejected 'Caliper without system/upstream Image was not rejected.'
$orphan.Dispose()

Invoke-Execute $source 'S27 source' | Out-Null
Invoke-Execute $threshold 'S27 threshold' | Out-Null
$flowRecord = Invoke-Execute $caliperFlow 'S27 typed image flow'
Assert-True ($flowRecord.ResultCode -eq 'OK') 'Typed upstream Image caliper execution failed.'
$judgeRecord = Invoke-Execute $judge 'S27 numeric judge'
Assert-True ($judgeRecord.ResultCode -eq 'OK') 'PrimaryDistance did not drive NumericJudge.'

$refreshLayers.Invoke($window, @()) | Out-Null
$regionLayer = $layers | Where-Object { $_.ToolId -eq $threshold.ToolId -and $_.DataType -eq 'Region' } | Select-Object -First 1
$xldLayer = $layers | Where-Object { $_.ToolId -eq $caliperFlow.ToolId -and $_.DataType -eq 'XLD' } | Select-Object -First 1
Assert-True ($null -ne $regionLayer -and $null -ne $xldLayer) 'Current-cycle Region and XLD layers were not both listed.'
Assert-True ($regionLayer.StateKey -eq 'Valid' -and $xldLayer.StateKey -eq 'Valid') 'Live output layers were not marked valid.'
Assert-True (-not [string]::IsNullOrWhiteSpace($regionLayer.ColorName) -and $xldLayer.ObjectCount -gt 0) 'Layer color/object metadata is incomplete.'

$regionSnapshot = Get-Out $threshold Region
$xldSnapshot = Get-Out $caliperFlow XLD
$bindingBefore = $caliperFlow.GetInputBinding('Image').SourceToolId + ':' + $caliperFlow.GetInputBinding('Image').SourcePortName
$setAllLayers.Invoke($window, [object[]]@($false)) | Out-Null
Assert-True (($layers | Where-Object IsVisible).Count -eq 0) 'Hide-all did not hide every result layer.'
Assert-True (-not $regionSnapshot.IsDisposed -and -not $xldSnapshot.IsDisposed) 'Display-only hide released runtime outputs.'
Assert-True (($caliperFlow.GetInputBinding('Image').SourceToolId + ':' + $caliperFlow.GetInputBinding('Image').SourcePortName) -eq $bindingBefore) 'Display-only hide changed InputBindings.'

$setAllLayers.Invoke($window, [object[]]@($true)) | Out-Null
$resultList.SelectedItem = $xldLayer
$isolateLayer.Invoke($window, [object[]]@($null, [Windows.RoutedEventArgs]::new())) | Out-Null
Assert-True (($layers | Where-Object IsVisible).Count -eq 1) 'Isolate did not leave exactly one visible layer.'
$selectedKey = ($layers | Where-Object IsVisible | Select-Object -First 1).LayerKey
Assert-True ($selectedKey -eq $xldLayer.LayerKey) 'Isolate displayed the wrong layer.'

$resultList.SelectedItem = $layers[$layers.Count - 1]
$movingKey = $resultList.SelectedItem.LayerKey
$moveLayer.Invoke($window, [object[]]@(-1)) | Out-Null
Assert-True (($layers | Where-Object LayerKey -eq $movingKey | Select-Object -First 1).Sequence -lt $layers.Count) 'Move-up did not change layer order.'
$refreshLayers.Invoke($window, @()) | Out-Null
Assert-True (($layers | Where-Object LayerKey -eq $movingKey | Select-Object -First 1).Sequence -lt $layers.Count) 'Layer order did not survive a runtime refresh.'

$xldSnapshot = Get-Out $caliperFlow XLD
$xldSnapshot.Dispose()
$refreshLayers.Invoke($window, @()) | Out-Null
$invalidLayer = $layers | Where-Object { $_.ToolId -eq $caliperFlow.ToolId -and $_.DataType -eq 'XLD' } | Select-Object -First 1
Assert-True ($invalidLayer.StateKey -eq 'Invalid') 'Disposed output snapshot was not marked invalid.'
Invoke-Execute $caliperFlow 'S27 rerun after disposed output' | Out-Null
$refreshLayers.Invoke($window, @()) | Out-Null
Assert-True (($layers | Where-Object { $_.ToolId -eq $caliperFlow.ToolId -and $_.DataType -eq 'XLD' } | Select-Object -First 1).StateKey -eq 'Valid') 'Rerun did not restore an invalid layer.'

$liveAfterRerun = Get-Out $caliperFlow XLD
$clearOverlay.Invoke($window, [object[]]@($null, [Windows.RoutedEventArgs]::new())) | Out-Null
Assert-True (-not $liveAfterRerun.IsDisposed) 'Clear overlay released a live algorithm output.'
Assert-True (($layers | Where-Object IsVisible).Count -eq 0) 'Clear overlay no longer behaves as display-only hide.'

$recipe = [Activator]::CreateInstance($visionRecipeType)
$recipe.ToolFlow = $captureFlow.Invoke($window, @())
$recipePath = Join-Path $tempRoot 's27-caliper-recipe.json'
$service.SaveRecipe($recipePath, $recipe)
$loaded = $service.LoadRecipe($recipePath)
$loadedCaliper = $loaded.ToolFlow | Where-Object ToolType -eq 'CaliperEdgePairMeasurement' | Select-Object -First 1
Assert-True ($null -ne $loadedCaliper -and [Math]::Abs($loadedCaliper.Parameters.CaliperLength1 - 90) -lt .001 -and $loadedCaliper.Parameters.CaliperTransition -eq 'Positive') 'Caliper parameters did not survive recipe save/load.'

$sourceSnapshot = Get-Out $source Image
$sourceSnapshot.Dispose()
$rejected = $false
try { Invoke-Execute $caliperFlow 'S27 disposed upstream image' | Out-Null } catch { $rejected = $true }
Assert-True $rejected 'Disposed upstream Image was not rejected.'

$disposeFlow.Invoke($window, @()) | Out-Null
$flow.Clear()
$mainType.GetField('recipeCloseConfirmed', $private).SetValue($window, $true)
$window.Close()
if (Test-Path $tempRoot) { Remove-Item -LiteralPath $tempRoot -Recurse -Force }
Write-Output 'S27 result-layer and HALCON caliper smoke tests passed.'
