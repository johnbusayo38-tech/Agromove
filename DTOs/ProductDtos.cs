// namespace AgroMove.API.DTOs.Shop
// {
//  public class ProductResponse
//     {
//         public Guid Id { get; set; }
//         // Changed from Name to Label to match your database request
//         public string Label { get; set; } = string.Empty; 
//         public string? Description { get; set; }
//         public string Category { get; set; } = string.Empty;
//         public string? ImageUrl { get; set; }
        
//         // Add these as direct properties
//         public decimal Price { get; set; }
//         public decimal Weight { get; set; } 

//         public bool IsLocal { get; set; }
//         public bool IsInternational { get; set; }
        
//         // Remove the Gauges list entirely if you no longer use nested variations
//     }

//     public class ProductGaugeDto
//     {
//         public string Id { get; set; } = string.Empty;
//         public string Label { get; set; } = string.Empty; 
//         public decimal Price { get; set; }
//         public double Weight { get; set; } 
//     }

//     public class CreateAgroOrderRequest
//     {
//         public List<CartItemDto> Items { get; set; } = new();
//         public string DeliveryAddress { get; set; } = string.Empty;
        
//         // Fields for Receiver details
//         public string ReceiverName { get; set; } = string.Empty;
//         public string ReceiverPhone { get; set; } = string.Empty;
        
//         // Market Context
//         public bool IsInternational { get; set; }
//         public string? DestinationCountry { get; set; } 
        
//         public double TotalWeight { get; set; }
//         public decimal TotalAmount { get; set; } 
//         public decimal LogisticsFee { get; set; }
//     }

//     public class CartItemDto
//     {
//         public Guid ProductId { get; set; }
//         public string GaugeId { get; set; } = string.Empty;
//         public int Quantity { get; set; }
//     }

//     public class AdminProductRequest : ProductResponse 
//     {
//         public decimal LocalRatePerKg { get; set; }
//         public decimal InternationalRatePerKg { get; set; }
//         public string? ExportNotes { get; set; }
//     }
// }


namespace AgroMove.API.DTOs.Shop
{
    public class ProductResponse
    {
        public Guid Id { get; set; }
        public string Label { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Category { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        
        // This serialized string is required by your mobile GaugeSelector component
        public string GaugesJson { get; set; } = string.Empty;

        public bool IsLocal { get; set; }
        public bool IsInternational { get; set; }
    }

    public class ProductGaugeDto
    {
        public Guid? Id { get; set; }
        public string Label { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public double Weight { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class CreateAgroOrderRequest
    {
        public List<CartItemDto> Items { get; set; } = new();
        public string DeliveryAddress { get; set; } = string.Empty;
        public string ReceiverName { get; set; } = string.Empty;
        public string ReceiverPhone { get; set; } = string.Empty;
        public bool IsInternational { get; set; }
        public double TotalWeight { get; set; }
        public decimal TotalAmount { get; set; } 
    }

    public class CartItemDto
    {
        public Guid ProductId { get; set; }
        public Guid GaugeId { get; set; } 
        public int Quantity { get; set; }
    }

    public class AdminProductRequest
    {
        public string Label { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Category { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public bool IsLocal { get; set; }
        public bool IsInternational { get; set; }
        public decimal LocalRatePerKg { get; set; }
        public decimal InternationalRatePerKg { get; set; }
        public List<ProductGaugeDto> Gauges { get; set; } = new();
    }

   public class QuoteRequest
{
    public List<CartItemDto>? Items { get; set; }
    public bool IsInternational { get; set; }
    
    // ADD THIS LINE
    public string? Weight { get; set; } 

    // Ensure these exist too if you are using them in the controller
    public string? Origin { get; set; }
    public string? Destination { get; set; }
}

    // 2. Define the Response going back to React Native
    public class OrderQuoteResponse
    {
        public decimal ItemSubtotal { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal TotalAmount { get; set; }
        public double TotalWeight { get; set; }
        public string Currency { get; set; } = "NGN";
    }
}