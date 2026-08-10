[CmdletBinding()]
param(
    [int]$ClientPort = 5510,
    [int]$ApiPort = 5269,
    [switch]$NoBrowser
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$clientRoot = Join-Path $projectRoot "Frontend"
$apiUrl = "http://127.0.0.1:$ApiPort"
$clientUrl = "http://127.0.0.1:$ClientPort"

function Test-Endpoint([string]$Url) {
    try {
        $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 2
        return $response.StatusCode -ge 200 -and $response.StatusCode -lt 500
    } catch {
        return $false
    }
}

function Wait-Endpoint([string]$Url, [string]$Name) {
    for ($attempt = 0; $attempt -lt 45; $attempt++) {
        if (Test-Endpoint $Url) { return }
        Start-Sleep -Seconds 1
    }
    throw "$Name n'a pas démarré à l'adresse $Url."
}

if (-not (Test-Endpoint "$apiUrl/health")) {
    $previousEnvironment = $env:ASPNETCORE_ENVIRONMENT
    $previousUrls = $env:ASPNETCORE_URLS
    try {
        $env:ASPNETCORE_ENVIRONMENT = "Development"
        $env:ASPNETCORE_URLS = $apiUrl
        Start-Process -FilePath "dotnet" -ArgumentList @("run", "--project", "src/SopmineWorkshop.API/SopmineWorkshop.API.csproj", "--no-launch-profile") -WorkingDirectory $projectRoot | Out-Null
    } finally {
        $env:ASPNETCORE_ENVIRONMENT = $previousEnvironment
        $env:ASPNETCORE_URLS = $previousUrls
    }
}

if (-not (Test-Endpoint "$clientUrl/")) {
    $previousPort = $env:PORT
    $previousApiPort = $env:API_PORT
    $previousApiOrigin = $env:API_ORIGIN
    try {
        $env:PORT = "$ClientPort"
        $env:API_PORT = "$ApiPort"
        $env:API_ORIGIN = $apiUrl
        Start-Process -FilePath "node" -ArgumentList @("server.mjs") -WorkingDirectory $clientRoot | Out-Null
    } finally {
        $env:PORT = $previousPort
        $env:API_PORT = $previousApiPort
        $env:API_ORIGIN = $previousApiOrigin
    }
}

Wait-Endpoint "$apiUrl/health" "L'API"
Wait-Endpoint "$clientUrl/" "L'interface"

$appUrl = "$clientUrl/"
Write-Host "Sopmine est prêt : $appUrl" -ForegroundColor Green
Write-Host "API : $apiUrl" -ForegroundColor DarkGray
if (-not $NoBrowser) { Start-Process $appUrl }
