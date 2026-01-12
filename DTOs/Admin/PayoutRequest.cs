// DTOs/Admin/PayoutRequest.cs
namespace AgroMove.API.DTOs.Admin
{
    public class PayoutRequest
    {
        public Guid UserId { get; set; }
        public decimal Amount { get; set; }
    }
}