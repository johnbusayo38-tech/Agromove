// Services/IEmailService.cs
using System.Threading.Tasks;

namespace AgroMove.API.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlMessage);
    }
}