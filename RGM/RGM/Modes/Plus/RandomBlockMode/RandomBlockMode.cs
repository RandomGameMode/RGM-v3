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
5 ~ 180초 사이에 플레이어마다 금지된 행동이 변경됩니다.
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
            달리기,
            점프,
            공격,
            말하기,
            아이템_사용,
            문_상호작용,
            발전기_열기,
            카드키_들기,
            총_들기,
            의료_아이템_들기,
            천천히_걷기,
            탈출하기,
            움직이기,
        }

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

            Timing.KillCoroutines(_onModeStarted);
            Timing.KillCoroutines(_check);
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

                ushort time = (ushort)UnityEngine.Random.Range(5, 181);

                foreach (var player in PlayerManager.List)
                {
                    if (!_dict.ContainsKey(player))
                        _dict.Add(player, BlockedActions.달리기);

                    var blockedAction = Tools.EnumToList<BlockedActions>().GetRandomValue();
                    _dict[player] = blockedAction;

                    player.AddBroadcast(time, $"<size=30>당신은 <color=red>{blockedAction.ToString().Replace("_", " ")}</color>(을)를 할 수 없습니다.</size>");
                }

                yield return Timing.WaitForSeconds(time);
            }
        }

        private IEnumerator<float> Check()
        {
            while (true)
            {
                foreach (var player in _dict.Keys.Where(x => !x.IsDead))
                {
                    try
                    {
                        var blockedAction = _dict[player];
                        FirstPersonMovementModule fpcModule =
                            (player.ReferenceHub.roleManager.CurrentRole as FpcStandardRoleBase)?.FpcModule;
                        if (fpcModule is null) continue;

                        switch (blockedAction)
                        {
                            case BlockedActions.달리기:
                            {
                                if (fpcModule.CurrentMovementState == PlayerMovementState.Sprinting)
                                    player.ExplodeGrenade(ignore: true);
                                break;
                            }
                            case BlockedActions.천천히_걷기:
                            {
                                if (fpcModule.CurrentMovementState == PlayerMovementState.Sneaking)
                                    player.ExplodeGrenade(ignore: true);
                                break;
                            }
                            case BlockedActions.움직이기:
                            {
                                if (!_posDict.ContainsKey(player))
                                    _posDict.Add(player, player.Position);

                                if (_posDict[player] != player.Position)
                                    player.ExplodeGrenade(ignore: true);

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

            if (_dict.ContainsKey(ev.Player) && _dict[ev.Player] == BlockedActions.점프)
                ev.Player.ExplodeGrenade(ignore: true);
        }

        private void OnHurting(HurtingEventArgs ev)
        {
            if (ev.Player.IsDead)
                return;

            if (ev.Attacker != null && _dict.ContainsKey(ev.Attacker) && _dict[ev.Attacker] == BlockedActions.공격)
                ev.Attacker.ExplodeGrenade(ignore: true);
        }

        private void OnVoiceChatting(VoiceChattingEventArgs ev)
        {
            if (ev.Player.IsDead)
                return;

            if (_dict.ContainsKey(ev.Player) && _dict[ev.Player] == BlockedActions.말하기 && !ev.Player.IsDead)
                ev.Player.ExplodeGrenade(ignore: true);
        }

        private void OnUsedItem(UsingItemEventArgs ev)
        {
            if (ev.Player.IsDead)
                return;

            if (_dict.ContainsKey(ev.Player) && _dict[ev.Player] == BlockedActions.아이템_사용)
                ev.Player.ExplodeGrenade(ignore: true);
        }

        private void OnInteractingDoor(InteractingDoorEventArgs ev)
        {
            if (ev.Player.IsDead)
                return;

            if (_dict.ContainsKey(ev.Player) && _dict[ev.Player] == BlockedActions.문_상호작용)
                ev.Player.ExplodeGrenade(ignore: true);
        }

        private void OnOpeningGenerator(OpeningGeneratorEventArgs ev)
        {
            if (ev.Player.IsDead)
                return;

            if (_dict.ContainsKey(ev.Player) && _dict[ev.Player] == BlockedActions.발전기_열기)
                ev.Player.ExplodeGrenade(ignore: true);
        }

        private void OnChangingItem(ChangingItemEventArgs ev)
        {
            if (ev.Player.IsDead)
                return;

            if (!_dict.ContainsKey(ev.Player)) return;
            
            if (_dict[ev.Player] == BlockedActions.카드키_들기 && ev.Item.Type.IsKeycard())
                ev.Player.ExplodeGrenade(ignore: true);

            if (_dict[ev.Player] == BlockedActions.총_들기 && ev.Item.Type.IsWeapon())
                ev.Player.ExplodeGrenade(ignore: true);

            if (_dict[ev.Player] == BlockedActions.의료_아이템_들기 && ev.Item.Type.IsMedical())
                ev.Player.ExplodeGrenade(ignore: true);
        }

        private void OnEscaping(EscapingEventArgs ev)
        {
            if (ev.Player.IsDead)
                return;

            if (_dict.ContainsKey(ev.Player) && _dict[ev.Player] == BlockedActions.탈출하기)
                ev.Player.ExplodeGrenade(ignore: true);

            ev.IsAllowed = false;
        }
    }
}
