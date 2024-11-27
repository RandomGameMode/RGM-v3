using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Exiled.API.Features;
using Exiled.Events.Commands.Reload;
using MEC;
using MultiBroadcast.API;
using PlayerRoles;
using RGM.API.Components;
using RGM.API.DataBases;
using UnityEngine;
using DiscordInteraction.Discord;

using static RGM.Variables.ServerManagers;

namespace RGM.API.Features
{
    public class Tools
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
                if (!item.ToString().Contains("None"))
                    itemList.Add(item);
            }

            return itemList;
        }

        public static List<Transform> GetObjectList(string Name)
        {
            return GameObject.FindObjectsOfType<Transform>().Where(t => t.name == Name).ToList();
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
                return new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);

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


<size=30><b>{player.Nickname}</b>님의 정보</size>

SteamID: {player.UserId}
Exp: {uc[0]}
RP: {uc[1]}
<i>Cash</i>: ₩{int.Parse(uc[2]).ToString("N0")}

보유한 킬 이펙트: {GetJoinedInfo(3)}
장착한 킬 이펙트: {(uc[4] == "0" ? "-" : uc[4])}
<size=15>{(uc[4] == "0" ? "'.킬이펙트 <킬이펙트 이름>' 명령어를 사용하여 킬 이펙트를 장착할 수 있습니다." : KillEffects[uc[4]])}</size>

보유한 커스텀: {GetJoinedInfo(7)}
커스텀 닉네임: {(uc[5] == "0" ? "-" : uc[5])}
<size=15>{(uc[5] == "0" ? "'.닉네임 <텍스트>' 명령어를 사용하여 커스텀 닉네임을 설정할 수 있습니다." : "이 멋진 닉네임이 모두에게 보입니다!")}</size>
커스텀 인포: {(uc[6] == "0" ? "-" : uc[6])}
<size=15>{(uc[6] == "0" ? "'.인포 <텍스트>' 명령어를 사용하여 커스텀 인포를 설정할 수 있습니다." : "이 아름다운 언포가 모두에게 보입니다!")}</size>

보유한 페인트: {GetJoinedInfo(8)}
장착한 페인트: {(uc[9] == "0" ? "-" : uc[9])}
<size=15>{(uc[9] == "0" ? "'.페인트 <페인트 이름>' 명령어를 사용하여 페인트를 장착할 수 있습니다." : Paints[uc[9]])}</size>
""";
        }

        public static void ChangePaint(Player player, string Color)
        {
            if (player.Group != null && UsersManager.UsersCache[player.UserId][9] != "0")
            {
                Dictionary<string, string[]> ColorDictionary = new Dictionary<string, string[]>()
                {
                    {"블랙골드", new string[] { "brown", "yellow" } },
                    {"핫핑크", new string[] { "magenta", "pink" } },
                    {"레인보우", Datas.Colors.Keys.ToArray() }
                };

                TagController rtController = player.GameObject.AddComponent<TagController>();
                rtController.Colors = ColorDictionary[Color];
                rtController.Interval = 1;
            }
        }

        public static void RemovePaint(Player player)
        {
            if (player.GameObject.TryGetComponent<TagController>(out TagController rtc))
                UnityEngine.Object.Destroy(rtc);
        }

        public static bool TryGetNearestPlayer(Player player, out Player nearestPlayer, out float radius, List<Player> exceptPlayers = null)
        {
            nearestPlayer = null;
            radius = 99999;

            if (exceptPlayers == null)
                exceptPlayers = new List<Player>();

            foreach (var near in Player.List.Where(x => x.IsAlive && x != player && !exceptPlayers.Contains(x)))
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

        public static bool TryGetLookPlayer(Player player, float Distance, out Player target)
        {
            target = null;

            if (Physics.Raycast(player.ReferenceHub.PlayerCameraReference.position + player.ReferenceHub.PlayerCameraReference.forward * 0.2f, player.ReferenceHub.PlayerCameraReference.forward, out RaycastHit hit, Distance, InventorySystem.Items.Firearms.Modules.BuckshotHitreg.HitregMask) &&
                    hit.collider.TryGetComponent<IDestructible>(out IDestructible destructible))
            {
                if (Player.TryGet(hit.collider.GetComponentInParent<ReferenceHub>(), out Player t) && player != t)
                {
                    target = t;

                    return true;
                }
            }

            return false;
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
                var modeInstance = Activator.CreateInstance(modeType);
                var onEnabledMethod = modeType.GetMethod("OnEnabled");
                onEnabledMethod?.Invoke(modeInstance, null);

                EnabledModeList.Add(ModeType);

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

        public static Player SpawnDJ(string name, RoleTypeId roleTypeId, Vector3 position, string sn = null)
        {
            ReferenceHub dj = GGUtils.Gtool.Spawn(roleTypeId, position);

            if (sn == null)
                sn = $"{UnityEngine.Random.value}";

            Dictionary<ReferenceHub, string> register = new Dictionary<ReferenceHub, string>()
            {
                { dj, sn }
            };

            foreach (var reg in register)
            {
                try
                {
                    GGUtils.Gtool.Register(reg.Key, reg.Value);
                }
                catch
                {
                }
            }

            GGUtils.Gtool.PlayerGet(sn).DisplayNickname = name;

            return Player.Get(dj);
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
            int RequiredCount = Player.List.Count / 2;
            bool IsSuccess = false;

            Webhook.Send($"🗳️ **버그 투표**ㅣ{host.Nickname}에 의해 시작됨");

            for (int i = 1; i<21; i++)
            {
                if (BugVotePlayers.Count >= RequiredCount)
                {
                    IsSuccess = true;
                    break;
                }

                foreach (var player in Player.List)
                    player.ShowHint($"<size=25>{host.DisplayNickname}(이)가 <b><color=#FFBF00>버그 투표</color></b>를 개설하였습니다.\n라운드를 강제로 종료해야 한다면 <b>.찬성</b> 명령어를 입력하세요.</size>\n이유 : {reason}\n<size=20>투표 종료까지 {21 - i}초 남음 ({BugVotePlayers.Count}/{RequiredCount})</size>", 1.2f);

                yield return Timing.WaitForSeconds(1);
            }

            if (IsSuccess)
            {
                foreach (var player in Player.List)
                    player.AddBroadcast(5, $"버그 투표가 <b><color=#9AFE2E>가결</color></b>되었습니다. 곧 서버가 재시작됩니다.");

                Webhook.Send($"🗳️ **버그 투표**ㅣ✅ 가결됨 (투표자: {string.Join(", ", BugVotePlayers.Select(x => x.Nickname))})");
                yield return Timing.WaitForSeconds(5);

                Server.ExecuteCommand($"sr");
            }
            else
            {
                foreach (var player in Player.List)
                    player.AddBroadcast(5, $"버그 투표가 <b><color=#FE2E2E>부결</color></b>되었습니다.");

                Webhook.Send($"🗳️ **버그 투표**ㅣ❌ 부결됨 (투표자: {string.Join(", ", BugVotePlayers.Select(x => x.Nickname))})");
            }

            IsBugVoteProcessing = false;
            BugVotePlayers.Clear();
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
                p.Position = new Vector3(-0.08203125f, 1000.96f, 6.828125f);

                foreach (ItemType Item in Items)
                    p.AddItem(Item);

                for (int i = 1; i < 3; i++)
                {
                    foreach (var Ammo in Ammos)
                        p.AddItem(Ammo);
                }
            }

            if (Convener != null)
                Convener.ShowHint($"<i>{SnakeHands.Count()}명의 <color=#FE2EF7>동료</color>들이 당신과 함께합니다..</i>", 5f);
        }
    }
}
