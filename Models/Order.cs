

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgroMove.API.Models
{
public enum OrderStatus
{
    Pending,
    Accepted,
    InTransit, // Ensure there is no space here
    Cleared,
    Delivered,
    Cancelled
}
    [Table("Orders")]
    public class Order
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ShipperId { get; set; }
        public User Shipper { get; set; } = null!;

        public Guid? DriverId { get; set; }
        public User? Driver { get; set; }

        [Column(TypeName = "text")]
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        public bool IsInternational { get; set; } = false;
        public string PickupLocation { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;

        // Logistics Data
        public string? ProduceType { get; set; }
        public string? Weight { get; set; } 
      // public int Quantity { get; set; } 
        public string? BoxSize { get; set; }
        
        // Receiver Info
        public string? ReceiverName { get; set; }
        public string? ReceiverPhone { get; set; }
        public string? ReceiverEmail { get; set; }
        public string? ReceiverAddress { get; set; }
        public string? SenderName { get; set; }
        public string? SpecialInstructions { get; set; } 

        [Column(TypeName = "decimal(18,2)")]
        public decimal EstimatedCost { get; set; }

        // Logistics Advice Fields 
        public string RecommendedVehicle { get; set; } = string.Empty;
        public string SpecialAdvice { get; set; } = string.Empty;
        public string? EstimatedTime { get; set; }
        public string? CargoImageUrl { get; set; }

        /// <summary>
        /// FIX: Defaulted to "[]" (Empty Array) instead of "{}" (Empty Object).
        /// This prevents the "Unexpected end of input" SQL error when 
        /// the Admin Controller tries to parse Marketplace items.
        /// </summary>
        public string OrderDetailsJson { get; set; } = "[]";

        // Status Timestamps 
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? AcceptedAt { get; set; }
        public DateTime? InTransitAt { get; set; }
        public DateTime? ClearedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime? CancelledAt { get; set; }

        // Navigation property for Marketplace items
        public List<OrderItem> OrderItems { get; set; } = new();
    }
}