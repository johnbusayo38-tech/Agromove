// Models/WalletTransaction.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgroMove.API.Models
{
    public class WalletTransaction
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public string Type { get; set; } = "DEBIT"; // "CREDIT" or "DEBIT"

        public string Description { get; set; } = string.Empty;

        public string Status { get; set; } = "SUCCESS"; // "SUCCESS", "PENDING", "FAILED"

        // Foreign Key
        public Guid WalletId { get; set; }

        // Navigation Property
        public Wallet Wallet { get; set; } = null!;
    }
}