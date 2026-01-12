// DTOs/Notification/SendNotificationRequest.cs
// Updated: RelatedOrderId remains string? (from client)
// Controller will parse to Guid?

namespace AgroMove.API.DTOs.Notification
{
    public class SendNotificationRequest
    {
        public Guid? UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Type { get; set; }
        public string? RelatedOrderId { get; set; } // String from client (UUID string)
    }
}