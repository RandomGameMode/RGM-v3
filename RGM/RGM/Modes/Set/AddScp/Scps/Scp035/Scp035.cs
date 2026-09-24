using CustomPlayerEffects;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Pickups;
using Exiled.Events.EventArgs.Player;
using MEC;
using PlayerRoles;
using ProjectMER.Features;
using ProjectMER.Features.Objects;
using RGM.API.Features;
using System.Collections.Generic;
using UnityEngine;

namespace RGM.Modes.Sets.AddScp.Scps
{
    public static class Scp035
    {
        public static Player Create(Player player)
        {
            player.Role.Set(RoleTypeId.Tutorial, RoleSpawnFlags.None);
            player.MaxHealth = 1350;
            player.Health = player.MaxHealth;
            player.EnableEffect(EffectType.MovementBoost, 25);
            player.AddHint("SCP-035 설명",
"""
<size=25>
당신은 <color=red>SCP-035</color>(<color=red>Keter</color>)입니다.
</size>
<size=20>
인간의 뇌를 지배한 가면입니다.
• 뱀의 손 역할군으로 지정되며, 모든 진영에 중립입니다.
• 점액질로 인해 체력이 3초당 1씩 감소합니다. 주의하세요!
• 당신이 사망한 후, 누군가가 <color=red>SCP-035</color>를 회수하면 그 유저가 <color=red>SCP-035</color>가 됩니다.
</size>
""", 20);
            SchematicObject schematic = ObjectSpawner.SpawnSchematic("SCP_035", new Vector3(0, 0, 0), new Quaternion(0, 0, 0, 0), new Vector3(0.7f, 0.7f, 0.7f));
            schematic.transform.parent = player.Transform;
            schematic.transform.localPosition = new Vector3(-0.007f, 0.585f, 0.15f);
            schematic.transform.localRotation = new Quaternion(0, -45, 0, 0);

            IEnumerator<float> main()
            {
                while (true)
                {
                    if (player.TryGetEffect<Stained>(out var effect) && !effect.IsEnabled)
                        player.EnableEffect(EffectType.Stained, 1);

                    player.Hurt(1, "SCP-035의 부식성 물질로 인해 사망했습니다.");

                    yield return Timing.WaitForSeconds(3);
                }
            }

            var main_c = Timing.RunCoroutine(main());

            void OnDying(DyingEventArgs ev)
            {
                if (ev.Player != player) return;
                Vector3 pos = ev.Player.Position;

                Timing.CallDelayed(Timing.WaitForOneFrame, () =>
                {
                    if (!ev.Player.IsDead) return;
                    if (ev.Player != player) return;
                    Pickup pickup = Pickup.CreateAndSpawn(ItemType.KeycardO5, pos);

                    SchematicObject scp035 = ObjectSpawner.SpawnSchematic("SCP_035", new Vector3(0, 0, 0), Quaternion.Euler(90, 90, 0), new Vector3(0.9f, 0.9f, 0.9f));
                    scp035.transform.parent = pickup.Transform;
                    scp035.transform.localPosition = Vector3.zero;

                    void OnPickingUpItem(PickingUpItemEventArgs e)
                    {
                        if (e.Pickup == pickup)
                        {
                            e.IsAllowed = false;
                            e.Pickup.Destroy();

                            Create(e.Player);

                            Exiled.Events.Handlers.Player.PickingUpItem -= OnPickingUpItem;
                        }
                    }

                    Exiled.Events.Handlers.Player.PickingUpItem += OnPickingUpItem;

                    schematic.Destroy();
                    Timing.KillCoroutines(main_c);

                    Exiled.Events.Handlers.Player.Dying -= OnDying;
                });
            }

            Exiled.Events.Handlers.Player.Dying += OnDying;
            return player;
        }
    }
}
