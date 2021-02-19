using MassTransit;
using MG.EventBus.Components.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MG.EventBus.Components.Services
{
	public interface IEventBusProducerService
	{
		Task Publish<T>(object values)
			where T : class;

		Task PublishAsync<T>(object values, CancellationToken cancellationToken = default)
			where T : class;

		Task Send<TContract, TConsumer>(object values, QueuePriority priority = QueuePriority.Normal)
			where TContract : class
			where TConsumer : class, IConsumer<TContract>;

		Task SendAsync<TContract, TConsumer>(object values, QueuePriority priority = QueuePriority.Normal, CancellationToken cancellationToken = default)
			where TContract : class
			where TConsumer : class, IConsumer<TContract>;

		Task ScheduleSend<TContract>(DateTime scheduledTime, object values)
			where TContract : class/*, IContract*/;

		Task ScheduleSendAsync<TContract>(DateTime scheduledTime, object values, CancellationToken cancellationToken = default)
			where TContract : class/*, IContract*/;
	}
}
