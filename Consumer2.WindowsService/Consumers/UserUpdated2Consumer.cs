using System;
using System.Threading.Tasks;
using Contracts;
using MassTransit;

namespace Consumer2.WindowsService.Consumers
{
    public class UserUpdated2Consumer : IConsumer<UserUpdated>
    {
        public Task Consume(ConsumeContext<UserUpdated> context)
        {
            Console.WriteLine($"[{DateTime.Now:G}] Message has been received. UserID: {context.Message.Id}.");
            return Task.CompletedTask;
        }
    }
}
