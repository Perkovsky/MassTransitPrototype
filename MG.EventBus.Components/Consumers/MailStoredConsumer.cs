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
		#region Data to Test Exceptions

		private const string ERROR_MARKER = "error";        // after all retries - Fault 
		private const string WARNING_MARKER = "warning";    // after 3rd retries - Success
		private static int retryCount = 0;

		#endregion

		public async Task Consume(ConsumeContext<MailStored> context)
		{
			if (context.Message.Message.Equals(ERROR_MARKER, StringComparison.InvariantCultureIgnoreCase))
				throw new Exception(ERROR_MARKER);
			if (context.Message.Message.Equals(WARNING_MARKER, StringComparison.InvariantCultureIgnoreCase)
				&& retryCount < 3)
			{
				retryCount++;
				throw new Exception(WARNING_MARKER);
			}
			else
			{
				retryCount = 0;
			}

			await Console.Out.WriteLineAsync($"Received: ID={context.Message.Id}, Msg='{context.Message.Message}', {context.Message.CreatedDate}");
		}
	}
}
