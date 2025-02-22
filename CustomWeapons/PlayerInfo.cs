using Microsoft.Extensions.Logging;
using Serilog.Core;
using System.Diagnostics.Eventing.Reader;

namespace CustomWeapons
{
    public class PlayerInfo
    {
        public ulong SteamId64 { get; }
        public Dictionary<string, string> SelectedWeapons { get; private set; }
        public HashSet<string> AccessibleWeapons { get; private set; }

        // Хранение всех игроков
        private static readonly Dictionary<ulong, PlayerInfo> _players = new Dictionary<ulong, PlayerInfo>();

        public PlayerInfo(ulong steamId64)
        {
            SteamId64 = steamId64;
            SelectedWeapons = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            AccessibleWeapons = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        // Установка доступных скинов
        public void SetAccessibleWeapons(HashSet<string> accessibleWeapons)
        {
            AccessibleWeapons = accessibleWeapons;
        }

        // Установка выбранного оружия
        public void SetSelectedWeapons(Dictionary<string, string> selectedWeapons)
        {
            SelectedWeapons = selectedWeapons;
        }

        // Получение информации о игроке
        public static PlayerInfo GetPlayerInfo(ulong steamId64, ILogger logger)
        {
            if (_players.TryGetValue(steamId64, out var playerInfo))
            {
                logger.LogInformation($"Найден playerInfo для {playerInfo.SteamId64}");
                return playerInfo;
            }

            // Если игрока нет в списке — создаем его
            var newPlayer = new PlayerInfo(steamId64);
            _players[steamId64] = newPlayer;
            logger.LogInformation($"Создан новый playerInfo");
            return newPlayer;
        }

        public static bool ContainsPlayerInfo(ulong steamId64)
        {
            return _players.ContainsKey(steamId64);
        }

        // Добавляем игрока
        public static void AddPlayer(ulong steamId64, PlayerInfo playerinfo)
        {
            if (!_players.TryGetValue(steamId64, out var playerInfo))
            {
                _players[steamId64] = playerinfo;
            }
        }

        // Удаление игрока при выходе
        public static void RemovePlayer(ulong steamId64)
        {
            _players.Remove(steamId64);
        }
    }
}

