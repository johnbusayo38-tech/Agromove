// // DTOs/Admin/AdminOrderDetailResponse.cs
// // Detail response inherits from base
// // Use 'new' for CargoImageUrl to hide inherited member (suppresses warning)

// namespace AgroMove.API.DTOs.Admin
// {
//     public class AdminOrderDetailResponse : AdminOrderResponse
//     {
//         public AdminUserSummary Shipper { get; set; } = new();
//         public AdminUserSummary? Driver { get; set; }
//         public string RecommendedVehicle { get; set; } = string.Empty;
//         public string SpecialAdvice { get; set; } = string.Empty;
//         public string EstimatedTime { get; set; } = string.Empty;
//         public DateTime? AcceptedAt { get; set; }
//         public new string? CargoImageUrl { get; set; } // 'new' hides base property
//         public Dictionary<string, object> Details { get; set; } = new();
//     }
// }



// DTOs/Admin/AdminOrderDetailResponse.cs (Updated)
using System;
using System.Collections.Generic;

namespace AgroMove.API.DTOs.Admin
{
    public class AdminOrderDetailResponse
    {
        public Guid Id { get; set; }
        public AdminUserSummary Shipper { get; set; } = null!;
        public AdminUserSummary? Driver { get; set; }
        public string PickupLocation { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        
        // The raw quote from the Admin
        public decimal EstimatedCost { get; set; }
        
        // NEW: The final backend-calculated total (Items Sum OR EstimatedCost)
        public decimal TotalPayable { get; set; }

        public string RecommendedVehicle { get; set; } = string.Empty;
        public string SpecialAdvice { get; set; } = string.Empty;
        public string EstimatedTime { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsInternational { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? AcceptedAt { get; set; }
        public string? CargoImageUrl { get; set; }
        
        public object? Details { get; set; }
        public string? ProduceType { get; set; }
        public string? Quantity { get; set; }
        public string? Weight { get; set; }
        public string? BoxSize { get; set; }
        public string? ReceiverName { get; set; }
        public string? ReceiverPhone { get; set; }
        public string? SenderName { get; set; }
        public string? MarketplaceSummary { get; set; }
        public string? SpecialInstructions { get; set; }
    }
}