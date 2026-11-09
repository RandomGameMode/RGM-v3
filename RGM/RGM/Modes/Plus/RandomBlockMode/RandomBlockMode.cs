using System;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using MEC;
using PlayerRoles.FirstPersonControl;
using RGM.API.Features;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static RGM.Variables.Variable;

namespace RGM.Modes
{
    [Mode(ModeCategory.Public, ModeInfo.Plus, ModeType.RandomBlockMode)]
    public class RandomBlockMode : Mode
    {
        public override string Name => "랜덤금지모드";
        public override string Description => "절대로 금지된 행동을 해선 안됩니다!";

        public override string Detail =>
            """
            5 ~ 120초 사이에 플레이어마다 금지된 행동이 변경됩니다.
            금지된 행동이 고지되기 전에 2초 간 경고 시간이 주어집니다.

            금지된 행동을 하면 귀여워질 수 있습니다.
            """;

        public override string Color => "d97053";

        private CoroutineHandle _onModeStarted;
        private CoroutineHandle _check;

        private readonly Dictionary<Player, BlockedActions> _dict = new();
        private readonly Dictionary<Player, Vector3> _posDict = new();

        private enum BlockedActions
        {
            Running, // 달리기 금지
            Jumping, // 점프 금지
            Attacking, // 공격 금지
            Speaking, // 말하기 금지
            UsingItem, // 아이템사용 금지
            InteractingDoor, // 문 상호작용 금지
            OpenGenerator, // 발전기 열기 금지
            HeldKeyCard, // 키카드 들기 금지
            HeldGun, // 총 들기 금지
            HeldMedicalItem, // 의료 아이템 들기 금지
            WalkSlowly, // 천천히 걷기 금지
            Escaping, // 탈출하기 금지
            Moving, // 움직이기 금지
        }

        // 플레이어에게 보여주는 표시 이름. 동작 로직은 enum 이름을 그대로 사용한다.
        // 안내 문구가 "당신은 {이름}(을)를 할 수 없습니다."이므로 "금지"는 제외한다.
        private static readonly Dictionary<BlockedActions, string> DisplayNames = new()
        {
            [BlockedActions.Running] = "달리기",
            [BlockedActions.Jumping] = "점프",
            [BlockedActions.Attacking] = "공격",
            [BlockedActions.Speaking] = "말하기",
            [BlockedActions.UsingItem] = "아이템사용",
            [BlockedActions.InteractingDoor] = "문 상호작용",
            [BlockedActions.OpenGenerator] = "발전기 열기",
            [BlockedActions.HeldKeyCard] = "키카드 들기",
            [BlockedActions.HeldGun] = "총 들기",
            [BlockedActions.HeldMedicalItem] = "의료 아이템 들기",
            [BlockedActions.WalkSlowly] = "천천히 걷기",
            [BlockedActions.Escaping] = "탈출하기",
            [BlockedActions.Moving] = "움직이기",
        };

        private static string GetDisplayName(BlockedActions action)
            => DisplayNames.TryGetValue(action, out var name) ? name : action.ToString();

        public override void OnEnabled()
        {
            Exiled.Events.Handlers.Player.Jumping += OnJumping;
            Exiled.Events.Handlers.Player.Hurting += OnHurting;
            Exiled.Events.Handlers.Player.VoiceChatting += OnVoiceChatting;
            Exiled.Events.Handlers.Player.UsingItem += OnUsedItem;
            Exiled.Events.Handlers.Player.InteractingDoor += OnInteractingDoor;
            Exiled.Events.Handlers.Player.OpeningGenerator += OnOpeningGenerator;
            Exiled.Events.Handlers.Player.ChangingItem += OnChangingItem;
            Exiled.Events.Handlers.Player.Escaping += OnEscaping;
            Exiled.Events.Handlers.Player.Left += OnLeft;

            _onModeStarted = Timing.RunCoroutine(OnModeStarted());
            _check = Timing.RunCoroutine(Check());
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Player.Jumping -= OnJumping;
            Exiled.Events.Handlers.Player.Hurting -= OnHurting;
            Exiled.Events.Handlers.Player.VoiceChatting -= OnVoiceChatting;
            Exiled.Events.Handlers.Player.UsingItem -= OnUsedItem;
            Exiled.Events.Handlers.Player.InteractingDoor -= OnInteractingDoor;
            Exiled.Events.Handlers.Player.OpeningGenerator -= OnOpeningGenerator;
            Exiled.Events.Handlers.Player.ChangingItem -= OnChangingItem;
            Exiled.Events.Handlers.Player.Escaping -= OnEscaping;
            Exiled.Events.Handlers.Player.Left -= OnLeft;

            Timing.KillCoroutines(_onModeStarted);
            Timing.KillCoroutines(_check);

            _dict.Clear();
            _posDict.Clear();
        }

        // 퇴장했거나 파괴된 Player 인스턴스에 수류탄을 생성하면 Exiled의 ItemAdded 이벤트에서 NRE가 발생한다.
        private static bool IsValid(Player player)
            => player != null && player.IsConnected && player.ReferenceHub != null && player.GameObject != null;

        private void OnLeft(LeftEventArgs ev)
        {
            _dict.Remove(ev.Player);
            _posDict.Remove(ev.Player);
        }

        private static void Punish(Player player)
        {
            if (!IsValid(player) || player.IsDead)
                return;

            player.ExplodeGrenade(ignore: true);
        }

        private IEnumerator<float> OnModeStarted()
        {
            while (true)
            {
                foreach (var room in Room.List)
                    room.Color = UnityEngine.Color.red;

                GlobalPlayer.TryPlay("출석 체크", 1.5f);

                yield return Timing.WaitForSeconds(2);

                foreach (var room in Room.List)
                    room.ResetColor();

                ushort time = (ushort)UnityEngine.Random.Range(5, 121);

                foreach (var player in PlayerManager.List.Where(IsValid).ToList())
                {
                    if (!_dict.ContainsKey(player))
                        _dict.Add(player, BlockedActions.Running);

                    var blockedAction = Tools.EnumToList<BlockedActions>().GetRandomValue();
                    _dict[player] = blockedAction;

                    player.AddBroadcast(time,
                        $"<size=30>당신은 <color=red>{GetDisplayName(blockedAction)}</color>(을)를 할 수 없습니다.</size>");
                }

                yield return Timing.WaitForSeconds(time);
            }
        }

        private IEnumerator<float> Check()
        {
            while (true)
            {
                foreach (var player in _dict.Keys.ToList())
                {
                    try
                    {
                        if (!IsValid(player))
                        {
                            _dict.Remove(player);
                            _posDict.Remove(player);
                            continue;
                        }

                        if (player.IsDead)
                            continue;

                        var blockedAction = _dict[player];
                        FirstPersonMovementModule fpcModule =
                            (player.ReferenceHub.roleManager.CurrentRole as FpcStandardRoleBase)?.FpcModule;
                        if (fpcModule is null) continue;

                        switch (blockedAction)
                        {
                            case BlockedActions.Running:
                            {
                                if (fpcModule.CurrentMovementState == PlayerMovementState.Sprinting)
                                    Punish(player);
                                break;
                            }
                            case BlockedActions.WalkSlowly:
                            {
                                if (fpcModule.CurrentMovementState == PlayerMovementState.Sneaking)
                                    Punish(player);
                                break;
                            }
                            case BlockedActions.Moving:
                            {
                                if (!_posDict.ContainsKey(player))
                                    _posDict.Add(player, player.Position);

                                if (_posDict[player] != player.Position)
                                    Punish(player);

                                _posDict[player] = player.Position;
                                break;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                }

                yield return Timing.WaitForOneFrame;
            }
        }

        private void OnJumping(JumpingEventArgs ev)
        {
            if (ev.Player.IsDead)
                return;

            if (_dict.ContainsKey(ev.Player) && _dict[ev.Player] == BlockedActions.Jumping)
                Punish(ev.Player);
        }

        private void OnHurting(HurtingEventArgs ev)
        {
            if (ApplyFixedDamage.IsApplying) return;
            if (ev.Player.IsDead)
                return;

            if (ev.Attacker != null && _dict.ContainsKey(ev.Attacker) && _dict[ev.Attacker] == BlockedActions.Attacking)
                Punish(ev.Attacker);
        }

        private void OnVoiceChatting(VoiceChattingEventArgs ev)
        {
            if (ev.Player.IsDead)
                return;

            if (_dict.ContainsKey(ev.Player) && _dict[ev.Player] == BlockedActions.Speaking && !ev.Player.IsDead)
                Punish(ev.Player);
        }

        private void OnUsedItem(UsingItemEventArgs ev)
        {
            if (ev.Player.IsDead)
                return;

            if (_dict.ContainsKey(ev.Player) && _dict[ev.Player] == BlockedActions.UsingItem)
                Punish(ev.Player);
        }

        private void OnInteractingDoor(InteractingDoorEventArgs ev)
        {
            if (ev.Player.IsDead)
                return;

            if (_dict.ContainsKey(ev.Player) && _dict[ev.Player] == BlockedActions.InteractingDoor)
                Punish(ev.Player);
        }

        private void OnOpeningGenerator(OpeningGeneratorEventArgs ev)
        {
            if (ev.Player.IsDead)
                return;

            if (_dict.ContainsKey(ev.Player) && _dict[ev.Player] == BlockedActions.OpenGenerator)
                Punish(ev.Player);
        }

        private void OnChangingItem(ChangingItemEventArgs ev)
        {
            if (ev.Player.IsDead)
                return;

            if (ev.Item == null || !_dict.ContainsKey(ev.Player)) return;

            if (_dict[ev.Player] == BlockedActions.HeldKeyCard && ev.Item.Type.IsKeycard())
                Punish(ev.Player);

            if (_dict[ev.Player] == BlockedActions.HeldGun && ev.Item.Type.IsWeapon())
                Punish(ev.Player);

            if (_dict[ev.Player] == BlockedActions.HeldMedicalItem && ev.Item.Type.IsMedical())
                Punish(ev.Player);
        }

        private void OnEscaping(EscapingEventArgs ev)
        {
            if (ev.Player.IsDead)
                return;

            if (_dict.ContainsKey(ev.Player) && _dict[ev.Player] == BlockedActions.Escaping)
                Punish(ev.Player);

            ev.IsAllowed = false;
        }
    }
}
