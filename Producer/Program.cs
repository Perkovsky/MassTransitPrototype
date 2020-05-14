using MG.EventBus.Components.Consumers;
using MG.EventBus.Components.Models;
using MG.EventBus.Components.Services;
using MG.EventBus.Contracts;
using MG.EventBus.Startup;
using Microsoft.Extensions.DependencyInjection;
using SimpleInjector;
using SimpleInjector.Lifestyles;
using System;
using System.Threading.Tasks;

namespace Producer
{
	class Program
	{
		const string BALK_MARKER = "balk";
		const string LOW_MARKER = "low";

		static readonly Container _container;
		static readonly ServiceProvider _serviceProvider;

		static Program()
		{
			#region SimpleInjector

			//_container = new Container();
			//_container.RegisterEventBusProducerDependencies();
			//_container.Verify();

			#endregion

			#region Microsoft DependencyInjection

			_serviceProvider = new ServiceCollection()
				.RegisterEventBusProducerDependencies()
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

				if (msg.Equals(BALK_MARKER, StringComparison.InvariantCultureIgnoreCase))
				{
					Task.Run(async () =>
					{
						for (int i = 0; i < 10000; i++)
						{
							await producer.SendAsync<SendMail, SendMailConsumer>(new
							{
								Id = i,
								CreatedDate = DateTime.UtcNow,
								Message = $"{BALK_MARKER}-{i}"
							}, QueuePriority.Lowest);
						}
					});
					continue;
				}

				producer.Send<SendMail, SendMailConsumer>(new
				{
					Id = random.Next(1, int.MaxValue),
					CreatedDate = DateTime.UtcNow,
					Message = msg
				} 
				, msg.Equals(LOW_MARKER, StringComparison.InvariantCultureIgnoreCase)
					? QueuePriority.Lowest
					: QueuePriority.Normal);

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
