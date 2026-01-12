// DTOs/Wallet/TransactionResponse.cs
using System;

namespace AgroMove.API.DTOs.Wallet
{
    public class TransactionResponse
    {
        public Guid Id { get; set; }
        public decimal Amount { get; set; }
        public string Type { get; set; } = "DEBIT"; // "CREDIT" or "DEBIT"
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "SUCCESS"; // "SUCCESS", "PENDING", "FAILED"
        public DateTime Timestamp { get; set; }
    }
}