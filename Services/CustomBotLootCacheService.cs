﻿using System.Collections.Concurrent;
using _progressiveBotSystem.Helpers;
using _progressiveBotSystem.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Generators;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Bots;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils.Cloners;

namespace _progressiveBotSystem.Services;

[Injectable(InjectionType.Singleton)]
public class CustomBotLootCacheService(
    ApbsLogger apbsLogger,
    ItemHelper itemHelper,
    PMCLootGenerator pmcLootGenerator,
    ServerLocalisationService serverLocalisationService,
    ICloner cloner,
    BotEquipmentHelper botEquipmentHelper)
{
    private readonly ConcurrentDictionary<string, BotLootCache> _lootCache = new();
    private readonly Lock _drugLock = new();
    private readonly Lock _foodLock = new();
    private readonly Lock _drinkLock = new();
    private readonly Lock _currencyLock = new();
    private readonly Lock _stimLock = new();
    private readonly Lock _grenadeLock = new();
    private readonly Lock _specialLock = new();
    private readonly Lock _healingLock = new();

    public void ClearApbsCache()
    {
        _lootCache.Clear();
    }
    
    public Dictionary<MongoId, double> GetLootFromCache(
        string botRole,
        bool isPmc,
        string lootType,
        BotType botJsonTemplate,
        int botLevel,
        int tier,
        MinMax<double>? itemPriceMinMax = null
    )
    {
        var tierInfo = tier.ToString();
        var combinedBotRoleTier = botRole + tierInfo;
        
        if (!BotRoleExistsInCache(combinedBotRoleTier))
        {
            InitCacheForBotRole(combinedBotRoleTier);
            AddLootToCache(botRole, isPmc, botJsonTemplate, botLevel, tier);
        }

        if (!_lootCache.TryGetValue(combinedBotRoleTier, out var botRoleCache))
        {
            apbsLogger.Error($"Unable to find: {botRole} in loot cache");
            return [];
        }

        Dictionary<MongoId, double> result;
        switch (lootType)
        {
            case LootCacheType.Special:
                result = botRoleCache.SpecialItems;
                break;
            case LootCacheType.Backpack:
                result = botRoleCache.BackpackLoot;
                break;
            case LootCacheType.Pocket:
                result = botRoleCache.PocketLoot;
                break;
            case LootCacheType.Vest:
                result = botRoleCache.VestLoot;
                break;
            case LootCacheType.Secure:
                result = botRoleCache.SecureLoot;
                break;
            case LootCacheType.Combined:
                result = botRoleCache.CombinedPoolLoot;
                break;
            case LootCacheType.HealingItems:
                result = botRoleCache.HealingItems;
                break;
            case LootCacheType.GrenadeItems:
                result = botRoleCache.GrenadeItems;
                break;
            case LootCacheType.DrugItems:
                result = botRoleCache.DrugItems;
                break;
            case LootCacheType.FoodItems:
                result = botRoleCache.FoodItems;
                break;
            case LootCacheType.DrinkItems:
                result = botRoleCache.DrinkItems;
                break;
            case LootCacheType.CurrencyItems:
                result = botRoleCache.CurrencyItems;
                break;
            case LootCacheType.StimItems:
                result = botRoleCache.StimItems;
                break;
            default:
                apbsLogger.Error(
                    serverLocalisationService.GetText(
                        "bot-loot_type_not_found",
                        new
                        {
                            lootType,
                            botRole,
                            isPmc,
                        }
                    )
                );

                return [];
        }

        if (!result.Any())
        {
            // No loot, exit
            return result;
        }

        if (itemPriceMinMax is null)
        {
            // No filtering requested, return all results
            return cloner.Clone(result);
        }

        // Filter the loot pool by item value prior to returning
        var filteredResult = result.Where(i =>
        {
            var itemPrice = itemHelper.GetItemPrice(i.Key);

            var priceLimitMin = itemPriceMinMax.Min;
            var priceLimitMax = itemPriceMinMax.Max;

            // Treat -1 as no limit
            if (priceLimitMax.Approx(-1))
            {
                // only check min limit value
                return itemPrice >= priceLimitMin;
            }

            return itemPrice >= priceLimitMin && itemPrice <= priceLimitMax;
        });

        return cloner.Clone(filteredResult.ToDictionary(pair => pair.Key, pair => pair.Value));
    }

    /// <summary>
    ///     Generate loot for a bot and store inside a private class property
    /// </summary>
    /// <param name="botRole">bots role (assault / pmcBot etc)</param>
    /// <param name="isPmc">Is the bot a PMC (alters what loot is cached)</param>
    /// <param name="botJsonTemplate">db template for bot having its loot generated</param>
    private void AddLootToCache(string botRole, bool isPmc, BotType botJsonTemplate, int botLevel, int tier)
    {
        var tierInfo = tier.ToString();
        var combinedBotRoleTier = botRole + tierInfo;
        var chances = botEquipmentHelper.GetChancesByBotRole(botRole, tier);
        var realWhitelist = chances.Generation.Items;
        
        // Full pool of loot we use to create the various sub-categories with
        var lootPool = botJsonTemplate.BotInventory.Items;

        // Flatten all individual slot loot pools into one big pool, while filtering out potentially missing templates
        Dictionary<MongoId, double> specialLootPool = new();
        Dictionary<MongoId, double> backpackLootPool = new();
        Dictionary<MongoId, double> pocketLootPool = new();
        Dictionary<MongoId, double> vestLootPool = new();
        Dictionary<MongoId, double> secureLootPool = new();
        Dictionary<MongoId, double> combinedLootPool = new();

        if (isPmc)
        {
            // Replace lootPool from bot json with our own generated list for PMCs
            lootPool.Backpack = cloner.Clone(pmcLootGenerator.GeneratePMCBackpackLootPool(botRole));
            lootPool.Pockets = cloner.Clone(pmcLootGenerator.GeneratePMCPocketLootPool(botRole));
            lootPool.TacticalVest = cloner.Clone(pmcLootGenerator.GeneratePMCVestLootPool(botRole));
        }

        // Backpack/Pockets etc
        var poolsToProcess = new Dictionary<string, Dictionary<MongoId, double>>
        {
            { "Backpack", lootPool.Backpack },
            { "Pockets", lootPool.Pockets },
            { "SecuredContainer", lootPool.SecuredContainer },
            { "SpecialLoot", lootPool.SpecialLoot },
            { "TacticalVest", lootPool.TacticalVest },
        };

        foreach (var (containerType, itemPool) in poolsToProcess)
        {
            // No items to add, skip
            if (itemPool.Count == 0)
            {
                continue;
            }

            // Sort loot pool into separate buckets
            switch (containerType)
            {
                case "SpecialLoot":
                    AddItemsToPool(specialLootPool, itemPool);
                    break;
                case "Pockets":
                    AddItemsToPool(pocketLootPool, itemPool);
                    break;
                case "TacticalVest":
                    AddItemsToPool(vestLootPool, itemPool);
                    break;
                case "SecuredContainer":
                    AddItemsToPool(secureLootPool, itemPool);
                    break;
                case "Backpack":
                    AddItemsToPool(backpackLootPool, itemPool);
                    break;
                default:
                    apbsLogger.Warning($"How did you get here {containerType}");
                    break;
            }

            // If pool has items and items were going into a non-secure container pool, add to combined
            if (itemPool.Count > 0 && !containerType.Equals("securedcontainer", StringComparison.OrdinalIgnoreCase))
            {
                // fill up 'combined' pool of all loot
                AddItemsToPool(combinedLootPool, itemPool);
            }
        }

        // Assign whitelisted special items to bot if any exist
        var (specialLootItems, addSpecialLootItems) = GetGenerationWeights(realWhitelist.SpecialItems?.Whitelist);
        if (addSpecialLootItems) // key = tpl, value = weight
        {
            // No whitelist, find and assign from combined item pool
            foreach (var itemKvP in specialLootPool)
            {
                var itemTemplate = itemHelper.GetItem(itemKvP.Key).Value;
                if (!(IsBulletOrGrenade(itemTemplate.Properties) || IsMagazine(itemTemplate.Properties)))
                {
                    lock (_specialLock)
                    {
                        specialLootItems.TryAdd(itemKvP.Key, itemKvP.Value);
                    }
                }
            }
        }

        var (healingItemsInWhitelist, addHealingItems) = GetGenerationWeights(realWhitelist.Healing?.Whitelist);
        var (drugItemsInWhitelist, addDrugItems) = GetGenerationWeights(realWhitelist.Drugs?.Whitelist);
        var (foodItemsInWhitelist, addFoodItems) = GetGenerationWeights(realWhitelist.Food?.Whitelist);
        var (drinkItemsInWhitelist, addDrinkItems) = GetGenerationWeights(realWhitelist.Drink?.Whitelist);
        var (currencyItemsInWhitelist, addCurrencyItems) = GetGenerationWeights(realWhitelist.Currency?.Whitelist);
        var (stimItemsInWhitelist, addStimItems) = GetGenerationWeights(realWhitelist.Stims?.Whitelist);
        var (grenadeItemsInWhitelist, addGrenadeItems) = GetGenerationWeights(realWhitelist.Grenades?.Whitelist);

        foreach (var itemKvP in combinedLootPool)
        {
            var itemTemplate = itemHelper.GetItem(itemKvP.Key).Value;
            if (itemTemplate is null)
            {
                continue;
            }

            if (addHealingItems)
            {
                // Whitelist has no healing items, hydrate it using items from combinedLootPool that meet criteria
                if (
                    IsMedicalItem(itemTemplate.Properties)
                    && itemTemplate.Parent != BaseClasses.STIMULATOR
                    && itemTemplate.Parent != BaseClasses.DRUGS
                )
                {
                    lock (_healingLock)
                    {
                        healingItemsInWhitelist.TryAdd(itemKvP.Key, itemKvP.Value);
                    }
                }
            }

            if (addDrugItems)
            {
                if (itemTemplate.Parent == BaseClasses.DRUGS && IsMedicalItem(itemTemplate.Properties))
                {
                    lock (_drugLock)
                    {
                        drugItemsInWhitelist.TryAdd(itemKvP.Key, itemKvP.Value);
                    }
                }
            }

            if (addFoodItems)
            {
                if (itemHelper.IsOfBaseclass(itemTemplate.Id, BaseClasses.FOOD))
                {
                    lock (_foodLock)
                    {
                        foodItemsInWhitelist.TryAdd(itemKvP.Key, itemKvP.Value);
                    }
                }
            }

            if (addDrinkItems)
            {
                if (itemHelper.IsOfBaseclass(itemTemplate.Id, BaseClasses.DRINK))
                {
                    lock (_drinkLock)
                    {
                        drinkItemsInWhitelist.TryAdd(itemKvP.Key, itemKvP.Value);
                    }
                }
            }

            if (addCurrencyItems)
            {
                if (itemHelper.IsOfBaseclass(itemTemplate.Id, BaseClasses.MONEY))
                {
                    lock (_currencyLock)
                    {
                        currencyItemsInWhitelist.TryAdd(itemKvP.Key, itemKvP.Value);
                    }
                }
            }

            if (addStimItems)
            {
                if (itemTemplate.Parent == BaseClasses.STIMULATOR && IsMedicalItem(itemTemplate.Properties))
                {
                    lock (_stimLock)
                    {
                        stimItemsInWhitelist.TryAdd(itemKvP.Key, itemKvP.Value);
                    }
                }
            }

            if (addGrenadeItems)
            {
                if (IsGrenade(itemTemplate.Properties))
                {
                    lock (_grenadeLock)
                    {
                        grenadeItemsInWhitelist.TryAdd(itemKvP.Key, itemKvP.Value);
                    }
                }
            }
        }

        // Get backpack loot (excluding magazines, bullets, grenades, drink, food and healing/stim items)
        var filteredBackpackItems = FilterItemPool(
            backpackLootPool,
            (itemTemplate) =>
                IsBulletOrGrenade(itemTemplate.Properties)
                || IsMagazine(itemTemplate.Properties)
                || IsMedicalItem(itemTemplate.Properties)
                || IsGrenade(itemTemplate.Properties)
                || IsFood(itemTemplate.Id)
                || IsDrink(itemTemplate.Id)
                || IsCurrency(itemTemplate.Id)
        );

        // Get pocket loot (excluding magazines, bullets, grenades, drink, food medical and healing/stim items)
        var filteredPocketItems = FilterItemPool(
            pocketLootPool,
            (itemTemplate) =>
                IsBulletOrGrenade(itemTemplate.Properties)
                || IsMagazine(itemTemplate.Properties)
                || IsMedicalItem(itemTemplate.Properties)
                || IsGrenade(itemTemplate.Properties)
                || IsFood(itemTemplate.Id)
                || IsDrink(itemTemplate.Id)
                || IsCurrency(itemTemplate.Id)
                || itemTemplate.Properties.Height is null
                || // lacks height
                itemTemplate.Properties.Width is null
        ); // lacks width

        // Get vest loot (excluding magazines, bullets, grenades, medical and healing/stim items)
        var filteredVestItems = new Dictionary<MongoId, double>();
        foreach (var itemKvP in vestLootPool)
        {
            var itemResult = itemHelper.GetItem(itemKvP.Key);
            if (itemResult.Value is null)
            {
                continue;
            }

            var itemTemplate = itemResult.Value;
            if (
                IsBulletOrGrenade(itemTemplate.Properties)
                || IsMagazine(itemTemplate.Properties)
                || IsMedicalItem(itemTemplate.Properties)
                || IsGrenade(itemTemplate.Properties)
                || IsFood(itemTemplate.Id)
                || IsDrink(itemTemplate.Id)
                || IsCurrency(itemTemplate.Id)
            )
            {
                continue;
            }

            filteredVestItems.TryAdd(itemKvP.Key, itemKvP.Value);
        }

        // Get secure loot (excluding magazines, bullets)
        var filteredSecureLoot = FilterItemPool(
            secureLootPool,
            (itemTemplate) => IsBulletOrGrenade(itemTemplate.Properties) || IsMagazine(itemTemplate.Properties)
        );

        if (!_lootCache.TryGetValue(combinedBotRoleTier, out var cacheForRole))
        {
            apbsLogger.Error($"Unable to get loot cache value using key: {botRole}");

            return;
        }

        cacheForRole.HealingItems = healingItemsInWhitelist;
        cacheForRole.DrugItems = drugItemsInWhitelist;
        cacheForRole.FoodItems = foodItemsInWhitelist;
        cacheForRole.DrinkItems = drinkItemsInWhitelist;
        cacheForRole.CurrencyItems = currencyItemsInWhitelist;
        cacheForRole.StimItems = stimItemsInWhitelist;
        cacheForRole.GrenadeItems = grenadeItemsInWhitelist;
        cacheForRole.SpecialItems = specialLootItems;
        cacheForRole.BackpackLoot = filteredBackpackItems;
        cacheForRole.PocketLoot = filteredPocketItems;
        cacheForRole.VestLoot = filteredVestItems;
        cacheForRole.SecureLoot = filteredSecureLoot;
    }

    /// <summary>
    /// Helper function - Filter out items from passed in pool based on a passed in delegate
    /// </summary>
    /// <param name="lootPool">Pool to filter</param>
    /// <param name="shouldBeSkipped">Delegate to filter pool by</param>
    /// <returns></returns>
    private Dictionary<MongoId, double> FilterItemPool(Dictionary<MongoId, double> lootPool, Func<TemplateItem, bool> shouldBeSkipped)
    {
        var filteredItems = new Dictionary<MongoId, double>();
        foreach (var (itemTpl, itemWeight) in lootPool)
        {
            var (isValidItem, itemTemplate) = itemHelper.GetItem(itemTpl);
            if (!isValidItem)
            {
                continue;
            }

            if (shouldBeSkipped(itemTemplate))
            {
                continue;
            }

            filteredItems.TryAdd(itemTpl, itemWeight);
        }

        return filteredItems;
    }

    /// <summary>
    /// Return provided weights or an empty dictionary
    /// </summary>
    /// <param name="weights">Weights to return</param>
    /// <returns>Dictionary and should pool be hydrated by items in combined loot pool</returns>
    private static (Dictionary<MongoId, double>, bool populateFromCombinedPool) GetGenerationWeights(Dictionary<MongoId, double>? weights)
    {
        var result = weights ?? [];
        return (result, !result.Any()); // empty dict = should be populated from combined pool
    }

    /// <summary>
    /// merge item tpls + weightings to passed in dictionary
    /// If exits already, skip
    /// </summary>
    /// <param name="poolToAddTo">Dictionary to add item to</param>
    /// <param name="poolOfItemsToAdd">Dictionary of items to add</param>
    private void AddItemsToPool(Dictionary<MongoId, double> poolToAddTo, Dictionary<MongoId, double> poolOfItemsToAdd)
    {
        foreach (var (tpl, weight) in poolOfItemsToAdd)
        {
            // Skip adding items that already exist
            poolToAddTo.TryAdd(tpl, weight);
        }
    }

    /// <summary>
    ///     Ammo/grenades have this property
    /// </summary>
    /// <param name="properties"></param>
    /// <returns></returns>
    private bool IsBulletOrGrenade(TemplateItemProperties properties)
    {
        return properties.AmmoType is not null;
    }

    /// <summary>
    ///     Internal and external magazine have this property
    /// </summary>
    /// <param name="properties"></param>
    /// <returns></returns>
    private bool IsMagazine(TemplateItemProperties properties)
    {
        return properties.ReloadMagType is not null;
    }

    /// <summary>
    ///     Medical use items (e.g. morphine/lip balm/grizzly)
    /// </summary>
    /// <param name="properties"></param>
    /// <returns></returns>
    private bool IsMedicalItem(TemplateItemProperties properties)
    {
        return properties.MedUseTime is not null;
    }

    /// <summary>
    ///     Grenades have this property (e.g. smoke/frag/flash grenades)
    /// </summary>
    /// <param name="properties"></param>
    /// <returns></returns>
    private bool IsGrenade(TemplateItemProperties properties)
    {
        return properties.ThrowType is not null;
    }

    private bool IsFood(MongoId tpl)
    {
        return itemHelper.IsOfBaseclass(tpl, BaseClasses.FOOD);
    }

    private bool IsDrink(MongoId tpl)
    {
        return itemHelper.IsOfBaseclass(tpl, BaseClasses.DRINK);
    }

    private bool IsCurrency(MongoId tpl)
    {
        return itemHelper.IsOfBaseclass(tpl, BaseClasses.MONEY);
    }

    /// <summary>
    ///     Check if a bot type exists inside the loot cache
    /// </summary>
    /// <param name="botRole">role to check for</param>
    /// <returns>true if they exist</returns>
    private bool BotRoleExistsInCache(string botRole)
    {
        return _lootCache.ContainsKey(botRole);
    }

    /// <summary>
    ///     If lootcache is undefined, init with empty property arrays
    /// </summary>
    /// <param name="botRole">Bot role to hydrate</param>
    private void InitCacheForBotRole(string botRole)
    {
        _lootCache.TryAdd(botRole, new BotLootCache());
    }

    /// <summary>
    ///     Compares two item prices by their flea (or handbook if that doesn't exist) price
    /// </summary>
    /// <param name="itemAPrice"></param>
    /// <param name="itemBPrice"></param>
    /// <returns></returns>
    private int CompareByValue(int itemAPrice, int itemBPrice)
    {
        // If item A has no price, it should be moved to the back when sorting
        if (itemAPrice is 0)
        {
            return 1;
        }

        if (itemBPrice is 0)
        {
            return -1;
        }

        if (itemAPrice < itemBPrice)
        {
            return -1;
        }

        if (itemAPrice > itemBPrice)
        {
            return 1;
        }

        return 0;
    }
}