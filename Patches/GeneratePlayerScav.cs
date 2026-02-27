using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Utils;
using System.Reflection;
using _progressiveBotSystem.Globals;
using HarmonyLib;
using SPTarkov.Server.Core.Generators;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Services;

namespace _progressiveBotSystem.Patches;

public class GeneratePlayerScav_Patch : AbstractPatch
{
    
    private static readonly RandomUtil RandomUtil = ServiceLocator.ServiceProvider.GetRequiredService<RandomUtil>();
    private static readonly DatabaseService DatabaseService = ServiceLocator.ServiceProvider.GetRequiredService<DatabaseService>();
    
    
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(BotGenerator),"GeneratePlayerScav");
    }

    [PatchPrefix]
    public static void Prefix(ref string role, out string __state)
    {
        __state = String.Empty;
        
        if (!ModConfig.Config.PlayerScavConfig.Enable)
            return;

        if (ModConfig.Config.PlayerScavConfig.AllowedBosses.Count == 0)
            return;
        
        // Maybe compatibility with Skills Extended?
        if (role == "sectantWarrior")
            return;

        if (!RandomUtil.GetChance100(ModConfig.Config.PlayerScavConfig.Chance))
            return;

        var selectedRole = RandomUtil.GetRandomElement(ModConfig.Config.PlayerScavConfig.AllowedBosses);
        role = selectedRole;
        __state = selectedRole.ToLowerInvariant();
    }
    
    [PatchPostfix]
    public static void Postfix(PmcData __result, string __state)
    {
        if(string.IsNullOrEmpty(__state))
            return;

        if (!DatabaseService.GetBots().Types.TryGetValue(__state, out var bot))
            return;

        if (__result.Customization is null || bot is null)
            return;
        
        __result.Customization.Body = bot.BotAppearance.Body.First().Key;
        __result.Customization.Feet = bot.BotAppearance.Feet.First().Key;
        __result.Customization.Head = bot.BotAppearance.Head.Last().Key;
        __result.Customization.Hands = bot.BotAppearance.Hands.First().Key;
        __result.Customization.Voice = bot.BotAppearance.Voice.First().Key;

        if (!ModConfig.Config.PlayerScavConfig.UseBossHealth)
            return;
        
        var newBotBodyParts = bot.BotHealth.BodyParts.FirstOrDefault();
        if (newBotBodyParts is null)
            return;
        
        if (__result.Health?.BodyParts is null)
            return;
        
        foreach (var (partName, partProperties) in __result.Health.BodyParts)
        {
            if (partProperties.Health is null)
                continue;

            var sourceMinMax = partName switch
            {
                "Head" => newBotBodyParts.Head,
                "Chest" => newBotBodyParts.Chest,
                "Stomach" => newBotBodyParts.Stomach,
                "LeftArm" => newBotBodyParts.LeftArm,
                "RightArm" => newBotBodyParts.RightArm,
                "LeftLeg" => newBotBodyParts.LeftLeg,
                "RightLeg" => newBotBodyParts.RightLeg,
                _ => null
            };

            if (sourceMinMax == null)
                continue;

            partProperties.Health.Maximum = sourceMinMax.Max;
            partProperties.Health.Current = sourceMinMax.Max;
        }
    }
}