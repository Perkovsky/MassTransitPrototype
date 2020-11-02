namespace MG.EventBus.Startup.Models
{
	public class RetryPolicySettings
	{
		public int RetryCount { get; set; }
		public int Interval { get; set; } // mins
	}
}
