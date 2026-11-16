using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Mahube_Pharmacy.Data;
using Mahube_Pharmacy.Helpers;
using Mahube_Pharmacy.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace WebApplication1.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    private static string Normalize(string s) => s.Trim().ToLowerInvariant();

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Identifier) || string.IsNullOrEmpty(request.Password))
            return BadRequest(new { Message = "Email/Phone and password are required." });

        var identifier = request.Identifier.Trim();
        User? user;

        if (identifier.Contains('@'))
        {
            var email = Normalize(identifier);
            user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        }
        else
        {
            if (!PhoneHelper.TryNormalize(identifier, out var normalizedPhone))
                return BadRequest(new { Message = "Invalid phone format." });
            user = await _db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == normalizedPhone);
        }

        if (user != null)
        {
            var (ok, needsSave) = VerifyPassword(user, request.Password);
            if (ok)
            {
                if (needsSave) await _db.SaveChangesAsync();

                var token = GenerateJwtToken(user);
                return Ok(new { Token = token, User = new { user.UserId, user.FullName, user.Email, user.Role } });
            }
        }

        return Unauthorized(new { Message = "That email, phone number, or password didn't match." });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var email = Normalize(request.Email);

        if (!EmailHelper.IsValid(email) || !PhoneHelper.TryNormalize(request.Phone, out var normalizedPhone))
            return BadRequest(new { Message = "The email or phone number is incorrectly formatted." });

        if (!PasswordHelper.IsStrong(request.Password))
            return BadRequest(new { Message = "Password must be at least 8 characters and include a number and a special character." });

        if (await _db.Users.AnyAsync(u => u.Email == email))
            return BadRequest(new { Message = "Email already registered." });

        if (await _db.Users.AnyAsync(u => u.PhoneNumber == normalizedPhone))
            return BadRequest(new { Message = "That phone number is already registered." });

        var newUser = new User
        {
            FullName = request.Fullname.Trim(),
            Email = email,
            PhoneNumber = normalizedPhone,
            Password = "",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = "CUSTOMER"
        };

        _db.Users.Add(newUser);
        await _db.SaveChangesAsync();

        return Ok(new { Message = "Registration successful. Please login." });
    }

    private string GenerateJwtToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_config["Jwt:Key"]!);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("FullName", user.FullName),
                new Claim(ClaimTypes.Role, user.Role)
            }),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private static (bool Success, bool NeedsSave) VerifyPassword(User user, string password)
    {
        if (LooksLikeBcrypt(user.PasswordHash))
        {
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash!)) return (false, false);
            if (!string.IsNullOrEmpty(user.Password) && !LooksLikeBcrypt(user.Password))
            {
                user.Password = "";
                return (true, true);
            }
            return (true, false);
        }

        if (LooksLikeBcrypt(user.Password))
        {
            if (!BCrypt.Net.BCrypt.Verify(password, user.Password!)) return (false, false);
            user.PasswordHash = user.Password;
            user.Password = "";
            return (true, true);
        }

        if (!string.IsNullOrEmpty(user.Password) && user.Password == password)
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            user.Password = "";
            return (true, true);
        }

        return (false, false);
    }

    private static bool LooksLikeBcrypt(string? value) =>
        value is not null && value.Length >= 7 && value[0] == '$' && value[1] == '2' &&
        (value[2] == 'a' || value[2] == 'b' || value[2] == 'y' || value[2] == 'x') && value[3] == '$';
}

public class LoginRequest { public string Identifier { get; set; } = ""; public string Password { get; set; } = ""; }
public class RegisterRequest { public string Fullname { get; set; } = ""; public string Email { get; set; } = ""; public string Phone { get; set; } = ""; public string Password { get; set; } = ""; }