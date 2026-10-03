$ErrorActionPreference = 'Stop'
$Here = Split-Path -Parent $MyInvocation.MyCommand.Path
if ($env:PDM_BACKEND_DIR) {
    $Backend = $env:PDM_BACKEND_DIR
} elseif (Test-Path (Join-Path $Here 'backend\docker-compose.yml')) {
    $Backend = Join-Path $Here 'backend'
} elseif (Test-Path (Join-Path $Here '..\docker-compose.yml')) {
    $Backend = (Resolve-Path (Join-Path $Here '..')).Path
} else {
    throw 'Mini-PDM: Backend sources were not found. Set PDM_BACKEND_DIR to the backend directory.'
}
$Port = if ($env:PDM_API_PORT) { $env:PDM_API_PORT } else { '5000' }
$ApiUrl = "http://127.0.0.1:$Port"
$Project = if ($env:PDM_COMPOSE_PROJECT_NAME) { $env:PDM_COMPOSE_PROJECT_NAME } else { 'cad' }
$Timeout = if ($env:PDM_STARTUP_TIMEOUT_SECONDS) { [int]$env:PDM_STARTUP_TIMEOUT_SECONDS } else { 180 }
$env:PDM_API_PORT = $Port

function Fail([string]$Message) { throw "Mini-PDM: $Message" }
try {
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) { Fail 'Docker is required. Install Docker Desktop, then retry.' }
    docker info *> $null
    if ($LASTEXITCODE -ne 0) {
        $DockerDesktop = Join-Path $env:ProgramFiles 'Docker\Docker\Docker Desktop.exe'
        if (Test-Path $DockerDesktop) { Start-Process $DockerDesktop }
        $Started = $false
        for ($i = 0; $i -lt 45; $i++) { Start-Sleep 2; docker info *> $null; if ($LASTEXITCODE -eq 0) { $Started = $true; break } }
        if (-not $Started) { Fail 'Docker daemon did not start. Open Docker Desktop and retry.' }
    }
    $Compose = @('--project-directory', $Backend, '-p', $Project, '-f', (Join-Path $Backend 'docker-compose.yml'), '-f', (Join-Path $Backend 'compose.native.yml'))
    if (Test-Path (Join-Path $Backend '.env')) { $Compose += @('--env-file', (Join-Path $Backend '.env')) }
    docker compose @Compose up -d --build api
    if ($LASTEXITCODE -ne 0) { Fail 'Could not start the API and its database/migration dependencies.' }
    $Ready = $false
    $Clock = [Diagnostics.Stopwatch]::StartNew()
    for ($i = 0; $i -lt [math]::Ceiling($Timeout / 2); $i++) {
        if ($Clock.Elapsed.TotalSeconds -ge $Timeout) { break }
        try { Invoke-WebRequest -UseBasicParsing -Uri "$ApiUrl/health" -TimeoutSec 2 | Out-Null; $Ready = $true; break } catch { Start-Sleep 2 }
    }
    if (-not $Ready) { Fail "API did not become healthy within $Timeout seconds at $ApiUrl/health." }
    Remove-Item Env:PDM_BROWSER_PICKER -ErrorAction SilentlyContinue
    $env:PDM_API_BASE_URL = $ApiUrl
    $App = Join-Path $Here 'desktop\MiniPdm.Desktop.exe'
    if (Test-Path $App) { & $App; exit $LASTEXITCODE }
    $ProjectFile = if ($env:PDM_DESKTOP_PROJECT) { $env:PDM_DESKTOP_PROJECT } else { Join-Path $Backend 'src\MiniPdm.Desktop\MiniPdm.Desktop.csproj' }
    if (Test-Path $ProjectFile) { dotnet run --project $ProjectFile -c Release; exit $LASTEXITCODE }
    Fail 'Desktop application executable was not found in this distribution.'
} catch {
    [Console]::Error.WriteLine("Mini-PDM: $_")
    exit 1
}
