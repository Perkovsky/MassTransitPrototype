using MassTransit;
using MG.EventBus.Contracts;
using System;
using System.Threading.Tasks;

namespace MG.EventBus.Components.Consumers
{
	public class TestSomeActionExecutedConsumer : IConsumer<TestSomeActionExecuted>
	{
		public async Task Consume(ConsumeContext<TestSomeActionExecuted> context)
		{
			await Console.Out.WriteLineAsync($"Received: ID={context.Message.Id}, Msg='{context.Message.Message}', {context.Message.CreatedDate}, {@context.DestinationAddress.PathAndQuery}");
		}
	}
}
