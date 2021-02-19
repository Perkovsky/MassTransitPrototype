using MassTransit;
using MG.EventBus.Contracts;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MG.EventBus.Components.Consumers
{
	public class SendMailConsumer : IConsumer<SendMail>
	{
		#region Data to Test Exceptions

		private const string ERROR_MARKER = "error";        // after all retries - Fault 
		private const string WARNING_MARKER = "warning";    // after 3rd retries - Success

		#endregion

		public async Task Consume(ConsumeContext<SendMail> context)
		{
			if (context.Message.Message.Equals(ERROR_MARKER, StringComparison.InvariantCultureIgnoreCase))
				throw new Exception(ERROR_MARKER);
			if (context.Message.Message.Equals(WARNING_MARKER, StringComparison.InvariantCultureIgnoreCase) && context.GetRetryAttempt() < 3)
				throw new Exception(WARNING_MARKER);

			await Console.Out.WriteLineAsync($"[{DateTime.Now}] - Received: ID={context.Message.Id}, Msg='{context.Message.Message}', {context.Message.CreatedDate}, {@context.DestinationAddress.PathAndQuery}");
		}
	}
}
