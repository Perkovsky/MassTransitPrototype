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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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

		private static Container RegisterBrokerDependencies(this Container container,
			EventBusSettings settings,
			IEnumerable<ReceiveEndpointRegistration> receiveEndpoints = null)
		{
			// CHANGE THE BROKER HERE. SEE ALSO ALL OVERLOADED EXTENDED METHODS

			container.RegisterDependencies(settings, Configure<ISimpleInjectorConfigurator, Container>, receiveEndpoints, AmazonMQConfigureBus);
			return container;
		}

		private static IServiceCollection RegisterBrokerDependencies(this IServiceCollection services,
			EventBusSettings settings,
			IEnumerable<ReceiveEndpointRegistration> receiveEndpoints = null)
		{
			// CHANGE THE BROKER HERE. SEE ALSO ALL OVERLOADED EXTENDED METHODS

			services.RegisterDependencies(settings, Configure<IServiceCollectionConfigurator, IServiceProvider>, receiveEndpoints, AmazonMQConfigureBus);
			return services;
		}

		private static void RetryPolicy(IRetryConfigurator retry)
		{
			//retry.Exponential(5, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5));
			retry.Interval(5, TimeSpan.FromSeconds(1));
		}

		private static Container RegisterDependencies(this Container container,
			EventBusSettings settings,
			Action<ISimpleInjectorConfigurator,
				EventBusSettings,
				IEnumerable<ReceiveEndpointRegistration>,
				Func<IRegistration, IEnumerable<ReceiveEndpointRegistration>, bool, EventBusSettings, IBusControl>> configure,
			IEnumerable<ReceiveEndpointRegistration> receiveEndpoints,
			Func<IRegistration, IEnumerable<ReceiveEndpointRegistration>, bool, EventBusSettings, IBusControl> configureBus)
		{
			container.Options.DefaultScopedLifestyle = new AsyncScopedLifestyle();
			container.Register<IEndpointNameFormatter>(() => KebabCaseEndpointNameFormatter.Instance, Lifestyle.Singleton);
			container.AddMassTransit(x => configure(x, settings, receiveEndpoints, configureBus));
			return container;
		}

		private static IServiceCollection RegisterDependencies(this IServiceCollection services,
			EventBusSettings settings,
			Action<IServiceCollectionConfigurator,
				EventBusSettings,
				IEnumerable<ReceiveEndpointRegistration>,
				Func<IRegistration, IEnumerable<ReceiveEndpointRegistration>, bool, EventBusSettings, IBusControl>> configure,
			IEnumerable<ReceiveEndpointRegistration> receiveEndpoints,
			Func<IRegistration, IEnumerable<ReceiveEndpointRegistration>, bool, EventBusSettings, IBusControl> configureBus)
		{
			services.TryAddSingleton(KebabCaseEndpointNameFormatter.Instance);
			services.AddMassTransit(x => configure(x, settings, receiveEndpoints, configureBus));
			return services;
		}

		private static Type[] GetConsumers(IEnumerable<ReceiveEndpointRegistration> receiveEndpoints)
		{
			var result = new List<Type>();
			foreach (var receiveEndpoint in receiveEndpoints)
			{
				result.AddRange(receiveEndpoint.Consumers);
				result.AddRange(receiveEndpoint.FaultConsumers);
			}
			return result.Distinct().ToArray();
		}

		private static void ReceiveEndpoint(IReceiveEndpointConfigurator configureEndpoint,
			IRegistration registration,
			IEnumerable<Type> consumers,
			IEnumerable<Type> faultConsumers = null)
		{
			configureEndpoint.UseMessageRetry(RetryPolicy);

			foreach (var consumer in consumers)
			{
				configureEndpoint.ConfigureConsumer(registration, consumer);
			}

			if (faultConsumers == null) return;
			foreach (var faultConsumer in faultConsumers)
			{
				configureEndpoint.ConfigureConsumer(registration, faultConsumer);
			}
		}

		private static void AddReceiveEndpoints(this IReceiveConfigurator cfg, IRegistration registration, IEnumerable<ReceiveEndpointRegistration> receiveEndpoints)
		{
			foreach (var receiveEndpoint in receiveEndpoints)
			{
				//TODO: configure activemq.xml -> destinationPolicy to use queue priority
				// see: https://activemq.apache.org/how-can-i-support-priority-queues

				cfg.ReceiveEndpoint(receiveEndpoint.QueueName,
					ec => ReceiveEndpoint(ec, registration, receiveEndpoint.Consumers, receiveEndpoint.FaultConsumers));

				if (receiveEndpoint.CanUsePriority)
				{
					cfg.ReceiveEndpoint(receiveEndpoint.QueueName + QueueHelper.GetQueueNameSuffix(QueuePriority.Lowest),
						ec => ReceiveEndpoint(ec, registration, receiveEndpoint.Consumers));

					cfg.ReceiveEndpoint(receiveEndpoint.QueueName + QueueHelper.GetQueueNameSuffix(QueuePriority.Highest),
						ec => ReceiveEndpoint(ec, registration, receiveEndpoint.Consumers));
				}
			}
		}

		private static void Configure<TConfigurator, TContainerContext>(TConfigurator configurator,
			EventBusSettings settings,
			IEnumerable<ReceiveEndpointRegistration> receiveEndpoints,
			Func<IRegistration, IEnumerable<ReceiveEndpointRegistration>, bool, EventBusSettings, IBusControl> configureBus)
				where TConfigurator : class, IRegistrationConfigurator<TContainerContext>, IRegistrationConfigurator
				where TContainerContext : class
		{
			bool isConsumer = receiveEndpoints?.Any() ?? false;

			if (isConsumer)
				configurator.AddConsumers(GetConsumers(receiveEndpoints));

			if (settings.UseInMemory)
				configurator.AddBus(p => InMemoryConfigureBus(p));
			else
				configurator.AddBus(p => configureBus(p, receiveEndpoints, isConsumer, settings));
		}

		/// <summary>
		/// This stub method is intended for developers who do not use a real AMQP broker.
		/// </summary>
		/// <param name="registration">This parameter is needed only to match the signature</param>
		/// <returns></returns>
		private static IBusControl InMemoryConfigureBus(IRegistration registration)
		{
			return Bus.Factory.CreateUsingInMemory(cfg => { });
		}

		private static IBusControl CloudAMQPConfigureBus(IRegistration registration,
			IEnumerable<ReceiveEndpointRegistration> receiveEndpoints,
			bool isConsumer,
			EventBusSettings settings)
		{
			string hostName = settings.CloudAMQP?.HostName;
			string vhost = settings.CloudAMQP?.VirtualHost;
			string port = settings.CloudAMQP?.Port;
			string username = settings.CloudAMQP?.UserName;
			string password = settings.CloudAMQP?.Password;

			return Bus.Factory.CreateUsingRabbitMq(cfg =>
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
					cfg.AddReceiveEndpoints(registration, receiveEndpoints);
			});
		}

		private static IBusControl AmazonMQConfigureBus(IRegistration registration,
			IEnumerable<ReceiveEndpointRegistration> receiveEndpoints,
			bool isConsumer,
			EventBusSettings settings)
		{
			string hostName = settings.AmazonMQ?.HostName;
			string username = settings.AmazonMQ?.UserName;
			string password = settings.AmazonMQ?.Password;

			return Bus.Factory.CreateUsingActiveMq(cfg =>
			{
				var host = cfg.Host(hostName, h =>
				{
					h.Username(username);
					h.Password(password);

					//h.UseSsl();
				});

				if (isConsumer)
					cfg.AddReceiveEndpoints(registration, receiveEndpoints);
			});
		}

		#endregion

		public static Container RegisterEventBusProducerDependencies(this Container container, EventBusSettings settings)
		{
			container.RegisterBrokerDependencies(settings)
				.Register<IEventBusProducerService, EventBusProducerService>(Lifestyle.Scoped);
			return container;
		}

		public static IServiceCollection RegisterEventBusProducerDependencies(this IServiceCollection services, EventBusSettings settings)
		{
			services.RegisterBrokerDependencies(settings)
				.AddScoped<IEventBusProducerService, EventBusProducerService>();
			return services;
		}

		public static Container RegisterSendMailConsumerDependencies(this Container container, EventBusSettings settings) 
		{
			var receiveEndpoints = new List<ReceiveEndpointRegistration>
			{
				new ReceiveEndpointRegistration(
					queueName: QueueHelper.GetQueueName<SendMailConsumer>(),
					consumers: new List<Type> { typeof(SendMailConsumer) },
					faultConsumers: new List<Type> { typeof(FaultSendMailConsumer) }
					//canUsePriority: true
				)
			};

			container.RegisterBrokerDependencies(settings, receiveEndpoints);
			return container;
		}

		public static Container RegisterTestSomeActionExecutedConsumerDependencies(this Container container, EventBusSettings settings)
		{
			var receiveEndpoints = new List<ReceiveEndpointRegistration>
			{
				new ReceiveEndpointRegistration(
					queueName: QueueHelper.GetQueueName<TestSomeActionExecutedConsumer>(),
					consumers: new List<Type> { typeof(TestSomeActionExecutedConsumer) }
				)
			};

			container.RegisterBrokerDependencies(settings, receiveEndpoints);
			return container;
		}
	}
}
