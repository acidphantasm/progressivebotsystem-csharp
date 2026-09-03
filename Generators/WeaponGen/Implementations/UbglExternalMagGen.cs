namespace ProgressiveBotSystem.Generators.WeaponGen.Implementations;

using Helpers;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers.Bot;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Utils;

[Injectable]
public class ApbsUbglExternalMagGen(
    BotWeaponGeneratorHelper botWeaponGeneratorHelper,
    RandomUtil randomUtil,
    BotEquipmentHelper botEquipmentHelper,
    InventoryMagGenHelper inventoryMagGenHelper
) : ApbsInventoryMagGen, IApbsInventoryMagGen
{
    public int GetPriority() => 1;

    public bool CanHandleInventoryMagGen(ApbsInventoryMagGen inventoryMagGen) =>
        inventoryMagGen.GetWeaponTemplate().Parent == BaseClasses.LAUNCHER;

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
            var ammoTable = botEquipmentHelper.GetAmmoByBotRole(
                inventoryMagGen.GetBotRole(),
                inventoryMagGen.GetTier()
            );
            var rerolledAmmoTpl = inventoryMagGenHelper.GetWeightedCompatibleAmmo(
                ammoTable,
                weapon
            );

            botWeaponGeneratorHelper.AddAmmoIntoEquipmentSlots(
                inventoryMagGen.GetBotId(),
                rerolledAmmoTpl,
                (int)bulletCount,
                inventoryMagGen.GetPmcInventory()
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
