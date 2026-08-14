param([string]$ProjectRoot=(Split-Path -Parent $PSScriptRoot))
$ErrorActionPreference='Stop'
if([Threading.Thread]::CurrentThread.ApartmentState-ne[Threading.ApartmentState]::STA){throw 'Run with powershell.exe -STA.'}
$halcon='C:\Program Files\MVTec\HALCON-20.11-Progress';$env:PATH=(Join-Path $halcon 'bin\x64-win64')+';'+$env:PATH
Add-Type -AssemblyName PresentationFramework
if(-not('S54LoopbackServer' -as [type])){Add-Type -TypeDefinition @'
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
public sealed class S54LoopbackServer : IDisposable {
  private TcpListener listener; private Thread thread;
  public int Port { get; private set; } public string ReceivedText { get; private set; } public bool ClientClosed { get; private set; }
  public S54LoopbackServer(){listener=new TcpListener(IPAddress.Loopback,0);listener.Start();Port=((IPEndPoint)listener.LocalEndpoint).Port;thread=new Thread(Run);thread.IsBackground=true;thread.Start();}
  private void Run(){try{using(TcpClient client=listener.AcceptTcpClient()){using(NetworkStream stream=client.GetStream()){byte[] b=new byte[65536];int n=stream.Read(b,0,b.Length);ReceivedText=Encoding.UTF8.GetString(b,0,Math.Max(0,n));byte[] reply=Encoding.UTF8.GetBytes("ACK:"+ReceivedText);stream.Write(reply,0,reply.Length);stream.Flush();}}}catch{}finally{ClientClosed=true;try{listener.Stop();}catch{}}}
  public void Dispose(){try{listener.Stop();}catch{}if(thread!=null&&thread.IsAlive)thread.Join(1000);}
}
'@}
function Assert([bool]$condition,[string]$message){if(-not$condition){throw $message}}
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $ProjectRoot 'bin\x64\Debug\HalconWinFormsDemo.exe'))
$mainType=$assembly.GetType('HalconWinFormsDemo.MainWindow',$true);$serviceType=$assembly.GetType('HalconWinFormsDemo.Services.RecipeService',$true);$loggerType=$assembly.GetType('HalconWinFormsDemo.Services.AppLogger',$true);$kindType=$assembly.GetType('HalconWinFormsDemo.Models.VmToolKind',$true);$metadataType=$assembly.GetType('HalconWinFormsDemo.Models.ToolMetadata',$true)
$private=[Reflection.BindingFlags]'Instance,NonPublic';$all=[Reflection.BindingFlags]'Instance,NonPublic,Public';$static=[Reflection.BindingFlags]'Static,Public,NonPublic';$temp=Join-Path([IO.Path]::GetTempPath())('halcon-vm-s54-'+[Guid]::NewGuid().ToString('N'));[IO.Directory]::CreateDirectory($temp)|Out-Null
$service=$serviceType.GetConstructor([Type[]]@([string])).Invoke([object[]]@($temp.ToString()));$logger=$loggerType.GetConstructor([Type[]]@([string])).Invoke([object[]]@($temp.ToString()));$window=$mainType.GetConstructor($private,$null,[Type[]]@($serviceType,$loggerType),$null).Invoke([object[]]@($service,$logger))
function Field([string]$name){$value=$mainType.GetField($name,$all).GetValue($window);Write-Output -NoEnumerate $value}
function Kind([string]$name){[Enum]::Parse($kindType,$name)}
function Tool([string]$kind,[string]$name){$mainType.GetMethod('CreateFlowTool',$private).Invoke($window,[object[]]@((Kind $kind),$name,$true,$null))}
function Out($tool,[string]$name){$args=[object[]]@($name,$null);Assert($tool.GetType().GetMethod('TryGetOutputValue').Invoke($tool,$args))("Missing output "+$name);$args[1]}
function Run($tool){$mainType.GetMethod('ExecuteFlowTool',$private).Invoke($window,[object[]]@($tool,'S54 smoke'))}
function LoadDock($tool){$mainType.GetMethod('LoadDockConfigurationDraft',$private).Invoke($window,[object[]]@($tool))|Out-Null}
try{
  $supports=$mainType.GetMethod('SupportsDockConfiguration',$static);$allKinds=[Enum]::GetValues($kindType);$covered=0
  foreach($kind in $allKinds){if(-not[bool]$supports.Invoke($null,[object[]]@($kind))){continue};$tool=$mainType.GetMethod('CreateFlowTool',$private).Invoke($window,[object[]]@($kind,('Priority '+$kind),$true,$null));LoadDock $tool;$covered++;Assert((Field 'DockParameterPriorityBar').Visibility.ToString()-eq'Visible')("Missing priority bar for "+$kind);$summary=[string](Field 'DockKeyParameterSummaryText').Text;$match=[regex]::Match($summary,'(\d+)');Assert($match.Success)("Missing key count for "+$kind+': '+$summary);Assert([int]$match.Groups[1].Value-le6)("More than six key parameters for "+$kind+': '+$summary)}
  Assert($covered-ge40) 'Priority hierarchy did not cover the migrated dock tools'

  $flow=Field 'flowTools';$source=Tool ImageSource 'Assembly Inputs';$text=Tool TextAssembly 'Text Assembly';$tcp=Tool TcpClientExchange 'TCP Consumer';$flow.Add($source);$flow.Add($text);$flow.Add($tcp)
  $unicode=([string][char]0x6279)+([string][char]0x6B21)+'-A7';$source.SetOutputValue('SN',$unicode);$source.SetOutputValue('Width',[double]12.3456);$source.SetOutputValue('ResultCode','OK');$source.ResultCode='OK'
  $text.SetInputBinding('String',$source.ToolId,'SN');$text.SetInputBinding('Number',$source.ToolId,'Width');$text.SetInputBinding('ResultCode',$source.ToolId,'ResultCode')
  $text.Parameters.TextAssemblyTemplate='SN={String}{Separator}N={Number}{Separator}R={ResultCode}{Separator}V={Values}';$text.Parameters.TextAssemblySeparator='\t';$text.Parameters.TextAssemblyNumberFormat='F2';$text.Parameters.TextAssemblyEmptyPolicy='Keep'
  $textInputs=$metadataType.GetMethod('GetInputPorts',[Reflection.BindingFlags]'Static,Public').Invoke($null,[object[]]@((Kind TextAssembly)));$textOutputs=$metadataType.GetMethod('GetOutputPorts',[Reflection.BindingFlags]'Static,Public').Invoke($null,[object[]]@((Kind TextAssembly)));
  Assert($textInputs.Count-eq3-and($textOutputs|Where-Object PortName -eq 'Text').DataType-eq'String'-and($textOutputs|Where-Object PortName -eq 'Length').DataType-eq'Number') 'Text Assembly typed contract is incomplete'
  $record=Run $text;$expected="SN=$unicode`tN=12.35`tR=OK`tV=$unicode`t12.35`tOK";Assert($record.ResultCode-eq'OK'-and(Out $text Text)-eq$expected) 'Unicode/template/separator/number formatting failed';Assert([int](Out $text Length)-eq$expected.Length) 'Text length output failed'

  $source.SetOutputValue('SN','');$text.Parameters.TextAssemblyTemplate='{Values}';$text.Parameters.TextAssemblySeparator=',';$text.Parameters.TextAssemblyEmptyPolicy='Keep';Run $text|Out-Null;Assert((Out $text Text)-eq',12.35,OK') 'Keep-empty policy failed'
  $text.Parameters.TextAssemblyEmptyPolicy='Skip';Run $text|Out-Null;Assert((Out $text Text)-eq'12.35,OK') 'Skip-empty policy failed'
  $text.Parameters.TextAssemblyEmptyPolicy='Reject';$rejected=$false;try{Run $text|Out-Null}catch{$rejected=$true};Assert($rejected-and([string]$text.ErrorMessage).Length-gt0) 'Reject-empty policy failed'
  $source.SetOutputValue('SN',$unicode);$text.Parameters.TextAssemblyEmptyPolicy='Keep';$text.Parameters.TextAssemblyNumberFormat='Q2';$invalid=$false;try{Run $text|Out-Null}catch{$invalid=$true};Assert($invalid) 'Invalid number format was not rejected'
  $text.Parameters.TextAssemblyNumberFormat='0.###';$text.Parameters.TextAssemblyTemplate='LINE={String}{Separator}{ResultCode}';$text.Parameters.TextAssemblySeparator='\n';Run $text|Out-Null;Assert((Out $text Text)-eq("LINE="+$unicode+"`nOK")) 'Escaped newline separator failed'

  $tcp.SetInputBinding('String',$text.ToolId,'Text');$tcp.Parameters.TcpClientHost='127.0.0.1';$tcp.Parameters.TcpClientConnectTimeoutMilliseconds=500;$tcp.Parameters.TcpClientSendTimeoutMilliseconds=500;$tcp.Parameters.TcpClientReceiveTimeoutMilliseconds=500;$tcp.Parameters.TcpClientEncoding='UTF-8';$tcp.Parameters.TcpClientLineEnding='None';$tcp.Parameters.TcpClientSendTemplate='{String}';$tcp.Parameters.TcpClientWaitForResponse=$true
  $server=[S54LoopbackServer]::new();try{$tcp.Parameters.TcpClientPort=$server.Port;$tcpRecord=Run $tcp;Assert($tcpRecord.ResultCode-eq'OK') 'Text to TCP execution failed';Assert((Out $tcp ReceivedText)-eq('ACK:'+(Out $text Text))) 'TCP did not receive the real Text output'}finally{$server.Dispose()};Assert($server.ClientClosed) 'TCP resource was not released'
  $judge=Tool NumericJudge 'Length Judge';$judge.SetInputBinding('Value',$text.ToolId,'Length');$judge.InputToolId=$text.ToolId;$judge.InputPortName='Length';$judge.NumericLowerLimit=1;$judge.NumericUpperLimit=1000;$flow.Add($judge);Assert((Run $judge).ResultCode-eq'OK') 'Text Length Number downstream failed'

  LoadDock $text;Assert((Field 'DockTextAssemblyPanel').Visibility.ToString()-eq'Visible') 'Text Assembly dock panel is missing';$advancedRows=Field 'dockAdvancedParameterRows';Assert($advancedRows.Count-gt0-and$advancedRows[0].Row.Height.Value-eq0) 'Text Assembly advanced field is not collapsed by default'
  (Field 'DockTextAssemblyNumberFormatTextBox').Text='Q2';$read=$mainType.GetMethod('TryReadDockConfigurationDraft',$private);$args=[object[]]@($null,$null,$null,$null,$null,$null,$null);Assert(-not[bool]$read.Invoke($window,$args)) 'Invalid advanced draft unexpectedly passed';$expanded=Field 'dockAdvancedParameterExpandedByKind';Assert([bool]$expanded[(Kind TextAssembly)]) 'Advanced validation did not auto-expand';Assert((Field 'DockAdvancedParameterValidationText').Visibility.ToString()-eq'Visible') 'Advanced validation was not shown in its group'
  $simple=Tool ImageFilter 'Simple Filter';LoadDock $simple;LoadDock $text;$expanded=Field 'dockAdvancedParameterExpandedByKind';Assert([bool]$expanded[(Kind TextAssembly)]) 'Advanced expansion state was not remembered by tool kind'

  (Field 'DockTextAssemblyNumberFormatTextBox').Text='F1';(Field 'DockTextAssemblyTemplateTextBox').Text='APPLIED-{Values}';$oldTemplate=$text.Parameters.TextAssemblyTemplate;$apply=$mainType.GetMethod('ApplyDockConfigurationDraft',$private);$applied=[bool]$apply.Invoke($window,[object[]]@($true));Assert $applied ('Text Assembly dock Apply failed: '+[string](Field 'DockValidationText').Text);Assert($text.Parameters.TextAssemblyTemplate-eq'APPLIED-{Values}') 'Applied Text Assembly draft was not stored'
  $mainType.GetMethod('UndoFlowEditButton_Click',$private).Invoke($window,[object[]]@($null,$null))|Out-Null;$restored=$null;foreach($candidate in $flow){if($candidate.ToolId -eq $text.ToolId){$restored=$candidate;break}};Assert($restored.Parameters.TextAssemblyTemplate -eq $oldTemplate) 'Text Assembly undo failed'
  $mainType.GetMethod('RedoFlowEditButton_Click',$private).Invoke($window,[object[]]@($null,$null))|Out-Null;$restored=$null;foreach($candidate in $flow){if($candidate.ToolId -eq $text.ToolId){$restored=$candidate;break}};Assert($restored.Parameters.TextAssemblyTemplate -eq 'APPLIED-{Values}') 'Text Assembly redo failed'
  LoadDock $restored;$list=Field 'FlowToolList';$list.SelectedItems.Clear();$list.SelectedItem=$restored;$before=$flow.Count;$mainType.GetMethod('DuplicateFlowToolButton_Click',$private).Invoke($window,[object[]]@($null,$null))|Out-Null;$copy=$flow|Where-Object{$_.Kind.ToString()-eq'TextAssembly'-and$_.ToolId-ne$restored.ToolId}|Select-Object -Last 1;Assert($flow.Count-eq($before+1)-and$copy.Parameters.TextAssemblyTemplate-eq'APPLIED-{Values}') 'Text Assembly copy/parameter cloning failed'
  $recipe=$mainType.GetMethod('CaptureRecipe',$private).Invoke($window,@());$saved=$recipe.ToolFlow|Where-Object{$_.ToolId -eq $restored.ToolId}|Select-Object -First 1;Assert($saved.Parameters.TextAssemblyNumberFormat -eq 'F1' -and $saved.Parameters.TextAssemblyEmptyPolicy -eq 'Keep') 'Text Assembly recipe persistence failed'
  Write-Output ("S54 advanced-parameter hierarchy covered {0} dock tools; Text Assembly and real Text-to-TCP chain passed; BSB not implemented by user directive." -f $covered)
}finally{$mainType.GetField('recipeCloseConfirmed',$private).SetValue($window,$true);$window.Close();if(Test-Path $temp){Remove-Item -LiteralPath $temp -Recurse -Force}}
