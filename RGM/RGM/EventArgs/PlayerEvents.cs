using CustomPlayerEffects;
using DiscordInteraction.Discord;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Roles;
using Exiled.Events.EventArgs.Player;
using MapGeneration.Holidays;
using MEC;
using PlayerRoles;
using PlayerStatsSystem;
using ProjectMER.Features;
using RGM.API.DataBases;
using RGM.API.Features;
using RGM.API.Interfaces;
using RGM.Modes.Abilities.Synergy;
using RGM.Modes.SubClass;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;
using static RGM.IEnumerators.ServerIEnumerator;
using static RGM.Variables.Variable;


namespace RGM.EventArgs
{
    public static class PlayerEvents
    {
        public static IEnumerator<float> OnVerified(VerifiedEventArgs ev)
        {
            if (ev.Player == null)
                yield return Timing.WaitForOneFrame;

            if (ev.Player == null) yield break;

            ev.Player.Setup();

            /*
             * 현재 출석 체크 기능 작동 안 함. 점검 필요.
             */
            List<string> defaultValues = [.. Enumerable.Repeat("0", 35)];

            if (!UsersManager.UsersCache.ContainsKey(ev.Player.UserId))
            {
                UsersManager.AddUser(ev.Player.UserId, defaultValues);

                UsersManager.SaveUsers();
            }
            else
            {
                List<string> uc = UsersManager.UsersCache[ev.Player.UserId];

                if (uc[29] == "0") // 오늘 아직 출석 안 했다면
                {
                    var total = int.Parse(uc[27]);
                    var current = int.Parse(uc[30]);
                    var max = int.Parse(uc[28]);

                    total++;
                    current++;

                    bool isNewRecord = current > max;
                    if (isNewRecord)
                        max = current;

                    uc[29] = "1"; // 오늘 출석 처리
                    uc[27] = total.ToString(); // 누적 출석
                    uc[30] = current.ToString(); // 현재 연속
                    uc[28] = max.ToString(); // 최대 연속

                    PlayersAudio[ev.Player].TryPlay(isNewRecord ? "출석 체크 굿" : "출석 체크");

                    UsersManager.SaveUsers();

                    ev.Player.AddBroadcast(
                        20,
                        $"<size=25><b>출석 체크 완료!</b> 오늘도 즐거운 랜덤게임모드(RGM) 되세요!</size>\n" +
                        $"<size=20>" +
                        (isNewRecord
                            ? $"우와 신기록이네요! {current}일 연속으로 잊지 않고 놀러와주셔서 감사합니다."
                            : $"총 {total}회 출석하셨고, 현재 {current}일 연속 출석 중입니다. 최고 기록은 {max}일입니다.")
                        + "</size>"
                    );
                }

                try
                {
                    UsersManager.UsersCache[ev.Player.UserId][12] = ev.Player.DisplayNickname;
                    UsersManager.UsersCache[ev.Player.UserId][14] = $"{Tools.GenerateRandomString(6)}";
                    UsersManager.SaveUsers();

                    ev.Player.Group = null;
                    ev.Player.RankName = null;
                    ev.Player.BadgeHidden = false;

                    Timing.CallDelayed(1, () =>
                    {
                        if (uc[17] == "1" && uc[10] != "0")
                            uc[11] = uc[10].Split('/').GetRandomValue();

                        ev.Player.RankName = $"{(uc[25] != "0" ? $"{uc[25]} " : "")}{(uc[11] != "0" ? uc[11] : "")}";

                        Tools.RemovePaint(ev.Player);

                        if (uc[16] == "1" && uc[8] != "0")
                            uc[9] = uc[8].Split('/').GetRandomValue();

                        Tools.ChangePaint(ev.Player, uc[9]);
                    });
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }

                if (uc.Count < defaultValues.Count)
                {
                    int diff = defaultValues.Count - uc.Count;

                    for (int i = 0; i < diff; i++)
                        uc.Add("0");

                    UsersManager.SaveUsers();
                }

                if (uc[5] != "0")
                    ev.Player.DisplayNickname = uc[5];

                if (uc[6] != "0")
                    ev.Player.CustomInfo = uc[6];

                if (uc[22] != "0")
                {
                    string value = $"""
                                    <b><color=red>경고! 제재당할 위기입니다. 아래 내용을 확인해보세요.</color></b>
                                    {string.Join("\n", uc[22].Split('/'))}
                                    """;
                    ev.Player.AddHint($"경고", $"{value}", 100);
                    ev.Player.SendConsoleMessage($"\n{value}", "white");
                }
            }

            ev.Player.AddBroadcast(10, Notions.WelcomeMessage);

            // ---------------------------------------------------------------------------------------

            if (Random.Range(1, 101) == 1)
                CapybaraPet.Create(ev.Player);

            // ---------------------------------------------------------------------------------------

            if (Round.IsStarted)
            {
                string Name = ModeList[CurrentMode].Name;
                string Color = ModeList[CurrentMode].Color;
                string Description = ModeList[CurrentMode].Description;
                string FileName = ModeList[CurrentMode].ToString();
                string Detail = ModeList[CurrentMode].Detail;

                string Message = Notions.LateJoinModeDescription
                    .Replace("{ModeColor}", Color)
                    .Replace("{CurrentMode}", Name)
                    .Replace("{CurrentSubMode}",
                        CurrentSubMode != ModeType.None
                            ? $"<size=20>추가된 서브 모드 : <color=#{ModeList[CurrentSubMode].Color}>{CurrentSubMode.GetModeData().Name}</color></size>\n"
                            : "")
                    .Replace("{ModeDescription}", Description)
                    .Replace("{ModeInfo}", CurrentMode.GetModeData().Info.ToString());

                ev.Player.AddBroadcast(10, Message);

                ev.Player.SendConsoleMessage($"\n{Message.Replace("\n", "\n")}", "white");
                ev.Player.SendConsoleMessage(Detail == "" ? $"\n해당 모드에 대한 자세한 설명이 없습니다." : $"\n{Detail}", "white");
            }
            else
            {
                Server.ExecuteCommand($"/speak {ev.Player.Id} 1");
                IntercomPlayers.Add(ev.Player);

                yield return Timing.WaitForOneFrame;

                Tools.TeleportToLobby(ev.Player);

                ItemType itemType = ItemType.Coin;

                if (HolidayUtils.IsHolidayActive(HolidayType.Christmas))
                    itemType = ItemType.Snowball;

                else if (HolidayUtils.IsHolidayActive(HolidayType.Halloween))
                    ev.Player.AddItem(ItemType.Lantern);

                ev.Player.AddItem(itemType);
                ev.Player.EnableEffect(EffectType.MovementBoost, 50);
                ev.Player.EnableEffect(EffectType.Lightweight, 100);

                if (SelectMode.Contains("Secret"))
                    ev.Player.EnableEffect(EffectType.Invisible);

                ModeType iv(int num)
                {
                    return ModeVote.Keys.ToList()[num - 1];
                }

                while (!Round.IsStarted && ev.Player.IsConnected)
                {
                    try
                    {
                        if (Physics.Raycast(ev.Player.Position, Vector3.down, out RaycastHit hit, 5f, (LayerMask)1))
                        {
                            if (hit.transform.name == "Credit")
                            {
                                ev.Player.AddHint("로비",
                                    """
                                    <size=50><b>[ ⭐ 랜덤게임모드(RGM) 크레딧 ⭐ ]</b></size>

                                    <align=left><size=30>
                                    <b><size=35><color=#F7FE2E>관리진</color></size></b>
                                    @alvar_noah - 서버 소유자
                                    @normal._.person - 총 관리자 (베테랑)
                                    정규 관리자 - @bluefox2322, @wanjeon_chobo

                                    <b><size=35><color=#C8FE2E>개발진</color></size></b>
                                    @mercedes83 - 총괄 개발자
                                    @cocoa_1.19 - 서브 개발자

                                    <b><size=35><color=#F79F81>후원자</color></size></b>
                                    <size=20>@dotory001, @milkyway_0119, @1__neeko__1, @yeeeee222, @tampast, @decoding_, @hs_bini, @solminb27, @LESI_2010, @handsome_dobby</size>

                                    <b><size=35><color=#F8E0F7>도움 주신 분들</color></size><b>
                                    <size=20>@cocoa_1.19, @leejihyuk, @mujishungplay, @changwonfirebird</size>
                                    </size></align>
                                    \n\n\n\n\n\n\n\n\n
                                    """, 1.2f);
                            }
                            else if (hit.transform.name == "Mode")
                            {
                                List<string> Modes = [];

                                foreach (var mode in ModeList)
                                {
                                    ModeData modeData = mode.Key.GetModeData();

                                    string Name = modeData.Name;
                                    string Color = modeData.Color;
                                    ModeCategory flag = modeData.Category;

                                    switch (flag)
                                    {
                                        case ModeCategory.Private:
                                            Modes.Add($"<s><color=#{Color}>{Name}</color></s>");
                                            break;
                                        case ModeCategory.OnlySub:
                                            Modes.Add($"<mark=#FFFF000D><color=#{Color}>{Name}</color></s></mark>");
                                            break;
                                        default:
                                            Modes.Add($"<color=#{Color}>{Name}</color>");
                                            break;
                                    }
                                }

                                ev.Player.AddHint("로비",
                                    $"\n\n\n\n\n\n<size=40><b>[ ⭐ 랜덤게임모드(RGM) 모드 목록 ⭐ ]</b></size>\n\n<size=25>{string.Join(", ", Modes)}</size>");
                            }
                            else if (hit.transform.name == "ExpLeaderBoard")
                            {
                                List<string> queue = [];

                                string c(int num)
                                {
                                    switch (num)
                                    {
                                        case 1:
                                            return $"<color=#ffd700>#{num}</color>";

                                        case 2:
                                            return $"<color=#c0c0c0>#{num}</color>";

                                        case 3:
                                            return $"<color=#cd7f32>#{num}</color>";

                                        default:
                                            return $"#{num}";
                                    }
                                }

                                foreach (var user in UsersManager.UsersCache
                                             .OrderByDescending(x => int.TryParse(x.Value[0], out var exp) ? exp : 0)
                                             .Take(10))
                                {
                                    try
                                    {
                                        string Name = user.Value[12];
                                        string Exp = user.Value[0];

                                        queue.Add(
                                            $"{c(queue.Count() + 1)} - <i>{Name}</i>(<color=yellow>{Exp}</color>)");
                                    }
                                    catch (Exception e)
                                    {
                                        Log.Error(e);
                                    }
                                }

                                ev.Player.AddHint("로비",
                                    $"<align=left><size=30><b>[ ⭐ 랜덤게임모드(RGM) EXP 순위표 ⭐ ]</b></size>\n\n<size=25>{string.Join("\n", queue)}</size></align>\n\n\n\n\n");
                            }
                            else
                            {
                                ModeType SelectedMode = ModeType.None;
                                string Color;
                                string Description;

                                for (int i = 0; i < 4; i++)
                                {
                                    if (ModeVote.ContainsKey(ModeVote.Keys.ToList()[i]) &&
                                        ModeVote[ModeVote.Keys.ToList()[i]].Contains(ev.Player))
                                        ModeVote[ModeVote.Keys.ToList()[i]].Remove(ev.Player);
                                }

                                if (new List<string>() { "First", "Second", "Third", "Fourth" }.Contains(hit.collider
                                        .name))
                                {
                                    switch (hit.collider.name)
                                    {
                                        case "First":
                                            SelectedMode = ModeVote.Keys.ToList()[0];
                                            break;
                                        case "Second":
                                            SelectedMode = ModeVote.Keys.ToList()[1];
                                            break;
                                        case "Third":
                                            SelectedMode = ModeVote.Keys.ToList()[2];
                                            break;
                                        default:
                                            SelectedMode = ModeVote.Keys.ToList()[3];
                                            break;
                                    }

                                    ModeVote[SelectedMode].Add(ev.Player);

                                    Color = ModeList[SelectedMode].Color;
                                    Description = ModeList[SelectedMode].Description;
                                }
                                else
                                {
                                    string FirstDesc()
                                    {
                                        switch (SelectMode)
                                        {
                                            case "RandomSelect":
                                                return
                                                    "<b>[선택 모드 : 무작위]</b> <color=#F6CECE>랜덤한 모드가 선택됩니다. 과연 어떤 모드가 걸릴까요?</color>";
                                            case "SimpleSelect":
                                                return
                                                    "<b>[선택 모드 : 롤토체스]</b> <color=#F5D0A9>투표한 유저 중에서 모드가 자동으로 결정됩니다.</color>";
                                            case "MostVote":
                                                return
                                                    "<b>[선택 모드 : 다수결]</b> <color=#E6E0F8>원하는 모드의 번호가 할당된 플랫폼을 밟아 투표하세요.</color>";
                                            case "SecretVote":
                                                return
                                                    "<b>[선택 모드 : 비밀 선거]</b> <color=#E6F8E0>누가 어떤 모드를 투표했는지 알 수 없습니다.</color>";
                                            case "FightVote":
                                                return
                                                    "<b>[선택 모드 : 공포 정치]</b> <color=#FA5858>소수가 지배하는 모드 투표장이 되었습니다.</color>";
                                            default:
                                                return "<b>[버그로 추정됨 : 문의 요망]</b> 어떤 선택 모드도 선택되지 않았습니다. 뭔가 이상합니다.";
                                        }
                                    }

                                    Color = "ffffff";
                                    Description =
                                        $"{FirstDesc()}\n<size=25>[ESC] -> [Settings] -> [Server-specific]에서 유용한 정보를 확인해보세요.</size>";
                                }

                                string createBy()
                                {
                                    if (!ModeList.ContainsKey(SelectedMode) || ModeList[SelectedMode].Author == "")
                                        return "";
                                    else
                                        return
                                            $" <size=20><color=white>Made by <i>{ModeList[SelectedMode].Author}</i></color></size>";
                                }

                                string IdeaBy()
                                {
                                    if (!ModeList.ContainsKey(SelectedMode) || ModeList[SelectedMode].Suggester == "")
                                        return "";
                                    return
                                        $" <size=20><color=white>Idea by <i>{ModeList[SelectedMode].Suggester}</i></color></size>";
                                }

                                string St(int num)
                                {
                                    return SelectMode.Contains("Secret") ? "?" : ModeVote[iv(num)].Count.ToString();
                                }

                                List<string> uc = UsersManager.UsersCache[ev.Player.UserId];
                                string formatted = Notions.LobbyMessage
                                    .Replace("{FirstMark}", ModeVote[iv(1)].Contains(ev.Player) ? "■" : "□")
                                    .Replace("{SecondMark}", ModeVote[iv(2)].Contains(ev.Player) ? "■" : "□")
                                    .Replace("{ThirdMark}", ModeVote[iv(3)].Contains(ev.Player) ? "■" : "□")
                                    .Replace("{FourthMark}", ModeVote[iv(4)].Contains(ev.Player) ? "■" : "□")
                                    .Replace("{First}",
                                        (CurrentMode != ModeType.None
                                            ? CurrentMode.GetModeData().Name
                                            : $"<color=#{ModeList[iv(1)].Color}>{iv(1).GetModeData().Name}</color>") +
                                        (SubModeVote[0] != ModeType.None
                                            ? $" + <b> <size=20><color=#{ModeList[SubModeVote[0]].Color}>{SubModeVote[0].GetModeData().Name}</color></size></b>"
                                            : ""))
                                    .Replace("{FirstVote}",
                                        ModeVote[iv(1)].Contains(ev.Player) ? $"<color=yellow>{St(1)}</color>" : St(1))
                                    .Replace("{Second}",
                                        (CurrentMode != ModeType.None
                                            ? CurrentMode.GetModeData().Name
                                            : $"<color=#{ModeList[iv(2)].Color}>{iv(2).GetModeData().Name}</color>") +
                                        (SubModeVote[1] != ModeType.None
                                            ? $" + <b><size=20><color=#{ModeList[SubModeVote[1]].Color}>{SubModeVote[1].GetModeData().Name}</color></size></b>"
                                            : ""))
                                    .Replace("{SecondVote}",
                                        ModeVote[iv(2)].Contains(ev.Player) ? $"<color=yellow>{St(2)}</color>" : St(2))
                                    .Replace("{Third}",
                                        (CurrentMode != ModeType.None
                                            ? CurrentMode.GetModeData().Name
                                            : $"<color=#{ModeList[iv(3)].Color}>{iv(3).GetModeData().Name}</color>") +
                                        (SubModeVote[2] != ModeType.None
                                            ? $" + <b> <size=20><color=#{ModeList[SubModeVote[2]].Color}>{SubModeVote[2].GetModeData().Name}</color></size></b>"
                                            : ""))
                                    .Replace("{ThirdVote}",
                                        ModeVote[iv(3)].Contains(ev.Player) ? $"<color=yellow>{St(3)}</color>" : St(3))
                                    .Replace("{Fourth}",
                                        (CurrentMode != ModeType.None
                                            ? CurrentMode.GetModeData().Name
                                            : $"<color=#{ModeList[iv(4)].Color}>{iv(4).GetModeData().Name}</color>") +
                                        (SubModeVote[3] != ModeType.None
                                            ? $" + <b> <size=20><color=#{ModeList[SubModeVote[3]].Color}>{SubModeVote[3].GetModeData().Name}</color></size></b>"
                                            : ""))
                                    .Replace("{FourthVote}",
                                        ModeVote[iv(4)].Contains(ev.Player) ? $"<color=yellow>{St(4)}</color>" : St(4))
                                    .Replace("{ModeName}",
                                        $"{(SelectedMode == ModeType.None ? "참고" : SelectedMode.GetModeData().Name)}{createBy()}{IdeaBy()}")
                                    .Replace("{ModeColor}", $"{Color}").Replace("{ModeDescription}", $"{Description}")
                                    .Replace("{Lines}", $"{(Description.Contains("\n") ? "\n" : "\n\n")}")
                                    .Replace("{Exp}", $"{uc[0]}")
                                    .Replace("{RC}", $"{uc[1]}")
                                    .Replace("{Cash}", $"{int.Parse(uc[2]):N0}")
                                    .Replace("{Tip}", Tip)
                                    .Replace("{Version}", $"{Main.Instance.Version}")
                                    .Replace("{Logo}", $"{Logo}");

                                foreach (string name in HighlightModes.Select(x => x.GetModeData().Name))
                                {
                                    formatted = formatted.Replace(name, $"<b>{name}</b>");
                                }

                                ev.Player.AddHint("로비", formatted, 1.2f);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }

                    yield return Timing.WaitForSeconds(0.5f);
                }
            }
        }

        public static IEnumerator<float> OnLeft(LeftEventArgs ev)
        {
            TranslatorPlayers.Remove(ev.Player);
            Chats.Remove(ev.Player);
            if (Texts.ContainsKey(ev.Player))
            {
                Texts[ev.Player].Destroy();
                Texts.Remove(ev.Player);
            }

            OnGround.Remove(ev.Player.UserId);
            PlayersAudio.Remove(ev.Player);
            EffectIntensities.Remove(ev.Player);

            if (Round.IsLobby)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (ModeVote.ContainsKey(ModeVote.Keys.ToList()[i]) &&
                        ModeVote[ModeVote.Keys.ToList()[i]].Contains(ev.Player))
                        ModeVote[ModeVote.Keys.ToList()[i]].Remove(ev.Player);
                }
            }
            else
            {
                if (!PlayersInfo.ContainsKey(ev.Player.UserId)) yield break;
                string nickname = ev.Player.Nickname;
                string role = ev.Player.Role.Name;
                string userId = ev.Player.UserId;

                yield return Timing.WaitForSeconds(1f);

                Webhook.Send($"**⚖️ 재접속 대기**ㅣ`{nickname}`({role}, {userId})");

                for (int i = 1; i < 181; i++)
                {
                    Log.Info($"{nickname}({userId}) 재접속 대기 중.. ({181 - i})");

                    foreach (var player in PlayerManager.List.Where(x => !x.IsNPC))
                    {
                        if (userId != player.UserId) continue;
                        player.Role.Set(PlayersInfo[userId].RoleType);
                        player.MaxHealth = PlayersInfo[userId].MaxHealth;
                        player.Health = PlayersInfo[userId].Health;

                        foreach (var effect in PlayersInfo[userId].ActiveEffects)
                            player.EnableEffect(effect, effect.Intensity, effect.Duration);

                        foreach (var item in PlayersInfo[userId].Items)
                            player.AddItem(item.Type);

                        player.CurrentItem = player.Items.ToList()
                            .Find(x => x.Type == PlayersInfo[userId].CurrentItem.Type);

                        player.Position = new Vector3(PlayersInfo[userId].Position.x,
                            PlayersInfo[userId].Position.y, PlayersInfo[userId].Position.z);

                        PlayersInfo.Remove(userId);

                        PlayerManager.List.Where(x => x.IsDead).ToList().ForEach(x =>
                            x.AddBroadcast(10,
                                $"<size=20>❤️ SCP 재접속 -> <b><i>{player.DisplayNickname}</i></b>(<color={player.Role.Color.ToHex()}>{Trans.Role[player.Role.Type]}</color>)</size>"));
                        Webhook.Send($"**✅ 재접속 완료**ㅣ`{nickname}`({role}, {userId})");
                        yield break;
                    }

                    yield return Timing.WaitForSeconds(1f);
                }
            }
        }

        public static void OnSpawningRagdoll(SpawningRagdollEventArgs ev)
        {
            if (!Round.IsStarted)
                ev.IsAllowed = false;
        }

        public static void OnSpawnedRagdoll(SpawnedRagdollEventArgs ev)
        {
            var ragdoll = ev.Ragdoll;
            if (ragdoll == null)
                return;

            Timing.CallDelayed(5 * 60, () =>
            {
                if (ragdoll.Base == null)
                    return;

                ragdoll.Destroy();
            });
        }

        public static void OnChangingRole(ChangingRoleEventArgs ev)
        {
            if (ev.Player.IsDnd() && ev.NewRole.IsFlamingo())
                ev.IsAllowed = false;
        }

        public static void OnSpawned(SpawnedEventArgs ev)
        {
            if (ev.Player.IsNPC || ev.Player == null || ev.Player.IsHost)
                return;

            ev.Player.EnableEffect(EffectType.FogControl, 1);

            if (MapUtils.LoadedMaps.Keys.Any(Maps.Contains))
                ev.Player.EnableEffect(EffectType.NightVision, 100);


            if (ev.Player.IsAlive)
            {
                if (Round.IsStarted && !ev.Player.IsNonePlayer() && Server.PlayerCount >= 20)
                {
                    DateTime kst = DateTime.UtcNow.AddHours(9); // 어린이날 전용
                    if (kst is { Month: 5, Day: 5 })
                    {
                        if (Random.Range(1, 201) == 1)
                        {
                            Foxy.Create(ev.Player);

                            var name = Random.Range(1, 3) == 1 ? "고급 모드 제안서" : "고급 모드 리롤권";

                            ev.Player.UserId.AddProduct(name, out _);

                            Map.Broadcast(20,
                                $"<size=30><b>🎁 축하드립니다!</b> <i>{ev.Player.DisplayNickname}</i>님께서 {name}(을)를 획득했습니다!</size>");
                            GlobalPlayer.TryPlay("6AM", 1.5f);
                        }
                        else if (Random.Range(1, 51) == 1)
                            Steve.Create(ev.Player);
                    }
                }

                ev.Player.Scale = new Vector3(1, 1, 1);

                if (Round.IsLobby || ev.Reason == SpawnReason.RoundStart)
                {
                }
                else
                    PlayersReport[ev.Player.UserId].Revive += 1;

                if (ev.Player.IsScpRole())
                    Timing.RunCoroutine(ScpGlow(ev.Player));
            }

            if (ev.Reason == SpawnReason.RoundStart)
            {
                if (ev.Player.Zone == ZoneType.Surface) // 지상에 그대로 스폰되는 경우
                {
                    ev.Player.Role.Set(RoleTypeId.Tutorial);
                    ev.Player.Position = PlayerManager.List.GetRandomValue(x => x.IsHuman && x != ev.Player).Position;
                }

                if (ev.Player.IsScpRole())
                {
                    if (ev.Player.Role.Type == RoleTypeId.Scp079)
                    {
                        ev.Player.MaxHealth = 4500;
                        ev.Player.Health = ev.Player.MaxHealth;
                    }

                    if (CurrentMode != ModeType.None && CurrentMode.GetModeData().Info == ModeInfo.Plus)
                    {
                        if (!PlayersInfo.ContainsKey(ev.Player.UserId))
                        {
                            PlayersInfo.Add(ev.Player.UserId, new PlayerInfo
                            {
                                RoleType = ev.Player.Role.Type,
                                MaxHealth = ev.Player.MaxHealth,
                                Health = ev.Player.Health,
                                ActiveEffects = [.. ev.Player.ActiveEffects],
                                Items = [.. ev.Player.Items],
                                CurrentItem = ev.Player.CurrentItem,
                                Position =
                                    new Vector3(ev.Player.Position.x, ev.Player.Position.y, ev.Player.Position.z),
                                Rotation = new Quaternion(ev.Player.Rotation.x, ev.Player.Rotation.y,
                                    ev.Player.Rotation.z, ev.Player.Rotation.w),
                            });
                        }
                    }
                }
                else if (ev.Player.IsHuman)
                {
                    switch (StartupRandom)
                    {
                        // 시작 카오스
                        case 1:
                        {
                            if (ev.Player.Role.Type == RoleTypeId.FacilityGuard)
                                ev.Player.Role.Set(RoleTypeId.ChaosConscript);
                            break;
                        }
                        // 시작 NTF
                        case 2:
                        {
                            if (ev.Player.Role.Type == RoleTypeId.FacilityGuard)
                                ev.Player.Role.Set(RoleTypeId.NtfPrivate);
                            break;
                        }
                    }

                    if (Random.Range(1, 1001) == 1) // 시작 049-2
                    {
                        ev.Player.Role.Set(RoleTypeId.Scp0492);
                        ev.Player.MaxHealth = 1000;
                        ev.Player.Health = ev.Player.MaxHealth;
                    }

                    if (Random.Range(1, 101) == 1 &&
                        !(HolidayUtils.IsHolidayActive(HolidayType.Halloween) ||
                          HolidayUtils.IsHolidayActive(HolidayType.Christmas))) // SCP-3114 추가
                    {
                        ev.Player.Role.Set(RoleTypeId.Scp3114);
                    }
                }
            }

            if (ev.Player.IsAlive &&
                Round.IsStarted &&
                CurrentMode != ModeType.None &&
                new List<SpawnReason>
                {
                    SpawnReason.RoundStart,
                    SpawnReason.Respawn,
                    SpawnReason.RespawnMiniwave,
                    SpawnReason.ItemUsage,
                    SpawnReason.Escaped
                }.Contains(ev.Reason) &&
                (CurrentMode.GetModeData().Info == ModeInfo.Plus ||
                 (CurrentMode.GetModeData().Info == ModeInfo.Lock &&
                  CurrentMode != ModeType.RussianRoulette &&
                  CurrentMode != ModeType.WitGame)))
            {
                ev.Player.EnableEffect(EffectType.SpawnProtected, 1, 10);
            }
        }

        public static void OnInteractingDoor(InteractingDoorEventArgs ev)
        {
            if (!ev.Player.IsScpRole()) return;
            if (ev.Door.Type.ToString().Contains("Checkpoint"))
            {
                if (ev.Player.CurrentItem != null)
                    ev.Door.IsOpen = false;

                if (ev.Door.IsFullyClosed)
                {
                    Timing.CallDelayed(Timing.WaitForOneFrame, () => { ev.Door.IsOpen = true; });
                }
            }

            else if (ev.Player.Role.Type != RoleTypeId.Scp079 && !ev.Door.IsOpen &&
                     !ev.Door.Type.ToString().Contains("Scp079"))
            {
                Timing.CallDelayed(0.1f, () =>
                {
                    if (ev.Door.IsOpen) return;
                    if (!InteractedDoors.ContainsKey(ev.Door))
                        InteractedDoors.Add(ev.Door, 0);

                    InteractedDoors[ev.Door] += 1;

                    if (InteractedDoors[ev.Door] >= 444)
                    {
                        ev.Door.IsOpen = true;

                        InteractedDoors.Remove(ev.Door);
                    }
                    else
                        ev.Player.AddHint("SCP 문 강제 개폐",
                            $"앞으로 {444 - InteractedDoors[ev.Door]}번 상호작용하면 문이 강제로 열립니다.");
                });
            }
        }

        public static void OnHurting(HurtingEventArgs ev)
        {
            if (ev.Player.IsNPC)
            {
                if (ev.Attacker == null || ev.Attacker.IsNPC || ev.Attacker.IsNonePlayer()) return;
                float damage = ev.IsInstantKill
                    ? ev.Player.MaxHealth + ev.Player.MaxArtificialHealth + ev.Player.MaxHumeShield
                    : ev.DamageHandler.Damage;

                if (ev.Attacker != ev.Player && damage <= ushort.MaxValue)
                    PlayersReport[ev.Attacker.UserId].Damage += (int)damage;

                return;
            }

            if (ev.Player.IsNonePlayer()) return;

            if (ev.DamageHandler.Type == DamageType.Falldown &&
                ev.Player.TryGetEffect(EffectType.Lightweight, out StatusEffectBase lightweight) &&
                lightweight.IsEnabled)
            {
                if (HolidayUtils.IsHolidayActive(HolidayType.Halloween) &&
                    ev.Player.TryGetEffect(EffectType.Metal, out StatusEffectBase metal) && metal.IsEnabled)
                {
                }
                else
                    ev.IsAllowed = false;
            }

            if (Round.IsLobby || Round.IsEnded)
                return;

            if (GodModePlayers.Contains(ev.Player))
            {
                if (!Datas.BlockDamageTypes.Contains(ev.DamageHandler.Type) &&
                    !WeakPointAttack.ShouldIgnoreDefenses(ev.Attacker))
                    ev.IsAllowed = false;
            }
            else if (ev.Attacker != null && !ev.Attacker.IsNonePlayer())
            {
                if (ev.Attacker.IsScpRole() &&
                    (ev.DamageHandler.Type.IsWeapon() ||
                     ev.DamageHandler.Type == DamageType.Scp127 ||
                     ev.DamageHandler.Type == DamageType.Scp1509))
                    ev.DamageHandler.Damage *= 0.6f;

                float damage = ev.IsInstantKill
                    ? ev.Player.MaxHealth + ev.Player.MaxArtificialHealth + ev.Player.MaxHumeShield
                    : ev.DamageHandler.Damage;

                if ((HitboxIdentity.IsEnemy(ev.Attacker.ReferenceHub, ev.Player.ReferenceHub) ||
                     ev.Attacker.LeadingTeam != ev.Player.LeadingTeam || Server.FriendlyFire) &&
                    ev.Attacker != ev.Player && damage <= ushort.MaxValue)
                    PlayersReport[ev.Attacker.UserId].Damage += (int)damage;
            }
        }

        public static void OnDying(DyingEventArgs ev)
        {
            if (Round.IsEnded)
                return;

            if (Round.IsLobby)
            {
                ev.Player.ClearInventory();
            }
            else
            {
                if (GodModePlayers.Contains(ev.Player))
                {
                    if (!Datas.BlockDamageTypes.Contains(ev.DamageHandler.Type) &&
                        !WeakPointAttack.ShouldIgnoreDefenses(ev.Attacker))
                        ev.IsAllowed = false;

                    else if (Datas.BlockDamageTypes.Contains(ev.DamageHandler.Type))
                    {
                        GodModePlayers.Remove(ev.Player);
                        ev.Player.Kill(ev.DamageHandler);
                    }
                }
                else
                {
                    if (ev.DamageHandler.Type != DamageType.PocketDimension) return;
                    var attacker = Player.Get(RoleTypeId.Scp106).GetRandomValue();

                    if (attacker == null) return;

                    ev.IsAllowed = false;

                    ev.Player.Kill(new ScpDamageHandler(attacker.ReferenceHub, DeathTranslations.PocketDecay));
                }
            }
        }

        public static void OnDied(DiedEventArgs ev)
        {
            if (ev.Player.IsNonePlayer() ||
                Round.IsEnded ||
                (ev.Attacker != null && ev.Attacker.IsNonePlayer()))
                return;

            // 저장된 효과 삭제
            if (EffectIntensities.TryGetValue(ev.Player, out var effectIntensities))
                effectIntensities.Clear();

            if (!Round.IsStarted)
            {
                // 공포 정치(FightVote) 로비에서는 자동 로비 복귀를 하지 않음
                if (SelectMode == "FightVote")
                    return;

                Timing.CallDelayed(5, () =>
                {
                    if (!Round.IsStarted)
                        Tools.TeleportToLobby(ev.Player);
                });
            }
            else
            {
                string MessageFormat()
                {
                    return ev.Attacker == null
                        ? $"{(PlayersInfo.ContainsKey(ev.Player.UserId) && ev.DamageHandler.Type == DamageType.Unknown ? "⏳ <color=#FF0000><b>SCP 탈주</b></color>(3분 내로 재접속 가능)" : "💀 <color=#A4A4A4>자살</color>")}ㅣ{Tools.BadgeFormat(ev.Player)}<color=#F2F5A9>{ev.Player.DisplayNickname}</color>(<color={ev.TargetOldRole.GetColor().ToHex()}>{Trans.Role[ev.TargetOldRole]}</color>) - {ev.DamageHandler.Type}"
                        : $"💔 <color=#FAAC58>{(ev.Player.IsCuffed ? "<b>체포킬</b>(신고 가능 여부는 규칙 확인)" : "사살")}</color>ㅣ{Tools.BadgeFormat(ev.Attacker)}<color=#F2F5A9><i>{ev.Attacker.DisplayNickname}</i></color>(<color={ev.Attacker.Role.Color.ToHex()}>{Trans.Role[ev.Attacker.Role.Type]}</color>) -> {Tools.BadgeFormat(ev.Player)}<color=#F2F5A9>{ev.Player.DisplayNickname}</color>(<color={ev.TargetOldRole.GetColor().ToHex()}>{Trans.Role[ev.TargetOldRole]}</color>) - {ev.DamageHandler.Type}";
                }

                foreach (var player in PlayerManager.List.Where(x => x.IsDead || x == ev.Attacker))
                    player.AddBroadcast(10, $"<size=20>{MessageFormat()}</size>", tag: "kill");

                if (ev.Attacker != null && !ev.Attacker.IsNPC)
                {
                    PlayersReport[ev.Attacker.UserId].Kill += 1;

                    if (ev.TargetOldRole.IsScpRole())
                        PlayersReport[ev.Attacker.UserId].KillScp += 1;
                    else
                        PlayersReport[ev.Attacker.UserId].KillHuman += 1;

                    if (PlayersAudio.TryGetValue(ev.Attacker, out var attackerAudio))
                        attackerAudio.TryPlay("Overwatch2Kill", 2);
                }

                if (ev.Player.IsNPC) return;
                PlayersReport[ev.Player.UserId].Death += 1;
                PlayersReport[ev.Player.UserId].LastDeath = DateTime.UtcNow;
            }
        }

        public static void OnItemAdded(ItemAddedEventArgs ev)
        {
            if (!ev.Player.IsScpRole()) return;
            ev.Player.CurrentItem ??= ev.Item; // IF ev.Player.CurrentItem NULL
        }

        public static void OnUsingItem(UsingItemEventArgs ev)
        {
            if (ev.Item.Type != ItemType.SCP1576) return;
            foreach (var none in NonePlayer.Players.ToList())
            {
                none.Role.Set(RoleTypeId.Spectator);
            }
        }

        public static void OnDroppingItem(DroppingItemEventArgs ev)
        {
            if (Round.IsLobby)
                ev.IsAllowed = false;
        }

        public static void OnDroppingAmmo(DroppingAmmoEventArgs ev)
        {
            if (Round.IsLobby)
                ev.IsAllowed = false;
        }

        public static void OnDroppedItem(DroppedItemEventArgs ev)
        {
            var pickup = ev.Pickup;
            if (pickup == null)
                return;

            Timing.CallDelayed(5 * 60, () =>
            {
                if (pickup.Base == null)
                    return;

                pickup.Destroy();
            });
        }

        public static void OnDroppedAmmo(DroppedAmmoEventArgs ev)
        {
            var ammoPickups = ev.AmmoPickups?.ToArray();
            if (ammoPickups == null)
                return;

            Timing.CallDelayed(5 * 60, () =>
            {
                foreach (var ammo in ammoPickups)
                {
                    if (ammo?.Base == null)
                        continue;

                    ammo.Destroy();
                }
            });
        }

        public static void OnShooting(ShootingEventArgs ev)
        {
            if (ev.ClaimedTarget == null) return;
            if (ev.Player.Role is not Scp173Role scp173) return;
            if (!ev.Player.TryGetLookPlayer(1000, out Player target, out RaycastHit? raycastHit)) return;
            if (ev.ClaimedTarget != target) return;
            if (!scp173.IsObserved) return;

            HitboxType hitbox = raycastHit?.collider.TryGetComponent(out HitboxIdentity hitboxIdentity) == true
                ? hitboxIdentity.HitboxType
                : HitboxType.Body;
            float hitboxMultiplier = FirearmDamageHandler.HitboxDamageMultipliers.TryGetValue(
                hitbox,
                out float multiplier)
                ? multiplier
                : 1f;

            ev.ClaimedTarget.Hurt(new ScpDamageHandler(ev.Player.ReferenceHub,
                ev.Firearm.Damage * 0.6f * hitboxMultiplier, DeathTranslations.Scp173));

            ev.Player.ShowHitMarker();
        }

        public static void OnKicking(KickingEventArgs ev)
        {
            if (ev.Player.IsNPC)
            {
                ev.IsAllowed = false;
                return;
            }

            MultiBroadcast.API.MultiBroadcast.AddMapBroadcast(10,
                $"<size=20>{ev.Target.Nickname}(이)가 서버에서 <color=red>추방</color>되었습니다. (사유: {ev.Reason})</size>");
        }

        public static void OnBanning(BanningEventArgs ev)
        {
            if (ev.Player.IsNPC)
            {
                ev.IsAllowed = false;
                return;
            }

            MultiBroadcast.API.MultiBroadcast.AddMapBroadcast(10,
                $"<size=20>{ev.Target.Nickname}(이)가 서버에서 <color=red>차단</color>되었습니다. (사유: {ev.Reason})</size>");
        }

        public static void OnChangingGroup(ChangingGroupEventArgs ev)
        {
            if (ev.Player.Group == null) return;
            var permission = ev.Player.Group.Permissions;

            Timing.CallDelayed(1, () => { ev.Player.Group.Permissions = permission; });
        }

        public static void OnVoiceChatting(VoiceChattingEventArgs ev)
        {
            if (SelectMode.Contains("Secret") && Round.IsLobby)
                ev.IsAllowed = false;
        }

        public static void OnDamagingShootingTarget(DamagingShootingTargetEventArgs ev)
        {
            if (ev.ShootingTarget == Target1 || ev.ShootingTarget == Target2)
            {
                ShootingTargetSignal = true;
            }
        }

        public static void OnEscaping(EscapingEventArgs ev)
        {
            if (!ev.Player.IsCuffed || !ev.IsAllowed) return;
            if (ev.Player.Cuffer.Role.Type != RoleTypeId.Tutorial) return;
            ev.IsAllowed = false;

            Tools.MakeSnake(ev.Player);
        }
    }
}