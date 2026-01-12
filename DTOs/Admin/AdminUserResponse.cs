// DTOs/Admin/AdminUserResponse.cs
namespace AgroMove.API.DTOs.Admin
{
    public class AdminUserResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty; // Added
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public decimal WalletBalance { get; set; } // Added
        public int OrderCount { get; set; } // Added
        public DateTime CreatedAt { get; set; } // Added
        public bool IsVerified { get; set; } // Added
        public DateTime? LastLogin { get; set; } // Added
    }
    public class AdminUserDetailResponse : AdminUserResponse
    {
        public string Location { get; set; } = string.Empty;
    }

    public class AdminUserSummary
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
    }

    public class PaginatedResponse<T>
    {
        public List<T> Data { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int Size { get; set; }
    }
}