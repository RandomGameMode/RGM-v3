using System.Collections.Generic;
using Exiled.API.Features;
using Exiled.API.Enums;
using MEC;
using PlayerRoles;
using Exiled.Events.EventArgs.Player;
using static RGM.Variables.Variable;
using RGM.API.Features;
using MapGeneration.Holidays;
using UnityEngine;
using RGM.API.DataBases;
using RGM.Patches;

namespace RGM.Modes
{
    [Mode(ModeCategory.Public, ModeInfo.Plus, ModeType.Spirit)]
    internal class Spirit : Mode
    {
        public override string Name => "스피릿";
        public override string Description => "죽으면 영혼 상태에 돌입합니다!";

        public override string Detail =>
            """
            죽으면 영혼 상태로 부활합니다. 이 상태에서 사망하면 성불됩니다.
            또한, 자살로 사망한 경우 곧바로 성불됩니다.

            * 게임 시작 10분 뒤 <color=red>자동핵</color>이 작동됩니다.
            """;

        public override string Color => "CED8F6";

        public static Spirit Instance;

        private List<Player> spirits = new();

        private CoroutineHandle _onModeStarted;
        private readonly AutoWarhead _autoWarhead = new(10, 1);

        public override void OnEnabled()
        {
            Exiled.Events.Handlers.Player.Hurting += OnHurting;
            Exiled.Events.Handlers.Player.Died += OnDied;
            Exiled.Events.Handlers.Player.Shot += OnShot;
            Exiled.Events.Handlers.Player.Hurt += OnHurt;

            _onModeStarted = Timing.RunCoroutine(OnModeStarted());
            _autoWarhead.RunCoroutine();
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Player.Hurting -= OnHurting;
            Exiled.Events.Handlers.Player.Died -= OnDied;
            Exiled.Events.Handlers.Player.Shot -= OnShot;
            Exiled.Events.Handlers.Player.Hurt -= OnHurt;

            Timing.KillCoroutines(_onModeStarted);
            _autoWarhead.KillCoroutine();
        }

        private IEnumerator<float> OnModeStarted()
        {
            if (Random.Range(1, 101) <= 10) //10% 확률로 워크스테이션 업그레이드 시작
                Tools.TryInstallMode(ModeType.ABattle);
            while (true)
            {
                foreach (var player in PlayerManager.List)
                    if (spirits.Contains(player))
                    {
                        player.EnableEffect(EffectType.Invisible);
                        if (!HolidayUtils.IsHolidayActive(HolidayType.Halloween))
                            player.EnableEffect(EffectType.Ghostly);
                    }

                yield return Timing.WaitForSeconds(1.5f);
            }
        }

        private void OnHurting(HurtingEventArgs ev)
        {
            if (GodModePlayers.Contains(ev.Player))
                GodModePlayers.Remove(ev.Player);

            if (spirits.Contains(ev.Player) && spirits.Contains(ev.Attacker)) // 스피릿이 스피릿을 공격한 경우 패널티 부여
            {
                ev.Attacker.Kill("같은 유령을 죽이는 실수를 범해서는 안됬습니다.");
                ev.Player.Kill("유령은 퇴치당했습니다.");
            }
            else if (spirits.Contains(ev.Player))
            {
                ev.Player.Kill("유령은 퇴치당했습니다.");
            }
        }

        private IEnumerator<float> OnDied(DiedEventArgs ev)
        {
            if (spirits.Contains(ev.Player) ||
                Datas.BlockDamageTypes.Contains(ev.DamageHandler.Type))
            {
                ev.Player.AddHint("스피릿 성불", $"성불했습니다..");
                spirits.Remove(ev.Player);
            }
            else
            {
                for (var i = 1; i < 6; i++)
                {
                    ev.Player.ShowHint($"{6 - i}초 뒤 영혼 상태에 돌입합니다.", 1.1f);

                    yield return Timing.WaitForSeconds(1f);
                }

                Timing.CallDelayed(1f, () =>
                {
                    ev.Player.MaxHealth = 1;
                    ev.Player.Health = ev.Player.MaxHealth;
                    ev.Player.EnableEffect(EffectType.Ghostly);
                });

                spirits.Add(ev.Player);

                ev.Player.Role.Set(RoleTypeId.Tutorial, RoleSpawnFlags.None);
            }
        }

        private void OnShot(ShotEventArgs ev)
        {
            if (spirits.Contains(ev.Player))
                ev.Player.DisableEffect(EffectType.Invisible);
        }

        private void OnHurt(HurtEventArgs ev)
        {
            if (ev.Attacker != null && spirits.Contains(ev.Attacker))
                ev.Attacker.DisableEffect(EffectType.Invisible);

            if (spirits.Contains(ev.Player))
                ev.Player.DisableEffect(EffectType.Invisible);
        }
    }
}
