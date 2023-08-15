namespace CommonLibraries.Services
{
    public class EmailSendService : IEmailSendService
    {
        public Task SendCriticalEmailAsync(Exception exception, string subject, string body)
        {
            Console.WriteLine($"Critical email has been sent. Subject: '{subject}'. Data: '{body}'.");
            return Task.CompletedTask;
        }
    }
}
