using System;

namespace MG.EventBus.Contracts
{
	public interface SendMail
	{
		int Id { get; }
		DateTime CreatedDate { get; }
		string Message { get; }
		int NumberOfRetries { get; }
	}
}
