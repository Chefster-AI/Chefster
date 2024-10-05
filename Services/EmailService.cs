using System.Net;
using System.Net.Mail;
using Chefster.Common;

namespace Chefster.Services;

public class EmailService(IConfiguration configuration, LoggingService loggingService)
{
    private readonly IConfiguration _configuration = configuration;
    private readonly LoggingService _logger = loggingService;

    public void SendEmail(string email, string subject, string body)
    {
        // The credentials for the sending email
        var fromEmail = _configuration["FROM_EMAIL"]!;
        var pwd = _configuration["FROM_PASS"]!;

        var client = new SmtpClient
        {
            Port = 587,
            Host = "smtp.gmail.com",
            EnableSsl = true,
            Credentials = new NetworkCredential(fromEmail, pwd)
        };

        var message = new MailMessage(fromEmail, email)
        {
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };

        try
        {
            client.Send(message);
            _logger.Log(
                $"Successfully sent email. Subject: {subject}",
                LogLevels.Info,
                "EmailService"
            );
        }
        catch (Exception e)
        {
            _logger.Log($"Failed to send email with error {e}", LogLevels.Error, "EmailService");
        }
    }
}
