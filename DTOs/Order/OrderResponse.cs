namespace AgroMove.API.DTOs.Order
{
    public class OrderResponse
    {
        public Guid Id { get; set; }
        public string Status { get; set; } = "Pending";
        public bool IsInternational { get; set; }

        public string PickupLocation { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;

        public DateTime? AcceptedAt { get; set; }
        public DateTime? InTransitAt { get; set; }
        public DateTime? ClearedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime? CancelledAt { get; set; }


        // Specific properties for Dashboard visibility
        public string? ProduceType { get; set; }
        //public int Quantity { get; set; } = 1;
        public string? Weight { get; set; }
        public string? BoxSize { get; set; }
        public string? SpecialInstructions { get; set; }
        public string? ReceiverName { get; set; }
        public string? ReceiverPhone { get; set; }
        public string? SenderName { get; set; }

        public decimal EstimatedCost { get; set; }
        public string RecommendedVehicle { get; set; } = string.Empty;
        public string SpecialAdvice { get; set; } = string.Empty;
        public string? EstimatedTime { get; set; }

        public string? CargoImageUrl { get; set; }
        public string? DriverName { get; set; }
public List<MarketplaceItemResponse>? Items { get; set; }
        public DateTime CreatedAt { get; set; }
        public Dictionary<string, object>? Details { get; set; }

        public class MarketplaceItemResponse
    {
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
    }

   
}