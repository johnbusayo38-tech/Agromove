// Services/IUploadService.cs
namespace AgroMove.API.Services
{
    public interface IUploadService
    {
        Task<string> UploadCargoImageAsync(string base64Image);
    }
}