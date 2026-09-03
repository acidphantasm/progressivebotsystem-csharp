namespace ProgressiveBotSystem.Generators.WeaponGen;

using Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

[Injectable]
public class ApbsInventoryMagGen()
{
    private readonly TemplateItem? _ammoTemplate;
    private readonly MongoId _botId;
    private readonly int _botLevel;
    private readonly string _botRole;
    private readonly TemplateItem? _magazineTemplate;
    private readonly ApbsGenerationData? _magCounts;
    private readonly BotBaseInventory? _pmcInventory;
    private readonly EnableChance? _rerollDetails;
    private readonly int _tier;
    private readonly ToploadConfig? _toploadConfig;
    private readonly TemplateItem? _weaponTemplate;

    public ApbsInventoryMagGen(
        ApbsGenerationData magCounts,
        TemplateItem magazineTemplate,
        TemplateItem weaponTemplate,
        TemplateItem ammoTemplate,
        BotBaseInventory pmcInventory,
        MongoId botId,
        string botRole,
        int botLevel,
        int tier,
        ToploadConfig toploadDetails,
        EnableChance rerollDetails
    )
        : this()
    {
        _magCounts = magCounts;
        _magazineTemplate = magazineTemplate;
        _weaponTemplate = weaponTemplate;
        _ammoTemplate = ammoTemplate;
        _pmcInventory = pmcInventory;
        _botId = botId;
        _botRole = botRole;
        _botLevel = botLevel;
        _tier = tier;
        _toploadConfig = toploadDetails;
        _rerollDetails = rerollDetails;
    }

    public ApbsGenerationData GetMagCount() => _magCounts!;

    public TemplateItem GetMagazineTemplate() => _magazineTemplate!;

    public TemplateItem GetWeaponTemplate() => _weaponTemplate!;

    public TemplateItem GetAmmoTemplate() => _ammoTemplate!;

    public BotBaseInventory GetPmcInventory() => _pmcInventory!;

    public MongoId GetBotId() => _botId!;

    public string GetBotRole() => _botRole!;

    public int GetBotLevel() => _botLevel!;

    public int GetTier() => _tier!;

    public ToploadConfig GetToploadConfig() => _toploadConfig!;

    public EnableChance GetRerollDetails() => _rerollDetails!;
}
