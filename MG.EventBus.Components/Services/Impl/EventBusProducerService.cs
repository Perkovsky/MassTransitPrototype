using MassTransit;
using MassTransit.Definition;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MG.EventBus.Components.Services.Impl
{
	public class EventBusProducerService : IEventBusProducerService
	{
		private readonly IPublishEndpoint _publishEndpoint;
		private readonly ISendEndpointProvider _sendEndpointProvider;

		public EventBusProducerService(IPublishEndpoint publishEndpoint, ISendEndpointProvider sendEndpointProvider)
		{
			_publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
			_sendEndpointProvider = sendEndpointProvider ?? throw new ArgumentNullException(nameof(sendEndpointProvider));
		}

		#region Private Methods

		private Uri GetQueueUri<T>()
			where T : class, IConsumer
		{
			string queueName = KebabCaseEndpointNameFormatter.Instance.Consumer<T>();
			return new Uri($"queue:{queueName}");
		}

		#endregion

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

		public Task Send<TContract, TConsumer>(object values)
			where TContract : class
			where TConsumer : class, IConsumer<TContract>
		{
			return SendAsync<TContract, TConsumer>(values);
		}

		public async Task SendAsync<TContract, TConsumer>(object values, CancellationToken cancellationToken = default)
			where TContract : class
			where TConsumer : class, IConsumer<TContract>
		{
			var endpoint = await _sendEndpointProvider.GetSendEndpoint(GetQueueUri<TConsumer>());
			await endpoint.Send<TContract>(values);
		}
	}
}
