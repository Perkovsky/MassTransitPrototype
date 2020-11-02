namespace MG.EventBus.Startup.Models
{
	public class EventBusSettings
	{
		public RetryPolicySettings RetryPolicy { get; set; }
		public bool UseInMemory { get; set; }
		public CloudAMQPSettings CloudAMQP { get; set; }
		public AmazonMQSettings AmazonMQ { get; set; }
	}
}
