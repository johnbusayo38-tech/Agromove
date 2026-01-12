// Models/AgroOrder.cs
using System.ComponentModel.DataAnnotations;

namespace AgroMove.Backend.Models
{
    public class AgroOrder
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public string UserId { get; set; }
        
        // Logistics
        public string PickupLocation { get; set; } = "Agro Central Hub";
        public string DropoffAddress { get; set; }
        public string ReceiverName { get; set; }
        public string ReceiverPhone { get; set; }
        
        // Financials
        public double TotalWeight { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal TotalAmount { get; set; } // Items + Shipping
        
        public string Status { get; set; } = "Pending";
        public bool IsInternational { get; set; }
        public string? DestinationCountry { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Relationship
        public List<AgroOrderItem> Items { get; set; } = new();
    }

    public class AgroOrderItem
    {
        [Key]
        public int Id { get; set; }
        public Guid AgroOrderId { get; set; }
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public string GaugeLabel { get; set; }
        public int Quantity { get; set; }
        public decimal PriceAtPurchase { get; set; }
    }
}