using MG.EventBus.Startup.Models;

namespace Settings.Stub
{
	public static class SettingsStub
	{
		public static EventBusSettings GetSetting()
		{
			return new EventBusSettings
			{
				UseInMemory = false, // if true, then the stub method is used: use in memory (FOR DEVELOPERS ONLY)
				CloudAMQP = new CloudAMQPSettings
				{
					UserName = "oklnbiuq",
					Password = "OD0mXJWCHtbXBt8JADrigXSlebXNORMA",
					VirtualHost = "oklnbiuq",
					HostName = "barnacle.rmq.cloudamqp.com",
					Port = "5672"
				},
				AmazonMQ = new AmazonMQSettings
				{
					UserName = "admin",
					Password = "admin",
					HostName = "localhost"
				}
			};
		}
	}
}
