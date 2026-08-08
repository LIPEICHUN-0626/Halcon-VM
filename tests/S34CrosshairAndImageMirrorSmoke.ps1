param([string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot))

$ErrorActionPreference = 'Stop'
if ([Threading.Thread]::CurrentThread.ApartmentState -ne [Threading.ApartmentState]::STA) { throw 'Run with powershell.exe -STA.' }
$halconRoot = 'C:\Program Files\MVTec\HALCON-20.11-Progress'
$env:PATH = (Join-Path $halconRoot 'bin\x64-win64') + ';' + $env:PATH
Add-Type -AssemblyName PresentationFramework
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
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
function Write-GrayPgm([string]$Path, [int]$Width, [int]$Height) {
    $header = [Text.Encoding]::ASCII.GetBytes("P5`n$Width $Height`n255`n")
    $pixels = New-Object byte[] ($Width * $Height)
    for ($row = 0; $row -lt $Height; $row++) {
        for ($column = 0; $column -lt $Width; $column++) { $pixels[$row * $Width + $column] = [byte](($row * 3 + $column * 2) % 256) }
    }
    $bytes = New-Object byte[] ($header.Length + $pixels.Length)
    [Array]::Copy($header,0,$bytes,0,$header.Length); [Array]::Copy($pixels,0,$bytes,$header.Length,$pixels.Length)
    [IO.File]::WriteAllBytes($Path,$bytes)
}
function Write-MarkerPgm([string]$Path) {
    $header = [Text.Encoding]::ASCII.GetBytes("P5`n4 3`n255`n")
    $pixels = [byte[]](0,1,2,3,10,11,12,13,20,21,22,23)
    $bytes = New-Object byte[] ($header.Length + $pixels.Length)
    [Array]::Copy($header,0,$bytes,0,$header.Length); [Array]::Copy($pixels,0,$bytes,$header.Length,$pixels.Length)
    [IO.File]::WriteAllBytes($Path,$bytes)
}
function Write-ColorPpm([string]$Path) {
    $header = [Text.Encoding]::ASCII.GetBytes("P6`n4 3`n255`n")
    $pixels = New-Object byte[] 36
    for ($index = 0; $index -lt 12; $index++) { $pixels[$index * 3] = [byte]($index + 1); $pixels[$index * 3 + 1] = [byte]($index + 31); $pixels[$index * 3 + 2] = [byte]($index + 61) }
    $bytes = New-Object byte[] ($header.Length + $pixels.Length)
    [Array]::Copy($header,0,$bytes,0,$header.Length); [Array]::Copy($pixels,0,$bytes,$header.Length,$pixels.Length)
    [IO.File]::WriteAllBytes($Path,$bytes)
}

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('halcon-vm-s34-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($tempRoot) | Out-Null
$grayPath = Join-Path $tempRoot 'gray.pgm'; Write-GrayPgm $grayPath 100 80
$markerPath = Join-Path $tempRoot 'marker.pgm'; Write-MarkerPgm $markerPath
$colorPath = Join-Path $tempRoot 'color.ppm'; Write-ColorPpm $colorPath

$mainType = $assembly.GetType('HalconWinFormsDemo.MainWindow',$true)
$recipeServiceType = $assembly.GetType('HalconWinFormsDemo.Services.RecipeService',$true)
$loggerType = $assembly.GetType('HalconWinFormsDemo.Services.AppLogger',$true)
$toolKindType = $assembly.GetType('HalconWinFormsDemo.Models.VmToolKind',$true)
$metadataType = $assembly.GetType('HalconWinFormsDemo.Models.ToolMetadata',$true)
$roiType = $assembly.GetType('HalconWinFormsDemo.Models.RoiData',$true)
$private = [Reflection.BindingFlags]'Instance,NonPublic'
$all = [Reflection.BindingFlags]'Instance,NonPublic,Public'
$staticPublic = [Reflection.BindingFlags]'Static,Public'
$ctorArgs = New-Object 'object[]' 1; $ctorArgs[0] = $tempRoot.ToString()
$service = $recipeServiceType.GetConstructor([Type[]]@([string])).Invoke($ctorArgs)
$logger = $loggerType.GetConstructor([Type[]]@([string])).Invoke($ctorArgs)
$window = $mainType.GetConstructor($private,$null,[Type[]]@($recipeServiceType,$loggerType),$null).Invoke([object[]]@($service,$logger))
$flow = $mainType.GetField('flowTools',$private).GetValue($window)
$catalog = $mainType.GetField('toolCatalog',$private).GetValue($window)
$create = $mainType.GetMethod('CreateFlowTool',$private)
$execute = $mainType.GetMethod('ExecuteFlowTool',$private)
$runMirror = $mainType.GetMethod('RunImageMirrorTool',$private)
$loadImage = $mainType.GetMethod('LoadImage',$private)
$disposeFlow = $mainType.GetMethod('DisposeFlowTools',$private)
$captureRecipe = $mainType.GetMethod('CaptureRecipe',$private)
$setDualMode = $mainType.GetMethod('SetDualImageMode',$private)

function Field([string]$Name) { $mainType.GetField($Name,$all).GetValue($window) }
function New-Tool([string]$Kind,[string]$Name) { $create.Invoke($window,[object[]]@([Enum]::Parse($toolKindType,$Kind),$Name,$true,$null)) }
function Invoke-Execute($Tool,[string]$Source) {
    try { $execute.Invoke($window,[object[]]@($Tool,$Source)) }
    catch [Reflection.TargetInvocationException] { throw $_.Exception.InnerException }
}
function Invoke-Mirror($Tool,$Image) {
    try { $runMirror.Invoke($window,[object[]]@($Tool,$Image)) }
    catch [Reflection.TargetInvocationException] { throw $_.Exception.InnerException }
}
function Get-Out($Tool,[string]$Port) {
    $value = $null
    if (-not $Tool.TryGetOutputValue($Port,[ref]$value)) { throw "$($Tool.InstanceName).$Port missing" }
    $value
}

foreach ($fieldName in @(
    'CrosshairPanelToggleButton','CrosshairSettingsPanel','CrosshairTargetComboBox','CrosshairStatusText',
    'Crosshair1EnabledCheckBox','Crosshair1XTextBox','Crosshair1YTextBox','Crosshair1ColorComboBox','Crosshair1ThicknessComboBox','Crosshair1LockedCheckBox',
    'Crosshair2EnabledCheckBox','Crosshair2XTextBox','Crosshair2YTextBox','Crosshair2ColorComboBox','Crosshair2ThicknessComboBox','Crosshair2LockedCheckBox',
    'DockImageMirrorPanel','DockImageMirrorModeComboBox')) {
    Assert-True ($null -ne (Field $fieldName)) "$fieldName is missing."
}
Assert-True ((Field 'CrosshairSettingsPanel').Visibility.ToString() -eq 'Collapsed') 'Crosshair panel should default collapsed so a low-height image viewport is not squeezed out.'
$mainType.GetMethod('CrosshairPanelToggleButton_Click',$private).Invoke($window,[object[]]@($null,$null)) | Out-Null
Assert-True ((Field 'CrosshairSettingsPanel').Visibility.ToString() -eq 'Visible') 'Persistent in-workspace crosshair panel did not expand.'
Assert-True ($null -ne ($catalog | Where-Object { $_.Kind.ToString() -eq 'ImageMirror' } | Select-Object -First 1)) 'Image mirror is missing from the toolbox.'
$mirrorKind = [Enum]::Parse($toolKindType,'ImageMirror')
$inputs = $metadataType.GetMethod('GetInputPorts',$staticPublic).Invoke($null,[object[]]@($mirrorKind))
$outputs = $metadataType.GetMethod('GetOutputPorts',$staticPublic).Invoke($null,[object[]]@($mirrorKind))
Assert-True (($inputs | Where-Object { $_.PortName -eq 'Image' -and $_.DataType -eq 'Image' -and -not $_.IsOptional }).Count -eq 1) 'Image mirror required typed input is missing.'
foreach ($port in @('Image','Width','Height','Channels','Mode','Parameters','ResultCode')) { Assert-True (($outputs | Where-Object PortName -eq $port).Count -eq 1) "Image mirror output $port is missing." }

$app = [Windows.Application]::Current
if ($null -eq $app) { $app = New-Object Windows.Application; $app.ShutdownMode = [Windows.ShutdownMode]::OnExplicitShutdown }
$disposeFlow.Invoke($window,@()) | Out-Null; $flow.Clear()
$source = New-Tool ImageSource 'S34 Source'
$mirror = New-Tool ImageMirror 'S34 Mirror'
$threshold = New-Tool ImageThreshold 'S34 Threshold'
$source.Parameters.LocalImagePath = $grayPath; $source.Parameters.LocalImageSerialNumber = 'S34-GRAY'
$mirror.Parameters.ImageMirrorMode = 'row'; $mirror.SetInputBinding('Image',$source.ToolId,'Image')
$threshold.Parameters.ImageThresholdMinGray = 0; $threshold.Parameters.ImageThresholdMaxGray = 255; $threshold.SetInputBinding('Image',$mirror.ToolId,'Image')
foreach ($tool in @($source,$mirror,$threshold)) { $flow.Add($tool) }

$window.WindowStartupLocation = [Windows.WindowStartupLocation]::Manual
$window.Left = 2; $window.Top = 2; $window.Width = 1550; $window.Height = 970; $window.Topmost = $true
$window.Show(); $window.Activate() | Out-Null; Pump-Ui 450
$loadImage.Invoke($window,[object[]]@($grayPath.ToString(),$false)) | Out-Null
Invoke-Execute $source 'S34 source' | Out-Null
Invoke-Execute $mirror 'S34 row flow' | Out-Null
Invoke-Execute $threshold 'S34 downstream threshold' | Out-Null
Assert-True ($mirror.ResultCode -eq 'OK' -and $threshold.ResultCode -eq 'OK' -and $null -ne (Get-Out $threshold Region)) 'Mirrored Image did not drive downstream threshold.'
$firstSnapshot = Get-Out $mirror Image
$mirror.Parameters.ImageMirrorMode = 'column'; Invoke-Execute $mirror 'S34 rerun column' | Out-Null
Assert-True $firstSnapshot.IsDisposed 'Image mirror rerun did not release the previous managed snapshot.'

# Exact grayscale marker mapping for row/column/diagonal, including non-square width/height swap.
$markerImage = [HalconDotNet.HImage]::new($markerPath)
try {
    foreach ($mode in @('row','column','diagonal')) {
        $tool = New-Tool ImageMirror ("S34 " + $mode); $tool.Parameters.ImageMirrorMode = $mode
        try {
            Invoke-Mirror $tool $markerImage | Out-Null
            $copy = (Get-Out $tool Image).CreateImageCopy()
            try {
                if ($mode -eq 'row') {
                    Assert-True ([int](Get-Out $tool Width) -eq 4 -and [int](Get-Out $tool Height) -eq 3) 'Row mirror dimensions are wrong.'
                    Assert-True ([double]$copy.GetGrayval(0,0).D -eq 20 -and [double]$copy.GetGrayval(2,3).D -eq 3) 'Row mirror marker mapping is wrong.'
                } elseif ($mode -eq 'column') {
                    Assert-True ([double]$copy.GetGrayval(0,0).D -eq 3 -and [double]$copy.GetGrayval(2,3).D -eq 20) 'Column mirror marker mapping is wrong.'
                } else {
                    Assert-True ([int](Get-Out $tool Width) -eq 3 -and [int](Get-Out $tool Height) -eq 4) 'Diagonal non-square mirror did not swap dimensions.'
                    Assert-True ([double]$copy.GetGrayval(0,2).D -eq 20 -and [double]$copy.GetGrayval(3,0).D -eq 3 -and [double]$copy.GetGrayval(3,2).D -eq 23) 'Diagonal marker mapping is wrong.'
                }
                Assert-True ((Get-Out $tool Mode) -eq $mode) 'Mirror Mode output is not the normalized HALCON mode.'
            }
            finally { $copy.Dispose() }
        }
        finally { $tool.Dispose() }
    }
}
finally { $markerImage.Dispose() }

# RGB channels and a reduced input domain must both survive HALCON mirror_image semantics.
$rgbTool = New-Tool ImageMirror 'S34 RGB'; $rgbTool.Parameters.ImageMirrorMode = 'diagonal'; $rgbImage = [HalconDotNet.HImage]::new($colorPath)
try {
    Invoke-Mirror $rgbTool $rgbImage | Out-Null
    Assert-True ([int](Get-Out $rgbTool Channels) -eq 3 -and [int](Get-Out $rgbTool Width) -eq 3 -and [int](Get-Out $rgbTool Height) -eq 4) 'RGB diagonal mirror did not preserve channels/swap dimensions.'
}
finally { $rgbImage.Dispose(); $rgbTool.Dispose() }

$domainTool = New-Tool ImageMirror 'S34 Domain'; $domainTool.Parameters.ImageMirrorMode = 'row'; $domainImage = [HalconDotNet.HImage]::new($markerPath)
$domainRoi = $roiType.GetMethod('CreateRectangle',$staticPublic).Invoke($null,[object[]]@([double]0,[double]1,[double]0,[double]2)); $reduced = $domainImage.ReduceDomain($domainRoi.Region)
try {
    Invoke-Mirror $domainTool $reduced | Out-Null
    $domainOutput = (Get-Out $domainTool Image).CreateImageCopy(); $outputDomain = $domainOutput.GetDomain()
    try {
        $area = $null; $row = $null; $column = $null; [HalconDotNet.HOperatorSet]::AreaCenter($outputDomain,[ref]$area,[ref]$row,[ref]$column)
        Assert-True ([double]$area.D -eq 2 -and [Math]::Abs([double]$row.D - 2) -lt 1e-9 -and [Math]::Abs([double]$column.D - 1.5) -lt 1e-9) 'Reduced domain was not mirrored with the row image.'
    }
    finally { $outputDomain.Dispose(); $domainOutput.Dispose() }
}
finally { $reduced.Dispose(); $domainRoi.Dispose(); $domainImage.Dispose(); $domainTool.Dispose() }

$invalidTool = New-Tool ImageMirror 'S34 Invalid'; $invalidTool.Parameters.ImageMirrorMode = 'invented'; $invalidImage = [HalconDotNet.HImage]::new($markerPath)
try {
    $rejected = $false; try { Invoke-Mirror $invalidTool $invalidImage | Out-Null } catch { $rejected = $true }
    Assert-True $rejected 'Invalid mirror mode was accepted.'
}
finally { $invalidImage.Dispose(); $invalidTool.Dispose() }

# Configure two right-view guides and two isolated left-view guides without a popup.
$list = Field 'FlowToolList'; $list.SelectedItem = $mirror; Pump-Ui 80
$setDualMode.Invoke($window,[object[]]@($true)) | Out-Null; Pump-Ui 100
$setUiUpdating = $mainType.GetField('crosshairUiUpdating',$private)
$applyCrosshair = $mainType.GetMethod('ApplyCrosshairEditorValues',$private)
function Set-Crosshairs([string]$Target,[double]$X1,[double]$Y1,[double]$X2,[double]$Y2,[bool]$Lock2) {
    $setUiUpdating.SetValue($window,$true)
    try {
        (Field 'CrosshairTargetComboBox').SelectedValue = $Target
        (Field 'Crosshair1EnabledCheckBox').IsChecked = $true; (Field 'Crosshair1XTextBox').Text = $X1.ToString([Globalization.CultureInfo]::InvariantCulture); (Field 'Crosshair1YTextBox').Text = $Y1.ToString([Globalization.CultureInfo]::InvariantCulture)
        (Field 'Crosshair1ColorComboBox').SelectedValue = 'cyan'; (Field 'Crosshair1ThicknessComboBox').SelectedItem = 3; (Field 'Crosshair1LockedCheckBox').IsChecked = $false
        (Field 'Crosshair2EnabledCheckBox').IsChecked = $true; (Field 'Crosshair2XTextBox').Text = $X2.ToString([Globalization.CultureInfo]::InvariantCulture); (Field 'Crosshair2YTextBox').Text = $Y2.ToString([Globalization.CultureInfo]::InvariantCulture)
        (Field 'Crosshair2ColorComboBox').SelectedValue = 'yellow'; (Field 'Crosshair2ThicknessComboBox').SelectedItem = 2; (Field 'Crosshair2LockedCheckBox').IsChecked = $Lock2
    }
    finally { $setUiUpdating.SetValue($window,$false) }
    Assert-True ($applyCrosshair.Invoke($window,@())) "Crosshair settings for $Target were rejected."
}
Set-Crosshairs 'Right' 20 30 55 65 $true
Set-Crosshairs 'Left' 70 60 10 15 $false
$rightSettings = $mainType.GetField('rightImageCrosshairs',$private).GetValue($window)
$leftSettings = $mainType.GetField('leftImageCrosshairs',$private).GetValue($window)
Assert-True ($rightSettings.Guide1.X -eq 20 -and $rightSettings.Guide1.Y -eq 30 -and $rightSettings.Guide2.IsLocked) 'Right-view crosshair state is wrong.'
Assert-True ($leftSettings.Guide1.X -eq 70 -and $leftSettings.Guide1.Y -eq 60 -and $leftSettings.Guide1.X -ne $rightSettings.Guide1.X) 'Left/right crosshair coordinates are not isolated.'

# Display-only changes must not mutate the managed algorithm output or its real input binding.
$stableSnapshot = Get-Out $mirror Image; $bindingBefore = $mirror.InputBindings[0].SourceToolId
Set-Crosshairs 'Right' 25 35 55 65 $true
$rightSettings = $mainType.GetField('rightImageCrosshairs',$private).GetValue($window)
Assert-True ([Object]::ReferenceEquals($stableSnapshot,(Get-Out $mirror Image)) -and -not $stableSnapshot.IsDisposed -and $mirror.InputBindings[0].SourceToolId -eq $bindingBefore) 'Crosshair display settings mutated algorithm output/binding state.'

# Unlocked intersection dragging is direct and bidirectional; the locked second guide rejects drag.
$tryBegin = $mainType.GetMethod('TryBeginCrosshairDrag',$private); $updateDrag = $mainType.GetMethod('UpdateCrosshairDrag',$private); $finishDrag = $mainType.GetMethod('FinishCrosshairDrag',$private)
$rightViewport = $mainType.GetField('viewport',$private).GetValue($window); $rightControl = $mainType.GetField('imageWindow',$private).GetValue($window)
$guide1WindowPoint = [Drawing.Point]::new([int](25.0 / 100.0 * $rightControl.Width),[int](35.0 / 80.0 * $rightControl.Height))
$began = $tryBegin.Invoke($window,[object[]]@('Right',[Drawing.PointF]::new(25,35),$guide1WindowPoint,$rightViewport,$rightControl))
Assert-True $began 'Unlocked crosshair intersection did not begin direct drag.'
$updateDrag.Invoke($window,[object[]]@('Right',[Drawing.PointF]::new(32,42))) | Out-Null; $finishDrag.Invoke($window,[object[]]@('Right',$rightControl)) | Out-Null
Assert-True ([Math]::Abs($rightSettings.Guide1.X - 32) -lt .001 -and [Math]::Abs($rightSettings.Guide1.Y - 42) -lt .001) 'Direct crosshair drag did not update numeric coordinates.'
$guide2WindowPoint = [Drawing.Point]::new([int](55.0 / 100.0 * $rightControl.Width),[int](65.0 / 80.0 * $rightControl.Height))
$lockedBegan = $tryBegin.Invoke($window,[object[]]@('Right',[Drawing.PointF]::new(55,65),$guide2WindowPoint,$rightViewport,$rightControl))
Assert-True (-not $lockedBegan) 'Locked crosshair guide still accepted direct drag.'

# Save/load must preserve both view settings and mirror mode.
$mirror.Parameters.ImageMirrorMode = 'diagonal'
$recipe = $captureRecipe.Invoke($window,@()); $recipePath = Join-Path $tempRoot 's34-recipe.json'; $service.SaveRecipe($recipePath,$recipe); $loaded = $service.LoadRecipe($recipePath)
$loadedMirror = $loaded.ToolFlow | Where-Object ToolType -eq 'ImageMirror' | Select-Object -First 1
Assert-True ($null -ne $loadedMirror -and $loadedMirror.Parameters.ImageMirrorMode -eq 'diagonal') 'Image mirror mode did not survive recipe save/load.'
Assert-True ([Math]::Abs($loaded.RightImageCrosshairs.Guide1.X - 32) -lt .001 -and [Math]::Abs($loaded.RightImageCrosshairs.Guide1.Y - 42) -lt .001 -and $loaded.RightImageCrosshairs.Guide2.IsLocked) 'Right-view crosshairs did not survive recipe save/load.'
Assert-True ($loaded.LeftImageCrosshairs.Guide1.X -eq 70 -and $loaded.LeftImageCrosshairs.Guide1.Y -eq 60) 'Left-view crosshairs did not survive recipe save/load.'

$mainType.GetField('recipeCloseConfirmed',$private).SetValue($window,$true)
$window.Close(); $app.Shutdown()
if (Test-Path $tempRoot) { Remove-Item -LiteralPath $tempRoot -Recurse -Force }
Write-Output 'S34 VM-style double crosshair guides and HALCON image mirror smoke tests passed.'
