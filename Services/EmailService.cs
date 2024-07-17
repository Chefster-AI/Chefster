using System.Net;
using System.Net.Mail;

namespace Chefster.Services;

public class EmailService()
{
    public void SendEmail(string email, string subject, string body)
    {
        // The credentials for the sending email
        var fromEmail = Environment.GetEnvironmentVariable("FROM_EMAIL")!;
        var pwd = Environment.GetEnvironmentVariable("FROM_PASS")!;

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
