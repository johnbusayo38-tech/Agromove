
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using AgroMove.API.Data;
using AgroMove.API.DTOs.Wallet;
using AgroMove.API.Models;
using Microsoft.EntityFrameworkCore;

namespace AgroMove.API.Controllers
{
    [Route("api/wallet")]
    [ApiController]
    [Authorize]
    public class WalletController : ControllerBase
    {
        private readonly AgroMoveDbContext _context;

        public WalletController(AgroMoveDbContext context)
        {
            _context = context;
        }

        private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // GET: api/wallet — Get full wallet details (balance + recent transactions)
        [HttpGet]
        public async Task<ActionResult<WalletResponse>> GetWallet()
        {
            var wallet = await _context.Wallets
                .Include(w => w.Transactions)
                .FirstOrDefaultAsync(w => w.UserId == CurrentUserId);

            if (wallet == null)
            {
                return NotFound(new { message = "Wallet not found for this user" });
            }

            var response = new WalletResponse
            {
                Id = wallet.Id,
                Balance = wallet.Balance,
                CreatedAt = wallet.CreatedAt,
                Transactions = wallet.Transactions
                    .OrderByDescending(t => t.Timestamp)
                    .Take(50) // Limit recent transactions for performance
                    .Select(t => new TransactionResponse
                    {
                        Id = t.Id,
                        Amount = t.Amount,
                        Type = t.Type,
                        Description = t.Description,
                        Status = t.Status,
                        Timestamp = t.Timestamp
                    })
                    .ToList()
            };

            return Ok(response);
        }

        // GET: api/wallet/balance — Lightweight endpoint for just balance
        [HttpGet("balance")]
        public async Task<ActionResult<object>> GetBalanceOnly()
        {
            var wallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.UserId == CurrentUserId);

            if (wallet == null)
            {
                return NotFound(new { message = "Wallet not found for this user" });
            }

            return Ok(new { balance = wallet.Balance });
        }

        // GET: api/wallet/transactions — Full transaction history
        [HttpGet("transactions")]
        public async Task<ActionResult<List<TransactionResponse>>> GetTransactions()
        {
            var transactions = await _context.WalletTransactions
                .Where(t => t.Wallet.UserId == CurrentUserId)
                .OrderByDescending(t => t.Timestamp)
                .Select(t => new TransactionResponse
                {
                    Id = t.Id,
                    Amount = t.Amount,
                    Type = t.Type,
                    Description = t.Description,
                    Status = t.Status,
                    Timestamp = t.Timestamp
                })
                .ToListAsync();

            return Ok(transactions);
        }

        // POST: api/wallet/fund — Add funds (credit)
        [HttpPost("fund")]
        public async Task<ActionResult<WalletResponse>> FundWallet([FromBody] FundWalletRequest request)
        {
            if (request.Amount <= 0)
            {
                return BadRequest(new { message = "Amount must be greater than zero" });
            }

            var wallet = await _context.Wallets
                .Include(w => w.Transactions)
                .FirstOrDefaultAsync(w => w.UserId == CurrentUserId);

            if (wallet == null)
            {
                return NotFound(new { message = "Wallet not found for this user" });
            }

            wallet.Balance += request.Amount;

            var transaction = new WalletTransaction
            {
                WalletId = wallet.Id,
                Amount = request.Amount,
                Type = "CREDIT",
                Description = request.Method == "BANK_TRANSFER" 
                    ? "Bank Transfer Funding" 
                    : "Card Funding",
                Status = request.Method == "BANK_TRANSFER" ? "PENDING" : "SUCCESS",
                Timestamp = DateTime.UtcNow
            };

            _context.WalletTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            // Return updated wallet
            var response = new WalletResponse
            {
                Id = wallet.Id,
                Balance = wallet.Balance,
                CreatedAt = wallet.CreatedAt,
                Transactions = wallet.Transactions
                    .OrderByDescending(t => t.Timestamp)
                    .Take(50)
                    .Select(t => new TransactionResponse
                    {
                        Id = t.Id,
                        Amount = t.Amount,
                        Type = t.Type,
                        Description = t.Description,
                        Status = t.Status,
                        Timestamp = t.Timestamp
                    })
                    .ToList()
            };

            return Ok(response);
        }

        // POST: api/wallet/debit — Deduct funds (e.g., for order payment)
        [HttpPost("debit")]
        public async Task<ActionResult<WalletResponse>> DebitWallet([FromBody] DebitWalletRequest request)
        {
            if (request.Amount <= 0)
            {
                return BadRequest(new { message = "Amount must be greater than zero" });
            }

            if (string.IsNullOrWhiteSpace(request.Description))
            {
                return BadRequest(new { message = "Transaction description is required" });
            }

            var wallet = await _context.Wallets
                .Include(w => w.Transactions)
                .FirstOrDefaultAsync(w => w.UserId == CurrentUserId);

            if (wallet == null)
            {
                return NotFound(new { message = "Wallet not found for this user" });
            }

            if (wallet.Balance < request.Amount)
            {
                return BadRequest(new { message = "Insufficient wallet balance" });
            }

            wallet.Balance -= request.Amount;

            var transaction = new WalletTransaction
            {
                WalletId = wallet.Id,
                Amount = request.Amount,
                Type = "DEBIT",
                Description = request.Description,
                Status = "SUCCESS",
                Timestamp = DateTime.UtcNow
            };

            _context.WalletTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            var response = new WalletResponse
            {
                Id = wallet.Id,
                Balance = wallet.Balance,
                CreatedAt = wallet.CreatedAt,
                Transactions = wallet.Transactions
                    .OrderByDescending(t => t.Timestamp)
                    .Take(50)
                    .Select(t => new TransactionResponse
                    {
                        Id = t.Id,
                        Amount = t.Amount,
                        Type = t.Type,
                        Description = t.Description,
                        Status = t.Status,
                        Timestamp = t.Timestamp
                    })
                    .ToList()
            };

            return Ok(response);
        }
    }

    // DTOs
    public class FundWalletRequest
    {
        public decimal Amount { get; set; }
        public string Method { get; set; } = "CARD"; // "CARD" or "BANK_TRANSFER"
    }

    public class DebitWalletRequest
    {
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}