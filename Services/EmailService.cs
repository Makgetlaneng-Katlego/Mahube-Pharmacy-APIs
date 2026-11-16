using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace WebApplication1.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendOrderConfirmationAsync(string toEmail, string customerName, int orderId, decimal totalAmount, string itemsHtml)
    {
        var email = new MimeMessage();

        var senderName = _config["EmailSettings:SenderName"] ?? "Mahube Pharmacy";
        var senderEmail = _config["EmailSettings:SenderEmail"];
        var password = _config["EmailSettings:Password"];
        var smtpServer = _config["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
        var smtpPort = int.Parse(_config["EmailSettings:SmtpPort"] ?? "587");

        email.From.Add(new MailboxAddress(senderName, senderEmail));
        email.To.Add(new MailboxAddress(customerName, toEmail));
        email.Subject = $"Order Confirmation #{orderId} - Mahube Pharmacy";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #2c3e50;'>Thank you for your order, {customerName}!</h2>
                    <p>Your order <strong>#{orderId}</strong> has been placed successfully and is currently being processed.</p>
                    
                    <h3 style='border-bottom: 2px solid #eee; padding-bottom: 10px;'>Order Summary</h3>
                    <table style='width: 100%; text-align: left; margin-bottom: 20px; border-collapse: collapse;'>
                        <thead>
                            <tr>
                                <th style='padding: 8px; border-bottom: 1px solid #ddd;'>Product</th>
                                <th style='padding: 8px; border-bottom: 1px solid #ddd;'>Qty</th>
                                <th style='padding: 8px; border-bottom: 1px solid #ddd;'>Price</th>
                            </tr>
                        </thead>
                        <tbody>
                            {itemsHtml}
                        </tbody>
                    </table>

                    <h3 style='text-align: right; color: #27ae60;'>Total: R {totalAmount:F2}</h3>
                    <p style='font-size: 12px; color: #7f8c8d;'>Payment Method: Cash on Delivery</p>
                    
                    <p>If you have any questions, reply to this email.</p>
                    <p>Regards,<br/><strong>Mahube Pharmacy Team</strong></p>
                </div>"
        };

        email.Body = bodyBuilder.ToMessageBody();

        using var smtp = new SmtpClient();
        try
        {
            await smtp.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(senderEmail, password);
            await smtp.SendAsync(email);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send email: {ex.Message}");
        }
        finally
        {
            await smtp.DisconnectAsync(true);
        }
    }
}