using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using AgroMove.API.Data;
using AgroMove.API.Models;
using AgroMove.API.DTOs.Order;
using AgroMove.API.DTOs.Shop;
using AgroMove.API.Hubs;

namespace AgroMove.API.Controllers
{
    [Route("api/orders")]
    [ApiController]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly AgroMoveDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public OrdersController(AgroMoveDbContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);




[HttpPost("quote")]
public IActionResult GetQuote([FromBody] QuoteRequest request)
{
    decimal itemSubtotal = 0;
    double totalWeight = 0;

    // 1. Calculate Marketplace Items (The code that was failing)
    if (request.Items != null)
    {
        foreach (var item in request.Items)
        {
            itemSubtotal += (item.Price * item.Qty);
            if (double.TryParse(item.Weight?.Replace("kg", ""), out var w))
                totalWeight += (w * item.Qty);
        }
    }

    // 2. Add weight from the manual logistics form
    if (!string.IsNullOrEmpty(request.Weight) && 
        double.TryParse(request.Weight.Replace("kg", ""), out var formWeight))
    {
        totalWeight += formWeight;
    }

    // 3. Apply pricing tiers [cite: 2026-01-09]
    decimal shippingCost = request.IsInternational 
        ? 5000 + (decimal)(totalWeight * 1200) 
        : 1500 + (decimal)(totalWeight * 300);

    decimal customsCost = request.IsInternational ? 7500 : 0;
    
    // SUM EVERYTHING INTO THE TOTAL
    decimal finalTotal = itemSubtotal + shippingCost + customsCost;

    return Ok(new {
        itemSubtotal,
        shippingCost,
        customsCost,
        totalWeight,
        totalAmount = finalTotal // THIS MUST NOT BE 0
    });
}
        // ==========================================
        // 1. MARKETPLACE / AGRO SHOP ORDERS
        // ==========================================
        [HttpPost("agro")]
        public async Task<IActionResult> CreateAgroOrder([FromBody] CreateMarketplaceOrderRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Debit wallet
                var debitSuccess = await InternalDebitLogic(CurrentUserId, request.TotalAmount, $"Agro Shop Order: {request.Items.Count} items");
                if (!debitSuccess) return BadRequest(new { message = "Insufficient wallet balance." });

                var mOrder = new MarketplaceOrder
                {
                    UserId = CurrentUserId,
                    TotalAmount = request.TotalAmount,
                    DeliveryAddress = request.DeliveryAddress,
                    ReceiverName = request.ReceiverName,
                    ReceiverPhone = request.ReceiverPhone,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow,
                    Items = request.Items.Select(item => new MarketplaceOrderItem
                    {
                        ProductId = item.ProductId,
                        ProductName = item.Name,
                        GaugeLabel = item.GaugeLabel,
                        //Quantity = item.Qty,
                        PriceAtPurchase = item.Price,
                        Weight = item.Weight ?? "0kg",
                        ImageUrl = item.ImageUrl ?? "",
                        Description = item.Description ?? ""
                    }).ToList()
                };

                _context.MarketplaceOrders.Add(mOrder);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == CurrentUserId);

                return Ok(new
                {
                    message = "Agro shop order placed successfully",
                    orderId = mOrder.Id,
                    newBalance = wallet?.Balance ?? 0
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "Order creation failed", error = ex.Message });
            }
        }

        [HttpGet("marketplace/history")]
        public async Task<IActionResult> GetMarketplaceHistory()
        {
            var history = await _context.MarketplaceOrders
                .Include(o => o.Items)
                .Where(o => o.UserId == CurrentUserId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return Ok(history);
        }

        // ==========================================
        // 2. LOCAL LOGISTICS ORDERS
        // ==========================================
       // ==========================================
// 2. LOCAL LOGISTICS ORDERS
// ==========================================
[HttpPost("local")]
public async Task<ActionResult<OrderResponse>> CreateLocalOrder([FromBody] CreateLocalOrderRequest request)
{
    using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
        // [cite: 2026-01-09] Backend-driven cost validation
        // Ensure request.EstimatedCost is actually deducted from the wallet
        var debitSuccess = await InternalDebitLogic(
            CurrentUserId, 
            request.EstimatedCost, 
            $"Local Shipment: {request.ProduceType} to {request.DropoffLocation}"
        );

        if (!debitSuccess) return BadRequest(new { message = "Insufficient wallet balance." });

        var order = new Order
        {
            ShipperId = CurrentUserId,
            Status = OrderStatus.Pending,
            IsInternational = false,
            PickupLocation = request.PickupLocation,
            Destination = request.DropoffLocation,
            ReceiverName = request.ReceiverName,
            ReceiverPhone = request.ReceiverPhone,
            EstimatedCost = request.EstimatedCost, // The amount that was just debited
            ProduceType = request.ProduceType,
            Weight = request.Weight,
            CreatedAt = DateTime.UtcNow,
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(MapToResponse(order));
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        return StatusCode(500, new { message = "Local order creation failed", error = ex.Message });
    }
}

// ==========================================
// 3. INTERNATIONAL LOGISTICS ORDERS
// ==========================================
[HttpPost("international")]
public async Task<ActionResult<OrderResponse>> CreateInternationalOrder([FromBody] CreateInternationalOrderRequest request)
{
    using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
        // [cite: 2026-01-09] Debit logic for International
        var debitSuccess = await InternalDebitLogic(
            CurrentUserId, 
            request.EstimatedCost, 
            $"International Shipment: {request.ProduceType} to {request.DestinationCountry}"
        );

        if (!debitSuccess) return BadRequest(new { message = "Insufficient wallet balance." });

        var order = new Order
        {
            ShipperId = CurrentUserId,
            Status = OrderStatus.Pending,
            IsInternational = true,
            PickupLocation = request.PickupLocation,
            Destination = request.DestinationCountry,
            ReceiverName = request.ReceiverName,
            ReceiverPhone = request.ReceiverPhone,
            EstimatedCost = request.EstimatedCost,
            ProduceType = request.ProduceType,
            Weight = request.Weight,
            CreatedAt = DateTime.UtcNow,
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(MapToResponse(order));
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        return StatusCode(500, new { message = "International order creation failed", error = ex.Message });
    }
}

        // ==========================================
        // 4. MY ORDERS (User-specific)
        // ==========================================
        [HttpGet("my-orders")]
        public async Task<ActionResult<List<OrderResponse>>> GetMyOrders()
        {
            var orders = await _context.Orders
                .Where(o => o.ShipperId == CurrentUserId || o.DriverId == CurrentUserId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return Ok(orders.Select(MapToResponse).ToList());
        }

        // ==========================================
        // 5. ORDER DETAIL (User-specific)
        // ==========================================
        [HttpGet("{id}")]
        public async Task<ActionResult<OrderResponse>> GetOrderDetail(Guid id)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == id && (o.ShipperId == CurrentUserId || o.DriverId == CurrentUserId));

            if (order == null)
            {
                return NotFound(new { message = "Order not found or access denied" });
            }

            return Ok(MapToResponse(order));
        }



        // ==========================================
// NEW: INTEGRATED MARKETPLACE + LOGISTICS CHECKOUT
// ==========================================
[HttpPost("agro/checkout")]
public async Task<IActionResult> CheckoutAgroOrder([FromBody] AgroCheckoutRequest request)
{
    using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
        decimal itemsTotal = 0;
        double totalWeightKg = 0;

        // 1. Calculate Backend Costs
        foreach (var item in request.Items)
        {
            // Parse weight for calculation (e.g., "2kg" -> 2.0)
            double itemWeight = double.TryParse(item.Weight?.Replace("kg", ""), out var w) ? w : 0;
            
            if (request.IsInternational)
            {
                // International Logic: Item price * weight
                itemsTotal += item.Price * (decimal)(itemWeight > 0 ? itemWeight : 1);
            }
            else
            {
                // Local Logic: Standard item price
                itemsTotal += item.Price * item.Qty;
            }
            totalWeightKg += itemWeight * item.Qty;
        }

        // 2. Add Logistics & Clearing
        decimal finalTotal = itemsTotal + request.LogisticsCost;
        if (request.IsInternational)
        {
            finalTotal += request.CustomsClearingCost;
        }

        // 3. Debit Wallet
        var description = $"Marketplace Checkout ({(request.IsInternational ? "Intl" : "Local")})";
        var debitSuccess = await InternalDebitLogic(CurrentUserId, finalTotal, description);
        if (!debitSuccess) return BadRequest(new { message = "Insufficient wallet balance." });

        // 4. Create Marketplace Order Record
        var mOrder = new MarketplaceOrder
        {
            UserId = CurrentUserId,
            TotalAmount = finalTotal,
            DeliveryAddress = request.DeliveryAddress,
            ReceiverName = request.ReceiverName,
            ReceiverPhone = request.ReceiverPhone,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            Items = request.Items.Select(item => new MarketplaceOrderItem
            {
                ProductId = item.ProductId,
                ProductName = item.Name,
                GaugeLabel = item.GaugeLabel,
                //Quantity = item.Qty,
                PriceAtPurchase = item.Price,
                Weight = item.Weight ?? "0kg",
                ImageUrl = item.ImageUrl ?? ""
            }).ToList()
        };

        _context.MarketplaceOrders.Add(mOrder);
        await _context.SaveChangesAsync();

        // 5. Link to Logistics Order (So the driver sees it)
        var logisticOrder = new Order
        {
            ShipperId = CurrentUserId,
            Status = OrderStatus.Pending,
            IsInternational = request.IsInternational,
            PickupLocation = "AgroMove Warehouse", // Default warehouse pickup
            Destination = request.DeliveryAddress,
            ReceiverName = request.ReceiverName,
            ReceiverPhone = request.ReceiverPhone,
            EstimatedCost = request.LogisticsCost,
            ProduceType = "Marketplace Goods",
            Weight = $"{totalWeightKg}kg",
           // Quantity = request.Items.Sum(i => i.Qty),
            
            CreatedAt = DateTime.UtcNow,
            OrderDetailsJson = JsonSerializer.Serialize(new { MarketplaceOrderId = mOrder.Id })
        };

        _context.Orders.Add(logisticOrder);
        await _context.SaveChangesAsync();

        await transaction.CommitAsync();

        return Ok(new
        {
            message = "Order and Logistics placed successfully",
            orderId = mOrder.Id,
            shipmentId = logisticOrder.Id,
            totalCharged = finalTotal
        });
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        return StatusCode(500, new { message = "Checkout failed", error = ex.Message });
    }
}



        // ==========================================
        // 6. UPDATE STATUS (Driver or Admin)
        // ==========================================
        [HttpPost("{id}/status")]
        [Authorize(Roles = "DRIVER,ADMIN,SUPERADMIN")]
        public async Task<IActionResult> UpdateOrderStatus(Guid id, [FromBody] UpdateStatusRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await _context.Orders
                    .Include(o => o.Shipper)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null) return NotFound(new { message = "Order not found" });

                // Optional: Role check - only driver assigned or admin can update
                if (!User.IsInRole("ADMIN") && !User.IsInRole("SUPERADMIN") && order.DriverId != CurrentUserId)
                {
                    return Forbid();
                }

                var oldStatus = order.Status;
                order.Status = request.NewStatus;

                var now = DateTime.UtcNow;
                if (request.NewStatus == OrderStatus.Accepted && order.AcceptedAt == null) order.AcceptedAt = now;
                if (request.NewStatus == OrderStatus.InTransit && order.InTransitAt == null) order.InTransitAt = now;
                if (request.NewStatus == OrderStatus.Cleared && order.ClearedAt == null) order.ClearedAt = now;
                if (request.NewStatus == OrderStatus.Delivered && order.DeliveredAt == null) order.DeliveredAt = now;
                if (request.NewStatus == OrderStatus.Cancelled && order.CancelledAt == null) order.CancelledAt = now;

                // Refund on cancellation
                if (request.NewStatus == OrderStatus.Cancelled && oldStatus != OrderStatus.Cancelled)
                {
                    var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == order.ShipperId);
                    if (wallet != null)
                    {
                        wallet.Balance += order.EstimatedCost;

                        _context.WalletTransactions.Add(new WalletTransaction
                        {
                            WalletId = wallet.Id,
                            Amount = order.EstimatedCost,
                            Type = "CREDIT",
                            Description = $"Refund for cancelled order #{order.Id}",
                            Status = "SUCCESS",
                            Timestamp = DateTime.UtcNow
                        });
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Real-time notification to shipper
                if (oldStatus != request.NewStatus && order.ShipperId != null)
                {
                    var notification = new
                    {
                        title = "Order Status Update",
                        message = $"Your order is now {request.NewStatus}",
                        type = "ORDER_UPDATE",
                        orderId = order.Id,
                        timestamp = now
                    };

                    await _hubContext.Clients.User(order.ShipperId.ToString())
                        .SendAsync("ReceiveNotification", notification);
                }

                return Ok(new { message = "Order status updated successfully" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "Status update failed", error = ex.Message });
            }
        }

        // ==========================================
        // 7. INTERNAL DEBIT LOGIC
        // ==========================================
private async Task<bool> InternalDebitLogic(Guid userId, decimal amount, string description)
{
    // 1. Find the wallet belonging to the user
    var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
    
    // 2. Security check: Ensure wallet exists and has enough money
    if (wallet == null || wallet.Balance < amount) return false;

    // 3. Deduct the balance [cite: 2026-01-09]
    wallet.Balance -= amount;

    // 4. Create the transaction record using the CORRECT property names
    var walletTransaction = new WalletTransaction
    {
        WalletId = wallet.Id,      // Correct: Models use WalletId, not UserId
        Amount = amount,           // Correct
        Type = "DEBIT",            // Correct
        Description = description, // Correct
        Status = "SUCCESS",        // Correct
        Timestamp = DateTime.UtcNow // Correct: Models use Timestamp, not CreatedAt
    };

    _context.WalletTransactions.Add(walletTransaction);
    
    // We do not call SaveChanges here because the calling method 
    // (CreateLocalOrder/CreateInternationalOrder) handles the transaction commit.
    return true;
}

        // ==========================================
        // 8. MAPPING HELPER
        // ==========================================
        private OrderResponse MapToResponse(Order order)
        {
            return new OrderResponse
            {
                Id = order.Id,
                Status = order.Status.ToString(),
                IsInternational = order.IsInternational,
                PickupLocation = order.PickupLocation,
                Destination = order.Destination,
                ProduceType = order.ProduceType,
                Weight = order.Weight,
               // Quantity = order.Quantity,
                ReceiverName = order.ReceiverName,
                ReceiverPhone = order.ReceiverPhone,
                EstimatedCost = order.EstimatedCost,
                CreatedAt = order.CreatedAt,
                AcceptedAt = order.AcceptedAt,
                InTransitAt = order.InTransitAt,
                ClearedAt = order.ClearedAt,
                DeliveredAt = order.DeliveredAt,
                CancelledAt = order.CancelledAt
            };
        }
    }

    public class UpdateStatusRequest
    {
        public OrderStatus NewStatus { get; set; }
    }

public class QuoteRequest
{
    public List<MarketplaceItemDto>? Items { get; set; }
    public string PickupLocation { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public string Weight { get; set; } = string.Empty;
    public string ProduceType { get; set; } = string.Empty;
    public bool IsInternational { get; set; }
    public string? DropoffLocation { get; set; } 
}
 public class AgroCheckoutRequest
{
    public List<MarketplaceItemDto> Items { get; set; } = new();
    public string DeliveryAddress { get; set; } = string.Empty;
    public string ReceiverName { get; set; } = string.Empty;
    public string ReceiverPhone { get; set; } = string.Empty;
    
    public string Weight { get; set; } = "0"; 
    public string ProduceType { get; set; } = string.Empty;
    public string Incoterms { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;

    public bool IsInternational { get; set; }
    
    // 🟢 ADD THESE TWO PROPERTIES:
    public decimal TotalAmount { get; set; } 
    public string PaymentMethod { get; set; } = "Wallet";

    public decimal LogisticsCost { get; set; }
    public decimal CustomsClearingCost { get; set; }
}
}