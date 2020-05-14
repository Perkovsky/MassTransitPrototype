﻿using GreenPipes;
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

			container.RegisterDependencies(Configure<ISimpleInjectorConfigurator, Container>, receiveEndpoints, AmazonMQConfigureBus);
			return container;
		}

		private static IServiceCollection RegisterBrokerDependencies(this IServiceCollection services, IEnumerable<ReceiveEndpointRegistration> receiveEndpoints = null)
		{
			// CHANGE THE BROKER HERE. SEE ALSO ALL OVERLOADED EXTENDED METHODS

			services.RegisterDependencies(Configure<IServiceCollectionConfigurator, IServiceProvider>, receiveEndpoints, AmazonMQConfigureBus);
			return services;
		}

		private static void RetryPolicy(IRetryConfigurator retry)
		{
			//retry.Exponential(5, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5));
			retry.Interval(5, TimeSpan.FromSeconds(1));
		}

		private static Container RegisterDependencies(this Container container,
			Action<ISimpleInjectorConfigurator, 
				IEnumerable<ReceiveEndpointRegistration>,
				Func<IRegistration, IEnumerable<ReceiveEndpointRegistration>, bool, IBusControl>> configure,
			IEnumerable<ReceiveEndpointRegistration> receiveEndpoints,
			Func<IRegistration, IEnumerable<ReceiveEndpointRegistration>, bool, IBusControl> configureBus)
		{
			container.Options.DefaultScopedLifestyle = new AsyncScopedLifestyle();
			container.Register<IEndpointNameFormatter>(() => KebabCaseEndpointNameFormatter.Instance, Lifestyle.Singleton);
			container.AddMassTransit(x => configure(x, receiveEndpoints, configureBus));
			return container;
		}

		private static IServiceCollection RegisterDependencies(this IServiceCollection services,
			Action<IServiceCollectionConfigurator,
				IEnumerable<ReceiveEndpointRegistration>,
				Func<IRegistration, IEnumerable<ReceiveEndpointRegistration>, bool, IBusControl>> configure,
			IEnumerable<ReceiveEndpointRegistration> receiveEndpoints,
			Func<IRegistration, IEnumerable<ReceiveEndpointRegistration>, bool, IBusControl> configureBus)
		{
			services.TryAddSingleton(KebabCaseEndpointNameFormatter.Instance);
			services.AddMassTransit(x => configure(x, receiveEndpoints, configureBus));
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
			IEnumerable<ReceiveEndpointRegistration> receiveEndpoints,
			Func<IRegistration, IEnumerable<ReceiveEndpointRegistration>, bool, IBusControl> configureBus)
				where TConfigurator : class, IRegistrationConfigurator<TContainerContext>, IRegistrationConfigurator
				where TContainerContext : class
		{
			bool isConsumer = receiveEndpoints?.Any() ?? false;

			if (isConsumer)
				configurator.AddConsumers(GetConsumers(receiveEndpoints));

			configurator.AddBus(p => configureBus(p, receiveEndpoints, isConsumer));
		}

		private static IBusControl CloudAMQPConfigureBus(IRegistration registration, IEnumerable<ReceiveEndpointRegistration> receiveEndpoints, bool isConsumer)
		{
			var config = ConfigurationHelper.GetConfiguration("eventbussettings.json");
			string hostName = @config["CloudAMQP:HostName"];
			string vhost = @config["CloudAMQP:VirtualHost"];
			string port = @config["CloudAMQP:Port"];
			string username = @config["CloudAMQP:UserName"];
			string password = @config["CloudAMQP:Password"];

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

		private static IBusControl AmazonMQConfigureBus(IRegistration registration, IEnumerable<ReceiveEndpointRegistration> receiveEndpoints, bool isConsumer)
		{
			var config = ConfigurationHelper.GetConfiguration("eventbussettings.json");
			string hostName = @config["AmazonMQ:HostName"];
			string username = @config["AmazonMQ:UserName"];
			string password = @config["AmazonMQ:Password"];

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
				new ReceiveEndpointRegistration(
					queueName: QueueHelper.GetQueueName<SendMailConsumer>(),
					consumers: new List<Type> { typeof(SendMailConsumer) },
					faultConsumers: new List<Type> { typeof(FaultSendMailConsumer) },
					canUsePriority: true
				)
			};

			container.RegisterBrokerDependencies(receiveEndpoints);
			return container;
		}

		public static Container RegisterTestSomeActionExecutedConsumerDependencies(this Container container)
		{
			var receiveEndpoints = new List<ReceiveEndpointRegistration>
			{
				new ReceiveEndpointRegistration(
					queueName: QueueHelper.GetQueueName<TestSomeActionExecutedConsumer>(),
					consumers: new List<Type> { typeof(TestSomeActionExecutedConsumer) }
				)
			};

			container.RegisterBrokerDependencies(receiveEndpoints);
			return container;
		}
	}
}
