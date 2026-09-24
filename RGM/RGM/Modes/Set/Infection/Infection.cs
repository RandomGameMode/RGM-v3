using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using MEC;

using PlayerRoles;
using UnityEngine;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using RGM.API.Features;

using static RGM.Variables.Variable;
using Exiled.Events.EventArgs.Player;
using Exiled.API.Features.Doors;
using Exiled.Events.EventArgs.Warhead;
using Random = UnityEngine.Random;

namespace RGM.Modes
{
    [Mode(ModeCategory.Public, ModeInfo.Set, ModeType.Infection)]
    class Infection : Mode
    {
        public override string Name => "감염";
        public override string Description => "모두를 감염시키려는 숙주와 살아남으려는 인류의 대립";
        public override string Detail =>
"""
<b>인류</b>는 무슨 수를 써서라도 9분을 버텨야 합니다.
<b><color=red>숙주</color></b>는 무슨 수를 써서라도 모든 인류를 감염시켜야 합니다.

* <b><color=red>숙주</color></b>는 일반 감염자보다 더 강력합니다.
* <color=red>SCP-079</color>는 <b><color=red>숙주</color></b>의 조력자로 탄생합니다.
""";
        public override string Color => "FF0000";

        public static Infection Instance;

        private readonly List<Player> _hostZombies = [];
        private readonly List<Player> _heroes = [];
        
        private bool _isHumanEnd = false;

        private CoroutineHandle _onModeStarted;
        private CoroutineHandle _checkEnd;

        private AudioClipPlayback _audio;

        private static readonly List<ItemType> HumanItem = [
            ItemType.KeycardScientist,
            ItemType.GunFRMG0,
            ItemType.Radio,
            ItemType.Adrenaline,
            ItemType.GrenadeHE,
            ItemType.Medkit
        ];
        
        private static readonly List<ItemType> HeroItem = [
            ItemType.KeycardFacilityManager,
            ItemType.GunLogicer,
            ItemType.SCP1509,
            ItemType.SCP500,
            ItemType.Radio,
            ItemType.Adrenaline,
            ItemType.GrenadeHE,
            ItemType.SCP500
        ];
        
        public override void OnEnabled()
        {
            Respawn.PauseWaves();
            Round.IsLocked = true;

            Exiled.Events.Handlers.Warhead.Detonating += OnDetonating;
            Exiled.Events.Handlers.Warhead.Stopping += OnStopping;

            Exiled.Events.Handlers.Player.Verified += OnVerified;
            Exiled.Events.Handlers.Player.Spawned += OnSpawned;
            Exiled.Events.Handlers.Player.InteractingDoor += OnInteractingDoor;
            Exiled.Events.Handlers.Player.Died += OnDied;
            Exiled.Events.Handlers.Player.Hurting += OnHurting;

            _onModeStarted = Timing.RunCoroutine(OnModeStarted());
            _checkEnd = Timing.RunCoroutine(CheckEnd());
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Warhead.Detonating -= OnDetonating;
            Exiled.Events.Handlers.Warhead.Stopping -= OnStopping;

            Exiled.Events.Handlers.Player.Verified -= OnVerified;
            Exiled.Events.Handlers.Player.Spawned -= OnSpawned;
            Exiled.Events.Handlers.Player.InteractingDoor -= OnInteractingDoor;
            Exiled.Events.Handlers.Player.Died -= OnDied;
            Exiled.Events.Handlers.Player.Hurting -= OnHurting;

            Timing.KillCoroutines(_onModeStarted);
            Timing.KillCoroutines(_checkEnd);

            _audio.IsPaused = true;
        }

        private IEnumerator<float> OnModeStarted()
        {
            if (Random.Range(1, 101) <= 10) { //10% 확률로 워크스테이션 업그레이드 시작
                Tools.TryInstallMode(ModeType.ABattle);
            }
            _audio = Tools.PlayGlobalAudio("Voices", 0.3f, true);

            for (int i = 0; i < Mathf.Max(1, PlayerManager.List.Count / 5); i++)
            {
                Player hostZombie = PlayerManager.List.Where(x => x.IsAlive && !_hostZombies.Contains(x) && !_heroes.Contains(x))
                    .ToList()
                    .GetRandomValue();

                _hostZombies.Add(hostZombie);

                Timing.CallDelayed(1, () =>
                {
                    hostZombie.Role.Set(RoleTypeId.Scp0492);
                });
            }

            for (int i = 0; i < Mathf.Max(1, PlayerManager.List.Count / 5); i++)
            {
                Player heroes = PlayerManager.List.Where(x => x.IsAlive && !_hostZombies.Contains(x) && !_heroes.Contains(x))
                    .ToList()
                    .GetRandomValue();
                
                _heroes.Add(heroes);
            }

            foreach (var player in PlayerManager.List)
            {
                try
                {
                    if (_hostZombies.Contains(player)) continue;
                    if (_heroes.Contains(player))
                    {
                        player.Role.Set(RoleTypeId.NtfCaptain, RoleSpawnFlags.None);
                        player.ClearInventory();
                        foreach (var heroitems in HeroItem)
                        {
                            player.AddItem(heroitems);
                        }
                        player.AddItem(ItemType.Ammo762x39, 30);

                        player.MaxHealth += 100;
                        player.Health = player.MaxHealth;
                        player.EnableEffect(EffectType.MovementBoost, 10);
                        player.EnableEffect(EffectType.Lightweight, 10);
                        player.EnableEffect(EffectType.Scp1344);
                        player.Scale = new Vector3(0.92f, 0.92f, 0.92f);
                    }
                    else
                    {
                        player.Role.Set(RoleTypeId.FacilityGuard, RoleSpawnFlags.None);
                        player.ClearInventory();
                        foreach (var humanitems in HumanItem)
                        {
                            player.AddItem(humanitems);
                        }
                        player.AddItem(ItemType.Ammo556x45, 15);
                    }
                }
                catch (Exception ex)
                {
                    player.AddHint("에러", $"Error: {ex}");
                }
            }

            for (int i = 0; i < 540; i++)
            {
                MultiBroadcast.API.MultiBroadcast.AddMapBroadcast(1, $"<b><size=25>{540 - i}초 뒤 인류가 승리합니다.</size></b>");

                yield return Timing.WaitForSeconds(1);
            }

            if (!Round.IsEnded) 
            {
                Round.IsLocked = false;
                _isHumanEnd = true;
                Timing.RunCoroutine(Tools.SetWinner(
                    PlayerManager.List.Where(x => x.IsHuman).ToList(), 
                    PlayerManager.List.Count(x => x.IsHuman) == 1 
                        ? PlayerManager.List.Count : 1));

                foreach (var player in PlayerManager.List)
                {
                    player.AddBroadcast(20, $"<size=30><b>인류의 승리입니다. <color=#9AFE2E>좀비들은 해독제를 맞고 치료되었습니다.</color></b></size>");

                    if (player.Role.Type == RoleTypeId.Scp0492)
                        player.Role.Set(RoleTypeId.Tutorial, RoleSpawnFlags.None);
                }
            }
        }

        private static IEnumerator<float> CheckEnd()
        {
            while (!Round.IsEnded)
            {
                if (PlayerManager.List.Count(x => x.IsHuman) < 1)
                {
                    Round.IsLocked = false;
                    Timing.RunCoroutine(Tools.SetWinner(PlayerManager.List.Where(x => x.Role.Type == RoleTypeId.Scp0492).ToList(), 1));

                    MultiBroadcast.API.MultiBroadcast.AddMapBroadcast(20, $"<size=30><b>숙주의 승리입니다. <color=red>남겨진 인류는 안타까운 결말을 맞이할 것입니다.</color></b></size>");

                    yield break;
                }

                yield return Timing.WaitForSeconds(1);
            }
        }

        private void OnDetonating(DetonatingEventArgs ev)
        {
            Door door = Door.Get(DoorType.NukeSurface);

            foreach (var player in PlayerManager.List.Where(x => x.IsScpRole()))
            {
                player.Position = door.Position + new Vector3(0, 2, 0);
            }
        }

        private static void OnVerified(VerifiedEventArgs ev)
        {
            ev.Player.Kill("동료들과 함께 인간을 섬멸하십시오.");
        }

        private void OnSpawned(SpawnedEventArgs ev)
        {
            Timing.CallDelayed(Timing.WaitForOneFrame, () =>
            {
                ev.Player.EnableEffect(EffectType.FogControl, 12);

                if (_hostZombies.Contains(ev.Player))
                {
                    ev.Player.MaxHealth = 666;
                    ev.Player.MaxHumeShield = 166;
                    ev.Player.Health = ev.Player.MaxHealth;
                    ev.Player.HumeShield = ev.Player.MaxHumeShield;
                    ev.Player.AddEffect(EffectType.MovementBoost, 5);
                    ev.Player.AddEffect(EffectType.Lightweight, 10);
                    ev.Player.Scale = new Vector3(0.9f, 0.9f, 0.9f);
                    ev.Player.IsBypassModeEnabled = true;
                }

                if (ev.Player.Role.Type != RoleTypeId.Scp079) return;
                
                if (!_hostZombies.Contains(ev.Player))
                    _hostZombies.Add(ev.Player);

                if (GodModePlayers.Contains(ev.Player))
                    GodModePlayers.Remove(ev.Player);

                ev.Player.Kill("숙주 좀비가 될 것입니다.");
            });
        }

        private void OnInteractingDoor(InteractingDoorEventArgs ev)
        {
            if (!_hostZombies.Contains(ev.Player)) return;
            if (ev.Door is BreakableDoor door)
                door.Break();
        }

        private IEnumerator<float> OnDied(DiedEventArgs ev)
        {
            for (int i = 0; i < 5; i++)
            {
                ev.Player.ShowHint($"<size=25>{5 - i}초 뒤 <color=red>동료</color> 근처에서 부활합니다.</size>", 1.2f);

                yield return Timing.WaitForSeconds(1f);
            }

            if (!_isHumanEnd)
            {
                ev.Player.Role.Set(RoleTypeId.Scp0492);
                ev.Player.MaxHealth = 444;
                ev.Player.MaxHumeShield = 111;
                ev.Player.Health = ev.Player.MaxHealth;
                ev.Player.HumeShield = ev.Player.MaxHumeShield;

                try
                {
                    List<Player> zombies = [.. PlayerManager.List.Where(x => x.Role.Type == RoleTypeId.Scp0492)];

                    ev.Player.Position = zombies.Count < 1
                        ? PlayerManager.List.Where(x => x.Role.Type is RoleTypeId.NtfCaptain or RoleTypeId.FacilityGuard).Select(x => x.Position)
                            .ToList().GetRandomValue()
                        : zombies.Select(x => x.Position).ToList().GetRandomValue();
                }
                catch (Exception)
                {
                    ev.Player.Position = PlayerManager.List.Where(x => x.Role.Type is RoleTypeId.NtfCaptain or RoleTypeId.FacilityGuard)
                        .Select(x => x.Position).ToList().GetRandomValue();
                }
            }
            else ev.Player.Role.Set(RoleTypeId.Tutorial, RoleSpawnFlags.None);
        }

        private void OnHurting(HurtingEventArgs ev)
        {
            if (ev.Attacker != null && _hostZombies.Contains(ev.Player))
                ev.DamageHandler.Damage += 30;
        }

        private static void OnStopping(StoppingEventArgs ev)
        {
            if (ev.Player.Role != RoleTypeId.Scp0492 || ev.Player.IsNPC) return;
            
            ev.IsAllowed = false;
        }
    }
}
