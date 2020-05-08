using MassTransit;
using MG.EventBus.Contracts;
using System;
using System.Threading.Tasks;

namespace MG.EventBus.Components.Consumers
{
	public class FaultMailStoredConsumer : IConsumer<Fault<MailStored>>
	{
		public async Task Consume(ConsumeContext<Fault<MailStored>> context)
		{
			// error handling here
			//	logging, changing status email on DB, etc.
			await Console.Out.WriteLineAsync($">>> Consuming Fault: ID={context.Message.Message.Id}, Msg='{context.Message.Message.Message}', {context.Message.Exceptions[0].Message}");
			await Console.Out.WriteLineAsync($@"\t\t{context.Message.Exceptions[0].Message}");
		}
	}
}
