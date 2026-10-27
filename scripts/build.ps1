param([string]$Version='0.1.0-preview.1')
$ErrorActionPreference='Stop'
if($Version -notmatch '^\d+\.\d+\.\d+(-[A-Za-z0-9.]+)?$'){throw 'Invalid version'}
$root=Split-Path $PSScriptRoot -Parent
$csc=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if(!(Test-Path -LiteralPath $csc)){throw '.NET Framework C# compiler not found. Use Windows x64 with .NET Framework 4.8.'}
$package=Join-Path $root ('dist\JLUConnect-'+$Version+'-windows-x64')
New-Item -ItemType Directory -Path $package -Force | Out-Null
$sources=@('CampusAssistant.cs','CampusUI.cs','AssemblyInfo.cs') | ForEach-Object {Join-Path $root ('src\'+$_)}
& $csc /nologo /target:winexe /optimize+ /platform:x64 ('/win32icon:'+(Join-Path $root 'assets\app.ico')) ('/out:'+(Join-Path $package 'JLUConnect.exe')) /r:System.Windows.Forms.dll /r:System.Drawing.dll @sources
if($LASTEXITCODE -ne 0){throw 'Application build failed'}
& $csc /nologo /target:winexe /optimize+ /platform:x64 ('/win32icon:'+(Join-Path $root 'assets\app.ico')) ('/out:'+(Join-Path $package 'JLUConnect.Launcher.exe')) /r:System.Windows.Forms.dll (Join-Path $root 'src\CampusLauncher.cs') (Join-Path $root 'src\AssemblyInfo.cs')
if($LASTEXITCODE -ne 0){throw 'Launcher build failed'}
$test=Join-Path $package 'PolicyTests.exe'
& $csc /nologo /target:exe /platform:x64 ('/out:'+$test) ('/r:'+(Join-Path $package 'JLUConnect.exe')) (Join-Path $root 'tests\PolicyTests.cs')
if($LASTEXITCODE -ne 0){throw 'Tests build failed'}
& $test
if($LASTEXITCODE -ne 0){throw 'Offline tests failed'}
Remove-Item -LiteralPath $test
Copy-Item -LiteralPath (Join-Path $root 'scripts\install.ps1'),(Join-Path $root 'scripts\uninstall.ps1'),(Join-Path $root 'Install.cmd'),(Join-Path $root 'Uninstall.cmd'),(Join-Path $root 'README.md'),(Join-Path $root 'LICENSE') -Destination $package -Force
New-Item -ItemType Directory -Path (Join-Path $package 'docs'),(Join-Path $package 'assets') -Force | Out-Null
Copy-Item -Path (Join-Path $root 'docs\*.md') -Destination (Join-Path $package 'docs') -Force
Copy-Item -LiteralPath (Join-Path $root 'assets\screenshot.png') -Destination (Join-Path $package 'assets') -Force
$archive=Join-Path $root ('dist\JLUConnect-'+$Version+'-windows-x64.zip')
Compress-Archive -Path $package -DestinationPath $archive -Force
$hash=(Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText((Join-Path $root 'dist\SHA256SUMS.txt'),$hash+'  '+[IO.Path]::GetFileName($archive)+[Environment]::NewLine,(New-Object Text.UTF8Encoding($false)))
Write-Output ('Built '+$archive)
