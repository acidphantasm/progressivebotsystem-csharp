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
    private bool ChristmasEnabled { get; set; }
    
    public Task OnLoad()
    {
        AprilFoolsEnabled = CalculateAprilFools();
        HalloweenEnabled = CalculateHalloween();
        ChristmasEnabled = CalculateChristmas();
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

    public bool IsChristmasEnabled()
    {
        return !_initialized ? CalculateChristmas() : ChristmasEnabled;
    }
    
    private bool CalculateChristmas()
    {
        if (!ModConfig.Config.CompatibilityConfig.Secrets.ChristmasEvent)
            return false;
        
        var now = DateTime.Now;
        var start = new DateTime(now.Year, 12, 24);
        var end = new DateTime(now.Year, 12, 25);

        return now.Date >= start.Date && now.Date <= end.Date;
    }
}