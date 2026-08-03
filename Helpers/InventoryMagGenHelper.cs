namespace ProgressiveBotSystem.Helpers;

using System.Collections.Frozen;
using ProgressiveBotSystem.Models;
using ProgressiveBotSystem.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Helpers.Items;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Services.Locales;
using SPTarkov.Server.Core.Utils;

[Injectable]
public class InventoryMagGenHelper
{
    private readonly ItemHelper _itemHelper;
    private readonly ApbsLogger _apbsLogger;
    private readonly RandomUtil _randomUtil;
    private readonly WeightedRandomHelper _weightedRandomHelper;
    private readonly ServerLocalisationService _serverLocalisationService;

    public InventoryMagGenHelper(
        ItemHelper itemHelper,
        ApbsLogger apbsLogger,
        RandomUtil randomUtil,
        WeightedRandomHelper weightedRandomHelper,
        ServerLocalisationService serverLocalisationService)
    {
        _itemHelper = itemHelper;
        _apbsLogger = apbsLogger;
        _randomUtil = randomUtil;
        _weightedRandomHelper = weightedRandomHelper;
        _serverLocalisationService = serverLocalisationService;
    }

    private static readonly FrozenSet<string> MagCheck = ["CylinderMagazine", "SpringDrivenCylinder"];

    public int GetRandomizedMagazineCount(ApbsGenerationData magCounts)
    {
        return (int)_weightedRandomHelper.GetWeightedValue(magCounts.Weights);
    }

    public double? GetRandomizedBulletCount(ApbsGenerationData magCounts, TemplateItem magTemplate)
    {
        var randomizedMagazineCount = Math.Max(GetRandomizedMagazineCount(magCounts), 1); // Never return lower than 1 to prevent a multiplication by 0
        var parentItem = _itemHelper.GetItem(magTemplate.Parent).Value;
        if (parentItem is null)
        {
            _apbsLogger.Error($"Parent item null when trying to get randomized bullet count for: {magTemplate.Id}");
            return null;
        }

        double? chamberBulletCount;
        if (MagazineIsCylinderRelated(parentItem.Name ?? string.Empty))
        {
            var firstSlotAmmoTpl =
                magTemplate.Properties?.Cartridges?.FirstOrDefault()?.Properties?.Filters?.First().Filter?.FirstOrDefault()
                ?? MongoId.Empty();
            var ammoMaxStackSize = _itemHelper.GetItem(firstSlotAmmoTpl).Value?.Properties?.StackMaxSize ?? 1;
            chamberBulletCount =
                ammoMaxStackSize == 1
                    ? 1 // Rotating grenade launcher
                    : magTemplate.Properties?.Slots?.Count(); // Shotguns/revolvers. We count the number of camoras as the _max_count of the magazine is 0
        }
        else if (parentItem.Id == BaseClasses.LAUNCHER)
        {
            // Underbarrel launchers can only have 1 chambered grenade
            chamberBulletCount = 1;
        }
        else
        {
            chamberBulletCount = magTemplate.Properties?.Cartridges?.First().MaxCount;
        }

        // Get the amount of bullets that would fit in the internal magazine
        // and multiply by how many magazines were supposed to be created
        return chamberBulletCount * randomizedMagazineCount;
    }

    private bool MagazineIsCylinderRelated(string magazineParentName)
    {
        return MagCheck.Contains(magazineParentName);
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
        var isUbgl = _itemHelper.IsOfBaseclass(magTemplate.Id, BaseClasses.LAUNCHER);
        if (isUbgl)
        {
            return;
        }

        var cartridgeDetails = _itemHelper.GetItem(cartridgeTpl);
        if (!cartridgeDetails.Key)
        {
            _apbsLogger.Error(_serverLocalisationService.GetText("item-invalid_tpl_item", cartridgeTpl));
        }

        var cartridgeMaxStackSize = cartridgeDetails.Value?.Properties?.StackMaxSize;
        if (cartridgeMaxStackSize is null)
        {
            _apbsLogger.Error($"Item with tpl: {cartridgeTpl} lacks a _props or StackMaxSize property");
        }

        var magProperties = magTemplate.Properties;
        var magazineCartridgeMaxCount = _itemHelper.IsOfBaseclass(magTemplate.Id, BaseClasses.SPRING_DRIVEN_CYLINDER)
            ? magProperties?.Slots?.Count() // Edge case for rotating grenade launcher magazine
            : magProperties?.Cartridges?.FirstOrDefault()?.MaxCount;

        if (magazineCartridgeMaxCount is null)
        {
            _apbsLogger.Warning($"Magazine: {magTemplate.Id} {magTemplate.Name} lacks a Cartridges array, unable to fill magazine with ammo");

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
        var desiredTopLoadAmount = (int)Math.Round(Math.Max(1, _randomUtil.GetPercentOfValue(percentOfMag, desiredMaxStackCount, 0)));
        var desiredBottomLoadAmount = desiredMaxStackCount - desiredTopLoadAmount;

        if (magazineWithChildCartridges.Count > 1)
        {
            _apbsLogger.Warning($"Magazine {magTemplate.Name} already has cartridges defined,  this may cause issues");
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
                _itemHelper.CreateCartridges(magazineWithChildCartridges[0].Id, cartridgeTplToAdd, cartridgeCountToAdd, location)
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

    public List<MongoId> GetCustomFilteredMagazinePoolByCapacity(int tier, TemplateItem weapon, HashSet<MongoId> modPool, int min, int max)
    {
        var weaponTpl = weapon.Id;
        var desiredMagazineTpls = modPool.Where(magTpl =>
        {
            var magazineDb = _itemHelper.GetItem(magTpl).Value;
            var maxCount = magazineDb?.Properties?.Cartridges?.Max(x => x.MaxCount.Value);

            return maxCount is not null
                   && maxCount < max
                   && maxCount >= min;
        }).ToList();

        if (desiredMagazineTpls.Count == 0)
        {
            _apbsLogger.Debug($"[MAGAZINE] Magazine size filter for: {weaponTpl} does not have small capacity magazines available in tier {tier}. Using any available magazine.");
            return modPool.ToList();
        }

        return desiredMagazineTpls;
    }

    public TemplateItem GetFallbackFittingMagazine(Dictionary<MongoId,Dictionary<string,HashSet<MongoId>>> modPool, TemplateItem weapon, TemplateItem magTemplate, int tier)
    {
        if (!modPool.TryGetValue(weapon.Id, out var weaponModPool) ||
            !weaponModPool.TryGetValue("mod_magazine", out var magazinePool))
        {
            return magTemplate;
        }

        if (magazinePool.Contains(magTemplate.Id))
        {
            return magTemplate;
        }

        var filteredPool = GetCustomFilteredMagazinePoolByCapacity(tier, weapon, magazinePool, 10, 31);

        if (filteredPool.Count > 0)
        {
            var tpl = _randomUtil.GetArrayValue(filteredPool);
            return _itemHelper.GetItem(tpl).Value;
        }

        if (magazinePool.Count > 0)
        {
            var tpl = _randomUtil.GetArrayValue(magazinePool.ToList());
            return _itemHelper.GetItem(tpl).Value;
        }

        return magTemplate;
    }

    public bool DoesMagazineFitWeapon(TemplateItem weapon, TemplateItem magTemplate)
    {
        return weapon?.Properties?.Slots?
            .FirstOrDefault(s => s.Name == "mod_magazine")?
            .Properties?.Filters?
            .SelectMany(f => f?.Filter ?? Enumerable.Empty<MongoId>())
            .Contains(magTemplate.Id) == true;
    }

    public MongoId GetWeightedCompatibleAmmo(Dictionary<string, Dictionary<MongoId, double>> cartridgePool, TemplateItem weaponTemplate)
    {
        var desiredCaliber = GetWeaponCaliber(weaponTemplate);
        if (!cartridgePool.TryGetValue(desiredCaliber, out var cartridgePoolForWeapon) || cartridgePoolForWeapon?.Count == 0)
        {
            _apbsLogger.Debug(
                _serverLocalisationService.GetText(
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
            var magazineCaliberData = _itemHelper.GetItem(compatibleCartridgesInMagazine.FirstOrDefault()).Value.Properties.Caliber;
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

        return _weightedRandomHelper.GetWeightedValue(compatibleCartridges);
    }

    private string? GetWeaponCaliber(TemplateItem weaponTemplate)
    {
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
            var ammoInChamber = _itemHelper.GetItem(
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

        // Get the first magazine's template from the weapon
        var magazineSlot = weaponTemplate.Properties.Slots?.FirstOrDefault(slot => slot.Name == "mod_magazine");
        if (magazineSlot is null)
        {
            return [];
        }

        var magazineTemplate = _itemHelper.GetItem(
            magazineSlot.Properties?.Filters.FirstOrDefault()?.Filter?.FirstOrDefault() ?? MongoId.Empty()
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