namespace ProgressiveBotSystem.Patches;

using System.Reflection;
using Globals;
using HarmonyLib;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Generators.Bot;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Utils;

[Injectable]
public class GeneratePlayerScavPatch : AbstractPatch
{
    private static BotTable _botTable = default!;
    private static RandomUtil _randomUtil = default!;

    public GeneratePlayerScavPatch(BotTable botTable, RandomUtil randomUtil)
    {
        _botTable = botTable;
        _randomUtil = randomUtil;
    }

    protected override MethodBase GetTargetMethod() =>
        AccessTools.Method(typeof(BotGenerator), "GeneratePlayerScav");

    [PatchPrefix]
    public static void Prefix(ref string role, out string __state)
    {
        __state = string.Empty;

        if (!ModConfig.Config.PlayerScavConfig.Enable)
        {
            return;
        }

        if (!ModConfig.Config.PlayerScavConfig.AllowBossRegeneration)
        {
            return;
        }

        if (ModConfig.Config.PlayerScavConfig.AllowedBosses.Count == 0)
        {
            return;
        }

        // Maybe compatibility with Skills Extended?
        if (role == "sectantWarrior")
        {
            return;
        }

        if (!_randomUtil.GetChance100(ModConfig.Config.PlayerScavConfig.Chance))
        {
            return;
        }

        var selectedRole = _randomUtil.GetRandomElement(
            ModConfig.Config.PlayerScavConfig.AllowedBosses
        );
        role = selectedRole;
        __state = selectedRole.ToLowerInvariant();
    }

    [PatchPostfix]
    public static void Postfix(PmcData __result, string __state)
    {
        if (string.IsNullOrEmpty(__state))
        {
            return;
        }

        if (!_botTable.Types.TryGetValue(__state, out var bot))
        {
            return;
        }

        if (__result.Customization is null || bot is null)
        {
            return;
        }

        __result.Customization.Body = bot.BotAppearance.Body.First().Key;
        __result.Customization.Feet = bot.BotAppearance.Feet.First().Key;
        __result.Customization.Head = bot.BotAppearance.Head.Last().Key;
        __result.Customization.Hands = bot.BotAppearance.Hands.First().Key;
        __result.Customization.Voice = bot.BotAppearance.Voice.First().Key;

        if (!ModConfig.Config.PlayerScavConfig.UseBossHealth)
        {
            return;
        }

        var newBotBodyParts = bot.BotHealth.BodyParts.FirstOrDefault();
        if (newBotBodyParts is null)
        {
            return;
        }

        if (__result.Health?.BodyParts is null)
        {
            return;
        }

        foreach (var (partName, partProperties) in __result.Health.BodyParts)
        {
            if (partProperties.Health is null)
            {
                continue;
            }

            var sourceMinMax = partName switch
            {
                "Head" => newBotBodyParts.Head,
                "Chest" => newBotBodyParts.Chest,
                "Stomach" => newBotBodyParts.Stomach,
                "LeftArm" => newBotBodyParts.LeftArm,
                "RightArm" => newBotBodyParts.RightArm,
                "LeftLeg" => newBotBodyParts.LeftLeg,
                "RightLeg" => newBotBodyParts.RightLeg,
                _ => null,
            };

            if (sourceMinMax == null)
            {
                continue;
            }

            partProperties.Health.Maximum = sourceMinMax.Max;
            partProperties.Health.Current = sourceMinMax.Max;
        }
    }
}
