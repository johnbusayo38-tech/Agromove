// Services/IAuthService.cs (Updated)
using AgroMove.API.DTOs.Auth;
using AgroMove.API.Models;

namespace AgroMove.API.Services
{
    public interface IAuthService
    {
        string GenerateJwtToken(User user);
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
    }
}