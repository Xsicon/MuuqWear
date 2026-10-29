$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$webOut = Join-Path $root "artifacts\web"
$hosting = Join-Path $root "hosting-wwwroot"

dotnet publish (Join-Path $root "MuuqWear.Web\MuuqWear.Web.csproj") -c Release -o $webOut --nologo
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if (Test-Path $hosting) { Remove-Item $hosting -Recurse -Force }
New-Item -ItemType Directory -Path $hosting | Out-Null
Copy-Item -Path (Join-Path $webOut "wwwroot\*") -Destination $hosting -Recurse -Force
Copy-Item -Path (Join-Path $root "MuuqWear.Client\wwwroot\index.html") -Destination (Join-Path $hosting "index.html") -Force
Copy-Item -Path (Join-Path $root "MuuqWear.Client\wwwroot\appsettings.json") -Destination (Join-Path $hosting "appsettings.json") -Force
Copy-Item -Path (Join-Path $root "MuuqWear.Client\wwwroot\appsettings.Production.json") -Destination (Join-Path $hosting "appsettings.Production.json") -Force

$wasm = Join-Path $hosting "_framework\blazor.webassembly.js"
if (-not (Test-Path $wasm)) {
    Write-Error "Published output is missing _framework/blazor.webassembly.js"
    exit 1
}

Write-Host "Hosting files are in $hosting"
