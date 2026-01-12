// DTOs/Admin/CreateAdminRequest.cs
namespace AgroMove.API.DTOs.Admin
{
    public class CreateAdminRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool IsSuperAdmin { get; set; } = false;
    }
}
