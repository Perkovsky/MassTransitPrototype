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

			int numberOfRetries = context.Message.Message.NumberOfRetries;
			if (numberOfRetries < 5)
			{
				++numberOfRetries;
				await Console.Out.WriteLineAsync($">>> Fault! ID={context.Message.Message.Id}, NumberOfRetries: {numberOfRetries}.");
				DateTime scheduledTime = DateTime.UtcNow + TimeSpan.FromSeconds(5);
				await context.ScheduleSend<SendMail>(scheduledTime, new
				{
					Id = context.Message.Message.Id,
					CreatedDate = context.Message.Message.CreatedDate,
					Message = context.Message.Message.Message,
					NumberOfRetries = numberOfRetries,
				});
				return;
			}

			await Console.Out.WriteLineAsync($">>> Consuming Fault: ID={context.Message.Message.Id}, Msg='{context.Message.Message.Message}', {@context.DestinationAddress.PathAndQuery}");
			await Console.Out.WriteLineAsync($">>> Exception: {context.Message.Exceptions[0].Message}");
			await Console.Out.WriteLineAsync($">>> END.\n");
		}
	}
}
