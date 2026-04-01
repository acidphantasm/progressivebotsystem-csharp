using _progressiveBotSystem.Globals;
using SPTarkov.DI.Annotations;

namespace _progressiveBotSystem.Helpers;

[Injectable(InjectionType.Singleton)]
public class DateHelper
{
    public bool IsAprilFools()
    {
        if (!ModConfig.Config.CompatibilityConfig.Secrets.AprilFoolsEvent)
            return false;
        
        var now = DateTime.Now;
        return now is { Month: 4, Day: 1 };
    }
}