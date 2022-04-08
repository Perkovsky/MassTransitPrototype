using MG.Shared.EventBus.Models;

namespace Consumer.WindowsService.Services
{
    public class SystemSettingsAccessor : ISystemSettingsAccessor
    {
        public EventBusSettings GetEventBusSettings()
        {
            return new EventBusSettings
            {
                UserName = "admin",
                Password = "admin",
                HostNames = new[] { "localhost" },
                Port = 61616
            };
        }
    }
}
