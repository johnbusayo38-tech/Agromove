// DTOs/Admin/VerifyDriverRequest.cs
namespace AgroMove.API.DTOs.Admin
{
    public class VerifyDriverRequest
    {
        public bool IsApproved { get; set; }
        public string? Notes { get; set; }
    }
}