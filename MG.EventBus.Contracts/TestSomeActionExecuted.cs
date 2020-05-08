using System;

namespace MG.EventBus.Contracts
{
	public interface TestSomeActionExecuted
	{
		int Id { get; }
		DateTime CreatedDate { get; }
		string Message { get; }
	}
}
