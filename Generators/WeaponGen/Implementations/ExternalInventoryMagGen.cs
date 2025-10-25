using _progressiveBotSystem.Constants;
using _progressiveBotSystem.Globals;
using _progressiveBotSystem.Helpers;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;
using LogLevel = SPTarkov.Server.Core.Models.Spt.Logging.LogLevel;

namespace SPTarkov.Server.Core.Generators.WeaponGen.Implementations;

[Injectable]
public class ApbsExternalInventoryMagGen(
    ISptLogger<ApbsExternalInventoryMagGen> logger,
    ItemHelper itemHelper,
    ServerLocalisationService serverLocalisationService,
    BotWeaponGeneratorHelper botWeaponGeneratorHelper,
    BotGeneratorHelper botGeneratorHelper,
    RandomUtil randomUtil,
    BotEquipmentHelper botEquipmentHelper
) : ApbsInventoryMagGen, IApbsInventoryMagGen
{
    public int GetPriority()
    {
        return 99;
    }

    public bool CanHandleInventoryMagGen(ApbsInventoryMagGen inventoryMagGen)
    {
        return true; // Fallback, if code reaches here it means no other implementation can handle this type of magazine
    }

    public void Process(ApbsInventoryMagGen inventoryMagGen)
    {
        // Count of attempts to fit a magazine into bot inventory
        var fitAttempts = 0;

        // Magazine Db template
        var magTemplate = inventoryMagGen.GetMagazineTemplate();
        var magazineTpl = magTemplate.Id;
        var weapon = inventoryMagGen.GetWeaponTemplate();
        var ammoCaliber = inventoryMagGen.GetAmmoTemplate().Properties.Caliber;
        var selectedAmmoId = inventoryMagGen.GetAmmoTemplate().Id;
        HashSet<MongoId> attemptedMagBlacklist = [];
        var defaultMagazineTpl = weapon.GetWeaponsDefaultMagazineTpl();
        var isShotgun = itemHelper.IsOfBaseclass(weapon.Id, BaseClasses.SHOTGUN);

        var botRole = inventoryMagGen.GetBotRole();
        var tier = inventoryMagGen.GetTier();
        var ammoTable = botEquipmentHelper.GetAmmoByBotRole(botRole, tier);
        var modPool = botEquipmentHelper.GetModsByBotRole(botRole, tier);
        var rerollConfig = inventoryMagGen.GetRerollDetails();
        var toploadConfig = inventoryMagGen.GetToploadConfig();

        var hasSwitchedToSmallerMags = false;
        var isTryingSmallerMags = false;
        
        var shouldBotRerollAmmo = rerollConfig.Enable && !toploadConfig.Enable && randomUtil.GetChance100(rerollConfig.Chance);
        var shouldBotTopload = toploadConfig.Enable && !rerollConfig.Enable && randomUtil.GetChance100(toploadConfig.Chance);
        
        var randomizedMagazineCount = botWeaponGeneratorHelper.GetRandomizedMagazineCount(inventoryMagGen.GetMagCount());
        if (itemHelper.IsOfBaseclass(weapon.Id, BaseClasses.PISTOL)) randomizedMagazineCount = randomUtil.GetInt(1, 2);
        for (var i = 0; i < randomizedMagazineCount; i++)
        {
            if (ModConfig.Config.GeneralConfig.EnableLargeCapacityMagazineLimit && !hasSwitchedToSmallerMags &&
                !VanillaItemConstants.WeaponsWithNoSmallMagazines.Contains(weapon.Id))
            {
                modPool.TryGetValue(weapon.Id, out var weaponModPool);
                HashSet<MongoId> magazinePool = null!;
                if (weaponModPool != null) weaponModPool.TryGetValue("mod_magazine", out magazinePool);
                if (magazinePool is not null && magTemplate.Properties!.Cartridges!.Any(x => x.MaxCount.HasValue) && magazinePool.Count != 0)
                {
                    var currentMagazineCountSize = magTemplate.Properties!.Cartridges!.Sum(x => x.MaxCount!.Value);

                    if (currentMagazineCountSize > 35 &&
                        i >= (ModConfig.Config.GeneralConfig.LargeCapacityMagazineCount - 1))
                    {
                        isTryingSmallerMags = true;
                        var smallMagazinePool = inventoryMagGen.GetCustomFilteredMagazinePoolByCapacity(tier, weapon, magazinePool);
                        if (smallMagazinePool.Count != 0)
                        {
                            magazineTpl = randomUtil.GetArrayValue(smallMagazinePool);
                            magTemplate = itemHelper.GetItem(magazineTpl).Value;
                        }
                    }
                }
            }

            var selectedAmmoForMag = selectedAmmoId;
            List<Item> magazineWithAmmo = new List<Item>();

            if (shouldBotRerollAmmo)
            {
                selectedAmmoForMag = inventoryMagGen.GetWeightedCompatibleAmmo(ammoTable, weapon);
            }
            if (shouldBotTopload)
            {
                magazineWithAmmo = inventoryMagGen.CreateMagazineWithAmmo(magazineTpl, selectedAmmoForMag, ammoTable, ammoCaliber, magTemplate,
                    toploadConfig.Percent);
            }
            else
            {
                magazineWithAmmo = botWeaponGeneratorHelper.CreateMagazineWithAmmo(
                    magazineTpl,
                    inventoryMagGen.GetAmmoTemplate().Id,
                    magTemplate
                );
            }

            var fitsIntoInventory = botGeneratorHelper.AddItemWithChildrenToEquipmentSlot(
                inventoryMagGen.GetBotId(),
                [EquipmentSlots.TacticalVest, EquipmentSlots.Pockets],
                magazineWithAmmo[0].Id,
                magazineTpl,
                magazineWithAmmo,
                inventoryMagGen.GetPmcInventory()
            );

            if (fitsIntoInventory == ItemAddedResult.NO_CONTAINERS)
            // No containers to fit magazines, stop trying
            {
                break;
            }

            // No space for magazine and we haven't reached desired magazine count
            if (fitsIntoInventory == ItemAddedResult.NO_SPACE && i < randomizedMagazineCount)
            {
                // Prevent infinite loop by only allowing 5 attempts at fitting a magazine into inventory
                if (fitAttempts > 5)
                {
                    if (logger.IsLogEnabled(LogLevel.Debug))
                    {
                        logger.Debug($"Failed {fitAttempts} times to add magazine {magazineTpl} to bot inventory, stopping");
                    }

                    break;
                }

                // We were unable to fit at least the minimum amount of magazines,
                // Fallback to default magazine and try again.
                // Temporary workaround to Killa spawning with no extra mags if he spawns with a drum mag

                if (magazineTpl == defaultMagazineTpl)
                // We were already on default - stop here to prevent infinite loop
                {
                    break;
                }

                // Add failed magazine tpl to blacklist
                attemptedMagBlacklist.Add(magazineTpl);

                if (defaultMagazineTpl is null)
                // No default to fall back to, stop trying to add mags
                {
                    break;
                }

                if (defaultMagazineTpl == BaseClasses.MAGAZINE)
                // Magazine base type, do not use
                {
                    break;
                }

                // Set chosen magazine tpl to the weapons default magazine tpl and try to fit into inventory next loop
                magazineTpl = defaultMagazineTpl.Value;
                magTemplate = itemHelper.GetItem(magazineTpl).Value;
                if (magTemplate is null)
                {
                    logger.Error(serverLocalisationService.GetText("bot-unable_to_find_default_magazine_item", magazineTpl));

                    break;
                }

                // Edge case - some weapons (SKS + shotguns) have an internal magazine as default, choose random non-internal magazine to add to bot instead
                if (magTemplate.Properties.ReloadMagType == ReloadMode.InternalMagazine)
                {
                    var result = GetRandomExternalMagazineForInternalMagazineGun(
                        inventoryMagGen.GetWeaponTemplate().Id,
                        attemptedMagBlacklist
                    );

                    if (result?.Id is null)
                    {
                        // Highly likely shotgun has no external mags
                        if (isShotgun)
                        {
                            break;
                        }

                        if (logger.IsLogEnabled(LogLevel.Debug))
                        {
                            logger.Debug(
                                $"Unable to add additional magazine into bot inventory: vest/pockets for weapon: {weapon.Name}, attempted: {fitAttempts} times. Reason: {fitsIntoInventory}"
                            );
                        }

                        break;
                    }

                    magazineTpl = result.Id;
                    magTemplate = result;
                    fitAttempts++;
                }

                // Reduce loop counter by 1 to ensure we get full cout of desired magazines
                i--;
            }

            if (fitsIntoInventory == ItemAddedResult.SUCCESS)
            // Reset fit counter now it succeeded
            {
                fitAttempts = 0;
            }
        }
    }

    /// <summary>
    ///     Get a random compatible external magazine for a weapon, exclude internal magazines from possible pool
    /// </summary>
    /// <param name="weaponTpl"> Weapon to get mag for </param>
    /// <param name="magazineBlacklist"> Blacklisted magazines </param>
    /// <returns> Item of chosen magazine </returns>
    public TemplateItem? GetRandomExternalMagazineForInternalMagazineGun(MongoId weaponTpl, HashSet<MongoId> magazineBlacklist)
    {
        // The mag Slot data for the weapon
        var magSlot = itemHelper.GetItem(weaponTpl).Value.Properties.Slots.FirstOrDefault(x => x.Name == "mod_magazine");
        if (magSlot is null)
        {
            return null;
        }

        // All possible mags that fit into the weapon excluding blacklisted
        var magazinePool = magSlot
            .Properties.Filters.First()
            .Filter.Where(x => !magazineBlacklist.Contains(x))
            .Select(x => itemHelper.GetItem(x).Value);
        if (magazinePool is null)
        {
            return null;
        }

        // Non-internal magazines that fit into the weapon
        var externalMagazineOnlyPool = magazinePool.Where(x => x.Properties.ReloadMagType != ReloadMode.InternalMagazine);
        if (externalMagazineOnlyPool is null || !externalMagazineOnlyPool.Any())
        {
            return null;
        }

        // Randomly chosen external magazine
        return randomUtil.GetArrayValue(externalMagazineOnlyPool);
    }
}
