using ProgressiveBotSystem.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace ProgressiveBotSystem.Generators.WeaponGen;

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

    public ApbsGenerationData GetMagCount()
    {
        return _magCounts!;
    }

    public TemplateItem GetMagazineTemplate()
    {
        return _magazineTemplate!;
    }

    public TemplateItem GetWeaponTemplate()
    {
        return _weaponTemplate!;
    }

    public TemplateItem GetAmmoTemplate()
    {
        return _ammoTemplate!;
    }

    public BotBaseInventory GetPmcInventory()
    {
        return _pmcInventory!;
    }

    public MongoId GetBotId()
    {
        return _botId!;
    }

    public string GetBotRole()
    {
        return _botRole!;
    }

    public int GetBotLevel()
    {
        return _botLevel!;
    }

    public int GetTier()
    {
        return _tier!;
    }

    public ToploadConfig GetToploadConfig()
    {
        return _toploadConfig!;
    }

    public EnableChance GetRerollDetails()
    {
        return _rerollDetails!;
    }
}
