param([string]$ExpectedSid='')
$ErrorActionPreference='Stop'
try {
 $identity=[Security.Principal.WindowsIdentity]::GetCurrent()
 $admin=(New-Object Security.Principal.WindowsPrincipal($identity)).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
 if(!$admin){
  $elevated=Start-Process powershell.exe -ArgumentList @('-NoProfile','-ExecutionPolicy','Bypass','-File',('"'+$PSCommandPath+'"'),'-ExpectedSid',$identity.User.Value) -Verb RunAs -Wait -PassThru
  if($elevated.ExitCode -ne 0){throw '安装未完成；请确认管理员授权，并使用当前 Windows 账户安装。'}
  exit 0
 }
 if($ExpectedSid -and $ExpectedSid -ne $identity.User.Value){throw '不支持使用另一个管理员账户代装。请使用具有管理员权限的本人 Windows 账户。'}
 $base=Join-Path $env:ProgramFiles 'JLUConnect'
 $exe=Join-Path $base 'JLUConnect.exe'
 if(!(Test-Path -LiteralPath (Join-Path $PSScriptRoot 'JLUConnect.exe'))){throw '请先解压完整 Windows 发布包，再运行 Install.cmd；源码目录不是安装包。'}
 $startupEnabled=$true
 $preference=Get-ItemProperty 'HKCU:\Software\JLUConnect' -Name Startup -ErrorAction SilentlyContinue
 if($preference){$startupEnabled=$preference.Startup -ne 0}
 foreach($name in @('JLUConnect','JLUConnect.Open')){
  $old=Get-ScheduledTask -TaskName $name -ErrorAction SilentlyContinue
  if($old){if(@($old.Actions | Where-Object {$_.Execute -ne $exe}).Count){throw '发现同名但不属于此安装位置的任务，请先人工检查。'};Stop-ScheduledTask -TaskName $name -ErrorAction SilentlyContinue}
 }
 Get-Process JLUConnect -ErrorAction SilentlyContinue | Where-Object {$_.Path -eq $exe} | Stop-Process -Force
 New-Item -ItemType Directory -Path $base -Force | Out-Null
 Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'JLUConnect.exe'),(Join-Path $PSScriptRoot 'JLUConnect.Launcher.exe') -Destination $base -Force
 Set-Content -LiteralPath (Join-Path $base 'installed.flag') -Value '0.1.0-preview.1'
 $principal=New-ScheduledTaskPrincipal -UserId $identity.Name -LogonType Interactive -RunLevel Highest
 $options=New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable -ExecutionTimeLimit ([TimeSpan]::Zero) -MultipleInstances IgnoreNew
 $trigger=New-ScheduledTaskTrigger -AtLogOn -User $identity.Name
 $trigger.Delay='PT10S'
 $background=New-ScheduledTaskAction -Execute $exe -Argument '--background' -WorkingDirectory $base
 $interactive=New-ScheduledTaskAction -Execute $exe -WorkingDirectory $base
 Register-ScheduledTask -TaskName 'JLUConnect' -Action $background -Trigger $trigger -Principal $principal -Settings $options -Description 'JLUConnect: campus connection recovery after network changes.' -Force | Out-Null
 Register-ScheduledTask -TaskName 'JLUConnect.Open' -Action $interactive -Principal $principal -Settings $options -Description 'Open JLUConnect.' -Force | Out-Null
 if(!$startupEnabled){Disable-ScheduledTask -TaskName 'JLUConnect' | Out-Null}
 New-Item 'HKCU:\Software\JLUConnect' -Force | Out-Null
 Set-ItemProperty 'HKCU:\Software\JLUConnect' -Name Startup -Value ([int]$startupEnabled)
 $shell=New-Object -ComObject WScript.Shell
 $link=$shell.CreateShortcut((Join-Path ([Environment]::GetFolderPath('Desktop')) 'JLUConnect.lnk'))
 $link.TargetPath=Join-Path $base 'JLUConnect.Launcher.exe'
 $link.WorkingDirectory=$base;$link.IconLocation=$exe+',0';$link.Description='JLUConnect 校园网助手';$link.Save()
 Start-ScheduledTask -TaskName 'JLUConnect.Open'
 Write-Host '安装完成。请在打开的设置页面识别并确认校园网络。'
} catch {Write-Error $_;exit 1}
