using GreenPipes;
using GreenPipes.Configurators;
using MassTransit;
using MassTransit.ActiveMqTransport;
using MassTransit.Definition;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using MassTransit.SimpleInjectorIntegration;
using MG.EventBus.Components.Consumers;
using MG.EventBus.Components.Helpers;
using MG.EventBus.Components.Models;
using MG.EventBus.Components.Services;
using MG.EventBus.Components.Services.Impl;
using MG.IntegrationSystems.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SimpleInjector;
using SimpleInjector.Lifestyles;
using System;
using System.Linq;

namespace MG.EventBus.Startup
{
	public static class EventBusDependencyExtensions
	{
		#region Private Methods

		private static Container RegisterBrokerDependencies(this Container container, string queue = null, bool canUsePriority = false, params Type[] consumers)
		{
			// CHANGE THE BROKER HERE. SEE ALSO ALL OVERLOADED EXTENDED METHODS

			container.RegisterCloudAMQPDependencies(queue, canUsePriority, consumers);
			return container;
		}

		private static IServiceCollection RegisterBrokerDependencies(this IServiceCollection services, string queue = null, bool canUsePriority = false, params Type[] consumers)
		{
			// CHANGE THE BROKER HERE. SEE ALSO ALL OVERLOADED EXTENDED METHODS

			services.RegisterCloudAMQPDependencies(queue, canUsePriority, consumers);
			return services;
		}

		private static void RetryPolicy(IRetryConfigurator retry)
		{
			retry.Exponential(5, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5));
		}

		private static Container RegisterDependencies(this Container container,
			Action<ISimpleInjectorConfigurator, string, bool, Type[]> configure, 
			string queue = null,
			bool canUsePriority = false,
			params Type[] consumers)
		{
			container.Options.DefaultScopedLifestyle = new AsyncScopedLifestyle();
			container.Register<IEndpointNameFormatter>(() => KebabCaseEndpointNameFormatter.Instance, Lifestyle.Singleton);
			container.AddMassTransit(x => configure(x, queue, canUsePriority, consumers));
			return container;
		}

		private static IServiceCollection RegisterDependencies(this IServiceCollection services,
			Action<IServiceCollectionConfigurator, string, bool, Type[]> configure,
			string queue = null,
			bool canUsePriority = false,
			params Type[] consumers)
		{
			services.TryAddSingleton(KebabCaseEndpointNameFormatter.Instance);
			services.AddMassTransit(x => configure(x, queue, canUsePriority, consumers));
			return services;
		}

		private static void ReceiveEndpoint(IReceiveEndpointConfigurator configureEndpoint, IRegistration registration)
		{
			configureEndpoint.UseMessageRetry(RetryPolicy);
			configureEndpoint.ConfigureConsumers(registration);
		}

		private static void AddReceiveEndpoints(this IReceiveConfigurator cfg, IRegistration registration, string queue, bool canUsePriority)
		{
			cfg.ReceiveEndpoint(queue, ec => ReceiveEndpoint(ec, registration));

			if (canUsePriority)
			{
				cfg.ReceiveEndpoint(queue + QueueHelper.GetQueueNameSuffix(QueuePriority.Lowest), ec => ReceiveEndpoint(ec, registration));
				cfg.ReceiveEndpoint(queue + QueueHelper.GetQueueNameSuffix(QueuePriority.Highest), ec => ReceiveEndpoint(ec, registration));
			}
		}

		#region CloudAMQP

		/// <summary>
		/// CloudAMQP Dependency Registration Extension Method: Producer and Consumers
		/// </summary>
		/// <param name="configurator">Configures the container registration: SimpleInjector, Microsoft DependencyInjection, etc.</param>
		/// <param name="queue">ONLY FOR THE CONSUMER. Name of queue for consumer registration</param>
		/// <param name="consumers">ONLY FOR THE CONSUMER. An array of consumer types for registering consumers</param>
		private static void CloudAMQPConfigure<TConfigurator, TContainerContext>(TConfigurator configurator, string queue = null, bool canUsePriority = false, params Type[] consumers)
			where TConfigurator : class, IRegistrationConfigurator<TContainerContext>, IRegistrationConfigurator
			where TContainerContext : class
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

			if (isConsumer)
				configurator.AddConsumers(consumers);

			configurator.AddBus(p => Bus.Factory.CreateUsingRabbitMq(cfg =>
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
					cfg.AddReceiveEndpoints(p, queue, canUsePriority);
			}));
		}

		private static Container RegisterCloudAMQPDependencies(this Container container, string queue = null, bool canUsePriority = false, params Type[] consumers)
		{
			container.RegisterDependencies(CloudAMQPConfigure<ISimpleInjectorConfigurator, Container>, queue, canUsePriority, consumers);
			return container;
		}

		private static IServiceCollection RegisterCloudAMQPDependencies(this IServiceCollection services, string queue = null, bool canUsePriority = false, params Type[] consumers)
		{
			services.RegisterDependencies(CloudAMQPConfigure<IServiceCollectionConfigurator, IServiceProvider>, queue, canUsePriority, consumers);
			return services;
		}

		#endregion

		#region AmazonMQ

		/// <summary>
		/// AmazonMQ Dependency Registration Extension Method: Producer and Consumers
		/// </summary>
		/// <param name="configurator">Configures the container registration: SimpleInjector, Microsoft DependencyInjection, etc. </param>
		/// <param name="queue">ONLY FOR THE CONSUMER. Name of queue for consumer registration</param>
		/// <param name="consumers">ONLY FOR THE CONSUMER. An array of consumer types for registering consumers</param>
		private static void AmazonMQConfigure<TConfigurator, TContainerContext>(TConfigurator configurator, string queue = null, bool canUsePriority = false, params Type[] consumers)
			where TConfigurator : class, IRegistrationConfigurator<TContainerContext>, IRegistrationConfigurator
			where TContainerContext : class
		{
			bool isConsumer = !string.IsNullOrEmpty(queue);
			if (isConsumer && !consumers.Any())
				throw new ArgumentException("An array of consumer types for registering consumers can't be empty.");

			var config = ConfigurationHelper.GetConfiguration("eventbussettings.json");
			string hostName = @config["AmazonMQ:HostName"];
			string username = @config["AmazonMQ:UserName"];
			string password = @config["AmazonMQ:Password"];

			if (isConsumer)
				configurator.AddConsumers(consumers);

			configurator.AddBus(p => Bus.Factory.CreateUsingActiveMq(cfg =>
			{
				var host = cfg.Host(hostName, h =>
				{
					h.Username(username);
					h.Password(password);

					//h.UseSsl();
				});

				if (isConsumer)
					cfg.AddReceiveEndpoints(p, queue, canUsePriority);
			}));
		}
		
		private static Container RegisterAmazonMQDependencies(this Container container, string queue = null, bool canUsePriority = false, params Type[] consumers)
		{
			container.RegisterDependencies(AmazonMQConfigure<ISimpleInjectorConfigurator, Container>, queue, canUsePriority, consumers);
			return container;
		}

		private static IServiceCollection RegisterAmazonMQDependencies(this IServiceCollection services, string queue = null, bool canUsePriority = false, params Type[] consumers)
		{
			services.RegisterDependencies(AmazonMQConfigure<IServiceCollectionConfigurator, IServiceProvider>, queue, canUsePriority, consumers);
			return services;
		}

		#endregion

		#endregion

		public static Container RegisterEventBusProducerDependencies(this Container container)
		{
			container.RegisterBrokerDependencies()
				.Register<IEventBusProducerService, EventBusProducerService>(Lifestyle.Scoped);
			return container;
		}

		public static IServiceCollection RegisterEventBusProducerDependencies(this IServiceCollection services)
		{
			services.RegisterBrokerDependencies()
				.AddScoped<IEventBusProducerService, EventBusProducerService>();
			return services;
		}

		public static Container RegisterSendMailConsumerDependencies(this Container container) 
		{
			container.RegisterBrokerDependencies(
				queue: QueueHelper.GetQueueName<SendMailConsumer>(),
				//canUsePriority: true,
				consumers: new Type[] { typeof(SendMailConsumer), typeof(FaultSendMailConsumer) }
			);
			return container;
		}

		public static Container RegisterTestSomeActionExecutedConsumerDependencies(this Container container)
		{
			container.RegisterBrokerDependencies(
				queue: QueueHelper.GetQueueName<TestSomeActionExecutedConsumer>(),
				consumers: typeof(TestSomeActionExecutedConsumer)
			);
			return container;
		}
	}
}
