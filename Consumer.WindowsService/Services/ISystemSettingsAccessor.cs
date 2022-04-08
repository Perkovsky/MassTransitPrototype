using MG.Shared.EventBus.Models;

namespace Consumer.WindowsService.Services
{
    public interface ISystemSettingsAccessor
    {
        EventBusSettings GetEventBusSettings();
    }
}
