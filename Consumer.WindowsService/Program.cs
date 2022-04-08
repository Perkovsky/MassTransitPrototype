using System;
using System.Collections.Generic;
using Consumer.WindowsService.Consumers;
using Consumer.WindowsService.Services;
using Contracts;
using MassTransit;
using MG.Shared.EventBus.Extensions;
using MG.Shared.EventBus.Infrastructure;
using MG.Shared.EventBus.Models;
using Microsoft.Extensions.DependencyInjection;
using Topshelf;

namespace Consumer.WindowsService
{
	class Program
	{
        static readonly ServiceProvider ServiceProvider;

        static Program()
        {
            EventBusSettings SettingsProvider(IServiceProvider sp)
            {
                var settingsAccessor = sp.GetRequiredService<ISystemSettingsAccessor>();
                return settingsAccessor.GetEventBusSettings();
            }

            EventBusErrorNotifier ErrorNotifierProvider(IServiceProvider sp)
            {
                var emailSendService = sp.GetRequiredService<IEmailSendService>();
                return emailSendService.SendCriticalEmailAsync;
            }

            var receiveEndpoints = new List<ReceiveEndpointRegistration>
            {
                new ReceiveEndpointRegistration(
                    queueName: QueueBuilder.GetQueueName<SendEmail>(),
                    consumers: new List<Type> { typeof(SendEmailConsumer) },
                    canUsePriority: true
                ),
                new ReceiveEndpointRegistration(
                    queueName: QueueBuilder.GetQueueName<UserUpdated>(),
                    consumers: new List<Type> { typeof(UserUpdatedConsumer) }
                ),
            };

            ServiceProvider = new ServiceCollection()
                .AddSingleton<ISystemSettingsAccessor, SystemSettingsAccessor>()
                .AddSingleton<IEmailSendService, EmailSendService>()
                .AddEventBusConsumers(SettingsProvider, ErrorNotifierProvider, receiveEndpoints)
                .BuildServiceProvider();
        }

        static void Main(string[] args)
        {
            var bus = ServiceProvider.GetRequiredService<IBusControl>();

            HostFactory.Run(x =>
            {
                x.Service<Worker>(s =>
                {
                    s.ConstructUsing(name => new Worker(bus));
                    s.WhenStarted(c => c.Start());
                    s.WhenStopped(c => c.Stop());
                });
                x.RunAsLocalSystem();

                x.SetDescription("Consumer Windows Service Prototype");
                x.SetDisplayName("Consumer Windows Service");
                x.SetServiceName("Consumer Windows Service");
            });
        }
    }
}
