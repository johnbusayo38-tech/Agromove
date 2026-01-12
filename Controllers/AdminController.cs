
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AgroMove.API.Data;
using AgroMove.API.Models;
using AgroMove.API.DTOs.Admin;

namespace AgroMove.API.Controllers
{
    [Route("api/admin")]
    [ApiController]
    [Authorize(Roles = "ADMIN,SUPERADMIN")]
    public class AdminController : ControllerBase
    {
        private readonly AgroMoveDbContext _context;

        public AdminController(AgroMoveDbContext context)
        {
            _context = context;
        }

        private bool IsSuperAdmin => User.IsInRole("SUPERADMIN");

        // GET: api/admin/metrics
        [HttpGet("metrics")]
        public async Task<ActionResult<AdminMetricsResponse>> GetMetrics()
        {
            var totalUsers = await _context.Users.CountAsync();
            var totalSenders = await _context.Users.Where(u => u.Role == "SENDER").CountAsync();
            var totalDrivers = await _context.Users.Where(u => u.Role == "DRIVER").CountAsync();
            var totalOrders = await _context.Orders.CountAsync();
            var pendingOrders = await _context.Orders.Where(o => o.Status == OrderStatus.Pending).CountAsync();
            var totalRevenue = await _context.WalletTransactions
                .Where(t => t.Type == "DEBIT" && t.Description.Contains("Order"))
                .SumAsync(t => t.Amount);

            return Ok(new AdminMetricsResponse
            {
                TotalUsers = totalUsers,
                TotalSenders = totalSenders,
                TotalDrivers = totalDrivers,
                TotalOrders = totalOrders,
                PendingOrders = pendingOrders,
                TotalRevenue = totalRevenue,
                ActiveUsersToday = await _context.Users.Where(u => u.LastLogin >= DateTime.UtcNow.Date).CountAsync()
            });
        }

        // GET: api/admin/users
        [HttpGet("users")]
        public async Task<ActionResult<PaginatedResponse<AdminUserResponse>>> GetUsers(
            [FromQuery] int page = 1,
            [FromQuery] int size = 20,
            [FromQuery] string? role = null,
            [FromQuery] string? search = null)
        {
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(role))
            {
                query = query.Where(u => u.Role == role.ToUpper());
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u => u.Name.Contains(search) || u.Phone.Contains(search) || (u.Email != null && u.Email.Contains(search)));
            }

            var total = await query.CountAsync();

            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * size)
                .Take(size)
                .Select(u => new AdminUserResponse
                {
                    Id = u.Id,
                    Name = u.Name,
                    Phone = u.Phone,
                    Email = u.Email ?? string.Empty,
                    Role = u.Role,
                    WalletBalance = u.Wallet.Balance,
                    OrderCount = u.OrdersAsShipper.Count,
                    CreatedAt = u.CreatedAt,
                    IsVerified = u.IsVerified,
                    LastLogin = u.LastLogin
                })
                .ToListAsync();

            return Ok(new PaginatedResponse<AdminUserResponse>
            {
                Data = users,
                Total = total,
                Page = page,
                Size = size
            });
        }

        // GET: api/admin/users/{id}
        [HttpGet("users/{id}")]
        public async Task<ActionResult<AdminUserDetailResponse>> GetUser(Guid id)
        {
            var user = await _context.Users
                .Include(u => u.Wallet)
                .Include(u => u.OrdersAsShipper)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(new AdminUserDetailResponse
            {
                Id = user.Id,
                Name = user.Name,
                Phone = user.Phone,
                Email = user.Email ?? string.Empty,
                Role = user.Role,
                Location = user.Location ?? string.Empty,
                WalletBalance = user.Wallet?.Balance ?? 0,
                OrderCount = user.OrdersAsShipper.Count,
                CreatedAt = user.CreatedAt,
                IsVerified = user.IsVerified,
                LastLogin = user.LastLogin
            });
        }

        // SUPERADMIN ONLY

        [HttpPost("users/{id}/ban")]
        [Authorize(Roles = "SUPERADMIN")]
        public async Task<IActionResult> BanUser(Guid id, [FromBody] BanUserRequest request)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.IsBanned = true;
            user.BanReason = request.Reason;

            await _context.SaveChangesAsync();

            return Ok(new { message = "User banned" });
        }

        [HttpPost("wallets/payout")]
        [Authorize(Roles = "SUPERADMIN")]
        public async Task<IActionResult> ApprovePayout([FromBody] PayoutRequest request)
        {
            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == request.UserId);
            if (wallet == null || wallet.Balance < request.Amount)
            {
                return BadRequest(new { message = "Insufficient balance" });
            }

            wallet.Balance -= request.Amount;

            var transaction = new WalletTransaction
            {
                WalletId = wallet.Id,
                Amount = request.Amount,
                Type = "DEBIT",
                Description = "Driver payout",
                Status = "SUCCESS"
            };

            _context.WalletTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Payout approved" });
        }

        [HttpPost("admins/create")]
        [Authorize(Roles = "SUPERADMIN")]
        public async Task<IActionResult> CreateAdmin([FromBody] CreateAdminRequest request)
        {
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return BadRequest(new { message = "Email exists" });
            }

            var adminUser = new User
            {
                Name = request.Name,
                Email = request.Email,
                Phone = request.Phone,
                Role = request.IsSuperAdmin ? "SUPERADMIN" : "ADMIN",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(adminUser);
            await _context.SaveChangesAsync();

            var wallet = new Wallet { UserId = adminUser.Id };
            _context.Wallets.Add(wallet);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Admin created" });
        }
    }
}