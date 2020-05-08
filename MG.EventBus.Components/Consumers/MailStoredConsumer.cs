using MassTransit;
using MG.EventBus.Contracts;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MG.EventBus.Components.Consumers
{
	public class MailStoredConsumer : IConsumer<MailStored>
	{
		public async Task Consume(ConsumeContext<MailStored> context)
		{
			await Console.Out.WriteLineAsync($"Received: ID={context.Message.Id}, Msg='{context.Message.Message}', {context.Message.CreatedDate}");
		}
	}
}
