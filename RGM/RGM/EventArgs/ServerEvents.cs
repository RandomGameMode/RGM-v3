using DiscordInteraction.Discord;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Server;
using MapGeneration.Holidays;
using MEC;

using PlayerRoles;
using ProjectMER.Features;
using RGM.API.Components;
using RGM.API.Features;
using RGM.Modes.Sets.AddScp.Scps;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using RGM.Patches;
using UnityEngine;
using Random = UnityEngine.Random;
using static RGM.IEnumerators.LobbyIEnumerator;
using static RGM.IEnumerators.ServerIEnumerator;
using static RGM.Variables.Variable;

namespace RGM.EventArgs
{
    public static class ServerEvents
    {
        public static IEnumerator<float> OnWaitingForPlayers()
        {
            ServerManager.Setup();

            yield return Timing.WaitForSeconds(1f);

            Server.Name = Server.Name.Replace("{version}", $"v{Main.Instance.Version.Major}.{Main.Instance.Version.Minor}.{Main.Instance.Version.Build}");

            try
            {
                var folderPath = $"{Paths.AppData}/SCP Secret Laboratory/LocalAdminLogs/{Server.Port}/";
                var files = new DirectoryInfo(folderPath)
                            .GetFiles()
                            .OrderByDescending(f => f.CreationTime)
                            .ToList();

                if (files.Count >= 2)
                {
                    FileInfo secondOldest = files[1];
                    string content = File.ReadAllText(secondOldest.FullName);

                    Webhook.Send($"LocalAdminLogs : {Server.Port}", Tools.ReadTextFile(Path.Combine(Paths.Configs, "RGM"), "Webhook2.txt"), $"{secondOldest.FullName}");
                }
            }
            catch (Exception e)
            {
                Log.Error($"Error sending LocalAdminLogs webhook: {e}");
            }

            UsersManager.LoadUsers();

            Tools.PlayGlobalAudio("First Blood (Cover)", 1.25f, true);

            Round.IsLobbyLocked = true;
            GameObject.Find("StartRound").transform.localScale = Vector3.zero;
            Tools.LoadMap($"RGMLobby");

            var donator = new Donator.Main();
            donator.OnEnabled();

            First = Tools.GetObjectList("First");
            Second = Tools.GetObjectList("Second");
            Third = Tools.GetObjectList("Third");
            Fourth = Tools.GetObjectList("Fourth");
            Numbers = Tools.GetObjectList("Number");
            RandomColors = Tools.GetObjectList("RandomColor");
            RandomLights = Tools.GetObjectList("RandomLight");
            Balls = Tools.GetObjectList("Ball");

            yield return Timing.WaitForSeconds(1);

            Tools.PickModes();
            Balls.ForEach(x => x.gameObject.AddComponent<BallComponent>());

            Timing.RunCoroutine(ThrowawayBroadcast());
            Timing.RunCoroutine(GameStartButton());
            Timing.RunCoroutine(ModeResetButton());
            Timing.RunCoroutine(IsFallDown());
            Timing.RunCoroutine(Ball());
            Timing.RunCoroutine(MovingShootingTarget());

            var rand = Random.Range(1, 6);

            switch (rand)
            {
                case 1:
                    SelectMode = "RandomSelect";
                    break;
                case 2:
                    SelectMode = "SimpleSelect";
                    break;
                case 3:
                    SelectMode = "SecretVote";
                    break;
                case 4:
                    SelectMode = "FightVote";
                    Server.FriendlyFire = true;
                    break;
                default:
                    SelectMode = "MostVote";
                    break;
            }

            while (true)
            {
                TranslationManager.EnsureFileCacheLoaded();
                UsersManager.LoadUsers();

                yield return Timing.WaitForSeconds(1f);
            }
        }

        public static IEnumerator<float> OnRoundStarted()
        {
            Server.FriendlyFire = false;

            MapUtils.UnloadMap("RGMLobby");
            First.Clear();
            Second.Clear();
            Third.Clear();
            Fourth.Clear();
            Numbers.Clear();
            RandomColors.Clear();
            RandomLights.Clear();
            Balls.Clear();

            Server.ExecuteCommand($"/speak {string.Join(".", PlayerManager.List.Select(x => x.Id))}. 0");
            IntercomPlayers.Clear();
            EnabledModeList.Clear();

            if (AudioPlayer.TryGet("Global AudioPlayer", out AudioPlayer ap))
                ap.RemoveAllClips();

            if (CurrentMode == ModeType.None)
            {
                try
                {
                    var maxLength = ModeVote.Values.Max(list => list.Count);
                    var longestKeys = ModeVote.Keys.Where(key => ModeVote[key].Count == maxLength).ToList();
                    var randomKey = longestKeys[Random.Range(0, longestKeys.Count)];
                    CurrentMode = randomKey;
                    CurrentSubMode = SubModeVote[ModeVote.Keys.ToList().IndexOf(randomKey)];

                    if (SelectMode == "SimpleSelect")
                    {
                        List<Player> mergedPlayers = ModeVote.Values.SelectMany(x => x).ToList();
                        List<Player> filiteredPlayers = mergedPlayers.Where(mergedPlayers.Contains).ToList();

                        if (filiteredPlayers.Count > 0)
                        {
                            Player player = filiteredPlayers.GetRandomValue();
                            CurrentMode = ModeVote.FirstOrDefault(x => x.Value.Contains(player)).Key;
                            CurrentSubMode = SubModeVote[ModeVote.Keys.ToList().IndexOf(CurrentMode)];

                            Timing.CallDelayed(1f, () =>
                            {
                                foreach (var p in PlayerManager.List)
                                    p.AddBroadcast(10, $"<size=25><b>롤토체스 당첨자(<b><i>{player.DisplayNickname}</i></b>)</b>에 의해 모드가 {CurrentMode.GetModeData().Name}(으)로 선택되었습니다.</size>");
                            });
                        }
                    }
                }
                finally
                {
                    if (!ModeList.ContainsKey(CurrentMode))
                    {
                        CurrentMode = ModeList.Keys.Where(x => ModeList[x].Category == ModeCategory.Public).ToList().GetRandomValue();

                        foreach (var p in PlayerManager.List)
                            p.AddBroadcast(10, $"<size=25><b>알 수 없는 이유로 모드가 선택되지 않았으므로, 모드가 랜덤으로 선택되었습니다.</size>");
                    }
                }
            }

            Server.Name = Server.Name.Replace("[라운드 시작 전 로비]", $"[현재 모드: <color=#{CurrentMode.GetModeData().Color}>{CurrentMode.GetModeData().Name}</color>{(CurrentSubMode == ModeType.None ? "" : $"<size=25> + <color=#{CurrentSubMode.GetModeData().Color}>{CurrentSubMode.GetModeData().Name}</color></size>")}]");

            List<string> ModeDesc = Tools.GetModeDesc(CurrentMode, CurrentSubMode);

            foreach (var player in PlayerManager.List)
            {
                player.AddBroadcast(10, ModeDesc[0]);

                player.SendConsoleMessage($"\n{ModeDesc[0].Replace("\n", "\n")}", "white");
                player.SendConsoleMessage(ModeDesc[3] == "" ? $"\n{ModeDesc[2]}" : $"\n{ModeDesc[3]}", "white");
            }

            Tools.TryInstallMode(CurrentMode);

            if (CurrentSubMode != ModeType.None)
                Tools.TryInstallMode(CurrentSubMode);

            if (StartupRandom == 3 && CurrentMode != ModeType.Juggernaut)
                Tools.CallSnakeHand(null, PlayerManager.List.Where(x => x.Role == RoleTypeId.FacilityGuard).ToList());

            Timing.RunCoroutine(Detonation());

            Webhook.Send($"시작된 모드 : {CurrentMode.GetModeData().Name}");
            Log.Info($"시작된 모드 : {CurrentMode.GetModeData().Name}");

            if (CurrentMode == ModeType.Develop) yield break;
            if (CurrentMode.GetModeData().Info == ModeInfo.Plus)
            {
                Timing.RunCoroutine(HumanLoop());
                Timing.RunCoroutine(Scp079Broadcast());

                if (Convert.ToByte(Random.Range(1, 101)) <= 10)
                {
                    foreach (var special in Specials.Where(_ => Convert.ToByte(Random.Range(1, 101)) <= 25)) 
                    {
                        Tools.LoadMap(special);
                    }

                    if (Convert.ToByte(Random.Range(1, 101)) <= 35)
                        Scp294.OnEnabled();

                    if (Convert.ToByte(Random.Range(1, 101)) <= 35)
                        Scp1162.OnEnabled();
                }
            }

            yield return Timing.WaitUntilDone(new AutoWarhead().RunCoroutine());
        }

        public static IEnumerator<float> OnRoundEnded(RoundEndedEventArgs ev)
        {
            if (HolidayUtils.IsHolidayActive(HolidayType.Halloween))
            {
                if (Convert.ToByte(Random.Range(1, 101)) <= 20)
                {
                    List<EffectType> effects =
                    [
                        EffectType.Metal,
                        EffectType.Lightweight,
                        EffectType.MovementBoost,
                        EffectType.SugarRush
                    ];

                    foreach (var player in PlayerManager.List)
                    {
                        foreach (var effect in effects)
                        {
                            player.AddEffect(effect, 255);
                        }
                    }
                }
                else
                {
                    List<EffectType> effects =
                    [
                        EffectType.Spicy,
                        EffectType.OrangeCandy,
                        EffectType.SugarCrave,
                        EffectType.SugarHigh,
                        EffectType.SugarRush,
                        EffectType.Ghostly,
                        EffectType.Prismatic,
                        EffectType.WhiteCandy,
                        EffectType.Metal
                    ];
                    var effect = effects.GetRandomValue();

                    foreach (var player in PlayerManager.List)
                    {
                        player.AddEffect(effect, 255);
                    }
                }
            }

            if (CurrentMode.GetModeData().Info == ModeInfo.Plus)
            {
                List<Player> players = [.. PlayerManager.List.Where(x => x.IsAlive && !x.IsNPC)];

                switch (players.Count)
                {
                    case 1:
                        Timing.RunCoroutine(Tools.SetWinner([.. players], 5));
                        break;
                    case > 1:
                        Timing.RunCoroutine(Tools.SetWinner([.. players], 1));
                        break;
                }
            }

            Tools.TryInstallMode(ModeType.FriendlyFire);

            foreach (var player in Player.List)
            {
                Server.ExecuteCommand($"/speak {player.Id} 1");
                IntercomPlayers.Add(player);
            }

            foreach (var player in Player.List)
            {
                List<string> uc = UsersManager.UsersCache[player.UserId];

                if (uc[22] == "0") continue;
                if (Convert.ToByte(Random.Range(1, 21)) != 1) continue;
                uc[22] = "0";
                UsersManager.UsersCache[player.UserId] = uc;
                UsersManager.SaveUsers();

                player.AddHint($"경고 해제", "부여된 경고가 해제되었습니다. 행운을 빕니다.", 20);
            }

            try
            {
                string path = $"{Paths.Configs}/RGM/Users.db";

                if (File.Exists(path) && new FileInfo(path).Length == 0)
                {
                    string tmpPath = path + ".tmp";
                    if (File.Exists(tmpPath))
                    {
                        File.Delete(path);
                        File.Move(tmpPath, path);
                    }
                }

                Webhook.Send($"# {Server.IpAddress}:{Server.Port}", Tools.ReadTextFile(Path.Combine(Paths.Configs, "RGM"), "Webhook4.txt"), path);
            }
            catch (Exception e)
            {
                Log.Error($"Error sending webhook: {e}");
            }

            while (Round.IsEnded)
            {
                var top10 = PlayersReport
                .OrderByDescending(kv => kv.Value.Damage)
                .Take(10)
                .ToList();

                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"<size=30><b>이번 라운드 TOP 10</b></size>");
                byte rank = 1;

                foreach (var kv in top10)
                {
                    try
                    {
                        var userId = kv.Key;
                        var report = kv.Value;

                        Player player = null;
                        bool found = false;
                        try
                        {
                            found = Player.TryGet(userId, out player);
                        }
                        catch (Exception e)
                        {
                            Log.Error($"Player.TryGet 예외: {e}");
                        }

                        if (found && player != null)
                        {
                            sb.AppendLine($"<size=25><color=#{ranking(rank)}>{rank}.</color> {Tools.BadgeFormat(player)}<color={player.Role.Color.ToHex()}><b><i>{player.DisplayNickname}</i></b></color> - {report.Kill} kill / {report.Death} death / {report.Damage} damage</size>");
                        }
                        else
                        {
                            sb.AppendLine($"<size=25><color=#{ranking(rank)}>{rank}.</color> <color=#888888>null</color> - {report.Kill} kill / {report.Death} death / {report.Damage} damage</size>");
                        }
                        rank++;
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Error in generating round summary: {e}");
                    }
                }

                foreach (var player in Player.List)
                {
                    try
                    {
                        var report = PlayersReport[player.UserId];
                        string text = $"<align=left><size=20><b><i>{player.DisplayNickname}</i></b> - {report.Kill} kill / {report.Death} death / {report.Damage} damage</size></align>\n<align=left>{sb}</align>\n\n\n\n";
                        string result = $"{WinMessage}\n\n{text}";

                        player.ShowHint(result, 1.2f);
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Error in AddHint: {e}");
                    }
                }

                yield return Timing.WaitForSeconds(1);
                continue;

                string ranking(byte r)
                {
                    switch (r)
                    {
                        case 1:
                            return "fffa66";
                        case 2:
                            return "808d8e";
                        case 3:
                            return "dfae4d";
                        default:
                            return "ffffff";
                    }
                }
            }
        }

        public static void OnRespawnedTeam(RespawnedTeamEventArgs ev)
        {
            if (HolidayUtils.IsHolidayActive(HolidayType.Christmas) && Convert.ToByte(Random.Range(1, 101)) <= 10)
            {
                Exiled.API.Features.Cassie.Clear();
                Exiled.API.Features.Cassie.MessageTranslated("$pitch_0.10 .G6", "");

                foreach (var player in ev.Players)
                {
                    player.Role.Set(ev.Wave.TargetFaction == Faction.FoundationStaff ? RoleTypeId.NtfFlamingo : RoleTypeId.ChaosFlamingo, RoleSpawnFlags.AssignInventory);
                }
            }

            if (CurrentMode != ModeType.Juggernaut && Convert.ToByte(Random.Range(1, 101)) <= 5)
            {
                CallTutorialSupport(ev.Players);
            }
        }

        public static void CallTutorialSupport(IEnumerable<Player> players)
        {
            Exiled.API.Features.Cassie.Clear();
            Exiled.API.Features.Cassie.MessageTranslated("$pitch_0.10 .G7", "");

            Tools.CallSnakeHand(null, players.ToList());
        }
    }
}
