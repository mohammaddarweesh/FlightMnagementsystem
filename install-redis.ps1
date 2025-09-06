# Redis Installation Script for Windows
Write-Host "🔴 Redis Installation Helper" -ForegroundColor Red

# Check if Redis is already installed
Write-Host "`n🔍 Checking if Redis is already installed..." -ForegroundColor Yellow

# Check for Redis in common locations
$redisLocations = @(
    "C:\Program Files\Redis\redis-server.exe",
    "C:\Program Files (x86)\Redis\redis-server.exe",
    "C:\Redis\redis-server.exe",
    "C:\tools\redis\redis-server.exe"
)

$redisFound = $false
foreach ($location in $redisLocations) {
    if (Test-Path $location) {
        Write-Host "✅ Redis found at: $location" -ForegroundColor Green
        $redisFound = $true
        break
    }
}

# Check if Redis is in PATH
try {
    $redisVersion = redis-server --version 2>$null
    if ($redisVersion) {
        Write-Host "✅ Redis is available in PATH" -ForegroundColor Green
        $redisFound = $true
    }
} catch {
    # Redis not in PATH
}

if ($redisFound) {
    Write-Host "`n🎉 Redis is already installed!" -ForegroundColor Green
    
    # Try to start Redis
    Write-Host "`n🚀 Attempting to start Redis..." -ForegroundColor Yellow
    try {
        Start-Process -FilePath "redis-server" -WindowStyle Minimized
        Start-Sleep -Seconds 3
        
        # Test connection
        $testResult = redis-cli ping 2>$null
        if ($testResult -eq "PONG") {
            Write-Host "✅ Redis is running and responding!" -ForegroundColor Green
        } else {
            Write-Host "⚠️  Redis started but not responding to ping" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "❌ Could not start Redis automatically" -ForegroundColor Red
        Write-Host "Try starting manually: redis-server" -ForegroundColor Yellow
    }
} else {
    Write-Host "`n❌ Redis not found. Let's install it!" -ForegroundColor Red
    
    # Check if Chocolatey is installed
    try {
        $chocoVersion = choco --version 2>$null
        if ($chocoVersion) {
            Write-Host "✅ Chocolatey is installed" -ForegroundColor Green
            
            Write-Host "`n🍫 Installing Redis via Chocolatey..." -ForegroundColor Yellow
            Write-Host "This requires Administrator privileges." -ForegroundColor Yellow
            
            $choice = Read-Host "Install Redis now? (y/n)"
            if ($choice -eq "y" -or $choice -eq "Y") {
                try {
                    Start-Process -FilePath "powershell" -ArgumentList "choco install redis-64 -y" -Verb RunAs -Wait
                    Write-Host "✅ Redis installation completed!" -ForegroundColor Green
                } catch {
                    Write-Host "❌ Installation failed. Try running as Administrator:" -ForegroundColor Red
                    Write-Host "choco install redis-64" -ForegroundColor Yellow
                }
            }
        } else {
            Write-Host "❌ Chocolatey not found" -ForegroundColor Red
        }
    } catch {
        Write-Host "❌ Chocolatey not found" -ForegroundColor Red
    }
    
    if (!$chocoVersion) {
        Write-Host "`n📋 Manual Installation Options:" -ForegroundColor Cyan
        Write-Host "1. Install Chocolatey first:" -ForegroundColor White
        Write-Host "   Set-ExecutionPolicy Bypass -Scope Process -Force" -ForegroundColor Gray
        Write-Host "   iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))" -ForegroundColor Gray
        Write-Host "   Then run: choco install redis-64" -ForegroundColor Gray
        
        Write-Host "`n2. Download Redis manually:" -ForegroundColor White
        Write-Host "   https://github.com/microsoftarchive/redis/releases" -ForegroundColor Gray
        Write-Host "   Download: Redis-x64-3.0.504.msi" -ForegroundColor Gray
        
        Write-Host "`n3. Use WSL2:" -ForegroundColor White
        Write-Host "   wsl --install" -ForegroundColor Gray
        Write-Host "   sudo apt install redis-server" -ForegroundColor Gray
        Write-Host "   sudo service redis-server start" -ForegroundColor Gray
    }
}

Write-Host "`n📝 Next Steps:" -ForegroundColor Magenta
Write-Host "1. Ensure Redis is running: redis-server" -ForegroundColor White
Write-Host "2. Test connection: redis-cli ping" -ForegroundColor White
Write-Host "3. Run the setup test: .\test-setup.ps1" -ForegroundColor White

Write-Host "`n🔗 Useful Redis Commands:" -ForegroundColor Cyan
Write-Host "redis-server                 # Start Redis server" -ForegroundColor Gray
Write-Host "redis-cli ping              # Test connection" -ForegroundColor Gray
Write-Host "redis-cli                   # Open Redis CLI" -ForegroundColor Gray
Write-Host "redis-server --service-install  # Install as Windows service" -ForegroundColor Gray
Write-Host "redis-server --service-start    # Start Windows service" -ForegroundColor Gray

Write-Host "`nPress any key to continue..." -ForegroundColor Yellow
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
