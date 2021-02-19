using Apache.NMS;
using MassTransit;
using MassTransit.ActiveMqTransport;
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
		private readonly IMessageScheduler _messageScheduler;

		public EventBusProducerService(IPublishEndpoint publishEndpoint, ISendEndpointProvider sendEndpointProvider, IMessageScheduler messageScheduler)
		{
			_publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
			_sendEndpointProvider = sendEndpointProvider ?? throw new ArgumentNullException(nameof(sendEndpointProvider));
			_messageScheduler = messageScheduler ?? throw new ArgumentNullException(nameof(messageScheduler));
		}

		#region Privatec Methods

		private MsgPriority GetPriority(QueuePriority priority)
		{
			switch (priority)
			{
				case QueuePriority.Lowest:
					return MsgPriority.Lowest;
				case QueuePriority.Highest:
					return MsgPriority.Highest;
				default:
					return MsgPriority.Normal;
			}
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
			var endpoint = await _sendEndpointProvider.GetSendEndpoint(QueueHelper.GetQueueUri<TConsumer>(priority));
			await endpoint.Send<TContract>(values, x => x.SetPriority(GetPriority(priority)), cancellationToken);
		}

		public Task ScheduleSend<TContract>(DateTime scheduledTime, object values)
			where TContract : class/*, IContract*/
		{
			return ScheduleSendAsync<TContract>(scheduledTime, values);
		}

		public async Task ScheduleSendAsync<TContract>(DateTime scheduledTime, object values, CancellationToken cancellationToken = default)
			where TContract : class/*, IContract*/
		{
			await _messageScheduler.SchedulePublish<TContract>(scheduledTime, values, cancellationToken);
		}
	}
}
