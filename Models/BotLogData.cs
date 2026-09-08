using ProgressiveBotSystem.Models.Enums;

namespace ProgressiveBotSystem.Models;

public class BotLogData
{
    public DateTime Timestamp { get; set; }

    public BotLogType BotType { get; set; }

    public bool IsApbsBot { get; set; }

    public int Tier { get; set; } = 0;
    public bool PovertyBot { get; set; }

    public string Role { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public string Difficulty { get; set; } = string.Empty;
    public string? GameVersion { get; set; }
    public int PrestigeLevel { get; set; }
    public string? DogTagId { get; set; }

    public int GrenadeCount { get; set; }

    public BotWeaponLog? Primary { get; set; }
    public BotWeaponLog? Secondary { get; set; }
    public BotWeaponLog? Holster { get; set; }
    public BotWeaponLog? Melee { get; set; }

    public BotEquipmentLog? Helmet { get; set; }
    public BotEquipmentLog? NightVision { get; set; }
    public BotEquipmentLog? EarPiece { get; set; }

    public BotArmorLog? Armor { get; set; }
}

public class BotWeaponLog
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public string? CaliberId { get; set; }
    public string? Caliber { get; set; }
}

public class BotEquipmentLog
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class BotArmorLog
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public int? FrontPlateClass { get; set; }
    public int? BackPlateClass { get; set; }
    public int? LeftPlateClass { get; set; }
    public int? RightPlateClass { get; set; }
}
