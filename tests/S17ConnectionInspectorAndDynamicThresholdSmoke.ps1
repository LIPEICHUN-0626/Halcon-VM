param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'
if ([Threading.Thread]::CurrentThread.ApartmentState -ne [Threading.ApartmentState]::STA) {
    throw 'Run this smoke test with powershell.exe -STA.'
}

$halconRoot = 'C:\Program Files\MVTec\HALCON-20.11-Progress'
$env:PATH = (Join-Path $halconRoot 'bin\x64-win64') + ';' + $env:PATH
Add-Type -AssemblyName PresentationFramework
[Reflection.Assembly]::LoadFrom((Join-Path $halconRoot 'bin\dotnet35\halcondotnet.dll')) | Out-Null
$exePath = Join-Path $ProjectRoot 'bin\x64\Debug\HalconWinFormsDemo.exe'
if (-not (Test-Path -LiteralPath $exePath)) { throw "Build Debug x64 first: $exePath" }
$assembly = [Reflection.Assembly]::LoadFrom($exePath)

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw "Assertion failed: $Message" }
}

function Write-Pgm([string]$Path, [int]$Width, [int]$Height, [scriptblock]$PixelFactory) {
    $header = [Text.Encoding]::ASCII.GetBytes("P5`n$Width $Height`n255`n")
    $pixels = New-Object byte[] ($Width * $Height)
    for ($row = 0; $row -lt $Height; $row++) {
        for ($column = 0; $column -lt $Width; $column++) {
            $pixels[$row * $Width + $column] = [byte](& $PixelFactory $row $column)
        }
    }
    $bytes = New-Object byte[] ($header.Length + $pixels.Length)
    [Array]::Copy($header, 0, $bytes, 0, $header.Length)
    [Array]::Copy($pixels, 0, $bytes, $header.Length, $pixels.Length)
    [IO.File]::WriteAllBytes($Path, $bytes)
}

function Write-Ppm([string]$Path, [int]$Width, [int]$Height) {
    $header = [Text.Encoding]::ASCII.GetBytes("P6`n$Width $Height`n255`n")
    $pixels = New-Object byte[] ($Width * $Height * 3)
    for ($index = 0; $index -lt $Width * $Height; $index++) {
        $pixels[$index * 3] = 80
        $pixels[$index * 3 + 1] = 100
        $pixels[$index * 3 + 2] = 120
    }
    $bytes = New-Object byte[] ($header.Length + $pixels.Length)
    [Array]::Copy($header, 0, $bytes, 0, $header.Length)
    [Array]::Copy($pixels, 0, $bytes, $header.Length, $pixels.Length)
    [IO.File]::WriteAllBytes($Path, $bytes)
}

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s17-smoke-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($tempRoot) | Out-Null
$originalPath = Join-Path $tempRoot 'original.pgm'
$referencePath = Join-Path $tempRoot 'reference.pgm'
$smallReferencePath = Join-Path $tempRoot 'reference-small.pgm'
$colorPath = Join-Path $tempRoot 'color.ppm'
Write-Pgm $originalPath 80 60 {
    param($row, $column)
    if ($row -ge 10 -and $row -le 29 -and $column -ge 10 -and $column -le 29) { return 160 }
    if ($row -ge 25 -and $row -le 44 -and $column -ge 45 -and $column -le 64) { return 40 }
    return 100
}
Write-Pgm $referencePath 80 60 { param($row, $column) return 100 }
Write-Pgm $smallReferencePath 70 60 { param($row, $column) return 100 }
Write-Ppm $colorPath 80 60

$mainType = $assembly.GetType('HalconWinFormsDemo.MainWindow', $true)
$recipeServiceType = $assembly.GetType('HalconWinFormsDemo.Services.RecipeService', $true)
$loggerType = $assembly.GetType('HalconWinFormsDemo.Services.AppLogger', $true)
$toolKindType = $assembly.GetType('HalconWinFormsDemo.Models.VmToolKind', $true)
$private = [Reflection.BindingFlags]'Instance,NonPublic'
$allFields = [Reflection.BindingFlags]'Instance,NonPublic,Public'
$constructorArgs = New-Object 'object[]' 1
$constructorArgs[0] = $tempRoot.ToString()
$recipeService = $recipeServiceType.GetConstructor([Type[]]@([string])).Invoke($constructorArgs)
$logger = $loggerType.GetConstructor([Type[]]@([string])).Invoke($constructorArgs)
$windowConstructor = $mainType.GetConstructor($private, $null, [Type[]]@($recipeServiceType, $loggerType), $null)
$window = $windowConstructor.Invoke([object[]]@($recipeService, $logger))

$flow = $mainType.GetField('flowTools', $private).GetValue($window)
$catalog = $mainType.GetField('toolCatalog', $private).GetValue($window)
$connections = $mainType.GetField('flowConnections', $private).GetValue($window)
$createTool = $mainType.GetMethod('CreateFlowTool', $private)
$executeTool = $mainType.GetMethod('ExecuteFlowTool', $private)
$disposeTools = $mainType.GetMethod('DisposeFlowTools', $private)
$refresh = $mainType.GetMethod('RefreshUiState', $private)
$selectConnection = $mainType.GetMethod('SelectFlowConnection', $private)
$repairConnection = $mainType.GetMethod('RepairFlowConnectionButton_Click', $private)
$clearConnection = $mainType.GetMethod('ClearFlowConnectionButton_Click', $private)
$locateSource = $mainType.GetMethod('LocateFlowConnectionSourceButton_Click', $private)
$locateTarget = $mainType.GetMethod('LocateFlowConnectionTargetButton_Click', $private)
$fitConnection = $mainType.GetMethod('FitSelectedFlowConnectionButton_Click', $private)
$openTargetInput = $mainType.GetMethod('OpenFlowConnectionTargetInputButton_Click', $private)
$openConfiguration = $mainType.GetMethod('OpenSelectedToolConfiguration', $private)
$applyDock = $mainType.GetMethod('ApplyDockConfigurationDraft', $private)
$captureRecipe = $mainType.GetMethod('CaptureRecipe', $private)

$flowToolList = $mainType.GetField('FlowToolList', $allFields).GetValue($window)
$inspectorBorder = $mainType.GetField('FlowConnectionInspectorBorder', $allFields).GetValue($window)
$inspectorSource = $mainType.GetField('FlowConnectionInspectorSourceText', $allFields).GetValue($window)
$inspectorTarget = $mainType.GetField('FlowConnectionInspectorTargetText', $allFields).GetValue($window)
$inspectorType = $mainType.GetField('FlowConnectionInspectorTypeText', $allFields).GetValue($window)
$inspectorError = $mainType.GetField('FlowConnectionInspectorErrorText', $allFields).GetValue($window)
$rightTabs = $mainType.GetField('RightTabs', $allFields).GetValue($window)
$dockTab = $mainType.GetField('DockConfigurationTab', $allFields).GetValue($window)
$dockTabs = $mainType.GetField('DockConfigurationTabs', $allFields).GetValue($window)
$dockInputList = $mainType.GetField('DockInputPortItemsControl', $allFields).GetValue($window)
$dockPanel = $mainType.GetField('DockDynamicThresholdPanel', $allFields).GetValue($window)
$dockPolarity = $mainType.GetField('DockDynamicThresholdPolarityComboBox', $allFields).GetValue($window)
$dockOffset = $mainType.GetField('DockDynamicThresholdOffsetTextBox', $allFields).GetValue($window)
$dockValidation = $mainType.GetField('DockValidationText', $allFields).GetValue($window)

function New-Tool([string]$Kind, [string]$Name) {
    return $createTool.Invoke($window, [object[]]@([Enum]::Parse($toolKindType, $Kind), $Name, $true, $null))
}

function Invoke-Tool($Tool, [string]$Label) {
    try { return $executeTool.Invoke($window, [object[]]@($Tool, $Label)) }
    catch [Reflection.TargetInvocationException] { throw $_.Exception.InnerException }
}

function Get-Output($Tool, [string]$Port) {
    $value = $null
    if (-not $Tool.TryGetOutputValue($Port, [ref]$value)) { throw "$($Tool.InstanceName).$Port has no output." }
    return $value
}

$disposeTools.Invoke($window, @()) | Out-Null
$flow.Clear()
$original = New-Tool 'ImageSource' 'Original_Input'
$reference = New-Tool 'ImageSource' 'Reference_Input'
$dynamic = New-Tool 'DynamicThreshold' 'Dynamic_LightDark'
$morphology = New-Tool 'RegionMorphology' 'Morph_After_Dynamic'
$feature = New-Tool 'RegionFeatureFilter' 'Feature_After_Dynamic'
$blob = New-Tool 'Blob' 'Blob_Dynamic_ROI'
$gray = New-Tool 'GrayStat' 'Gray_Dynamic_ROI'
$edge = New-Tool 'EdgeMeasure' 'Edge_Dynamic_ROI'
$original.Parameters.LocalImagePath = $originalPath
$original.Parameters.LocalImageSerialNumber = 'S17-ORIGINAL'
$reference.Parameters.LocalImagePath = $referencePath
$reference.Parameters.LocalImageSerialNumber = 'S17-REFERENCE'
$dynamic.Parameters.DynamicThresholdPolarity = 'Light'
$dynamic.Parameters.DynamicThresholdOffset = 20
$morphology.Parameters.RegionMorphologyMode = 'OpeningCircle'
$morphology.Parameters.RegionMorphologyRadius = 1.5
$feature.Parameters.RegionFeature = 'Area'
$feature.Parameters.RegionFeatureMin = 1
$feature.Parameters.RegionFeatureMax = 1000
$blob.Parameters.BlobMinGray = 120
$blob.Parameters.BlobMaxGray = 255
$blob.Parameters.BlobMinArea = 10
$gray.Parameters.GrayMin = 0
$gray.Parameters.GrayMax = 255
$edge.Parameters.EdgeThreshold = 5
foreach ($tool in @($original, $reference, $dynamic, $morphology, $feature, $blob, $gray, $edge)) { $flow.Add($tool) }
$dynamic.SetInputBinding('OriginalImage', $original.ToolId, 'Image')
$dynamic.SetInputBinding('ReferenceImage', $reference.ToolId, 'Image')
$morphology.SetInputBinding('Region', $dynamic.ToolId, 'Region')
$feature.SetInputBinding('Region', $morphology.ToolId, 'Region')
foreach ($consumer in @($blob, $gray, $edge)) {
    $consumer.SetInputBinding('Image', $original.ToolId, 'Image')
    $consumer.SetInputBinding('ROI', $dynamic.ToolId, 'Region')
}

$dynamicCatalog = $catalog | Where-Object Kind -eq ([Enum]::Parse($toolKindType, 'DynamicThreshold')) | Select-Object -First 1
Assert-True ($null -ne $dynamicCatalog -and -not [string]::IsNullOrWhiteSpace($dynamicCatalog.Name)) 'Dynamic threshold is missing from the visible toolbox.'

$flowToolList.SelectedItem = $dynamic
$refresh.Invoke($window, @()) | Out-Null
Assert-True ($connections.Count -eq 10) "Expected 10 real binding paths, got $($connections.Count)."
Assert-True (($connections | Where-Object { $_.TargetToolId -eq $dynamic.ToolId -and $_.DataType -eq 'Image' }).Count -eq 2) 'The two Image convergence paths are not visible.'

$dynamic.SetInputBinding('OriginalImage', 'missing-original-source', 'Image')
$refresh.Invoke($window, @()) | Out-Null
$invalidConnection = $connections | Where-Object { $_.TargetToolId -eq $dynamic.ToolId -and $_.TargetPortName -eq 'OriginalImage' } | Select-Object -First 1
Assert-True ($null -ne $invalidConnection -and $invalidConnection.StateKey -eq 'Error') 'The invalid saved Image source is not visible as an error connection.'
$selectConnection.Invoke($window, [object[]]@($invalidConnection.PSObject.BaseObject)) | Out-Null
Assert-True ($inspectorBorder.Visibility.ToString() -eq 'Visible') 'Selecting a real path did not open the connection inspector.'
Assert-True (-not [string]::IsNullOrWhiteSpace($inspectorSource.Text) -and $inspectorTarget.Text.Contains('OriginalImage')) 'The inspector does not identify both endpoints.'
Assert-True ($inspectorType.Text -eq 'Image' -and -not [string]::IsNullOrWhiteSpace($inspectorError.Text)) 'The inspector does not show data type and error reason.'
Assert-True (($connections | Where-Object IsSelected).Count -eq 1) 'Exactly one real path must be selected.'
Assert-True (($dynamic.FlowInputPorts | Where-Object { $_.PortName -eq 'OriginalImage' }).IsConnectionEndpointSelected) 'The selected target port is not highlighted.'

$repairConnection.Invoke($window, @($null, $null)) | Out-Null
$repairedBinding = $dynamic.GetInputBinding('OriginalImage')
Assert-True ($repairedBinding.SourceToolId -eq $original.ToolId -and $repairedBinding.SourcePortName -eq 'Image') 'Automatic repair did not write the nearest non-duplicate compatible source to InputBindings.'
Assert-True (($original.FlowOutputPorts | Where-Object { $_.PortName -eq 'Image' }).IsConnectionEndpointSelected) 'The repaired source port is not highlighted.'
$openTargetInput.Invoke($window, @($null, $null)) | Out-Null
Assert-True ([object]::ReferenceEquals($rightTabs.SelectedItem, $dockTab) -and $dockTabs.SelectedIndex -eq 2) 'Open target input did not navigate to the docked Input page.'
Assert-True ($dockInputList.SelectedItem.PortName -eq 'OriginalImage') 'The target input row was not selected.'
$locateSource.Invoke($window, @($null, $null)) | Out-Null
Assert-True ([object]::ReferenceEquals($flowToolList.SelectedItem, $original)) 'Locate source did not select the source module.'
$locateTarget.Invoke($window, @($null, $null)) | Out-Null
Assert-True ([object]::ReferenceEquals($flowToolList.SelectedItem, $dynamic)) 'Locate target did not select the target module.'
$fitConnection.Invoke($window, @($null, $null)) | Out-Null

$clearConnection.Invoke($window, @($null, $null)) | Out-Null
Assert-True ($null -eq $dynamic.GetInputBinding('OriginalImage')) 'Clear connection did not remove the real InputBinding.'
$dynamic.SetInputBinding('OriginalImage', $original.ToolId, 'Image')
$refresh.Invoke($window, @()) | Out-Null

$unrunRejected = $false
try { Invoke-Tool $dynamic 'S17 unrun upstream' | Out-Null } catch { $unrunRejected = $_.Exception.Message.Contains('OK') }
Assert-True $unrunRejected 'Dynamic threshold must reject unrun explicit upstream Images.'

Invoke-Tool $original 'S17 original' | Out-Null
Invoke-Tool $reference 'S17 reference' | Out-Null
Invoke-Tool $dynamic 'S17 light' | Out-Null
Assert-True ($dynamic.ResultCode -eq 'OK') 'Light dynamic threshold did not return OK.'
Assert-True ([Math]::Abs([double](Get-Output $dynamic 'Area') - 400) -lt 0.001) 'Light dynamic threshold area must be 400.'
Assert-True ([int][double](Get-Output $dynamic 'RegionCount') -eq 1) 'Light dynamic threshold region count must be 1.'
Assert-True ([string](Get-Output $dynamic 'Polarity') -eq 'Light' -and [double](Get-Output $dynamic 'Offset') -eq 20) 'Polarity/Offset outputs are incorrect.'

Invoke-Tool $morphology 'S17 morphology consumer' | Out-Null
Invoke-Tool $feature 'S17 feature consumer' | Out-Null
Invoke-Tool $blob 'S17 blob ROI consumer' | Out-Null
Invoke-Tool $gray 'S17 gray ROI consumer' | Out-Null
Invoke-Tool $edge 'S17 edge ROI consumer' | Out-Null
Assert-True ($morphology.ResultCode -eq 'OK' -and $feature.ResultCode -eq 'OK' -and $blob.ResultCode -eq 'OK' -and $gray.ResultCode -eq 'OK') 'Morphology/feature/Blob/Gray did not consume the dynamic Region.'
Assert-True ([Math]::Abs([double](Get-Output $gray 'Mean') - 160) -lt 0.001) 'Gray ROI consumer did not measure the bright region mean.'
Assert-True ($edge.ResultCode -eq 'OK' -or $edge.ResultCode -eq 'NG') 'Edge ROI consumer did not execute.'

$dynamic.Parameters.DynamicThresholdPolarity = 'Dark'
Invoke-Tool $dynamic 'S17 dark' | Out-Null
Assert-True ($dynamic.ResultCode -eq 'OK' -and [Math]::Abs([double](Get-Output $dynamic 'Area') - 400) -lt 0.001) 'Dark dynamic threshold area must be 400.'
Assert-True ([string](Get-Output $dynamic 'Polarity') -eq 'Dark') 'Dark polarity output is incorrect.'

$flowToolList.SelectedItem = $dynamic
$openConfiguration.Invoke($window, @()) | Out-Null
Assert-True ($dockPanel.Visibility.ToString() -eq 'Visible') 'Dynamic threshold dock panel is not visible.'
$dockPolarity.SelectedValue = 'Light'
$dockOffset.Text = '0'
Assert-True (-not [bool]$applyDock.Invoke($window, @($false))) 'The dock must reject a zero offset.'
Assert-True (-not [string]::IsNullOrWhiteSpace($dockValidation.Text)) 'Invalid offset needs an actionable dock error.'
$dockOffset.Text = '20'
Assert-True ([bool]$applyDock.Invoke($window, @($false))) 'Applying valid dynamic threshold settings failed.'

$recipe = $captureRecipe.Invoke($window, @())
$recipePath = Join-Path $tempRoot 's17-roundtrip.json'
$recipeService.SaveRecipe($recipePath, $recipe)
$loaded = $recipeService.LoadRecipe($recipePath)
$dynamicRecipe = $loaded.ToolFlow | Where-Object ToolId -eq $dynamic.ToolId
Assert-True ($dynamicRecipe.Parameters.DynamicThresholdPolarity -eq 'Light' -and $dynamicRecipe.Parameters.DynamicThresholdOffset -eq 20) 'Dynamic threshold parameters did not round-trip.'
Assert-True (($dynamicRecipe.InputBindings | Where-Object TargetPortName -eq 'OriginalImage').SourceToolId -eq $original.ToolId) 'OriginalImage binding did not round-trip.'
Assert-True (($dynamicRecipe.InputBindings | Where-Object TargetPortName -eq 'ReferenceImage').SourceToolId -eq $reference.ToolId) 'ReferenceImage binding did not round-trip.'

$dynamic.Parameters.DynamicThresholdOffset = 200
Invoke-Tool $dynamic 'S17 empty result' | Out-Null
Assert-True ($dynamic.ResultCode -eq 'NG' -and [double](Get-Output $dynamic 'Area') -eq 0 -and [int][double](Get-Output $dynamic 'RegionCount') -eq 0) 'An empty dynamic result must be managed Region + NG/0/0.'

$dynamic.Parameters.DynamicThresholdOffset = 20
$reference.Parameters.LocalImagePath = $smallReferencePath
Invoke-Tool $reference 'S17 smaller reference' | Out-Null
$sizeRejected = $false
try { Invoke-Tool $dynamic 'S17 size mismatch' | Out-Null } catch { $sizeRejected = -not [string]::IsNullOrWhiteSpace($_.Exception.Message) }
Assert-True $sizeRejected 'Mismatched Image sizes must be rejected.'

$reference.Parameters.LocalImagePath = $referencePath
$original.Parameters.LocalImagePath = $colorPath
Invoke-Tool $original 'S17 color original' | Out-Null
Invoke-Tool $reference 'S17 gray reference again' | Out-Null
$channelRejected = $false
try { Invoke-Tool $dynamic 'S17 channel mismatch' | Out-Null } catch { $channelRejected = -not [string]::IsNullOrWhiteSpace($_.Exception.Message) }
Assert-True $channelRejected 'Mismatched Image channels must be rejected.'

$original.Parameters.LocalImagePath = $originalPath
Invoke-Tool $original 'S17 original restored' | Out-Null
Invoke-Tool $reference 'S17 reference restored' | Out-Null
$releasedInput = Get-Output $original 'Image'
$releasedInput.Dispose()
$releasedRejected = $false
try { Invoke-Tool $dynamic 'S17 released input' | Out-Null } catch { $releasedRejected = -not [string]::IsNullOrWhiteSpace($_.Exception.Message) }
Assert-True $releasedRejected 'A released upstream Image snapshot must be rejected.'

Invoke-Tool $original 'S17 lifecycle original' | Out-Null
Invoke-Tool $dynamic 'S17 lifecycle dynamic' | Out-Null
$liveRegion = Get-Output $dynamic 'Region'
Invoke-Tool $original 'S17 invalidate downstream' | Out-Null
Assert-True ($liveRegion.IsDisposed -and $dynamic.ResultCode -eq '--' -and $morphology.ResultCode -eq '--') 'Upstream rerun must dispose and invalidate the dynamic Region chain.'

$dynamic.SetInputBinding('OriginalImage', $reference.ToolId, 'Image')
$sameSourceRejected = $false
try { Invoke-Tool $dynamic 'S17 duplicate source' | Out-Null } catch { $sameSourceRejected = -not [string]::IsNullOrWhiteSpace($_.Exception.Message) }
Assert-True $sameSourceRejected 'The two required Image inputs must reject the same upstream endpoint.'

$disposeTools.Invoke($window, @()) | Out-Null
$flow.Clear()
$mainType.GetField('recipeCloseConfirmed', $private).SetValue($window, $true)
$window.Close()

Write-Output 'S17_CONNECTION_INSPECTOR_DYNAMIC_THRESHOLD=PASS; Inspector=SELECT/ENDPOINTS/VALUE/ERROR; Navigation=SOURCE/TARGET/INPUT/FIT; Repair=INPUTBINDINGS; HALCON=DYN_THRESHOLD_20_11; LightArea=400; DarkArea=400; Downstream=MORPH/FEATURE/BLOB/GRAY/EDGE; Errors=UNRUN/SIZE/CHANNEL/RELEASED/EMPTY/DUPLICATE; Dock=PASS; Recipe=PASS; Lifecycle=PASS'
