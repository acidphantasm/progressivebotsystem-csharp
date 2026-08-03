using ProgressiveBotSystem.Helpers;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Utils;

namespace ProgressiveBotSystem.Generators.WeaponGen.Implementations;

using SPTarkov.Server.Core.Helpers.Bot;

[Injectable]
public class ApbsInternalMagazineInventoryMagGen(
    BotWeaponGeneratorHelper botWeaponGeneratorHelper,
    RandomUtil randomUtil,
    BotEquipmentHelper botEquipmentHelper,
    InventoryMagGenHelper inventoryMagGenHelper) : ApbsInventoryMagGen, IApbsInventoryMagGen
{
    public int GetPriority()
    {
        return 0;
    }

    public bool CanHandleInventoryMagGen(ApbsInventoryMagGen inventoryMagGen)
    {
        return inventoryMagGen.GetMagazineTemplate().Properties.ReloadMagType == ReloadMode.InternalMagazine;
    }

    public void Process(ApbsInventoryMagGen inventoryMagGen)
    {
        var bulletCount = inventoryMagGenHelper.GetRandomizedBulletCount(
            inventoryMagGen.GetMagCount(),
            inventoryMagGen.GetMagazineTemplate()
        );
        
        var rerollConfig = inventoryMagGen.GetRerollDetails();
        if (rerollConfig.Enable && randomUtil.GetChance100(rerollConfig.Chance))
        {
            var weapon = inventoryMagGen.GetWeaponTemplate();
            var ammoTable = botEquipmentHelper.GetAmmoByBotRole(inventoryMagGen.GetBotRole(), inventoryMagGen.GetTier());
            var rerolledAmmoTpl = inventoryMagGenHelper.GetWeightedCompatibleAmmo(ammoTable, weapon);

            if (bulletCount > 20)
            {
                bulletCount = randomUtil.GetInt(10, (int)bulletCount);
            }
            
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
            null
        );
    }
}
