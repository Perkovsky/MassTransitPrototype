using MassTransit;
using MG.EventBus.Startup;
using Settings.Stub;
using SimpleInjector;
using System;

namespace Consumer.ConsoleApp
{
	class Program
	{
		static readonly Container container;

		static Program()
		{
			container = new Container();
			container.RegisterTestSomeActionExecutedConsumerDependencies(SettingsStub.GetSetting());
			container.Verify();
		}

		#region Testing without DI

		//static void Main(string[] args)
		//{
		//	var bus = Bus.Factory.CreateUsingRabbitMq(cfg =>
		//	{
		//		var host = cfg.Host(new System.Uri(@"rabbitmq://barnacle.rmq.cloudamqp.com:5672/oklnbiuq/"), h =>
		//		{
		//			h.Username("oklnbiuq");
		//			h.Password("OD0mXJWCHtbXBt8JADrigXSlebXNORMA");

		//			//h.UseSsl(s =>
		//			//{
		//			//	s.Protocol = System.Security.Authentication.SslProtocols.Tls12;
		//			//});

		//			cfg.ReceiveEndpoint(/*"EmailQueue", */ec =>
		//	  {
		//		  ec.Consumer<MG.EventBus.Components.Consumers.TestSomeActionExecutedConsumer>();
		//	  });
		//		});
		//	});

		//	bus.Start();
		//	Console.WriteLine("Listening test some commands...");
		//	Console.ReadKey();
		//	bus.Stop();
		//}

		#endregion

		static void Main(string[] args)
		{
			var bus = container.GetInstance<IBusControl>();
			bus.Start();
			Console.WriteLine("Listening test some events... Press any key to exit");
			Console.ReadKey();
			bus.Stop();
		}
	}
}
