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
using PlayerRoles.PlayableScps;
using ProjectMER.Features;
using ProjectMER.Features.Serializable;
using RGM.API.Components;
using RGM.API.DataBases;
using RGM.API.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using RGM.Modes;
using UnityEngine;
using Random = UnityEngine.Random;
using static RGM.Variables.Variable;

namespace RGM.API.Features
{
    public static class Tools
    {
        /*[Obsolete("이 메서드 대신 Exiled 의 확장 메서드 GetRandomValue를 사용하세요")]
        public static T GetRandomValue<T>(List<T> list)
        {
            System.Random random = new System.Random();
            int index = random.Next(0, list.Count);
            return list[index];
        }*/

        public static List<T> EnumToList<T>()
        {
            Array items = Enum.GetValues(typeof(T));
            List<T> itemList = [];

            foreach (T item in items)
            {
                List<string> list =
                [
                    "None",
                    "Unknown",
                    "Destroyed"
                ];

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
                    var staticModeList = ModeList.Keys.Where(x => ModeList[x].Category == ModeCategory.Public && !ModeVote.ContainsKey(x)).ToList();
                    var mode = staticModeList.GetRandomValue();
                    ModeVote.Add(mode, []);

                    if (mode.GetModeData().Info != ModeInfo.Lock && Random.Range(1, 11) == 1)
                        SubModeVote.Add(ModeList.Keys.Where(x1 =>
                                ModeList[x1].Category != ModeCategory.Private && ModeList[x1].Info != ModeInfo.Lock &&
                                !ModeVote.ContainsKey(x1) &&
                                ModeList.Keys.Where(x2 => x2.GetModeData().Info != ModeInfo.Set).Contains(x1))
                            .GetRandomValue());

                    else
                        SubModeVote.Add(ModeType.None);
                }
                List<List<Transform>> pads = [First, Second, Third, Fourth];

                for (int i = 0; i < 4; i++)
                {
                    Color padColor = ColorUtility.TryParseHtmlString("#" + ModeList[ModeVote.Keys.ToList()[i]].Color, out Color color)
                        ? color
                        : Color.white;

                    SetPrimitiveColor(pads[i], padColor);
                }

                Color randomColor = GetRandomColor(true);

                SetPrimitiveColor(Numbers, randomColor);
                SetPrimitiveColor(RandomColors, randomColor);
                SetLightColor(RandomLights);
                SetRandomPrimitiveColor(Balls);
            }
            catch (Exception e)
            {
                Log.Error($"[RGM] {e}");
            }
        }

        private static void SetPrimitiveColor(List<Transform> transforms, Color color)
        {
            transforms.RemoveAll(transform => transform == null);

            foreach (var transform in transforms)
            {
                if (transform.TryGetComponent<PrimitiveObjectToy>(out var primitive))
                    primitive.NetworkMaterialColor = color;
            }
        }

        private static void SetLightColor(List<Transform> transforms)
        {
            transforms.RemoveAll(transform => transform == null);

            foreach (var transform in transforms)
            {
                if (transform.TryGetComponent<LightSourceToy>(out var light))
                    light.NetworkLightColor = GetRandomColor();
            }
        }

        private static void SetRandomPrimitiveColor(List<Transform> transforms)
        {
            transforms.RemoveAll(transform => transform == null);

            foreach (var transform in transforms)
            {
                if (transform.TryGetComponent<PrimitiveObjectToy>(out var primitive))
                    primitive.NetworkMaterialColor = GetRandomColor(true);
            }
        }

        public static void TeleportToLobby(Player player)
        {
            List<RoleTypeId> humans =
            [
                RoleTypeId.ClassD,
                RoleTypeId.Scientist,
                RoleTypeId.FacilityGuard,
                RoleTypeId.ChaosConscript,
                RoleTypeId.NtfSpecialist,
                RoleTypeId.Tutorial
            ];
            
            player.Role.Set(humans.GetRandomValue());
            player.ClearInventory();

            if (GameObject.Find("LobbyStartPoint") == null) return; 
            
            player.Position = GameObject.Find("LobbyStartPoint").transform.position;

            if (SelectMode != "FightVote") return;

            var fightvoterand = Random.Range(1, 101);

            if (fightvoterand <= 8)
            {
                player.AddItem(ItemType.GunRevolver);
                player.AddAmmo(AmmoType.Ammo44Cal, 120);
            }
            else if (fightvoterand is >= 9 and <= 15)
            {
                player.AddItem(ItemType.GrenadeHE);
            }
            else if (fightvoterand == 44)
            {
                player.AddItem(ItemType.SCP1509);
            }
            else if (fightvoterand == 66)
            {
                player.AddItem(ItemType.GunSCP127);
            }
        }

        public static List<Transform> GetObjectList(string name)
        {
            return GameObject.FindObjectsByType<Transform>(FindObjectsSortMode.InstanceID).Where(t => t.name == name).ToList();
        }

        /// <summary>
        /// Schematic Empty(Transform.name)와 YAML player_spawnpoints(MapEditorObject.Id) 모두에서 스폰 위치를 수집합니다.
        /// exact: "Spot A" / prefix: "Spawn_ClassD" → Spawn_ClassD_1, Spawn_ClassD_2 ...
        /// </summary>
        public static List<Vector3> GetSpawnPositions(params string[] keys)
        {
            var positions = new List<Vector3>();
            if (keys == null || keys.Length == 0)
                return positions;

            var transforms = GameObject.FindObjectsByType<Transform>(FindObjectsSortMode.InstanceID);
            foreach (var key in keys)
            {
                if (string.IsNullOrEmpty(key))
                    continue;

                foreach (var transform in transforms)
                {
                    // Schematic Empty: "Spot A", 복제본 "Spot A (1)", 다중 "Spawn_ClassD_1"
                    if (transform.name == key ||
                        transform.name.StartsWith(key + " (") ||
                        transform.name.StartsWith(key + "_"))
                        positions.Add(transform.position);
                }

                foreach (var map in MapUtils.LoadedMaps.Values)
                {
                    foreach (var obj in map.SpawnedObjects)
                    {
                        // YAML player_spawnpoints: Id == key 또는 Spawn_ClassD_1 형태
                        if (obj.Id == key || obj.Id.StartsWith(key + "_"))
                            positions.Add(obj.transform.position);
                    }
                }
            }

            return positions;
        }

        public static List<string> GetModeDesc(ModeType modeType, ModeType subModeType)
        {
            string color = ModeList[modeType].Color;
            string name = ModeList[modeType].Name;
            string description = ModeList[modeType].Description;
            string detail = ModeList[modeType].Detail;

            string message = Notions.StartModeDescription
                .Replace("{ModeColor}", color)
                .Replace("{CurrentMode}", name)
                .Replace("{CurrentSubMode}", subModeType != ModeType.None ? $"<size=20>추가된 서브 모드 : <color=#{ModeList[subModeType].Color}>{ModeList[subModeType].Name}</color></size>\n" : "")
                .Replace("{ModeDescription}", description)
                .Replace("{ModeInfo}", modeType.GetModeData().Info.ToString());

            return
            [
                message,
                "성공적으로 모드 설명을 불러왔습니다.",
                "해당 모드에 대한 자세한 설명이 없습니다.",
                detail
            ];
        }

        private static Color GetRandomColor(bool transparency = false)
        {
            return !transparency
                ? new Color(Random.value, Random.value, Random.value, 1)
                : new Color(Random.value, Random.value, Random.value);
        }

        public static string GetPlayerInfo(Player player)
        {
            List<string> uc = UsersManager.UsersCache[player.UserId];

            return
$"""


<size=20><b><i>{player.Nickname}</i></b>님의 정보</size>

<size=15>SteamID: {player.UserId}</size>
<size=15>Exp: {uc[0]}</size>
<size=15>랜덤코인: {uc[1]}</size>
<size=15>Cash: ₩{int.Parse(uc[2]):N0}</size>
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

            string GetJoinedInfo(int num)
            {
                return uc[num] == "0" ? "-" : string.Join(", ", uc[num].Split('/'));
            }
        }

        public static void ChangePaint(Player player, string color)
        {
            if (color == "0") return;
            Dictionary<string, string[]> colorDictionary = new Dictionary<string, string[]>
            {
                {"블랙골드", ["brown", "yellow"] },
                {"핫핑크", ["magenta", "pink"] },
                {"레인보우", [.. Datas.Colors.Keys] },
                {"분홍색", ["pink"] },
                {"빨간색", ["red"] },
                {"흰색", ["default"] },
                {"갈색", ["brown"] },
                {"은색", ["silver"] },
                {"밝은 녹색", ["light_green"] },
                {"진홍색", ["crimson"] },
                {"청록색", ["cyan"] },
                {"옥색", ["aqua"] },
                {"진한 분홍색", ["deep_pink"] },
                {"토마토색", ["tomato"] },
                {"노란색", ["yellow"] },
                {"짙은 홍색", ["magenta"] },
                {"푸른 녹색", ["blue_green"] },
                {"주황색", ["orange"] },
                {"라임색", ["lime"] },
                {"초록색", ["green"] },
                {"에메랄드색", ["emerald"] },
                {"카민색", ["carmine"] },
                {"니켈색", ["nickel"] },
                {"박하색", ["mint"] },
                {"군대 녹색", ["army_green"] },
                {"호박색", ["pumpkin"] }
            };

            if (player.GameObject.TryGetComponent(out TagController rtc))
                UnityEngine.Object.Destroy(rtc);

            TagController rtController = player.GameObject.AddComponent<TagController>();
            rtController.Colors = colorDictionary[color];
            rtController.Interval = 1;
        }

        public static void RemovePaint(Player player)
        {
            if (player.GameObject.TryGetComponent(out TagController rtc))
                UnityEngine.Object.Destroy(rtc);

            player.RankColor = null;
        }

        public static IEnumerator<float> SetWinner(List<Player> playerList, int amount)
        {
            if (IsWinnerSelected || Main.Instance.Config.FixedModes.Any()) yield break;

            IsWinnerSelected = true;

            if (Server.PlayerCount >= 10)
            {
                int IsUsingGameChipUsers(Player player)
                {
                    if (!UsingGameChipUsers.Contains(player.UserId)) return 1;
                    PlaySound(player.Transform, "money-soundfx", 2);
                    return 10;
                }

                foreach (var player in playerList.Where(x => !x.IsNonePlayer() && UsersManager.UsersCache.ContainsKey(x.UserId)))
                {
                    UsersManager.UsersCache[player.UserId][0] = (int.Parse(UsersManager.UsersCache[player.UserId][0]) + amount).ToString();
                    UsersManager.UsersCache[player.UserId][1] = (int.Parse(UsersManager.UsersCache[player.UserId][1]) + amount * IsUsingGameChipUsers(player)).ToString();
                }

                UsersManager.SaveUsers();

                WinMessage = $"<size={30 - Math.Round(playerList.Count * 0.5f)}><color=yellow><b>✨</b></color> <b>{string.Join($", ", playerList.Select(x => $"<color={x.Role.Color.ToHex()}><i>{x.DisplayNickname}</i></color>"))}</b>(이)가 <b>{amount}</b> EXP, 랜덤코인을 획득하였습니다";
            }
            else
            {
                WinMessage = $"<size=25>서버 인원이 10명 이하이므로 우승 보상은 지급되지 않습니다.</size>";
            }
        }

        /*
         * SetPersonalWin 구상
         *
         * 
         */
        
        /*
        public static IEnumerator<float> SetPersonalWin(List<Player> playerList, int amount)
        {
           
        }
        */
    
        public static bool TryGetNearestPlayer(this Player player, out Player nearestPlayer, out float radius, List<Player> exceptPlayers = null)
        {
            nearestPlayer = null;
            radius = 99999;

            exceptPlayers ??= [];

            foreach (var near in PlayerManager.List.Where(x => x.IsAlive && x != player && !exceptPlayers.Contains(x)))
            {
                float distance = Vector3.Distance(near.Position, player.Position);

                if (distance < radius)
                {
                    nearestPlayer = near;
                    radius = distance;
                }
            }

            return nearestPlayer != null;
        }

        public static bool TryGetLookPlayer(this Player player, float distance, out Player target, out RaycastHit? raycastHit)
        {
            target = null;
            raycastHit = null;

            if (player?.ReferenceHub?.PlayerCameraReference == null)
                return false;

            var camera = player.ReferenceHub.PlayerCameraReference;
            if (Physics.Raycast(camera.position + camera.forward * 0.2f, camera.forward, out RaycastHit hit, distance) &&
                hit.collider != null &&
                hit.collider.TryGetComponent<IDestructible>(out _) &&
                hit.collider.GetComponentInParent<ReferenceHub>() is { } referenceHub)
            {
                if (Player.TryGet(referenceHub.gameObject, out Player t) && player != t)
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
            targets = [];
            raycastHit = null;

            if (player?.ReferenceHub?.PlayerCameraReference == null)
                return false;

            var camera = player.ReferenceHub.PlayerCameraReference;
            var origin = camera.position + camera.forward * 0.2f;
            var direction = camera.forward;
            RaycastHit[] hits = Physics.RaycastAll(origin, direction, distance);

            foreach (var hit in hits.OrderBy(h => h.distance))
            {
                if (!hit.collider.TryGetComponent<IDestructible>(out _) ||
                    hit.collider.GetComponentInParent<ReferenceHub>() is not { } referenceHub ||
                    !Player.TryGet(referenceHub.gameObject, out Player t) ||
                    player == t ||
                    targets.Contains(t)) continue;
                
                targets.Add(t);
                if (targets.Count == 1)
                    raycastHit = hit;
                if (targets.Count >= count)
                    return true;
            }

            return targets.Count > 0;
        }
        
        /**
         * 화면 내에 비치는 가장 가까운 플레이어를 구합니다.
         * <param name="observer">관찰자 (확장)</param>
         * <param name="nearestPlayer">가장 가까운 플레이어</param>
         * <param name="radius">가장 가까운 플레이어와의 거리</param>
         * <param name="maxDistance">거리</param>
         * <param name="fov">시야각</param>
         * <param name="exceptPlayers">탐색 대상에서 제외할 플레이어</param>
         */
        public static bool TryGetNearestVisiblePlayer(
            this Player observer,
            out Player nearestPlayer,
            out float radius,
            float maxDistance = 30f,
            float fov = 90f,
            List<Player> exceptPlayers = null)
        {
            nearestPlayer = null;
            radius = float.MaxValue;
            exceptPlayers ??= [];
            
            foreach (var near in PlayerManager.List.Where(x => x.IsAlive && x != observer && !exceptPlayers.Contains(x)))
            {
                float distance = Vector3.Distance(near.Position, observer.Position);

                if (distance >= radius ||
                    distance > maxDistance ||
                    !observer.IsLookingAt(near, maxDistance, fov)) continue;

                nearestPlayer = near;
                radius = distance;
            }

            return nearestPlayer != null;
        }
        
        /// <summary>
        /// observer(관찰자)가 target(대상)을 실제로 바라보고 있는지 확인합니다.
        /// </summary>
        /// <param name="observer">보는 사람(확장)</param>
        /// <param name="target">대상 플레이어</param>
        /// <param name="maxDistance">최대 거리 (기본값: 30m)</param>
        /// <param name="fov">시야각 (기본값: 60도)</param>
        public static bool IsLookingAt(this Player observer, Player target, float maxDistance = 30f, float fov = 60f)
        {
            if (observer == null || target == null) 
                throw new NullReferenceException("The object Observer or Target is null.");

            var observerEyePos = observer.CameraTransform.position;
            var targetPos = target.Position;

            float distance = Vector3.Distance(observerEyePos, targetPos);
            if (distance > maxDistance) return false;

            Vector3 directionToTarget = (targetPos - observerEyePos).normalized;
            Vector3 forwardDirection = observer.CameraTransform.forward;

            if (Vector3.Angle(forwardDirection, directionToTarget) > fov) return false;

            List<Vector3> options =
            [
                target.CameraTransform.position,
                targetPos + Vector3.up * 1.0f,
                targetPos + Vector3.up * 0.1f
            ];

            int visionMask = VisionInformation.VisionLayerMask;
            return options.Any(x => !Physics.Linecast(observerEyePos, x, visionMask));
        }
        
        /// <summary>
        /// 특정 위치를 기준으로 방향을 봤을 때 target(대상)을 실제로 바라보고 있는지 확인합니다.
        /// </summary>
        /// <param name="direction">보는 방향</param>
        /// <param name="pos">볼 위치</param>
        /// <param name="target">대상 플레이어</param>
        /// <param name="maxDistance">최대 거리 (기본값: 30m)</param>
        /// <param name="fov">시야각 (기본값: 60도)</param>
        public static bool IsLookingAt(Vector3 direction, Vector3 pos, Player target, float maxDistance = 30f, float fov = 60f)
        {
            if (target == null)
                throw new NullReferenceException("The target is null.");
            
            Vector3 targetPos = target.Position;

            float distance = Vector3.Distance(pos, targetPos);
            if (distance > maxDistance) return false;

            Vector3 directionToTarget = (targetPos - pos).normalized;

            if (Vector3.Angle(direction, directionToTarget) > fov) return false;

            int visionMask = VisionInformation.VisionLayerMask;
            List<Vector3> options =
            [
                target.CameraTransform.position,
                targetPos + Vector3.up * 1.0f,
                targetPos + Vector3.up * 0.1f
            ];

            return options.Any(x => !Physics.Linecast(pos, x, visionMask));
        }

        /// <summary>
        /// observer(관찰자)가 target(대상)을 실제로 바라보고 있는지 확인합니다.
        /// </summary>
        /// <param name="observer">보는 사람(확장)</param>
        /// <param name="position">대상 위치</param>
        /// <param name="maxDistance">최대 거리 (기본값: 30m)</param>
        /// <param name="fov">시야각 (기본값: 60도)</param>
        /// <param name="offset"> 오프셋</param>
        public static bool IsLookingAt(this Player observer, Vector3 position, float maxDistance = 30f, float fov = 60f, float offset = 0f)
        {
            if (observer == null) return false;
            if (!observer.IsAlive) return false;

            List<Vector3> options = 
            [
                Vector3.up,
                Vector3.down,
                Vector3.left,
                Vector3.right,
                Vector3.forward,
                Vector3.back
            ];
            
            Vector3 observerEyePos = observer.CameraTransform.position;

            float distance = Vector3.Distance(observerEyePos, position);
            if (distance > maxDistance) return false;

            Vector3 directionToTarget = (position - observerEyePos).normalized;
            Vector3 forwardDirection = observer.CameraTransform.forward;

            if (Vector3.Angle(forwardDirection, directionToTarget) > fov) return false;
            int visionMask = VisionInformation.VisionLayerMask;

            if (offset != 0 && options.Any(option 
                        => !Physics.Linecast(observerEyePos, position + option * offset, visionMask))) return true;

            return !Physics.Linecast(observerEyePos, position, visionMask);
        }
        // 
        public static bool TryGetLookPoint(Player player, float distance, SurfaceType type, out Vector3 point, float floorNormalThreshold = 0.7f, int layerMask = ~0)
        {
            point = Vector3.zero;

            Vector3 forward = player.CameraTransform.forward;
            Vector3 origin = player.CameraTransform.position + forward * 0.2f;

            if (!Physics.Raycast(origin, forward, out RaycastHit hit, distance, layerMask))
                return false;

            bool isFloor = hit.normal.y >= floorNormalThreshold;
            bool isWall = !isFloor && Mathf.Abs(hit.normal.y) < floorNormalThreshold;

            if ((isFloor && type.HasFlag(SurfaceType.Floor)) ||
                (isWall && type.HasFlag(SurfaceType.Wall)))
            {
                point = hit.point;
                return true;
            }

            return false;
        }

        public static bool TryGetLookFirstPoint(Player player, float distance, SurfaceType type, out Vector3 point, float floorNormalThreshold = 0.7f, int layerMask = ~0) 
        {
            point = Vector3.zero;
            Vector3 forward = player.CameraTransform.forward;
            Vector3 origin = player.CameraTransform.position + forward * 0.2f;

            // 최대 5개의 충돌체를 감지 (적당히 넉넉하게 설정)
            RaycastHit[] hits = new RaycastHit[5];
            int hitCount = Physics.RaycastNonAlloc(origin, forward, hits, distance, layerMask);

            if (hitCount == 0) return false;

            // 거리가 가까운 순서대로 정렬
            Array.Sort(hits, 0, hitCount, Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance)));

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = hits[i];

                // 충돌한 오브젝트가 플레이어 본인이거나 그 하위 오브젝트라면 패스
                if (hit.transform.IsChildOf(player.GameObject.transform) || hit.transform == player.GameObject.transform)
                    continue;

                // 본인이 아닌 첫 번째 충돌체를 찾았으므로 기존 로직 수행
                bool isFloor = hit.normal.y >= floorNormalThreshold;
                bool isWall = !isFloor && Mathf.Abs(hit.normal.y) < floorNormalThreshold;

                if ((isFloor && type.HasFlag(SurfaceType.Floor)) || (isWall && type.HasFlag(SurfaceType.Wall))) 
                {
                    point = hit.point;
                    return true;
                }
        
                // 만약 본인이 아닌 첫 충돌체가 지정한 SurfaceType(예: 바닥)이 아니라면 
                // 뒤에 있는 오브젝트를 투과하지 않고 바로 실패 처리해야 하므로 break
                break;
            }

            return false;
        }

        public static void ForceLookAt(this Player player, Vector3 targetPosition)
        {
            if (player == null || !player.IsAlive) return;

            Vector3 direction = (targetPosition - player.CameraTransform.position).normalized;

            if (direction == Vector3.zero) return;

            Quaternion targetRotation = Quaternion.LookRotation(direction);

            player.Rotation = targetRotation;
        }


        /// <summary>
        /// 특정 위치에서 바라보는 방향으로 Nm만큼 이동한 점를 구합니다.
        /// </summary>
        /// <param name="t">트랜스폼</param>
        /// <param name="moveDistance">갈 거리</param>
        /// <returns>이동한 위치</returns>
        public static Vector3 GetPointForward(Transform t, float moveDistance)
        {
            return t.position + t.forward * moveDistance;
        }
        
        
        public static bool TryInstallMode(ModeType m)
        {
            var modeType = Type.GetType($"RGM.Modes.{m}");

            if (modeType == null &&
                ModeList.ContainsKey(m.GetModeData().Type))
                modeType = Type.GetType($"RGM.Modes.{m.GetModeData().Type}");

            if (modeType == null) return false;
            Mode modeInstance = (Mode)Activator.CreateInstance(modeType);
            modeInstance.Data = ModeList[m];
            EnabledModeList.Add(modeInstance);

            var onEnabledMethod = modeType.GetMethod("OnEnabled");

            if (m.GetModeData().Map != "")
                LoadMap(m.GetModeData().Map);

            onEnabledMethod?.Invoke(modeInstance, null);

            if (!Main.Instance.Config.IsDevMode && !Round.IsEnded && Server.PlayerCount >= 15)
                Webhook.Send($"{m.GetModeData().Name}", ReadTextFile(Path.Combine(Paths.Configs, "RGM"), "Webhook3.txt"));

            return true;
        }

        public static bool UnInstallMode(ModeType m)
        {
            var modeType = Type.GetType($"RGM.Modes.{m}");

            if (modeType == null)
            {
                if (ModeList.ContainsKey(m.GetModeData().Type))
                    modeType = Type.GetType($"RGM.Modes.{modeType}");
            }

            if (modeType != null)
            {
                Mode mode = EnabledModeList.First(x => x.Data.Type == m);

                if (mode == null)
                    return false;

                mode.OnDisabled();

                if (m.GetModeData().Map != "")
                    Server.ExecuteCommand($"/mp unload {m.GetModeData().Map}");

                EnabledModeList.Remove(mode);

                return true;
            }
            return false;
        }

        public static string TryGetUserId(string Name)
        {
            if (Name.Contains("@steam"))
                return Name;

            return Player.TryGet(Name, out Player player) ? player.UserId : null;
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

        public static void MakeSnake(Player player)
        {
            List<ItemType> items =
            [
                ItemType.KeycardFacilityManager,
                ItemType.GunCrossvec,
                ItemType.GunRevolver,
                ItemType.Adrenaline,
                ItemType.SCP500,
                ItemType.ArmorLight
            ];

            List<ItemType> ammoes =
            [
                ItemType.Ammo44cal,
                ItemType.Ammo9x19
            ];

            player.Role.Set(RoleTypeId.Tutorial);
            player.Position = new Vector3(0.125f, 300.9572f, 4.960938f);

            foreach (ItemType item in items)
                player.AddItem(item);

            for (int i = 1; i < 3; i++)
            {
                foreach (var ammo in ammoes)
                    player.AddItem(ammo);
            }
        }

        public static void CallSnakeHand(Player convener, List<Player> playerList)
        {
            foreach (var p in playerList)
                MakeSnake(p);

            if (convener != null)
                convener.AddHint("뱀의 손", $"{playerList.Count}명의 <color=#FE2EF7>동료</color>들이 당신과 함께합니다..", 5f);
        }

        private static string ColorFormat(string cn)
        {
            if (ColorUtility.TryParseHtmlString(cn, out Color color))
                return color.ToHex();

            var cd = Datas.Colors;

            return cd.TryGetValue(cn, out var colorFormat) ? colorFormat : "#FFFFFF";
        }

        public static string BadgeFormat(Player player)
        {
            if (player.RankName != null && !player.BadgeHidden)
                return $"[<color={ColorFormat(player.RankColor)}>{player.RankName}</color>] ";

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

            if (!Physics.Raycast(player.CameraTransform.position + forward, forward, out var raycastHit, _distance))
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

        public static AudioClipPlayback PlayGlobalAudio(string clipName, float volume = 1, bool loop = false, bool destroyOnEnd = true, bool isNoNotice = false)
        {
            string notice = $"로드된 오디오: {clipName}";

            if (!isNoNotice)
                foreach (var player in PlayerManager.List)
                    player.AddBroadcast(10, $"<size=20>{notice}</size>");

            Log.Info(notice);

            return GlobalPlayer.TryPlay(clipName, volume, loop, destroyOnEnd);
        }

        public static MapSchematic LoadMap(string mapName, bool notice = true)
        {
            try
            {
                Log.Info($"로드 시도중인 맵: {mapName}");
                MapSchematic map = MapUtils.GetMapData(mapName);


                if (!MapUtils.LoadedMaps.ContainsKey(mapName))
                    MapUtils.LoadMap(mapName);

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
            catch (FileNotFoundException)
            {
                Log.Error($"맵 '{mapName}'을(를) 찾을 수 없습니다. 로드 실패.");
                return null;
            }
            catch (Exception e)
            {
                Log.Error($"맵 '{mapName}'을(를) 로드하는 중에 오류가 발생했습니다. 로드 실패. {e.Message}");
                return null;
            }
        }

        public static string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new System.Random();
            return new string([
                .. Enumerable.Repeat(chars, length)
                    .Select(s => s[random.Next(s.Length)])
            ]);
        }

        public static string GenerateRandomHexColor()
        {
            int colorValue = Random.Range(0, 0x1000000);
            return $"#{colorValue:X6}";
        }

        public static IEnumerator<float> DoRocket(Player attacker, Player player, float speed, bool isInstantKill = false)
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
                    ExplosiveGrenade grenade = (ExplosiveGrenade)Item.Create(ItemType.GrenadeHE);
                    grenade.FuseTime = 0.5f;
                    grenade.SpawnActive(player.Position, attacker);
                    if (isInstantKill)
                    {
                        ApplyInstantKill.Apply(attacker, player);
                    }
                    else
                    {
                        player.Hit(attacker, player.MaxHealth);
                    }
                }

                yield return Timing.WaitForOneFrame;
            }
        }

        public static LabApi.Features.Wrappers.TextToy CreateText(Vector3 pos, Quaternion rot, string text, float time = 20)
        {
            var textToy = LabApi.Features.Wrappers.TextToy.Create();
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
            AudioPlayer audioPlayer = AudioPlayer.CreateOrGet($"Transform - {transform.position}",
                condition: (ReferenceHub hub) => !MuteBGMPlayers.Contains(Player.Get(hub)), onIntialCreation: p =>
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
            if (!ColorUtility.TryParseHtmlString(hexColor, out var baseColor))
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
            List<CandyKindID> poll = [];
            List<CandyKindID> mythos =
            [
                CandyKindID.Evil
            ];
            
            List<CandyKindID> legendary =
            [
                CandyKindID.Black,
                CandyKindID.Pink
            ];
            
            List<CandyKindID> epic =
            [
                CandyKindID.White,
                CandyKindID.Orange,
                CandyKindID.Gray
            ];
            
            List<CandyKindID> rare =
            [
                CandyKindID.Rainbow
            ];
            
            List<CandyKindID> general =
            [
                CandyKindID.Blue,
                CandyKindID.Green,
                CandyKindID.Red,
                CandyKindID.Yellow,
                CandyKindID.Purple
            ];
            
            // 총 100개의 아이템 카테고리를 가중치에 따라 담고, 그 내에서 랜덤 추출
            for (int i = 0; i < 3; i++) 
                poll.AddRange(mythos); // 3%

            for (int i = 0; i < 6; i++) 
                poll.AddRange(legendary); // 6%

            for (int i = 0; i < 16; i++) 
                poll.AddRange(epic); // 16%

            for (int i = 0; i < 30; i++) 
                poll.AddRange(rare); // 30%

            for (int i = 0; i < 45; i++) 
                poll.AddRange(general); // 45%

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
        [Flags]
        public enum SurfaceType
        {
            None  = 0,
            Floor = 1 << 0, // 1
            Wall  = 1 << 1, // 2
            Both  = Floor | Wall // 3
        }

        public static void CallAutoBan(
            this Player player,
            string reason,
            ModeData data,
            Func<Player, bool> predicate) => CallBan(player, reason, data, BanTime, predicate);

        public static void CallAutoBan(
            this Player player,
            string reason,
            ModeData data,
            TimeSpan banTime,
            Func<Player, bool> predicate) => CallBan(player, reason, data, banTime, predicate);

        private static void CallBan(
            Player player,
            string reason,
            ModeData data,
            TimeSpan banTime,
            Func<Player, bool> predicate)
        {
            if (player.IsNPC || player.IsHost || !predicate(player)) return;
            
            player.Ban(banTime, 
                $"""
                 -----[자동제재({data.Name})]----- 
                 사유: {reason} 
                 제재 해제일: {banTime.Days}일 {banTime.Hours}시간 {banTime.Minutes}분 후
                  제재 소명을 통하여 더 빠르게 해제하실 수 있습니다.
                 """);
        }
    }
}
