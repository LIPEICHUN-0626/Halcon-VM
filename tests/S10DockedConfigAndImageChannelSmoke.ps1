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
Add-Type -AssemblyName System.Drawing
[Reflection.Assembly]::LoadFrom((Join-Path $halconRoot 'bin\dotnet35\halcondotnet.dll')) | Out-Null
$exePath = Join-Path $ProjectRoot 'bin\x64\Debug\HalconWinFormsDemo.exe'
if (-not (Test-Path -LiteralPath $exePath)) {
    throw "Build Debug x64 first: $exePath"
}
$assembly = [Reflection.Assembly]::LoadFrom($exePath)

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s10-smoke-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($tempRoot) | Out-Null
$imagePath = Join-Path $tempRoot 'rgb-source.png'
$bitmap = New-Object Drawing.Bitmap 80, 50
try {
    for ($row = 0; $row -lt $bitmap.Height; $row++) {
        for ($column = 0; $column -lt $bitmap.Width; $column++) {
            $color = if ($column -ge 20 -and $column -lt 60 -and $row -ge 10 -and $row -lt 40) {
                [Drawing.Color]::FromArgb(240, 40, 80)
            }
            else {
                [Drawing.Color]::FromArgb(20, 100, 200)
            }
            $bitmap.SetPixel($column, $row, $color)
        }
    }
    $bitmap.Save($imagePath, [Drawing.Imaging.ImageFormat]::Png)
}
finally {
    $bitmap.Dispose()
}

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) {
        throw "Assertion failed: $Message"
    }
}

$mainType = $assembly.GetType('HalconWinFormsDemo.MainWindow', $true)
$recipeServiceType = $assembly.GetType('HalconWinFormsDemo.Services.RecipeService', $true)
$loggerType = $assembly.GetType('HalconWinFormsDemo.Services.AppLogger', $true)
$toolKindType = $assembly.GetType('HalconWinFormsDemo.Models.VmToolKind', $true)
$channelModeType = $assembly.GetType('HalconWinFormsDemo.Models.VmImageChannelMode', $true)
$contextType = $assembly.GetType('HalconWinFormsDemo.Models.VmImageContextOption', $true)
$constructorTypes = [Type[]]@([string])
$constructorArgs = New-Object 'object[]' 1
$constructorArgs[0] = $tempRoot.ToString()
$recipeService = $recipeServiceType.GetConstructor($constructorTypes).Invoke($constructorArgs)
$logger = $loggerType.GetConstructor($constructorTypes).Invoke($constructorArgs)
$windowConstructor = $mainType.GetConstructor(
    [Reflection.BindingFlags]'Instance,NonPublic',
    $null,
    [Type[]]@($recipeServiceType, $loggerType),
    $null)
$window = $windowConstructor.Invoke(@($recipeService, $logger))

$private = [Reflection.BindingFlags]'Instance,NonPublic'
$allFields = [Reflection.BindingFlags]'Instance,NonPublic,Public'
$flow = $mainType.GetField('flowTools', $private).GetValue($window)
$createTool = $mainType.GetMethod('CreateFlowTool', $private)
$executeTool = $mainType.GetMethod('ExecuteFlowTool', $private)
$disposeTools = $mainType.GetMethod('DisposeFlowTools', $private)
$openConfiguration = $mainType.GetMethod('OpenSelectedToolConfiguration', $private)
$applyDock = $mainType.GetMethod('ApplyDockConfigurationDraft', $private)
$revertDock = $mainType.GetMethod('DockRevertButton_Click', $private)
$trialDock = $mainType.GetMethod('DockTrialRunButton_Click', $private)
$captureRecipe = $mainType.GetMethod('CaptureRecipe', $private)
$resolveContext = $mainType.GetMethod('ResolveImageContext', $private)
$flowToolList = $mainType.GetField('FlowToolList', $allFields).GetValue($window)
$rightTabs = $mainType.GetField('RightTabs', $allFields).GetValue($window)
$dockTab = $mainType.GetField('DockConfigurationTab', $allFields).GetValue($window)
$dockModeCombo = $mainType.GetField('DockImageChannelModeComboBox', $allFields).GetValue($window)
$dockChannelIndex = $mainType.GetField('DockImageChannelIndexTextBox', $allFields).GetValue($window)
$dockInputRows = $mainType.GetField('dockInputPortRows', $private).GetValue($window)
$dockDirtyField = $mainType.GetField('dockDraftDirty', $private)
$contextCombo = $mainType.GetField('ImageContextComboBox', $allFields).GetValue($window)

$disposeTools.Invoke($window, @()) | Out-Null
$flow.Clear()

function New-Tool([string]$Kind, [string]$Name) {
    return $createTool.Invoke($window, @([Enum]::Parse($toolKindType, $Kind), $Name, $true, $null))
}

function Invoke-Tool($Tool, [string]$Label) {
    try {
        return $executeTool.Invoke($window, @($Tool, $Label))
    }
    catch [Reflection.TargetInvocationException] {
        throw $_.Exception.InnerException
    }
}

function Get-Output($Tool, [string]$Port) {
    $value = $null
    if (-not $Tool.TryGetOutputValue($Port, [ref]$value)) {
        throw "$($Tool.InstanceName).$Port has no output."
    }
    return $value
}

$keepMode = [string]$channelModeType.GetField('Keep').GetValue($null)
$grayMode = [string]$channelModeType.GetField('ToGray').GetValue($null)
$extractMode = [string]$channelModeType.GetField('Extract').GetValue($null)

$source = New-Tool 'ImageSource' 'RGB_Source'
$channel = New-Tool 'ImageChannel' 'Channel_Workbench'
$singleChannel = New-Tool 'ImageChannel' 'Gray_Compatibility'
$blob = New-Tool 'Blob' 'Blob_AfterChannel'
$gray = New-Tool 'GrayStat' 'Gray_AfterChannel'
$edge = New-Tool 'EdgeMeasure' 'Edge_AfterChannel'
$source.Parameters.LocalImagePath = $imagePath
$source.Parameters.LocalImageSerialNumber = 'S10-RGB-001'
$channel.Parameters.ImageChannelMode = $keepMode
$channel.Parameters.ImageChannelIndex = 1
$singleChannel.Parameters.ImageChannelMode = $grayMode
$blob.Parameters.BlobMinGray = 0
$blob.Parameters.BlobMaxGray = 255
$blob.Parameters.BlobMinArea = 0
$edge.Parameters.EdgeThreshold = 5
$flow.Add($source)
$flow.Add($channel)
$flow.Add($singleChannel)
$flow.Add($blob)
$flow.Add($gray)
$flow.Add($edge)

$flowToolList.SelectedItem = $channel
$openConfiguration.Invoke($window, @()) | Out-Null
Assert-True ([object]::ReferenceEquals($rightTabs.SelectedItem, $dockTab)) 'Supported tools must open the docked configuration tab instead of a modal window.'
Assert-True ($dockInputRows.Count -eq 1) 'ImageChannel must expose one editable Image input row in the dock.'
$imageRow = $dockInputRows[0]
$sourceOption = $imageRow.SourceOptions | Where-Object { $_.SourceToolId -eq $source.ToolId -and $_.SourcePortName -eq 'Image' } | Select-Object -First 1
Assert-True ($null -ne $sourceOption) 'Dock input editor did not list the previous Image source.'
$imageRow.SelectedSourceKey = $sourceOption.Key
$dockModeCombo.SelectedValue = $grayMode
$dockChannelIndex.Text = '1'
Assert-True ([bool]$dockDirtyField.GetValue($window)) 'Editing dock fields must mark the instance draft as dirty.'
$applied = [bool]$applyDock.Invoke($window, @($false))
Assert-True $applied 'Dock draft application failed.'
Assert-True (-not [bool]$dockDirtyField.GetValue($window)) 'Applying a dock draft must clear the draft dirty flag.'
Assert-True ($channel.Parameters.ImageChannelMode -eq $grayMode) 'Dock mode did not apply to the current instance.'
Assert-True ($channel.GetInputBinding('Image').SourceToolId -eq $source.ToolId) 'Dock Image binding did not apply.'

Invoke-Tool $source 'S10 source' | Out-Null
$sourceSnapshot = Get-Output $source 'Image'
$sourcePixel = $sourceSnapshot.GetPixelDisplay(5, 5).Split('/')
Assert-True ($sourcePixel.Count -eq 3) 'The RGB source must contain three HALCON channels.'

Invoke-Tool $channel 'S10 gray' | Out-Null
$graySnapshot = Get-Output $channel 'Image'
Assert-True ([double](Get-Output $channel 'InputChannels') -eq 3) 'Gray conversion input channel count must be 3.'
Assert-True ([double](Get-Output $channel 'OutputChannels') -eq 1) 'Gray conversion output channel count must be 1.'
Assert-True ($graySnapshot.GetPixelDisplay(5, 5).Split('/').Count -eq 1) 'Gray output pixel must contain one channel.'
$grayPixel = $graySnapshot.GetPixelDisplay(5, 5)

$singleChannel.SetInputBinding('Image', $channel.ToolId, 'Image')
Invoke-Tool $singleChannel 'S10 single channel gray compatibility' | Out-Null
Assert-True ([double](Get-Output $singleChannel 'InputChannels') -eq 1) 'Single-channel compatibility input count must be 1.'
Assert-True ([double](Get-Output $singleChannel 'OutputChannels') -eq 1) 'Single-channel ToGray must safely keep one channel.'

$blob.SetInputBinding('Image', $singleChannel.ToolId, 'Image')
$gray.SetInputBinding('Image', $singleChannel.ToolId, 'Image')
$edge.SetInputBinding('Image', $singleChannel.ToolId, 'Image')
Invoke-Tool $blob 'S10 downstream Blob' | Out-Null
Invoke-Tool $gray 'S10 downstream Gray' | Out-Null
Invoke-Tool $edge 'S10 downstream Edge' | Out-Null
Assert-True ($blob.ResultCode -eq 'OK' -and $gray.ResultCode -eq 'OK' -and $edge.ResultCode -eq 'OK') 'Blob/Gray/Edge did not run from ImageChannel.Image.'
$blobArea = Get-Output $blob 'Area'
$grayMean = Get-Output $gray 'Mean'
$edgeLength = Get-Output $edge 'Length'

$extractedValues = @()
for ($index = 1; $index -le 3; $index++) {
    $channel.Parameters.ImageChannelMode = $extractMode
    $channel.Parameters.ImageChannelIndex = $index
    Invoke-Tool $channel "S10 extract $index" | Out-Null
    $snapshot = Get-Output $channel 'Image'
    $value = $snapshot.GetPixelDisplay(5, 5)
    $extractedValues += $value
    Assert-True ($value -eq $sourcePixel[$index - 1]) "Extracted channel $index pixel does not match source tuple."
}
Assert-True (($extractedValues | Select-Object -Unique).Count -eq 3) 'The three extracted RGB channels must remain distinct.'

$channel.Parameters.ImageChannelMode = $extractMode
$channel.Parameters.ImageChannelIndex = 4
$invalidRejected = $false
$invalidDetails = '--'
try {
    Invoke-Tool $channel 'S10 invalid channel' | Out-Null
}
catch {
    $invalidDetails = "caught=$($_.Exception.Message); tool=$($channel.ErrorMessage)"
    $invalidRejected = -not [string]::IsNullOrWhiteSpace($channel.ErrorMessage) -and
        $channel.ErrorMessage.Contains('3') -and
        $channel.ErrorMessage.Contains('4')
}
Assert-True $invalidRejected "Extracting a channel beyond the input count must be rejected with a clear error. $invalidDetails"

$channel.Parameters.ImageChannelMode = $grayMode
$channel.Parameters.ImageChannelIndex = 1
Invoke-Tool $channel 'S10 restore gray' | Out-Null
$oldChannelSnapshot = Get-Output $channel 'Image'
Invoke-Tool $singleChannel 'S10 restore single' | Out-Null
Invoke-Tool $blob 'S10 restore downstream' | Out-Null
Invoke-Tool $source 'S10 source rerun' | Out-Null
Assert-True $sourceSnapshot.IsDisposed 'Rerunning the source must dispose the previous source snapshot.'
Assert-True $oldChannelSnapshot.IsDisposed 'Rerunning the source must invalidate and dispose downstream ImageChannel snapshots.'
Assert-True ($channel.ResultCode -eq '--' -and $blob.ResultCode -eq '--') 'Source rerun must invalidate the complete downstream Image chain.'

Invoke-Tool $channel 'S10 channel after source rerun' | Out-Null
$flowToolList.SelectedItem = $channel
$contextCombo.SelectedValue = $contextType.GetField('ModuleOutput').GetValue($null)
$context = $resolveContext.Invoke($window, @())
Assert-True ($context.HasImage -and $context.SourceText -like '*Channel_Workbench.Image*') 'Module output context did not resolve ImageChannel.Image.'

$flowToolList.SelectedItem = $channel
$dockModeCombo.SelectedValue = $extractMode
$dockChannelIndex.Text = '2'
$trialDock.Invoke($window, @($null, $null)) | Out-Null
Assert-True ($channel.Parameters.ImageChannelMode -eq $grayMode) 'Dock trial run must restore applied parameters after running the draft.'
$trialSnapshot = Get-Output $channel 'Image'
Assert-True ($trialSnapshot.GetPixelDisplay(5, 5) -eq $sourcePixel[1]) 'Dock trial run did not execute the draft channel mode.'
$revertDock.Invoke($window, @($null, $null)) | Out-Null
Assert-True ($dockModeCombo.SelectedValue -eq $grayMode -and -not [bool]$dockDirtyField.GetValue($window)) 'Revert must restore the applied mode and clear draft state.'

$recipe = $captureRecipe.Invoke($window, @())
$recipePath = Join-Path $tempRoot 's10-roundtrip.json'
$recipeService.SaveRecipe($recipePath, $recipe)
$loaded = $recipeService.LoadRecipe($recipePath)
$channelRecipe = $loaded.ToolFlow | Where-Object ToolId -eq $channel.ToolId
Assert-True ($channelRecipe.Parameters.ImageChannelMode -eq $grayMode -and $channelRecipe.Parameters.ImageChannelIndex -eq 1) 'ImageChannel parameters did not round-trip.'
Assert-True (($channelRecipe.InputBindings | Where-Object TargetPortName -eq 'Image').SourceToolId -eq $source.ToolId) 'ImageChannel Image binding did not round-trip.'

$finalSnapshot = Get-Output $channel 'Image'
$disposeTools.Invoke($window, @()) | Out-Null
Assert-True $finalSnapshot.IsDisposed 'Disposing the flow must dispose ImageChannel snapshots.'
$closeConfirmed = $mainType.GetField('recipeCloseConfirmed', $private)
$closeConfirmed.SetValue($window, $true)
$window.Close()

Write-Output ('S10_DOCK_CHANNEL=PASS; RGB={0}; Extract={1}; Gray={2}; Blob={3}; GrayMean={4}; Edge={5}; Dock=APPLY/TRIAL/REVERT; Recipe=PASS; Lifecycle=PASS; Context=PASS; InvalidChannel=REJECTED' -f ($sourcePixel -join '/'), ($extractedValues -join '/'), $grayPixel, $blobArea, $grayMean, $edgeLength)
