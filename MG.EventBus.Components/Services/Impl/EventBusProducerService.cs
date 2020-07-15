using MassTransit;
using MassTransit.Monitoring.Health;
using MG.EventBus.Components.Helpers;
using MG.EventBus.Components.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MG.EventBus.Components.Services.Impl
{
	public class EventBusProducerService : IEventBusProducerService
	{
		private readonly IPublishEndpoint _publishEndpoint;
		private readonly ISendEndpointProvider _sendEndpointProvider;
		private readonly IBusHealth _busHealth;

		public EventBusProducerService(IPublishEndpoint publishEndpoint, ISendEndpointProvider sendEndpointProvider, IBusHealth busHealth)
		{
			_publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
			_sendEndpointProvider = sendEndpointProvider ?? throw new ArgumentNullException(nameof(sendEndpointProvider));
			_busHealth = busHealth ?? throw new ArgumentNullException(nameof(busHealth));

			var health = _busHealth.CheckHealth();
		}

		public Task Publish<T>(object values)
			where T : class
		{
			return PublishAsync<T>(values);
		}

		public async Task PublishAsync<T>(object values, CancellationToken cancellationToken = default)
			where T : class
		{
			await _publishEndpoint.Publish<T>(values, cancellationToken);
		}

		public Task Send<TContract, TConsumer>(object values, QueuePriority priority = QueuePriority.Normal)
			where TContract : class
			where TConsumer : class, IConsumer<TContract>
		{
			return SendAsync<TContract, TConsumer>(values, priority);
		}

		public async Task SendAsync<TContract, TConsumer>(object values, QueuePriority priority = QueuePriority.Normal, CancellationToken cancellationToken = default)
			where TContract : class
			where TConsumer : class, IConsumer<TContract>
		{
			var health = _busHealth.CheckHealth();

			var endpoint = await _sendEndpointProvider.GetSendEndpoint(QueueHelper.GetQueueUri<TConsumer>(priority));
			await endpoint.Send<TContract>(values);
		}
	}
}
