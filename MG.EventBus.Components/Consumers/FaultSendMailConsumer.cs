using MassTransit;
using MG.EventBus.Contracts;
using System;
using System.Threading.Tasks;

namespace MG.EventBus.Components.Consumers
{
	public class FaultSendMailConsumer : IConsumer<Fault<SendMail>>
	{
		public async Task Consume(ConsumeContext<Fault<SendMail>> context)
		{
			// error handling here
			//	logging, changing status email into DB, etc.

			await Console.Out.WriteLineAsync($">>> Consuming Fault: ID={context.Message.Message.Id}, Msg='{context.Message.Message.Message}', {@context.DestinationAddress.PathAndQuery}");
			await Console.Out.WriteLineAsync($">>> Exception: {context.Message.Exceptions[0].Message}");
		}
	}
}
