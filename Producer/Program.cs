using MassTransit;
using MG.EventBus.Contracts;
using MG.EventBus.Startup;
using SimpleInjector;
using System;

namespace Producer
{
	class Program
	{
		static readonly Container container;

		static Program()
		{
			container = new Container();
			container.RegisterEventBusProducerDependencies();
			container.Verify();
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

		//		bus.Publish<MailStored>(new
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
			var producer = container.GetInstance<IPublishEndpoint>();
			var random = new Random();
			Console.WriteLine("-- PRODUCER --");
			Console.WriteLine("Enter message (or quit to exit)..." + Environment.NewLine);

			while (true)
			{
				Console.Write("> ");
				string msg = Console.ReadLine();
				if (msg.Equals("quit", StringComparison.InvariantCultureIgnoreCase))
					break;

				/*await*/ producer.Publish<MailStored>(new
				{
					Id = random.Next(1, int.MaxValue),
					CreatedDate = DateTime.UtcNow,
					Message = msg
				});

				producer.Publish<TestSomeActionExecuted>(new
				{
					Id = random.Next(1, int.MaxValue),
					CreatedDate = DateTime.UtcNow,
					Message = msg + "--test"
				});
			}
		}
	}
}
