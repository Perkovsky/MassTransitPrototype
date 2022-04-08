using System;
using System.Threading.Tasks;
using Contracts;
using Microsoft.Extensions.DependencyInjection;
using MG.Shared.EventBus.Extensions;
using MG.Shared.EventBus.Infrastructure;
using MG.Shared.EventBus.Models;
using MG.Shared.EventBus.Services;

namespace Producer
{
	class Program
	{
		static readonly ServiceProvider ServiceProvider;

		static Program()
		{
			EventBusSettings SettingsProvider(IServiceProvider sp)
            {
                return new EventBusSettings
                {
                    UserName = "admin",
                    Password = "admin",
                    HostNames =  new[] {"localhost"},
                    Port = 61616
                };
            }
            EventBusErrorNotifier ErrorNotifierProvider(IServiceProvider sp) => (e, s, b) => Task.CompletedTask;

            ServiceProvider = new ServiceCollection()
				.AddEventBusProducer(SettingsProvider, ErrorNotifierProvider)
				.BuildServiceProvider();
		}

		static async Task Main(string[] args)
		{
			var producer = ServiceProvider.GetRequiredService<IEventBusProducerService>();
				
			var random = new Random();
			Console.WriteLine("-- PRODUCER --");
			Console.WriteLine("Enter message (or quit to exit)..." + Environment.NewLine);

			while (true)
			{
				Console.Write("> ");
				var msg = Console.ReadLine();
				
                if (msg.Equals("quit", StringComparison.InvariantCultureIgnoreCase) || msg.Equals("q", StringComparison.InvariantCultureIgnoreCase))
					break;

                if (msg.Equals("send", StringComparison.InvariantCultureIgnoreCase) || msg.Equals("s", StringComparison.InvariantCultureIgnoreCase))
                {
					await producer.SendAsync(new SendEmail {Email = "user@example.com", Text = "Send some text"});
					continue;
                }

                if (msg.Equals("send-highest", StringComparison.InvariantCultureIgnoreCase) || msg.Equals("sh", StringComparison.InvariantCultureIgnoreCase))
                {
                    await producer.SendAsync(new SendEmail { Email = "user@example.com", Text = "Send some text in the highest priority" }, QueuePriority.Highest);
                    continue;
                }

				if (msg.Equals("publish", StringComparison.InvariantCultureIgnoreCase) || msg.Equals("p", StringComparison.InvariantCultureIgnoreCase))
                {
                    await producer.PublishAsync(new UserUpdated {Id = 4});
                    continue;
                }

                if (msg.Equals("schedule-send", StringComparison.InvariantCultureIgnoreCase) || msg.Equals("ss", StringComparison.InvariantCultureIgnoreCase))
                {
					await producer.ScheduleSendAsync(DateTime.Now.AddMinutes(1), new SendEmail { Email = "user@example.com", Text = $"Send some text by schedule. Time: {DateTime.Now}" });
					continue;
                }

                if (msg.Equals("schedule-publish", StringComparison.InvariantCultureIgnoreCase) || msg.Equals("sp", StringComparison.InvariantCultureIgnoreCase))
                {
                    await producer.SchedulePublishAsync(DateTime.Now.AddMinutes(1), new UserUpdated { Id = 44 });
                    continue;
                }
			}
		}
	}
}
