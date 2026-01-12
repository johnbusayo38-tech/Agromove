// DTOs/Admin/AdminLoginResponse.cs
namespace AgroMove.API.DTOs.Admin
{
    public class AdminLoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public AdminUserResponse User { get; set; } = new();
    }
}