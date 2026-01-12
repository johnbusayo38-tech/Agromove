// DTOs/Notification/NotificationResponse.cs
// Updated: RelatedOrderId as string? for JSON response

namespace AgroMove.API.DTOs.Notification
{
    public class NotificationResponse
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = "INFO";
        public string? RelatedOrderId { get; set; } // String for client
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}