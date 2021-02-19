using MG.EventBus.Components.Services;
using MG.EventBus.Contracts;
using MG.EventBus.Startup;
using Microsoft.Extensions.DependencyInjection;
using Settings.Stub;
using System;

namespace Producer
{
	class Program
	{
		static readonly ServiceProvider _serviceProvider;

		static Program()
		{
			_serviceProvider = new ServiceCollection()
				.RegisterEventBusProducerDependencies(SettingsStub.GetSetting())
				.BuildServiceProvider();
		}

		static void Main(string[] args)
		{
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

				DateTime scheduledTime = DateTime.Now.AddSeconds(130);
				producer.ScheduleSend<SendMail>(scheduledTime, new
				{
					Id = random.Next(1, int.MaxValue),
					CreatedDate = DateTime.Now,
					Message = msg
				});
			}
		}
	}
}
