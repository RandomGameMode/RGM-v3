using AFK;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Doors;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using MEC;

using PlayerRoles;
using RGM.API.Features;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Extensions;
using UnityEngine;

namespace RGM.Modes
{
    [Mode(ModeCategory.Public, ModeInfo.Set, ModeType.HideAndSeek)]
    internal class HideAndSeek : Mode
    {
        public override string Name => "숨바꼭질";
        public override string Description => "술래를 피해 특정 구역에서 제한 시간동안 버티세요!";

        public override string Detail =>
            """
            꼭꼭 숨어라~
            """;

        public override string Color => "e7c77d";

        private readonly List<Player> _finders = [];

        private CoroutineHandle _onModeStarted;

        public override void OnEnabled()
        {
            Round.IsLocked = true;
            Respawn.PauseWaves();
            AFKManager._kickTime = 120500;

            Exiled.Events.Handlers.Server.RoundEnded += OnRoundEnded;

            Exiled.Events.Handlers.Player.DroppingItem += OnDroppingItem;
            Exiled.Events.Handlers.Player.DroppingAmmo += OnDroppingAmmo;
            Exiled.Events.Handlers.Player.Shooting += OnShooting;

            _onModeStarted = Timing.RunCoroutine(OnModeStarted());
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Server.RoundEnded -= OnRoundEnded;

            Exiled.Events.Handlers.Player.DroppingItem -= OnDroppingItem;
            Exiled.Events.Handlers.Player.DroppingAmmo -= OnDroppingAmmo;
            Exiled.Events.Handlers.Player.Shooting -= OnShooting;

            Timing.KillCoroutines(_onModeStarted);
        }

        private IEnumerator<float> OnModeStarted()
        {
            foreach (var door in Door.List)
                if (new List<DoorType>
                    {
                        DoorType.CheckpointGateA,
                        DoorType.CheckpointGateB,
                        DoorType.ElevatorLczA,
                        DoorType.ElevatorLczB
                    }.Contains(door.Type))
                    door.Lock(DoorLockType.AdminCommand);
                else
                    door.IsOpen = true;

            Exiled.API.Features.Map.CleanAllItems();

            for (int i = 1; i < PlayerManager.List.Count / 6 + 2; i++)
                _finders.Add(PlayerManager.List.Where(x => !_finders.Contains(x)).ToList().GetRandomValue());

            foreach (var finder in _finders)
            {
                finder.Role.Set(RoleTypeId.Tutorial);
                finder.Role.Set(RoleTypeId.Scp049, RoleSpawnFlags.None);

                Server.ExecuteCommand($"/speak {finder.Id} 1");
            }

            foreach (var player in PlayerManager.List.Where(x => !_finders.Contains(x)))
            {
                player.Role.Set(RoleTypeId.ClassD);
                player.Scale = new Vector3(0.4f, 0.4f, 0.4f);
                player.EnableEffect(EffectType.Lightweight, 100);
                player.Position = Door.Get(DoorType.HIDLab).Position + new Vector3(0, 2, 0);

                Server.ExecuteCommand($"/speak {player.Id} 1");
            }

            for (var i = 1; i < 60; i++)
            {
                MultiBroadcast.API.MultiBroadcast.AddMapBroadcast(1,
                    $"<size=25><b><color=red>{60 - i}초 뒤 술래가 출몰합니다. 빨리 숨으세요!</color></b></size>");

                yield return Timing.WaitForSeconds(1f);
            }

            Tools.TryInstallMode(ModeType.SuperStar);

            const int remaining = 330;

            foreach (var player in PlayerManager.List.Where(x => !_finders.Contains(x)))
            {
                player.Scale = new Vector3(0.4f, 0.4f, 0.4f);
                player.EnableEffect(EffectType.SinkHole);
                player.DisableEffect(EffectType.Lightweight);
                player.EnableEffect(EffectType.HeavyFooted, 100);
                player.AddItem(ItemType.KeycardChaosInsurgency);
            }

            foreach (var finder in _finders)
            {
                finder.Role.Set(RoleTypeId.FacilityGuard);
                finder.Scale = new Vector3(0.4f, 0.4f, 0.4f);
                finder.ClearInventory();
                finder.EnableEffect(EffectType.Lightweight, 100);
                finder.EnableEffect(EffectType.MovementBoost, 50);
                foreach (var item in new List<ItemType>
                         {
                             ItemType.Radio,
                             ItemType.GunShotgun,
                             ItemType.GunRevolver
                         })
                    finder.AddItem(item);
                finder.Position = Door.Get(DoorType.HIDLab).Position + new Vector3(0, 2, 0);
                finder.IsBypassModeEnabled = true;
                finder.MaxHealth *= 5;
                finder.Health = finder.MaxHealth;
            }

            yield return Timing.WaitForSeconds(1f);

            Round.IsLocked = false;

            for (var i = 1; i < remaining; i++)
            {
                MultiBroadcast.API.MultiBroadcast.AddMapBroadcast(1,
                    $"<size=25><b><color=#2EFEF7>{remaining - i}초 뒤 술래가 패배합니다.</color></b></size>");

                if (i == remaining - 60)
                {
                    foreach (var finder in _finders)
                    {
                        finder.DisableAllEffects();
                        finder.AddEffect(EffectType.Scp1344, 1);
                        finder.AddEffect(EffectType.Lightweight, 100);
                        finder.AddEffect(EffectType.MovementBoost, 100);
                        finder.AddEffect(EffectType.NightVision, 100);
                        finder.AddEffect(EffectType.FogControl, 1);
                        finder.AddEffect(EffectType.Scp1853, 1);
                        finder.ClearInventory();
                        finder.AddItem(ItemType.GunLogicer);
                        finder.AddItem(ItemType.Radio);
                    }

                    MultiBroadcast.API.MultiBroadcast.AddMapBroadcast(10,
                        $"<size=30><color=red>버닝 타임</color>이 시작됩니다, 행운을 빕니다!</size>");
                }

                yield return Timing.WaitForSeconds(1f);
            }

            foreach (var player in Player.List.Where(x => x.Role.Type == RoleTypeId.FacilityGuard))
                player.Kill($"제한 시간 안에 생존자를 전부 죽이지 못했습니다.");
        }

        private void OnRoundEnded(RoundEndedEventArgs ev)
        {
            List<Player> players = [.. PlayerManager.List.Where(x => x.IsAlive && !x.IsNPC)];

            switch (players.Count)
            {
                case 1:
                    Timing.RunCoroutine(Tools.SetWinner([.. players], PlayerManager.List.Count / 2));
                    break;
                case > 1:
                    Timing.RunCoroutine(Tools.SetWinner([.. players], 1));
                    break;
            }
        }

        private void OnDroppingItem(DroppingItemEventArgs ev)
        {
            ev.IsAllowed = false;
        }

        private void OnDroppingAmmo(DroppingAmmoEventArgs ev)
        {
            ev.IsAllowed = false;
        }

        private void OnShooting(ShootingEventArgs ev)
        {
            ev.Player.AddAmmo(ev.Firearm.AmmoType, 1);
        }
    }
}
