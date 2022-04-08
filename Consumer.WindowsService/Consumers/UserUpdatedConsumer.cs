using System;
using System.Threading.Tasks;
using Contracts;
using MassTransit;

namespace Consumer.WindowsService.Consumers
{
    public class UserUpdatedConsumer : IConsumer<UserUpdated>
    {
        public Task Consume(ConsumeContext<UserUpdated> context)
        {
            Console.WriteLine($"[{DateTime.Now:G}] Message has been received. UserID: {context.Message.Id}.");
            return Task.CompletedTask;
        }
    }
}
