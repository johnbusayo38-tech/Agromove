
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AgroMove.API.Data;
using AgroMove.API.Models;
using AgroMove.API.DTOs.Admin;
using BCrypt.Net; // Ensure package BCrypt.Net-Next is installed

namespace AgroMove.API.Controllers
{
    [Route("api/admin")]
    [ApiController]
    public class AdminAuthController : ControllerBase
    {
        private readonly AgroMoveDbContext _context;
        private readonly IConfiguration _configuration;

        public AdminAuthController(AgroMoveDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // POST: api/admin/login
        [HttpPost("login")]
        public async Task<ActionResult<AdminLoginResponse>> Login([FromBody] AdminLoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "Email and password are required" });
            }

            var adminUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email && (u.Role == "ADMIN" || u.Role == "SUPERADMIN"));

            if (adminUser == null || !BCrypt.Net.BCrypt.Verify(request.Password, adminUser.PasswordHash))
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            // Update last login
            adminUser.LastLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Generate JWT
            var token = GenerateJwtToken(adminUser);

            return Ok(new AdminLoginResponse
            {
                Token = token,
                User = new AdminUserResponse
                {
                    Id = adminUser.Id,
                    Name = adminUser.Name,
                    Email = adminUser.Email ?? string.Empty,
                    Role = adminUser.Role
                }
            });
        }

        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured")));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}