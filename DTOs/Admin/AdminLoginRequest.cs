// DTOs/Admin/AdminLoginRequest.cs
namespace AgroMove.API.DTOs.Admin
{
    public class AdminLoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}