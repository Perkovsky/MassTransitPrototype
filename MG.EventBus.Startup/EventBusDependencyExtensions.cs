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
using MG.EventBus.Startup.Models;
using MG.IntegrationSystems.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RabbitMQ.Client.Events;
using SimpleInjector;
using SimpleInjector.Lifestyles;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MG.EventBus.Startup
{
	public static class EventBusDependencyExtensions
	{
		#region Private Methods

		private static Container RegisterBrokerDependencies(this Container container, IEnumerable<ReceiveEndpointRegistration> receiveEndpoints = null)
		{
			// CHANGE THE BROKER HERE. SEE ALSO ALL OVERLOADED EXTENDED METHODS

			container.RegisterCloudAMQPDependencies(receiveEndpoints);
			return container;
		}

		private static IServiceCollection RegisterBrokerDependencies(this IServiceCollection services, IEnumerable<ReceiveEndpointRegistration> receiveEndpoints = null)
		{
			// CHANGE THE BROKER HERE. SEE ALSO ALL OVERLOADED EXTENDED METHODS

			services.RegisterCloudAMQPDependencies(receiveEndpoints);
			return services;
		}

		private static void RetryPolicy(IRetryConfigurator retry)
		{
			retry.Exponential(5, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5));
		}

		private static Container RegisterDependencies(this Container container,
			Action<ISimpleInjectorConfigurator, IEnumerable<ReceiveEndpointRegistration>> configure,
			IEnumerable<ReceiveEndpointRegistration> receiveEndpoints)
		{
			container.Options.DefaultScopedLifestyle = new AsyncScopedLifestyle();
			container.Register<IEndpointNameFormatter>(() => KebabCaseEndpointNameFormatter.Instance, Lifestyle.Singleton);
			container.AddMassTransit(x => configure(x, receiveEndpoints));
			return container;
		}

		private static IServiceCollection RegisterDependencies(this IServiceCollection services,
			Action<IServiceCollectionConfigurator, IEnumerable<ReceiveEndpointRegistration>> configure,
			IEnumerable<ReceiveEndpointRegistration> receiveEndpoints)
		{
			services.TryAddSingleton(KebabCaseEndpointNameFormatter.Instance);
			services.AddMassTransit(x => configure(x, receiveEndpoints));
			return services;
		}

		private static Type[] GetConsumers(IEnumerable<ReceiveEndpointRegistration> receiveEndpoints)
		{
			var result = new List<Type>();
			foreach (var receiveEndpoint in receiveEndpoints)
			{
				result.AddRange(receiveEndpoint.Consumers);
			}
			return result.ToArray();
		}

		private static void ReceiveEndpoint(IReceiveEndpointConfigurator configureEndpoint, IRegistration registration, IEnumerable<Type> consumers)
		{
			configureEndpoint.UseMessageRetry(RetryPolicy);
			foreach (var consumer in consumers)
			{
				configureEndpoint.ConfigureConsumer(registration, consumer);
			}
		}

		private static void AddReceiveEndpoints(this IReceiveConfigurator cfg, IRegistration registration, IEnumerable<ReceiveEndpointRegistration> receiveEndpoints)
		{
			foreach (var receiveEndpoint in receiveEndpoints)
			{
				cfg.ReceiveEndpoint(receiveEndpoint.QueueName, ec => ReceiveEndpoint(ec, registration, receiveEndpoint.Consumers));

				if (receiveEndpoint.CanUsePriority)
				{
					cfg.ReceiveEndpoint(receiveEndpoint.QueueName + QueueHelper.GetQueueNameSuffix(QueuePriority.Lowest), 
						ec => ReceiveEndpoint(ec, registration, receiveEndpoint.Consumers));

					cfg.ReceiveEndpoint(receiveEndpoint.QueueName + QueueHelper.GetQueueNameSuffix(QueuePriority.Highest),
						ec => ReceiveEndpoint(ec, registration, receiveEndpoint.Consumers));
				}
			}
		}

		#region CloudAMQP

		/// <summary>
		/// CloudAMQP Dependency Registration Extension Method: Producer and Consumers
		/// </summary>
		/// <param name="configurator">Configures the container registration: SimpleInjector, Microsoft DependencyInjection, etc.</param>
		/// <param name="receiveEndpoints">ONLY FOR THE CONSUMERS. Receive Endpoints list</param>
		private static void CloudAMQPConfigure<TConfigurator, TContainerContext>(TConfigurator configurator, IEnumerable<ReceiveEndpointRegistration> receiveEndpoints)
			where TConfigurator : class, IRegistrationConfigurator<TContainerContext>, IRegistrationConfigurator
			where TContainerContext : class
		{
			bool isConsumer = receiveEndpoints?.Any() ?? false;

			var config = ConfigurationHelper.GetConfiguration("eventbussettings.json");
			string hostName = @config["CloudAMQP:HostName"];
			string vhost = @config["CloudAMQP:VirtualHost"];
			string port = @config["CloudAMQP:Port"];
			string username = @config["CloudAMQP:UserName"];
			string password = @config["CloudAMQP:Password"];

			if (isConsumer)
				configurator.AddConsumers(GetConsumers(receiveEndpoints));

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
					cfg.AddReceiveEndpoints(p, receiveEndpoints);
			}));
		}

		private static Container RegisterCloudAMQPDependencies(this Container container, IEnumerable<ReceiveEndpointRegistration> receiveEndpoints)
		{
			container.RegisterDependencies(CloudAMQPConfigure<ISimpleInjectorConfigurator, Container>, receiveEndpoints);
			return container;
		}

		private static IServiceCollection RegisterCloudAMQPDependencies(this IServiceCollection services, IEnumerable<ReceiveEndpointRegistration> receiveEndpoints)
		{
			services.RegisterDependencies(CloudAMQPConfigure<IServiceCollectionConfigurator, IServiceProvider>, receiveEndpoints);
			return services;
		}

		#endregion

		#region AmazonMQ

		/// <summary>
		/// AmazonMQ Dependency Registration Extension Method: Producer and Consumers
		/// </summary>
		/// <param name="configurator">Configures the container registration: SimpleInjector, Microsoft DependencyInjection, etc. </param>
		/// <param name="receiveEndpoints">ONLY FOR THE CONSUMERS. Receive Endpoints list</param>
		private static void AmazonMQConfigure<TConfigurator, TContainerContext>(TConfigurator configurator, IEnumerable<ReceiveEndpointRegistration> receiveEndpoints)
			where TConfigurator : class, IRegistrationConfigurator<TContainerContext>, IRegistrationConfigurator
			where TContainerContext : class
		{
			bool isConsumer = receiveEndpoints?.Any() ?? false;

			var config = ConfigurationHelper.GetConfiguration("eventbussettings.json");
			string hostName = @config["AmazonMQ:HostName"];
			string username = @config["AmazonMQ:UserName"];
			string password = @config["AmazonMQ:Password"];

			if (isConsumer)
				configurator.AddConsumers(GetConsumers(receiveEndpoints));

			configurator.AddBus(p => Bus.Factory.CreateUsingActiveMq(cfg =>
			{
				var host = cfg.Host(hostName, h =>
				{
					h.Username(username);
					h.Password(password);

					//h.UseSsl();
				});

				if (isConsumer)
					cfg.AddReceiveEndpoints(p, receiveEndpoints);
			}));
		}
		
		private static Container RegisterAmazonMQDependencies(this Container container, IEnumerable<ReceiveEndpointRegistration> receiveEndpoints)
		{
			container.RegisterDependencies(AmazonMQConfigure<ISimpleInjectorConfigurator, Container>, receiveEndpoints);
			return container;
		}

		private static IServiceCollection RegisterAmazonMQDependencies(this IServiceCollection services, IEnumerable<ReceiveEndpointRegistration> receiveEndpoints)
		{
			services.RegisterDependencies(AmazonMQConfigure<IServiceCollectionConfigurator, IServiceProvider>, receiveEndpoints);
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
			var receiveEndpoints = new List<ReceiveEndpointRegistration>
			{
				new ReceiveEndpointRegistration
				{
					QueueName = QueueHelper.GetQueueName<SendMailConsumer>(),
					CanUsePriority = true,
					Consumers = new List<Type> { typeof(SendMailConsumer), typeof(FaultSendMailConsumer) }
				}
			};

			container.RegisterBrokerDependencies(receiveEndpoints);
			return container;
		}

		public static Container RegisterTestSomeActionExecutedConsumerDependencies(this Container container)
		{
			var receiveEndpoints = new List<ReceiveEndpointRegistration>
			{
				new ReceiveEndpointRegistration
				{
					QueueName = QueueHelper.GetQueueName<TestSomeActionExecutedConsumer>(),
					Consumers = new List<Type> { typeof(TestSomeActionExecutedConsumer) }
				}
			};

			container.RegisterBrokerDependencies(receiveEndpoints);
			return container;
		}
	}
}
