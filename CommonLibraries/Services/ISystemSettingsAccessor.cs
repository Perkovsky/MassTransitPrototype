using MG.Shared.EventBus.Models;

namespace CommonLibraries.Services
{
    public interface ISystemSettingsAccessor
    {
        EventBusSettings GetEventBusSettings();
    }
}
