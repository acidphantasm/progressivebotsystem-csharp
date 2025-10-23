using System.Reflection;
using _progressiveBotSystem.Models;
using HarmonyLib;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Generators;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Bots;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Utils;

namespace _progressiveBotSystem.Patches;

public class GetCompatibleModFromPool_Patch : AbstractPatch
{
    private static ISptLogger<BotEquipmentModGenerator>? _logger;
    private static ItemHelper? _itemHelper;


    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(BotEquipmentModGenerator), nameof(BotEquipmentModGenerator.GetCompatibleModFromPool));
    }

    [PatchPrefix]
    public static bool Prefix(BotEquipmentModGenerator __instance, ref ChooseRandomCompatibleModResult __result,
        HashSet<MongoId> modPool,
        ModSpawn? modSpawnType,
        IEnumerable<Item> weapon)
    {
        _logger ??= ServiceLocator.ServiceProvider.GetService<ISptLogger<BotEquipmentModGenerator>>();
        _itemHelper ??= ServiceLocator.ServiceProvider.GetService<ItemHelper>();

        // Create exhaustable pool to pick mod item from
        var exhaustableModPool = __instance.CreateExhaustableArray(modPool);

        // Create default response if no compatible item is found below
        ChooseRandomCompatibleModResult chosenModResult = new()
        {
            Incompatible = true,
            Found = false,
            Reason = "unknown",
        };

        // Limit how many attempts to find a compatible mod can occur before giving up
        var maxBlockedAttempts = modPool.Count; // 100% of pool size
        var blockedAttemptCount = 0;
        while (exhaustableModPool.HasValues())
        {
            var chosenTpl = exhaustableModPool.GetRandomValue();
            var pickedItemDetails = _itemHelper.GetItem(chosenTpl);
            if (!pickedItemDetails.Key)
            // Not valid item, try again
            {
                continue;
            }

            if (pickedItemDetails.Value.Properties is null)
            // no props data, try again
            {
                continue;
            }

            // Success - Default wanted + only 1 item in pool
            if (modSpawnType == ModSpawn.DEFAULT_MOD && modPool.Count == 1)
            {
                chosenModResult.Found = true;
                chosenModResult.Incompatible = false;
                chosenModResult.ChosenTemplate = chosenTpl;

                break;
            }

            // Check if existing weapon mods are incompatible with chosen item
            var existingItemBlockingChoice = weapon.FirstOrDefault(item =>
                pickedItemDetails.Value.Properties.ConflictingItems?.Contains(item.Template) ?? false
            );
            if (existingItemBlockingChoice is not null)
            {
                // Give max of x attempts of picking a mod if blocked by another
                // OR Blocked and mod pool only had 1 item
                if (blockedAttemptCount > maxBlockedAttempts || modPool.Count == 1)
                {
                    blockedAttemptCount = 0; // reset
                    //chosenModResult.SlotBlocked = true; // Later in code we try to find replacement, but only when "slotBlocked" is not true
                    chosenModResult.Reason = "Blocked";

                    break;
                }

                blockedAttemptCount++;
                // Not compatible - Try again
                continue;
            }

            // Edge case - Some mod combos will never work, make sure this isn't the case
            if (__instance.WeaponModComboIsIncompatible(weapon, chosenTpl))
            {
                chosenModResult.Reason = $"Chosen weapon mod: {chosenTpl} can never be compatible with existing weapon mods";
                break;
            }

            // Success
            chosenModResult.Found = true;
            chosenModResult.Incompatible = false;
            chosenModResult.ChosenTemplate = chosenTpl;

            break;
        }

        __result = chosenModResult;
        return false;
    }
}