using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgroMove.API.Models
{
    [Table("OrderItems")]
    public class OrderItem
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid OrderId { get; set; }

        [Required]
        public Guid ProductId { get; set; }

        public string ProductName { get; set; } = string.Empty; // From Product.Label
        public string GaugeLabel { get; set; } = string.Empty; // e.g., "Small Bag"
        
        public int Quantity { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal PriceAtPurchase { get; set; }

        [ForeignKey("OrderId")]
        public Order? Order { get; set; }
    }
}