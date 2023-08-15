using System;
using System.Collections.Generic;
using CommonLibraries.Services;
using Consumer3.WindowsService.Consumers;
using Contracts;
using MassTransit;
using MG.Shared.EventBus.Extensions;
using MG.Shared.EventBus.Infrastructure;
using MG.Shared.EventBus.Models;
using Microsoft.Extensions.DependencyInjection;
using Topshelf;

namespace Consumer3.WindowsService
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
                    queueName: QueueBuilder.GetQueueName<UserUpdated>(),
                    consumers: new List<Type> { typeof(UserUpdated3Consumer) }
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

                x.SetDescription("Consumer 3 Windows Service Prototype");
                x.SetDisplayName("Consumer 3 Windows Service");
                x.SetServiceName("Consumer 3 Windows Service");
            });
        }
    }
}
