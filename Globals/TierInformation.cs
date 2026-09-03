namespace ProgressiveBotSystem.Globals;

using Models;
using SPTarkov.DI.Annotations;

[Injectable(InjectionType.Singleton)]
public class TierInformation
{
    public required List<TierData> Tiers;
}
