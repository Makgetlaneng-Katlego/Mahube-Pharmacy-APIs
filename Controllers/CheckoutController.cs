using Mahube_Pharmacy.Data;
using Mahube_Pharmacy.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using WebApplication1.Services;

namespace WebApplication1.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class CheckoutController : ControllerBase
{
    private const decimal ServiceFee = 20.00m;
    private readonly AppDbContext _db;
    private readonly IEmailService _emailService;

    public CheckoutController(AppDbContext db, IEmailService emailService)
    {
        _db = db;
        _emailService = emailService;
    }

    private int CurrentUserId
    {
        get
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(id, out var userId)) throw new InvalidOperationException("User id claim missing.");
            return userId;
        }
    }

    [HttpPost("PlaceOrder")]
    public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderRequest request)
    {
        var userId = CurrentUserId;

        if (request.Items == null || !request.Items.Any())
            return BadRequest(new { Message = "Your cart is empty." });

        var address = await _db.Addresses.FirstOrDefaultAsync(a => a.AddressId == request.SelectedAddressId && a.UserId == userId);
        if (address is null) return BadRequest(new { Message = "Invalid delivery address." });

        var productIds = request.Items.Select(i => i.ProductId).ToList();
        var products = await _db.Products.Where(p => productIds.Contains(p.ProductId)).ToDictionaryAsync(p => p.ProductId);

        foreach (var item in request.Items)
        {
            if (!products.TryGetValue(item.ProductId, out var p) || (p.StockQuantity ?? 0) < item.Quantity)
                return BadRequest(new { Message = $"Insufficient stock for {(p?.ProductName ?? "an item")}." });
        }

        // FIX: Explicitly cast the nullable double? to decimal
        var subtotal = request.Items.Sum(item => (decimal)(products[item.ProductId].Price ?? 0) * item.Quantity);
        var total = subtotal + ServiceFee;

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var order = new Order
            {
                CustomerId = userId,
                TotalAmount = total,
                Status = "Pending",
                PaymentMethod = "Cash on Delivery",
                PaymentStatus = "PENDING",
                OrderDate = DateTime.UtcNow,
                AddressId = address.AddressId
            };

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            var itemsHtmlBuilder = new StringBuilder();

            foreach (var item in request.Items)
            {
                var p = products[item.ProductId];

                // FIX: Explicitly cast the nullable double? to decimal
                var priceDecimal = (decimal)(p.Price ?? 0);

                _db.OrderItems.Add(new OrderItem { OrderId = order.OrderId, ProductId = item.ProductId, Quantity = item.Quantity, PriceAtTime = priceDecimal });

                if (p.StockQuantity.HasValue)
                {
                    p.StockQuantity -= item.Quantity;
                }

                itemsHtmlBuilder.Append($@"
                    <tr>
                        <td style='padding: 8px; border-bottom: 1px solid #ddd;'>{p.ProductName}</td>
                        <td style='padding: 8px; border-bottom: 1px solid #ddd;'>{item.Quantity}</td>
                        <td style='padding: 8px; border-bottom: 1px solid #ddd;'>R {priceDecimal:F2}</td>
                    </tr>");
            }

            _db.PaymentLogs.Add(new PaymentLog { OrderId = order.OrderId, ResponseData = "Order placed (Cash on Delivery)", CreatedAt = DateTime.UtcNow });

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            // Send the email in the background
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);
            if (user != null && !string.IsNullOrEmpty(user.Email))
            {
                _ = Task.Run(() => _emailService.SendOrderConfirmationAsync(
                    user.Email,
                    user.FullName,
                    order.OrderId,
                    order.TotalAmount,
                    itemsHtmlBuilder.ToString()
                ));
            }

            return Ok(new { Message = "Order placed successfully", OrderId = order.OrderId });
        }
        catch (DbUpdateConcurrencyException)
        {
            await tx.RollbackAsync();
            return BadRequest(new { Message = "Stock changed while you were checking out. Please try again." });
        }
    }
}

public class PlaceOrderRequest { public int SelectedAddressId { get; set; } public List<CartItemDto> Items { get; set; } = new(); }
public class CartItemDto { public int ProductId { get; set; } public int Quantity { get; set; } }