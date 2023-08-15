using MG.Shared.EventBus.Abstractions;

namespace Contracts
{
    public class UserUpdated : IContract
    {
        public int Id { get; set; }
    }
}
