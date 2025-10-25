using _progressiveBotSystem.Models;
using _progressiveBotSystem.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;

namespace _progressiveBotSystem.Generators.WeaponGen;

[Injectable]
public class ApbsInventoryMagGen()
{
    private readonly TemplateItem? _ammoTemplate;
    private readonly TemplateItem? _magazineTemplate;
    private readonly GenerationData? _magCounts;
    private readonly BotBaseInventory? _pmcInventory;
    private readonly MongoId _botId;
    private readonly TemplateItem? _weaponTemplate;
    private readonly string _botRole;
    private readonly int _botLevel;
    private readonly int _tier;
    private readonly ToploadConfig? _toploadConfig;
    private readonly EnableChance? _rerollDetails;

    public ApbsInventoryMagGen(
        GenerationData magCounts,
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

    public GenerationData GetMagCount()
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

    public List<Item> CreateMagazineWithAmmo(MongoId magazineTpl, MongoId ammoTpl,
        Dictionary<string, Dictionary<MongoId, double>> ammoPool, string ammoCaliber, TemplateItem magTemplate,
        int percentOfMag)
    {
        List<Item> magazine = [new() { Id = new MongoId(), Template = magazineTpl }];

        FillMagazineWithCartridge(magazine, magTemplate, ammoTpl, ammoPool, ammoCaliber, percentOfMag);

        return magazine;
    }

    private void FillMagazineWithCartridge(List<Item> magazineWithChildCartridges, TemplateItem magTemplate,
        MongoId cartridgeTpl, Dictionary<string, Dictionary<MongoId, double>> ammoPool, string ammoCaliber,
        int percentOfMag)
    {
        var itemHelper = ServiceLocator.ServiceProvider.GetService<ItemHelper>();
        var serverLocalisationService = ServiceLocator.ServiceProvider.GetService<ServerLocalisationService>();
        var apbsLogger = ServiceLocator.ServiceProvider.GetService<ApbsLogger>();
        var randomUtil = ServiceLocator.ServiceProvider.GetService<RandomUtil>();
        
        var isUbgl = itemHelper.IsOfBaseclass(magTemplate.Id, BaseClasses.LAUNCHER);
        if (isUbgl)
        {
            return;
        }
        
        var cartridgeDetails = itemHelper.GetItem(cartridgeTpl);
        if (!cartridgeDetails.Key)
        {
            apbsLogger.Error(serverLocalisationService.GetText("item-invalid_tpl_item", cartridgeTpl));
        }

        var cartridgeMaxStackSize = cartridgeDetails.Value?.Properties?.StackMaxSize;
        if (cartridgeMaxStackSize is null)
        {
            apbsLogger.Error($"Item with tpl: {cartridgeTpl} lacks a _props or StackMaxSize property");
        }
        
        var magProperties = magTemplate.Properties;
        var magazineCartridgeMaxCount = itemHelper.IsOfBaseclass(magTemplate.Id, BaseClasses.SPRING_DRIVEN_CYLINDER)
            ? magProperties?.Slots?.Count() // Edge case for rotating grenade launcher magazine
            : magProperties?.Cartridges?.FirstOrDefault()?.MaxCount;

        if (magazineCartridgeMaxCount is null)
        {
            apbsLogger.Warning($"Magazine: {magTemplate.Id} {magTemplate.Name} lacks a Cartridges array, unable to fill magazine with ammo");

            return;
        }

        var bottomLoadTpl = cartridgeTpl;
        var topLoadTpl = cartridgeTpl;
        ammoPool.TryGetValue(ammoCaliber, out var ammoCaliberPool);
        var ammoCaliberPoolKeys = ammoCaliberPool?.Keys.ToList() ?? [];
        
        var indexOfTopLoad = ammoCaliberPoolKeys.IndexOf(cartridgeTpl);
        if (indexOfTopLoad >= 0 && ammoCaliberPoolKeys.Count > 1)
        {
            if (indexOfTopLoad > 0)
                bottomLoadTpl = ammoCaliberPoolKeys[indexOfTopLoad - 1];

            if (indexOfTopLoad < ammoCaliberPoolKeys.Count - 1)
                topLoadTpl = ammoCaliberPoolKeys[indexOfTopLoad + 1];
        }

        var desiredMaxStackCount = (int)magazineCartridgeMaxCount.Value;
        var desiredTopLoadAmount = (int)Math.Round(Math.Max(1, randomUtil.GetPercentOfValue(percentOfMag, desiredMaxStackCount, 0)));
        var desiredBottomLoadAmount = desiredMaxStackCount - desiredTopLoadAmount;

        if (magazineWithChildCartridges.Count > 1)
        {
            apbsLogger.Warning($"Magazine {magTemplate.Name} already has cartridges defined,  this may cause issues");
        }
        
        // Loop over cartridge count and add stacks to magazine
        var cartridgeTplToAdd = cartridgeTpl;
        var currentStoredCartridgeCount = 0;
        var location = 0;

        while (currentStoredCartridgeCount < desiredMaxStackCount)
        {
            var remainingMagSpace = desiredMaxStackCount - currentStoredCartridgeCount;
            var cartridgeCountToAdd = 0;
            
            if (currentStoredCartridgeCount < desiredBottomLoadAmount)
            {
                cartridgeTplToAdd = bottomLoadTpl;
                cartridgeCountToAdd = desiredBottomLoadAmount <= cartridgeMaxStackSize.Value
                    ? desiredBottomLoadAmount
                    : cartridgeMaxStackSize.Value;
                if (cartridgeCountToAdd > (remainingMagSpace - desiredTopLoadAmount))
                {
                    cartridgeCountToAdd = remainingMagSpace - desiredTopLoadAmount;
                }
            }
            else
            {
                cartridgeTplToAdd = topLoadTpl;
                cartridgeCountToAdd = desiredTopLoadAmount <= cartridgeMaxStackSize.Value ? desiredTopLoadAmount : cartridgeMaxStackSize.Value;
            }

            // Ensure we don't go over the max stackCount size
            var remainingSpace = desiredMaxStackCount - currentStoredCartridgeCount;
            if (cartridgeCountToAdd > remainingSpace)
            {
                cartridgeCountToAdd = remainingSpace;
            }

            // Add cartridge item object into items array
            magazineWithChildCartridges.Add(
                itemHelper.CreateCartridges(magazineWithChildCartridges[0].Id, cartridgeTplToAdd, cartridgeCountToAdd, location)
            );

            currentStoredCartridgeCount += cartridgeCountToAdd;
            location++;
        }

        // Only one cartridge stack added, remove location property as it's only used for 2 or more stacks
        if (location == 1)
        {
            magazineWithChildCartridges[1].Location = null;
        }
    }
    
    public List<MongoId> GetCustomFilteredMagazinePoolByCapacity(int tier, TemplateItem weapon, HashSet<MongoId> modPool)
    {
        var itemHelper = ServiceLocator.ServiceProvider.GetService<ItemHelper>();
        var apbsLogger = ServiceLocator.ServiceProvider.GetService<ApbsLogger>();
        
        var weaponTpl = weapon.Id;
        var desiredMagazineTpls = modPool.Where(magTpl =>
        {
            var magazineDb = itemHelper.GetItem(magTpl).Value;
            return magazineDb?.Properties?.Cartridges is not null
                   && magazineDb.Properties.Cartridges.FirstOrDefault()?.MaxCount < 40 && magazineDb.Properties.Cartridges.FirstOrDefault()?.MaxCount >= 25;
        }).ToList();

        if (desiredMagazineTpls.Count == 0)
        {
            apbsLogger.Warning($"[MAGAZINE] Magazine size filter for: {weaponTpl} does not have small capacity magazines available in tier {tier}. Ignoring filter.");
        }

        return desiredMagazineTpls;
    }
    
    public MongoId GetWeightedCompatibleAmmo(Dictionary<string, Dictionary<MongoId, double>> cartridgePool, TemplateItem weaponTemplate)
    {
        var apbsLogger = ServiceLocator.ServiceProvider.GetService<ApbsLogger>();
        var serverLocalisationService = ServiceLocator.ServiceProvider.GetService<ServerLocalisationService>();
        var itemHelper = ServiceLocator.ServiceProvider.GetService<ItemHelper>();
        var weightedRandomHelper = ServiceLocator.ServiceProvider.GetService<WeightedRandomHelper>();
        
        var desiredCaliber = GetWeaponCaliber(weaponTemplate);
        if (!cartridgePool.TryGetValue(desiredCaliber, out var cartridgePoolForWeapon) || cartridgePoolForWeapon?.Count == 0)
        {
            apbsLogger.Debug(
                serverLocalisationService.GetText(
                    "bot-no_caliber_data_for_weapon_falling_back_to_default",
                    new
                    {
                        weaponId = weaponTemplate.Id,
                        weaponName = weaponTemplate.Name,
                        defaultAmmo = weaponTemplate.Properties.DefAmmo,
                    }
                )
            );

            if (weaponTemplate.Properties.DefAmmo.HasValue)
            {
                return weaponTemplate.Properties.DefAmmo.Value;
            }

            // last ditch attempt to get default ammo tpl
            return weaponTemplate.Properties.Chambers.FirstOrDefault().Properties.Filters.FirstOrDefault().Filter.FirstOrDefault();
        }

        // Get cartridges the weapons first chamber allow
        var compatibleCartridgesInTemplate = GetCompatibleCartridgesFromWeaponTemplate(weaponTemplate);
        if (compatibleCartridgesInTemplate.Count == 0)
        // No chamber data found in weapon, send default
        {
            return weaponTemplate.Properties.DefAmmo.Value;
        }

        // Inner join the weapons allowed + passed in cartridge pool to get compatible cartridges
        Dictionary<MongoId, double> compatibleCartridges = new();
        foreach (var cartridge in cartridgePoolForWeapon)
        {
            if (compatibleCartridgesInTemplate.Contains(cartridge.Key))
            {
                compatibleCartridges[cartridge.Key] = cartridge.Value;
            }
        }

        // No cartridges found, try and get something that's compatible with the gun
        if (!compatibleCartridges.Any())
        {
            // Get cartridges from the weapons first magazine in filters
            var compatibleCartridgesInMagazine = GetCompatibleCartridgesFromMagazineTemplate(weaponTemplate);
            if (compatibleCartridgesInMagazine.Count == 0)
            {
                // No compatible cartridges found in magazine, use default
                return weaponTemplate.Properties.DefAmmo.Value;
            }

            // Get the caliber data from the first compatible round in the magazine
            var magazineCaliberData = itemHelper.GetItem(compatibleCartridgesInMagazine.FirstOrDefault()).Value.Properties.Caliber;
            cartridgePoolForWeapon = cartridgePool[magazineCaliberData];

            foreach (var cartridgeKvP in cartridgePoolForWeapon)
            {
                if (compatibleCartridgesInMagazine.Contains(cartridgeKvP.Key))
                {
                    compatibleCartridges[cartridgeKvP.Key] = cartridgeKvP.Value;
                }
            }

            // Nothing found after also checking magazines, return default ammo
            if (compatibleCartridges.Count == 0)
            {
                return weaponTemplate.Properties.DefAmmo.Value;
            }
        }

        return weightedRandomHelper.GetWeightedValue(compatibleCartridges);
    }
    
    private string? GetWeaponCaliber(TemplateItem weaponTemplate)
    {
        var itemHelper = ServiceLocator.ServiceProvider.GetService<ItemHelper>();
        
        if (!string.IsNullOrEmpty(weaponTemplate.Properties.Caliber))
        {
            return weaponTemplate.Properties.Caliber;
        }

        if (!string.IsNullOrEmpty(weaponTemplate.Properties.AmmoCaliber))
            // 9x18pmm has a typo, should be Caliber9x18PM
        {
            return weaponTemplate.Properties.AmmoCaliber == "Caliber9x18PMM" ? "Caliber9x18PM" : weaponTemplate.Properties.AmmoCaliber;
        }

        if (!string.IsNullOrEmpty(weaponTemplate.Properties.LinkedWeapon))
        {
            var ammoInChamber = itemHelper.GetItem(
                weaponTemplate.Properties.Chambers.First().Properties.Filters.First().Filter.FirstOrDefault()
            );
            return !ammoInChamber.Key ? null : ammoInChamber.Value.Properties.Caliber;
        }

        return null;
    }
    
    private HashSet<MongoId> GetCompatibleCartridgesFromWeaponTemplate(TemplateItem weaponTemplate)
    {
        ArgumentNullException.ThrowIfNull(weaponTemplate);

        var cartridges = weaponTemplate.Properties?.Chambers?.FirstOrDefault()?.Properties?.Filters?.First().Filter;
        if (cartridges is not null)
        {
            return cartridges;
        }

        // Fallback to the magazine if possible, e.g. for revolvers
        return GetCompatibleCartridgesFromMagazineTemplate(weaponTemplate);
    }
    
    private HashSet<MongoId> GetCompatibleCartridgesFromMagazineTemplate(TemplateItem weaponTemplate)
    {
        ArgumentNullException.ThrowIfNull(weaponTemplate);
        var itemHelper = ServiceLocator.ServiceProvider.GetService<ItemHelper>();
        
        // Get the first magazine's template from the weapon
        var magazineSlot = weaponTemplate.Properties.Slots?.FirstOrDefault(slot => slot.Name == "mod_magazine");
        if (magazineSlot is null)
        {
            return [];
        }

        var magazineTemplate = itemHelper.GetItem(
            magazineSlot.Properties?.Filters.FirstOrDefault()?.Filter?.FirstOrDefault() ?? new MongoId(null)
        );
        if (!magazineTemplate.Key)
        {
            return [];
        }

        // Try to get cartridges from slots array first, if none found, try Cartridges array
        var cartridges =
            magazineTemplate.Value.Properties.Slots.FirstOrDefault()?.Properties?.Filters?.FirstOrDefault()?.Filter ?? magazineTemplate
                .Value.Properties.Cartridges.FirstOrDefault()
                ?.Properties?.Filters?.FirstOrDefault()
                ?.Filter;

        return cartridges ?? [];
    }
}
