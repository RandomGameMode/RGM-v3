using System.Collections.Generic;
using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.API.Features.Roles;
using Exiled.Events.EventArgs.Item;
using Exiled.Events.EventArgs.Player;
using InventorySystem.Items.MicroHID.Modules;
using MEC;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp096;
using RGM.API.Features;
using UnityEngine;
using Scp096Role = Exiled.API.Features.Roles.Scp096Role;

namespace RGM.Modes;

[Mode(ModeCategory.Public, ModeInfo.Plus, ModeType.Unlimited)]
internal class Unlimited : Mode
{
    public override string Name => "무제한";
    public override string Description => "말 그대로 제한이 없습니다.";

    public override string Detail =>
        """
        몇몇 개의 기능들은 제한이 있습니다.
        """;

    public override string Color => "3F13AB";

    public static Unlimited Instance;

    private int _tantrum;

    private CoroutineHandle _onModeStarted;

    private const float ChargingJailbirdDuration = 5f;

    private static readonly Dictionary<ushort, float> ChargingJailbirds = [];

    public override void OnEnabled()
    {
        Exiled.Events.Handlers.Player.Spawned += OnSpawned;
        Exiled.Events.Handlers.Player.Healing += OnHealing;
        Exiled.Events.Handlers.Player.UsingItem += OnUsingItem;
        Exiled.Events.Handlers.Player.UsingRadioBattery += OnUsingRadioBattery;
        Exiled.Events.Handlers.Player.Shooting += OnShooting;
        Exiled.Events.Handlers.Player.ChangingMicroHIDState += OnChangingMicroHIDState;
        Exiled.Events.Handlers.Player.UsingMicroHIDEnergy += OnUsingMicroHIDEnergy;
        Exiled.Events.Handlers.Player.Hurting += OnHurting;

        Exiled.Events.Handlers.Scp106.Teleporting += OnTeleporting;
        Exiled.Events.Handlers.Scp106.Stalking += OnStalking;
        Exiled.Events.Handlers.Scp106.Attacking += OnScp106Attacking;

        Exiled.Events.Handlers.Scp939.PlayingSound += OnPlayingSound;

        Exiled.Events.Handlers.Scp079.ChangingCamera += OnChangingCamera;
        Exiled.Events.Handlers.Scp079.Pinging += OnPinging;

        Exiled.Events.Handlers.Scp049.StartingRecall += OnStartingRecall;
        Exiled.Events.Handlers.Scp049.Attacking += OnScp049Attacking;

        Exiled.Events.Handlers.Scp096.Enraging += OnEnraging;

        Exiled.Events.Handlers.Scp173.PlacingTantrum += OnPlacingTantrum;
        Exiled.Events.Handlers.Scp173.UsingBreakneckSpeeds += OnUsingBreakneckSpeeds;

        Exiled.Events.Handlers.Item.ChargingJailbird += OnChargingJailbird;

        Timing.RunCoroutine(OnModeStarted());
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Player.Spawned -= OnSpawned;
        Exiled.Events.Handlers.Player.Healing -= OnHealing;
        Exiled.Events.Handlers.Player.UsingItem -= OnUsingItem;
        Exiled.Events.Handlers.Player.UsingRadioBattery -= OnUsingRadioBattery;
        Exiled.Events.Handlers.Player.Shooting -= OnShooting;
        Exiled.Events.Handlers.Player.ChangingMicroHIDState -= OnChangingMicroHIDState;
        Exiled.Events.Handlers.Player.UsingMicroHIDEnergy -= OnUsingMicroHIDEnergy;
        Exiled.Events.Handlers.Player.Hurting -= OnHurting;

        Exiled.Events.Handlers.Scp106.Teleporting -= OnTeleporting;
        Exiled.Events.Handlers.Scp106.Stalking -= OnStalking;
        Exiled.Events.Handlers.Scp106.Attacking -= OnScp106Attacking;

        Exiled.Events.Handlers.Scp939.PlayingSound -= OnPlayingSound;

        Exiled.Events.Handlers.Scp079.ChangingCamera -= OnChangingCamera;
        Exiled.Events.Handlers.Scp079.Pinging -= OnPinging;

        Exiled.Events.Handlers.Scp049.StartingRecall -= OnStartingRecall;
        Exiled.Events.Handlers.Scp049.Attacking -= OnScp049Attacking;

        Exiled.Events.Handlers.Scp096.Enraging -= OnEnraging;

        Exiled.Events.Handlers.Scp173.PlacingTantrum -= OnPlacingTantrum;
        Exiled.Events.Handlers.Scp173.UsingBreakneckSpeeds -= OnUsingBreakneckSpeeds;

        Exiled.Events.Handlers.Item.ChargingJailbird -= OnChargingJailbird;

        Timing.KillCoroutines(_onModeStarted);
        ChargingJailbirds.Clear();
    }

    private static IEnumerator<float> OnModeStarted()
    {
        if (Random.Range(1, 101) <= 10)
        {
            //10% 확률로 워크스테이션 업그레이드 시작
            Tools.TryInstallMode(ModeType.ABattle);
        }

        PlayerManager.List.ToList().ForEach(Spawned);

        while (true)
        {
            foreach (var player in PlayerManager.List)
            {
                switch (player.Role)
                {
                    case Scp049Role scp049:
                    {
                        if (scp049.IsAlive)
                            scp049.HumeShieldModule.HsStat.MaxValue = player.MaxHealth * 0.5f;

                        if (scp049.CallCooldown > 0)
                            scp049.CallCooldown = 0;

                        if (scp049.GoodSenseCooldown > 0)
                            scp049.GoodSenseCooldown = 0;

                        if (scp049.RemainingAttackCooldown > 0)
                            scp049.RemainingAttackCooldown = 0;
                        break;
                    }
                    case Scp106Role scp106:
                    {
                        if (scp106.IsAlive)
                            scp106.HumeShieldModule.HsStat.MaxValue = player.MaxHealth * 0.5f;

                        if (scp106.CaptureCooldown > 0)
                            scp106.CaptureCooldown = 0;

                        if (scp106.RemainingSinkholeCooldown > 0)
                            scp106.RemainingSinkholeCooldown = 0;
                        break;
                    }
                    case Scp173Role scp173:
                    {
                        if (scp173.IsAlive)
                            scp173.HumeShieldModule.HsStat.MaxValue = player.MaxHealth * 0.5f;

                        if (scp173.BlinkCooldown > 1f)
                            scp173.BlinkCooldown = 1f;

                        if (scp173.RemainingBreakneckCooldown > 1f)
                            scp173.RemainingBreakneckCooldown = 1f;
                        break;
                    }
                    case Scp096Role scp096:
                    {
                        if (scp096.IsAlive)
                            scp096.HumeShieldModule.HsStat.MaxValue = player.MaxHealth * 0.5f;

                        if (scp096.EnrageCooldown > 0)
                            scp096.EnrageCooldown = 0;

                        if (scp096.ChargeCooldown > 0)
                            scp096.ChargeCooldown = 0;
                        break;
                    }
                    case Scp939Role scp939:
                    {
                        if (scp939.IsAlive)
                            scp939.HumeShieldModule.HsStat.MaxValue = player.MaxHealth * 0.5f;

                        if (scp939.MimicryCooldown > 0)
                            scp939.MimicryCooldown = 0;

                        if (scp939.AmnesticCloudCooldown > 0)
                            scp939.AmnesticCloudCooldown = 0;

                        if (scp939.AttackCooldown > 0)
                            scp939.AttackCooldown = 0;
                        break;
                    }
                    case Scp079Role scp079:
                    {
                        if (scp079.BlackoutZoneCooldown > 0)
                            scp079.BlackoutZoneCooldown = 0;

                        if (scp079.RoomLockdownCooldown > 0)
                            scp079.RoomLockdownCooldown = 0;

                        if (scp079.PingAbility._instantCooldown > 0)
                            scp079.PingAbility._instantCooldown = 0;

                        if (scp079.PingAbility != null)
                        {
                            var field = typeof(PlayerRoles.PlayableScps.Scp079.Pinging.Scp079PingAbility)
                                .GetField("_rateLimiter",
                                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic |
                                    System.Reflection.BindingFlags.Public);

                            if (field != null)
                            {
                                field.SetValue(scp079.PingAbility, new RateLimiter(0f));
                            }
                        }

                        break;
                    }
                    case Scp0492Role scp0492:
                    {
                        if (scp0492.IsAlive)
                            scp0492.HumeShieldModule.HsStat.MaxValue = player.MaxHealth * 0.5f;
                        break;
                    }
                    case Scp3114Role scp3114:
                    {
                        if (scp3114.IsAlive)
                            scp3114.HumeShieldModule.HsStat.MaxValue = player.MaxHealth * 0.5f;
                        break;
                    }
                }
            }

            yield return Timing.WaitForSeconds(0.125f);
        }
    }

    private static void OnSpawned(SpawnedEventArgs ev)
    {
        Spawned(ev.Player);
    }

    private static void Spawned(Player player)
    {
        if (!player.IsAlive) return;
        player.IsUsingStamina = false;

        if (player.Role.Type == RoleTypeId.Scp0492)
            player.MaxHealth += 100;
    }

    private static void OnHealing(HealingEventArgs ev)
    {
        ev.Player.MaxHealth += ev.Amount;
    }

    private static IEnumerator<float> OnUsingItem(UsingItemEventArgs ev)
    {
        yield return Timing.WaitForOneFrame;

        ev.Cooldown = 0.1f;
    }

    private static void OnUsingRadioBattery(UsingRadioBatteryEventArgs ev)
    {
        ev.Drain = 0;
    }

    private static IEnumerator<float> OnTeleporting(Exiled.Events.EventArgs.Scp106.TeleportingEventArgs ev)
    {
        yield return Timing.WaitForOneFrame;

        ev.Scp106.RemainingSinkholeCooldown = 0;
    }

    private static IEnumerator<float> OnStalking(Exiled.Events.EventArgs.Scp106.StalkingEventArgs ev)
    {
        yield return Timing.WaitForOneFrame;

        ev.Scp106.RemainingSinkholeCooldown = 0;
    }

    private static IEnumerator<float> OnScp106Attacking(Exiled.Events.EventArgs.Scp106.AttackingEventArgs ev)
    {
        yield return Timing.WaitForOneFrame;

        ev.Scp106.CaptureCooldown = 0;
    }

    private static IEnumerator<float> OnPlayingSound(Exiled.Events.EventArgs.Scp939.PlayingSoundEventArgs ev)
    {
        yield return Timing.WaitForOneFrame;

        ev.Scp939.MimicryCooldown = 0;
    }

    private static void OnChangingCamera(Exiled.Events.EventArgs.Scp079.ChangingCameraEventArgs ev)
    {
        ev.Scp079.Energy = short.MaxValue;
    }

    private static void OnPinging(Exiled.Events.EventArgs.Scp079.PingingEventArgs ev)
    {
        ev.Scp079.Energy = short.MaxValue;
    }

    private static IEnumerator<float> OnStartingRecall(Exiled.Events.EventArgs.Scp049.StartingRecallEventArgs ev)
    {
        yield return Timing.WaitForOneFrame;

        ev.Scp049.RemainingCallDuration = 0;
    }

    private static IEnumerator<float> OnScp049Attacking(Exiled.Events.EventArgs.Scp049.AttackingEventArgs ev)
    {
        yield return Timing.WaitForOneFrame;

        ev.Scp049.RemainingAttackCooldown = 0;
        ev.Scp049.RemainingGoodSenseDuration = 0;
    }

    private static IEnumerator<float> OnEnraging(Exiled.Events.EventArgs.Scp096.EnragingEventArgs ev)
    {
        yield return Timing.WaitForOneFrame;

        ev.Scp096.EnrageCooldown = 0;
        ev.Scp096.EnragedTimeLeft = 9999;
        ev.Scp096.SprintingSpeed = 500;
        ev.Scp096.RageManager.HumeShieldBlocked = false;
    }

    private IEnumerator<float> OnPlacingTantrum(Exiled.Events.EventArgs.Scp173.PlacingTantrumEventArgs ev)
    {
        if (_tantrum >= 10)
        {
            ev.Player.AddHint("무제한 땅콩 똥 제한", $"렉 방지를 위해 10개로 제한됩니다. (하나 당 180초)", 1);
            ev.IsAllowed = false;
        }
        else
        {
            _tantrum += 1;
            yield return Timing.WaitForOneFrame;
            ev.Cooldown.Remaining = 0;
            yield return Timing.WaitForSeconds(180f);
            _tantrum -= 1;
        }
    }

    private static IEnumerator<float> OnUsingBreakneckSpeeds(
        Exiled.Events.EventArgs.Scp173.UsingBreakneckSpeedsEventArgs ev)
    {
        yield return Timing.WaitForSeconds(1f);

        ev.Scp173.RemainingBreakneckCooldown = 0;
    }

    private static void OnShooting(ShootingEventArgs ev)
    {
        ev.Player.CurrentItem.As<Firearm>().MagazineAmmo = 101;
    }

    private static void OnChangingMicroHIDState(ChangingMicroHIDStateEventArgs ev)
    {
        if (ev.NewPhase == MicroHidPhase.WindingUp)
            ev.NewPhase = MicroHidPhase.Firing;
        ev.MicroHID.Energy = 1f;
    }

    private static void OnUsingMicroHIDEnergy(UsingMicroHIDEnergyEventArgs ev)
    {
        ev.MicroHID.Energy = 1f;
    }

    private static void OnHurting(HurtingEventArgs ev)
    {
        if (ev.DamageHandler.Type == DamageType.Explosion)
            return;

        if (ev.Attacker == null)
            return;

        if (ev.Attacker.CurrentItem is not Jailbird jailbird ||
            !ChargingJailbirds.TryGetValue(jailbird.Serial, out var expiresAt))
            return;

        if (Time.time > expiresAt)
        {
            ChargingJailbirds.Remove(jailbird.Serial);
            return;
        }

        ChargingJailbirds.Remove(jailbird.Serial);

        if (HitboxIdentity.IsEnemy(ev.Attacker.ReferenceHub, ev.Player.ReferenceHub))
        {
            jailbird.TotalDamageDealt = 0;

            var g = (ExplosiveGrenade)Item.Create(ItemType.GrenadeHE, ev.Player);
            g.FuseTime = 0.1f;
            g.MaxRadius = 0f;
            g.SpawnActive(ev.Player.Position, ev.Attacker);
        }
    }

    private static void OnChargingJailbird(ChargingJailbirdEventArgs ev)
    {
        if (ev.Item == null || ev.Player == null) return;

        var jailbird = ev.Item.As<Jailbird>();
        jailbird.TotalCharges = 0;
        ChargingJailbirds[jailbird.Serial] = Time.time + ChargingJailbirdDuration;
    }
}