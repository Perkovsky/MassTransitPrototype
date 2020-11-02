using System.Collections.Generic;

namespace MG.EventBus.Startup.Models
{
	public class AmazonMQSettings
	{
		public string UserName { get; set; }
		public string Password { get; set; }
		public IEnumerable<string> HostNames { get; set; }
		public int Port { get; set; }
	}
}
