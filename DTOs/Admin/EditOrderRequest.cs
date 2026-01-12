// DTOs/Admin/EditOrderRequest.cs
namespace AgroMove.API.DTOs.Admin
{
    public class EditOrderRequest
    {
        public string? PickupLocation { get; set; }
        public string? Destination { get; set; }
        public decimal? EstimatedCost { get; set; }
        public string? RecommendedVehicle { get; set; }
        public string? SpecialAdvice { get; set; }
        public string? EstimatedTime { get; set; }
        public string? CargoImageBase64 { get; set; }
        public Dictionary<string, object>? Details { get; set; }
    }
}
