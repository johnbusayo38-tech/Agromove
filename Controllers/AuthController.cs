
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.RegularExpressions;
using AgroMove.API.Data;
using AgroMove.API.DTOs.Auth;
using AgroMove.API.Services;
using AgroMove.API.Models;

namespace AgroMove.API.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly AgroMoveDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public AuthController(
            IAuthService authService, 
            AgroMoveDbContext context, 
            IEmailService emailService,
            IConfiguration configuration)
        {
            _authService = authService;
            _context = context;
            _emailService = emailService;
            _configuration = configuration;
        }

        private Guid CurrentUserId
        {
            get
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var id))
                {
                    throw new UnauthorizedAccessException("Invalid or missing user ID in token");
                }
                return id;
            }
        }

        // POST: api/auth/register
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
        {
            if (await _context.Users.AnyAsync(u => u.Phone == request.Phone || u.Email == request.Email))
            {
                return BadRequest(new { message = "Phone or email already registered" });
            }

            var user = new User
            {
                Name = request.Name,
                Email = request.Email,
                Phone = request.Phone,
                Role = request.Role ?? "SENDER",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                CreatedAt = DateTime.UtcNow,
                IsVerified = false,
                VerificationToken = Guid.NewGuid().ToString("N"),
                VerificationTokenExpires = DateTime.UtcNow.AddHours(24)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Create wallet for new user
            var wallet = new Wallet
            {
                UserId = user.Id,
                Balance = 50000.00m,
                CreatedAt = DateTime.UtcNow
            };
            _context.Wallets.Add(wallet);
            await _context.SaveChangesAsync();

            // Send verification email
            var verificationLink = $"{_configuration["App:FrontendUrl"]}/verify-email?token={user.VerificationToken}";
            var htmlMessage = $@"
                <h2>Welcome to AgroMove, {user.Name}!</h2>
                <p>Thank you for registering. Please verify your email address by clicking the link below:</p>
                <p><a href='{verificationLink}' style='padding:10px 20px; background:#166534; color:white; text-decoration:none; border-radius:8px;'>Verify Email Address</a></p>
                <p>This link expires in 24 hours.</p>
                <p>If you didn't create an account, please ignore this email.</p>
            ";

            try
            {
                await _emailService.SendEmailAsync(user.Email!, "Verify Your AgroMove Account", htmlMessage);
            }
            catch (Exception ex)
            {
                // Log but don't fail registration
                Console.WriteLine($"Verification email failed: {ex.Message}");
            }

            var token = _authService.GenerateJwtToken(user);

            return Ok(new AuthResponse
            {
                Token = token,
                User = new UserResponse
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email ?? string.Empty,
                    Phone = user.Phone,
                    Role = user.Role,
                    WalletBalance = wallet.Balance,
                    IsVerified = user.IsVerified
                },
                Message = "Account created successfully. Please check your email to verify your account."
            });
        }

        // GET: api/auth/verify-email
        [HttpGet("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return BadRequest(new { message = "Verification token is required" });
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.VerificationToken == token && u.VerificationTokenExpires > DateTime.UtcNow);

            if (user == null)
            {
                return BadRequest(new { message = "Invalid or expired verification token" });
            }

            user.IsVerified = true;
            user.VerificationToken = null;
            user.VerificationTokenExpires = null;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Email verified successfully. You can now log in." });
        }

        // POST: api/auth/resend-verification
        [HttpPost("resend-verification")]
        public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email && !u.IsVerified);

            if (user == null)
            {
                return BadRequest(new { message = "Email not found or already verified" });
            }

            user.VerificationToken = Guid.NewGuid().ToString("N");
            user.VerificationTokenExpires = DateTime.UtcNow.AddHours(24);
            await _context.SaveChangesAsync();

            var verificationLink = $"{_configuration["App:FrontendUrl"]}/verify-email?token={user.VerificationToken}";
            var htmlMessage = $@"
                <h2>Resend: Verify Your AgroMove Account</h2>
                <p>Click the link below to verify your email:</p>
                <p><a href='{verificationLink}' style='padding:10px 20px; background:#166534; color:white; text-decoration:none; border-radius:8px;'>Verify Email</a></p>
                <p>Link expires in 24 hours.</p>
            ";

            try
            {
                await _emailService.SendEmailAsync(user.Email!, "Verify Your AgroMove Account", htmlMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Resend email failed: {ex.Message}");
            }

            return Ok(new { message = "Verification email resent successfully" });
        }

        // POST: api/auth/login
       [HttpPost("login")]
public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
{
    // Basic validation (optional but recommended for immediate feedback)
    if (string.IsNullOrWhiteSpace(request.Email))
        return BadRequest(new { message = "Email is required." });

    if (string.IsNullOrWhiteSpace(request.Password))
        return BadRequest(new { message = "Password is required." });

    try
    {
        // Delegate to the AuthService which now enforces email-only login
        var response = await _authService.LoginAsync(request);

        return Ok(response);
    }
    catch (Exception ex)
    {
        // All login failures (invalid creds, etc.) return 401 Unauthorized for security
        // (Don't reveal whether email exists or password is wrong)
        return Unauthorized(new { message = ex.Message });
    }
}

        // GET: api/auth/profile
        [HttpGet("profile")]
        [Authorize]
        public async Task<ActionResult<UserResponse>> GetProfile()
        {
            var user = await _context.Users
                .Include(u => u.Wallet)
                .FirstOrDefaultAsync(u => u.Id == CurrentUserId);

            if (user == null) return NotFound(new { message = "User not found" });

            return Ok(new UserResponse
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email ?? string.Empty,
                Phone = user.Phone,
                Role = user.Role,
                Location = user.Location ?? string.Empty,
                WalletBalance = user.Wallet?.Balance ?? 0,
                IsVerified = user.IsVerified
            });
        }

        // PUT: api/auth/profile
        [HttpPut("profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            var user = await _context.Users.FindAsync(CurrentUserId);
            if (user == null) return NotFound(new { message = "User not found" });

            if (!string.IsNullOrEmpty(request.Name)) user.Name = request.Name;

            if (!string.IsNullOrEmpty(request.Email))
            {
                if (await _context.Users.AnyAsync(u => u.Email == request.Email && u.Id != CurrentUserId))
                    return BadRequest(new { message = "Email already in use" });

                user.Email = request.Email;
                // Require re-verification on email change
                user.IsVerified = false;
                user.VerificationToken = Guid.NewGuid().ToString("N");
                user.VerificationTokenExpires = DateTime.UtcNow.AddHours(24);

                // Send new verification email
                var verificationLink = $"{_configuration["App:FrontendUrl"]}/verify-email?token={user.VerificationToken}";
                var htmlMessage = $@"
                    <h2>Email Changed - Verify New Address</h2>
                    <p>Please verify your new email by clicking below:</p>
                    <p><a href='{verificationLink}' style='padding:10px 20px; background:#166534; color:white; text-decoration:none; border-radius:8px;'>Verify New Email</a></p>
                    <p>Expires in 24 hours.</p>
                ";

                try
                {
                    await _emailService.SendEmailAsync(user.Email, "Verify Your New Email", htmlMessage);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Email change verification failed: {ex.Message}");
                }
            }

            if (!string.IsNullOrEmpty(request.Phone))
            {
                if (await _context.Users.AnyAsync(u => u.Phone == request.Phone && u.Id != CurrentUserId))
                    return BadRequest(new { message = "Phone already in use" });
                user.Phone = request.Phone;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Profile updated successfully. If email changed, please verify the new email." });
        }

        // POST: api/auth/change-password
        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(request.OldPassword))
                errors.Add("Old password is required");

            if (string.IsNullOrWhiteSpace(request.NewPassword))
                errors.Add("New password is required");
            else
            {
                if (request.NewPassword.Length < 8)
                    errors.Add("New password must be at least 8 characters");

                if (!Regex.IsMatch(request.NewPassword, @"[A-Z]"))
                    errors.Add("New password must contain at least one uppercase letter");

                if (!Regex.IsMatch(request.NewPassword, @"[a-z]"))
                    errors.Add("New password must contain at least one lowercase letter");

                if (!Regex.IsMatch(request.NewPassword, @"[0-9]"))
                    errors.Add("New password must contain at least one number");

                if (!Regex.IsMatch(request.NewPassword, @"[!@#$%^&*()_+=\[{\]};:<>|./?,-]"))
                    errors.Add("New password must contain at least one special character");

                if (request.NewPassword == request.OldPassword)
                    errors.Add("New password cannot be the same as old password");
            }

            if (request.NewPassword != request.ConfirmNewPassword)
                errors.Add("New passwords do not match");

            if (errors.Any())
            {
                return BadRequest(new { message = "Password validation failed", errors });
            }

            var user = await _context.Users.FindAsync(CurrentUserId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash))
            {
                return BadRequest(new { message = "Old password is incorrect" });
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Password changed successfully" });
        }

        // POST: api/auth/device-token
        [HttpPost("device-token")]
        [Authorize]
        public async Task<IActionResult> SaveDeviceToken([FromBody] SaveDeviceTokenRequest request)
        {
            var user = await _context.Users.FindAsync(CurrentUserId);
            if (user == null) return NotFound(new { message = "User not found" });

            user.DeviceToken = request.DeviceToken;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Device token saved successfully" });
        }

        // POST: api/auth/logout
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var user = await _context.Users.FindAsync(CurrentUserId);
            if (user == null) return NotFound();

            user.DeviceToken = null;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Logged out successfully" });
        }
    }

    // DTOs
    public class UpdateProfileRequest
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
    }

    public class SaveDeviceTokenRequest
    {
        public string DeviceToken { get; set; } = string.Empty;
    }

    public class ChangePasswordRequest
    {
        public string OldPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }

    public class ResendVerificationRequest
    {
        public string Email { get; set; } = string.Empty;
    }
}