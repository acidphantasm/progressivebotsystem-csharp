namespace _progressiveBotSystem.Models;

public class BotLogData
{
    public int Tier { get; set; } = 0;
    public string Role { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; } = 0;
    public string Difficulty { get; set; } = string.Empty;
    public string GameVersion { get; set; } = string.Empty;
    public int PrestigeLevel { get; set; } = 0;
    public string PrimaryWeaponId { get; set; } = string.Empty;
    public string PrimaryWeaponCaliber { get; set; } = string.Empty;
    public string SecondaryWeaponId { get; set; } = string.Empty;
    public string SecondaryWeaponCaliber { get; set; } = string.Empty;
    public string HolsterWeaponId { get; set; } = string.Empty;
    public string HolsterWeaponCaliber { get; set; } = string.Empty;
    public string ScabbardId { get; set; } = string.Empty;
    public string HelmetId { get; set; } = string.Empty;
    public string NightVisionId { get; set; } = string.Empty;
    public string EarPieceId { get; set; } = string.Empty;
    public bool CanHavePlates { get; set; } = false;
    public string ArmourVestId { get; set; } = string.Empty;
    public string FrontPlateId { get; set; } = string.Empty;
    public string BackPlateId { get; set; } = string.Empty;
    public string LeftSidePlateId { get; set; } = string.Empty;
    public string RightSidePlateId { get; set; } = string.Empty;
    public int GrenadeCount { get; set; } = 0;
}
