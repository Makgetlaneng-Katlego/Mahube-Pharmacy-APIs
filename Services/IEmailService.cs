namespace WebApplication1.Services;

public interface IEmailService
{
    Task SendOrderConfirmationAsync(string toEmail, string customerName, int orderId, decimal totalAmount, string itemsHtml);
}