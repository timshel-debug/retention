# Release Retention - API + UI Launcher
# Starts the .NET API (ports 5219/7080) and React UI (port 5173)
# Usage: .\start-dev.ps1 [--https]

param(
    [switch]$https = $false,
    [int]$ApiStartupTimeoutSeconds = 30
)

$ErrorActionPreference = "Stop"

# Color output helpers
function Write-Header {
    param([string]$Message)
    Write-Host "`n" -NoNewline
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host $Message -ForegroundColor Cyan
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
}

function Write-Status {
    param([string]$Message)
    Write-Host "→ $Message" -ForegroundColor Blue
}

function Write-Success {
    param([string]$Message)
    Write-Host "✓ $Message" -ForegroundColor Green
}

function Write-Error2 {
    param([string]$Message)
    Write-Host "✗ $Message" -ForegroundColor Red
}

# Get script root directory (repository root)
$repoRoot = if ($PSCommandPath) {
    Split-Path -Parent $PSCommandPath
} else {
    Get-Location
}

Write-Header "Release Retention Console - Development Server"

# Determine port configuration
if ($https) {
    $apiUrl = "https://localhost:7080"
    $apiPort = 7080
    $protocol = "HTTPS"
    Write-Status "Starting in HTTPS mode"
} else {
    $apiUrl = "http://localhost:5219"
    $apiPort = 5219
    $protocol = "HTTP"
    Write-Status "Starting in HTTP mode"
}

$uiUrl = "http://localhost:5173"

# Check if ports are already in use
Write-Status "Checking port availability..."

try {
    $portCheck = netstat -ano | Select-String ":$apiPort\s" -ErrorAction SilentlyContinue
    if ($portCheck) {
        Write-Error2 "API port $apiPort is already in use!"
        Write-Host "Running processes:" -ForegroundColor Yellow
        Write-Host $portCheck
        Write-Host "`nPlease stop the existing process or use a different port." -ForegroundColor Yellow
        exit 1
    }
} catch {
    Write-Host "Note: Could not verify port availability (netstat check skipped)" -ForegroundColor Yellow
}

# Start API in background
Write-Header "Starting .NET API ($protocol)"
Write-Status "Building solution..."

Push-Location $repoRoot
try {
    dotnet build --configuration Debug | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Error2 "Build failed!"
        exit 1
    }
    Write-Success "Build completed"

    Write-Status "Starting API (port $apiPort)..."
    
    # Start API in background job
    $apiJob = Start-Job -ScriptBlock {
        param($repoRoot, $protocol)
        Push-Location $repoRoot
        
        if ($protocol -eq "HTTPS") {
            dotnet run --project "src/Retention.Api" --launch-profile "https"
        } else {
            dotnet run --project "src/Retention.Api" --launch-profile "http"
        }
    } -ArgumentList $repoRoot, $protocol

    # Wait for API to be ready
    Write-Status "Waiting for API to be ready (timeout: ${ApiStartupTimeoutSeconds}s)..."
    $startTime = Get-Date
    $ready = $false

    while ((New-TimeSpan -Start $startTime -End (Get-Date)).TotalSeconds -lt $ApiStartupTimeoutSeconds) {
        try {
            # Try to connect to API Swagger endpoint
            $response = Invoke-WebRequest -Uri "$apiUrl/swagger/index.html" -TimeoutSec 2 -ErrorAction SilentlyContinue
            if ($response.StatusCode -eq 200) {
                $ready = $true
                break
            }
        } catch {
            Start-Sleep -Milliseconds 500
        }
    }

    if ($ready) {
        Write-Success "API is running at $apiUrl"
    } else {
        Write-Error2 "API failed to start within timeout"
        Write-Host "`nAPI Job output:" -ForegroundColor Yellow
        Receive-Job $apiJob
        Stop-Job $apiJob
        Remove-Job $apiJob
        exit 1
    }

} finally {
    Pop-Location
}

# Start UI in background
Write-Header "Starting React UI (Vite)"
Write-Status "Starting UI development server (port 5173)..."

Push-Location "$repoRoot/src/retention-ui"
try {
    $uiJob = Start-Job -ScriptBlock {
        param($uiPath)
        Push-Location $uiPath
        npm run dev
    } -ArgumentList "$repoRoot/src/retention-ui"

    # Wait a moment for UI to start
    Start-Sleep -Seconds 3
    
    if ($uiJob.State -eq "Running") {
        Write-Success "UI is running at $uiUrl"
    } else {
        Write-Error2 "UI failed to start"
        Receive-Job $uiJob
        exit 1
    }

} finally {
    Pop-Location
}

# Display summary
Write-Header "Both Services Running"

Write-Host @"

✓ API ($protocol):        $apiUrl
✓ UI (Vite):              $uiUrl

Open your browser to:  $uiUrl

Press Ctrl+C to stop both services.

"@ -ForegroundColor Green

# Keep running until user interrupts
try {
    while ($true) {
        Start-Sleep -Seconds 1
        
        # Check if jobs are still running
        if ($apiJob.State -ne "Running") {
            Write-Error2 "API process crashed!"
            break
        }
        if ($uiJob.State -ne "Running") {
            Write-Error2 "UI process crashed!"
            break
        }
    }
} finally {
    Write-Host "`n`nShutting down services..." -ForegroundColor Yellow
    
    Stop-Job $apiJob -ErrorAction SilentlyContinue
    Stop-Job $uiJob -ErrorAction SilentlyContinue
    
    Remove-Job $apiJob -ErrorAction SilentlyContinue
    Remove-Job $uiJob -ErrorAction SilentlyContinue
    
    Write-Success "Services stopped"
}
