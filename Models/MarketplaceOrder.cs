namespace AgroMove.API.Models
{
    public class MarketplaceOrder
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; }

        public decimal TotalAmount { get; set; }
        public string DeliveryAddress { get; set; } = string.Empty;
        public string ReceiverName { get; set; } = string.Empty;
        public string ReceiverPhone { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending"; // Pending, Processing, Shipped, Delivered
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Relationship: One Order has many Items
        public List<MarketplaceOrderItem> Items { get; set; } = new();
    }

public class MarketplaceOrderItem
{
    public Guid Id { get; set; }
    public Guid MarketplaceOrderId { get; set; }
    
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty; // e.g., "Organic Tomatoes"
    public string GaugeLabel { get; set; } = string.Empty; // e.g., "2kg Basket" or "Small Bag"
    public string Description { get; set; } = string.Empty; // Snapshot of the product description
    public string ImageUrl { get; set; } = string.Empty; // Store the image link
    public string Weight { get; set; } = string.Empty; // e.g., "5kg"
    
    public int Quantity { get; set; }
    public decimal PriceAtPurchase { get; set; }
}
}