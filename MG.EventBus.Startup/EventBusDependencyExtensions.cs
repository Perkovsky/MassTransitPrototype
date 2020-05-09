using GreenPipes;
using MassTransit;
using MassTransit.Definition;
using MG.EventBus.Components.Consumers;
using MG.IntegrationSystems.Tools;
using SimpleInjector;
using SimpleInjector.Lifestyles;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MG.EventBus.Startup
{
	public static class EventBusDependencyExtensions
	{
		private static string SendMailQueueName => KebabCaseEndpointNameFormatter.Instance.Consumer<SendMailConsumer>();
		private static Uri SendMailQueueUri => new Uri($"queue:{SendMailQueueName}");

		public static async Task<ISendEndpoint> GetSendMailSendEndpointAsync(this ISendEndpointProvider provider)
		{
			return await provider.GetSendEndpoint(SendMailQueueUri);
		}

		public static ISendEndpoint GetSendMailSendEndpoint(this ISendEndpointProvider provider)
		{
			return provider.GetSendMailSendEndpointAsync().Result;
		}

		/// <summary>
		/// CloudAMQP Dependency Registration Extension Method: Producer and Consumers
		/// </summary>
		/// <param name="container">SimpleInjector Container</param>
		/// <param name="queue">ONLY FOR THE CONSUMER. Name of queue for consumer registration</param>
		/// <param name="consumers">ONLY FOR THE CONSUMER. An array of consumer types for registering consumers</param>
		private static void RegisterCloudAMQPDependencies(this Container container, string queue = null, params Type[] consumers)
		{
			bool isConsumer = !string.IsNullOrEmpty(queue);
			if (isConsumer && !consumers.Any())
				throw new ArgumentException("An array of consumer types for registering consumers can't be empty.");

			var config = ConfigurationHelper.GetConfiguration("eventbussettings.json");
			string hostName = @config["CloudAMQP:HostName"];
			string vhost = @config["CloudAMQP:VirtualHost"];
			string port = @config["CloudAMQP:Port"];
			string username = @config["CloudAMQP:UserName"];
			string password = @config["CloudAMQP:Password"];

			container.Options.DefaultScopedLifestyle = new AsyncScopedLifestyle();

			container.AddMassTransit(x =>
			{
				if (isConsumer)
					x.AddConsumers(consumers);

				x.AddBus(() => Bus.Factory.CreateUsingRabbitMq(cfg =>
				{
					var host = cfg.Host(new Uri($@"rabbitmq://{hostName}:{port}/{vhost}/"), h =>
					{
						h.Username(username);
						h.Password(password);

						//h.UseSsl(s =>
						//{
						//	s.Protocol = SslProtocols.Tls12;
						//});
					});

					if (isConsumer)
					{
						cfg.ReceiveEndpoint(queue, ec =>
						{
							ec.UseMessageRetry(rp => rp.Exponential(5, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5)));
							ec.ConfigureConsumers(container);
						});
					}
				}));
			});
		}

		public static void RegisterEventBusProducerDependencies(this Container container)
		{
			container.RegisterCloudAMQPDependencies();
		}

		public static void RegisterSendMailConsumerDependencies(this Container container) 
		{
			container.RegisterCloudAMQPDependencies(SendMailQueueName, typeof(SendMailConsumer), typeof(FaultSendMailConsumer));
		}

		public static void RegisterTestSomeActionExecutedConsumerDependencies(this Container container)
		{
			var queue = KebabCaseEndpointNameFormatter.Instance.Consumer<TestSomeActionExecutedConsumer>();
			container.RegisterCloudAMQPDependencies(queue, typeof(TestSomeActionExecutedConsumer));
		}
	}
}
