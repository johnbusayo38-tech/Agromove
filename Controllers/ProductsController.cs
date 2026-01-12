

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using AgroMove.API.Data;
using AgroMove.API.Models;
using AgroMove.API.DTOs.Shop;
using System.Security.Claims;

namespace AgroMove.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly AgroMoveDbContext _context;

        public ProductsController(AgroMoveDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductResponse>>> GetProducts([FromQuery] string market = "local")
        {
            bool fetchInternational = market.ToLower() == "international";

            var products = await _context.Products
                .Include(p => p.Gauges) // Critical: fetch the variations
                .Where(p => p.IsAvailable && (fetchInternational ? p.IsInternational : p.IsLocal))
                .ToListAsync();

            return Ok(products.Select(p => new ProductResponse
            {
                Id = p.Id,
                Label = p.Label,
                Description = p.Description,
                Category = p.Category,
                ImageUrl = p.ImageUrl,
                IsLocal = p.IsLocal,
                IsInternational = p.IsInternational,
                // Serialize Gauges into the format needed by GaugeSelector.jsx
                GaugesJson = JsonSerializer.Serialize(p.Gauges.Select(g => new {
                    id = g.Id,
                    label = g.Label,
                    weight = g.Weight,
                    price = g.Price
                }))
            }));
        }

        [HttpPost("purchase")]
        public async Task<IActionResult> PlaceMarketplaceOrder([FromBody] CreateAgroOrderRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = string.IsNullOrEmpty(userIdClaim) ? Guid.Empty : Guid.Parse(userIdClaim);

            // Backend validation: recalculate totals based on DB Gauge records (Security)
            decimal calculatedProductTotal = 0;
            double calculatedWeightTotal = 0;

            foreach (var item in request.Items)
            {
                var gauge = await _context.ProductGauges
                    .FirstOrDefaultAsync(g => g.Id == item.GaugeId && g.ProductId == item.ProductId);
                
                if (gauge == null) return BadRequest("Invalid product gauge selection.");

                calculatedProductTotal += gauge.Price * item.Quantity;
                calculatedWeightTotal += gauge.Weight * item.Quantity;
            }

            var productBase = await _context.Products.FindAsync(request.Items[0].ProductId);
            decimal rate = request.IsInternational ? productBase.InternationalRatePerKg : productBase.LocalRatePerKg;
            decimal shippingCost = (decimal)calculatedWeightTotal * rate;

            var order = new Order
            {
                Id = Guid.NewGuid(),
                ShipperId = userId,
                SenderName = "AgroMove Marketplace",
                PickupLocation = "Agro Central Warehouse",
                Destination = request.DeliveryAddress,
                ReceiverName = request.ReceiverName,
                ReceiverPhone = request.ReceiverPhone,
                IsInternational = request.IsInternational,
                Weight = calculatedWeightTotal.ToString(),
                EstimatedCost = shippingCost,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                OrderDetailsJson = JsonSerializer.Serialize(new {
                    items = request.Items,
                    totalProductPrice = calculatedProductTotal,
                    shippingFee = shippingCost
                })
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return Ok(new { 
                success = true, 
                trackingNumber = order.Id.ToString().Substring(0, 8).ToUpper() 
            });
        }

       [HttpPost("admin/add")]
public async Task<IActionResult> AdminAddProduct([FromBody] AdminProductRequest dto)
{
    var product = new Product
    {
        Id = Guid.NewGuid(),
        Label = dto.Label,
        Description = dto.Description,
        Category = dto.Category,
        ImageUrl = dto.ImageUrl, // This is the main/default product image
        IsLocal = dto.IsLocal,
        IsInternational = dto.IsInternational,
        LocalRatePerKg = dto.LocalRatePerKg,
        InternationalRatePerKg = dto.InternationalRatePerKg,
        
        // Mapping the variations
        Gauges = dto.Gauges.Select(g => new ProductGauge
        {
            Id = Guid.NewGuid(),
            Label = g.Label,
            Weight = g.Weight,
            Price = g.Price,
            // UPDATED: Map the gauge-specific image from the DTO
            ImageUrl = g.ImageUrl 
        }).ToList()
    };

    _context.Products.Add(product);
    await _context.SaveChangesAsync();

    return Ok(new { message = "Product and variations created successfully." });
}

        [HttpDelete("admin/delete/{id}")]
        public async Task<IActionResult> AdminDeleteProduct(Guid id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound(new { message = "Product not found." });

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Product deleted." });
        }
    }
}