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
        private static bool _abilityCooldown;

        public static Player Create(Player player)
        {
            player.Role.Set(RoleTypeId.Scp0492, RoleSpawnFlags.AssignInventory);
            player.EnableEffect(EffectType.SilentWalk, 10);
            player.EnableEffect(EffectType.Slowness, 20);
            player.MaxHealth = 1972;
            player.Health = player.MaxHealth;
            Timing.CallDelayed(1, () => { player.Scale = new Vector3(0.3f, 0.3f, 0.3f); });
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

            SchematicObject schematic = ObjectSpawner.SpawnSchematic("SCP_457", player.Position, player.Rotation,
                new Vector3(0.4f, 0.4f, 0.4f));
            schematic.transform.parent = player.Transform;
            
            var mainCoroutine = Timing.RunCoroutine(Main());
            var makeSoundCoroutine = Timing.RunCoroutine(MakeSound());
            var burn2Coroutine = Timing.RunCoroutine(Burn2());
            var attackCoroutine = Timing.RunCoroutine(Attack());

            Exiled.Events.Handlers.Player.Hurting += OnHurting;
            Exiled.Events.Handlers.Player.TogglingNoClip += OnTogglingNoClip;
            Exiled.Events.Handlers.Player.Died += OnDied;
            Exiled.Events.Handlers.Player.Dying += OnDying;
            return player;

            IEnumerator<float> Burn2()
            {
                while (true)
                {
                    foreach (var p in PlayerManager.List.Where(x =>
                                 !x.IsScpRole() && x != player && Vector3.Distance(x.Position, player.Position) < 3.6f))
                    {
                        p.EnableEffect(EffectType.Burned, 1, 0.1f);
                        p.Hit(player, player.Scale.x * 5 + (player.Scale.x >= 1.15f ? player.MaxHealth * 0.005f : 0));

                        player.Heal(player.Scale.x * 0.3f);
                    }

                    yield return Timing.WaitForSeconds(0.1f);
                }
            }

            IEnumerator<float> Attack()
            {
                while (true)
                {
                    _abilityCooldown = false;

                    while (!_abilityCooldown)
                    {
                        player.AddHint("SCP-457 공격 가능", "<size=20>[ALT]키를 눌러 원거리 공격</size>", 1);

                        yield return Timing.WaitForSeconds(1);
                    }

                    yield return Timing.WaitForSeconds(60);
                }
            }

            void OnHurting(HurtingEventArgs ev)
            {
                if (ev.Player != player)
                    return;

                if (ev.DamageHandler.Type == DamageType.Explosion)
                {
                    ev.DamageHandler.Damage /= 5;
                }
            }

            IEnumerator<float> OnTogglingNoClip(TogglingNoClipEventArgs ev)
            {
                if (ev.Player != player || _abilityCooldown)
                    yield break;

                Throwable throwable = ev.Player.ThrowGrenade(ProjectileType.FragGrenade);
                SchematicObject fireBall = ObjectSpawner.SpawnSchematic("FireBall", new Vector3(0, 0, 0));
                fireBall.transform.parent = throwable.Projectile.Transform;

                _abilityCooldown = true;

                yield return Timing.WaitForSeconds(0.3f);

                if (throwable.Projectile is ExplosionGrenadeProjectile grenade)
                {
                    while (!grenade.IsAlreadyDetonated)
                    {
                        if (Physics.OverlapSphere(grenade.Position, 0.3f).Length > 4)
                        {
                            grenade.Base.Network_syncTargetTime = 0.1f;

                            fireBall.Destroy();

                            Vector3 pos = grenade.Position;

                            IEnumerator<float> Lava()
                            {
                                SchematicObject lava = ObjectSpawner.SpawnSchematic("LavaChicken", pos,
                                    new Quaternion(0, 0, 0, 0), new Vector3(0.4f, 0.4f, 0.4f));

                                if (Physics.Raycast(new Vector3(pos.x, pos.y + 1, pos.z), Vector3.down,
                                        out RaycastHit hit, 100, (LayerMask)1))
                                {
                                    lava.Position = hit.point;
                                }

                                var audio = Tools.PlaySound(lava.transform, "scp-457-burn");

                                for (int i = 0; i < 20; i++)
                                {
                                    foreach (var p in PlayerManager.List.Where(x =>
                                                 !x.IsScpRole() && x != player &&
                                                 Vector3.Distance(x.Position, pos) < 4))
                                    {
                                        p.EnableEffect(EffectType.Burned, 1, 0.1f);
                                        p.Hit(player, player.Scale.x * 25 + (player.Scale.x >= 1.15f ? player.MaxHealth * 0.01f : 0));
                                    }

                                    yield return Timing.WaitForSeconds(1);
                                }

                                lava.Destroy();
                                audio.IsPaused = true;
                            }

                            Timing.RunCoroutine(Lava());
                        }

                        yield return Timing.WaitForOneFrame;
                    }
                }
            }

            void OnDied(DiedEventArgs ev)
            {
                if (ev.Attacker == null || ev.Attacker != player)
                    return;

                if (ev.Attacker.Scale.x <= 1.15f)
                    ev.Attacker.Scale += new Vector3(0.02f, 0.02f, 0.02f);
                ev.Attacker.MaxHealth *= 1.01f;
                ev.Attacker.Health *= 1.01f;
            }

            void OnDying(DyingEventArgs ev)
            {
                if (ev.Player != player)
                    return;

                Vector3 pos = ev.Player.Position;

                Timing.CallDelayed(Timing.WaitForOneFrame, () =>
                {
                    if (!ev.Player.IsDead) return;
                    if (ev.Player != player) return;
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
                });
            }

            IEnumerator<float> MakeSound()
            {
                while (true)
                {
                    var audio = Tools.PlaySound(player.Transform, "scp-457-burn");

                    yield return Timing.WaitForSeconds((float)audio.Duration.TotalSeconds);
                }
            }

            IEnumerator<float> Main()
            {
                while (true)
                {
                    foreach (var pickup in Pickup.List.Where(x => x.Type != ItemType.KeycardO5))
                    {
                        if (!(Vector3.Distance(player.Position, pickup.Position) < 3.6f)) continue;
                        if (player.Scale.x <= 1.15f)
                            player.Scale += new Vector3(0.001f * pickup.Weight, 0.001f * pickup.Weight,
                                0.001f * pickup.Weight);
                        player.MaxHealth += pickup.Weight * 1.1f;
                        player.Health += pickup.Weight * 1.1f;

                        pickup.Destroy();
                    }

                    yield return Timing.WaitForSeconds(1);
                }
            }
        }
    }
}