// Services/UploadService.cs
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AgroMove.API.Services
{
    public class UploadService : IUploadService
    {
        private readonly string _uploadsFolder;

        public UploadService(IHostEnvironment environment)
        {
            _uploadsFolder = Path.Combine(environment.ContentRootPath, "uploads");
            if (!Directory.Exists(_uploadsFolder))
            {
                Directory.CreateDirectory(_uploadsFolder);
            }
        }

        public async Task<string> UploadCargoImageAsync(string base64Image)
        {
            if (string.IsNullOrWhiteSpace(base64Image))
            {
                throw new ArgumentException("Base64 image string is empty");
            }

            try
            {
                // Remove data URL prefix if present (e.g., "data:image/jpeg;base64,")
                var prefix = base64Image.IndexOf(',') + 1;
                if (prefix > 0 && prefix < base64Image.Length)
                {
                    base64Image = base64Image.Substring(prefix);
                }

                var bytes = Convert.FromBase64String(base64Image);
                var fileName = $"{Guid.NewGuid()}.jpg"; // You can detect extension if needed
                var filePath = Path.Combine(_uploadsFolder, fileName);

                await File.WriteAllBytesAsync(filePath, bytes);

                // Return URL path that maps to static files
                return $"/uploads/{fileName}";
            }
            catch (FormatException)
            {
                throw new ArgumentException("Invalid base64 string");
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to upload image", ex);
            }
        }
    }
}