using AdminToys;
using CustomPlayerEffects;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using InventorySystem.Items.Usables.Scp330;
using MEC;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.FirstPersonControl.Thirdperson;
using PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers.OverlayAnims;
using PlayerStatsSystem;
using ProjectMER.Features;
using ProjectMER.Features.Objects;
using RGM.API.Interfaces;
using RGM.Modes.SubClass;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static RGM.Variables.Variable;
using Random = System.Random;

namespace RGM.API.Features
{
    ///
    ///<summary>플레이어 관련 작업을 처리합니다</summary>
     ///
    public static class PlayerManager
    {
        /// <summary>
        /// <c>AddRandomItem()</c> 메서드 사용 시 해당 유저의 <b>무작위 값을 저장하는 역할</b>을 수행합니다.
        /// </summary>
        private static readonly Dictionary<Player, List<byte>> PlayerRandomValueCount = [];

        /// <summary>
        /// <c>Player</c>의 리스트를 가져옵니다.
        /// <c>NonePlayer</c>에 등록된 <c>Player</c> 객체는 제외됩니다.
        /// </summary>
        /// <returns><c>Player</c>의 리스트를 반환합니다.</returns>
        public static List<Player> List
        {
            get
            {
                return Main.Instance.Config.FixedModes.Any()
                    ? Player.List.ToList()
                    : Player.List.Where(x => !x.IsNPC || (!x.IsDnd() && !x.IsNonePlayer())).ToList();
            }
        }

        /// <summary>
        /// 해당 유저의 <b>번역기 사용 여부</b>를 반환합니다.
        /// </summary>
        /// <param name="player">대상 <c>Player</c>입니다. <code>
        /// 
        /// if(player.IsUsingTranslator()) 
        /// </code>으로 활용할 수 있습니다.</param>
        /// <returns>번역기 사용 시 <b>True</b>를 반환합니다.</returns>
        public static bool IsUsingTranslator(this Player player)
        {
            if (player.IsNPC) return false;
            return !Main.Instance.Config.FixedModes.Any() && TranslatorPlayers[player] != "ko";
        }

        /// <summary>
        /// <c>Player</c>의 방해 금지 활성 여부를 반환합니다.
        /// </summary>
        /// <param name="player">대상 <c>Player</c>입니다. <b>NPC(null)은 제외</b>됩니다.</param>
        /// <returns>방해 금지 활성 시 <b>True</b>를 반환합니다.</returns>
        public static bool IsDnd(this Player player)
        {
            if (player == null || player.IsNPC)
                return true;

            if (Main.Instance.Config.FixedModes.Any() ||
                string.IsNullOrEmpty(player.UserId) ||
                !UsersManager.UsersCache.TryGetValue(player.UserId, out List<string> userData) ||
                userData.Count <= 23)
            {
                return false;
            }

            return userData[23] is "1";
        }

        /// <summary>
        /// <c>Player</c>가 NonePlayer인지 확인합니다.
        /// </summary>
        /// <param name="player">대상 <c>Player</c>입니다.</param>
        /// <returns>NonePlayer일 경우 <b>True</b>를 반환합니다.</returns>
        public static bool IsNonePlayer(this Player player) 
            => NonePlayer.Players.Contains(player);

        /// <summary>
        /// <c>Player</c>가 SCP 역할인지 확인합니다.
        /// </summary>
        /// <param name="player">대상 <c>Player</c>입니다.</param>
        /// <returns>SCP 역할일 경우 <b>True</b>를 반환합니다.</returns>
        public static bool IsScpRole(this Player player)
        {
            return player.Role.Type.IsScpRole();
        }

        /// <summary>
        /// 해당 <c>RoleTypeId</c>이(가) SCP 역할인지 확인합니다.
        /// </summary>
        /// <param name="roleTypeId">대상 <c>RoleTypeId</c>입니다.</param>
        /// <returns>SCP 역할일 경우 <b>True</b>를 반환합니다.</returns>
        public static bool IsScpRole(this RoleTypeId roleTypeId)
        {
            return roleTypeId.IsScp() || roleTypeId.ToString().Contains("Flamingo");
        }
        
        ///<summary>
        /// <c>Player</c>의 확장 메서드로서 플레이어를 세팅하는 메서드를 추가합니다
        /// </summary>
        ///<param name="player">대상 플레이어입니다.</param>
        ///
        public static void Setup(this Player player)
        {
            TranslatorPlayers.Add(player, "ko");
            Chats.Add(player, new List<string>());

            var text = Tools.CreateText(Vector3.zero, new Quaternion(0, 180, 0, 0), "", 0);
            text.Parent = player.Transform;
            Texts.Add(player, text);

            OnGround.Add(player.UserId, 5);

            if (!PlayersAudio.ContainsKey(player))
            {
                AudioPlayer audioPlayer = AudioPlayer.CreateOrGet($"Player - {player.UserId}", condition: (hub) =>
                    {
                        Player ply = Player.Get(hub);

                        return (ply == player && !MuteBGMPlayers.Contains(ply)) ||
                               (player.CurrentSpectatingPlayers.Contains(ply) && !MuteBGMPlayers.Contains(ply));
                    }
                    , onIntialCreation: p =>
                    {
                        Speaker speaker = p.AddSpeaker("Main", isSpatial: false, minDistance: 0, maxDistance: 5000);
                    });

                PlayersAudio.Add(player, audioPlayer);
            }

            if (!PlayersReport.ContainsKey(player.UserId))
            {
                PlayersReport.Add(player.UserId, new PlayerReport()
                {
                    Kill = 0,
                    Death = 0,
                    Revive = 0,
                    KillScp = 0,
                    KillHuman = 0,
                    Damage = 0,
                    LastDeath = DateTime.MinValue
                });
            }
        }

        /// <summary>
        /// 대상 <c>Player</c>에게 효과를 추가합니다.
        /// </summary>
        /// <param name="player">대상 <c>Player</c>입니다.</param>
        /// <param name="type">대상 <c>EffectType</c>입니다.</param>
        /// <param name="intensity">대상 <c>EffectType</c>의 강도입니다.</param>
        /// <param name="duration">대상 <c>EffectType</c>의 지속 시간입니다.</param>
        /// <param name="addDuration">지속시간입니다.</param>
        public static void AddEffect(this Player player, EffectType type, int intensity, float duration = 0f,
            bool addDuration = false)
        {
            if (!EffectIntensities.ContainsKey(player))
                EffectIntensities[player] = new Dictionary<EffectType, int>();

            if (!EffectIntensities[player].ContainsKey(type))
                EffectIntensities[player][type] = 0;

            EffectIntensities[player][type] += intensity;

            const byte maxIntensity = 255;
            var applyIntensity = Convert.ToByte(Math.Min(EffectIntensities[player][type], maxIntensity));

            var effect = player.ActiveEffects.FirstOrDefault(x => x.GetEffectType() == type);
            float newDuration = effect != null && addDuration
                ? effect.Duration + duration
                : Math.Max(effect?.Duration ?? 0, duration);

            player.DisableEffect(type);
            player.EnableEffect(type, applyIntensity, duration == 0f ? 0f : newDuration);

            if (duration > 0f)
            {
                Timing.CallDelayed(duration, () =>
                {
                    if (EffectIntensities.ContainsKey(player) && EffectIntensities[player].ContainsKey(type))
                    {
                        EffectIntensities[player][type] -= intensity;
                        if (EffectIntensities[player][type] <= 0)
                        {
                            EffectIntensities[player].Remove(type);
                            player.DisableEffect(type);
                        }
                        else
                        {
                            var newApplyIntensity = Convert.ToByte(Math.Min(EffectIntensities[player][type], 255));
                            player.EnableEffect(type, newApplyIntensity);
                        }
                    }
                });
            }
        }

        /// <summary>
        /// 해당 <c>Player</c>의 <c>EffectType</c>을 제거합니다.
        /// </summary>
        /// <param name="player">대상 <c>Player</c>입니다.</param>
        /// <param name="type">해당되는 <c>EffectType</c>입니다.</param>
        /// <param name="intensity">해당 효과의 강도입니다.</param>
        public static void RemoveEffect(this Player player, EffectType type, int intensity)
        {
            if (!EffectIntensities.ContainsKey(player) || !EffectIntensities[player].ContainsKey(type))
                return;

            EffectIntensities[player][type] -= intensity;
            if (EffectIntensities[player][type] <= 0)
            {
                EffectIntensities[player].Remove(type);
                player.DisableEffect(type);
            }
            else
            {
                byte applyIntensity = (byte)Math.Min(EffectIntensities[player][type], 255);
                player.EnableEffect(type, applyIntensity);
            }
        }

        /// <summary>
        /// 대상 <c>Player</c>의 모든 <c>EffectType</c>을 제거합니다.
        /// </summary>
        /// <param name="player">대상 <c>Player</c>입니다.</param>
        public static void ClearEffect(this Player player)
        {
            foreach (var effect in player.ActiveEffects.ToList())
                player.DisableEffect(effect.GetEffectType());

            if (EffectIntensities.ContainsKey(player))
                EffectIntensities[player].Clear();
        }

        public static void Hit(this Player player, Player attacker, float damage)
        {
            attacker.ShowHitMarker(damage / 10);
            player.Hurt(new DisruptorDamageHandler(new InventorySystem.Items.Firearms.ShotEvents.DisruptorShotEvent(
                    InventorySystem.Items.ItemIdentifier.None,
                    attacker.Footprint,
                    InventorySystem.Items.Firearms.Modules.DisruptorActionModule.FiringState.FiringRapid),
                player.Position,
                damage));
        }

        public static bool HasKeycardPermission(this Player player, KeycardPermissions permissions,
            bool requiresAllPermissions = false)
        {
            if (player.IsEffectActive<AmnesiaVision>())
                return false;

            return requiresAllPermissions
                ? player.Items.Any(item => item is Keycard keycard && keycard.Permissions.HasFlag(permissions))
                : player.Items.Any(item => item is Keycard keycard && (keycard.Permissions & permissions) != 0);
        }

        public static void AddCustomKeycard(this Player player, string info)
        {
            Server.ExecuteCommand($"/ckeycard {player.Id} {info}");

            Server.ExecuteCommand(
                $"/ckeycard 1 KeycardCustomSite02 커스텀_키카드 0 0 0 #56C491 #C1CE79 jumpscare-Scp939 #891064 AudioClips 100");
        }

        public static bool AddBadge(this string userId, string args, out string response,
            ArraySegment<string>? arguments = null)
        {
            if (arguments is { Count: < 2 })
            {
                response = "칭호추가 <player> <badge name>";
                return false;
            }

            if (Badges.ContainsKey(args))
            {
                List<string> uc = UsersManager.UsersCache[userId];

                if (uc[10] == "0")
                {
                    uc[10] = args;
                    UsersManager.UsersCache[userId] = uc;
                    response = "Successfully add badge.";

                    UsersManager.SaveUsers();
                    return true;
                }

                if (uc[10].Split('/').Contains(args))
                {
                    response = "This player already have this badge.";
                    return false;
                }

                uc[10] += $"/{args}";
                UsersManager.UsersCache[userId] = uc;
                response = "Successfully add badge.";

                UsersManager.SaveUsers();
                return true;
            }

            response = "This badge is not exist.";
            return false;
        }

        public static bool AddCustom(this string userId, string args, out string response,
            ArraySegment<string>? arguments = null)
        {
            if (arguments is { Count: < 2 })
            {
                response = "커스텀추가 <player> <custom feature name>";
                return false;
            }

            if (Customizations.ContainsKey(args))
            {
                List<string> uc = UsersManager.UsersCache[userId];

                if (uc[7] == "0")
                {
                    uc[7] = args;
                    UsersManager.UsersCache[userId] = uc;
                    response = "Successfully add custom feature.";

                    UsersManager.SaveUsers();
                    return true;
                }

                if (uc[7].Split('/').Contains(args))
                {
                    response = "This player already have this custom feature.";
                    return false;
                }

                uc[7] += $"/{args}";
                UsersManager.UsersCache[userId] = uc;
                response = "Successfully add custom feature.";

                UsersManager.SaveUsers();
                return true;
            }

            response = "This custom feature is not exist.";
            return false;
        }

        public static bool AddKillEffect(this string userId, string args, out string response,
            ArraySegment<string>? arguments = null)
        {
            if (arguments is { Count: < 2 })
            {
                response = "킬이펙트추가 <player> <kill effect name>";
                return false;
            }

            if (KillEffects.ContainsKey(args))
            {
                List<string> uc = UsersManager.UsersCache[userId];

                if (uc[3] == "0")
                {
                    uc[3] = args;
                    UsersManager.UsersCache[userId] = uc;
                    response = "Successfully add kill effect.";

                    UsersManager.SaveUsers();
                    return true;
                }

                if (uc[3].Split('/').Contains(args))
                {
                    response = "This player already have this kill effect.";
                    return false;
                }

                uc[3] += $"/{args}";
                UsersManager.UsersCache[userId] = uc;
                response = "Successfully add kill effect.";

                UsersManager.SaveUsers();
                return true;
            }

            response = "This kill effect is not exist.";
            return false;
        }

        public static bool AddPaint(this string userId, string args, out string response,
            ArraySegment<string>? arguments = null)
        {
            if (arguments is { Count: < 2 })
            {
                response = "페인트추가 <player> <paint name>";
                return false;
            }

            if (Paints.ContainsKey(args))
            {
                List<string> uc = UsersManager.UsersCache[userId];

                if (uc[8] == "0")
                {
                    uc[8] = args;
                    UsersManager.UsersCache[userId] = uc;
                    response = "Successfully add paint.";

                    UsersManager.SaveUsers();
                    return true;
                }

                if (uc[8].Split('/').Contains(args))
                {
                    response = "This player already have this paint.";
                    return false;
                }

                uc[8] += $"/{args}";
                UsersManager.UsersCache[userId] = uc;
                response = "Successfully add paint.";

                UsersManager.SaveUsers();
                return true;
            }

            response = "This paint is not exist.";
            return false;
        }

        public static bool SetCash(this string userId, int cash, out string response, bool result = true)
        {
            List<string> uc = UsersManager.UsersCache[userId];

            if (result)
            {
                if (cash < 0)
                {
                    response = "0 upper";
                    return false;
                }

                uc[2] = cash.ToString();
                UsersManager.UsersCache[userId] = uc;
                response = "successfully set up Cash.";

                UsersManager.SaveUsers();
            }
            else
            {
                response = $"{uc[2]}";
            }

            return true;
        }

        public static bool SetRC(this string userId, int rc, out string response, bool result = true)
        {
            List<string> uc = UsersManager.UsersCache[userId];

            if (result)
            {
                if (rc < 0)
                {
                    response = "0 upper.";
                    return false;
                }

                uc[1] = rc.ToString();
                UsersManager.UsersCache[userId] = uc;
                response = "successfully set up Random Coin.";

                UsersManager.SaveUsers();
                return true;
            }

            response = $"{uc[1]}";
            return false;
        }

        public static bool SetExp(this string userId, int rc, out string response, bool result = true)
        {
            List<string> uc = UsersManager.UsersCache[userId];

            if (result)
            {
                if (rc < 0)
                {
                    response = "0 upper.";
                    return false;
                }

                uc[0] = rc.ToString();
                UsersManager.UsersCache[userId] = uc;
                response = "successfully set up Exp.";

                UsersManager.SaveUsers();
                return true;
            }

            response = $"{uc[0]}";
            return false;
        }

        public static bool AddProduct(this string userId, string args, out string response,
            ArraySegment<string>? arguments = null)
        {
            if (arguments is { Count: < 2 })
            {
                response = "아이템추가 <player> <item name>";
                return false;
            }

            if (Products.Select(x => x.Name).Contains(args))
            {
                List<string> uc = UsersManager.UsersCache[userId];

                if (uc[18] == "0")
                {
                    uc[18] = args;
                    UsersManager.UsersCache[userId] = uc;
                    response = "Successfully add item.";

                    UsersManager.SaveUsers();
                    return true;
                }

                uc[18] += $"/{args}";
                UsersManager.UsersCache[userId] = uc;
                response = "Successfully add item.";

                UsersManager.SaveUsers();
                return true;
            }

            response = "This item is not exist.";
            return false;
        }

        public static bool AddIcon(this string userId, string args, out string response,
            ArraySegment<string>? arguments = null)
        {
            if (arguments is { Count: < 2 })
            {
                response = "아이콘추가 <player> <icon name>";
                return false;
            }

            if (Icons.ContainsKey(args))
            {
                List<string> uc = UsersManager.UsersCache[userId];

                if (uc[24] == "0")
                {
                    uc[24] = args;
                    UsersManager.UsersCache[userId] = uc;
                    response = "Successfully add icon.";

                    UsersManager.SaveUsers();
                    return true;
                }

                if (uc[24].Split('/').Contains(args))
                {
                    response = "This player already have this icon.";
                    return false;
                }

                uc[24] += $"/{args}";
                UsersManager.UsersCache[userId] = uc;
                response = "Successfully add icon.";

                UsersManager.SaveUsers();
                return true;
            }

            response = "This icon is not exist.";
            return false;
        }

        public static bool AddWarn(this string userId, string args, out string response,
            ArraySegment<string>? arguments = null)
        {
            List<string> uc = UsersManager.UsersCache[userId];

            if (args == "")
            {
                response = $"{string.Join("\n", uc[22].Split('/'))}\n-";
                return true;
            }

            if (uc[22] == "0")
            {
                uc[22] = args;
                UsersManager.UsersCache[userId] = uc;
                response = $"Successfully add warn.\n\n{string.Join("\n", uc[22].Split('/'))}\n-";

                UsersManager.SaveUsers();
                return true;
            }
            else
            {
                uc[22] += $"/{args}";
                UsersManager.UsersCache[userId] = uc;
                response = $"Successfully add warn.\n\n{string.Join("\n", uc[22].Split('/'))}\n-";

                UsersManager.SaveUsers();
                return true;
            }
        }

        public static void AddCandy(this Player player, CandyKindID candyKindID)
        {
            var existing = player.Items
                .Where(x => x.Type == ItemType.SCP330)
                .OfType<Scp330>()
                .FirstOrDefault(scp => scp.Candies.Count < 6);

            if (existing != null)
            {
                existing.AddCandy(candyKindID);
                return;
            }

            Scp330 scp330 = (Scp330)Item.Create(ItemType.SCP330);
            scp330.AddCandy(candyKindID);
            scp330.RemoveCandy(scp330.Candies.ToList()[0]);
            player.AddItem(scp330);
        }

        public static void Push(this Player player, Player target, float distance = 5, float height = 1)
        {
            Vector3 horizontalDirection = target.Position - player.Position;
            horizontalDirection.y = 0;
            horizontalDirection = horizontalDirection.normalized;

            Vector3 throwVector = horizontalDirection * distance;
            throwVector.y = height;

            var hits = Physics.RaycastAll(
                player.ReferenceHub.PlayerCameraReference.position +
                player.ReferenceHub.PlayerCameraReference.forward * 0.2f,
                player.ReferenceHub.PlayerCameraReference.forward,
                distance);

            RaycastHit? validHit = null;
            foreach (var h in hits.OrderBy(hit => hit.distance))
            {
                if (h.collider.TryGetComponent<IDestructible>(out var destructible)) continue;

                validHit = h;
                break;
            }

            if (validHit.HasValue)
                target.Position = validHit.Value.point;
            else
                target.Position = player.Position + throwVector;
        }

        public static void ApplyGodMode(this Player player, float time)
        {
            GodModePlayers.Add(player);

            Timing.CallDelayed(time, () =>
            {
                if (GodModePlayers.Contains(player))
                    GodModePlayers.Remove(player);
            });
        }

        public static void Grab(this Player player)
        {
            if (player.ReferenceHub.roleManager.CurrentRole is not IFpcRole currentRole ||
                currentRole.FpcModule.CharacterModelInstance is not AnimatedCharacterModel
                    characterModelInstance ||
                !characterModelInstance.TryGetSubcontroller(out OverlayAnimationsSubcontroller subcontroller))
                return;

            subcontroller._overlayAnimations[1].OnStarted();
            subcontroller._overlayAnimations[1].SendRpc();
        }

        public static Item AddRandomItem(this Player player)
        {
            if (player.IsNonePlayer() || player.IsNPC)
                return Item.Create(ItemType.None);
            
            List<ItemType> poll = [];
            // 추후 constant로 옮길 예정
            List<ItemType> mythos =
            [
                ItemType.GunSCP127,
                ItemType.SCP1509,
                ItemType.SCP268,
                ItemType.SCP1344,
                ItemType.AntiSCP207,

                // 무기
                ItemType.Jailbird,
                ItemType.MicroHID,
                ItemType.ParticleDisruptor
            ];
            List<ItemType> legendary =
            [
                ItemType.SCP018,
                ItemType.SCP1853,
                ItemType.SCP500,

                // 카드
                ItemType.KeycardO5,
                ItemType.KeycardMTFCaptain,
                ItemType.KeycardChaosInsurgency,

                // 무기
                ItemType.GunLogicer,
                ItemType.GunFRMG0,
                ItemType.GunE11SR,
                ItemType.GunAK
            ];
            List<ItemType> epic =
            [
                ItemType.SCP207,
                ItemType.SCP244a,
                ItemType.SCP244b,
                ItemType.SCP1576,
                ItemType.SCP2176,

                // 카드
                ItemType.KeycardMTFOperative,
                ItemType.KeycardMTFPrivate,
                ItemType.KeycardFacilityManager,

                // 무기
                ItemType.GunRevolver,
                ItemType.GunShotgun,
                ItemType.GunFSP9,

                // 치료
                ItemType.Adrenaline,

                // 방탄복
                ItemType.ArmorHeavy,

                // 기타
                ItemType.GrenadeHE
            ];
            List<ItemType> rare =
            [
                ItemType.SCP330,

                // 카드
                ItemType.KeycardZoneManager,
                ItemType.KeycardGuard,
                ItemType.KeycardContainmentEngineer,

                // 무기
                ItemType.GunCrossvec,
                ItemType.GunCom45,
                ItemType.GunA7,

                // 치료
                ItemType.Medkit,

                // 방탄복
                ItemType.ArmorCombat,

                // 기타
                ItemType.Radio,
                ItemType.GrenadeFlash
            ];
            List<ItemType> general =
            [
                ItemType.KeycardJanitor,
                ItemType.KeycardScientist,
                ItemType.KeycardResearchCoordinator,
                ItemType.SurfaceAccessPass,

                // 무기
                ItemType.GunCOM18,
                ItemType.GunCOM15,

                // 치료
                ItemType.Painkillers,

                // 방탄복
                ItemType.ArmorLight,

                // 기타
                ItemType.Flashlight,
                ItemType.Lantern,
                ItemType.Coin
            ];

            if (!PlayerRandomValueCount.ContainsKey(player))
                PlayerRandomValueCount.Add(player, [0, 0]);

            if (PlayerRandomValueCount[player][0] >= 79)
            {
                Light(Color.red);
                PlayerRandomValueCount[player][0] = 0;

                return player.AddItem(mythos.GetRandomValue());
            }

            if (PlayerRandomValueCount[player][1] >= 9)
            {
                Light(Color.yellow);
                PlayerRandomValueCount[player][1] = 0;

                return player.AddItem(legendary.GetRandomValue());
            }

            // 총 100개의 아이템 카테고리를 가중치에 따라 담고, 그 내에서 랜덤 추출
            for (int i = 0; i < 2; i++) 
                poll.AddRange(mythos); // 2%

            for (int i = 0; i < 6; i++) 
                poll.AddRange(legendary); // 6%

            for (int i = 0; i < 16; i++) 
                poll.AddRange(epic); // 16%

            for (int i = 0; i < 31; i++) 
                poll.AddRange(rare); // 31%

            for (int i = 0; i < 45; i++) 
                poll.AddRange(general); // 45%
            

            var item = player.AddItem(poll.GetRandomValue());
            PlayerRandomValueCount[player][0]++;
            PlayerRandomValueCount[player][1]++;

            if (mythos.Contains(item.Type))
            {
                Tools.PlaySound(player.Transform, "L 등급", 4);
                Light(Color.red);
                PlayerRandomValueCount[player][0] = 0;
            }

            if (legendary.Contains(item.Type))
            {
                Tools.PlaySound(player.Transform, "S 등급", 2);
                Light(Color.yellow);
                PlayerRandomValueCount[player][1] = 0;
            }

            if (epic.Contains(item.Type))
                Light(Color.magenta);

            return item;


            void Light(Color color)
            {
                try
                {
                    SchematicObject schematic = ObjectSpawner.SpawnSchematic("Light", Vector3.zero);
                    LightSourceToy light = schematic.GetComponentsInChildren<LightSourceToy>().First();

                    schematic.transform.parent = player.Transform;
                    schematic.transform.localPosition = Vector3.zero;

                    light.NetworkLightColor = color;
                    light.NetworkLightRange = 25;
                    light.NetworkLightIntensity = 8;

                    Timing.CallDelayed(3, schematic.Destroy);
                }
                catch (NullReferenceException)
                {
                    Log.Warn("Failure to fetch object 'light'.");
                }
            }
        }

        public static void AddRandomCandy(this Player player)
            => player.AddCandy(Tools.PickRandomCandy());

        public static void AddBroadcast(this Player player, ushort duration, string message, byte priority = 0,
            string tag = "")
        {
            message = message.Replace("<color=#855439>*</color>", "");
            
            
            if (TranslatorPlayers.ContainsKey(player) && 
                player.IsUsingTranslator() && 
                tag != "chat" && tag != "kill")
                
                TranslationManager.TranslatePreserveNewlines(message, TranslatorPlayers[player], translated
                    => MultiBroadcast.API.BroadcastExtensions.AddBroadcast(player, duration, translated, priority,
                        tag));
            else
                MultiBroadcast.API.BroadcastExtensions.AddBroadcast(player, duration, message, priority, tag);
        }

        public static void EditBroadcast(this Player player, string text, string tag)
        {
            if (player.IsUsingTranslator())
                TranslationManager.TranslatePreserveNewlines(text, TranslatorPlayers[player], translated
                    => MultiBroadcast.API.BroadcastExtensions.EditBroadcast(player, translated, tag));
            else
                MultiBroadcast.API.BroadcastExtensions.EditBroadcast(player, text, tag);
        }

        public static void AddCustomHint(this Player player, HintServiceMeow.Core.Models.Hints.Hint hint)
        {
            if (player.IsUsingTranslator())
                TranslationManager.TranslatePreserveNewlines(hint.Text, TranslatorPlayers[player], translated =>
                {
                    hint.Text = translated;
                    HintServiceMeow.Core.Extension.ExiledPlayerExtension.AddHint(player, hint);
                });
            else
                HintServiceMeow.Core.Extension.ExiledPlayerExtension.AddHint(player, hint);
        }

        public static void ExplodeGrenade(this Player player, Vector3? pos = null, float fuseTime = 0,
            ItemType grenade = ItemType.GrenadeHE, bool ignore = false, bool instakill = true)
        {
            pos ??= player.Position;
            if (grenade == ItemType.GrenadeFlash)
            {
                var g = (FlashGrenade)Item.Create(grenade, player);
                g.FuseTime = fuseTime;
                g.SpawnActive(pos.Value, player);
            }
            else
            {
                var g = (ExplosiveGrenade)Item.Create(grenade, player);
                g.FuseTime = fuseTime;
                g.MaxRadius = ignore ? 0 : g.MaxRadius;
                g.SpawnActive(pos.Value, player);

                if (!instakill) return;
                if (GodModePlayers.Contains(player)) GodModePlayers.Remove(player);
                player.Kill(DamageType.Explosion);
            }
        }
    }
}