using _progressiveBotSystem.Helpers;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Utils;

namespace _progressiveBotSystem.Generators.WeaponGen.Implementations;

[Injectable]
public class ApbsUbglExternalMagGen(
    BotWeaponGeneratorHelper botWeaponGeneratorHelper,
    RandomUtil randomUtil,
    BotEquipmentHelper botEquipmentHelper) : ApbsInventoryMagGen, IApbsInventoryMagGen
{
    private IApbsInventoryMagGen _apbsInventoryMagGenImplementation;

    public int GetPriority()
    {
        return 1;
    }

    public bool CanHandleInventoryMagGen(ApbsInventoryMagGen inventoryMagGen)
    {
        return inventoryMagGen.GetWeaponTemplate().Parent == BaseClasses.LAUNCHER;
    }

    public void Process(ApbsInventoryMagGen inventoryMagGen)
    {
        var bulletCount = botWeaponGeneratorHelper.GetRandomizedBulletCount(
            inventoryMagGen.GetMagCount(),
            inventoryMagGen.GetMagazineTemplate()
        );
        
        var rerollConfig = inventoryMagGen.GetRerollDetails();
        if (rerollConfig.Enable && randomUtil.GetChance100(rerollConfig.Chance))
        {
            var weapon = inventoryMagGen.GetWeaponTemplate();
            var ammoTable = botEquipmentHelper.GetAmmoByBotRole(inventoryMagGen.GetBotRole(), inventoryMagGen.GetTier());
            var rerolledAmmoTpl = inventoryMagGen.GetWeightedCompatibleAmmo(ammoTable, weapon);
            
            botWeaponGeneratorHelper.AddAmmoIntoEquipmentSlots(
                inventoryMagGen.GetBotId(),
                rerolledAmmoTpl,
                (int)bulletCount,
                inventoryMagGen.GetPmcInventory(),
                null
            );
        }
        
        botWeaponGeneratorHelper.AddAmmoIntoEquipmentSlots(
            inventoryMagGen.GetBotId(),
            inventoryMagGen.GetAmmoTemplate().Id,
            (int)bulletCount,
            inventoryMagGen.GetPmcInventory(),
            [EquipmentSlots.TacticalVest, EquipmentSlots.Pockets]
        );
    }
}
