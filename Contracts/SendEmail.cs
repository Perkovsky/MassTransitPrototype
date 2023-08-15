using MG.Shared.EventBus.Abstractions;

namespace Contracts
{
    public class SendEmail : IContract
    {
        public string Email { get; set; }
        public string Text { get; set; }
    }
}
