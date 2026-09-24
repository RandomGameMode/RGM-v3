using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.API.Features.Pickups;
using Exiled.API.Features.Pickups.Projectiles;
using Exiled.Events.EventArgs.Player;
using MEC;
using PlayerRoles;
using ProjectMER.Features;
using ProjectMER.Features.Objects;
using RGM.API.Features;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RGM.Modes.Sets.AddScp.Scps
{
    public static class Scp457
    {
        static bool abilityCooldown = false;

        public static Player Create(Player player)
        {
            player.Role.Set(RoleTypeId.Scp0492, RoleSpawnFlags.AssignInventory);
            player.EnableEffect(EffectType.SilentWalk, 10);
            player.EnableEffect(EffectType.Slowness, 30);
            player.MaxHealth = 1500;
            player.Health = player.MaxHealth;
            Timing.CallDelayed(1, () =>
            {
                player.Scale = new Vector3(0.3f, 0.3f, 0.3f);
            });
            player.AddHint("SCP-457 설명",
"""  
<size=25>
당신은 <color=red>SCP-457</color>(<color=#f4fe48>Euclid</color>/<color=#fe4848>Potential Keter</color>)입니다.
</size>
<size=20>
진영은 <color=red>SCP</color> 소속입니다.
• 인간에게 다가가면 지속적인 피해를 입힙니다. (데미지는 크기에 비례)
• 아이템에 다가가면 해당 아이템을 체력과 크기로 흡수합니다. (무게에 비례)
• 60초마다 [ALT]키를 눌러 원거리 공격을 할 수 있습니다.
• 사망할 경우 폭발합니다.
</size>
""", 20);
                
            SchematicObject schematic = ObjectSpawner.SpawnSchematic("SCP_457", player.Position, player.Rotation, new Vector3(0.4f, 0.4f, 0.4f));
            schematic.transform.parent = player.Transform;

            IEnumerator<float> main()
            {
                while (true)
                {
                    foreach (var pickup in Pickup.List.Where(x => x.Type != ItemType.KeycardO5))
                    {
                        if (Vector3.Distance(player.Position, pickup.Position) < 3)
                        {
                            if (player.Scale.x < 1.15f)
                                player.Scale += new Vector3(0.001f * pickup.Weight, 0.001f * pickup.Weight, 0.001f * pickup.Weight);
                            player.MaxHealth += pickup.Weight * 1.1f;
                            player.Health += pickup.Weight * 1.1f;

                            pickup.Destroy();
                        }
                    }

                    yield return Timing.WaitForSeconds(1);
                }
            }

            IEnumerator<float> makeSound()
            {
                while (true)
                {
                    var audio = Tools.PlaySound(player.Transform, "scp-457-burn");

                    yield return Timing.WaitForSeconds((float)audio.Duration.TotalSeconds);
                }
            }

            IEnumerator<float> burn2()
            {
                while (true)
                {
                    foreach (var p in PlayerManager.List.Where(x => !x.IsScpRole() && x != player && Vector3.Distance(x.Position, player.Position) < 3.5f))
                    {
                        p.EnableEffect(EffectType.Burned, 1, 0.1f);
                        p.Hit(player, player.Scale.x * 5);

                        player.Heal(player.Scale.x * 0.3f);
                    }

                    yield return Timing.WaitForSeconds(0.1f);
                }
            }

            IEnumerator<float> attack()
            {
                while (true)
                {
                    abilityCooldown = false;

                    while (!abilityCooldown)
                    {
                        player.AddHint("SCP-457 공격 가능", "<size=20>[ALT]키를 눌러 원거리 공격</size>", 1);

                        yield return Timing.WaitForSeconds(1);
                    }

                    yield return Timing.WaitForSeconds(60);
                }
            }

            var mainCoroutine = Timing.RunCoroutine(main());
            var makeSoundCoroutine = Timing.RunCoroutine(makeSound());
            var burn2Coroutine = Timing.RunCoroutine(burn2());
            var attackCoroutine = Timing.RunCoroutine(attack());

            void OnHurting(HurtingEventArgs ev)
            {
                if (ev.Attacker == null || ev.Attacker != player)
                    return;

                if (ev.DamageHandler.Type == DamageType.Explosion)
                {
                    ev.DamageHandler.Damage /= 5;
                }
            }

            IEnumerator<float> OnTogglingNoClip(TogglingNoClipEventArgs ev)
            {
                if (ev.Player != player || abilityCooldown)
                    yield break;

                Throwable throwable = ev.Player.ThrowGrenade(ProjectileType.FragGrenade);
                SchematicObject fireBall = ObjectSpawner.SpawnSchematic("FireBall", new Vector3(0, 0, 0));
                fireBall.transform.parent = throwable.Projectile.Transform;

                abilityCooldown = true;

                yield return Timing.WaitForSeconds(0.3f);

                if (throwable.Projectile is ExplosionGrenadeProjectile grenade)
                {
                    while (!grenade.IsAlreadyDetonated)
                    {
                        if (Physics.OverlapSphere(grenade.Position, 0.3f).Count() > 4)
                        {
                            grenade.Base.Network_syncTargetTime = 0.1f;

                            fireBall.Destroy();

                            Vector3 pos = grenade.Position;

                            IEnumerator<float> lava()
                            {
                                SchematicObject lava = ObjectSpawner.SpawnSchematic("LavaChicken", pos, new Quaternion(0, 0, 0, 0), new Vector3(0.4f, 0.4f, 0.4f));

                                if (Physics.Raycast(new Vector3(pos.x, pos.y + 1, pos.z), Vector3.down, out RaycastHit hit, 100, (LayerMask)1))
                                {
                                    lava.Position = hit.point;
                                }

                                var audio = Tools.PlaySound(lava.transform, "scp-457-burn");

                                for (int i = 0; i < 20; i++)
                                {
                                    foreach (var p in PlayerManager.List.Where(x => !x.IsScpRole() && x != player && Vector3.Distance(x.Position, pos) < 4))
                                    {
                                        p.EnableEffect(EffectType.Burned, 1, 0.1f);
                                        p.Hit(player, player.Scale.x * 10);
                                    }

                                    yield return Timing.WaitForSeconds(1);
                                }

                                lava.Destroy();
                                audio.IsPaused = true;
                            }

                            Timing.RunCoroutine(lava());
                        }

                        yield return Timing.WaitForOneFrame;
                    }
                }
            }

            void OnDied(DiedEventArgs ev)
            {
                if (ev.Attacker == null || ev.Attacker != player)
                    return;

                if (ev.Attacker.Scale.x < 1.15f)
                    ev.Attacker.Scale += new Vector3(0.02f, 0.02f, 0.02f);
                ev.Attacker.MaxHealth += 15;
                ev.Attacker.Health += 15;
            }

            void OnDying(DyingEventArgs ev)
            {
                if (ev.Player != player)
                    return;

                Vector3 pos = ev.Player.Position;

                Timing.CallDelayed(Timing.WaitForOneFrame, () =>
                {
                    if (ev.Player.IsDead)
                    {
                        if (ev.Player == player)
                        {
                            var g = (ExplosiveGrenade)Item.Create(ItemType.GrenadeHE, player);
                            g.FuseTime = 0.1f;
                            g.SpawnActive(pos, player);

                            Timing.KillCoroutines(mainCoroutine);
                            Timing.KillCoroutines(makeSoundCoroutine);
                            Timing.KillCoroutines(burn2Coroutine);
                            Timing.KillCoroutines(attackCoroutine);

                            schematic.Destroy();

                            Exiled.Events.Handlers.Player.Hurting -= OnHurting;
                            Exiled.Events.Handlers.Player.TogglingNoClip -= OnTogglingNoClip;
                            Exiled.Events.Handlers.Player.Died -= OnDied;
                            Exiled.Events.Handlers.Player.Dying -= OnDying;
                        }
                    }
                });
            }

            Exiled.Events.Handlers.Player.Hurting += OnHurting;
            Exiled.Events.Handlers.Player.TogglingNoClip += OnTogglingNoClip;
            Exiled.Events.Handlers.Player.Died += OnDied;
            Exiled.Events.Handlers.Player.Dying += OnDying;
            return player;
        }
    }
}
