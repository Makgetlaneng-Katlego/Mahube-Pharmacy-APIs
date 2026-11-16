using Mahube_Pharmacy.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebApplication1.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProductController : ControllerBase
{
    private readonly AppDbContext _db;
    public ProductController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] string? q)
    {
        var query = _db.Products.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(p => p.ProductName.Contains(q));

        return Ok(await query.OrderBy(p => p.ProductName).ToListAsync());
    }
}