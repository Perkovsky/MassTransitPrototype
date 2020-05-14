using System;
using System.Collections.Generic;
using System.Linq;

namespace MG.EventBus.Startup.Models
{
	internal class ReceiveEndpointRegistration
	{
		public string QueueName { get; private set; }
		public bool CanUsePriority { get; private set; }

		public IEnumerable<Type> Consumers { get; private set; }
		public IEnumerable<Type> FaultConsumers { get; private set; }

		public ReceiveEndpointRegistration(string queueName, 
			IEnumerable<Type> consumers,
			IEnumerable<Type> faultConsumers = default,
			bool canUsePriority = false)
		{
			if (string.IsNullOrWhiteSpace(queueName))
				throw new ArgumentException("Value cannot be null or empty.", nameof(queueName));

			if (consumers == null || !consumers.Any())
				throw new ArgumentException("Value cannot be null or empty.", nameof(consumers));

			QueueName = queueName;
			Consumers = consumers;
			FaultConsumers = faultConsumers ?? Enumerable.Empty<Type>();
			CanUsePriority = canUsePriority;
		}
	}
}
