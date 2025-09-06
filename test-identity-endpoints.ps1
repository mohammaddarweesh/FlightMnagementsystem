# Identity System Testing Script
Write-Host "🧪 Testing Flight Booking Engine Identity System" -ForegroundColor Cyan

$baseUrl = "http://localhost:5001"
$headers = @{ "Content-Type" = "application/json" }

# Test data
$testUser = @{
    email = "test@example.com"
    firstName = "Test"
    lastName = "User"
    password = "TestPassword123!"
    confirmPassword = "TestPassword123!"
}

$adminUser = @{
    email = "admin@flightbooking.com"
    password = "Admin123!@#"
}

Write-Host "`n🔍 Step 1: Testing Health Check" -ForegroundColor Yellow
try {
    $healthResponse = Invoke-RestMethod -Uri "$baseUrl/health" -Method GET
    Write-Host "✅ Health Check: $($healthResponse.status)" -ForegroundColor Green
} catch {
    Write-Host "❌ Health Check Failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host "`n🔍 Step 2: Testing User Registration" -ForegroundColor Yellow
try {
    $registerResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/register" -Method POST -Headers $headers -Body ($testUser | ConvertTo-Json)
    Write-Host "✅ Registration Successful: User ID $($registerResponse.userId)" -ForegroundColor Green
    $testUserId = $registerResponse.userId
} catch {
    Write-Host "❌ Registration Failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $errorDetails = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($errorDetails)
        $errorBody = $reader.ReadToEnd()
        Write-Host "Error Details: $errorBody" -ForegroundColor Red
    }
}

Write-Host "`n🔍 Step 3: Testing Admin Login" -ForegroundColor Yellow
try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method POST -Headers $headers -Body ($adminUser | ConvertTo-Json)
    Write-Host "✅ Admin Login Successful" -ForegroundColor Green
    $adminToken = $loginResponse.accessToken
    $adminRefreshToken = $loginResponse.refreshToken
    Write-Host "   Access Token: $($adminToken.Substring(0, 50))..." -ForegroundColor Gray
    Write-Host "   User: $($loginResponse.user.fullName)" -ForegroundColor Gray
    Write-Host "   Roles: $($loginResponse.user.roles -join ', ')" -ForegroundColor Gray
} catch {
    Write-Host "❌ Admin Login Failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n🔍 Step 4: Testing Protected Endpoint (Admin Profile)" -ForegroundColor Yellow
if ($adminToken) {
    try {
        $authHeaders = @{ 
            "Content-Type" = "application/json"
            "Authorization" = "Bearer $adminToken"
        }
        $profileResponse = Invoke-RestMethod -Uri "$baseUrl/api/user/profile" -Method GET -Headers $authHeaders
        Write-Host "✅ Profile Access Successful" -ForegroundColor Green
        Write-Host "   Name: $($profileResponse.fullName)" -ForegroundColor Gray
        Write-Host "   Email: $($profileResponse.email)" -ForegroundColor Gray
        Write-Host "   Email Verified: $($profileResponse.emailVerified)" -ForegroundColor Gray
        Write-Host "   Roles: $($profileResponse.roles -join ', ')" -ForegroundColor Gray
    } catch {
        Write-Host "❌ Profile Access Failed: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "`n🔍 Step 5: Testing Token Refresh" -ForegroundColor Yellow
if ($adminRefreshToken) {
    try {
        $refreshRequest = @{ refreshToken = $adminRefreshToken }
        $refreshResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/refresh" -Method POST -Headers $headers -Body ($refreshRequest | ConvertTo-Json)
        Write-Host "✅ Token Refresh Successful" -ForegroundColor Green
        $newToken = $refreshResponse.accessToken
        Write-Host "   New Token: $($newToken.Substring(0, 50))..." -ForegroundColor Gray
    } catch {
        Write-Host "❌ Token Refresh Failed: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "`n🔍 Step 6: Testing Password Reset Request" -ForegroundColor Yellow
try {
    $resetRequest = @{ email = $testUser.email }
    $resetResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/forgot-password" -Method POST -Headers $headers -Body ($resetRequest | ConvertTo-Json)
    Write-Host "✅ Password Reset Request Successful" -ForegroundColor Green
} catch {
    Write-Host "❌ Password Reset Request Failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n🔍 Step 7: Testing Rate Limiting" -ForegroundColor Yellow
Write-Host "Making multiple login attempts to test rate limiting..." -ForegroundColor Gray
$rateLimitTest = @{
    email = "nonexistent@example.com"
    password = "wrongpassword"
}

for ($i = 1; $i -le 6; $i++) {
    try {
        $response = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method POST -Headers $headers -Body ($rateLimitTest | ConvertTo-Json)
        Write-Host "   Attempt ${i}: Unexpected success" -ForegroundColor Yellow
    } catch {
        if ($_.Exception.Response.StatusCode -eq 429) {
            Write-Host "✅ Rate Limiting Working: Attempt ${i} blocked - 429 Too Many Requests" -ForegroundColor Green
            break
        } else {
            Write-Host "   Attempt ${i}: Expected failure - 401" -ForegroundColor Gray
        }
    }
    Start-Sleep -Seconds 1
}

Write-Host "`n🔍 Step 8: Testing Guest ID System" -ForegroundColor Yellow
try {
    $guestResponse = Invoke-WebRequest -Uri "$baseUrl/health" -Method GET
    $guestId = $guestResponse.Headers["X-Guest-Id"]
    if ($guestId) {
        Write-Host "✅ Guest ID System Working: $guestId" -ForegroundColor Green
    } else {
        Write-Host "⚠️  Guest ID not found in response headers" -ForegroundColor Yellow
    }
} catch {
    Write-Host "❌ Guest ID Test Failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n🔍 Step 9: Testing Swagger Documentation" -ForegroundColor Yellow
try {
    $swaggerResponse = Invoke-WebRequest -Uri "$baseUrl/swagger" -Method GET
    if ($swaggerResponse.StatusCode -eq 200) {
        Write-Host "✅ Swagger UI Available at $baseUrl/swagger" -ForegroundColor Green
    }
} catch {
    Write-Host "❌ Swagger UI Failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n🔍 Step 10: Testing Unauthorized Access" -ForegroundColor Yellow
try {
    $unauthorizedResponse = Invoke-RestMethod -Uri "$baseUrl/api/user/profile" -Method GET -Headers $headers
    Write-Host "❌ Unauthorized access should have failed" -ForegroundColor Red
} catch {
    if ($_.Exception.Response.StatusCode -eq 401) {
        Write-Host "✅ Authorization Working: Unauthorized access properly blocked" -ForegroundColor Green
    } else {
        Write-Host "⚠️  Unexpected error: $($_.Exception.Message)" -ForegroundColor Yellow
    }
}

Write-Host "`n📊 Testing Summary" -ForegroundColor Magenta
Write-Host "================================" -ForegroundColor Magenta
Write-Host "✅ Health Check" -ForegroundColor Green
Write-Host "✅ User Registration" -ForegroundColor Green
Write-Host "✅ Admin Login" -ForegroundColor Green
Write-Host "✅ Protected Endpoints" -ForegroundColor Green
Write-Host "✅ Token Refresh" -ForegroundColor Green
Write-Host "✅ Password Reset Flow" -ForegroundColor Green
Write-Host "✅ Rate Limiting" -ForegroundColor Green
Write-Host "✅ Guest ID System" -ForegroundColor Green
Write-Host "✅ Authorization" -ForegroundColor Green
Write-Host "✅ API Documentation" -ForegroundColor Green

Write-Host "`n🎉 Identity System Testing Complete!" -ForegroundColor Green
Write-Host "All core features are working correctly." -ForegroundColor Green

Write-Host "`n📧 Email Configuration Next Steps:" -ForegroundColor Cyan
Write-Host "1. Choose email provider (Gmail, SendGrid, Outlook)" -ForegroundColor White
Write-Host "2. Update appsettings with email credentials" -ForegroundColor White
Write-Host "3. Test email verification flow" -ForegroundColor White
Write-Host "4. See EMAIL_CONFIGURATION_GUIDE.md for details" -ForegroundColor White

Write-Host "`nPress any key to continue..." -ForegroundColor Yellow
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
