﻿using MG.EventBus.Components.Consumers;
using MG.EventBus.Components.Models;
using MG.EventBus.Components.Services;
using MG.EventBus.Contracts;
using MG.EventBus.Startup;
using Microsoft.Extensions.DependencyInjection;
using Settings.Stub;
using SimpleInjector;
using SimpleInjector.Lifestyles;
using System;
using System.Threading.Tasks;

namespace Producer
{
	class Program
	{
		const string BULK_MARKER = "bulk";
		const string LOW_MARKER = "low";

		static readonly Container _container;
		static readonly ServiceProvider _serviceProvider;

		static Program()
		{
			#region SimpleInjector

			//_container = new Container();
			//_container.RegisterEventBusProducerDependencies(SettingsStub.GetSetting());
			//_container.Verify();

			#endregion

			#region Microsoft DependencyInjection

			_serviceProvider = new ServiceCollection()
				.RegisterEventBusProducerDependencies(SettingsStub.GetSetting())
				.BuildServiceProvider();

			#endregion
		}

		#region Testing without DI

		//static void Main(string[] args)
		//{
		//	Console.WriteLine("-- PRODUCER --");
		//	Console.WriteLine("Enter message (or quit to exit)..." + Environment.NewLine);

		//	var bus = Bus.Factory.CreateUsingRabbitMq(cfg =>
		//	{
		//		var host = cfg.Host(new Uri(@"rabbitmq://barnacle.rmq.cloudamqp.com:5672/oklnbiuq/"), h =>
		//		{
		//			h.Username("oklnbiuq");
		//			h.Password("OD0mXJWCHtbXBt8JADrigXSlebXNORMA");

		//			//h.UseSsl(s =>
		//			//{
		//			//	s.Protocol = System.Security.Authentication.SslProtocols.Tls12;
		//			//});
		//		});
		//	});
		//	bus.Start();

		//	while (true)
		//	{
		//		Console.Write("> ");
		//		string msg = Console.ReadLine();
		//		if (msg.Equals("quit", StringComparison.InvariantCultureIgnoreCase))
		//			break;

		//		bus.Publish<SendMail>(new
		//		{
		//			CreatedDate = DateTime.UtcNow,
		//			Message = msg
		//		});

		//		bus.Publish<TestSomeActionExecuted>(new
		//		{
		//			CreatedDate = DateTime.UtcNow,
		//			Message = msg + "--test"
		//		});
		//	}
		//	bus.Stop();
		//}

		#endregion

		static void Main(string[] args)
		{
			//using (AsyncScopedLifestyle.BeginScope(container))
			//{ 
			//	var producer = _container.GetInstance<IEventBusProducerService>();

			var producer = _serviceProvider.GetService<IEventBusProducerService>();
				
			var random = new Random();
			Console.WriteLine("-- PRODUCER --");
			Console.WriteLine("Enter message (or quit to exit)..." + Environment.NewLine);

			while (true)
			{
				Console.Write("> ");
				string msg = Console.ReadLine();
				if (msg.Equals("quit", StringComparison.InvariantCultureIgnoreCase))
					break;

				if (msg.Equals(BULK_MARKER, StringComparison.InvariantCultureIgnoreCase))
				{
					Task.Run(async () =>
					{
						static string GetMessage(int i) => i switch
						{
							100 => "error",
							300 => "warning",
							_ => $"{BULK_MARKER}-{i}",
						};
						
						for (int i = 0; i < 100; i++)
						{
							await producer.SendAsync<SendMail, SendMailConsumer>(new
							{
								Id = i,
								CreatedDate = DateTime.UtcNow,
								Message = GetMessage(i),
							}, QueuePriority.Lowest);
						}
					});
					continue;
				}

				var priority = QueuePriority.Normal;
				var templateLow = $"->{LOW_MARKER}";
				if (msg.EndsWith(templateLow, StringComparison.InvariantCultureIgnoreCase))
				{
					priority = QueuePriority.Lowest;
					msg = msg.Replace(templateLow, string.Empty);
				}

				producer.Send<SendMail, SendMailConsumer>(new
				{
					Id = random.Next(1, int.MaxValue),
					CreatedDate = DateTime.UtcNow,
					Message = msg
				}, priority);

				producer.Publish<TestSomeActionExecuted>(new
				{
					Id = random.Next(1, int.MaxValue),
					CreatedDate = DateTime.UtcNow,
					Message = msg + "--test"
				});
			}
			//}
		}
	}
}
