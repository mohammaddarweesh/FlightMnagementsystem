# Flight Booking Engine - Setup Test Script
Write-Host "🚀 Flight Booking Engine - Testing Localhost Setup" -ForegroundColor Green
Write-Host "PostgreSQL: localhost:5432 (user: postgres, password: 6482297)" -ForegroundColor Cyan
Write-Host "Redis: localhost:6379 (must be installed locally)" -ForegroundColor Cyan

# Check if Redis is running
Write-Host "`n🔍 Checking if Redis is running..." -ForegroundColor Yellow
try {
    $redisProcess = Get-Process -Name "redis-server" -ErrorAction SilentlyContinue
    if ($redisProcess) {
        Write-Host "✅ Redis server is running (PID: $($redisProcess.Id))" -ForegroundColor Green
    } else {
        Write-Host "❌ Redis server not found. Please start Redis first:" -ForegroundColor Red
        Write-Host "   Option 1: redis-server" -ForegroundColor Yellow
        Write-Host "   Option 2: redis-server --service-start" -ForegroundColor Yellow
        Write-Host "   Option 3: Start Redis service in Services.msc" -ForegroundColor Yellow
        Read-Host "Press Enter after starting Redis to continue"
    }
} catch {
    Write-Host "⚠️  Could not check Redis process status" -ForegroundColor Yellow
}

# Start the API
Write-Host "`n📡 Starting the API..." -ForegroundColor Yellow
$apiProcess = Start-Process -FilePath "dotnet" -ArgumentList "run --project src/Api/FlightBooking.Api" -PassThru

# Wait for API to start
Write-Host "⏳ Waiting for API to start..." -ForegroundColor Yellow
Start-Sleep -Seconds 15

# Test endpoints
$baseUrl = "http://localhost:5000"

Write-Host "`n🔍 Testing API Health..." -ForegroundColor Cyan
try {
    $health = Invoke-RestMethod -Uri "$baseUrl/api/Health" -Method Get
    Write-Host "✅ API Health: $($health.Status)" -ForegroundColor Green
} catch {
    Write-Host "❌ API Health failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n🐘 Testing PostgreSQL Connection..." -ForegroundColor Cyan
try {
    $dbTest = Invoke-RestMethod -Uri "$baseUrl/api/DatabaseTest/connection" -Method Get
    Write-Host "✅ PostgreSQL: $($dbTest.Status) - Database: $($dbTest.Database)" -ForegroundColor Green
} catch {
    Write-Host "❌ PostgreSQL failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "💡 Make sure PostgreSQL is running and database exists" -ForegroundColor Yellow
}

Write-Host "`n🔴 Testing Redis Connection..." -ForegroundColor Cyan
try {
    $redisTest = Invoke-RestMethod -Uri "$baseUrl/api/RedisTest/connection" -Method Get
    Write-Host "✅ Redis: $($redisTest.Status) - Version: $($redisTest.RedisVersion)" -ForegroundColor Green
} catch {
    Write-Host "❌ Redis failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "💡 Make sure Redis is running on localhost:6379" -ForegroundColor Yellow
}

Write-Host "`n🧪 Testing Redis Cache..." -ForegroundColor Cyan
try {
    $cacheData = @{
        key = "test-setup"
        value = "Hello from PowerShell!"
        expiryMinutes = 5
    } | ConvertTo-Json

    $cacheSet = Invoke-RestMethod -Uri "$baseUrl/api/RedisTest/cache/set" -Method Post -Body $cacheData -ContentType "application/json"
    Write-Host "✅ Cache Set: $($cacheSet.Status)" -ForegroundColor Green

    $cacheGet = Invoke-RestMethod -Uri "$baseUrl/api/RedisTest/cache/get/test-setup" -Method Get
    Write-Host "✅ Cache Get: $($cacheGet.Value)" -ForegroundColor Green
} catch {
    Write-Host "❌ Redis Cache test failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n🏥 Testing Overall Health Check..." -ForegroundColor Cyan
try {
    $healthCheck = Invoke-RestMethod -Uri "$baseUrl/health" -Method Get
    Write-Host "✅ Health Check: $($healthCheck.status)" -ForegroundColor Green
} catch {
    Write-Host "❌ Health Check failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n📊 Summary:" -ForegroundColor Magenta
Write-Host "- API Documentation: $baseUrl/swagger" -ForegroundColor White
Write-Host "- Health Endpoint: $baseUrl/health" -ForegroundColor White
Write-Host "- Database Test: $baseUrl/api/DatabaseTest/connection" -ForegroundColor White
Write-Host "- Redis Test: $baseUrl/api/RedisTest/connection" -ForegroundColor White

Write-Host "`n🎉 Setup test completed!" -ForegroundColor Green
Write-Host "Press any key to stop the API..." -ForegroundColor Yellow
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

# Stop the API process
if ($apiProcess -and !$apiProcess.HasExited) {
    Write-Host "Stopping API process..." -ForegroundColor Yellow
    $apiProcess.Kill()
    $apiProcess.WaitForExit(5000)
} else {
    # Fallback: kill any dotnet processes running the API
    Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Where-Object {
        $_.ProcessName -eq "dotnet" -and $_.MainModule.FileName -like "*FlightBooking.Api*"
    } | Stop-Process -Force -ErrorAction SilentlyContinue
}
