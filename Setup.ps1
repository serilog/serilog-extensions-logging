$ErrorActionPreference = "Stop"

$RequiredDotnetVersion =  $(cat ./global.json | convertfrom-json).sdk.version

New-Item -ItemType Directory -Force "./build/" | Out-Null

Invoke-WebRequest "https://dot.net/v1/dotnet-install.ps1" -OutFile "./build/installcli.ps1"
& ./build/installcli.ps1 -InstallDir "$pwd/.dotnetcli" -NoPath -Version $RequiredDotnetVersion
if ($LASTEXITCODE) { throw ".NET install failed" }

$env:Path = "$pwd/.dotnetcli;$env:Path"
