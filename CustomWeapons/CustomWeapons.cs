using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using MenuManager;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using System.Runtime.InteropServices;
using System.Collections.Concurrent;
using Serilog.Core;


namespace CustomWeapons;
public partial class CustomWeapons : BasePlugin, IPluginConfig<CustomWeaponsConfig>
{
    internal static CustomWeapons Instance { get; private set; } = new();

    public CustomWeaponsConfig Config { get; set; } = new();

    public override string ModuleName => "CustomWeapons";
    public override string ModuleDescription => "Custom weapons plugin";
    public override string ModuleVersion => "0.0.1a";
    public override string ModuleAuthor => "9avg";

    public override void Load(bool hotReload)
    {
        Instance = this;
    }



    public void OnConfigParsed(CustomWeaponsConfig config)
    {
        Config = config;

        if (config.DatabaseHost.Length < 1 || config.DatabaseName.Length < 1 || config.DatabaseUser.Length < 1)
        {
            Logger.LogError("You need to setup Database credentials in \"configs/plugins/CustomWeapons/CustomWeapons.json\"!");
            Unload(false);
            return;
        }


        var builder = new MySqlConnectionStringBuilder
        {
            Server = config.DatabaseHost,
            UserID = config.DatabaseUser,
            Password = config.DatabasePassword,
            Database = config.DatabaseName,
            Port = (uint)config.DatabasePort,
            Pooling = true,
            MaximumPoolSize = 640,
        };

        Database = new Database(builder.ConnectionString);

        _ = Utility.CheckDatabaseTables();
        Utility.CreateCustomWeaponsJsonFile("../../csgo/addons/counterstrikesharp/configs/plugins/CustomWeapons/CustomWeaponsModels.json", Logger);
        Utility.LoadCustomWeaponsFromFile("../../csgo/addons/counterstrikesharp/configs/plugins/CustomWeapons/CustomWeaponsModels.json", Logger);
        _localizer = Localizer;

        Utility.Config = config;
        Utility.ShowAd(ModuleVersion);
    }

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        try
        {
            MenuApi = MenuCapability.Get();

            if (MenuApi == null) Logger.LogInformation("MenuManager Core not found...");

            //SetupWeaponMenu();

            //RegisterCommands();
        }
        catch (Exception)
        {
            MenuApi = null;
            Logger.LogError("Error while loading required plugins");
            throw;
        }
    }

    [ConsoleCommand("set_model", "Set weapon model")]
    public void SetModel(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
        {
            Console.WriteLine("Command has been called by the server.");
            return;
        }

        CCSPlayerPawn? pawn = player.PlayerPawn.Value;
        if (pawn == null) return;

        var activeWeapon = pawn.WeaponServices?.ActiveWeapon.Value;

        if (activeWeapon == null) return;


        var viewModel = Utility.GetPlayerViewModel(player);
        if (viewModel == null || viewModel.Weapon.Value == null ||
            viewModel.Weapon.Value.Index != activeWeapon.Index) return;


        activeWeapon.SetModel("weapons/kxnrl/ak47/firefly/ak47_w.vmdl");
        viewModel.SetModel("weapons/kxnrl/ak47/firefly/ak47_v.vmdl");


        Console.WriteLine("Custom command called.");
        Console.WriteLine($"Player type: {player.GetType()}");
        Console.WriteLine($"Player type: {activeWeapon.GetType()}");
        Console.WriteLine($"Viewmodel type: {viewModel.GetType()}");
        //player.DropActiveWeapon();

    }

    //[GameEventHandler]
    //public HookResult onSwitchItem(EventItemEquip @event, GameEventInfo info)
    //{
    //    var activeWeapon = @event.Userid?.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;
    //    if (activeWeapon == null) return HookResult.Continue;


    //    var modelName = activeWeapon.CBodyComponent?.SceneNode?.GetSkeletonInstance().ModelState.ModelName;

    //    var viewModel = Utility.GetPlayerViewModel(@event.Userid);

    //    Console.WriteLine($"modelName: {modelName}");

    //    if (modelName != "weapons/kxnrl/ak47/firefly/ak47_w.vmdl" && viewModel?.Weapon.Value?.Entity?.DesignerName == "weapon_ak47") activeWeapon.SetModel("weapons/kxnrl/ak47/firefly/ak47_w.vmdl");

    //    if (viewModel == null || viewModel.Weapon.Value == null || viewModel?.Weapon.Value?.Entity?.DesignerName != "weapon_ak47") return HookResult.Continue;


    //    viewModel.SetModel("weapons/kxnrl/ak47/firefly/ak47_v.vmdl");

    //    return HookResult.Continue;
    //}

    [GameEventHandler]
    public HookResult onSwitchItem(EventItemEquip @event, GameEventInfo info)
    {
        var player = @event.Userid;
        PlayerInfo = PlayerInfo.GetPlayerInfo(player.SteamID, Logger);

        if (!Utility.IsPlayerValid(@event.Userid) || PlayerInfo == null)
            return HookResult.Continue;

        var activeWeapon = player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;
        if (activeWeapon == null) return HookResult.Continue;

        string weaponName = activeWeapon.DesignerName;
        uint weaponIndex = activeWeapon.Index;

        // Проверяем, есть ли выбранный скин для данного оружия у игрока или есть ли оружие в регистре
        if (!PlayerInfo.SelectedWeapons.TryGetValue(weaponName, out string? skinKey) && !WeaponSkinRegistry.TryGetValue(weaponIndex, out skinKey)) return HookResult.Continue;


        Utility.SetPlayerWeaponModel(player, activeWeapon, skinKey, Logger);

        if (!WeaponSkinRegistry.ContainsKey(weaponIndex))
        {
            WeaponSkinRegistry[weaponIndex] = skinKey;
        }

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult onPlayerConnected(EventPlayerConnect @event, GameEventInfo info)
    {
        var player = @event.Userid;
        Logger.LogInformation($"SteamID64: {@event.Xuid}");
        if (@event.Xuid == 0)
        {
            return HookResult.Continue;
        }

        var playerInfo = new PlayerInfo(@event.Xuid);

        // Загружаем доступные скины
        playerInfo.SetAccessibleWeapons(Utility.GetPlayerAccessibleSkins(@event.Xuid, Logger, Database));

        // Загружаем выбранные оружия
        playerInfo.SetSelectedWeapons(Utility.GetPlayerSelectedWeapons(@event.Xuid, Logger, Database));

        PlayerInfo.AddPlayer(@event.Xuid, playerInfo);

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult onPlayerDisconnected(EventPlayerDisconnect @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (@event.Xuid == 0)
        {
            return HookResult.Continue;
        }

        PlayerInfo.RemovePlayer(@event.Xuid);

        return HookResult.Continue;
    }

    [ConsoleCommand("test_cfg", "Test cfg")]
    public void TestCfg(CCSPlayerController? player, CommandInfo command)
    {
        Utility.PrintCustomWeapons(Logger);
    }

    [ConsoleCommand("print_pi", "Test cfg")]
    public void PrintPlayerInfo(CCSPlayerController? player, CommandInfo command)
    {
        Utility.PrintPlayerInfo(player.SteamID, Logger);
    }

    [ConsoleCommand("cw", "Показывает меню оружия с доступными скинами")]
    public void ChangeCustomWeapon(CCSPlayerController? player, CommandInfo command)
    {
        if(!Utility.IsPlayerValid(player)) { return; }

        // Проверка наличия данных о оружии
        if (root == null || root.CustomWeapons == null || root.CustomWeapons.Count == 0)
        {
            Logger.LogError("Данные CustomWeapons отсутствуют или пусты.");
            return;
        }

        var playerinfo = PlayerInfo.GetPlayerInfo(player.SteamID, Logger);
        Logger.LogInformation($"{playerinfo.SteamId64}");
        var accessibleSkins = playerinfo.AccessibleWeapons;
        if (accessibleSkins.Count == 0)
        {
            Logger.LogInformation("У игрока нет доступа ни к одному скину.");
            return;
        }

        // Создаем главное меню для выбора оружия
        var weaponMenu = Utility.CreateMenu("Выберите оружие");
        if (weaponMenu == null)
        {
            Logger.LogError("Не удалось создать меню оружия.");
            return;
        }

        // Словарь для сопоставления отображаемого имени оружия и его ключа
        var categoryMapping = new Dictionary<string, string>();

        // Обходим все категории оружия
        foreach (var categoryEntry in root.CustomWeapons)
        {
            string weaponKey = categoryEntry.Key; // Например, "weapon_knife"
                                                  // Фильтруем скины категории по доступу
            var skinsForCategory = categoryEntry.Value.Skins
                .Where(skinEntry => accessibleSkins.Contains(skinEntry.Key))
                .ToDictionary(s => s.Key, s => s.Value);

            // Если в категории нет ни одного доступного скина — пропускаем
            if (skinsForCategory.Count == 0)
                continue;

            string? weaponDisplayName = categoryEntry.Value.name ?? weaponKey;
            categoryMapping[weaponDisplayName] = weaponKey;

            // Добавляем опцию в главное меню
            weaponMenu.AddMenuOption(weaponDisplayName, (CCSPlayerController player, ChatMenuOption option) =>
            {
                if (option == null || option.Text == null)
                {
                    Logger.LogError("Неверная опция меню.");
                    return;
                }

                // Получаем ключ выбранной категории
                if (!categoryMapping.TryGetValue(option.Text, out string? selectedWeaponKey))
                {
                    Logger.LogError($"Не найдено соответствие для выбранного оружия: {option.Text}");
                    return;
                }

                if (!root.CustomWeapons.TryGetValue(selectedWeaponKey, out WeaponCategory? selectedCategory))
                {
                    Logger.LogError($"Не найдена категория для ключа: {selectedWeaponKey}");
                    return;
                }

                // Создаем подменю для выбора модели
                var skinMenu = Utility.CreateMenu($"Выберите модель для {selectedCategory.name ?? selectedWeaponKey}");
                if (skinMenu == null)
                {
                    Logger.LogError("Не удалось создать подменю для выбора модели.");
                    return;
                }

                var skinMapping = new Dictionary<string, string>();

                if (selectedCategory.Skins == null || selectedCategory.Skins.Count == 0)
                {
                    Logger.LogInformation("В этой категории нет доступных моделей.");
                    return;
                }

                // Фильтруем скины выбранной категории по доступу
                var accessibleSkinsInCategory = selectedCategory.Skins
                    .Where(skinEntry => accessibleSkins.Contains(skinEntry.Key))
                    .ToDictionary(s => s.Key, s => s.Value);

                // Обходим скины выбранной категории
                foreach (var skinEntry in accessibleSkinsInCategory)
                {
                    string skinKey = skinEntry.Key; // Например, "v_octane"
                    string skinDisplayName = skinEntry.Value.name ?? skinKey;
                    skinMapping[skinDisplayName] = skinKey;

                    // Добавляем опцию для выбора модели
                    skinMenu.AddMenuOption(skinDisplayName, (CCSPlayerController player2, ChatMenuOption skinOption) =>
                    {
                        if (skinOption == null || skinOption.Text == null)
                        {
                            Logger.LogError("Неверная опция подменю.");
                            return;
                        }

                        if (!skinMapping.TryGetValue(skinOption.Text, out string? selectedSkinKey))
                        {
                            Logger.LogError($"Не найдено соответствие для выбранной модели: {skinOption.Text}");
                            return;
                        }

                        var activeWeapon = player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;
                        var activeWeaponName = Utility.NormalizeWeaponName(activeWeapon.DesignerName);

                        // Выводим выбранные ключи оружия и модели через логгер
                        Logger.LogInformation($"Вы выбрали оружие: {selectedWeaponKey} и модель: {selectedSkinKey}");

                        Utility.UpdateLocalPlayerSelectedWeapon(player.SteamID, selectedWeaponKey, selectedSkinKey, Logger);
                        Utility.UpdatePlayerSelectedWeaponInDatabase(player.SteamID, selectedWeaponKey, selectedSkinKey, Logger, Database);


                        if (activeWeaponName == selectedWeaponKey)
                        {
                            Utility.SetPlayerWeaponModel(player, activeWeapon, selectedSkinKey, Logger);
                        }
                    });
                }

                // Добавляем кнопку "Назад" в подменю
                skinMenu.AddMenuOption("←Назад", (CCSPlayerController player2, ChatMenuOption backOption) =>
                {
                    // Возвращаемся к главному меню оружия
                    weaponMenu.Open(player2);
                });

                // Открываем подменю для выбора модели
                skinMenu.Open(player);
            });
        }

        // Открываем главное меню оружия для игрока
        weaponMenu.Open(player);
    }


    [ConsoleCommand("print_cw", "Test cfg")]
    public void PrintCustomWeapons(CCSPlayerController? player, CommandInfo command)
    {
        Utility.PrintCustomWeapons(Logger);
    }

    [ConsoleCommand("print_path", "Test cfg")]
    public void PrintCurrentPath(CCSPlayerController? player, CommandInfo command)
    {
        Logger.LogInformation(Directory.GetCurrentDirectory());
    }

    [ConsoleCommand("cw_give_test", "Test cfg")]
    [CommandHelper(minArgs: 3, usage: "[SteamID64] [modelName] [Duration]", whoCanExecute: CommandUsage.SERVER_ONLY)]
    public void GiveCustomWeapon(CCSPlayerController? player, CommandInfo command)
    {
        if (!SteamID.TryParse(command.GetArg(1), out SteamID steamId) || !steamId.IsValid())
        {
            Logger.LogError("Указан неверный SteamID!");
            return;
        }
        ulong steamid64 = ulong.Parse(command.GetArg(1));
        var modelName = command.GetArg(2);
        long duration = long.Parse(command.GetArg(3));

        if (PlayerInfo.ContainsPlayerInfo(steamid64))
        {
            Utility.UpdateLocalPlayerAccessibleSkin(steamid64, modelName, Logger);
        }

        Utility.UpdatePlayerAccessibleSkinInDatabase(steamid64, modelName, duration, Logger, Database);
    }
}

