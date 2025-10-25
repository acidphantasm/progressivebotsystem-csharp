using SPTarkov.Server.Core.Generators.WeaponGen;

namespace _progressiveBotSystem.Generators.WeaponGen;

public interface IApbsInventoryMagGen
{
    public int GetPriority();
    public bool CanHandleInventoryMagGen(ApbsInventoryMagGen inventoryMagGen);
    public void Process(ApbsInventoryMagGen inventoryMagGen);
}
