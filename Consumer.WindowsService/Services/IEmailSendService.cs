using System;
using System.Threading.Tasks;

namespace Consumer.WindowsService.Services
{
    public interface IEmailSendService
    {
        Task SendCriticalEmailAsync(Exception exception, string subject, string body);
    }
}
