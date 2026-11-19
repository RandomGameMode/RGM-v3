using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using MEC;
using PlayerRoles;
using RGM.API.Features;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RGM.Modes.Sets.AddScp.Scps
{
    public static class Scp008
    {
        public static Player Create(Player player)
        {
            player.MaxHealth = 800;
            player.Health = player.MaxHealth;
            player.AddHint("SCP-008 설명",
                """
                <size=25>
                당신은 <color=red>SCP-008</color>(<color=#f4fe48>Euclid</color>)입니다.
                </size>
                <size=20>
                당신은 이미 감염되었으며, 2분 뒤 <color=red>SCP-049-2</color>로 변모합니다.
                • 변모 시, 주변의 인간들을 전부 <color=red>SCP-049-2</color>로 만듭니다.
                • 인간을 사망에 이르게 한 경우 그 대상도 <color=red>SCP-049-2</color>로 만듭니다.
                </size>
                """, 20);


            var mainCoroutine = Timing.RunCoroutine(Main());

            Exiled.Events.Handlers.Player.Dying += OnDying;
            return player;

            void OnDying(DyingEventArgs ev)
            {
                if (ev.Attacker != null && ev.Attacker == player)
                {
                    ev.IsAllowed = false;
                    ev.Player.Role.Set(RoleTypeId.Scp0492, RoleSpawnFlags.None);
                }

                if (ev.Player != player) return;
                var pos = ev.Player.Position;

                Timing.CallDelayed(Timing.WaitForOneFrame, () =>
                {
                    if (!ev.Player.IsDead) return;
                    if (ev.Player != player) return;
                    Timing.KillCoroutines(mainCoroutine);

                    Exiled.Events.Handlers.Player.Dying -= OnDying;
                });
            }

            IEnumerator<float> Main()
            {
                yield return Timing.WaitForSeconds(120);

                player.Role.Set(RoleTypeId.Scp0492, RoleSpawnFlags.None);
                player.MaxHealth += 800;
                player.Heal(player.MaxHealth);
                player.EnableEffect(EffectType.MovementBoost, 25);

                foreach (var p in PlayerManager.List.Where(x =>
                             !x.IsScpRole() && !AddScpMode.SpecialScps.Contains(x) &&
                             Vector3.Distance(player.Position, x.Position) <= 6))
                {
                    p.Role.Set(RoleTypeId.Scp0492, RoleSpawnFlags.None);
                    p.AddHint("SCP-008에 의해 감염됨",
                        $"<size=25>당신은 <color=red>SCP-008-X</color>(<color=#f4fe48>Euclid</color>)입니다.</size>\n<size=20>당신은 <b><i>{player.DisplayNickname}</i></b>(으)로 인해 감염되었습니다.</size>",
                        20);
                }
            }
        }
    }
}
