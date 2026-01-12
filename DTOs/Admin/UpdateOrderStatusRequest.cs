// DTOs/Admin/UpdateOrderStatusRequest.cs
namespace AgroMove.API.DTOs.Admin
{
    public class UpdateOrderStatusRequest
    {
        public string Status { get; set; } = string.Empty; // Pending, Accepted, InTransit, Cleared, Delivered, Cancelled, Dispute
    }
}
