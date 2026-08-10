param(
    [int]$Port = 5510,
    [int]$ApiPort = 5269
)

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

$env:PORT = "$Port"
$env:API_PORT = "$ApiPort"

Set-Location $scriptRoot
node .\server.mjs
