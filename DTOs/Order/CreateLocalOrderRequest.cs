namespace AgroMove.API.DTOs.Order
{
    public class CreateLocalOrderRequest
    {
        public string PickupLocation { get; set; } = string.Empty;
        public string DropoffLocation { get; set; } = string.Empty;
        
        public string ProduceType { get; set; } = string.Empty;
       // public int Quantity { get; set; } = string.Empty;
        public string? Weight { get; set; }
        public string? BoxSize { get; set; } 
        public string? Details { get; set; } 

        public string ReceiverName { get; set; } = string.Empty;
        public string ReceiverPhone { get; set; } = string.Empty;
        public string? SenderName { get; set; }

        public decimal EstimatedCost { get; set; }
        public string RecommendedVehicle { get; set; } = string.Empty;
        public string SpecialAdvice { get; set; } = string.Empty;
        public string EstimatedTime { get; set; } = string.Empty;
        public string? CargoImageBase64 { get; set; }
    }
}