using MG.Shared.EventBus;

namespace Contracts
{
    public class UserUpdated : IContract
    {
        public int Id { get; set; }
    }
}
