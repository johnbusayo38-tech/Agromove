// DTOs/Wallet/WalletResponse.cs
using System;
using System.Collections.Generic;
using AgroMove.API.DTOs.Wallet;
namespace AgroMove.API.DTOs.Wallet
{
    public class WalletResponse
    {
        public Guid Id { get; set; }
        public decimal Balance { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<TransactionResponse> Transactions { get; set; } = new List<TransactionResponse>();
    }
}