# 📧 Email Configuration Guide for Flight Booking Engine

## 🎯 **Current Status**
- ✅ Email service is implemented and ready
- ✅ Email templates are created (verification, password reset, welcome)
- ✅ Currently logs emails to console for testing
- 🔧 **Ready to configure real email provider**

## 🚀 **Quick Setup Options**

### **Option 1: SendGrid (Recommended)**
```json
// Add to appsettings.Development.json
{
  "EmailSettings": {
    "Provider": "SendGrid",
    "SendGrid": {
      "ApiKey": "YOUR_SENDGRID_API_KEY",
      "FromEmail": "noreply@flightbooking.com",
      "FromName": "Flight Booking Engine"
    }
  }
}
```

### **Option 2: Gmail SMTP**
```json
{
  "EmailSettings": {
    "Provider": "SMTP",
    "SMTP": {
      "Host": "smtp.gmail.com",
      "Port": 587,
      "EnableSsl": true,
      "Username": "your-email@gmail.com",
      "Password": "your-app-password",
      "FromEmail": "your-email@gmail.com",
      "FromName": "Flight Booking Engine"
    }
  }
}
```

### **Option 3: Outlook/Hotmail SMTP**
```json
{
  "EmailSettings": {
    "Provider": "SMTP",
    "SMTP": {
      "Host": "smtp-mail.outlook.com",
      "Port": 587,
      "EnableSsl": true,
      "Username": "your-email@outlook.com",
      "Password": "your-password",
      "FromEmail": "your-email@outlook.com",
      "FromName": "Flight Booking Engine"
    }
  }
}
```

## 🔧 **Implementation Steps**

### **Step 1: Choose Your Email Provider**

#### **SendGrid Setup (Easiest)**
1. Go to [SendGrid.com](https://sendgrid.com)
2. Create free account (100 emails/day free)
3. Get API key from Settings > API Keys
4. Add configuration to appsettings

#### **Gmail Setup**
1. Enable 2-factor authentication on Gmail
2. Generate App Password: Google Account > Security > App passwords
3. Use app password (not your regular password)

#### **Outlook Setup**
1. Use your regular Outlook/Hotmail credentials
2. May need to enable "Less secure app access"

### **Step 2: Update Email Service**

I'll create an enhanced email service that supports multiple providers:

```csharp
// Enhanced EmailService with real providers
public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly string _provider;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _provider = _configuration["EmailSettings:Provider"] ?? "Console";
    }

    private async Task SendEmailAsync(string email, string subject, string body)
    {
        switch (_provider.ToLower())
        {
            case "sendgrid":
                await SendViaSendGridAsync(email, subject, body);
                break;
            case "smtp":
                await SendViaSMTPAsync(email, subject, body);
                break;
            default:
                // Console logging (current implementation)
                _logger.LogInformation("Email to {Email}: {Subject}", email, subject);
                _logger.LogDebug("Email body: {Body}", body);
                break;
        }
    }
}
```

### **Step 3: Add Required Packages**

```xml
<!-- Add to Infrastructure project -->
<PackageReference Include="SendGrid" Version="9.29.3" />
<PackageReference Include="MailKit" Version="4.3.0" />
```

## 📋 **Configuration Templates**

### **For Testing with Your Email**

```json
// appsettings.Development.json
{
  "EmailSettings": {
    "Provider": "SMTP",
    "SMTP": {
      "Host": "smtp.gmail.com",
      "Port": 587,
      "EnableSsl": true,
      "Username": "YOUR_EMAIL@gmail.com",
      "Password": "YOUR_APP_PASSWORD",
      "FromEmail": "YOUR_EMAIL@gmail.com",
      "FromName": "Flight Booking Test"
    }
  },
  "AppSettings": {
    "BaseUrl": "http://localhost:5000"
  }
}
```

## 🧪 **Testing Email Verification**

### **1. Register a New User**
```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "YOUR_EMAIL@gmail.com",
    "firstName": "Test",
    "lastName": "User", 
    "password": "TestPassword123!",
    "confirmPassword": "TestPassword123!"
  }'
```

### **2. Check Email for Verification Link**
- Email will contain link like: `http://localhost:5000/api/auth/verify-email?token=ABC123`

### **3. Click Link or Test Manually**
```bash
curl -X POST "http://localhost:5000/api/auth/verify-email?token=YOUR_TOKEN"
```

## 🔐 **Security Considerations**

### **Environment Variables (Recommended)**
```bash
# Set environment variables instead of config files
export EmailSettings__SendGrid__ApiKey="your-api-key"
export EmailSettings__SMTP__Password="your-password"
```

### **Azure Key Vault (Production)**
```json
{
  "EmailSettings": {
    "Provider": "SendGrid",
    "SendGrid": {
      "ApiKey": "@Microsoft.KeyVault(SecretUri=https://vault.vault.azure.net/secrets/sendgrid-key/)",
      "FromEmail": "noreply@yourdomain.com"
    }
  }
}
```

## 📧 **Email Templates**

The system includes these email templates:

### **1. Email Verification**
- **Subject**: "Verify Your Email Address"
- **Content**: Welcome message + verification button
- **Expiry**: 24 hours

### **2. Password Reset**
- **Subject**: "Reset Your Password"
- **Content**: Reset instructions + reset button
- **Expiry**: 1 hour

### **3. Welcome Email**
- **Subject**: "Welcome to Flight Booking Engine!"
- **Content**: Account activated confirmation
- **Sent**: After email verification

### **4. Password Changed**
- **Subject**: "Password Changed Successfully"
- **Content**: Security notification
- **Sent**: After password change

## 🚀 **Quick Start for Testing**

### **Option A: Use Your Gmail**
1. Enable 2FA on Gmail
2. Generate App Password
3. Update config with your Gmail + App Password
4. Test registration with your email

### **Option B: Use SendGrid Free**
1. Sign up at SendGrid.com
2. Get free API key (100 emails/day)
3. Update config with API key
4. Test with any email address

### **Option C: Keep Console Logging**
- Current setup logs emails to console
- Perfect for development/testing
- No external dependencies

## 📞 **Need Help?**

**To send test emails to you, I need:**

1. **Your preferred email provider** (Gmail, Outlook, SendGrid)
2. **Your email address** for testing
3. **If Gmail**: Your app password (not regular password)
4. **If Outlook**: Your email + password
5. **If SendGrid**: I can help you set up free account

**Current Status**: Ready to configure any provider you choose!

Would you like me to implement the enhanced email service with your preferred provider?
