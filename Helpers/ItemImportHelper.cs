using System.Collections.Frozen;
using _progressiveBotSystem.Globals;
using _progressiveBotSystem.Models.Enums;
using _progressiveBotSystem.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;
using Path = System.IO.Path;

namespace _progressiveBotSystem.Helpers;

[Injectable(InjectionType.Singleton)]
public class ItemImportHelper(
    ApbsLogger apbsLogger,
    JsonUtil jsonUtil,
    ItemHelper itemHelper,
    DatabaseService databaseService,
    BotBlacklistHelper botBlacklistHelper)
{
    private bool _alreadyRan;
    
    private HashSet<MongoId> _vanillaEquipmentLookup = new();
    private HashSet<MongoId> _vanillaAttachmentLookup = new();
    private HashSet<MongoId> _vanillaAmmoLookup = new();
    private HashSet<MongoId> _vanillaClothingLookup = new();
    
    
    private Dictionary<int, HashSet<MongoId>> _fullModConfigBlacklist = new();
    
    private Dictionary<ApbsEquipmentSlots, HashSet<MongoId>> _vanillaEquipmentSlotDictionary = new();
    private Dictionary<string, HashSet<MongoId>> _vanillaAmmoDictionary = new();
    private Dictionary<string, Dictionary<string, HashSet<MongoId>>> _vanillaClothingBotSlotDictionary = new();
    
    // Because we're looping the database, and holster is super special, we cache that slot once from default inventory instead of repeated lookups
    private HashSet<MongoId>? _holsterAllowedItems;
    private bool _holsterCacheInitialized;
    private readonly Lock _holsterLock = new();
    
    // Custom Base classes
    private static readonly MongoId BaseClassPackNStrapBelt = "6815465859b8c6ff13f94026";
    
    private readonly ApbsEquipmentSlots[] _shortRangeSlots =
    [
        ApbsEquipmentSlots.FirstPrimaryWeapon_ShortRange,
        ApbsEquipmentSlots.SecondPrimaryWeapon_ShortRange,
    ];

    private readonly ApbsEquipmentSlots[] _longRangeSlots =
    [
        ApbsEquipmentSlots.FirstPrimaryWeapon_LongRange,
        ApbsEquipmentSlots.SecondPrimaryWeapon_LongRange,
    ];
    
    private readonly FrozenSet<MongoId> _shortRangeBaseClasses =
    [
        BaseClasses.ASSAULT_RIFLE,
        BaseClasses.GRENADE_LAUNCHER,
        BaseClasses.MACHINE_GUN,
        BaseClasses.ROCKET_LAUNCHER,
        BaseClasses.SHOTGUN,
        BaseClasses.SMG,
    ];
    
    private readonly FrozenSet<MongoId> _longRangeBaseClasses =
    [
        BaseClasses.ASSAULT_CARBINE,
        BaseClasses.SNIPER_RIFLE,
        BaseClasses.MARKSMAN_RIFLE,
    ];
    
    private readonly FrozenSet<MongoId> _holsterBaseClasses =
    [
        BaseClasses.PISTOL,
    ];

    private readonly FrozenSet<MongoId> _allImportableWeaponBaseClasses =
    [
        BaseClasses.ASSAULT_CARBINE,
        BaseClasses.ASSAULT_RIFLE,
        BaseClasses.SNIPER_RIFLE,
        BaseClasses.MARKSMAN_RIFLE,
        BaseClasses.GRENADE_LAUNCHER,
        BaseClasses.MACHINE_GUN,
        BaseClasses.ROCKET_LAUNCHER,
        BaseClasses.SHOTGUN,
        BaseClasses.SMG,
        BaseClasses.KNIFE,
    ];

    private readonly FrozenSet<MongoId> _allImportableEquipmentBaseClasses =
    [
        BaseClasses.ARM_BAND,
        BaseClasses.ARMOR,
        BaseClasses.BACKPACK,
        BaseClasses.HEADPHONES,
        BaseClasses.VISORS,
        BaseClasses.FACE_COVER,
        BaseClasses.HEADWEAR,
        BaseClasses.VEST,
        BaseClassPackNStrapBelt
    ];

    private readonly FrozenSet<MongoId> _bannedItems =
    [
        ItemTpl.ASSAULTRIFLE_MASTER_HAND,
        ItemTpl.MACHINEGUN_NSV_UTYOS_127X108_HEAVY_MACHINE_GUN,
        ItemTpl.SIGNALPISTOL_ZID_SP81_26X75_SIGNAL_PISTOL,
        ItemTpl.FLARE_RSP30_REACTIVE_SIGNAL_CARTRIDGE_BLUE,
        ItemTpl.FLARE_RSP30_REACTIVE_SIGNAL_CARTRIDGE_FIREWORK,
        ItemTpl.FLARE_RSP30_REACTIVE_SIGNAL_CARTRIDGE_GREEN,
        ItemTpl.FLARE_RSP30_REACTIVE_SIGNAL_CARTRIDGE_RED,
        ItemTpl.FLARE_RSP30_REACTIVE_SIGNAL_CARTRIDGE_SPECIAL_YELLOW,
        ItemTpl.FLARE_RSP30_REACTIVE_SIGNAL_CARTRIDGE_YELLOW,
        ItemTpl.FLARE_ROP30_REACTIVE_FLARE_CARTRIDGE_WHITE,
        ItemTpl.GRENADELAUNCHER_FN40GL_02,
        ItemTpl.GRENADELAUNCHER_FN40GL_03,
        ItemTpl.KNIFE_SUPERFORS_DB_2020_DEAD_BLOW_HAMMER,
        ItemTpl.KNIFE_INFECTIOUS_STRIKE,
        ItemTpl.KNIFE_CHAINED_LABRYS,
        ItemTpl.AMMO_23X75_CHER7M,
        ItemTpl.AMMO_40X46_M716,
        ItemTpl.AMMO_556X45_6MM_BB,
        "683df636e1e3fa1bf165f7ed", // Weird dual caliber gun from, uhhh, ECOT? I dont member
        "660b2d05cec10101410e7d7b", // avenger a10 warthog weapon from fire support
        "68f5333abc75f09d96d97500", // FPV Drone mod ID (probably not needed, but I'm not chancing it)
        "690e9265772bfce1d043966d", // FPV Drone mod ID (probably not needed, but I'm not chancing it)
        "695f8753d24480b7b1921023", // FPV Drone mod ID (probably not needed, but I'm not chancing it)
        "68f51bfb3e92385d1908db68", // FPV Drone mod ID (probably not needed, but I'm not chancing it)
        "696504ca8ce4c9b2404e1b32", // FPV Drone mod ID (probably not needed, but I'm not chancing it)
        "6964ea3a5e4c1218314e1b2f", // FPV Drone mod ID (probably not needed, but I'm not chancing it)
        "69669ea64847b58fd5393f71" // FPV Drone mod ID (probably not needed, but I'm not chancing it)
    ];

    private readonly FrozenSet<MongoId> _bannedAttachments =
    [
        "686a5a6247c881f613196f91", // canted aiming attachment from echos of tarkov
        "b098f4d751ddc6246acdf160", // B-22 Attachment from EpicRangeTime-Weapons
        "b2d57758abe9bb9345c58e4a", // 34mm gieselle mount from EpicRangeTime-Weapons
        "67ea8b32e0d7701fc6bfc5bf", // 34mm gieselle mount from EpicRangeTime-Weapons
        "6cec33dd595d9c2d4e531eb7", // Weird split handguard from EpicRangeTime-Weapons
        "7422ca92107da0fc0e26f7d9", // Weird split handguard from EpicRangeTime-Weapons
        "cbba369e0fbb09a1eda36c83", // Weird split handguard from EpicRangeTime-Weapons
        "672e2e75c7c7c5232e513062", // Transparent dust cover from MoxoPixel-TGC
        "672e37d178e24689d6ff50ce", // Range finder from Black-Core
        "5c110624d174af029e69734c", // NVG/Thermals
        "5d1b5e94d7ad1a2b865a96b0", // FLIR
        "6478641c19d732620e045e17", // ECHO1
        "5a1eaa87fcdbcb001865f75e", // REAPIR
        "609bab8b455afd752b2e6138", // T12W Thermal
        "63fc44e2429a8a166c7f61e6", // Zues
        "5a1ead28fcdbcb001912fa9f", // DLOC
        "5ae30e795acfc408fb139a0b", // M4A1 Dumb Gasblock
        "63fc449f5bd61c6cf3784a88", // Zues sight mount
        "5b3b6dc75acfc47a8773fb1e", // Vulcan base
        "5c11046cd174af02a012e42b", // PVS-7
        "5a7c74b3e899ef0014332c29", // DOVETAILNV
        "544a3f024bdc2d1d388b4568", // Bugged optics
        "544a3d0a4bdc2d1b388b4567", // NXS Scope
        "5cf638cbd7f00c06595bc936",
        "576fd4ec2459777f0b518431",
        "5c82343a2e221644f31c0611",
        "5d0a29ead7ad1a0026013f27",
        "5dfe6104585a0c3e995c7b82",
        "618b9643526131765025ab35",
        "618bab21526131765025ab3f",
        "6171407e50224f204c1da3c5",
        "5aa66a9be5b5b0214e506e89", // 34mm optic mounts
        "5aa66be6e5b5b0214e506e97",
        "5aa66c72e5b5b00016327c93",
        "5c86592b2e2216000e69e77c",
        "61713cc4d8e3106d9806c109",
        "62811f461d5df4475f46a332",
        "6761759e7ee06333f108bf86",
        "676175789dcee773150c6925",
        "5648b62b4bdc2d9d488b4585", // General Attachments
        "5c0e2ff6d174af02a1659d4a",
        "5c0e2f5cd174af02a012cfc9",
        "5c6592372e221600133e47d7",
        "544a378f4bdc2d30388b4567",
        "5d1340bdd7ad1a0e8d245aab",
        "6764139c44b3c96e7b0e2f7b",
        "67641a851b2899700609901a",
        "67641b461c2eb66ade05dba6",
        "676176a162e0497044079f46",
        "67641bec4ad898aa100c1079",
        "628120f210e26c1f344e6558", // AXMC .308 conversion part
        "62811d61578c54356d6d67ea", // AXMC .308 conversion part
        "628120415631d45211793c99", // AXMC .308 conversion part
        "6281214c1d5df4475f46a33a", // AXMC .308 conversion part
        "6281215b4fa03b6b6c35dc6c", // AXMC .308 conversion part
        "628121651d5df4475f46a33c", // AXMC .308 conversion part
        "6761765f1f08ed5e8800b7a6",
        "671d85439ae8365d69117ba6",
        "6241c2c2117ad530666a5108", // airsoft mag
        "670e8eab8c1bb0e5a7075acf",
        "671d8617a3e45c1f5908278c",
        "671d8ac8a3e45c1f59082799",
        "671d8b38b769f0d88c0950f8",
        "671d8b8c0959c721a50ca838",
        "55d5f46a4bdc2d1b198b4567",
        "57ffb0062459777a045af529",
        "5926d2be86f774134d668e4e",
        "5a37cb10c4a282329a73b4e7",
        "5d0a29fed7ad1a002769ad08",
        "57dc334d245977597164366f",
        "618a75c9a3884f56c957ca1b",
        "59e5f5a486f7746c530b3ce2",
        "55d481904bdc2d8c2f8b456a",
        "5b1fb3e15acfc4001637f068",
        "564ca9df4bdc2d35148b4569",
        "61a4cda622af7f4f6a3ce617" // Rhino speedloader
    ];
    
    private readonly FrozenSet<MongoId> _tier4Optics = 
    [            
        "5c0517910db83400232ffee5",
        "558022b54bdc2dac148b458d",
        "58491f3324597764bc48fa02",
        "584924ec24597768f12ae244",
        "60a23797a37c940de7062d02",
        "59f9d81586f7744c7506ee62",
        "5b2389515acfc4771e1be0c0",
        "5b3b99265acfc4704b4a1afb",
        "5b31163c5acfc400153b71cb",
        "64785e7c19d732620e045e15",
        "655f13e0a246670fb0373245",
        "6567e751a715f85433025998",
        "67617ec9ea1e82ea5e103054",
        "672e37d19f3e60fb0cbbe568",
        "5649a2464bdc2d91118b45a8"
    ];
    
    private readonly FrozenSet<MongoId> _modScope000Whitelist = 
    [
        "5b2388675acfc4771e1be0be",
        "5b3b99475acfc432ff4dcbee",
        "5a37cb10c4a282329a73b4e7",
        "617151c1d92c473c770214ab",
        "57c5ac0824597754771e88a9",
        "6567e7681265c8a131069b0f",
        "67617ec9ea1e82ea5e103054",
        "672e37d19f3e60fb0cbbe568"
    ];
    
    private readonly FrozenSet<MongoId> _foldingModSights = 
    [
        "5caf16a2ae92152ac412efbc",
        "61816fcad92c473c770215cc",
        "61817865d3a39d50044c13a4",
        "5bb20e49d4351e3bac1212de",
        "5ba26b01d4351e0085325a51",
        "5ba26b17d4351e00367f9bdd",
        "5c1780312e221602b66cc189",
        "5c17804b2e2216152006c02f",
        "5dfa3d7ac41b2312ea33362a",
        "5dfa3d950dee1b22f862eae0",
        "5fb6564947ce63734e3fa1da",
        "5fb6567747ce63734e3fa1dc",
        "5bc09a18d4351e003562b68e",
        "5bc09a30d4351e00367fb7c8",
        "5c18b90d2e2216152142466b",
        "5c18b9192e2216398b5a8104",
        "5fc0fa362770a0045c59c677",
        "5fc0fa957283c4046c58147e",
        "5894a73486f77426d259076c",
        "5894a81786f77427140b8347"
    ];

    private readonly FrozenSet<MongoId> _armouryBossVariantWeapons =
    [
        "677ca3e62e9e964a11a55d8e", // golden ae50 (reshala) holster
        "679a6a534f3d279c99b135b9", // custom aen94 (gluhar) primary
        "690ebacfc047a9a9f1a98782", // RO991 SMG (gluhar) secondary
        "676b4e2ff185a450a0b300b4", // wages of sin (killa) primary
        "68677d09339b397ed3d37522", // sr2 udav (killa)  holster
        "6868d249cdee524f8c0ba45f", // beretta 92fs (knight + bigpipe + birdeye) holster
        "6657bd4d3a4d6e7c33fd2fdc", // camo svd (shturman) primary
        "686093d590c3dce07984c38a", // aek973 (shturman) secondary
        "68e0002a0d6a4dab56810fbd", // m78 lmg (parmesan) primary
        "6840ebf5b8687ba34f8dfbca", // browning auto-5 shotty (parmesan) secondary
        "684e32eaec9f5eb3cacc7ca7", // msr .300 (lighthouse shitter) primary
        "68a3836826dffa87b5767c04", // axmc .300 (lighthouse shitter) primary
        "6945dc69fe52c2415de357f7", // xm AMR op af (lighthouse shitter) primary
        "6761b213607f9a6f79017c7e", // pdw .300 (big pipe) primary
        "69161a1d649768162e8219ef", // custom masada (big pipe) primary
        "66e718dc498d978477e0ba75", // m249 lmg (big pipe) secondary
        "deee28079e76d537f532021c", // .338 lm (birdeye) primary
        "1bf618e47cce6d69bec01e9f", // .338 lm (birdeye) primary
        "68fd7d14ff5a09197b5ab82a", // cheytac m2000 (birdeye) primary
        "682d460e951a926af552d764", // ak5c custom (birdeye) secondary
        "68b7f4060a4536984f82cf4b", // mk23 .45 (birdeye) holster
    ];
    
    private readonly Dictionary<MongoId, string[]> _bossWeaponLookup =
    new()
    {
        ["677ca3e62e9e964a11a55d8e"] = ["bossbully"],

        ["679a6a534f3d279c99b135b9"] = ["bossgluhar"],
        ["690ebacfc047a9a9f1a98782"] = ["bossgluhar"],

        ["676b4e2ff185a450a0b300b4"] = ["bosskilla"],
        ["68677d09339b397ed3d37522"] = ["bosskilla"],

        ["6868d249cdee524f8c0ba45f"] =
        [
            "bossknight",
            "followerbigpipe",
            "followerbirdeye"
        ],

        ["6657bd4d3a4d6e7c33fd2fdc"] = ["bosskojaniy"],
        ["686093d590c3dce07984c38a"] = ["bosskojaniy"],

        ["68e0002a0d6a4dab56810fbd"] = ["bosspartisan"],
        ["6840ebf5b8687ba34f8dfbca"] = ["bosspartisan"],

        ["684e32eaec9f5eb3cacc7ca7"] = ["bosszryachiy"],
        ["68a3836826dffa87b5767c04"] = ["bosszryachiy"],
        ["6945dc69fe52c2415de357f7"] = ["bosszryachiy"],

        ["6761b213607f9a6f79017c7e"] = ["followerbigpipe"],
        ["69161a1d649768162e8219ef"] = ["followerbigpipe"],
        ["66e718dc498d978477e0ba75"] = ["followerbigpipe"],

        ["deee28079e76d537f532021c"] = ["followerbirdeye"],
        ["1bf618e47cce6d69bec01e9f"] = ["followerbirdeye"],
        ["68fd7d14ff5a09197b5ab82a"] = ["followerbirdeye"],
        ["682d460e951a926af552d764"] = ["followerbirdeye"],
        ["68b7f4060a4536984f82cf4b"] = ["followerbirdeye"],
    };
    
    /// <summary>
    ///     Helper to skip importing entirely
    /// </summary>
    public bool ShouldSkipImport()
    {
        return !ModConfig.Config.CompatibilityConfig.EnableModdedEquipment &&
               !ModConfig.Config.CompatibilityConfig.EnableModdedAttachments &&
               !ModConfig.Config.CompatibilityConfig.EnableModdedWeapons &&
               (!ModConfig.Config.CompatibilityConfig.EnableModdedClothing ||
                ModConfig.Config.PmcBots.AdditionalOptions.SeasonalPmcAppearance);
    }
    
    /// <summary>
    ///     Validates any configuration options, corrects them if needed and logs if they're invalid
    /// </summary>
    public void ValidateConfig()
    {
        if (ModConfig.Config.CompatibilityConfig.InitalTierAppearance < 1 || ModConfig.Config.CompatibilityConfig.InitalTierAppearance > 7)
        {
            ModConfig.Config.CompatibilityConfig.InitalTierAppearance = 3;
            apbsLogger.Error($"Compatibility Config -> InitialTierAppearance is invalid. Defaulting to 3. Fix your config in the WebApp.");
        }

        _fullModConfigBlacklist = BuildFullModConfigBlacklistByTier();
    }

    private Dictionary<int, HashSet<MongoId>> BuildFullModConfigBlacklistByTier()
    {
        var result = new Dictionary<int, HashSet<MongoId>>();

        for (var tier = 1; tier <= 7; tier++)
        {
            var tierSet = new HashSet<MongoId>();

            Add(tierSet, botBlacklistHelper.GetWeaponBlacklistTierData(tier));
            Add(tierSet, botBlacklistHelper.GetAmmoBlacklistTierData(tier));
            Add(tierSet, botBlacklistHelper.GetAttachmentBlacklistTierData(tier));
            Add(tierSet, botBlacklistHelper.GetClothingBlacklistTierData(tier));
            Add(tierSet, botBlacklistHelper.GetEquipmentBlacklistTierData(tier));

            result[tier] = tierSet;
        }

        return result;
    }

    private static void Add(HashSet<MongoId> set, List<string>? list)
    {
        if (list is null) return;

        foreach (var id in list)
            set.Add(id);
    }
    
    /// <summary>
    ///     Build the vanilla dictionaries that get generated the first time you run the server after installing APBS
    ///     This is the dictionary that contains all vanilla items that live in the same types of data APBS can import
    /// </summary>
    public async Task BuildVanillaDictionaries()
    {
        if (_alreadyRan) return;

        var equipmentTask = ValidateVanillaEquipmentDatabase();
        var ammoTask = ValidateVanillaAmmoDatabase();
        var attachmentTask = ValidateVanillaAttachmentDatabase();
        var clothingTask = ValidateVanillaClothingDatabase();

        await Task.WhenAll(equipmentTask, ammoTask, attachmentTask);

        _vanillaEquipmentSlotDictionary = equipmentTask.Result;
        _vanillaAmmoDictionary = ammoTask.Result;
        _vanillaAttachmentLookup = attachmentTask.Result;
        _vanillaClothingBotSlotDictionary = clothingTask.Result;

        _vanillaEquipmentLookup = _vanillaEquipmentSlotDictionary
            .SelectMany(x => x.Value)
            .ToHashSet();

        _vanillaAmmoLookup = _vanillaAmmoDictionary
            .SelectMany(x => x.Value)
            .ToHashSet();

        _vanillaClothingLookup = _vanillaClothingBotSlotDictionary
            .SelectMany(x => x.Value)
            .SelectMany(x => x.Value)
            .ToHashSet();
        
        _alreadyRan = true;
    }
    
    /// <summary>
    ///     Deserializes all the weapon / equipment JSON's from the VanillaMappings and adds those items to the dictionary
    /// </summary>
    private async Task<Dictionary<ApbsEquipmentSlots, HashSet<MongoId>>> ValidateVanillaEquipmentDatabase()
    {
        var returnDictionary = Enum
            .GetValues<ApbsEquipmentSlots>()
            .ToDictionary(slot => slot, _ => new HashSet<MongoId>());

        var primaryPath = Path.Combine(ModConfig._modPath, "GeneratedVanillaMappings-DO_NOT_TOUCH", "PrimaryWeapon.json");
        var primary = await jsonUtil.DeserializeFromFileAsync<Dictionary<MongoId, string>>(primaryPath)
                      ?? throw new ArgumentNullException(nameof(primaryPath));

        foreach (var kvp in primary)
        {
            var itemId = kvp.Key;
            var item = itemHelper.GetItem(itemId);
            if (item.Value is null) continue;

            foreach (var slot in GetSlotsForPrimaryWeapon(itemId))
            {
                returnDictionary[slot].Add(itemId);
            }
        }

        var equipmentFiles = new (string FileName, ApbsEquipmentSlots Slot)[]
        {
            ("Holster.json", ApbsEquipmentSlots.Holster),
            ("Headwear.json", ApbsEquipmentSlots.Headwear),
            ("ArmorVest.json", ApbsEquipmentSlots.ArmorVest),
            ("ArmouredRig.json", ApbsEquipmentSlots.ArmouredRig),
            ("Backpack.json", ApbsEquipmentSlots.Backpack),
            ("Earpiece.json", ApbsEquipmentSlots.Earpiece),
            ("Eyewear.json", ApbsEquipmentSlots.Eyewear),
            ("FaceCover.json", ApbsEquipmentSlots.FaceCover),
            ("Scabbard.json", ApbsEquipmentSlots.Scabbard),
            ("TacticalVest.json", ApbsEquipmentSlots.TacticalVest),
            ("ArmBand.json", ApbsEquipmentSlots.ArmBand)
        };

        foreach (var (fileName, slot) in equipmentFiles)
        {
            await AddEquipmentToSlotAsync(returnDictionary, fileName, slot);
        }

        foreach (var (slot, items) in returnDictionary)
        {
            apbsLogger.Debug($"[VANILLA] Equipment slot: {slot} contains {items.Count} items");
        }

        return returnDictionary;
    }
    
    /// <summary>
    ///     Adds item to the vanilla dictionary, this is a helper method
    ///     Called from ValidateVanillaEquipmentDatabase
    /// </summary>
    private async Task AddEquipmentToSlotAsync(Dictionary<ApbsEquipmentSlots, HashSet<MongoId>> dictionary, string fileName, ApbsEquipmentSlots slot)
    {
        var items = await jsonUtil.DeserializeFromFileAsync<Dictionary<MongoId, string>>(Path.Combine(ModConfig._modPath, "GeneratedVanillaMappings-DO_NOT_TOUCH", fileName)) ?? throw new ArgumentNullException(fileName);
        foreach (var itemId in items.Keys)
        {
            var item = itemHelper.GetItem(itemId);
            if (item.Value is null) continue;

            dictionary[slot].Add(itemId);
        }
    }

    /// <summary>
    ///     Deserializes all the ammo JSON's from the VanillaMappings and adds those items to the dictionary
    ///     We probably don't need these, but it's very quick to build this dictionary so we might as well
    /// </summary>
    private async Task<Dictionary<string, HashSet<MongoId>>> ValidateVanillaAmmoDatabase()
    {
        var returnDictionary = new Dictionary<string, HashSet<MongoId>>();

        var ammoFiles = new (string FileName, string Caliber)[]
        {
            ("Caliber9x18PM.json", "Caliber9x18PM"),
            ("Caliber9x19PARA.json", "Caliber9x19PARA"),
            ("Caliber9x21.json", "Caliber9x21"),
            ("Caliber9x33R.json", "Caliber9x33R"),
            ("Caliber9x39.json", "Caliber9x39"),
            ("Caliber12g.json", "Caliber12g"),
            ("Caliber20g.json", "Caliber20g"),
            ("Caliber23x75.json", "Caliber23x75"),
            ("Caliber40mmRU.json", "Caliber40mmRU"),
            ("Caliber40x46.json", "Caliber40x46"),
            ("Caliber46x30.json", "Caliber46x30"),
            ("Caliber57x28.json", "Caliber57x28"),
            ("Caliber68x51.json", "Caliber68x51"),
            ("Caliber86x70.json", "Caliber86x70"),
            ("Caliber127x33.json", "Caliber127x33"),
            ("Caliber127x55.json", "Caliber127x55"),
            ("Caliber127x99.json", "Caliber127x99"),
            ("Caliber366TKM.json", "Caliber366TKM"),
            ("Caliber545x39.json", "Caliber545x39"),
            ("Caliber556x45NATO.json", "Caliber556x45NATO"),
            ("Caliber725.json", "Caliber725"),
            ("Caliber762x25TT.json", "Caliber762x25TT"),
            ("Caliber762x35.json", "Caliber762x35"),
            ("Caliber762x39.json", "Caliber762x39"),
            ("Caliber762x51.json", "Caliber762x51"),
            ("Caliber762x54R.json", "Caliber762x54R"),
            ("Caliber1143x23ACP.json", "Caliber1143x23ACP")
        };

        foreach (var (fileName, caliber) in ammoFiles)
        {
            await AddAmmoToCaliberAsync(returnDictionary, fileName, caliber);
        }

        foreach (var (caliber, items) in returnDictionary)
        {
            apbsLogger.Debug($"[VANILLA] Ammo Caliber: {caliber} contains {items.Count} items");
        }

        return returnDictionary;
    }
    
    /// <summary>
    ///     Adds item to the vanilla dictionary, this is a helper method
    ///     Called from ValidateVanillaAmmoDatabase
    /// </summary>
    private async Task AddAmmoToCaliberAsync(Dictionary<string, HashSet<MongoId>> dictionary, string fileName, string caliber)
    {
        var items = await jsonUtil.DeserializeFromFileAsync<Dictionary<MongoId, string>>(Path.Combine(ModConfig._modPath, "GeneratedVanillaMappings-DO_NOT_TOUCH", fileName)) ?? throw new ArgumentNullException(fileName);
        foreach (var itemId in items.Keys)
        {
            var item = itemHelper.GetItem(itemId);
            if (item.Value is null) continue;
            
            if (!dictionary.TryGetValue(caliber, out var list))
            {
                list = new HashSet<MongoId>();
                dictionary[caliber] = list;
            }
            list.Add(itemId);
        }
    }

    /// <summary>
    ///     Deserializes the mods JSON from the VanillaMappings and adds those items to the list
    /// </summary>
    private async Task<HashSet<MongoId>> ValidateVanillaAttachmentDatabase()
    {
        var hashSet = new HashSet<MongoId>();
        
        await AddAttachmentsToModAsync(hashSet, "Mods.json");
        await AddAttachmentsToModAsync(hashSet, "ArmourPlates.json");
        
        // Debug Logging
        apbsLogger.Debug($"[VANILLA] Mods contains {hashSet.Count} items");
        return hashSet;
    }
    
    /// <summary>
    ///     Adds item to the vanilla list, this is a helper method
    ///     Called from ValidateVanillaAttachmentDatabase
    /// </summary>
    private async Task AddAttachmentsToModAsync(HashSet<MongoId> hashSet, string fileName)
    {
        var items = await jsonUtil.DeserializeFromFileAsync<Dictionary<MongoId, string>>(Path.Combine(ModConfig._modPath, "GeneratedVanillaMappings-DO_NOT_TOUCH", fileName)) ?? throw new ArgumentNullException(fileName);
        foreach (var itemId in items.Keys)
        {
            var item = itemHelper.GetItem(itemId);
            if (item.Value is null) continue;
            
            hashSet.Add(itemId);
        }
    }

    /// <summary>
    ///     Deserializes all the clothing JSON's from the VanillaMappings and adds those items to the dictionary
    /// </summary>
    private async Task<Dictionary<string, Dictionary<string, HashSet<MongoId>>>> ValidateVanillaClothingDatabase()
    {
        var returnDictionary = new Dictionary<string, Dictionary<string, HashSet<MongoId>>>();

        var clothingFiles = new (string FileName, string Side, string Slot)[]
        {
            ("BearFeet.json", "pmcBEAR", "feet"),
            ("BearHands.json", "pmcBEAR", "hands"),
            ("BearHead.json", "pmcBEAR", "head"),
            ("BearBody.json", "pmcBEAR", "body"),

            ("UsecFeet.json", "pmcUSEC", "feet"),
            ("UsecHands.json", "pmcUSEC", "hands"),
            ("UsecHead.json", "pmcUSEC", "head"),
            ("UsecBody.json", "pmcUSEC", "body")
        };

        foreach (var (fileName, side, slot) in clothingFiles)
        {
            await AddClothingToModAsync(returnDictionary, fileName, side, slot);
        }

        foreach (var (side, slotDict) in returnDictionary)
        {
            foreach (var (slot, items) in slotDict)
            {
                apbsLogger.Debug($"[VANILLA] Clothing Side: {side}, Slot: {slot} contains {items.Count} items");
            }
        }

        return returnDictionary;
    }

    /// <summary>
    /// Adds clothing items to the vanilla dictionary
    /// </summary>
    private async Task AddClothingToModAsync(Dictionary<string, Dictionary<string, HashSet<MongoId>>> dictionary, string fileName, string side, string slot)
    {
        var clothingData = await jsonUtil.DeserializeFromFileAsync<Dictionary<MongoId, string>>(Path.Combine(ModConfig._modPath, "GeneratedVanillaMappings-DO_NOT_TOUCH", fileName)) ?? throw new ArgumentNullException(fileName);
        
        if (!dictionary.TryGetValue(side, out var slotDict))
        {
            slotDict = new Dictionary<string, HashSet<MongoId>>();
            dictionary[side] = slotDict;
        }

        if (!slotDict.TryGetValue(slot, out var items))
        {
            items = new HashSet<MongoId>();
            slotDict[slot] = items;
        }

        foreach (var itemId in clothingData.Keys)
        {
            var item = databaseService.GetCustomization();
            if (!item.TryGetValue(itemId, out var _))
                continue;
            
            items.Add(itemId);
        }
    }

    /// <summary>
    ///     Return the proper slot types for specific weapon base classes
    /// </summary>
    private IReadOnlyList<ApbsEquipmentSlots> GetSlotsForPrimaryWeapon(MongoId itemId)
    {
        return itemHelper.IsOfBaseclasses(itemId, _shortRangeBaseClasses) ? _shortRangeSlots
            : itemHelper.IsOfBaseclasses(itemId, _longRangeBaseClasses) ? _longRangeSlots
            : [];
    }

    /// <summary>
    ///     Check if the item is banned from import
    ///     Check if the import is even enabled, and if it's importable
    ///     If neither the weapon nor equipment check pass, go ahead and return false so we skip this item for import
    ///     If either of them pass, go ahead and check if the vanilla dictionary contains that item, if it does then skip it
    ///     If all checks are completed, go ahead and mark the item for import
    /// </summary>
    public bool EquipmentNeedsImporting(MongoId itemId)
    {
        if (_bannedItems.Contains(itemId)) 
            return false;
        
        if (_vanillaEquipmentLookup.Contains(itemId))
        {
            var isVanillaWeapon = itemHelper.IsOfBaseclasses(itemId, _allImportableWeaponBaseClasses);
            if (isVanillaWeapon && !ModConfig.Config.CompatibilityConfig.EnableModdedAttachments)
                return false;

            return isVanillaWeapon;
        }

        var isWeapon = ModConfig.Config.CompatibilityConfig.EnableModdedWeapons &&
                        itemHelper.IsOfBaseclasses(itemId, _allImportableWeaponBaseClasses);

        var isEquipment = ModConfig.Config.CompatibilityConfig.EnableModdedEquipment &&
                           itemHelper.IsOfBaseclasses(itemId, _allImportableEquipmentBaseClasses);
        
        return isWeapon || isEquipment;
    }

    /// <summary>
    ///     Check if the item is banned from import
    ///     If it's a customization and also enabled return that
    /// </summary>
    public bool CustomizationNeedsImporting(CustomizationItem templateItem)
    {
        if (_bannedItems.Contains(templateItem.Id)) 
            return false;

        var isCustomization = ModConfig.Config.CompatibilityConfig.EnableModdedClothing && 
                              !ModConfig.Config.PmcBots.AdditionalOptions.SeasonalPmcAppearance &&
                              !_vanillaClothingLookup.Contains(templateItem.Id);

        if (!isCustomization) 
            return false;
        
        if (templateItem.Properties?.Side is null)
            return false;
        
        if (!templateItem.Properties.Side.Contains("Bear") &&
            !templateItem.Properties.Side.Contains("Usec")) 
            return false;
            
        return templateItem.Properties.BodyPart is "Body" or "Feet";
    }

    /// <summary>
    ///     Check if the weapon is vanilla
    /// </summary>
    public bool WeaponOrEquipmentIsVanilla(MongoId itemId)
    {
        return _vanillaEquipmentLookup.Contains(itemId);
    }

    /// <summary>
    ///     Check if the item is banned from import
    ///     Check if the item is ammo
    ///     The Caliber9x18PM caliber is also used for grenade shrapnel, so lets check those and skip if they are shrapnel
    ///     Go ahead and check specific calibers to skip, these are usually used for mounted weapons on the map. We don't spawn these
    ///     If all of these pass, go ahead and check if the vanilla dictionary contains the item, if it does then skip it
    ///     If all checks are completed, go ahead and mark the item for import
    /// </summary>
    public bool AmmoNeedsImporting(MongoId itemId, string caliber)
    {
        if (_bannedItems.Contains(itemId)) 
            return false;
        if (!itemHelper.IsOfBaseclass(itemId, BaseClasses.AMMO)) 
            return false;
        if (caliber == string.Empty) 
            return false;
        
        // Specifically check grenade shrapnel which also happens to always have 9x18PM as the caliber
        if (caliber == "Caliber9x18PM" && itemHelper.GetItemName(itemId).Contains("shrapnel")) 
            return false;
        // Skip these calibers as they are for things we don't spawn on bots (or we already have like the disks)
        if (caliber == "Caliber127x108" || caliber == "Caliber30x29" || caliber == "Caliber26x75" || caliber == "Caliber20x1mm") 
            return false;
        
        if (_vanillaAmmoLookup.Contains(itemId)) return false;

        return true;
    }

    /// <summary>
    ///     Check if the item is banned from import
    ///     Check if the item is ammo
    ///     The Caliber9x18PM caliber is also used for grenade shrapnel, so lets check those and skip if they are shrapnel
    ///     Go ahead and check specific calibers to skip, these are usually used for mounted weapons on the map. We don't spawn these
    ///     If all of these pass, go ahead and check if the vanilla dictionary contains the item, if it does then skip it
    ///     If all checks are completed, go ahead and mark the item for import
    /// </summary>
    public bool AmmoCaliberNeedsAdded(string caliber)
    {
        return !_vanillaAmmoDictionary.ContainsKey(caliber);
    }
    
    /// <summary>
    ///     Get the cartridge ids from a weapon's magazine template that work with the weapon
    /// </summary>
    /// <param name="weaponTemplate">Weapon db template to get magazine cartridges for</param>
    /// <returns>Hashset of cartridge tpls</returns>
    /// <exception cref="ArgumentNullException">Thrown when weaponTemplate is null.</exception>
    public HashSet<MongoId> GetCompatibleCartridgesFromMagazineTemplate(TemplateItem weaponTemplate)
    {
        var magazineSlot = weaponTemplate.Properties?.Slots?.FirstOrDefault(slot => slot.Name == "mod_magazine");
        if (magazineSlot is null)
        {
            return [];
        }

        var magazineTemplate = itemHelper.GetItem(magazineSlot.Properties?.Filters?.FirstOrDefault()?.Filter?.FirstOrDefault() ?? new MongoId(null));
        if (magazineTemplate.Value?.Properties == null) return new HashSet<MongoId>();


        var cartridges =
            magazineTemplate.Value.Properties.Slots.FirstOrDefault()?.Properties?.Filters?.FirstOrDefault()?.Filter
            ?? magazineTemplate.Value.Properties.Cartridges.FirstOrDefault()?.Properties?.Filters?.FirstOrDefault()?.Filter;

        return cartridges ?? [];
    }

    /// <summary>
    ///     Hydrate the holster cache
    ///     This cache contains all the weapons that can go in the holster, including the Pistol baseclass
    ///     We check it this way for Holster, specifically because there are some revolvers and some shotguns that go here and not in primary classes
    /// </summary>
    private void EnsureHolsterCacheExistsFirst()
    {
        lock (_holsterLock)
        {
            if (_holsterCacheInitialized) return;

            var defaultInventory = itemHelper.GetItem(ItemTpl.INVENTORY_DEFAULT).Value;

            _holsterAllowedItems = defaultInventory?
                .Properties?.Slots?.FirstOrDefault(x => x.Name == "Holster")?
                .Properties?.Filters?.FirstOrDefault(f => f.Filter?.Count > 0)?
                .Filter is { } filter
                ? [..filter]
                : null;

            _holsterCacheInitialized = true;
        }
    }
    
    /// <summary>
    ///     Is the item supposed to go in the Holster slot?
    /// </summary>
    public bool IsHolster(MongoId itemId)
    {
        EnsureHolsterCacheExistsFirst();
        
        return _holsterAllowedItems?.Contains(itemId) == true || itemHelper.IsOfBaseclasses(itemId, _holsterBaseClasses);
    }

    /// <summary>
    ///     Is the item supposed to go in the Primary slot?
    /// </summary>
    public bool IsPrimaryWeapon(MongoId itemId)
    {
        return itemHelper.IsOfBaseclasses(itemId, _shortRangeBaseClasses) || itemHelper.IsOfBaseclasses(itemId, _longRangeBaseClasses);
    }

    /// <summary>
    ///     Is the item supposed to go in the PrimaryLongRange slot?
    /// </summary>
    public bool IsLongRangePrimaryWeapon(MongoId itemId)
    {
        return itemHelper.IsOfBaseclasses(itemId, _longRangeBaseClasses);
    }

    /// <summary>
    ///     Is the item supposed to go in the Backpack slot?
    /// </summary>
    public bool IsBackpack(MongoId itemId)
    {
        return itemHelper.IsOfBaseclass(itemId, BaseClasses.BACKPACK);
    }

    /// <summary>
    ///     Is the item supposed to go in the Face cover slot?
    /// </summary>
    public bool IsFacecover(MongoId itemId)
    {
        return itemHelper.IsOfBaseclass(itemId, BaseClasses.FACE_COVER);
    }
    
    /// <summary>
    ///     Is the item supposed to go in the Tactical Rig slot?
    /// </summary>
    public bool IsRigSlot(MongoId itemId)
    {
        return itemHelper.IsOfBaseclass(itemId, BaseClasses.VEST);
    }
    
    /// <summary>
    ///     Is the item supposed to go in the ArmouredRig Data slot?
    /// </summary>
    public bool IsArmouredRig(TemplateItem? itemDetails)
    {
        // Armoured Rigs have Slots, Tactical Rigs do not
        return itemDetails?.Properties?.Slots != null && itemDetails.Properties.Slots.Any();
    }

    /// <summary>
    ///     Is the item supposed to go in the ArmourVest slot?
    /// </summary>
    public bool IsArmourVest(MongoId itemId)
    {
        return itemHelper.IsOfBaseclass(itemId, BaseClasses.ARMOR);
    }

    /// <summary>
    ///     Is the item supposed to go in the Armband slot?
    /// </summary>
    public bool IsArmband(MongoId itemId)
    {
        return itemHelper.IsOfBaseclass(itemId, BaseClasses.ARM_BAND);
    }

    /// <summary>
    ///     Is the item supposed to go in the Headphones slot?
    /// </summary>
    public bool IsHeadphones(MongoId itemId)
    {
        return itemHelper.IsOfBaseclass(itemId, BaseClasses.HEADPHONES);
    }

    /// <summary>
    ///     Is the item supposed to go in the Headwear slot?
    /// </summary>
    public bool IsHeadwear(MongoId itemId)
    {
        return itemHelper.IsOfBaseclass(itemId, BaseClasses.HEADWEAR);
    }

    /// <summary>
    ///     Is the item supposed to go in the Headwear slot and is armoured?
    /// </summary>
    public bool IsArmouredHelmet(TemplateItem itemDetails)
    {
        // Armoured helmets have slots, shit helmets do not
        return itemDetails.Properties?.Slots != null && itemDetails.Properties.Slots.Any();
    }

    /// <summary>
    ///     Is the item supposed to go in the Knife slot?
    /// </summary>
    public bool IsScabbard(MongoId itemId)
    {
        return itemHelper.IsOfBaseclass(itemId, BaseClasses.KNIFE);
    }

    /// <summary>
    ///     Is the item supposed to go in the Eyeglasses slot?
    /// </summary>
    public bool IsEyeglasses(MongoId itemId)
    {
        return itemHelper.IsOfBaseclass(itemId, BaseClasses.VISORS);
    }

    /// <summary>
    ///     Is the item a pack n strap belt?
    /// </summary>
    public bool IsPackNStrapBelt(MongoId itemId)
    {
        return itemHelper.IsOfBaseclass(itemId, BaseClassPackNStrapBelt);
    }

    /// <summary>
    ///     Marks the pack n strap belts as unlootable
    ///     Controlled by the mod config
    /// </summary>
    public void MarkPackNStrapUnlootable(TemplateItem itemDetails)
    {
        if (itemDetails.Properties is null) return;
        
        var unlootableFromSide = new List<PlayerSideMask> { PlayerSideMask.Bear, PlayerSideMask.Usec, PlayerSideMask.Savage };
        itemDetails.Properties.Unlootable = true;
        itemDetails.Properties.UnlootableFromSide = unlootableFromSide;
        itemDetails.Properties.UnlootableFromSlot = "ArmBand";
    }
    
    /// <summary>
    ///     Check if the item is a primary, if it is, send it to a different helper method
    ///     If it's not a primary weapon, go ahead and return the proper weights for each bot type
    /// </summary>
    public int GetWeaponSlotWeight(ApbsEquipmentSlots slot, string botType)
    {
        if (IsPrimary(slot))
        {
            return GetPrimaryWeaponWeight(botType);
        }
        
        return botType switch
        {
            "pmc" => slot switch
            {
                ApbsEquipmentSlots.Holster  => 5,
                ApbsEquipmentSlots.Scabbard => 6,
                _ => 1
            },

            "scav" => slot switch
            {
                ApbsEquipmentSlots.Holster  => 1,
                ApbsEquipmentSlots.Scabbard => 3,
                _ => 1
            },

            _ => slot switch
            {
                ApbsEquipmentSlots.Holster  => 3,
                ApbsEquipmentSlots.Scabbard => 3,
                _ => 1
            }
        };
    }

    /// <summary>
    ///     Is the slot being checked a primary weapon slot?
    /// </summary>
    private bool IsPrimary(ApbsEquipmentSlots slot)
    {
        return slot is ApbsEquipmentSlots.FirstPrimaryWeapon_LongRange
            or ApbsEquipmentSlots.FirstPrimaryWeapon_ShortRange
            or ApbsEquipmentSlots.SecondPrimaryWeapon_LongRange
            or ApbsEquipmentSlots.SecondPrimaryWeapon_ShortRange;
    }
    
    /// <summary>
    ///     Returns the Mod Config values for the imported weapon weights
    ///     This is a shortcut helper method for GetWeaponSlotWeight so we don't need additional logic there
    /// </summary>
    private int GetPrimaryWeaponWeight(string botType)
    {
        return botType switch
        {
            "pmc" => ModConfig.Config.CompatibilityConfig.PmcWeaponWeights,
            "scav" => ModConfig.Config.CompatibilityConfig.ScavWeaponWeights,
            "default" => ModConfig.Config.CompatibilityConfig.FollowerWeaponWeights,
            _ => 1
        };
    }
    
    private readonly FrozenSet<MongoId> _armourSlots = [BaseClasses.HEADWEAR, BaseClasses.VEST, BaseClasses.ARMOR];
    /// <summary>
    ///     Check if the item has an armour class or armour plate slots
    ///     If it isn't armoured, bail out and don't skip it
    ///     If it is armoured, check if it should be skipped by armour class
    /// </summary>
    public bool IfArmouredHelmetAndShouldSkip(TemplateItem templateItem, int currentTier)
    {
        if (currentTier >= 4) return false;
        if (!itemHelper.IsOfBaseclass(templateItem.Id, BaseClasses.HEADWEAR)) return false;
        
        foreach (var slot in templateItem.Properties?.Slots ?? [])
        {
            foreach (var filter in slot.Properties?.Filters ?? [])
            {
                foreach (var itemId in filter.Filter ?? [])
                {
                    var childArmorClass = itemHelper.GetItem(itemId).Value?.Properties?.ArmorClass ?? 0;
                    if (childArmorClass >= 4) return true;
                }
            }
        }

        return false;
    }
    
    /// <summary>
    ///     Checks various item properties, such as grid length, slot length, etc.
    ///     If the gear is for a scav, go ahead and return 1 to make it less likely
    ///     If the gear isn't for a scav, go ahead and get and return the correct weight for the slot based on the aforementioned properties if needed
    /// </summary>
    public int GetGearSlotWeight(ApbsEquipmentSlots slot, TemplateItem templateItem, bool isScav = false)
    {
        if (isScav) return 1;
        
        var gridLength = templateItem.Properties?.Grids?.Count() ?? 0;
        var equipmentSlotsLength = templateItem.Properties?.Slots?.Count() ?? 0;
        var armorClass = templateItem.Properties?.ArmorClass ?? 0;
        return slot switch
        {
            ApbsEquipmentSlots.ArmBand => 3,
            ApbsEquipmentSlots.ArmorVest => 10,
            ApbsEquipmentSlots.ArmouredRig => 7,
            ApbsEquipmentSlots.Backpack => 5,
            ApbsEquipmentSlots.Eyewear => 1,
            ApbsEquipmentSlots.Earpiece => 5,
            ApbsEquipmentSlots.FaceCover when armorClass > 2 => 1,
            ApbsEquipmentSlots.FaceCover when armorClass > 0 => 2,
            ApbsEquipmentSlots.FaceCover => 4,
            ApbsEquipmentSlots.Headwear when equipmentSlotsLength > 0 => 6,
            ApbsEquipmentSlots.Headwear => 1,
            ApbsEquipmentSlots.TacticalVest when gridLength > 10 => 10,
            ApbsEquipmentSlots.TacticalVest => 1,
            _ => 15
        };
    }

    public bool AreHeadphonesMountable(TemplateItem headphoneTemplateItem)
    {
        return headphoneTemplateItem.Properties?.BlocksEarpiece != null && (bool)headphoneTemplateItem.Properties?.BlocksEarpiece.Value;
    }

    public bool AttachmentNeedsImporting(TemplateItem parentItem, TemplateItem itemToAdd, string slot)
    {
        if (_bannedAttachments.Contains(itemToAdd.Id)) 
            return false;
        
        if (ModConfig.Config.CompatibilityConfig.EnableSafeGuard)
        {
            if ((IsVanillaAttachment(parentItem.Id) || WeaponOrEquipmentIsVanilla(parentItem.Id)) 
                && IsVanillaAttachment(itemToAdd.Id))
                return false;
        }

        if (VssValCheck(parentItem, slot))
            return false;

        if (Ar15Mod1Check(parentItem, slot))
            return false;

        if (MagazineWithNoCount(itemToAdd, slot))
            return false;

        if (IsFrontOrRearSightAndVanillaItem(parentItem, slot))
            return false;

        if (IsBannedModScope000(itemToAdd, slot))
            return false;

        if (IsFrontOrRearSightAndDoesntFold(itemToAdd, slot))
            return false;
        
        return true;
    }

    public bool IsVanillaAttachment(MongoId itemId)
    {
        return _vanillaAttachmentLookup.Contains(itemId);
    }

    private bool VssValCheck(TemplateItem parentItem, string slot)
    {
        return (parentItem.Id == ItemTpl.MARKSMANRIFLE_VSS_VINTOREZ_9X39_SPECIAL_SNIPER_RIFLE ||
                parentItem.Id == ItemTpl.ASSAULTCARBINE_AS_VAL_9X39_SPECIAL_ASSAULT_RIFLE) 
               && slot == "mod_mount_000";
    }
    
    private bool Ar15Mod1Check(TemplateItem parentItem, string slot)
    {
        return parentItem.Id == ItemTpl.HANDGUARD_AR15_AB_ARMS_MOD1 && slot == "mod_scope";
    }

    private bool MagazineWithNoCount(TemplateItem itemToAdd, string slot)
    {
        return slot == "mod_magazine" && !(itemToAdd.Properties?.Cartridges?.FirstOrDefault()?.MaxCount.HasValue ?? false);
    }

    private bool IsFrontOrRearSightAndVanillaItem(TemplateItem parentItem, string slot)
    {
        return slot.StartsWith("mod_sight_") && _vanillaAttachmentLookup.Contains(parentItem.Id);
    }

    private bool IsBannedModScope000(TemplateItem itemToAdd, string slot)
    {
        return slot == "mod_scope_000" && !_modScope000Whitelist.Contains(itemToAdd.Id);
    }

    private bool IsFrontOrRearSightAndDoesntFold(TemplateItem itemToAdd, string slot)
    {
        return slot.StartsWith("mod_sight_") && !_foldingModSights.Contains(itemToAdd.Id);
    }

    public bool AttachmentShouldBeInTier(TemplateItem parentItem, TemplateItem itemToAdd, string slot, int tier)
    {
        var isHighTier = tier >= 4;
        var hasDualOptions = HasLowerAndUpperOptionsAvailable(parentItem, slot, slot.StartsWith("mod_magazine") ? 30 : 8);

        // MAGAZINES
        if (slot.StartsWith("mod_magazine"))
        {
            if (!hasDualOptions)
                return true;

            var maxCount = itemToAdd.Properties?.Cartridges?.FirstOrDefault()?.MaxCount;
            if (!maxCount.HasValue)
                return true;
            
            return isHighTier ? maxCount.Value >= 30 : maxCount.Value <= 30;
        }

        // STOCKS / HANDGUARDS / RECEIVERS / PISTOLGRIP
        if (slot.StartsWith("mod_stock") || slot.StartsWith("mod_handguard") || slot.StartsWith("mod_reciever") || slot.StartsWith("mod_pistol_grip"))
        {
            if (!hasDualOptions)
                return true;

            var ergo = itemToAdd.Properties?.Ergonomics;
            if (!ergo.HasValue)
                return true;

            return isHighTier ? ergo.Value >= 8 : ergo.Value <= 8;
        }

        // SCOPES
        if (slot.StartsWith("mod_scope"))
        {
            return isHighTier && _tier4Optics.Contains(itemToAdd.Id);
        }

        return true;
    }



    private bool HasLowerAndUpperOptionsAvailable(TemplateItem parentItem, string slot, int thresholdValue)
    {
        var slotData = parentItem.Properties?.Slots?.FirstOrDefault(x => x.Name == slot);
        if (slotData == null) return false;
        
        var itemFilters = slotData.Properties?.Filters?.FirstOrDefault()?.Filter;
        var hasUpperOptions = false;
        var hasLowerOptions = false;

        foreach (var item in itemFilters ?? [])
        {
            var itemData = itemHelper.GetItem(item).Value;
            if (itemData == null || _bannedAttachments.Contains(itemData.Id))
                continue;

            var value = slot switch
            {
                "mod_magazine" => (int?)itemData.Properties?.Cartridges?.FirstOrDefault()?.MaxCount,
                
                "mod_handguard" or "mod_reciever" or "mod_stock"
                    or "mod_stock_000" or "mod_stock_001" or "mod_stock_002"
                    or "mod_stock_akms" or "mod_stock_axis"
                    => (int?)itemData.Properties?.Ergonomics,
                _ => null
            };

            if (!value.HasValue) continue;

            if (value >= thresholdValue) hasUpperOptions = true;
            if (value <= thresholdValue) hasLowerOptions = true;

            if (hasUpperOptions && hasLowerOptions)
                return true;
        }
            
        return false;
    }
    
    /// <summary>
    ///     Check if the item is banned from import
    /// </summary>
    public bool IsBlacklistedViaModConfig(MongoId itemId, int tier)
    {
        return _fullModConfigBlacklist[tier].Contains(itemId);
    }

    public bool IsWttBossWeapon(MongoId itemId)
    {
        return ModConfig.Config.CompatibilityConfig.WttArmouryAddBossVariantsToBosses && _armouryBossVariantWeapons.Contains(itemId);
    }

    public string[] BossAssignmentPerWtt(MongoId itemId)
    {
        return _bossWeaponLookup.TryGetValue(itemId, out var bossList) ? bossList : [];
    }
}