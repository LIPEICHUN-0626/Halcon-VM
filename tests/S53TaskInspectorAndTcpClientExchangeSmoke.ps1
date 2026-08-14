param([string]$ProjectRoot=(Split-Path -Parent $PSScriptRoot))
$ErrorActionPreference='Stop'
if([Threading.Thread]::CurrentThread.ApartmentState-ne[Threading.ApartmentState]::STA){throw 'Run with powershell.exe -STA.'}
$halcon='C:\Program Files\MVTec\HALCON-20.11-Progress';$env:PATH=(Join-Path $halcon 'bin\x64-win64')+';'+$env:PATH
Add-Type -AssemblyName PresentationFramework
if(-not('S53LoopbackServer' -as [type])){Add-Type -TypeDefinition @'
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
public sealed class S53LoopbackServer : IDisposable {
  private readonly string mode; private TcpListener listener; private Thread thread;
  public int Port { get; private set; } public string ReceivedText { get; private set; } public bool ClientClosed { get; private set; }
  public S53LoopbackServer(string mode) { this.mode=mode; ReceivedText=""; listener=new TcpListener(IPAddress.Loopback,0); listener.Start(); Port=((IPEndPoint)listener.LocalEndpoint).Port; thread=new Thread(Run); thread.IsBackground=true; thread.Start(); }
  private void Run(){try{using(TcpClient client=listener.AcceptTcpClient()){client.ReceiveTimeout=2000;using(NetworkStream stream=client.GetStream()){byte[] buffer=new byte[8192];int count=stream.Read(buffer,0,buffer.Length);ReceivedText=Encoding.UTF8.GetString(buffer,0,Math.Max(0,count));if(mode=="Echo"){byte[] reply=Encoding.UTF8.GetBytes("ACK:"+ReceivedText);stream.Write(reply,0,reply.Length);stream.Flush();}else if(mode=="Silent"){Thread.Sleep(350);}}}}catch{}finally{ClientClosed=true;try{listener.Stop();}catch{}}}
  public void Dispose(){try{listener.Stop();}catch{}if(thread!=null&&thread.IsAlive)thread.Join(1000);}
}
'@}
function Assert([bool]$condition,[string]$message){if(-not$condition){throw $message}}
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $ProjectRoot 'bin\x64\Debug\HalconWinFormsDemo.exe'))
$mainType=$assembly.GetType('HalconWinFormsDemo.MainWindow',$true);$serviceType=$assembly.GetType('HalconWinFormsDemo.Services.RecipeService',$true);$loggerType=$assembly.GetType('HalconWinFormsDemo.Services.AppLogger',$true);$kindType=$assembly.GetType('HalconWinFormsDemo.Models.VmToolKind',$true);$metadataType=$assembly.GetType('HalconWinFormsDemo.Models.ToolMetadata',$true)
$private=[Reflection.BindingFlags]'Instance,NonPublic';$all=[Reflection.BindingFlags]'Instance,NonPublic,Public';$static=[Reflection.BindingFlags]'Static,Public';$temp=Join-Path([IO.Path]::GetTempPath())('halcon-vm-s53-'+[Guid]::NewGuid().ToString('N'));[IO.Directory]::CreateDirectory($temp)|Out-Null
$service=$serviceType.GetConstructor([Type[]]@([string])).Invoke([object[]]@($temp.ToString()));$logger=$loggerType.GetConstructor([Type[]]@([string])).Invoke([object[]]@($temp.ToString()));$window=$mainType.GetConstructor($private,$null,[Type[]]@($serviceType,$loggerType),$null).Invoke([object[]]@($service,$logger))
function Field([string]$name){$value=$mainType.GetField($name,$all).GetValue($window);Write-Output -NoEnumerate $value}
function Tool([string]$kind,[string]$name){$mainType.GetMethod('CreateFlowTool',$private).Invoke($window,[object[]]@([Enum]::Parse($kindType,$kind),$name,$true,$null))}
function Out($tool,[string]$name){$args=[object[]]@($name,$null);Assert($tool.GetType().GetMethod('TryGetOutputValue').Invoke($tool,$args))("Missing output "+$name);$args[1]}
function Run($tool){$mainType.GetMethod('ExecuteFlowTool',$private).Invoke($window,[object[]]@($tool,'S53 smoke'))}
try{
  $xaml=[IO.File]::ReadAllText((Join-Path $ProjectRoot 'MainWindow.xaml'))
  Assert(([regex]::Matches($xaml,'x:Name="Right(?:Parameters|Io|Result)TaskButton"').Count-eq3)) 'Right task surface does not expose exactly three primary entries'
  Assert($xaml.Contains('ContentPresenter ContentSource="SelectedContent"')) 'Right tab headers are not visually suppressed'
  Assert($xaml.Contains('RightRunSettingsMenuItem_Click')-and$xaml.Contains('RightRoiSettingsMenuItem_Click')-and$xaml.Contains('RightHelpMenuItem_Click')) 'Low-frequency module settings are not reachable from More'

  $flow=Field 'flowTools';$source=Tool ImageSource 'Upstream Values';$tcp=Tool TcpClientExchange 'TCP Exchange';$flow.Add($source);$flow.Add($tcp)
  $source.SetOutputValue('SN','批次-A7');$source.SetOutputValue('Width',[double]720);$source.SetOutputValue('ResultCode','OK');$source.ResultCode='OK'
  $tcp.SetInputBinding('String',$source.ToolId,'SN');$tcp.SetInputBinding('Number',$source.ToolId,'Width');$tcp.SetInputBinding('ResultCode',$source.ToolId,'ResultCode')
  $tcp.Parameters.TcpClientHost='127.0.0.1';$tcp.Parameters.TcpClientConnectTimeoutMilliseconds=500;$tcp.Parameters.TcpClientSendTimeoutMilliseconds=500;$tcp.Parameters.TcpClientReceiveTimeoutMilliseconds=500;$tcp.Parameters.TcpClientEncoding='UTF-8';$tcp.Parameters.TcpClientLineEnding='CRLF';$tcp.Parameters.TcpClientSendTemplate='SN={String};W={Number};R={ResultCode}';$tcp.Parameters.TcpClientWaitForResponse=$true
  $inputs=$metadataType.GetMethod('GetInputPorts',$static).Invoke($null,[object[]]@([Enum]::Parse($kindType,'TcpClientExchange')));$outputs=$metadataType.GetMethod('GetOutputPorts',$static).Invoke($null,[object[]]@([Enum]::Parse($kindType,'TcpClientExchange')))
  Assert($inputs.Count-eq3-and($inputs|Where-Object PortName -eq 'Number').DataType-eq'Number') 'TCP typed inputs are incomplete'
  Assert(($outputs|Where-Object PortName -eq 'ReceivedText').DataType-eq'String'-and($outputs|Where-Object PortName -eq 'RoundTripMs').DataType-eq'Number') 'TCP outputs are incomplete'

  $server=[S53LoopbackServer]::new('Echo');try{$tcp.Parameters.TcpClientPort=$server.Port;$record=Run $tcp;Assert($record.ResultCode-eq'OK') 'UTF-8 echo exchange failed';Assert((Out $tcp Connected)-eq$true) 'Connected output failed';Assert((Out $tcp SentText).EndsWith("`r`n")) 'CRLF was not appended';Assert((Out $tcp ReceivedText).Contains('ACK:SN=批次-A7;W=720;R=OK')) 'Template variables or UTF-8 response failed';Assert([int](Out $tcp BytesSent)-gt0-and[int](Out $tcp BytesReceived)-gt0) 'Byte counters failed'}finally{$server.Dispose()}
  Assert($server.ClientClosed) 'TCP connection was not released after success'

  $server=[S53LoopbackServer]::new('Echo');try{$tcp.Parameters.TcpClientPort=$server.Port;$tcp.Parameters.TcpClientEncoding='ASCII';$tcp.Parameters.TcpClientLineEnding='LF';$tcp.Parameters.TcpClientSendTemplate='ASCII-{Number}';$record=Run $tcp;Assert($record.ResultCode-eq'OK'-and(Out $tcp SentText).EndsWith("`n")) 'ASCII/LF exchange failed'}finally{$server.Dispose()}
  $server=[S53LoopbackServer]::new('Empty');try{$tcp.Parameters.TcpClientPort=$server.Port;$tcp.Parameters.TcpClientEncoding='UTF-8';$tcp.Parameters.TcpClientLineEnding='None';$tcp.Parameters.TcpClientWaitForResponse=$true;$record=Run $tcp;Assert($record.ResultCode-eq'NG'-and(Out $tcp Connected)-eq$true-and[int](Out $tcp BytesReceived)-eq0) 'Empty-response semantics failed'}finally{$server.Dispose()}
  $server=[S53LoopbackServer]::new('Silent');try{$tcp.Parameters.TcpClientPort=$server.Port;$tcp.Parameters.TcpClientReceiveTimeoutMilliseconds=80;$record=Run $tcp;$timeoutOk=(($record.ResultCode -eq 'NG') -and ([string]$tcp.ErrorMessage).Length -gt 0 -and (Out $tcp Connected)-eq$false);Assert $timeoutOk ("Receive timeout failed: Result="+$record.ResultCode+" Error="+$tcp.ErrorMessage+" Received="+(Out $tcp BytesReceived))}finally{$server.Dispose()}
  $probe=[Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback,0);$probe.Start();$unused=([Net.IPEndPoint]$probe.LocalEndpoint).Port;$probe.Stop();$tcp.Parameters.TcpClientPort=$unused;$tcp.Parameters.TcpClientReceiveTimeoutMilliseconds=200;$record=Run $tcp;Assert($record.ResultCode-eq'NG'-and(Out $tcp Connected)-eq$false) 'Connection-refused path failed'
  $mainType.GetField('isStopRequested',$private).SetValue($window,$true);$record=Run $tcp;Assert(($record.ResultCode-eq'NG'-and([string]$tcp.ErrorMessage).Length-gt0)) 'Stop/cancel path failed';$mainType.GetField('isStopRequested',$private).SetValue($window,$false)
  $tcp.Parameters.TcpClientPort=0;$thrown=$false;try{Run $tcp|Out-Null}catch{$thrown=$true};Assert($thrown-and([string]$tcp.ErrorMessage).Length-gt0) 'Invalid configuration was not rejected'

  $server=[S53LoopbackServer]::new('Echo');try{$tcp.Parameters.TcpClientPort=$server.Port;$tcp.Parameters.TcpClientWaitForResponse=$true;$tcp.Parameters.TcpClientReceiveTimeoutMilliseconds=500;$tcp.Parameters.TcpClientSendTemplate='DOWNSTREAM-{Number}';$record=Run $tcp;Assert($record.ResultCode-eq'OK') 'Final TCP run failed'}finally{$server.Dispose()}
  $judge=Tool NumericJudge 'Round Trip Judge';$judge.SetInputBinding('Value',$tcp.ToolId,'RoundTripMs');$judge.InputToolId=$tcp.ToolId;$judge.InputPortName='RoundTripMs';$judge.NumericLowerLimit=0;$judge.NumericUpperLimit=5000;$flow.Add($judge);$judged=Run $judge;Assert($judged.ResultCode-eq'OK') 'Real Number downstream failed'

  (Field 'FlowToolList').SelectedItem=$tcp;$mainType.GetMethod('LoadDockConfigurationDraft',$private).Invoke($window,[object[]]@($tcp))|Out-Null
  Assert((Field 'DockTcpClientPanel').Visibility.ToString()-eq'Visible'-and(Field 'DockTcpClientTimeoutPanel').Visibility.ToString()-eq'Visible') 'TCP dock entry is missing'
  Assert((Field 'DockApplyButton').Visibility.ToString()-eq'Collapsed') 'Draft actions should be quiet when no draft exists'
  (Field 'DockTcpClientHostTextBox').Text='localhost';Assert((Field 'DockApplyButton').Visibility.ToString()-eq'Visible'-and(Field 'DockRevertButton').Visibility.ToString()-eq'Visible') 'Draft actions did not appear contextually'
  $mainType.GetMethod('RightIoTaskButton_Click',$private).Invoke($window,[object[]]@($null,$null))|Out-Null;Assert((Field 'DockConfigurationTabs').SelectedItem-eq(Field 'DockIoTaskTab')) 'Primary I/O task navigation failed'
  $mainType.GetMethod('RightRunSettingsMenuItem_Click',$private).Invoke($window,[object[]]@($null,$null))|Out-Null;Assert((Field 'DockConfigurationTabs').SelectedItem-eq(Field 'DockRunSettingsTaskTab')) 'More/run settings navigation failed'

  $tcp.Parameters.TcpClientHost='recipe-host';$tcp.Parameters.TcpClientPort=12345;$tcp.Parameters.TcpClientEncoding='ASCII';$tcp.Parameters.TcpClientLineEnding='CR';$tcp.Parameters.TcpClientSendTemplate='R={ResultCode}';$tcp.Parameters.TcpClientWaitForResponse=$false
  $recipe=$mainType.GetMethod('CaptureRecipe',$private).Invoke($window,@());$saved=$recipe.ToolFlow|Where-Object{$_.ToolId-eq$tcp.ToolId}|Select-Object -First 1
  Assert($saved.Parameters.TcpClientHost-eq'recipe-host'-and$saved.Parameters.TcpClientPort-eq12345-and$saved.Parameters.TcpClientLineEnding-eq'CR'-and-not$saved.Parameters.TcpClientWaitForResponse) 'TCP recipe persistence failed'
  $mainType.GetMethod('LoadDockConfigurationDraft',$private).Invoke($window,[object[]]@($tcp))|Out-Null;$list=Field 'FlowToolList';$list.SelectedItems.Clear();$list.SelectedItem=$tcp;$before=$flow.Count;$mainType.GetMethod('DuplicateFlowToolButton_Click',$private).Invoke($window,[object[]]@($null,$null))|Out-Null;$copy=$flow|Where-Object{$_.Kind.ToString()-eq'TcpClientExchange'-and$_.ToolId-ne$tcp.ToolId}|Select-Object -Last 1;Assert($flow.Count-eq($before+1)-and$null-ne$copy-and$copy.Parameters.TcpClientHost-eq'recipe-host') 'TCP copy/parameter cloning failed'
  Write-Output 'S53 task inspector and generic TCP client exchange smoke tests passed; BSB not implemented by user directive.'
}finally{
  $mainType.GetField('recipeCloseConfirmed',$private).SetValue($window,$true);$window.Close();if(Test-Path $temp){Remove-Item -LiteralPath $temp -Recurse -Force}
}
