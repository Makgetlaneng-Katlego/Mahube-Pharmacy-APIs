using Mahube_Pharmacy.Data;
using Mahube_Pharmacy.Models;
using Microsoft.AspNetCore.Mvc;

namespace WebApplication1.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PrescriptionsController : ControllerBase
{
    private readonly AppDbContext _db;
    private const long MaxBytes = 5L * 1024 * 1024;

    public PrescriptionsController(AppDbContext db) => _db = db;

    [HttpPost]
    [RequestSizeLimit(MaxBytes + 1024 * 1024)]
    public async Task<IActionResult> Create([FromForm] Prescription model, IFormFile? prescription_file)
    {
        if (string.IsNullOrWhiteSpace(model.UserEmail) || string.IsNullOrWhiteSpace(model.DoctorName) || model.DatePrescribed == default || string.IsNullOrWhiteSpace(model.Type))
            return BadRequest(new { Message = "Please fill in all required fields." });

        if (prescription_file == null || prescription_file.Length == 0)
            return BadRequest(new { Message = "Please upload a prescription image or PDF." });

        if (prescription_file.Length > MaxBytes)
            return BadRequest(new { Message = "File size exceeds 5MB limit." });

        using (var ms = new MemoryStream())
        {
            await prescription_file.CopyToAsync(ms);
            model.FileData = ms.ToArray();
        }

        model.Response = "";
        _db.Prescriptions.Add(model);
        await _db.SaveChangesAsync();

        return Ok(new { Message = "Prescription uploaded successfully!", PrescriptionId = model.Id });
    }
}