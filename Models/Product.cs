
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgroMove.API.Models
{
    [Table("Products")]
    public class Product
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string Label { get; set; } = string.Empty; // This is the 'Name'
        public string? Description { get; set; }
        public string Category { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }

        // Link to Variations
        public List<ProductGauge> Gauges { get; set; } = new();

        public decimal LocalRatePerKg { get; set; } = 500m;
        public decimal InternationalRatePerKg { get; set; } = 2500m;

        public bool IsLocal { get; set; } = true;
        public bool IsInternational { get; set; } = false;
        public bool IsAvailable { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    [Table("ProductGauges")]
    public class ProductGauge
    {
        [Key]
        public Guid Id { get; set; }
        
        public Guid ProductId { get; set; }
        
        [Required]
        public string Label { get; set; } = string.Empty; 
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }
        
        public double Weight { get; set; } // double matches your DTO
        public string? ImageUrl { get; set; }

        [ForeignKey("ProductId")]
        public Product? Product { get; set; }
    }
}