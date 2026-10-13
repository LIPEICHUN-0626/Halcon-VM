param([string]$ProjectRoot=(Split-Path -Parent $PSScriptRoot))
$ErrorActionPreference='Stop'
if([Threading.Thread]::CurrentThread.ApartmentState-ne[Threading.ApartmentState]::STA){throw 'Run with powershell -STA.'}
Add-Type -AssemblyName PresentationFramework
Add-Type -AssemblyName System.Drawing
$halcon='C:\Program Files\MVTec\HALCON-20.11-Progress'
$env:PATH=(Join-Path $halcon 'bin\x64-win64')+';'+$env:PATH
[Reflection.Assembly]::LoadFrom((Join-Path $halcon 'bin\dotnet35\halcondotnet.dll'))|Out-Null
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $ProjectRoot 'bin\x64\Debug\HalconWinFormsDemo.exe'))
$main=$assembly.GetType('HalconWinFormsDemo.MainWindow',$true)
$recipeType=$assembly.GetType('HalconWinFormsDemo.Models.VisionRecipe',$true)
$serviceType=$assembly.GetType('HalconWinFormsDemo.Services.RecipeService',$true)
$loggerType=$assembly.GetType('HalconWinFormsDemo.Services.AppLogger',$true)
$private=[Reflection.BindingFlags]'Instance,NonPublic';$all=[Reflection.BindingFlags]'Instance,NonPublic,Public'
$temp=Join-Path ([IO.Path]::GetTempPath()) ('halcon-s90-'+[Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($temp)|Out-Null
$service=$serviceType.GetConstructor([Type[]]@([string])).Invoke([object[]]@($temp.ToString()))
$logger=$loggerType.GetConstructor([Type[]]@([string])).Invoke([object[]]@($temp.ToString()))
$app=New-Object Windows.Application;$app.ShutdownMode=[Windows.ShutdownMode]::OnExplicitShutdown
$window=$null
function Assert([bool]$ok,[string]$message){if(-not$ok){throw $message}}
function F([string]$name){Write-Output -NoEnumerate ($main.GetField($name,$all).GetValue($window))}
function SetF([string]$name,$value){$main.GetField($name,$all).SetValue($window,$value)}
function M([string]$name,[object[]]$arguments=@()){
    $method=$main.GetMethods($private)|Where-Object{$_.Name-eq$name-and$_.GetParameters().Count-eq$arguments.Count}|Select-Object -First 1
    $actual=@($arguments|ForEach-Object{if($null-eq$_){$null}else{$_.PSObject.BaseObject}})
    try{Write-Output -NoEnumerate ($method.Invoke($window,$actual))}catch [Reflection.TargetInvocationException]{throw $_.Exception.InnerException}
}
function Blank{Write-Output -NoEnumerate ($recipeType.GetMethod('CreateBlank').Invoke($null,@()))}
function Pump([int]$ms){$frame=New-Object Windows.Threading.DispatcherFrame;$timer=New-Object Windows.Threading.DispatcherTimer;$timer.Interval=[TimeSpan]::FromMilliseconds($ms);$timer.Add_Tick({$timer.Stop();$frame.Continue=$false});$timer.Start();[Windows.Threading.Dispatcher]::PushFrame($frame)}
function Capture([string]$name){
    $folder=Join-Path $ProjectRoot 'artifacts\s90';[IO.Directory]::CreateDirectory($folder)|Out-Null
    $window.UpdateLayout();$bitmap=[Windows.Media.Imaging.RenderTargetBitmap]::new([int][Math]::Ceiling($window.ActualWidth),[int][Math]::Ceiling($window.ActualHeight),96,96,[Windows.Media.PixelFormats]::Pbgra32)
    $bitmap.Render($window);$encoder=New-Object Windows.Media.Imaging.PngBitmapEncoder;$encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
    $stream=[IO.File]::Open((Join-Path $folder $name),[IO.FileMode]::Create);try{$encoder.Save($stream)}finally{$stream.Dispose()}
}
try{
    $legacy=[Activator]::CreateInstance($recipeType);$legacy.Name='Legacy startup fixture'
    $legacyPath=Join-Path $temp 'previous.json';$service.SaveRecipe($legacyPath,$legacy)
    $layout=[Activator]::CreateInstance($assembly.GetType('HalconWinFormsDemo.Models.UiLayoutState',$true));$layout.LastRecipePath=$legacyPath;$service.SaveLayout($layout)
    $recovery=[Activator]::CreateInstance($assembly.GetType('HalconWinFormsDemo.Models.RecipeRecoveryData',$true))
    $recovery.Recipe=$legacy;$recovery.CapturedAtUtc=[DateTime]::UtcNow.ToString('O');$service.SaveRecovery($recovery)
    $recoveryHash=(Get-FileHash -LiteralPath $service.RecoveryPath).Hash;$legacyHash=(Get-FileHash -LiteralPath $legacyPath).Hash
    $window=$main.GetConstructor($private,$null,[Type[]]@($serviceType,$loggerType),$null).Invoke([object[]]@($service,$logger))
    Assert((F flowTools).Count-eq0) 'Constructor inserted a default tool'
    $window.WindowState=[Windows.WindowState]::Normal;$window.ShowActivated=$false;$window.Width=1366;$window.Height=768;$window.Show();Pump 300
    Assert((F flowTools).Count-eq0-and[string]::IsNullOrEmpty((F currentRecipePath))) 'Startup silently loaded last recipe'
    Assert((F RecipeNameEditTextBox).Text-eq'未命名方案') 'Blank workspace has a misleading recipe name'
    Assert((F FlowGraphEmptyState).Visibility-eq[Windows.Visibility]::Visible) 'Blank-flow onboarding is missing'
    Assert(-not(F RunOnceButton).IsEnabled) 'Empty flow can be executed'
    Assert(@((F recentRecipes)|Where-Object Path -eq $legacyPath).Count-eq1) 'Last recipe is no longer discoverable'
    Assert($null-ne(F pendingRecovery)-and(Get-FileHash -LiteralPath $service.RecoveryPath).Hash-eq$recoveryHash) 'Startup consumed or altered recovery'
    Assert((Get-FileHash -LiteralPath $legacyPath).Hash-eq$legacyHash) 'Startup changed the previous recipe'
    Capture 'blank-startup-1366x768.png'
    $window.Width=1938;$window.Height=1158;Pump 100;Capture 'blank-startup-1938x1158.png'
    $blank=Blank;$blankPath=Join-Path $temp 'empty.json';$service.SaveRecipe($blankPath,$blank);$loaded=$service.LoadRecipe($blankPath)
    Assert($null-ne$loaded.ToolFlow-and$loaded.ToolFlow.Count-eq0) 'Empty flow did not survive JSON persistence'
    # A present empty flow is authoritative, even if stale legacy flags say otherwise.
    $loaded.EnableShapeMatch=$true;$loaded.EnableBlob=$true
    M ApplyRecipe @($loaded)|Out-Null;Assert((F flowTools).Count-eq0) 'Explicit empty flow resurrected legacy modules'
    M ApplyRecipe @($service.LoadRecipe($legacyPath))|Out-Null
    Assert((F flowTools).Count-eq1-and(F flowTools)[0].Kind.ToString()-eq'ShapeMatch') 'Explicit legacy recipe lost migration support'
    $seedImage=New-Object HalconDotNet.HImage;$seedImage.GenImageConst('byte',64,48)
    M ReplaceCurrentImage @($seedImage,$true)|Out-Null
    $roiType=$assembly.GetType('HalconWinFormsDemo.Models.RoiData',$true)
    $roi=$roiType.GetMethod('CreateRectangle').Invoke($null,[object[]]@([double]5,[double]5,[double]25,[double]30))
    M AddRoiLayer @($roi,'Previous scheme ROI',(F flowTools)[0])|Out-Null
    Assert($null-ne(F currentImage)-and(F roiLayers).Count-eq1) 'Populated image/ROI fixture was not installed'
    # Exercise New from populated state without interacting with confirmation UI.
    SetF pendingRecovery $null;SetF isRecipeDirty $false
    M NewRecipeButton_Click @($null,$null)|Out-Null
    Assert((F flowTools).Count-eq0-and(F roiLayers).Count-eq0-and$null-eq(F currentImage)-and$null-eq(F originalImage)) 'New scheme retained tools, ROI or image'
    Assert(-not$seedImage.IsInitialized()) 'New scheme did not release its previous HALCON image'
    Assert([string]::IsNullOrEmpty((F currentRecipePath))-and(F isRecipeDirty)) 'New scheme retained a save path or lost first-save state'
    # Empty snapshot must stay empty across undo/redo after adding the first real tool.
    $before=M CaptureFlowEditSnapshot
    $kind=[Enum]::Parse($assembly.GetType('HalconWinFormsDemo.Models.VmToolKind'),'ImageSource')
    $source=M CreateFlowTool @($kind,'First source',$true,$null);(F flowTools).Add($source)
    M CommitFlowEditHistory @('Add first source',$before)|Out-Null
    M UndoFlowEditButton_Click @($null,$null)|Out-Null;Assert((F flowTools).Count-eq0) 'Undo resurrected a default matcher'
    M RedoFlowEditButton_Click @($null,$null)|Out-Null;Assert((F flowTools).Count-eq1-and(F flowTools)[0].Kind.ToString()-eq'ImageSource') 'Redo failed to restore the first source'
    Assert((Get-FileHash -LiteralPath $legacyPath).Hash-eq$legacyHash) 'New/open/undo modified the old recipe file'
    Write-Output 'S90_BLANK=PASS; Startup=0; NoAutoLoad=True; Recent=True; RecoveryUntouched=True; EmptyJson=True; LegacyShape=True; New=0; Undo=0; Redo=ImageSource; Captures=2'
}finally{
    if($null-ne$window){SetF dockDraftDirty $false;SetF recipeCloseConfirmed $true;$window.Close()};$app.Shutdown()
    $resolved=[IO.Path]::GetFullPath($temp);$parent=[IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\')+'\'
    if(-not$resolved.StartsWith($parent,[StringComparison]::OrdinalIgnoreCase)-or-not[IO.Path]::GetFileName($resolved).StartsWith('halcon-s90-')){throw 'Unsafe test cleanup path'}
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
