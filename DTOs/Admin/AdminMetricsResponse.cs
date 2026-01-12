// DTOs/Admin/AdminMetricsResponse.cs
namespace AgroMove.API.DTOs.Admin
{
    public class AdminMetricsResponse
    {
        public int TotalUsers { get; set; }
        public int TotalSenders { get; set; }
        public int TotalDrivers { get; set; }
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public int ActiveUsersToday { get; set; }
    }
}