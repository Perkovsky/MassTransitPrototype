using MG.Shared.EventBus;

namespace Contracts
{
    public class SendEmail : IContract
    {
        public string Email { get; set; }
        public string Text { get; set; }
    }
}
