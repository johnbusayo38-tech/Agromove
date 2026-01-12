namespace AgroMove.API.DTOs.Order
{
    public class CreateMarketplaceOrderRequest
    {
        public List<MarketplaceItemDto> Items { get; set; } = new();
        public string DeliveryAddress { get; set; } = string.Empty;
        
        // Change these to match the Controller's expectation
        public string ReceiverName { get; set; } = string.Empty; 
        public string ReceiverPhone { get; set; } = string.Empty;

        public decimal TotalAmount { get; set; }
        public bool IsInternational { get; set; }
    }

  public class MarketplaceItemDto
    {
        public Guid ProductId { get; set; }
        public Guid GaugeId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string GaugeLabel { get; set; } = string.Empty;
        public int Qty { get; set; }
        
        public decimal Price { get; set; }

        // ADD THESE THREE PROPERTIES TO FIX THE ERRORS:
        public string? Weight { get; set; }
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
    }
}