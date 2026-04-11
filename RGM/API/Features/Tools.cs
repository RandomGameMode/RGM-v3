using AdminToys;
using DiscordInteraction.Discord;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using InventorySystem.Items.Usables.Scp330;
using MEC;
using Mirror;
using NetworkManagerUtils.Dummies;
using PlayerRoles;
using ProjectMER.Features;
using ProjectMER.Features.Serializable;
using RGM.API.Components;
using RGM.API.DataBases;
using RGM.API.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using static RGM.Variables.Variable;

namespace RGM.API.Features
{
    public static class Tools
    {
        public static T GetRandomValue<T>(List<T> list)
        {
            System.Random random = new System.Random();
            int index = random.Next(0, list.Count);
            return list[index];
        }

        public static List<T> EnumToList<T>()
        {
            Array items = Enum.GetValues(typeof(T));
            List<T> itemList = new List<T>();

            foreach (T item in items)
            {
                List<string> list = new List<string>() 
                {
                    "None",
                    "Unknown",
                    "Destroyed"
                };

                if (!list.Contains(item.ToString()))
                    itemList.Add(item);
            }

            return itemList;
        }

        public static void PickModes()
        {
            try
            {
                ModeVote.Clear();
                SubModeVote.Clear();

                for (int i = 1; i < 5; i++)
                {
                    var StaticModeList = ModeList.Keys.Where(x => ModeList[x].Category == ModeCategory.Public && !ModeVote.ContainsKey(x)).ToList();
                    var mode = StaticModeList.GetRandomValue();
                    ModeVote.Add(mode, new List<Player>());

                    if (mode.GetModeData().Info != ModeInfo.Lock && UnityEngine.Random.Range(1, 11) == 1)
                        SubModeVote.Add(ModeList.Keys.Where(x => ModeList[x].Category != ModeCategory.Private && ModeList[x].Info != ModeInfo.Lock && !ModeVote.ContainsKey(x) && ModeList.Keys.Where(x => x.GetModeData().Info != ModeInfo.Set).Contains(x)).GetRandomValue());

                    else
                        SubModeVote.Add(ModeType.None);
                }
                List<List<Transform>> Pads = new List<List<Transform>>() { First, Second, Third, Fourth };

                for (int i = 0; i < 4; i++)
                {
                    try
                    {
                        foreach (var Pad in Pads[i])
                            Pad.GetComponent<PrimitiveObjectToy>().NetworkMaterialColor = ColorUtility.TryParseHtmlString("#" + ModeList[ModeVote.Keys.ToList()[i]].Color, out Color color) ? color : Color.white;
                    }
                    catch (Exception e) { }
                }

                Color randomColor = Tools.GetRandomColor(true);

                Numbers.ForEach(x => x.GetComponent<PrimitiveObjectToy>().NetworkMaterialColor = randomColor);
                RandomColors.ForEach(x => x.GetComponent<PrimitiveObjectToy>().NetworkMaterialColor = randomColor);
                RandomLights.ForEach(x => x.GetComponent<LightSourceToy>().NetworkLightColor = Tools.GetRandomColor());
                Balls.ForEach(x => x.GetComponent<PrimitiveObjectToy>().NetworkMaterialColor = Tools.GetRandomColor(true));
            }
            catch (Exception e)
            {
                Log.Error($"[RGM] {e}");
            }
        }

        public static void TeleportToLobby(Player player)
        {
            List<RoleTypeId> humans = new List<RoleTypeId>()
            {
                RoleTypeId.ClassD,
                RoleTypeId.Scientist,
                RoleTypeId.FacilityGuard,
                RoleTypeId.ChaosConscript,
                RoleTypeId.NtfSpecialist,
                RoleTypeId.Tutorial
            };

            player.Role.Set(humans.GetRandomValue());
            player.ClearInventory();
            player.Position = GameObject.Find("LobbyStartPoint").transform.position;

            if (SelectMode == "FightVote")
            {
                switch (UnityEngine.Random.Range(1, 16)) 
                {
                    case 1:
                        player.AddItem(ItemType.GunRevolver);
                        player.AddAmmo(AmmoType.Ammo44Cal, 1205);
                        break;

                    case 2:
                        player.AddItem(ItemType.GrenadeHE);
                        break;
                }
            }
        }

        public static List<Transform> GetObjectList(string Name)
        {
            return GameObject.FindObjectsByType<Transform>(FindObjectsSortMode.InstanceID).Where(t => t.name == Name).ToList();
        }

        public static List<string> GetModeDesc(ModeType ModeType, ModeType SubModeType)
        {
            string Color = ModeList[ModeType].Color;
            string Name = ModeList[ModeType].Name;
            string Description = ModeList[ModeType].Description;
            string Detail = ModeList[ModeType].Detail;

            string Message = Notions.StartModeDescription
                .Replace("{ModeColor}", Color)
                .Replace("{CurrentMode}", Name)
                .Replace("{CurrentSubMode}", SubModeType != ModeType.None ? $"<size=20>추가된 서브 모드 : <color=#{ModeList[SubModeType].Color}>{ModeList[SubModeType].Name}</color></size>\n" : "")
                .Replace("{ModeDescription}", Description)
                .Replace("{ModeInfo}", ModeType.GetModeData().Info.ToString());

            return new List<string>() 
            { 
                Message,
                            "성공적으로 모드 설명을 불러왔습니다.",
                            "해당 모드에 대한 자세한 설명이 없습니다.", 
                Detail 
            };
        }

        public static Color GetRandomColor(bool Transparency = false)
        {
            if (!Transparency)
                return new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value, 1);

            else
                return new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
        }

        public static string GetPlayerInfo(Player player)
        {
            List<string> uc = UsersManager.UsersCache[player.UserId];

            string GetJoinedInfo(int num)
            {
                if (uc[num] == "0")
                    return "-";

                else
                    return string.Join(", ", uc[num].Split('/'));
            }

            return
$"""


<size=20><b><i>{player.Nickname}</i></b>님의 정보</size>

<size=15>SteamID: {player.UserId}</size>
<size=15>Exp: {uc[0]}</size>
<size=15>랜덤코인: {uc[1]}</size>
<size=15>Cash: ₩{int.Parse(uc[2]).ToString("N0")}</size>
<size=15>누적 출석: {uc[27]}회 · 현재 {uc[30]}일 연속 출석 중 (최대 {uc[28]}일)</size>

<size=15>보유한 킬이펙트: {GetJoinedInfo(3)}</size>
<size=15>장착한 킬이펙트: {(uc[4] == "0" ? "-" : uc[4])} ({(uc[15] == "0" ? "랜덤 적용 ❌" : "랜덤 적용 ✅")})</size>
<size=10>{(uc[4] == "0" ? "'.킬이펙트 <킬이펙트 이름>' 명령어를 사용하여 킬이펙트를 장착할 수 있습니다." : KillEffects[uc[4]])}</size>

<size=15>보유한 스폰이펙트: {GetJoinedInfo(19)}</size>
<size=15>장착한 스폰이펙트: {(uc[20] == "0" ? "-" : uc[20])} ({(uc[21] == "0" ? "랜덤 적용 ❌" : "랜덤 적용 ✅")})</size>
<size=10>{(uc[20] == "0" ? "'.스폰이펙트 <스폰이펙트 이름>' 명령어를 사용하여 스폰이펙트를 장착할 수 있습니다." : SpawnEffects[uc[20]])}</size>

<size=15>보유한 커스텀: {GetJoinedInfo(7)}</size>
<size=15>커스텀 닉네임: {(uc[5] == "0" ? "-" : uc[5])}</size>
<size=10>{(uc[5] == "0" ? "'.닉네임 <텍스트>' 명령어를 사용하여 커스텀 닉네임을 설정할 수 있습니다." : $"미리 보기: {Tools.CustomFormatter(player, uc[5]).Replace("\n", "\\n")}")}</size>
<size=15>커스텀 인포: {(uc[6] == "0" ? "-" : uc[6])}</size>
<size=10>{(uc[6] == "0" ? "'.인포 <텍스트>' 명령어를 사용하여 커스텀 인포를 설정할 수 있습니다." : $"미리 보기: {Tools.CustomFormatter(player, uc[6]).Replace("\n", "\\n")}")}</size>

<size=15>보유한 페인트: {GetJoinedInfo(8)}</size>
<size=15>장착한 페인트: {(uc[9] == "0" ? "-" : uc[9])} ({(uc[16] == "0" ? "랜덤 적용 ❌" : "랜덤 적용 ✅")})</size>
<size=10>{(uc[9] == "0" ? "'.페인트 <페인트 이름>' 명령어를 사용하여 페인트를 장착할 수 있습니다." : Paints[uc[9]])}</size>

<size=15>보유한 칭호: {GetJoinedInfo(10)}</size>
<size=15>장착한 칭호: {(uc[11] == "0" ? "-" : uc[11])} ({(uc[17] == "0" ? "랜덤 적용 ❌" : "랜덤 적용 ✅")})</size>
<size=10>{(uc[11] == "0" ? "'.칭호 <칭호 이름>' 명령어를 사용하여 칭호를 장착할 수 있습니다." : Badges[uc[11]])}</size>

<size=15>보유한 아이콘: {GetJoinedInfo(24)}</size>
<size=15>장착한 아이콘: {(uc[25] == "0" ? "-" : uc[25])} ({(uc[26] == "0" ? "랜덤 적용 ❌" : "랜덤 적용 ✅")})</size>
<size=10>{(uc[25] == "0" ? "'.아이콘 <아이콘 이름>' 명령어를 사용하여 아이콘을 장착할 수 있습니다." : Icons[uc[25]])}</size>
""";
        }

        public static void ChangePaint(Player player, string Color)
        {
            if (Color != "0")
            {
                Dictionary<string, string[]> ColorDictionary = new Dictionary<string, string[]>()
                {
                    {"블랙골드", new string[] { "brown", "yellow" } },
                    {"핫핑크", new string[] { "magenta", "pink" } },
                    {"레인보우", Datas.Colors.Keys.ToArray() },
                    {"분홍색", new string[] { "pink" } },
                    {"빨간색", new string[] { "red" } },
                    {"흰색", new string[] { "default" } },
                    {"갈색", new string[] { "brown" } },
                    {"은색", new string[] { "silver" } },
                    {"밝은 녹색", new string[] { "light_green" } },
                    {"진홍색", new string[] { "crimson" } },
                    {"청록색", new string[] { "cyan" } },
                    {"옥색", new string[] { "aqua" } },
                    {"진한 분홍색", new string[] { "deep_pink" } },
                    {"토마토색", new string[] { "tomato" } },
                    {"노란색", new string[] { "yellow" } },
                    {"짙은 홍색", new string[] { "magenta" } },
                    {"푸른 녹색", new string[] { "blue_green" } },
                    {"주황색", new string[] { "orange" } },
                    {"라임색", new string[] { "lime" } },
                    {"초록색", new string[] { "green" } },
                    {"에메랄드색", new string[] { "emerald" } },
                    {"카민색", new string[] { "carmine" } },
                    {"니켈색", new string[] { "nickel" } },
                    {"박하색", new string[] { "mint" } },
                    {"군대 녹색", new string[] { "army_green" } },
                    {"호박색", new string[] { "pumpkin" } }
                };

                if (player.GameObject.TryGetComponent<TagController>(out TagController rtc))
                    UnityEngine.Object.Destroy(rtc);

                TagController rtController = player.GameObject.AddComponent<TagController>();
                rtController.Colors = ColorDictionary[Color];
                rtController.Interval = 1;
            }
        }

        public static void RemovePaint(Player player)
        {
            if (player.GameObject.TryGetComponent<TagController>(out TagController rtc))
                UnityEngine.Object.Destroy(rtc);

            player.RankColor = null;
        }

        public static IEnumerator<float> SetWinner(List<Player> playerList, int amount)
        {
            if (IsWinnerSelected || Main.Instance.Config.FixedModes.Count() > 0)
                yield break;

            IsWinnerSelected = true;

            if (Server.PlayerCount >= 15)
            {
                int is게임칩사용자(Player player)
                {
                    if (게임칩사용자.Contains(player.UserId))
                    {
                        PlaySound(player.Transform, "money-soundfx", 2);
                        return 10;
                    }

                    return 1;
                }

                foreach (var player in playerList.Where(x => !x.IsNonePlayer() && UsersManager.UsersCache.ContainsKey(x.UserId)))
                {
                    UsersManager.UsersCache[player.UserId][0] = (int.Parse(UsersManager.UsersCache[player.UserId][0]) + amount).ToString();
                    UsersManager.UsersCache[player.UserId][1] = (int.Parse(UsersManager.UsersCache[player.UserId][1]) + amount * is게임칩사용자(player)).ToString();
                }

                UsersManager.SaveUsers();

                WinMessage = $"<size={30 - Math.Round(playerList.Count() * 0.5f)}><color=yellow><b>✨</b></color> <b>{string.Join($", ", playerList.Select(x => $"<color={x.Role.Color.ToHex()}><i>{x.DisplayNickname}</i></color>"))}</b>(이)가 <b>{amount}</b> EXP, 랜덤코인을 획득하였습니다";
            }
            else
            {
                WinMessage = $"<size=25>서버 인원이 15명 이하이므로 우승 보상은 지급되지 않습니다.</size>";
            }
        }

        public static bool TryGetNearestPlayer(this Player player, out Player nearestPlayer, out float radius, List<Player> exceptPlayers = null)
        {
            nearestPlayer = null;
            radius = 99999;

            if (exceptPlayers == null)
                exceptPlayers = new List<Player>();

            foreach (var near in PlayerManager.List.Where(x => x.IsAlive && x != player && !exceptPlayers.Contains(x)))
            {
                float Distance = Vector3.Distance(near.Position, player.Position);

                if (Distance < radius)
                {
                    nearestPlayer = near;
                    radius = Distance;
                }
            }

            if (nearestPlayer != null)
                return true;

            else
                return false;
        }

        public static bool TryGetLookPlayer(this Player player, float Distance, out Player target, out RaycastHit? raycastHit)
        {
            target = null;
            raycastHit = null;

            if (Physics.Raycast(player.ReferenceHub.PlayerCameraReference.position + player.ReferenceHub.PlayerCameraReference.forward * 0.2f, player.ReferenceHub.PlayerCameraReference.forward, out RaycastHit hit, Distance) &&
                    hit.collider.TryGetComponent<IDestructible>(out IDestructible destructible))
            {
                if (Player.TryGet(hit.collider.GetComponentInParent<ReferenceHub>().gameObject, out Player t) && player != t)
                {
                    target = t;
                    raycastHit = hit;

                    return true;
                }
            }

            return false;
        }

        public static bool TryGetLookPlayers(Player player, float distance, out List<Player> targets, out RaycastHit? raycastHit, int count = 100)
        {
            targets = new List<Player>();
            raycastHit = null;

            var origin = player.ReferenceHub.PlayerCameraReference.position + player.ReferenceHub.PlayerCameraReference.forward * 0.2f;
            var direction = player.ReferenceHub.PlayerCameraReference.forward;
            RaycastHit[] hits = Physics.RaycastAll(origin, direction, distance);

            foreach (var hit in hits.OrderBy(h => h.distance))
            {
                if (hit.collider.TryGetComponent<IDestructible>(out IDestructible destructible))
                {
                    if (Player.TryGet(hit.collider.GetComponentInParent<ReferenceHub>().gameObject, out Player t) && !targets.Contains(t))
                    {
                        targets.Add(t);
                        if (targets.Count == 1)
                            raycastHit = hit;
                        if (targets.Count >= count)
                            return true;
                    }
                }
            }

            return targets.Count > 0;
        }

        public static bool TryInstallMode(ModeType ModeType)
        {
            var modeType = Type.GetType($"RGM.Modes.{ModeType}");

            if (modeType == null)
            {
                if (ModeList.ContainsKey(ModeType.GetModeData().Type))
                    modeType = Type.GetType($"RGM.Modes.{modeType}");
            }

            if (modeType != null)
            {
                Mode modeInstance = (Mode)Activator.CreateInstance(modeType);
                modeInstance.Data = ModeList[ModeType];
                EnabledModeList.Add(modeInstance);

                var onEnabledMethod = modeType.GetMethod("OnEnabled");

                if (ModeType.GetModeData().Map != "")
                    LoadMap(ModeType.GetModeData().Map);

                onEnabledMethod?.Invoke(modeInstance, null);

                if (Server.Port != 7803 && !Round.IsEnded && Server.PlayerCount >= 15)
                    Webhook.Send($"{ModeType.GetModeData().Name}", ReadTextFile(Path.Combine(Paths.Configs, "RGM"), "Webhook3.txt"));

                return true;
            }
            else
                return false;
        }

        public static bool UnInstallMode(ModeType ModeType)
        {
            var modeType = Type.GetType($"RGM.Modes.{ModeType}");

            if (modeType == null)
            {
                if (ModeList.ContainsKey(ModeType.GetModeData().Type))
                    modeType = Type.GetType($"RGM.Modes.{modeType}");
            }

            if (modeType != null)
            {
                Mode mode = EnabledModeList.First(x => x.Data.Type == ModeType);

                if (mode == null)
                    return false;

                mode.OnDisabled();

                if (ModeType.GetModeData().Map != "")
                    Server.ExecuteCommand($"/mp unload {ModeType.GetModeData().Map}");

                EnabledModeList.Remove(mode);

                return true;
            }
            else
                return false;
        }

        public static string TryGetUserId(string Name)
        {
            if (Name.Contains("@steam"))
                return Name;

            else if (Player.TryGet(Name, out Player player))
                return player.UserId;

            else
                return null;
        }

        public static List<Vector3> GetCirclePoints(Vector3 center, float radius, int pointCount)
        {
            List<Vector3> points = new List<Vector3>();
            float angleStep = 2 * Mathf.PI / pointCount;

            for (int i = 0; i < pointCount; i++)
            {
                float angle = i * angleStep;
                float x = center.x + radius * Mathf.Cos(angle);
                float z = center.z + radius * Mathf.Sin(angle);
                points.Add(new Vector3(x, center.y, z));
            }

            return points;
        }

        public static IEnumerator<float> BugVote(Player host, string reason)
        {
            int RequiredCount = PlayerManager.List.Count / 2;
            bool IsSuccess = false;

            Webhook.Send($"🗳️ **게임 진행 불가 투표**ㅣ{host.Nickname}에 의해 시작됨 ({reason})");

            for (int i = 1; i<21; i++)
            {
                if (BugVotePlayers.Count() >= RequiredCount)
                {
                    IsSuccess = true;
                    break;
                }

                foreach (var player in PlayerManager.List)
                    player.AddHint("게임진행불가투표", $"<size=25><i>{host.DisplayNickname}</i>(이)가 <b><color=#FFBF00>게임 진행 불가 투표</color></b>를 개설하였습니다.\n라운드를 강제로 종료해야 한다면 <b>.찬성</b> 명령어를 입력하세요.</size>\n이유 : {reason}\n<size=20>투표 종료까지 {21 - i}초 남음 ({BugVotePlayers.Count}/{RequiredCount})</size>", 1.2f);

                yield return Timing.WaitForSeconds(1);
            }

            if (IsSuccess)
            {
                MultiBroadcast.API.MultiBroadcast.AddMapBroadcast(5, $"게임 진행 불가 투표가 <b><color=#9AFE2E>가결</color></b>되었습니다. 곧 서버가 재시작됩니다.");

                Webhook.Send($"🗳️ **게임 진행 불가 투표**ㅣ✅ 가결됨 (투표자: {string.Join(", ", BugVotePlayers.Select(x => x.Nickname))})");

                yield return Timing.WaitForSeconds(5);

                Server.ExecuteCommand($"sr");
            }
            else
            {
                MultiBroadcast.API.MultiBroadcast.AddMapBroadcast(5, $"게임 진행 불가 투표가 <b><color=#FE2E2E>부결</color></b>되었습니다.");

                Webhook.Send($"🗳️ **게임 진행 불가 투표**ㅣ❌ 부결됨 (투표자: {string.Join(", ", BugVotePlayers.Select(x => x.Nickname))})");
            }

            IsBugVoteProcessing = false;
            BugVotePlayers.Clear();
        }

        public static IEnumerator<float> Suggest(Player host, string reason)
        {
            int RequiredCount = PlayerManager.List.Count / 2;
            bool IsSuccess = false;

            Webhook.Send($"🔐 **의문의 제안**ㅣ{host.Nickname}에 의해 시작됨 ({reason})");

            for (int i = 1; i < 31; i++)
            {
                if (SuggestPlayers.Count >= RequiredCount)
                {
                    IsSuccess = true;
                    break;
                }

                foreach (var player in PlayerManager.List)
                    player.AddHint("의문의 제안", $"<size=25><b><color=#DA81F5>의문의 제안</color></b>이 개설되었습니다.\n제안을 수락하시려면 <b>.수락</b> 명령어를 입력하세요.</size>\n{reason}\n<size=20>투표 종료까지 {31 - i}초 남음 ({SuggestPlayers.Count}/{RequiredCount})</size>", 1.2f);

                yield return Timing.WaitForSeconds(1);
            }

            if (IsSuccess)
            {
                MultiBroadcast.API.MultiBroadcast.AddMapBroadcast(5, $"의문의 제안이 <b><color=#9AFE2E>가결</color></b>되었습니다.");

                Webhook.Send($"🔐 **의문의 제안**ㅣ✅ 가결됨 (투표자: {string.Join(", ", SuggestPlayers.Select(x => x.Nickname))})");
            }
            else
            {
                MultiBroadcast.API.MultiBroadcast.AddMapBroadcast(5, $"의문의 제안이 <b><color=#FE2E2E>부결</color></b>되었습니다.");

                Webhook.Send($"🔐 **의문의 제안**ㅣ❌ 부결됨 (투표자: {string.Join(", ", SuggestPlayers.Select(x => x.Nickname))})");
            }

            IsSuggestProcessing = false;
            SuggestPlayers.Clear();
        }

        public static void CallSnakeHand(Player Convener, List<Player> PlayerList)
        {
            List<Player> SnakeHands = PlayerList;

            List<ItemType> Items = new List<ItemType>
                {
                    ItemType.KeycardFacilityManager,
                    ItemType.GunFSP9,
                    ItemType.GunRevolver,
                    ItemType.Adrenaline,
                    ItemType.AntiSCP207
                };

            List<ItemType> Ammos = new List<ItemType>
                {
                    ItemType.Ammo44cal,
                    ItemType.Ammo9x19
                };

            foreach (var p in SnakeHands)
            {
                p.Role.Set(RoleTypeId.Tutorial);
                p.Position = new Vector3(0.125f, 300.9572f, 4.960938f);

                foreach (ItemType Item in Items)
                    p.AddItem(Item);

                for (int i = 1; i < 3; i++)
                {
                    foreach (var Ammo in Ammos)
                        p.AddItem(Ammo);
                }
            }

            if (Convener != null)
                Convener.AddHint("뱀의 손", $"{SnakeHands.Count()}명의 <color=#FE2EF7>동료</color>들이 당신과 함께합니다..", 5f);
        }

        public static string ColorFormat(string cn)
        {
            if (ColorUtility.TryParseHtmlString(cn, out Color color))
                return color.ToHex();

            else
            {
                var cd = Datas.Colors;

                if (cd.ContainsKey(cn))
                    return cd[cn];

                else
                    return "#FFFFFF";
            }
        }

        public static string BadgeFormat(Player player)
        {
            if (player.RankName != null && !player.BadgeHidden)
                return $"[<color={ColorFormat(player.RankColor)}>{player.RankName}</color>] ";

            else
                return "";
        }

        public static string CustomFormatter(Player player, string str)
        {
            Dictionary<string, PlayerReport> pr = PlayersReport;

            return str
                .Replace("\\n", "\n")
                .Replace("{name}", player.Nickname)
                .Replace("{kill}", $"{pr[player.UserId].Kill}")
                .Replace("{death}", $"{pr[player.UserId].Death}")
                .Replace("{revive}", $"{pr[player.UserId].Revive}")
                .Replace("{kill_scp}", $"{pr[player.UserId].KillScp}")
                .Replace("{kill_human}", $"{pr[player.UserId].KillHuman}")
                .Replace("{max_health}", $"{player.MaxHealth}")
                .Replace("{health}", $"{player.Health}")
                .Replace("{items_count}", $"{player.Items.Count}")
                .Replace("{role}", $"{Trans.Role[player.Role]}")
                .Replace("{damage}", $"{pr[player.UserId].Damage}")
                ;
        }

        public static bool TryGetRaycastPoint(Player player, float _distance, out Vector3 _point)
        {
            Vector3 forward = player.CameraTransform.forward;
            RaycastHit raycastHit;

            if (!Physics.Raycast(player.CameraTransform.position + forward, forward, out raycastHit, _distance))
            {
                _point = Vector3.zero;
                return false;
            }
            else
            {
                _point = raycastHit.point;
                return true;
            }
        }

        public static void GetAllChildren(Transform parentTransform)
        {
            foreach (Transform childTransform in parentTransform)
            {
                Debug.Log(childTransform.name);

                PrimitiveObjectToy primitiveObject = childTransform.GetComponent<PrimitiveObjectToy>();

                if (primitiveObject != null)
                {
                    primitiveObject.PrimitiveFlags = PrimitiveFlags.Visible;
                }

                GetAllChildren(childTransform);
            }
        }

        public static AudioClipPlayback PlayGlobalAudio(string clipName, float volume = 1, bool loop = false, bool destroyOnEnd = true)
        {
            string notice = $"로드된 오디오: {clipName}";

            foreach (var player in PlayerManager.List)
            {
                player.AddBroadcast(10, $"<size=20>{notice}</size>");
            }

            Log.Info(notice);

            return GlobalPlayer.TryPlay(clipName, volume, loop, destroyOnEnd);
        }

        public static MapSchematic LoadMap(string mapName, bool notice = true)
        {
            Log.Info($"로드 시도중인 맵: {mapName}");
            MapSchematic map = MapUtils.GetMapData(mapName);

            if (map == null)
            {
                Log.Error($"맵 '{mapName}'을(를) 찾을 수 없습니다. 로드 실패.");
                return null;
            }

            if (!MapUtils.LoadedMaps.ContainsKey(mapName))
            {
                if (Maps.Contains(mapName))
                {
                    if (UnityEngine.Random.Range(1, 3) == 1)
                    {
                        ObjectSpawner.SpawnSchematic("Sun", new Vector3(0, 1500, 0));
                    }
                }

                MapUtils.LoadMap(mapName);
            }

            Log.Info($"로드된 맵: {mapName}");

            if (notice)
            {
                foreach (var player in PlayerManager.List)
                {
                    player.AddBroadcast(10, $"<size=20>로드된 맵: {mapName}</size>");
                }
            }

            return map;
        }

        public static string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            System.Random random = new System.Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static string GenerateRandomHexColor()
        {
            int colorValue = UnityEngine.Random.Range(0, 0x1000000);
            return $"#{colorValue:X6}";
        }

        public static IEnumerator<float> DoRocket(Player attacker, Player player, float speed)
        {
            int amnt = 0;
            while (player.Role != RoleTypeId.Spectator)
            {
                player.Position += Vector3.up * speed;
                int num = amnt;
                amnt = num + 1;
                bool flag = amnt >= 50;
                if (flag)
                {
                    player.IsGodModeEnabled = false;
                    ExplosiveGrenade grenade = (ExplosiveGrenade)Item.Create(ItemType.GrenadeHE, null);
                    grenade.FuseTime = 0.5f;
                    grenade.SpawnActive(player.Position, attacker);
                    player.Hit(attacker, player.MaxHealth);
                    grenade = null;
                }

                yield return float.NegativeInfinity;
            }

            yield break;
        }

        public static LabApi.Features.Wrappers.TextToy CreateText(Vector3 pos, Quaternion rot, string text, float time = 20)
        {
            LabApi.Features.Wrappers.TextToy textToy = LabApi.Features.Wrappers.TextToy.Create();
            textToy.Position = pos;
            textToy.Rotation = rot;
            textToy.DisplaySize = new Vector2(100000, 100000);
            textToy.TextFormat = text;

            if (time != 0)
                Timing.CallDelayed(time, textToy.Destroy);

            return textToy;
        }

        public static AudioClipPlayback PlaySound(Transform transform, string name, float volume = 1, bool loop = false, bool isSpatial = true, float minDistance = 1, float maxDistance = 10)
        {
            AudioPlayer audioPlayer = AudioPlayer.CreateOrGet($"Transform - {transform.position}", condition: (ReferenceHub hub) =>
            {
                return !MuteBGMPlayers.Contains(Player.Get(hub));
            },onIntialCreation: (p) =>
            {
                p.transform.parent = transform;

                Speaker speaker = p.AddSpeaker("Main", isSpatial: isSpatial, minDistance: minDistance, maxDistance: maxDistance);

                speaker.transform.parent = transform;
                speaker.transform.localPosition = Vector3.zero;
            });

            return audioPlayer.TryPlay(name, volume, loop);
        }

        public static string ApplyGradient(string hexColor, string text)
        {
            Color baseColor;
            if (!ColorUtility.TryParseHtmlString(hexColor, out baseColor))
                return text;

            int length = text.Length;
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < length; i++)
            {
                float t = (float)i / (length - 1);
                float brightness = Mathf.Lerp(0.6f, 1.4f, t);

                Color c = new Color(
                    Mathf.Clamp01(baseColor.r * brightness),
                    Mathf.Clamp01(baseColor.g * brightness),
                    Mathf.Clamp01(baseColor.b * brightness)
                );

                string colorHex = ColorUtility.ToHtmlStringRGB(c);
                sb.Append($"<color=#{colorHex}>{text[i]}</color>");
            }

            return sb.ToString();
        }

        public static string ReadTextFile(string directoryPath, string fileName)
        {
            string fullPath = Path.Combine(directoryPath, fileName);

            if (!File.Exists(fullPath))
                return null;

            return File.ReadAllText(fullPath);
        }

        public static CandyKindID PickRandomCandy()
        {
            List<CandyKindID> poll = new();

            List<CandyKindID> L = new()
            {
                CandyKindID.Evil,
            };
            List<CandyKindID> S = new()
            {
                CandyKindID.Black,
                CandyKindID.Pink
            };
            List<CandyKindID> A = new()
            {
                CandyKindID.White,
                CandyKindID.Orange,
                CandyKindID.Gray,
            };
            List<CandyKindID> B = new()
            {
                CandyKindID.Rainbow,
            };
            List<CandyKindID> C = new()
            {
                CandyKindID.Blue,
                CandyKindID.Green,
                CandyKindID.Red,
                CandyKindID.Yellow,
                CandyKindID.Purple,
            };
            List<CandyKindID> D = new()
            {
                CandyKindID.Brown
            };

            foreach (var iL in L)
                poll.Add(iL);

            for (int i = 0; i < 3; i++)
            {
                foreach (var iS in S)
                    poll.Add(iS);
            }

            for (int i = 0; i < 6; i++)
            {
                foreach (var iA in A)
                    poll.Add(iA);
            }

            for (int i = 0; i < 12; i++)
            {
                foreach (var iB in B)
                    poll.Add(iB);
            }

            for (int i = 0; i < 15; i++)
            {
                foreach (var iC in C)
                    poll.Add(iC);
            }

            for (int i = 0; i < 4; i++)
            {
                foreach (var iD in D)
                    poll.Add(iD);
            }

            return poll.GetRandomValue();
        }

        public static void PlaceCandy(CandyKindID candyKindID, Vector3 pos)
        {
            Scp330 scp330 = (Scp330)Item.Create(ItemType.SCP330);
            scp330.AddCandy(candyKindID);
            scp330.RemoveCandy(scp330.Candies.ToList()[0]);

            Player dummy = Player.Get(DummyUtils.SpawnDummy());
            dummy.Role.Set(RoleTypeId.Tutorial);
            dummy.Position = pos;
            dummy.AddItem(scp330);

            scp330.Base.ServerDropCandy(0);

            NetworkServer.Destroy(dummy.GameObject);
        }

        public static void MessageTranslated(string message, string translation, bool isHeld = false, bool isNoisy = true, bool isSubtitles = true)
        {
            if (Main.Instance.Config.EN)
            {
                TranslationManager.TranslatePreserveNewlines(message, "en", translated =>
                {
                    Exiled.API.Features.Cassie.MessageTranslated(message, translated, isHeld, isNoisy, isSubtitles);
                });
            }                
            else    
                Exiled.API.Features.Cassie.MessageTranslated(message, translation, isHeld, isNoisy, isSubtitles);
        }

        public static string InsertBreaks(string input, int maxLineLength)
        {
            if (string.IsNullOrEmpty(input) || maxLineLength <= 0)
                return input;

            StringBuilder sb = new StringBuilder();
            int currentLength = 0;
            string[] words = input.Split(' ');

            foreach (string word in words)
            {
                if (currentLength + word.Length > maxLineLength)
                {
                    if (sb.Length > 0)
                        sb.Append('\n');
                    sb.Append(word);
                    currentLength = word.Length;
                }
                else
                {
                    if (currentLength > 0)
                    {
                        sb.Append(' ');
                        currentLength++;
                    }
                    sb.Append(word);
                    currentLength += word.Length;
                }
            }

            return sb.ToString();
        }
    }
}
