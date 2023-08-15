using MG.Shared.EventBus.Models;

namespace CommonLibraries.Services
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
