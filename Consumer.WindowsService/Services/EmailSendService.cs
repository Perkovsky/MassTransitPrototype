using System;
using System.Threading.Tasks;

namespace Consumer.WindowsService.Services
{
    public class EmailSendService : IEmailSendService
    {
        public Task SendCriticalEmailAsync(Exception exception, string subject, string body)
        {
            return Task.CompletedTask;
        }
    }
}
