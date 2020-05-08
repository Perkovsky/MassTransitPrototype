using System;

namespace MG.EventBus.Contracts
{
	public interface MailStored
	{
		int Id { get; }
		DateTime CreatedDate { get; }
		string Message { get; }
	}
}
