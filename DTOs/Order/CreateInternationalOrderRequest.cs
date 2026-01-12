namespace AgroMove.API.DTOs.Order
{
    public class CreateInternationalOrderRequest
    {
        public string PickupLocation { get; set; } = string.Empty;
        public string DestinationCountry { get; set; } = string.Empty;
        public string ProduceType { get; set; } = string.Empty;
        public string Weight { get; set; } = string.Empty;
        public decimal Volume { get; set; }
        public string ContainerSize { get; set; } = "20ft";
        public string HsCode { get; set; } = string.Empty;
        public string Incoterm { get; set; } = "FOB";
        public string TransportMode { get; set; } = "SEA";

        // Consistency Fix: Receiver instead of Consignee
        public string ReceiverName { get; set; } = string.Empty;
        public string ReceiverEmail { get; set; } = string.Empty;
        public string ReceiverPhone { get; set; } = string.Empty;
        public string ReceiverAddress { get; set; } = string.Empty;

        public decimal EstimatedCost { get; set; }
        public string RecommendedVehicle { get; set; } = string.Empty;
        public string SpecialAdvice { get; set; } = string.Empty;
        public string? EstimatedTime { get; set; }
        public string? CargoImageBase64 { get; set; }
        //public int Quantity { get; set; }
    }
}