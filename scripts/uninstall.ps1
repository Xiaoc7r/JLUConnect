param([string]$ExpectedSid='')
$ErrorActionPreference='Stop'
try {
 $identity=[Security.Principal.WindowsIdentity]::GetCurrent()
 if(!(New-Object Security.Principal.WindowsPrincipal($identity)).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)){
  $p=Start-Process powershell.exe -ArgumentList @('-NoProfile','-ExecutionPolicy','Bypass','-File',('"'+$PSCommandPath+'"'),'-ExpectedSid',$identity.User.Value) -Verb RunAs -Wait -PassThru
  exit $p.ExitCode
 }
 if($ExpectedSid -and $ExpectedSid -ne $identity.User.Value){throw '请使用安装时的 Windows 账户卸载。'}
 $base=Join-Path $env:ProgramFiles 'JLUConnect';$exe=Join-Path $base 'JLUConnect.exe'
 foreach($name in @('JLUConnect','JLUConnect.Open')){
  $task=Get-ScheduledTask -TaskName $name -ErrorAction SilentlyContinue
  if($task){if(@($task.Actions | Where-Object {$_.Execute -ne $exe}).Count){throw '同名任务路径不匹配，已停止卸载。'};Stop-ScheduledTask -TaskName $name -ErrorAction SilentlyContinue;Unregister-ScheduledTask -TaskName $name -Confirm:$false}
 }
 Get-Process JLUConnect -ErrorAction SilentlyContinue | Where-Object {$_.Path -eq $exe} | Stop-Process -Force
 foreach($name in @('JLUConnect.exe','JLUConnect.Launcher.exe','installed.flag')){$path=Join-Path $base $name;if(Test-Path -LiteralPath $path){Remove-Item -LiteralPath $path}}
 if((Test-Path -LiteralPath $base) -and @(Get-ChildItem -LiteralPath $base -Force).Count -eq 0){Remove-Item -LiteralPath $base}
 $link=Join-Path ([Environment]::GetFolderPath('Desktop')) 'JLUConnect.lnk'
 if(Test-Path -LiteralPath $link){$shell=New-Object -ComObject WScript.Shell;if($shell.CreateShortcut($link).TargetPath -eq (Join-Path $base 'JLUConnect.Launcher.exe')){Remove-Item -LiteralPath $link}}
 Remove-ItemProperty 'HKCU:\Software\JLUConnect' -Name Startup -ErrorAction SilentlyContinue
 Remove-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name JLUConnect -ErrorAction SilentlyContinue
 Write-Host '已卸载助手。保留官方客户端、当前连接以及本机配置；不执行注销。'
}catch{Write-Error $_;exit 1}
