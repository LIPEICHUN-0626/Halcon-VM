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
        $pixels[$index * 3] = 100
        $pixels[$index * 3 + 1] = 110
        $pixels[$index * 3 + 2] = 120
    }
    $bytes = New-Object byte[] ($header.Length + $pixels.Length)
    [Array]::Copy($header, 0, $bytes, 0, $header.Length)
    [Array]::Copy($pixels, 0, $bytes, $header.Length, $pixels.Length)
    [IO.File]::WriteAllBytes($Path, $bytes)
}

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s18-smoke-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($tempRoot) | Out-Null
$imageAPath = Join-Path $tempRoot 'image-a.pgm'
$imageBPath = Join-Path $tempRoot 'image-b.pgm'
$smallPath = Join-Path $tempRoot 'image-small.pgm'
$colorPath = Join-Path $tempRoot 'image-color.ppm'
Write-Pgm $imageAPath 80 60 {
    param($row, $column)
    if ($row -ge 10 -and $row -le 29 -and $column -ge 10 -and $column -le 29) { return 180 }
    return 100
}
Write-Pgm $imageBPath 80 60 {
    param($row, $column)
    if ($row -ge 10 -and $row -le 29 -and $column -ge 10 -and $column -le 29) { return 120 }
    return 80
}
Write-Pgm $smallPath 70 60 { param($row, $column) return 80 }
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
$sourceOptions = $mainType.GetField('flowPortSourceOptions', $private).GetValue($window)
$consumers = $mainType.GetField('flowPortConsumers', $private).GetValue($window)
$createTool = $mainType.GetMethod('CreateFlowTool', $private)
$executeTool = $mainType.GetMethod('ExecuteFlowTool', $private)
$disposeTools = $mainType.GetMethod('DisposeFlowTools', $private)
$refresh = $mainType.GetMethod('RefreshUiState', $private)
$updatePortInspector = $mainType.GetMethod('UpdateFlowPortInspector', $private)
$applyPortSource = $mainType.GetMethod('ApplyFlowPortSourceButton_Click', $private)
$clearPortSource = $mainType.GetMethod('ClearFlowPortSourceButton_Click', $private)
$locateConsumer = $mainType.GetMethod('LocateFlowPortConsumerButton_Click', $private)
$openConfiguration = $mainType.GetMethod('OpenSelectedToolConfiguration', $private)
$applyDock = $mainType.GetMethod('ApplyDockConfigurationDraft', $private)
$captureRecipe = $mainType.GetMethod('CaptureRecipe', $private)

$flowToolList = $mainType.GetField('FlowToolList', $allFields).GetValue($window)
$portInspector = $mainType.GetField('FlowPortInspectorBorder', $allFields).GetValue($window)
$portDirection = $mainType.GetField('FlowPortInspectorDirectionText', $allFields).GetValue($window)
$portEndpoint = $mainType.GetField('FlowPortInspectorEndpointText', $allFields).GetValue($window)
$sourceCombo = $mainType.GetField('FlowPortSourceComboBox', $allFields).GetValue($window)
$portValidation = $mainType.GetField('FlowPortInputValidationText', $allFields).GetValue($window)
$consumerList = $mainType.GetField('FlowPortConsumerListBox', $allFields).GetValue($window)
$dockPanel = $mainType.GetField('DockImageDifferencePanel', $allFields).GetValue($window)
$dockMode = $mainType.GetField('DockImageDifferenceModeComboBox', $allFields).GetValue($window)
$dockMultiplier = $mainType.GetField('DockImageDifferenceMultiplierTextBox', $allFields).GetValue($window)
$dockAdd = $mainType.GetField('DockImageDifferenceAddTextBox', $allFields).GetValue($window)
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

function Select-Port([object]$Tool, [string]$PortName, [string]$Direction) {
    $mainType.GetField('selectedFlowConnectionKey', $private).SetValue($window, $null)
    $mainType.GetField('selectedFlowPortToolId', $private).SetValue($window, $Tool.ToolId)
    $mainType.GetField('selectedFlowPortName', $private).SetValue($window, $PortName)
    $mainType.GetField('selectedFlowPortDirection', $private).SetValue($window, $Direction)
    $flowToolList.SelectedItem = $Tool
    $refresh.Invoke($window, @()) | Out-Null
    $updatePortInspector.Invoke($window, @()) | Out-Null
}

$disposeTools.Invoke($window, @()) | Out-Null
$flow.Clear()
$sourceA = New-Tool 'ImageSource' 'Image_A'
$sourceB = New-Tool 'ImageSource' 'Image_B'
$sourceC = New-Tool 'ImageSource' 'Image_C_Alternate'
$difference = New-Tool 'ImageDifference' 'Surface_Change_Difference'
$threshold = New-Tool 'ImageThreshold' 'Difference_Threshold'
$dynamic = New-Tool 'DynamicThreshold' 'Difference_Dynamic'
$blob = New-Tool 'Blob' 'Difference_Blob'
$gray = New-Tool 'GrayStat' 'Difference_Gray'
$edge = New-Tool 'EdgeMeasure' 'Difference_Edge'
$sourceA.Parameters.LocalImagePath = $imageAPath
$sourceA.Parameters.LocalImageSerialNumber = 'S18-A'
$sourceB.Parameters.LocalImagePath = $imageBPath
$sourceB.Parameters.LocalImageSerialNumber = 'S18-B'
$sourceC.Parameters.LocalImagePath = $imageAPath
$sourceC.Parameters.LocalImageSerialNumber = 'S18-C'
$difference.Parameters.ImageDifferenceMode = 'AbsoluteDifference'
$difference.Parameters.ImageDifferenceMultiplier = 2
$difference.Parameters.ImageDifferenceAdd = 0
$threshold.Parameters.ImageThresholdMinGray = 100
$threshold.Parameters.ImageThresholdMaxGray = 255
$dynamic.Parameters.DynamicThresholdPolarity = 'Light'
$dynamic.Parameters.DynamicThresholdOffset = 20
$blob.Parameters.BlobMinGray = 100
$blob.Parameters.BlobMaxGray = 255
$blob.Parameters.BlobMinArea = 10
$gray.Parameters.GrayMin = 0
$gray.Parameters.GrayMax = 255
$edge.Parameters.EdgeThreshold = 5
foreach ($tool in @($sourceA, $sourceB, $sourceC, $difference, $threshold, $dynamic, $blob, $gray, $edge)) { $flow.Add($tool) }
$difference.SetInputBinding('ImageA', $sourceA.ToolId, 'Image')
$difference.SetInputBinding('ImageB', $sourceB.ToolId, 'Image')
$threshold.SetInputBinding('Image', $difference.ToolId, 'Image')
$dynamic.SetInputBinding('OriginalImage', $difference.ToolId, 'Image')
$dynamic.SetInputBinding('ReferenceImage', $sourceB.ToolId, 'Image')
foreach ($consumer in @($blob, $gray, $edge)) {
    $consumer.SetInputBinding('Image', $difference.ToolId, 'Image')
    $consumer.SetInputBinding('ROI', $threshold.ToolId, 'Region')
}

$differenceCatalog = $catalog | Where-Object Kind -eq ([Enum]::Parse($toolKindType, 'ImageDifference')) | Select-Object -First 1
Assert-True ($null -ne $differenceCatalog -and -not [string]::IsNullOrWhiteSpace($differenceCatalog.Name)) 'Image difference is missing from the visible toolbox.'

Select-Port $difference 'ImageA' 'IN'
Assert-True ($portInspector.Visibility.ToString() -eq 'Visible' -and $portDirection.Text.Contains('IN')) 'Input port selection did not open the port subscription inspector.'
Assert-True ($portEndpoint.Text.Contains('ImageA') -and $sourceOptions.Count -ge 4) 'The input inspector does not show the current endpoint and compatible previous outputs.'
Assert-True (($difference.FlowInputPorts | Where-Object PortName -eq 'ImageA').IsPortSelected) 'The selected input port is not highlighted.'

$sourceCombo.SelectedValue = ($sourceB.ToolId + '|Image')
$applyPortSource.Invoke($window, @($null, $null)) | Out-Null
Assert-True ($difference.GetInputBinding('ImageA').SourceToolId -eq $sourceA.ToolId) 'Duplicate two-input endpoint must be rejected and rolled back.'
Assert-True (-not [string]::IsNullOrWhiteSpace($portValidation.Text)) 'Duplicate input rejection needs an actionable error.'

$sourceCombo.SelectedValue = ($sourceC.ToolId + '|Image')
$applyPortSource.Invoke($window, @($null, $null)) | Out-Null
Assert-True ($difference.GetInputBinding('ImageA').SourceToolId -eq $sourceC.ToolId) 'Direct input replacement did not write the real InputBinding.'
$clearPortSource.Invoke($window, @($null, $null)) | Out-Null
Assert-True ($null -eq $difference.GetInputBinding('ImageA')) 'Direct clear did not remove the real InputBinding.'
Assert-True (($difference.FlowInputPorts | Where-Object PortName -eq 'ImageA').StateKey -eq 'Missing') 'A cleared required input must remain visibly missing.'
$sourceCombo.SelectedValue = ($sourceA.ToolId + '|Image')
$applyPortSource.Invoke($window, @($null, $null)) | Out-Null
Assert-True ($difference.GetInputBinding('ImageA').SourceToolId -eq $sourceA.ToolId) 'Input binding could not be restored from the port inspector.'

$unrunRejected = $false
try { Invoke-Tool $difference 'S18 unrun upstream' | Out-Null } catch { $unrunRejected = $_.Exception.Message.Contains('OK') }
Assert-True $unrunRejected 'Image difference must reject unrun explicit upstream Images.'

Invoke-Tool $sourceA 'S18 source A' | Out-Null
Invoke-Tool $sourceB 'S18 source B' | Out-Null
Invoke-Tool $sourceC 'S18 source C' | Out-Null
Invoke-Tool $difference 'S18 absolute difference' | Out-Null
$absoluteOutput = Get-Output $difference 'Image'
Assert-True ($absoluteOutput.GetPixelDisplay(0, 0) -eq '40') 'Absolute difference background must be abs(100-80)*2=40.'
Assert-True ($absoluteOutput.GetPixelDisplay(15, 15) -eq '120') 'Absolute difference patch must be abs(180-120)*2=120.'
Assert-True ([int][double](Get-Output $difference 'Channels') -eq 1 -and [string](Get-Output $difference 'Parameters') -eq 'Mult=2') 'Image difference managed outputs are incomplete.'

Invoke-Tool $threshold 'S18 threshold consumer' | Out-Null
Invoke-Tool $dynamic 'S18 dynamic consumer' | Out-Null
Invoke-Tool $blob 'S18 blob consumer' | Out-Null
Invoke-Tool $gray 'S18 gray consumer' | Out-Null
Invoke-Tool $edge 'S18 edge consumer' | Out-Null
Assert-True ($threshold.ResultCode -eq 'OK' -and [Math]::Abs([double](Get-Output $threshold 'Area') - 400) -lt 0.001) 'Threshold did not consume the difference Image.'
Assert-True ($blob.ResultCode -eq 'OK' -and $gray.ResultCode -eq 'OK') 'Blob/Gray did not consume the difference Image/Region chain.'
Assert-True ([Math]::Abs([double](Get-Output $gray 'Mean') - 120) -lt 0.001) 'Gray consumer did not measure the difference patch.'
Assert-True ($dynamic.ResultCode -eq 'OK' -or $dynamic.ResultCode -eq 'NG') 'Dynamic threshold did not execute from the difference Image.'
Assert-True ($edge.ResultCode -eq 'OK' -or $edge.ResultCode -eq 'NG') 'Edge consumer did not execute from the difference Image.'

$refresh.Invoke($window, @()) | Out-Null
Select-Port $difference 'Image' 'OUT'
Assert-True ($portInspector.Visibility.ToString() -eq 'Visible' -and $portDirection.Text.Contains('OUT')) 'Output port selection did not open the consumer navigator.'
Assert-True ($consumers.Count -eq 5) "Expected five real Image consumers, got $($consumers.Count)."
Assert-True (($difference.FlowOutputPorts | Where-Object PortName -eq 'Image').IsPortSelected) 'The selected output port is not highlighted.'
$consumerList.SelectedIndex = 0
$locateConsumer.Invoke($window, @($null, $null)) | Out-Null
Assert-True (-not [string]::IsNullOrWhiteSpace([string]$mainType.GetField('selectedFlowConnectionKey', $private).GetValue($window))) 'Locate consumer did not select the real connection path.'

$difference.Parameters.ImageDifferenceMode = 'SubtractAB'
$difference.Parameters.ImageDifferenceMultiplier = 1
$difference.Parameters.ImageDifferenceAdd = 10
Invoke-Tool $difference 'S18 A minus B' | Out-Null
$subtractAB = Get-Output $difference 'Image'
Assert-True ($subtractAB.GetPixelDisplay(0, 0) -eq '30' -and $subtractAB.GetPixelDisplay(15, 15) -eq '70') 'Directional A-B output is incorrect.'

$difference.Parameters.ImageDifferenceMode = 'SubtractBA'
$difference.Parameters.ImageDifferenceAdd = 100
Invoke-Tool $difference 'S18 B minus A' | Out-Null
$subtractBA = Get-Output $difference 'Image'
Assert-True ($subtractBA.GetPixelDisplay(0, 0) -eq '80' -and $subtractBA.GetPixelDisplay(15, 15) -eq '40') 'Directional B-A output is incorrect.'

$flowToolList.SelectedItem = $difference
$openConfiguration.Invoke($window, @()) | Out-Null
Assert-True ($dockPanel.Visibility.ToString() -eq 'Visible') 'Image difference dock panel is not visible.'
$dockMode.SelectedValue = 'AbsoluteDifference'
$dockMultiplier.Text = '0'
Assert-True (-not [bool]$applyDock.Invoke($window, @($false))) 'The dock must reject a zero difference multiplier.'
Assert-True (-not [string]::IsNullOrWhiteSpace($dockValidation.Text)) 'Invalid difference multiplier needs an actionable dock error.'
$dockMode.SelectedValue = 'SubtractAB'
$dockMultiplier.Text = '1.5'
$dockAdd.Text = '12'
Assert-True ([bool]$applyDock.Invoke($window, @($false))) 'Applying valid image difference settings failed.'

$recipe = $captureRecipe.Invoke($window, @())
$recipePath = Join-Path $tempRoot 's18-roundtrip.json'
$recipeService.SaveRecipe($recipePath, $recipe)
$loaded = $recipeService.LoadRecipe($recipePath)
$differenceRecipe = $loaded.ToolFlow | Where-Object ToolId -eq $difference.ToolId
Assert-True ($differenceRecipe.Parameters.ImageDifferenceMode -eq 'SubtractAB' -and [Math]::Abs($differenceRecipe.Parameters.ImageDifferenceMultiplier - 1.5) -lt 0.001 -and $differenceRecipe.Parameters.ImageDifferenceAdd -eq 12) 'Image difference parameters did not round-trip.'
Assert-True (($differenceRecipe.InputBindings | Where-Object TargetPortName -eq 'ImageA').SourceToolId -eq $sourceA.ToolId) 'ImageA binding did not round-trip.'
Assert-True (($differenceRecipe.InputBindings | Where-Object TargetPortName -eq 'ImageB').SourceToolId -eq $sourceB.ToolId) 'ImageB binding did not round-trip.'

$difference.Parameters.ImageDifferenceMultiplier = 0
$invalidRejected = $false
try { Invoke-Tool $difference 'S18 invalid multiplier' | Out-Null } catch { $invalidRejected = $_.Exception.Message.Contains('100') }
Assert-True ($invalidRejected -and $difference.ResultCode -eq 'NG') 'Invalid difference multiplier must fail with an actionable NG state.'
$difference.Parameters.ImageDifferenceMultiplier = 1
$difference.Parameters.ImageDifferenceMode = 'AbsoluteDifference'

$sourceB.Parameters.LocalImagePath = $smallPath
Invoke-Tool $sourceB 'S18 smaller B' | Out-Null
$sizeRejected = $false
try { Invoke-Tool $difference 'S18 size mismatch' | Out-Null } catch { $sizeRejected = $_.Exception.Message.Contains('size') -or $_.Exception.Message.Contains([char]0x5C3A) }
Assert-True $sizeRejected 'Mismatched Image sizes must be rejected.'

$sourceB.Parameters.LocalImagePath = $colorPath
Invoke-Tool $sourceB 'S18 color B' | Out-Null
$channelRejected = $false
try { Invoke-Tool $difference 'S18 channel mismatch' | Out-Null } catch { $channelRejected = $_.Exception.Message.Contains('channel') -or $_.Exception.Message.Contains([char]0x901A) }
Assert-True $channelRejected 'Mismatched Image channels must be rejected.'

$sourceB.Parameters.LocalImagePath = $imageBPath
Invoke-Tool $sourceB 'S18 B restored' | Out-Null
$releasedInput = Get-Output $sourceA 'Image'
$releasedInput.Dispose()
$releasedRejected = $false
try { Invoke-Tool $difference 'S18 released input' | Out-Null } catch { $releasedRejected = -not [string]::IsNullOrWhiteSpace($_.Exception.Message) }
Assert-True $releasedRejected 'A released upstream Image snapshot must be rejected.'

Invoke-Tool $sourceA 'S18 A lifecycle' | Out-Null
Invoke-Tool $difference 'S18 difference lifecycle' | Out-Null
$liveSnapshot = Get-Output $difference 'Image'
Invoke-Tool $sourceA 'S18 invalidate downstream' | Out-Null
Assert-True ($liveSnapshot.IsDisposed -and $difference.ResultCode -eq '--' -and $threshold.ResultCode -eq '--') 'Upstream rerun must dispose and invalidate the difference Image chain.'

$difference.SetInputBinding('ImageA', $sourceB.ToolId, 'Image')
$sameSourceRejected = $false
try { Invoke-Tool $difference 'S18 duplicate source' | Out-Null } catch { $sameSourceRejected = -not [string]::IsNullOrWhiteSpace($_.Exception.Message) }
Assert-True $sameSourceRejected 'The two required Image inputs must reject the same upstream endpoint.'

$disposeTools.Invoke($window, @()) | Out-Null
$flow.Clear()
$mainType.GetField('recipeCloseConfirmed', $private).SetValue($window, $true)
$window.Close()

Write-Output 'S18_PORT_NAVIGATION_IMAGE_DIFFERENCE=PASS; PortInput=CURRENT/OPTIONS/REPLACE/CLEAR/VALIDATE; PortOutput=CONSUMERS/LOCATE; HALCON=ABS_DIFF_IMAGE/SUB_IMAGE; Pixels=ABS40_120/AB30_70/BA80_40; Downstream=THRESHOLD/DYNAMIC/BLOB/GRAY/EDGE; Errors=UNRUN/DUPLICATE/SIZE/CHANNEL/RELEASED/PARAMETER; Dock=PASS; Recipe=PASS; Lifecycle=PASS'
