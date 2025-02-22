using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using CustomWeapons;
using MenuManager;
using CounterStrikeSharp.API.Modules.Menu;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using Microsoft.Extensions.Logging;


namespace CustomWeapons;

public partial class CustomWeapons
{
    public static IStringLocalizer? _localizer;
    internal static Database? Database;

    internal static IMenuApi? MenuApi;
    internal static readonly PluginCapability<IMenuApi> MenuCapability = new("menu:nfcore");

    public string[] accessWeapons = new string[65];
    public static CustomWeaponsRoot? root;
    public static PlayerInfo? PlayerInfo;

    private static Dictionary<uint, string> WeaponSkinRegistry = new Dictionary<uint, string>();
    public static HashSet<string> knifeNames { get; } = new()
        {
        "weapon_bayonet",
        "weapon_knife_css",
        "weapon_knife_flip",
        "weapon_knife_gut",
        "weapon_knife_karambit",
        "weapon_knife_m9_bayonet",
        "weapon_knife_tactical",
        "weapon_knife_falchion",
        "weapon_knife_survival_bowie",
        "weapon_knife_butterfly",
        "weapon_knife_push",
        "weapon_knife_cord",
        "weapon_knife_canis",
        "weapon_knife_ursus",
        "weapon_knife_gypsy_jackknife",
        "weapon_knife_outdoor",
        "weapon_knife_stiletto",
        "weapon_knife_widowmaker",
        "weapon_knife_skeleton",
        "weapon_knife_kukri"
        };
}


