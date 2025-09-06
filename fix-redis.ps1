# Redis Windows Fix Script
Write-Host "🔧 Fixing Redis Windows Binding Issue..." -ForegroundColor Yellow

$redisPath = "C:\Program Files\Redis"
$redisExe = Join-Path $redisPath "redis-server.exe"
$redisConf = Join-Path $redisPath "redis.windows.conf"

if (-not (Test-Path $redisExe)) {
    Write-Host "❌ Redis not found at $redisPath" -ForegroundColor Red
    exit
}

Write-Host "✅ Found Redis at: $redisPath" -ForegroundColor Green

# Kill any existing Redis processes
Write-Host "`n🛑 Stopping any existing Redis processes..." -ForegroundColor Yellow
Get-Process -Name "redis-server" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2

# Check if config file exists
if (Test-Path $redisConf) {
    Write-Host "✅ Found Windows config file: $redisConf" -ForegroundColor Green
    $useConfig = $redisConf
} else {
    Write-Host "⚠️ Windows config not found, creating one..." -ForegroundColor Yellow
    
    # Create a basic Windows-compatible Redis config
    $configContent = @"
# Redis Windows Configuration
bind 127.0.0.1
port 6379
timeout 0
save 900 1
save 300 10
save 60 10000
rdbcompression yes
dbfilename dump.rdb
dir ./
maxmemory-policy allkeys-lru
"@
    
    $configContent | Out-File -FilePath $redisConf -Encoding UTF8
    Write-Host "✅ Created Windows config file" -ForegroundColor Green
    $useConfig = $redisConf
}

# Method 1: Try with Windows config
Write-Host "`n🚀 Method 1: Starting Redis with Windows config..." -ForegroundColor Cyan
try {
    $process1 = Start-Process -FilePath $redisExe -ArgumentList $useConfig -PassThru -WindowStyle Normal
    Start-Sleep -Seconds 3
    
    if (-not $process1.HasExited) {
        Write-Host "✅ Redis started successfully with config!" -ForegroundColor Green
        $success = $true
    } else {
        Write-Host "❌ Redis exited immediately" -ForegroundColor Red
        $success = $false
    }
} catch {
    Write-Host "❌ Failed: $($_.Exception.Message)" -ForegroundColor Red
    $success = $false
}

# Method 2: Try without config if Method 1 failed
if (-not $success) {
    Write-Host "`n🚀 Method 2: Starting Redis without config..." -ForegroundColor Cyan
    try {
        $process2 = Start-Process -FilePath $redisExe -PassThru -WindowStyle Normal
        Start-Sleep -Seconds 3
        
        if (-not $process2.HasExited) {
            Write-Host "✅ Redis started successfully without config!" -ForegroundColor Green
            $success = $true
        } else {
            Write-Host "❌ Redis exited immediately" -ForegroundColor Red
            $success = $false
        }
    } catch {
        Write-Host "❌ Failed: $($_.Exception.Message)" -ForegroundColor Red
        $success = $false
    }
}

# Method 3: Try as Windows Service
if (-not $success) {
    Write-Host "`n🚀 Method 3: Installing Redis as Windows Service..." -ForegroundColor Cyan
    try {
        # Install Redis as service
        $installArgs = "--service-install --service-name Redis --port 6379"
        Start-Process -FilePath $redisExe -ArgumentList $installArgs -Wait -WindowStyle Hidden
        
        # Start the service
        Start-Process -FilePath $redisExe -ArgumentList "--service-start" -Wait -WindowStyle Hidden
        Start-Sleep -Seconds 3
        
        # Check if service is running
        $service = Get-Service -Name "Redis" -ErrorAction SilentlyContinue
        if ($service -and $service.Status -eq "Running") {
            Write-Host "✅ Redis service started successfully!" -ForegroundColor Green
            $success = $true
        } else {
            Write-Host "❌ Redis service failed to start" -ForegroundColor Red
        }
    } catch {
        Write-Host "❌ Service installation failed: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Test Redis connection
if ($success) {
    Write-Host "`n🧪 Testing Redis connection..." -ForegroundColor Cyan
    $redisCliPath = Join-Path $redisPath "redis-cli.exe"
    
    if (Test-Path $redisCliPath) {
        try {
            $testResult = & $redisCliPath ping 2>$null
            if ($testResult -eq "PONG") {
                Write-Host "✅ SUCCESS! Redis is running and responding!" -ForegroundColor Green
                Write-Host "🎉 Redis is ready at localhost:6379" -ForegroundColor Green
            } else {
                Write-Host "⚠️ Redis started but not responding to ping" -ForegroundColor Yellow
                Write-Host "Trying alternative test..." -ForegroundColor Yellow
                
                # Try connecting via telnet test
                try {
                    $tcpClient = New-Object System.Net.Sockets.TcpClient
                    $tcpClient.Connect("127.0.0.1", 6379)
                    $tcpClient.Close()
                    Write-Host "✅ Redis port 6379 is accessible!" -ForegroundColor Green
                } catch {
                    Write-Host "❌ Cannot connect to Redis port 6379" -ForegroundColor Red
                }
            }
        } catch {
            Write-Host "❌ redis-cli test failed: $($_.Exception.Message)" -ForegroundColor Red
        }
    } else {
        Write-Host "⚠️ redis-cli.exe not found, testing port directly..." -ForegroundColor Yellow
        try {
            $tcpClient = New-Object System.Net.Sockets.TcpClient
            $tcpClient.Connect("127.0.0.1", 6379)
            $tcpClient.Close()
            Write-Host "✅ Redis port 6379 is accessible!" -ForegroundColor Green
        } catch {
            Write-Host "❌ Cannot connect to Redis port 6379" -ForegroundColor Red
        }
    }
} else {
    Write-Host "`n❌ All Redis startup methods failed!" -ForegroundColor Red
    Write-Host "`n🔧 Alternative Solutions:" -ForegroundColor Cyan
    Write-Host "1. Try WSL2 Redis:" -ForegroundColor White
    Write-Host "   wsl --install" -ForegroundColor Gray
    Write-Host "   wsl" -ForegroundColor Gray
    Write-Host "   sudo apt update && sudo apt install redis-server" -ForegroundColor Gray
    Write-Host "   sudo service redis-server start" -ForegroundColor Gray
    
    Write-Host "`n2. Use Docker Desktop:" -ForegroundColor White
    Write-Host "   docker run -d --name redis -p 6379:6379 redis:latest" -ForegroundColor Gray
    
    Write-Host "`n3. Try different Redis version:" -ForegroundColor White
    Write-Host "   choco uninstall redis-64" -ForegroundColor Gray
    Write-Host "   choco install redis-64 --version=3.0.503" -ForegroundColor Gray
}

Write-Host "`nPress any key to continue..." -ForegroundColor Yellow
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
