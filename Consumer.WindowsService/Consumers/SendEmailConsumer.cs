using System;
using System.Threading.Tasks;
using Contracts;
using MassTransit;

namespace Consumer.WindowsService.Consumers
{
    public class SendEmailConsumer : IConsumer<SendEmail>
    {
        public Task Consume(ConsumeContext<SendEmail> context)
        {
            Console.WriteLine($"[{DateTime.Now:G}] Message has been received. Email: {context.Message.Email}, Text: {context.Message.Text}.");
            return Task.CompletedTask;
        }
    }
}
