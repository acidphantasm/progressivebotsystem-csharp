namespace _progressiveBotSystem.Globals;

public class TierInformation
{
    public List<TierData> Tiers =
    [
        new TierData()
        {
            Tier = 1,
            PlayerMinLevel = 1,
            PlayerMaxLevel = 10,
            BotMinLevelVariance = 10,
            BotMaxLevelVariance = 5,
            ScavMinLevelVariance = 10,
            ScavMaxLevelVariance = 5,
        },

        new TierData()
        {
            Tier = 2,
            PlayerMinLevel = 11,
            PlayerMaxLevel = 20,
            BotMinLevelVariance = 10,
            BotMaxLevelVariance = 5,
            ScavMinLevelVariance = 10,
            ScavMaxLevelVariance = 5,
        },

        new TierData()
        {
            Tier = 3,
            PlayerMinLevel = 21,
            PlayerMaxLevel = 30,
            BotMinLevelVariance = 10,
            BotMaxLevelVariance = 5,
            ScavMinLevelVariance = 10,
            ScavMaxLevelVariance = 5,
        },

        new TierData()
        {
            Tier = 4,
            PlayerMinLevel = 31,
            PlayerMaxLevel = 40,
            BotMinLevelVariance = 10,
            BotMaxLevelVariance = 5,
            ScavMinLevelVariance = 10,
            ScavMaxLevelVariance = 5,
        },

        new TierData()
        {
            Tier = 5,
            PlayerMinLevel = 41,
            PlayerMaxLevel = 50,
            BotMinLevelVariance = 10,
            BotMaxLevelVariance = 5,
            ScavMinLevelVariance = 10,
            ScavMaxLevelVariance = 5,
        },

        new TierData()
        {
            Tier = 6,
            PlayerMinLevel = 51,
            PlayerMaxLevel = 60,
            BotMinLevelVariance = 10,
            BotMaxLevelVariance = 5,
            ScavMinLevelVariance = 10,
            ScavMaxLevelVariance = 5,
        },

        new TierData()
        {
            Tier = 7,
            PlayerMinLevel = 61,
            PlayerMaxLevel = 100,
            BotMinLevelVariance = 10,
            BotMaxLevelVariance = 5,
            ScavMinLevelVariance = 10,
            ScavMaxLevelVariance = 5,
        }
    ];
}

public class TierData
{
    public int Tier { get; set; }
    public int PlayerMinLevel { get; set; }
    public int PlayerMaxLevel { get; set; }
    public int BotMinLevelVariance { get; set; }
    public int BotMaxLevelVariance { get; set; }
    public int ScavMinLevelVariance { get; set; }
    public int ScavMaxLevelVariance { get; set; }
}