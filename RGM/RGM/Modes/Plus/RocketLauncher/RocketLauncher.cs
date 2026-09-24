using System;
using System.Collections.Generic;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using MEC;
using PlayerRoles;
using RGM.API.DataBases;
using RGM.API.Features;
using RGM.Patches;
using Random = UnityEngine.Random;

namespace RGM.Modes
{
    [Mode(ModeCategory.Public, ModeInfo.Plus, ModeType.RocketLauncher)]
    class RocketLauncher : Mode
    {
        public override string Name => "로켓 런처";
        public override string Description => "무슨 이유로든 피격당하면 일정 확률로 승천합니다.";
        public override string Detail =>
"""
공격자가 <b>인간</b>인 경우 - 6%
공격자가 <b>SCP</b>인 경우 - 51%
공격자가 <b>???</b>인 경우 - 173%!!!!!

* 게임 시작 14분 뒤 <color=red>자동핵</color>이 작동됩니다.
""";
        public override string Color => "FA8258";

        public static RocketLauncher Instance;

        private readonly AutoWarhead _autoWarhead = new(14, 1);

        private readonly List<Player> _queue = [];

        public override void OnEnabled()
        {
            Exiled.Events.Handlers.Player.Hurt += OnHurt;
            _autoWarhead.RunCoroutine();
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Player.Hurt -= OnHurt;
            _autoWarhead.KillCoroutine();
        }

        private void OnHurt(HurtEventArgs ev)
        {
            if (ev.Attacker == null || 
                !HitboxIdentity.IsEnemy(ev.Attacker.ReferenceHub, ev.Player.ReferenceHub) ||
                ev.Player == ev.Attacker) return;
            
            if (_queue.Contains(ev.Player)) return;
            
            _queue.Add(ev.Player);

            if (Convert.ToByte(Random.Range(1, 101)) <= GetPercent())
            {
                Tools.MessageTranslated("", $"{ev.Player.DisplayNickname}(<color={ev.Player.Role.Color.ToHex()}>{( Trans.Role[ev.Player.Role.Type])}</color>)(이)가 하늘로 승천했습니다.");
                Timing.RunCoroutine(Tools.DoRocket(ev.Attacker, ev.Player, 1f));
            }

            Timing.CallDelayed(1, () =>
            {
                _queue.Remove(ev.Player);
            });
            
            return;

            byte GetPercent()
            {
                if (ev.Attacker.IsScpRole())
                    return 51;

                return Convert.ToByte(ev.Attacker.Role.Type == RoleTypeId.Tutorial ? 173 : 6);
            }
        }
    }
}
