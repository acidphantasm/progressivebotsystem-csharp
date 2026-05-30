using ProgressiveBotSystem.Models;
using SPTarkov.DI.Annotations;

namespace ProgressiveBotSystem.Globals;

[Injectable(InjectionType.Singleton)]
public class TierInformation
{
    public required List<TierData> Tiers;
}