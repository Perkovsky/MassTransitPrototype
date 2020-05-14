using System;
using System.Collections.Generic;

namespace MG.EventBus.Startup.Models
{
	internal class ReceiveEndpointRegistration
	{
		public string QueueName { get; set; }
		public bool CanUsePriority { get; set; }

		public IEnumerable<Type> Consumers { get; set; }
	}
}
