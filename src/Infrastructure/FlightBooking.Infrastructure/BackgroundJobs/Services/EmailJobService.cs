using FlightBooking.Application.Identity.Interfaces;
using FlightBooking.Infrastructure.BackgroundJobs.Attributes;
using FlightBooking.Infrastructure.BackgroundJobs.Configuration;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace FlightBooking.Infrastructure.BackgroundJobs.Services;

public class EmailJobService
{
    private readonly IEmailService _emailService;
    private readonly ILogger<EmailJobService> _logger;

    public EmailJobService(IEmailService emailService, ILogger<EmailJobService> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    [EmailRetry]
    [Queue(HangfireQueues.Emails)]
    public async Task SendBookingConfirmationEmailAsync(Guid bookingId, string customerEmail, string customerName, string bookingReference, string? correlationId = null)
    {
        _logger.LogInformation("Sending booking confirmation email for {BookingId}", bookingId);
        await _emailService.SendEmailAsync(customerEmail, "Booking Confirmation", $"Dear {customerName}, your booking {bookingReference} is confirmed.");
    }

    [EmailRetry]
    [Queue(HangfireQueues.Emails)]
    public async Task SendBookingReminderEmailAsync(Guid bookingId, string customerEmail, string customerName, string bookingReference, DateTime departureTime, string flightNumber, string? correlationId = null)
    {
        _logger.LogInformation("Sending booking reminder email for {BookingId}", bookingId);
        await _emailService.SendEmailAsync(customerEmail, "Flight Reminder", $"Dear {customerName}, your flight {flightNumber} departs on {departureTime}.");
    }

    [EmailRetry]
    [Queue(HangfireQueues.Emails)]
    public async Task SendBookingCancellationEmailAsync(Guid bookingId, string customerEmail, string customerName, string bookingReference, string reason, decimal? refundAmount = null, string? correlationId = null)
    {
        _logger.LogInformation("Sending booking cancellation email for {BookingId}", bookingId);
        await _emailService.SendEmailAsync(customerEmail, "Booking Cancelled", $"Dear {customerName}, your booking {bookingReference} has been cancelled. Reason: {reason}");
    }

    [EmailRetry]
    [Queue(HangfireQueues.Emails)]
    public async Task SendPasswordResetEmailAsync(string email, string firstName, string resetToken, string? correlationId = null)
    {
        _logger.LogInformation("Sending password reset email to {Email}", email);
        await _emailService.SendPasswordResetAsync(email, firstName, resetToken);
    }

    [EmailRetry]
    [Queue(HangfireQueues.Emails)]
    public async Task SendWelcomeEmailAsync(string email, string firstName, string? correlationId = null)
    {
        _logger.LogInformation("Sending welcome email to {Email}", email);
        await _emailService.SendWelcomeEmailAsync(email, firstName);
    }
}
