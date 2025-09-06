using FlightBooking.Application.Identity.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace FlightBooking.Infrastructure.Identity;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _baseUrl;
    private readonly string _provider;
    private readonly string _fromEmail;
    private readonly string _fromName;
    private readonly SmtpSettings _smtpSettings;

    public EmailService(ILogger<EmailService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _baseUrl = _configuration["AppSettings:BaseUrl"] ?? "http://localhost:5000";
        _provider = _configuration["EmailSettings:Provider"] ?? "Console";
        _fromEmail = _configuration["EmailSettings:FromEmail"] ?? "noreply@flightbooking.com";
        _fromName = _configuration["EmailSettings:FromName"] ?? "Flight Booking Engine";

        _smtpSettings = new SmtpSettings
        {
            Host = _configuration["EmailSettings:SmtpHost"] ?? "localhost",
            Port = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587"),
            EnableSsl = bool.Parse(_configuration["EmailSettings:SmtpEnableSsl"] ?? "true"),
            Username = _configuration["EmailSettings:SmtpUsername"] ?? "",
            Password = _configuration["EmailSettings:SmtpPassword"] ?? "",
            FromEmail = _fromEmail
        };
    }

    public async Task SendEmailVerificationAsync(string email, string firstName, string verificationToken)
    {
        var verificationUrl = $"{_baseUrl}/api/auth/verify-email?token={verificationToken}";
        
        var subject = "Verify Your Email Address";
        var body = $@"
            <h2>Welcome to Flight Booking Engine, {firstName}!</h2>
            <p>Thank you for registering with us. Please verify your email address by clicking the link below:</p>
            <p><a href='{verificationUrl}' style='background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Verify Email</a></p>
            <p>If the button doesn't work, copy and paste this link into your browser:</p>
            <p>{verificationUrl}</p>
            <p>This link will expire in 24 hours.</p>
            <p>If you didn't create an account with us, please ignore this email.</p>
        ";

        await SendEmailAsync(email, subject, body);
    }

    public async Task SendPasswordResetAsync(string email, string firstName, string resetToken)
    {
        var resetUrl = $"{_baseUrl}/reset-password?token={resetToken}";
        
        var subject = "Reset Your Password";
        var body = $@"
            <h2>Password Reset Request</h2>
            <p>Hello {firstName},</p>
            <p>We received a request to reset your password. Click the link below to reset it:</p>
            <p><a href='{resetUrl}' style='background-color: #dc3545; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Reset Password</a></p>
            <p>If the button doesn't work, copy and paste this link into your browser:</p>
            <p>{resetUrl}</p>
            <p>This link will expire in 1 hour.</p>
            <p>If you didn't request a password reset, please ignore this email.</p>
        ";

        await SendEmailAsync(email, subject, body);
    }

    public async Task SendWelcomeEmailAsync(string email, string firstName)
    {
        var subject = "Welcome to Flight Booking Engine!";
        var body = $@"
            <h2>Welcome aboard, {firstName}!</h2>
            <p>Your email has been verified and your account is now active.</p>
            <p>You can now:</p>
            <ul>
                <li>Search and book flights</li>
                <li>Manage your bookings</li>
                <li>Update your profile</li>
                <li>Receive exclusive offers</li>
            </ul>
            <p>Thank you for choosing Flight Booking Engine!</p>
        ";

        await SendEmailAsync(email, subject, body);
    }

    public async Task SendPasswordChangedNotificationAsync(string email, string firstName)
    {
        var subject = "Password Changed Successfully";
        var body = $@"
            <h2>Password Changed</h2>
            <p>Hello {firstName},</p>
            <p>Your password has been successfully changed.</p>
            <p>If you didn't make this change, please contact our support team immediately.</p>
            <p>For security reasons, you have been logged out of all devices.</p>
        ";

        await SendEmailAsync(email, subject, body);
    }

    public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true, string? from = null)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Flight Booking System", from ?? _smtpSettings.FromEmail));
        message.To.Add(new MailboxAddress("", to));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder();
        if (isHtml)
        {
            bodyBuilder.HtmlBody = body;
        }
        else
        {
            bodyBuilder.TextBody = body;
        }
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_smtpSettings.Host, _smtpSettings.Port, _smtpSettings.EnableSsl);

        if (!string.IsNullOrEmpty(_smtpSettings.Username))
        {
            await client.AuthenticateAsync(_smtpSettings.Username, _smtpSettings.Password);
        }

        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }

    public async Task SendBookingConfirmationAsync(string email, string customerName, string bookingReference,
        string flightDetails, decimal totalAmount, string currency = "USD")
    {
        var subject = $"Booking Confirmation - {bookingReference}";
        var body = $@"
            <h2>Booking Confirmation</h2>
            <p>Dear {customerName},</p>
            <p>Your booking has been confirmed!</p>
            <p><strong>Booking Reference:</strong> {bookingReference}</p>
            <p><strong>Flight Details:</strong> {flightDetails}</p>
            <p><strong>Total Amount:</strong> {totalAmount:C} {currency}</p>
            <p>Thank you for choosing our service!</p>
            <p>Best regards,<br>Flight Booking Team</p>
        ";

        await SendEmailAsync(email, subject, body);
    }

    public async Task SendBookingCancellationAsync(string email, string customerName, string bookingReference,
        string reason, decimal? refundAmount = null, string currency = "USD")
    {
        var subject = $"Booking Cancellation - {bookingReference}";
        var refundInfo = refundAmount.HasValue ? $"<p><strong>Refund Amount:</strong> {refundAmount:C} {currency}</p>" : "";

        var body = $@"
            <h2>Booking Cancellation</h2>
            <p>Dear {customerName},</p>
            <p>Your booking {bookingReference} has been cancelled.</p>
            <p><strong>Reason:</strong> {reason}</p>
            {refundInfo}
            <p>We apologize for any inconvenience caused.</p>
            <p>Best regards,<br>Flight Booking Team</p>
        ";

        await SendEmailAsync(email, subject, body);
    }

    public async Task SendBookingReminderAsync(string email, string customerName, string bookingReference,
        string flightDetails, DateTime departureTime)
    {
        var subject = $"Flight Reminder - {bookingReference}";
        var body = $@"
            <h2>Flight Reminder</h2>
            <p>Dear {customerName},</p>
            <p>This is a reminder for your upcoming flight.</p>
            <p><strong>Booking Reference:</strong> {bookingReference}</p>
            <p><strong>Flight Details:</strong> {flightDetails}</p>
            <p><strong>Departure Time:</strong> {departureTime:yyyy-MM-dd HH:mm}</p>
            <p>Please arrive at the airport at least 2 hours before departure.</p>
            <p>Best regards,<br>Flight Booking Team</p>
        ";

        await SendEmailAsync(email, subject, body);
    }

    private async Task SendEmailAsync(string email, string subject, string body)
    {
        try
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
                    _logger.LogInformation("📧 Email to {Email} with subject: {Subject}", email, subject);
                    _logger.LogDebug("Email body: {Body}", body);
                    await Task.Delay(100); // Simulate async operation
                    break;
            }

            _logger.LogInformation("Email sent successfully to {Email} via {Provider}", email, _provider);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email} via {Provider}", email, _provider);
            throw;
        }
    }

    private async Task SendViaSendGridAsync(string email, string subject, string body)
    {
        var apiKey = _configuration["EmailSettings:SendGrid:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("SendGrid API key not configured");

        var client = new SendGridClient(apiKey);
        var from = new EmailAddress(_fromEmail, _fromName);
        var to = new EmailAddress(email);
        var msg = MailHelper.CreateSingleEmail(from, to, subject, body, body);

        var response = await client.SendEmailAsync(msg);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Body.ReadAsStringAsync();
            throw new InvalidOperationException($"SendGrid error: {response.StatusCode} - {errorBody}");
        }
    }

    private async Task SendViaSMTPAsync(string email, string subject, string body)
    {
        var host = _configuration["EmailSettings:SMTP:Host"];
        var port = int.Parse(_configuration["EmailSettings:SMTP:Port"] ?? "587");
        var enableSsl = bool.Parse(_configuration["EmailSettings:SMTP:EnableSsl"] ?? "true");
        var username = _configuration["EmailSettings:SMTP:Username"];
        var password = _configuration["EmailSettings:SMTP:Password"];

        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            throw new InvalidOperationException("SMTP settings not properly configured");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_fromName, _fromEmail));
        message.To.Add(new MailboxAddress("", email));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = body,
            TextBody = StripHtml(body)
        };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(host, port, enableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);
        await client.AuthenticateAsync(username, password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }

    private static string StripHtml(string html)
    {
        return System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", string.Empty);
    }
}

public class SmtpSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
}
