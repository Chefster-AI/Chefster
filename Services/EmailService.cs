using System.Net;
using System.Net.Mail;

namespace Chefster.Services;

public class EmailService(IConfiguration configuration)
{
    private readonly IConfiguration _configuration = configuration;

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
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error sending email: {e}");
        }
    }
}
