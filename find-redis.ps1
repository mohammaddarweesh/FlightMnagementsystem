# Redis Finder and Starter Script
Write-Host "🔍 Finding Redis Installation..." -ForegroundColor Yellow

# Common Redis installation locations
$redisLocations = @(
    "C:\Program Files\Redis\redis-server.exe",
    "C:\Program Files (x86)\Redis\redis-server.exe",
    "C:\Redis\redis-server.exe",
    "C:\tools\redis\redis-server.exe",
    "C:\ProgramData\chocolatey\lib\redis-64\tools\redis-server.exe",
    "C:\ProgramData\chocolatey\bin\redis-server.exe"
)

$redisPath = $null
foreach ($location in $redisLocations) {
    if (Test-Path $location) {
        Write-Host "✅ Found Redis at: $location" -ForegroundColor Green
        $redisPath = $location
        break
    }
}

if (-not $redisPath) {
    Write-Host "❌ Redis not found in common locations" -ForegroundColor Red
    Write-Host "🔍 Searching entire system..." -ForegroundColor Yellow
    
    # Search for redis-server.exe in common drives
    $drives = Get-WmiObject -Class Win32_LogicalDisk | Where-Object {$_.DriveType -eq 3} | Select-Object -ExpandProperty DeviceID
    
    foreach ($drive in $drives) {
        Write-Host "Searching $drive..." -ForegroundColor Gray
        $found = Get-ChildItem -Path "$drive\" -Name "redis-server.exe" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($found) {
            $redisPath = Join-Path $drive $found
            Write-Host "✅ Found Redis at: $redisPath" -ForegroundColor Green
            break
        }
    }
}

if ($redisPath) {
    Write-Host "`n🚀 Starting Redis..." -ForegroundColor Yellow
    $redisDir = Split-Path $redisPath -Parent
    
    # Check for redis.windows.conf
    $configFile = Join-Path $redisDir "redis.windows.conf"
    if (-not (Test-Path $configFile)) {
        $configFile = Join-Path $redisDir "redis.conf"
    }
    
    try {
        if (Test-Path $configFile) {
            Write-Host "Using config file: $configFile" -ForegroundColor Cyan
            Start-Process -FilePath $redisPath -ArgumentList $configFile -WindowStyle Normal
        } else {
            Write-Host "No config file found, starting with defaults" -ForegroundColor Cyan
            Start-Process -FilePath $redisPath -WindowStyle Normal
        }
        
        Write-Host "✅ Redis started!" -ForegroundColor Green
        Write-Host "Waiting 3 seconds for Redis to initialize..." -ForegroundColor Yellow
        Start-Sleep -Seconds 3
        
        # Test connection using redis-cli
        $redisCliPath = Join-Path $redisDir "redis-cli.exe"
        if (Test-Path $redisCliPath) {
            Write-Host "`n🧪 Testing Redis connection..." -ForegroundColor Cyan
            $testResult = & $redisCliPath ping 2>$null
            if ($testResult -eq "PONG") {
                Write-Host "✅ Redis is responding! Connection successful!" -ForegroundColor Green
            } else {
                Write-Host "⚠️ Redis started but not responding to ping" -ForegroundColor Yellow
            }
        } else {
            Write-Host "⚠️ redis-cli.exe not found, cannot test connection" -ForegroundColor Yellow
        }
        
    } catch {
        Write-Host "❌ Failed to start Redis: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    Write-Host "`n📋 Redis Information:" -ForegroundColor Magenta
    Write-Host "Redis Path: $redisPath" -ForegroundColor White
    Write-Host "Redis Directory: $redisDir" -ForegroundColor White
    Write-Host "Config File: $configFile" -ForegroundColor White
    
    Write-Host "`n🔧 To start Redis manually in the future:" -ForegroundColor Cyan
    Write-Host "cd `"$redisDir`"" -ForegroundColor Gray
    Write-Host ".\redis-server.exe" -ForegroundColor Gray
    
    Write-Host "`n🧪 To test Redis manually:" -ForegroundColor Cyan
    Write-Host "cd `"$redisDir`"" -ForegroundColor Gray
    Write-Host ".\redis-cli.exe ping" -ForegroundColor Gray
    
} else {
    Write-Host "`n❌ Redis installation not found!" -ForegroundColor Red
    Write-Host "Please verify Redis was installed correctly." -ForegroundColor Yellow
    Write-Host "`nTry these installation methods:" -ForegroundColor Cyan
    Write-Host "1. choco install redis-64" -ForegroundColor White
    Write-Host "2. Download from: https://github.com/microsoftarchive/redis/releases" -ForegroundColor White
    Write-Host "3. Use WSL2: sudo apt install redis-server" -ForegroundColor White
}

Write-Host "`nPress any key to continue..." -ForegroundColor Yellow
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
