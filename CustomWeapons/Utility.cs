using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using Dapper;
using MenuManager;

//using MenuManager;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog.Core;
using System.Data;
using System.Numerics;
using System.Runtime.InteropServices;
using static CustomWeapons.CustomWeapons;

namespace CustomWeapons
{
    internal static class Utility
    {
        internal static CustomWeaponsConfig? Config { get; set; }


        internal static async Task CheckDatabaseTables()
        {
            if (CustomWeapons.Database is null) return;

            try
            {
                await using var connection = await CustomWeapons.Database.GetConnectionAsync();
                await using var transaction = await connection.BeginTransactionAsync();

                try
                {
                    string[] createTableQueries =
                    [
                        @"
                        CREATE TABLE IF NOT EXISTS `cweapon_access` (
                            `steamid64` VARCHAR(64) NOT NULL,
                            `model`     VARCHAR(64) NOT NULL,
                            `expires`   DATETIME NOT NULL,
                            UNIQUE (`steamid64`, `model`)
                        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;",

                        @"
                        CREATE TABLE IF NOT EXISTS `cw_player_weapons` (
                            `steamid64` VARCHAR(64) NOT NULL,
                            `weapon`    VARCHAR(64) NOT NULL,
                            `model`     VARCHAR(64) NOT NULL,
                            UNIQUE (`steamid64`, `weapon`)
                        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;"
                    ];

                    foreach (var query in createTableQueries)
                    {
                        await connection.ExecuteAsync(query, transaction: transaction);
                    }

                    await transaction.CommitAsync();
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw new Exception("[CustomWeapons] Unable to create tables!");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("[CustomWeapons] Unknown MySQL exception! " + ex.Message);
            }
        }


        internal static bool IsPlayerValid(CCSPlayerController? player)
        {
            if (player is null) return false;

            return player is { IsValid: true, IsBot: false, IsHLTV: false, UserId: not null };
        }

        internal static void CreateCustomWeaponsJsonFile(string filePath, ILogger logger)
        {
            // Формируем объект с нужной структурой

            if (File.Exists(filePath))
            {
                logger?.LogInformation($"Файл CustomWeaponsModels.json обнаружен успешно!");
                return;
            }

            var customWeaponsObject = new
            {
                CustomWeapons = new Dictionary<string, object>
    {
        {
            "weapon_knife", new
            {
                name = "CategoryName",
                // Название модели может быть любым (например, modelname)
                modelname = new
                {
                    name = "InGameName",
                    viewmodel = "",
                    worldmodel = "",
                    @private = "1",
                    private_prefix = "",
                    hide = "1"
                }
            }
        }
    }
            };

            // Сериализуем объект в форматированный JSON
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(customWeaponsObject, Formatting.Indented);

            try
            {
                // Записываем JSON в файл
                File.WriteAllText(filePath, json);
                logger?.LogError($"Файл CustomWeaponsModels.json успешно создан!");
            }
            catch (Exception ex)
            {
                logger?.LogError($"Не удалось создать файл CustomWeaponsModels.json: {ex.Message}");
            }
        }

        internal static void PrintCustomWeapons(ILogger logger)
        {
            // Обход категорий оружия
            foreach (KeyValuePair<string, WeaponCategory> categoryEntry in CustomWeapons.root.CustomWeapons)
            {
                string categoryKey = categoryEntry.Key; // Например, "weapon_knife"
                WeaponCategory category = categoryEntry.Value;
                logger.LogInformation($"Категория: {category.name} (ключ: {categoryKey})");

                if (category.Skins.Count == 0)
                {
                    logger.LogInformation("    Нет скинов для этой категории.");
                }
                else
                {
                    foreach (var skinEntry in category.Skins)
                    {
                        // skinEntry.Key — ключ скина (например, "v_octane")
                        // skinEntry.Value — объект скина
                        var skin = skinEntry.Value;
                        logger.LogInformation($"    Скин (ключ: {skinEntry.Key}): {skin.name}, viewmodel: {skin.viewmodel}, worldmodel: {skin.worldmodel}");
                    }
                }
            }
        }

        internal static void LoadCustomWeaponsFromFile(string filePath, ILogger logger)
        {
            if (!File.Exists(filePath))
            {
                logger?.LogError($"Файл не найден: {filePath}");
                return;
            }

            string json;
            try
            {
                json = File.ReadAllText(filePath);
                logger?.LogInformation($"Файл {filePath} успешно загружен!");
            }
            catch (Exception ex)
            {
                logger?.LogError($"Ошибка чтения файла {filePath}: {ex.Message}");
                return;
            }

            try
            {

                CustomWeapons.root = JsonConvert.DeserializeObject<CustomWeaponsRoot>(json);
                if (CustomWeapons.root == null || CustomWeapons.root.CustomWeapons == null)
                {
                    logger?.LogError("Десериализация не удалась: отсутствуют данные CustomWeapons.");
                    return;
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Ошибка десериализации JSON: {ex.Message}");
                return;
            }
        }

        internal static IMenu? CreateMenu(string title)
        {
            var menuType = CustomWeapons.Instance.Config.MenuType.ToLower();

            var menu = menuType switch
            {
                _ when menuType.Equals("selectable", StringComparison.CurrentCultureIgnoreCase) =>
                    CustomWeapons.MenuApi?.GetMenu(title),

                _ when menuType.Equals("dynamic", StringComparison.CurrentCultureIgnoreCase) =>
                    CustomWeapons.MenuApi?.GetMenuForcetype(title, MenuType.ButtonMenu),

                _ when menuType.Equals("center", StringComparison.CurrentCultureIgnoreCase) =>
                    CustomWeapons.MenuApi?.GetMenuForcetype(title, MenuType.CenterMenu),

                _ when menuType.Equals("chat", StringComparison.CurrentCultureIgnoreCase) =>
                    CustomWeapons.MenuApi?.GetMenuForcetype(title, MenuType.ChatMenu),

                _ when menuType.Equals("console", StringComparison.CurrentCultureIgnoreCase) =>
                    CustomWeapons.MenuApi?.GetMenuForcetype(title, MenuType.ConsoleMenu),

                _ => CustomWeapons.MenuApi?.GetMenu(title)
            };

            return menu;
        }

        internal static CBaseViewModel? GetPlayerViewModel(CCSPlayerController? player)
        {
            if (player?.PlayerPawn.Value == null || player.PlayerPawn.Value.ViewModelServices == null) return null;
            CCSPlayer_ViewModelServices viewModelServices = new(player.PlayerPawn.Value.ViewModelServices!.Handle);
            var ptr = viewModelServices.Handle + Schema.GetSchemaOffset("CCSPlayer_ViewModelServices", "m_hViewModel");
            var references = MemoryMarshal.CreateSpan(ref ptr, 3);
            var viewModel = (CHandle<CBaseViewModel>)Activator.CreateInstance(typeof(CHandle<CBaseViewModel>), references[0])!;
            return viewModel.Value == null ? null : viewModel.Value;
        }

        internal static void ShowAd(string moduleVersion)
        {
            Console.WriteLine(@" ");
            Console.WriteLine(@"   ______           __                _       __                                 ");
            Console.WriteLine(@"  / ____/_  _______/ /_____  ____ ___| |     / /__  ____ _____  ____  ____  _____");
            Console.WriteLine(@" / /   / / / / ___/ __/ __ \/ __ `__ \ | /| / / _ \/ __ `/ __ \/ __ \/ __ \/ ___/");
            Console.WriteLine(@"/ /___/ /_/ (__  ) /_/ /_/ / / / / / / |/ |/ /  __/ /_/ / /_/ / /_/ / / / (__  ) ");
            Console.WriteLine(@"\____/\__,_/____/\__/\____/_/ /_/ /_/|__/|__/\___/\__,_/ .___/\____/_/ /_/____/  ");
            Console.WriteLine(@"                                                      /_/                        ");
            Console.WriteLine(@"                               >> Version: " + moduleVersion);
            Console.WriteLine(@"			       >> Discord: 9avg");
            Console.WriteLine(@" ");
        }

        public static void SetupCustomWeaponMenu(CCSPlayerController? player, CustomWeaponsRoot? customWeapons, ILogger logger)
        {
            // Проверка валидности игрока
            if (player == null || !Utility.IsPlayerValid(player))
            {
                logger.LogError("Игрок не валиден.");
                return;
            }

            // Проверка наличия данных о оружии
            if (customWeapons == null || customWeapons.CustomWeapons == null || customWeapons.CustomWeapons.Count == 0)
            {
                logger.LogError("Данные CustomWeapons отсутствуют или пусты.");
                return;
            }

            // Создаем главное меню для выбора оружия
            var weaponMenu = Utility.CreateMenu("Выберите оружие");
            if (weaponMenu == null)
            {
                logger.LogError("Не удалось создать меню оружия.");
                return;
            }

            // Словарь для сопоставления отображаемого имени оружия и его ключа
            var categoryMapping = new Dictionary<string, string>();

            // Обходим все категории оружия
            foreach (var categoryEntry in customWeapons.CustomWeapons)
            {
                string weaponKey = categoryEntry.Key; // Например, "weapon_knife"
                string weaponDisplayName = categoryEntry.Value.name ?? weaponKey;
                categoryMapping[weaponDisplayName] = weaponKey;

                // Добавляем опцию в главное меню
                weaponMenu.AddMenuOption(weaponDisplayName, (CCSPlayerController p, ChatMenuOption option) =>
                {
                    if (option == null || option.Text == null)
                    {
                        logger.LogError("Неверная опция меню.");
                        return;
                    }

                    // Получаем ключ выбранной категории
                    if (!categoryMapping.TryGetValue(option.Text, out string? selectedWeaponKey))
                    {
                        logger.LogError($"Не найдено соответствие для выбранного оружия: {option.Text}");
                        return;
                    }

                    if (!customWeapons.CustomWeapons.TryGetValue(selectedWeaponKey, out WeaponCategory? selectedCategory))
                    {
                        logger.LogError($"Не найдена категория для ключа: {selectedWeaponKey}");
                        return;
                    }

                    // Создаем подменю для выбора модели
                    var skinMenu = Utility.CreateMenu($"Выберите модель для {selectedCategory.name ?? selectedWeaponKey}");
                    if (skinMenu == null)
                    {
                        logger.LogError("Не удалось создать подменю для выбора модели.");
                        return;
                    }

                    var skinMapping = new Dictionary<string, string>();

                    if (selectedCategory.Skins == null || selectedCategory.Skins.Count == 0)
                    {
                        logger.LogInformation("В этой категории нет доступных моделей.");
                        return;
                    }

                    // Обходим скины выбранной категории
                    foreach (var skinEntry in selectedCategory.Skins)
                    {
                        string skinKey = skinEntry.Key; // Например, "v_octane"
                        string skinDisplayName = skinEntry.Value.name ?? skinKey;
                        skinMapping[skinDisplayName] = skinKey;

                        // Добавляем опцию для выбора модели
                        skinMenu.AddMenuOption(skinDisplayName, (CCSPlayerController p2, ChatMenuOption skinOption) =>
                        {
                            if (skinOption == null || skinOption.Text == null)
                            {
                                logger.LogError("Неверная опция подменю.");
                                return;
                            }

                            if (!skinMapping.TryGetValue(skinOption.Text, out string? selectedSkinKey))
                            {
                                logger.LogError($"Не найдено соответствие для выбранной модели: {skinOption.Text}");
                                return;
                            }

                            // Выводим выбранные ключи оружия и модели через логгер
                            logger.LogInformation($"Вы выбрали оружие: {selectedWeaponKey} и модель: {selectedSkinKey}");
                        });
                    }

                    // Открываем подменю для выбора модели
                    skinMenu.Open(p);
                });
            }

            // Открываем главное меню оружия для игрока
            weaponMenu.Open(player);
        }

        internal static Dictionary<string, string> GetPlayerSelectedWeapons(ulong steamid64, ILogger logger, Database? database)
        {
            var selectedWeapons = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using var connection = database?.GetConnectionAsync().GetAwaiter().GetResult();
                if (connection == null)
                {
                    logger.LogError("Не удалось установить соединение с базой данных.");
                    return selectedWeapons;
                }

                string query = "SELECT weapon, model FROM cw_player_weapons WHERE steamid64 = @steamid64";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@steamid64", steamid64);

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    string weapon = reader.GetString("weapon");
                    string model = reader.GetString("model");

                    selectedWeapons[weapon] = model;
                    logger.LogDebug($"Выбранное оружие: {weapon}, Модель: {model}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Ошибка при запросе выбранных оружий: {ex.Message}");
            }
            return selectedWeapons;
        }

        internal static HashSet<string> GetPlayerAccessibleSkins(ulong steamid64, ILogger logger, Database? database)
        {
            var accessibleSkins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using var connection = database?.GetConnectionAsync().GetAwaiter().GetResult();
                if (connection == null)
                {
                    logger.LogError("Не удалось установить соединение с базой данных.");
                    return accessibleSkins;
                }

                string query = "SELECT model, expires FROM cweapon_access WHERE steamid64 = @steamid64";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@steamid64", steamid64);

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    string model = reader.GetString("model");
                    DateTime expires = reader.GetDateTime("expires");

                    if (expires == DateTime.MaxValue || expires > DateTime.UtcNow)
                    {
                        accessibleSkins.Add(model);
                        logger.LogDebug($"Доступный скин: {model}");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Ошибка при запросе доступных скинов: {ex.Message}");
            }
            return accessibleSkins;
        }

        internal static void PrintPlayerInfo(ulong steamid64, ILogger logger)
        {
            var player = PlayerInfo.GetPlayerInfo(steamid64, logger);
            logger.LogInformation($"=== Информация об игроке {steamid64} ===");

            // Вывод выбранных скинов
            logger.LogInformation("Выбранные скины:");
            if (player.SelectedWeapons.Count == 0)
            {
                logger.LogInformation("    У игрока нет выбранных скинов.");
            }
            else
            {
                foreach (var weapon in player.SelectedWeapons)
                {
                    logger.LogInformation($"    Оружие: {weapon.Key}, Модель: {weapon.Value}");
                }
            }

            // Вывод доступных скинов
            logger.LogInformation("Доступные скины:");
            if (player.AccessibleWeapons.Count == 0)
            {
                logger.LogInformation("    У игрока нет доступных скинов.");
            }
            else
            {
                foreach (var skin in player.AccessibleWeapons)
                {
                    logger.LogInformation($"    {skin}");
                }
            }

            logger.LogInformation("=====================================");
        }

        internal static void SetPlayerWeaponModel(CCSPlayerController? player, CBasePlayerWeapon? activeWeapon, string skinKey, ILogger Logger)
        {
            string? weaponName = NormalizeWeaponName(activeWeapon?.DesignerName);
            if (!CustomWeapons.root.CustomWeapons.TryGetValue(weaponName, out var weaponCategory) ||
                !weaponCategory.Skins.TryGetValue(skinKey, out var skin)) return;
            var viewModel = Utility.GetPlayerViewModel(player);
            string? currentWorldModel = activeWeapon?.CBodyComponent?.SceneNode?.GetSkeletonInstance().ModelState.ModelName;
            Logger.LogInformation($"Current world model: {currentWorldModel}");
            if (!string.IsNullOrEmpty(skin.worldmodel))
            {
                // Заменяем worldmodel, если она отличается
                if (currentWorldModel != skin.worldmodel)
                {
                    activeWeapon?.SetModel(skin.worldmodel);
                }
            }

            // Заменяем viewmodel, если она существует и игрок держит это оружие
            if (viewModel != null && viewModel.Weapon.Value?.Entity?.DesignerName == weaponName && !string.IsNullOrEmpty(skin.viewmodel))
            {
                viewModel.SetModel(skin.viewmodel);
            }
        }

        public static void UpdateLocalPlayerSelectedWeapon(ulong steamid64, string weaponName, string weaponSkinKey, ILogger logger)
        {
            // Получаем или создаем объект PlayerInfo для игрока
            var playerInfo = PlayerInfo.GetPlayerInfo(steamid64, logger);

            // Если оружие уже выбрано, обновляем его; иначе — добавляем новое
            if (playerInfo.SelectedWeapons.ContainsKey(weaponName))
            {
                playerInfo.SelectedWeapons[weaponName] = weaponSkinKey;
                logger.LogInformation($"Обновлено выбранное оружие для '{weaponName}': модель '{weaponSkinKey}'.");
            }
            else
            {
                playerInfo.SelectedWeapons.Add(weaponName, weaponSkinKey);
                logger.LogInformation($"Добавлено новое выбранное оружие '{weaponName}' с моделью '{weaponSkinKey}'.");
            }
        }

        public static void UpdatePlayerSelectedWeaponInDatabase(ulong steamid64, string weaponName, string weaponSkinKey, ILogger logger, Database? database)
        {
            try
            {
                using var connection = database?.GetConnectionAsync().GetAwaiter().GetResult();
                if (connection == null)
                {
                    logger.LogError("Не удалось установить соединение с базой данных.");
                    return;
                }

                string query = @"
                INSERT INTO cw_player_weapons (steamid64, weapon, model)
                VALUES (@steamid64, @weapon, @model)
                ON DUPLICATE KEY UPDATE model = @model";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@steamid64", steamid64);
                command.Parameters.AddWithValue("@weapon", weaponName);
                command.Parameters.AddWithValue("@model", weaponSkinKey);

                int affectedRows = command.ExecuteNonQuery();
                logger.LogInformation($"База данных обновлена. Затронуто строк: {affectedRows}");
            }
            catch (Exception ex)
            {
                logger.LogError($"Ошибка при обновлении выбранных оружий в базе данных: {ex.Message}");
            }
        }

        public static void UpdateLocalPlayerAccessibleSkin(ulong steamid64, string model, ILogger logger)
        {
            // Получаем или создаем информацию об игроке
            var playerInfo = PlayerInfo.GetPlayerInfo(steamid64, logger);

            // Если скин уже присутствует, просто логируем информацию, иначе добавляем его
            if (playerInfo.AccessibleWeapons.Contains(model))
            {
                logger.LogInformation($"У игрока {steamid64} уже есть доступ к скину: {model}");
            }
            else
            {
                playerInfo.AccessibleWeapons.Add(model);
                logger.LogInformation($"Добавлен доступ к скину: {model} для игрока {steamid64}");
            }
        }

        public static void UpdatePlayerAccessibleSkinInDatabase(ulong steamid64, string model, long durationSeconds, ILogger logger, Database? database)
        {
            try
            {
                using var connection = database?.GetConnectionAsync().GetAwaiter().GetResult();
                if (connection == null)
                {
                    logger.LogError("Не удалось установить соединение с базой данных.");
                    return;
                }

                DateTime expires;
                if (durationSeconds == 0)
                {
                    // Используем максимально допустимое значение для MySQL DATETIME
                    expires = new DateTime(9999, 12, 31, 23, 59, 59);
                }
                else
                {
                    expires = DateTime.UtcNow.AddSeconds(durationSeconds);
                    // Проверяем, чтобы значение не превышало допустимый диапазон
                    if (expires > new DateTime(9999, 12, 31, 23, 59, 59))
                    {
                        expires = new DateTime(9999, 12, 31, 23, 59, 59);
                    }
                }

                string query = @"
            INSERT INTO cweapon_access (steamid64, model, expires)
            VALUES (@steamid64, @model, @expires)
            ON DUPLICATE KEY UPDATE expires = @expires";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@steamid64", steamid64);
                command.Parameters.AddWithValue("@model", model);

                // Явно задаем тип параметра для даты
                var expiresParam = new MySqlParameter("@expires", MySqlDbType.DateTime)
                {
                    Value = expires
                };
                command.Parameters.Add(expiresParam);

                int affectedRows = command.ExecuteNonQuery();
                logger.LogInformation($"База данных cweapon_access обновлена. Затронуто строк: {affectedRows}");
            }
            catch (Exception ex)
            {
                logger.LogError($"Ошибка при обновлении cweapon_access в базе данных: {ex.Message}");
            }
        }


        public static string NormalizeWeaponName(string weaponName)
        {
            if (knifeNames.Contains(weaponName))
            {
                return "weapon_knife";
            }

            return weaponName;
        }
    }
}