using System.Collections.Concurrent;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using CustomWeapons;
using Newtonsoft.Json.Linq;

namespace WeaponPaints;

public partial class WeaponPaints
{
    //private void RegisterCommands()
    //{
    //    _config.Additional.CommandSkin.ForEach(c =>
    //    {
    //        AddCommand($"css_{c}", "Skins info", (player, info) =>
    //        {
    //            if (!Utility.IsPlayerValid(player)) return;
    //            OnCommandWS(player, info);
    //        });
    //    });

    //    _config.Additional.CommandRefresh.ForEach(c =>
    //    {
    //        AddCommand($"css_{c}", "Skins refresh", (player, info) =>
    //        {
    //            if (!Utility.IsPlayerValid(player)) return;
    //            OnCommandRefresh(player, info);
    //        });
    //    });

    //    if (Config.Additional.CommandKillEnabled)
    //    {
    //        _config.Additional.CommandKill.ForEach(c =>
    //        {
    //            AddCommand($"css_{c}", "kill yourself", (player, _) =>
    //            {
    //                if (player == null || !Utility.IsPlayerValid(player) || player.PlayerPawn.Value == null || !player.PlayerPawn.IsValid) return;

    //                player.PlayerPawn.Value.CommitSuicide(true, false);
    //            });
    //        });
    //    }
    //}

    //private void SetupKnifeMenu()
    //{
    //    var knivesOnly = 

    //    var giveItemMenu = Utility.CreateMenu(Localizer["wp_knife_menu_title"]);

    //    var handleGive = (CCSPlayerController player, ChatMenuOption option) =>
    //    {
    //        if (!Utility.IsPlayerValid(player)) return;

    //        var playerKnives = GPlayersKnife.GetOrAdd(player.Slot, new ConcurrentDictionary<CsTeam, string>());
    //        var teamsToCheck = player.TeamNum < 2
    //            ? new[] { CsTeam.Terrorist, CsTeam.CounterTerrorist }
    //            : [player.Team];

    //        var knifeName = option.Text;
    //        var knifeKey = knivesOnly.FirstOrDefault(x => x.Value == knifeName).Key;
    //        if (string.IsNullOrEmpty(knifeKey)) return;
    //        if (!string.IsNullOrEmpty(Localizer["wp_knife_menu_select"]))
    //        {
    //            player.Print(Localizer["wp_knife_menu_select", knifeName]);
    //        }

    //        if (!string.IsNullOrEmpty(Localizer["wp_knife_menu_kill"]) && Config.Additional.CommandKillEnabled)
    //        {
    //            player.Print(Localizer["wp_knife_menu_kill"]);
    //        }

    //        PlayerInfo playerInfo = new PlayerInfo
    //        {
    //            UserId = player.UserId,
    //            Slot = player.Slot,
    //            Index = (int)player.Index,
    //            SteamId = player.SteamID.ToString(),
    //            Name = player.PlayerName,
    //            IpAddress = player.IpAddress?.Split(":")[0]
    //        };

    //        foreach (var team in teamsToCheck)
    //        {
    //            // Attempt to get the existing knives
    //            playerKnives[team] = knifeKey;
    //        }

    //        if (_gBCommandsAllowed && (LifeState_t)player.LifeState == LifeState_t.LIFE_ALIVE)
    //            RefreshWeapons(player);

    //        if (WeaponSync != null)
    //            _ = Task.Run(async () => await WeaponSync.SyncKnifeToDatabase(playerInfo, knifeKey, teamsToCheck));
    //    };
    //    foreach (var knifePair in knivesOnly)
    //    {
    //        giveItemMenu?.AddMenuOption(knifePair.Value, handleGive);
    //    }
    //    _config.Additional.CommandKnife.ForEach(c =>
    //    {
    //        AddCommand($"css_{c}", "Knife Menu", (player, _) =>
    //        {
    //            if (giveItemMenu == null) return;
    //            if (!Utility.IsPlayerValid(player) || !_gBCommandsAllowed) return;

    //            if (player == null || player.UserId == null) return;

    //            if (!CommandsCooldown.TryGetValue(player.Slot, out var cooldownEndTime) ||
    //                DateTime.UtcNow >= (CommandsCooldown.TryGetValue(player.Slot, out cooldownEndTime) ? cooldownEndTime : DateTime.UtcNow))
    //            {
    //                CommandsCooldown[player.Slot] = DateTime.UtcNow.AddSeconds(Config.CmdRefreshCooldownSeconds);
    //                giveItemMenu.PostSelectAction = PostSelectAction.Close;

    //                giveItemMenu.Open(player);

    //                return;
    //            }
    //            if (!string.IsNullOrEmpty(Localizer["wp_command_cooldown"]))
    //            {
    //                player.Print(Localizer["wp_command_cooldown"]);
    //            }
    //        });
    //    });
    //}
}