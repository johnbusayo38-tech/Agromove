
// Controllers/AdminOrderController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using FirebaseAdmin.Messaging;
using AgroMove.API.Data;
using AgroMove.API.Models;
using AgroMove.API.DTOs.Admin;

namespace AgroMove.API.Controllers
{
    [Route("api/admin/orders")]
    [ApiController]
    [Authorize(Roles = "ADMIN,SUPERADMIN")]
    public class AdminOrderController : ControllerBase
    {
        private readonly AgroMoveDbContext _context;

        public AdminOrderController(AgroMoveDbContext context)
        {
            _context = context;
        }

        // --- BACKEND CALCULATION HELPERS ---
        private decimal CalculateTotalPayable(Order order)
        {
            // If Marketplace items exist, sum them using PriceAtPurchase
            if (order.OrderItems != null && order.OrderItems.Any())
            {
                return order.OrderItems.Sum(i => i.PriceAtPurchase * i.Quantity);
            }
            // Otherwise, fallback to the Admin-quoted EstimatedCost (for Logistics)
            return order.EstimatedCost;
        }

        private string GetMarketplaceSummary(Order order)
        {
            if (order.OrderItems != null && order.OrderItems.Any())
            {
                return string.Join(", ", order.OrderItems.Select(i => $"{i.Quantity}x {i.ProductName}"));
            }
            return "Logistics Service";
        }

        private JsonElement SafeParseJson(string? jsonString)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(jsonString))
                    return JsonDocument.Parse("{}").RootElement;
                return JsonSerializer.Deserialize<JsonElement>(jsonString);
            }
            catch
            {
                return JsonDocument.Parse("{}").RootElement;
            }
        }

        // GET: api/admin/orders/active
        [HttpGet("active")]
        public async Task<ActionResult<PaginatedResponse<AdminOrderResponse>>> GetActiveOrders(
            [FromQuery] int page = 1,
            [FromQuery] int size = 50)
        {
            var activeStatuses = new[] { 
                OrderStatus.Pending, 
                OrderStatus.Accepted, 
                OrderStatus.InTransit, 
                OrderStatus.Cleared 
            };

            var query = _context.Orders
                .Where(o => activeStatuses.Contains(o.Status))
                .Include(o => o.Shipper)
                .Include(o => o.Driver)
                .Include(o => o.OrderItems); 

            var total = await query.CountAsync();

            var ordersRaw = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();

            var responseData = ordersRaw.Select(o => new AdminOrderResponse
            {
                Id = o.Id,
                Status = o.Status.ToString(),
                IsInternational = o.IsInternational,
                PickupLocation = o.PickupLocation,
                Destination = o.Destination,
                ProduceType = o.ProduceType,
              //  Quantity = o.Quantity,
                Weight = o.Weight,
                BoxSize = o.BoxSize,
                SpecialInstructions = o.SpecialInstructions,
                ReceiverName = o.ReceiverName,
                ReceiverPhone = o.ReceiverPhone,
                SenderName = o.SenderName ?? o.Shipper?.Name,
                
                // BACKEND CALCULATED VALUES
                EstimatedCost = o.EstimatedCost,
                TotalPayable = CalculateTotalPayable(o),
                MarketplaceSummary = GetMarketplaceSummary(o),

                RecommendedVehicle = o.RecommendedVehicle,
                SpecialAdvice = o.SpecialAdvice,
                EstimatedTime = o.EstimatedTime,
                CargoImageUrl = o.CargoImageUrl,
                DriverName = o.Driver?.Name,
                CreatedAt = o.CreatedAt,
                Details = SafeParseJson(o.OrderDetailsJson)
            }).ToList();

            return Ok(new PaginatedResponse<AdminOrderResponse>
            {
                Data = responseData,
                Total = total,
                Page = page,
                Size = size
            });
        }

        // GET: api/admin/orders/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<AdminOrderDetailResponse>> GetOrderDetails(Guid id)
        {
            var order = await _context.Orders
                .Include(o => o.Shipper)
                .Include(o => o.Driver)
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound(new { message = "Order not found" });

            return Ok(new AdminOrderDetailResponse
            {
                Id = order.Id,
                Shipper = new AdminUserSummary { Id = order.Shipper.Id, Name = order.Shipper.Name, Phone = order.Shipper.Phone },
                Driver = order.Driver != null ? new AdminUserSummary { Id = order.Driver.Id, Name = order.Driver.Name, Phone = order.Driver.Phone } : null,
                PickupLocation = order.PickupLocation,
                Destination = order.Destination,
                ProduceType = order.ProduceType,
            //    Quantity = order.Quantity,
                Weight = order.Weight,
                BoxSize = order.BoxSize,
                ReceiverName = order.ReceiverName,
                ReceiverPhone = order.ReceiverPhone,
                SenderName = order.SenderName ?? order.Shipper.Name,
                SpecialInstructions = order.SpecialInstructions,
                
                // BACKEND CALCULATED VALUES
                EstimatedCost = order.EstimatedCost,
                TotalPayable = CalculateTotalPayable(order),
                MarketplaceSummary = GetMarketplaceSummary(order),

                RecommendedVehicle = order.RecommendedVehicle ?? string.Empty,
                SpecialAdvice = order.SpecialAdvice ?? string.Empty,
                EstimatedTime = order.EstimatedTime ?? string.Empty,
                Status = order.Status.ToString(),
                IsInternational = order.IsInternational,
                CreatedAt = order.CreatedAt,
                AcceptedAt = order.AcceptedAt,
                CargoImageUrl = order.CargoImageUrl,
                Details = SafeParseJson(order.OrderDetailsJson)
            });
        }

[HttpPut("{id}/status")]
public async Task<IActionResult> UpdateOrderStatus(Guid id, [FromBody] UpdateOrderStatusRequest request)
{
    if (!Enum.TryParse<OrderStatus>(request.Status, true, out var newStatus))
        return BadRequest(new { message = "Invalid status" });

    var order = await _context.Orders
        .Include(o => o.Shipper)
        .FirstOrDefaultAsync(o => o.Id == id);

    if (order == null) 
        return NotFound(new { message = "Order not found" });

    var oldStatus = order.Status;
    order.Status = newStatus;

    // Update relevant timestamps only if not already set
    switch (newStatus)
    {
        case OrderStatus.Accepted when order.AcceptedAt == null:
            order.AcceptedAt = DateTime.UtcNow;
            break;
        case OrderStatus.InTransit when order.InTransitAt == null:
            order.InTransitAt = DateTime.UtcNow;
            break;
        case OrderStatus.Cleared when order.ClearedAt == null:
            order.ClearedAt = DateTime.UtcNow;
            break;
        case OrderStatus.Delivered when order.DeliveredAt == null:
            order.DeliveredAt = DateTime.UtcNow;
            break;
        case OrderStatus.Cancelled when order.CancelledAt == null:
            order.CancelledAt = DateTime.UtcNow;
            break;
    }

    await _context.SaveChangesAsync();

    // Save notification to database (personal to shipper)
    if (order.ShipperId != null && oldStatus != newStatus)
    {
        var notification = new AgroMove.API.Models.Notification // Fully qualified to avoid any ambiguity
        {
            UserId = order.ShipperId,
            Title = "Order Status Update",
            Message = $"Your order for {order.ProduceType ?? "cargo"} is now {newStatus.ToString().Replace("_", " ")}.",
            Type = "ORDER_UPDATE",
            RelatedOrderId = order.Id,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        Console.WriteLine($"DB Notification saved for Shipper {order.ShipperId} - Order {order.Id}");
    }

    return Ok(new 
    { 
        message = "Order status updated and notification saved to database",
        status = newStatus.ToString()
    });
}
    }
}