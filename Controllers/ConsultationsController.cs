using Mahube_Pharmacy.Data;
using Mahube_Pharmacy.Models;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ConsultationsController : ControllerBase
{
    private readonly AppDbContext _db;
    public ConsultationsController(AppDbContext db) => _db = db;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Consultation model)
    {
        if (string.IsNullOrWhiteSpace(model.Fullname) || string.IsNullOrWhiteSpace(model.Email) ||
            string.IsNullOrWhiteSpace(model.PhoneNumber) || string.IsNullOrWhiteSpace(model.ConsultationType) ||
            string.IsNullOrWhiteSpace(model.Description))
        {
            return BadRequest(new { Message = "Please fill in all required fields." });
        }

        if (!new EmailAddressAttribute().IsValid(model.Email))
            return BadRequest(new { Message = "Please enter a valid email address." });

        model.Response = "";
        _db.Consultations.Add(model);
        await _db.SaveChangesAsync();

        return Ok(new { Message = "Your consultation has been submitted successfully!", ConsultationId = model.ConsultationId });
    }
}