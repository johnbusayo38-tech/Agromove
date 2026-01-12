


// Models/User.cs (Updated - Duplicate 'Location' removed)
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgroMove.API.Models
{
    public class User
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Name { get; set; } = string.Empty;

       [Required]
public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public string Role { get; set; } = "SENDER"; // SENDER, DRIVER, ADMIN, SUPERADMIN

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLogin { get; set; }

        public bool IsBanned { get; set; } = false;

        public string? BanReason { get; set; }

        public DateTime? BannedAt { get; set; }

        public bool IsVerified { get; set; } = false;

        public string? VerificationToken { get; set; }

        public DateTime? VerificationTokenExpires { get; set; }

        public string? DeviceToken { get; set; }

        // ← Location defined only once (duplicate removed)
        public string? Location { get; set; }

        // Navigation Properties
        public Wallet Wallet { get; set; } = null!;

        public List<Order> OrdersAsShipper { get; set; } = new();
        public List<Order> OrdersAsDriver { get; set; } = new();
    }
}