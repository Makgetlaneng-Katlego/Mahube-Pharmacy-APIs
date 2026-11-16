using System.Security.Claims;
using Mahube_Pharmacy.Data;
using Mahube_Pharmacy.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApplication1.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class AddressController : ControllerBase
{
    private readonly AppDbContext _db;
    public AddressController(AppDbContext db) => _db = db;

    private int CurrentUserId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id : throw new InvalidOperationException("User id claim missing.");

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Address model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        model.UserId = CurrentUserId;
        model.User = null;
        _db.Addresses.Add(model);
        await _db.SaveChangesAsync();

        return Ok(new { Message = "Address created successfully", AddressId = model.AddressId });
    }
}