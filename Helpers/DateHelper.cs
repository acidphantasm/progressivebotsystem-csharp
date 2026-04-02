using _progressiveBotSystem.Globals;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;

namespace _progressiveBotSystem.Helpers;

[Injectable(InjectionType.Singleton)]
public class DateHelper(): IOnLoad
{
    private bool _initialized;
    private bool AprilFoolsEnabled { get; set; }
    private bool HalloweenEnabled { get; set; }
    
    public Task OnLoad()
    {
        AprilFoolsEnabled = CalculateAprilFools();
        HalloweenEnabled = CalculateHalloween();
        _initialized = true;

        return Task.CompletedTask;
    }

    public bool IsAprilFoolsEnabled()
    {
        return !_initialized ? CalculateAprilFools() : AprilFoolsEnabled;
    }
    
    private bool CalculateAprilFools()
    {
        if (!ModConfig.Config.CompatibilityConfig.Secrets.AprilFoolsEvent)
            return false;
        
        var now = DateTime.Now;
        return now is { Month: 4, Day: 1 };
    }

    public bool IsHalloweenEnabled()
    {
        return !_initialized ? CalculateHalloween() : HalloweenEnabled;
    }
    
    private bool CalculateHalloween()
    {
        if (!ModConfig.Config.CompatibilityConfig.Secrets.HalloweenEvent)
            return false;
        
        var now = DateTime.Now;
        return now is { Month: 10, Day: 31 };
    }
}