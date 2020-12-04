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
using System.Security.Authentication;
using System.Threading.Tasks;

namespace MG.EventBus.Startup
{
	public static class EventBusDependencyExtensions
	{
		#region Private Methods

		private static readonly Func<Exception, string, string, Task> _mockErrorNotifier = async (e, s, d) => await Task.CompletedTask;

		private static Container RegisterBrokerDependencies(this Container container,
			EventBusSettings settings,
			Func<Exception, string, string, Task> errorNotifier,
			IEnumerable<ReceiveEndpointRegistration> receiveEndpoints = null)
		{
			// CHANGE THE BROKER HERE. SEE ALSO ALL OVERLOADED EXTENDED METHODS

			container.RegisterDependencies(settings, errorNotifier, Configure<ISimpleInjectorBusConfigurator, Container>, receiveEndpoints, AmazonMQConfigureBus);
			return container;
		}

		private static IServiceCollection RegisterBrokerDependencies(this IServiceCollection services,
			EventBusSettings settings,
			Func<Exception, string, string, Task> errorNotifier,
			IEnumerable<ReceiveEndpointRegistration> receiveEndpoints = null)
		{
			// CHANGE THE BROKER HERE. SEE ALSO ALL OVERLOADED EXTENDED METHODS

			services.RegisterDependencies(settings, errorNotifier, Configure<IServiceCollectionBusConfigurator, IServiceProvider>, receiveEndpoints, AmazonMQConfigureBus);
			return services;
		}

		private static void RetryPolicy(IRetryConfigurator retry, RetryPolicySettings retryPolicy)
		{
			//retry.ConnectRetryObserver(new RetryObserver());
#if DEBUG
			retry.Interval(5, TimeSpan.FromSeconds(10));
#else
			int retryCount = retryPolicy?.RetryCount > 0 ? retryPolicy.RetryCount : 5;
			int interval = retryPolicy?.Interval > 0 ? retryPolicy.Interval : 5;
			retry.Interval(retryCount, TimeSpan.FromMinutes(interval));
#endif
		}

		private static Container RegisterDependencies(this Container container,
			EventBusSettings settings,
			Func<Exception, string, string, Task> errorNotifier,
			Action<ISimpleInjectorBusConfigurator,
				EventBusSettings,
				Func<Exception, string, string, Task>,
				IEnumerable<ReceiveEndpointRegistration>,
				Func<IRegistration, IEnumerable<ReceiveEndpointRegistration>, bool, EventBusSettings, IBusControl>> configure,
			IEnumerable<ReceiveEndpointRegistration> receiveEndpoints,
			Func<IRegistration, IEnumerable<ReceiveEndpointRegistration>, bool, EventBusSettings, IBusControl> configureBus)
		{
			container.Options.DefaultScopedLifestyle = new AsyncScopedLifestyle();
			container.Register<IEndpointNameFormatter>(() => KebabCaseEndpointNameFormatter.Instance, Lifestyle.Singleton);
			container.AddMassTransit(x =>
			{
				x.AddActiveMqMessageScheduler();
				configure(x, settings, errorNotifier, receiveEndpoints, configureBus);
			});
			return container;
		}

		private static IServiceCollection RegisterDependencies(this IServiceCollection services,
			EventBusSettings settings,
			Func<Exception, string, string, Task> errorNotifier,
			Action<IServiceCollectionBusConfigurator,
				EventBusSettings,
				Func<Exception, string, string, Task>,
				IEnumerable<ReceiveEndpointRegistration>,
				Func<IRegistration, IEnumerable<ReceiveEndpointRegistration>, bool, EventBusSettings, IBusControl>> configure,
			IEnumerable<ReceiveEndpointRegistration> receiveEndpoints,
			Func<IRegistration, IEnumerable<ReceiveEndpointRegistration>, bool, EventBusSettings, IBusControl> configureBus)
		{
			services.TryAddSingleton(KebabCaseEndpointNameFormatter.Instance);
			services.AddMassTransit(x =>
			{
				x.AddActiveMqMessageScheduler();
				configure(x, settings, errorNotifier, receiveEndpoints, configureBus);
			});
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
			RetryPolicySettings retryPolicy,
			IEnumerable<Type> consumers,
			IEnumerable<Type> faultConsumers = null,
			int? concurrencyLimit = null)
		{
			((IActiveMqReceiveEndpointConfigurator)configureEndpoint).PrefetchCount = 30;

			//if (retryPolicy != null)
			//	configureEndpoint.UseMessageRetry(retry => RetryPolicy(retry, retryPolicy));

			//configureEndpoint.ConfigureConsumeTopology = false;

			configureEndpoint.DiscardFaultedMessages();
			configureEndpoint.DiscardSkippedMessages();

			if (concurrencyLimit.HasValue)
				configureEndpoint.UseConcurrencyLimit(concurrencyLimit.Value);

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

		private static void AddReceiveEndpoints(this IReceiveConfigurator cfg,
			IRegistration registration,
			IEnumerable<ReceiveEndpointRegistration> receiveEndpoints,
			RetryPolicySettings retryPolicy)
		{
			foreach (var receiveEndpoint in receiveEndpoints)
			{
				cfg.ReceiveEndpoint(receiveEndpoint.QueueName,
					ec => ReceiveEndpoint(ec, registration, retryPolicy, receiveEndpoint.Consumers, receiveEndpoint.FaultConsumers));

				if (receiveEndpoint.CanUsePriority)
				{
					cfg.ReceiveEndpoint(receiveEndpoint.QueueName + QueueHelper.GetQueueNameSuffix(QueuePriority.Lowest),
						ec => ReceiveEndpoint(ec, registration, retryPolicy, receiveEndpoint.Consumers));

					cfg.ReceiveEndpoint(receiveEndpoint.QueueName + QueueHelper.GetQueueNameSuffix(QueuePriority.Highest),
						ec => ReceiveEndpoint(ec, registration, retryPolicy, receiveEndpoint.Consumers));
				}
			}
		}

		private static void Configure<TConfigurator, TContainerContext>(TConfigurator configurator,
			EventBusSettings settings,
			Func<Exception, string, string, Task> errorNotifier,
			IEnumerable<ReceiveEndpointRegistration> receiveEndpoints,
			Func<IRegistration, IEnumerable<ReceiveEndpointRegistration>, bool, EventBusSettings, IBusControl> configureBus)
				where TConfigurator : class, IBusRegistrationConfigurator, IRegistrationConfigurator
				where TContainerContext : class
		{
			bool isConsumer = receiveEndpoints?.Any() ?? false;

			if (isConsumer)
				configurator.AddConsumers(GetConsumers(receiveEndpoints));

			if (settings.UseInMemory)
				configurator.AddBus(p => InMemoryConfigureBus(p));
			else
				configurator.AddBus(p => {
					var bus = configureBus(p, receiveEndpoints, isConsumer, settings);
					//bus.ConnectReceiveEndpointObserver(new ReceiveEndpointObserver(errorNotifier));
					//bus.ConnectReceiveObserver(new ReceiveObserver());
					//bus.ConnectPublishObserver(new PublishObserver());
					//bus.ConnectSendObserver(new SendObserver());
					return bus;
				});
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
				cfg.Host(new Uri($@"rabbitmq://{hostName}:{port}/{vhost}/"), h =>
				{
					h.Username(username);
					h.Password(password);

					//h.UseSsl(s =>
					//{
					//	s.Protocol = SslProtocols.Tls12;
					//});
				});

				if (isConsumer)
					cfg.AddReceiveEndpoints(registration, receiveEndpoints, settings.RetryPolicy);
			});
		}

		private static IBusControl AmazonMQConfigureBus(IRegistration registration,
			IEnumerable<ReceiveEndpointRegistration> receiveEndpoints,
			bool isConsumer,
			EventBusSettings settings)
		{
			string[] hostNames = settings.AmazonMQ?.HostNames?.ToArray();
			string username = settings.AmazonMQ?.UserName;
			string password = settings.AmazonMQ?.Password;
			int? port = settings.AmazonMQ?.Port;

			if (hostNames == null || hostNames.Length < 1 || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || !port.HasValue)
				throw new InvalidCredentialException("AmazonMQ credentials in a wrong state");

			return Bus.Factory.CreateUsingActiveMq(cfg =>
			{
				cfg.UseActiveMqMessageScheduler();

				cfg.Host(hostNames[0], port.Value, h =>
				{
					h.Username(username);
					h.Password(password);

					//h.UseSsl();

					if (hostNames.Length > 1)
						h.FailoverHosts(hostNames);
				});

				if (isConsumer)
					cfg.AddReceiveEndpoints(registration, receiveEndpoints, settings.RetryPolicy);
			});
		}

		#endregion

		public static Container RegisterEventBusProducerDependencies(this Container container,
			EventBusSettings settings)
		{
			container.RegisterBrokerDependencies(settings, _mockErrorNotifier)
				.Register<IEventBusProducerService, EventBusProducerService>(Lifestyle.Scoped);
			return container;
		}

		public static IServiceCollection RegisterEventBusProducerDependencies(this IServiceCollection services,
			EventBusSettings settings)
		{
			services.RegisterBrokerDependencies(settings, _mockErrorNotifier)
				.AddScoped<IEventBusProducerService, EventBusProducerService>();
			return services;
		}

		public static Container RegisterSendMailConsumerDependencies(this Container container,
			EventBusSettings settings,
			Func<Exception, string, string, Task> errorNotifier)
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

			container.RegisterBrokerDependencies(settings, errorNotifier, receiveEndpoints);
			return container;
		}

		public static Container RegisterTestSomeActionExecutedConsumerDependencies(this Container container,
			EventBusSettings settings,
			Func<Exception, string, string, Task> errorNotifier)
		{
			var receiveEndpoints = new List<ReceiveEndpointRegistration>
			{
				new ReceiveEndpointRegistration(
					queueName: QueueHelper.GetQueueName<TestSomeActionExecutedConsumer>(),
					consumers: new List<Type> { typeof(TestSomeActionExecutedConsumer) }
				)
			};

			container.RegisterBrokerDependencies(settings, errorNotifier, receiveEndpoints);
			return container;
		}
	}
}
