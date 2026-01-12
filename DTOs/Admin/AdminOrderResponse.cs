using System;

namespace AgroMove.API.DTOs.Admin
{
    public class AdminOrderResponse
    {
        public Guid Id { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsInternational { get; set; }
        public string PickupLocation { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public string? ProduceType { get; set; }
        public string? Quantity { get; set; }
        public string? Weight { get; set; }
        public string? BoxSize { get; set; }
        public string? ReceiverName { get; set; }
        public string? ReceiverPhone { get; set; }
        public string? SenderName { get; set; }
        
        public decimal EstimatedCost { get; set; }
        // NEW: Matches the logic in the controller
        public decimal TotalPayable { get; set; }

        public string? MarketplaceSummary { get; set; }
        public string? DriverName { get; set; }
        public DateTime CreatedAt { get; set; }
        public object? Details { get; set; }
        
        public string? CargoImageUrl { get; set; }
        public string? RecommendedVehicle { get; set; }
        public string? SpecialAdvice { get; set; }
        public string? EstimatedTime { get; set; }
        public string? SpecialInstructions { get; set; }
    }
}