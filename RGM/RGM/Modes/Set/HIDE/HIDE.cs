using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using MEC;
using Mirror;
using PlayerRoles;
using UnityEngine;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using RGM.API.Features;
using Exiled.Events.EventArgs.Server;
using static RGM.Variables.Variable;

namespace RGM.Modes
{
    [Mode(ModeCategory.Public, ModeInfo.Set, ModeType.HIDE)]
    class HIDE : Mode
    {
        public override string Name => "HIDE";
        public override string Description => "숨 죽이는 그를 사살하십시오.";
        public override string Detail =>
"""
<color=red>SCP-3114</color>는 피격당하거나 공격하면 투명이 해제됩니다.
또한, 평상시에도 반투명 상태로 존재합니다. 이 점을 잘 이용해보세요!

<b>[Map Credit]</b>
@vlrpfrjs
""";
        public override string Color => "0489B1";
        public override string Map => "container";

        public static HIDE Instance;

        private readonly List<Player> _pl = [];
        private Player _monster;

        private CoroutineHandle _onModeStarted;
        private CoroutineHandle _timer;

        public override void OnEnabled()
        {
            Respawn.PauseWaves(); 
            Server.ExecuteCommand($"/el l all");
            Server.ExecuteCommand($"/close **");
            Server.ExecuteCommand($"/lock **");

            Exiled.Events.Handlers.Player.Hurting += OnHurting;

            Exiled.Events.Handlers.Server.RoundEnded += OnRoundEnded;

            _onModeStarted = Timing.RunCoroutine(OnModeStarted());
            _timer = Timing.RunCoroutine(Timer());
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Player.Hurting -= OnHurting;

            Exiled.Events.Handlers.Server.RoundEnded -= OnRoundEnded;

            Timing.KillCoroutines(_onModeStarted);
            Timing.KillCoroutines(_timer);
        }

        private IEnumerator<float> OnModeStarted()
        {
            PlayerManager.List.ToList().CopyTo(_pl);
            _monster = PlayerManager.List.ToList().GetRandomValue();

            try
            {
                Timing.CallDelayed(1f, () =>
                {
                    _monster.Role.Set(RoleTypeId.Scp3114);
                    _monster.RankName = "MONSTER";
                    _monster.RankColor = "red";
                    _monster.Position = new Vector3(-0.6015625f, 332.9026f, -32.56641f);
                    Server.ExecuteCommand($"/open ESCAPE_PRIMARY");

                    float health = 15 * PlayerManager.List.Count;
                    _monster.MaxHealth = health;
                    _monster.Health = health;
                    _monster.IsUsingStamina = false;
                    _monster.MaxHumeShield = 60;
                    _monster.HumeShield = _monster.MaxHumeShield;
                    _monster.EnableEffect(EffectType.MovementBoost, 70);
                    _monster.EnableEffect(EffectType.Fade, 220);
                    _monster.EnableEffect(EffectType.Lightweight, 150);

                    foreach (var player in PlayerManager.List)
                    {
                        if (player == _monster) continue;
                        player.Role.Set(RoleTypeId.NtfPrivate);
                        player.Position = new Vector3(36.61497f, 332.9037f, -69.72147f);
                        for (int i = 1; i < 10; i++)
                            player.AddItem(ItemType.Ammo9x19);
                    }


                });
            }
            catch (Exception e)
            {
                ServerConsole.AddLog(e.ToString());
            }

            yield break;
        }

        private static IEnumerator<float> Timer()
        {
            for (int i = 1; i < 180; i++)
            {
                PlayerManager.List.ToList().ForEach(x => x.AddBroadcast(1, $"<size=25><color=#2ECCFA>NTF 승리</color>까지</color> {180 - i}초</size>"));

                yield return Timing.WaitForSeconds(1f);
            }

            foreach (var player in PlayerManager.List)
            {
                if (!player.IsScpRole()) continue;
                if (GodModePlayers.Contains(player))
                    GodModePlayers.Remove(player);

                player.Kill("제한시간이 초과하였습니다.");
            }
        }

        private static void OnHurting(Exiled.Events.EventArgs.Player.HurtingEventArgs ev)
        {
            if (!ev.Attacker.IsScpRole() || ev.DamageHandler.Type == DamageType.Strangled) return;
            ev.DamageHandler.Damage += 79;
            ev.Attacker.HumeShield += 12;
        }

        private static void OnRoundEnded(RoundEndedEventArgs ev)
        {
            List<Player> players = [.. PlayerManager.List.Where(x => x.IsAlive && !x.IsNPC)];

            switch (players.Count)
            {
                case 1:
                    Timing.RunCoroutine(Tools.SetWinner(players.ToList(), PlayerManager.List.Count / 2));
                    break;
                case > 1:
                    Timing.RunCoroutine(Tools.SetWinner(players.ToList(), 1));
                    break;
            }
        }
    }
}
