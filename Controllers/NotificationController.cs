













using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using AgroMove.API.Data;
using AgroMove.API.Models;
using AgroMove.API.DTOs.Notification;

// Aliases to prevent ambiguous reference errors between Firebase and DB entities
using FirebaseNotification = FirebaseAdmin.Messaging.Notification;
using DbNotification = AgroMove.API.Models.Notification;
using FirebaseMessaging = FirebaseAdmin.Messaging.FirebaseMessaging;
using FirebaseMessage = FirebaseAdmin.Messaging.Message;

namespace AgroMove.API.Controllers
{
    [Route("api/notifications")]
    [ApiController]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly AgroMoveDbContext _context;

        public NotificationController(AgroMoveDbContext context)
        {
            _context = context;
        }

        private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // GET: api/notifications
        // Returns user's notifications (optionally unread only)
        [HttpGet]
        public async Task<ActionResult<List<NotificationResponse>>> GetMyNotifications([FromQuery] bool unreadOnly = false)
        {
            var query = _context.Notifications
                .Where(n => n.UserId == CurrentUserId || n.UserId == null); // User-specific + broadcast

            if (unreadOnly)
            {
                query = query.Where(n => !n.IsRead);
            }

            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .Take(100) // Limit for performance
                .Select(n => new NotificationResponse
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.Type,
                    RelatedOrderId = n.RelatedOrderId.HasValue ? n.RelatedOrderId.Value.ToString() : null,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();

            return Ok(notifications);
        }

        // GET: api/notifications/unread-count
        // Returns count of unread notifications
        [HttpGet("unread-count")]
        public async Task<ActionResult<int>> GetUnreadCount()
        {
            var count = await _context.Notifications
                .CountAsync(n => (n.UserId == CurrentUserId || n.UserId == null) && !n.IsRead);

            return Ok(count);
        }

        // POST: api/notifications/mark-read/{id}
        [HttpPost("mark-read/{id}")]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && (n.UserId == CurrentUserId || n.UserId == null));

            if (notification == null)
            {
                return NotFound(new { message = "Notification not found or access denied" });
            }

            notification.IsRead = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Notification marked as read" });
        }

        // POST: api/notifications/mark-all-read
        [HttpPost("mark-all-read")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var notifications = await _context.Notifications
                .Where(n => (n.UserId == CurrentUserId || n.UserId == null) && !n.IsRead)
                .ToListAsync();

            foreach (var n in notifications)
            {
                n.IsRead = true;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "All notifications marked as read" });
        }

        // ADMIN ONLY: Send notification (DB + Firebase push)
        [HttpPost("send")]
        [Authorize(Roles = "ADMIN,SUPERADMIN")]
        public async Task<IActionResult> SendNotification([FromBody] SendNotificationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { message = "Title and message are required" });
            }

            // Validate target user if specified
            string? targetDeviceToken = null;
            if (request.UserId.HasValue)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId.Value);
                if (user == null)
                {
                    return BadRequest(new { message = "Target user does not exist" });
                }
                targetDeviceToken = user.DeviceToken;
            }

            // Parse RelatedOrderId safely
            Guid? parsedOrderId = null;
            if (!string.IsNullOrEmpty(request.RelatedOrderId) && Guid.TryParse(request.RelatedOrderId, out var guid))
            {
                parsedOrderId = guid;
            }

            // Save to database
            var dbNotif = new DbNotification
            {
                UserId = request.UserId, // null for broadcast
                Title = request.Title,
                Message = request.Message,
                Type = request.Type ?? "INFO",
                RelatedOrderId = parsedOrderId,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.Notifications.Add(dbNotif);
            await _context.SaveChangesAsync();

            // Send Firebase push if device token available
            if (!string.IsNullOrEmpty(targetDeviceToken))
            {
                try
                {
                    var fcmMessage = new FirebaseMessage
                    {
                        Token = targetDeviceToken,
                        Notification = new FirebaseNotification
                        {
                            Title = dbNotif.Title,
                            Body = dbNotif.Message
                        },
                        Data = new Dictionary<string, string>
                        {
                            { "notificationId", dbNotif.Id.ToString() },
                            { "type", dbNotif.Type },
                            { "orderId", dbNotif.RelatedOrderId?.ToString() ?? "" },
                            { "createdAt", dbNotif.CreatedAt.ToString("o") }
                        }
                    };

                    await FirebaseMessaging.DefaultInstance.SendAsync(fcmMessage);
                }
                catch (Exception ex)
                {
                    // Log but don't fail — DB record is primary
                    Console.WriteLine($"Firebase push failed: {ex.Message}");
                }
            }

            return Ok(new
            {
                message = "Notification sent and saved",
                notificationId = dbNotif.Id,
                pushSent = !string.IsNullOrEmpty(targetDeviceToken)
            });
        }

        // DELETE: api/notifications/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotification(Guid id)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && (n.UserId == CurrentUserId || n.UserId == null));

            if (notification == null)
            {
                return NotFound(new { message = "Notification not found or access denied" });
            }

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Notification deleted" });
        }

        // DELETE: api/notifications/clear-all
        [HttpDelete("clear-all")]
        public async Task<IActionResult> ClearAllNotifications()
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == CurrentUserId || n.UserId == null)
                .ToListAsync();

            _context.Notifications.RemoveRange(notifications);
            await _context.SaveChangesAsync();

            return Ok(new { message = "All notifications cleared" });
        }
    }
}