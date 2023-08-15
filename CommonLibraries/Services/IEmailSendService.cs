namespace CommonLibraries.Services
{
    public interface IEmailSendService
    {
        Task SendCriticalEmailAsync(Exception exception, string subject, string body);
    }
}
