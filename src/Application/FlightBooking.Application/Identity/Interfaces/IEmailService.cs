namespace FlightBooking.Application.Identity.Interfaces;

public interface IEmailService
{
    Task SendEmailVerificationAsync(string email, string firstName, string verificationToken);
    Task SendPasswordResetAsync(string email, string firstName, string resetToken);
    Task SendWelcomeEmailAsync(string email, string firstName);
    Task SendPasswordChangedNotificationAsync(string email, string firstName);

    // Generic email sending method for background jobs
    Task SendEmailAsync(string to, string subject, string body, bool isHtml = true, string? from = null);

    // Booking-related email methods
    Task SendBookingConfirmationAsync(string email, string customerName, string bookingReference,
        string flightDetails, decimal totalAmount, string currency = "USD");
    Task SendBookingCancellationAsync(string email, string customerName, string bookingReference,
        string reason, decimal? refundAmount = null, string currency = "USD");
    Task SendBookingReminderAsync(string email, string customerName, string bookingReference,
        string flightDetails, DateTime departureTime);
}
