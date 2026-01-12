// using Microsoft.EntityFrameworkCore;
// using Microsoft.IdentityModel.Tokens;
// using System.IdentityModel.Tokens.Jwt;
// using System.Security.Claims;
// using System.Text;
// using AgroMove.API.Data;
// using AgroMove.API.Models;
// using AgroMove.API.DTOs.Auth;
// using BCrypt.Net; // BCrypt.Net-Next package installed

// namespace AgroMove.API.Services
// {
//     public class AuthService : IAuthService
//     {
//         private readonly AgroMoveDbContext _context;
//         private readonly IConfiguration _config;

//         public AuthService(AgroMoveDbContext context, IConfiguration config)
//         {
//             _context = context;
//             _config = config;
//         }

//         public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
//         {
//             // Validate and normalize input
//             if (string.IsNullOrWhiteSpace(request.Email))
//                 throw new Exception("Email is required.");

//             var normalizedEmail = request.Email.Trim().ToLowerInvariant();

//             if (await _context.Users.AnyAsync(u => u.Email == normalizedEmail))
//                 throw new Exception("Email is already registered.");

//             if (string.IsNullOrWhiteSpace(request.Name))
//                 throw new Exception("Name is required.");

//             if (string.IsNullOrWhiteSpace(request.Password))
//                 throw new Exception("Password is required.");

//             // Create new user
//             var user = new User
//             {
//                 Name = request.Name.Trim(),
//                 Email = normalizedEmail,
//                 Phone = NormalizePhone(request.Phone), // Phone is optional
//                 Role = ValidateAndNormalizeRole(request.Role),
//                 PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
//                 CreatedAt = DateTime.UtcNow
//             };

//             // Create wallet with default ₦50,000 balance
//             user.Wallet = new Wallet
//             {
//                 Balance = 50000.00m
//             };

//             _context.Users.Add(user);
//             await _context.SaveChangesAsync();

//             var token = GenerateToken(user);

//             return new AuthResponse
//             {
//                 Token = token,
//                 User = new UserResponse
//                 {
//                     Id = user.Id,
//                     Name = user.Name,
//                     Phone = user.Phone ?? string.Empty,
//                     Email = user.Email,
//                     Role = user.Role,
//                     WalletBalance = user.Wallet.Balance
//                 }
//             };
//         }

//         public async Task<AuthResponse> LoginAsync(string email, string password)
//         {
//             if (string.IsNullOrWhiteSpace(email))
//                 throw new Exception("Email is required.");

//             var normalizedEmail = email.Trim().ToLowerInvariant();

//             var user = await _context.Users
//                 .Include(u => u.Wallet)
//                 .FirstOrDefaultAsync(u => u.Email == normalizedEmail);

//             if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
//                 throw new Exception("Invalid email or password.");

//             var token = GenerateToken(user!); // Non-null assertion: user cannot be null here

//             return new AuthResponse
//             {
//                 Token = token,
//                 User = new UserResponse
//                 {
//                     Id = user.Id,
//                     Name = user.Name,
//                     Phone = user.Phone ?? string.Empty,
//                     Email = user.Email,
//                     Role = user.Role,
//                     WalletBalance = user.Wallet?.Balance ?? 0m
//                 }
//             };
//         }

//         private string GenerateToken(User user)
//         {
//             var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
//             var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

//             var claims = new List<Claim>
//             {
//                 new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
//                 new Claim(ClaimTypes.Name, user.Name),
//                 new Claim(ClaimTypes.Role, user.Role),
//                 new Claim(ClaimTypes.Email, user.Email)
//             };

//             if (!string.IsNullOrEmpty(user.Phone))
//                 claims.Add(new Claim(ClaimTypes.MobilePhone, user.Phone));

//             var token = new JwtSecurityToken(
//                 issuer: _config["Jwt:Issuer"],
//                 audience: _config["Jwt:Audience"],
//                 claims: claims,
//                 expires: DateTime.UtcNow.AddDays(7),
//                 signingCredentials: creds
//             );

//             return new JwtSecurityTokenHandler().WriteToken(token);
//         }

//         private string NormalizePhone(string phone)
//         {
//             if (string.IsNullOrWhiteSpace(phone))
//                 return string.Empty;

//             var cleaned = phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");

//             if (cleaned.StartsWith("0") && cleaned.Length == 11)
//                 cleaned = "+234" + cleaned.Substring(1);
//             else if (cleaned.StartsWith("234") && cleaned.Length == 13)
//                 cleaned = "+" + cleaned;
//             else if (!cleaned.StartsWith("+234"))
//                 cleaned = "+234" + cleaned.Substring(cleaned.StartsWith("+") ? 1 : 0);

//             return cleaned;
//         }

//         private string ValidateAndNormalizeRole(string? role)
//         {
//             if (string.IsNullOrWhiteSpace(role))
//                 return "SENDER";

//             var upperRole = role.Trim().ToUpperInvariant();
//             return upperRole == "DRIVER" ? "DRIVER" : "SENDER";
//         }
//     }
// }


using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AgroMove.API.Data;
using AgroMove.API.Models;
using AgroMove.API.DTOs.Auth;
using BCrypt.Net;

namespace AgroMove.API.Services
{
    public class AuthService : IAuthService
    {
        private readonly AgroMoveDbContext _context;
        private readonly IConfiguration _config;

        public AuthService(AgroMoveDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public string GenerateJwtToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Role, user.Role)
            };

            if (!string.IsNullOrEmpty(user.Email))
                claims.Add(new Claim(ClaimTypes.Email, user.Email));

            if (!string.IsNullOrEmpty(user.Phone))
                claims.Add(new Claim(ClaimTypes.MobilePhone, user.Phone));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            // 1. Validation
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new Exception("Name is required.");

            if (string.IsNullOrWhiteSpace(request.Email))
                throw new Exception("Email is required.");

            if (string.IsNullOrWhiteSpace(request.Phone))
                throw new Exception("Phone is required.");

            if (string.IsNullOrWhiteSpace(request.Password))
                throw new Exception("Password is required.");

            var normalizedEmail = request.Email.Trim().ToLowerInvariant();

            // Check for existing email (always required now)
            if (await _context.Users.AnyAsync(u => u.Email == normalizedEmail))
                throw new Exception("Email already registered.");

            var normalizedPhone = NormalizePhone(request.Phone);

            // Check for existing phone
            if (await _context.Users.AnyAsync(u => u.Phone == normalizedPhone))
                throw new Exception("Phone number already registered.");

            // 2. Create User Entity
            var user = new User
            {
                Id = Guid.NewGuid(),
                Name = request.Name.Trim(),
                Email = normalizedEmail, // Email is now required and always stored in lowercase
                Phone = normalizedPhone,
                // Forced to SENDER as per requirements (ignores any role sent in request)
                Role = "SENDER",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                CreatedAt = DateTime.UtcNow,
                IsVerified = true, // Immediate access – change to false if you implement email verification later
                VerificationToken = Guid.NewGuid().ToString("N"),
                VerificationTokenExpires = DateTime.UtcNow.AddHours(24)
            };

            // 3. Create BRAND NEW Wallet for this user
            var wallet = new Wallet
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Balance = 0.00m, // Start at zero (or add a welcome bonus if desired)
                CreatedAt = DateTime.UtcNow
            };

            user.Wallet = wallet;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(user);

            return new AuthResponse
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
                    Location = user.Location ?? string.Empty,
                    IsVerified = user.IsVerified
                }
            };
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            // 1. Validate that Email is provided (phone is completely ignored for login)
            if (string.IsNullOrWhiteSpace(request.Email))
                throw new Exception("Email is required for login.");

            if (string.IsNullOrWhiteSpace(request.Password))
                throw new Exception("Password is required.");

            // 2. Normalize and find the user by email only
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            
            var user = await _context.Users
                .Include(u => u.Wallet)
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail);

            // 3. Verify user exists and password matches
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                throw new Exception("Invalid email or password.");
            }

            // 4. Update last login time
            user.LastLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // 5. Generate Token and Return Response
            var token = GenerateJwtToken(user);

            return new AuthResponse
            {
                Token = token,
                User = new UserResponse
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email ?? string.Empty,
                    Phone = user.Phone,
                    Role = user.Role,
                    WalletBalance = user.Wallet?.Balance ?? 0m,
                    Location = user.Location ?? string.Empty,
                    IsVerified = user.IsVerified
                }
            };
        }

        private string NormalizePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return string.Empty;

            var cleaned = new string(phone.Where(char.IsDigit).ToArray());

            if (cleaned.StartsWith("0") && cleaned.Length == 11)
                cleaned = "234" + cleaned.Substring(1);
            else if (cleaned.Length == 10)
                cleaned = "234" + cleaned;

            return $"+{cleaned}";
        }
    }
}