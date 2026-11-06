using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using MEC;
using UnityEngine;
using Exiled.API.Enums;
using PlayerRoles;
using RGM.API.Features;
using Mirror;
using static RGM.Variables.Variable;
using Exiled.API.Features.Doors;
using Exiled.Events.EventArgs.Player;

namespace RGM.Modes
{
    [Mode(ModeCategory.Private, ModeInfo.Set, ModeType.TailCatcher)]
    public class TailCatcter : Mode
    {
        public override string Name => "꼬리 잡기";
        public override string Description => "절대로 타깃이 아닌 유저를 쏘지 마세요! 꼬리가 잡히지 않도록 하십시오.";

        public override string Detail =>
            """
            <color=red>꼬리가 아닌 유저를 공격하면 데미지가 반사됩니다.</color>

            눈치 게임과 비슷하군요!
            """;

        public override string Color => "A9F5BC";
        public override string Map => "hp";

        public static TailCatcter Instance;

        private readonly List<Player> _pl = [];

        private CoroutineHandle _onModeStarted;

        private AudioClipPlayback _audio;

        private Player GetTarget(Player attacker)
        {
            int playerIndex = _pl.IndexOf(attacker);
            int targetIndex = (playerIndex + 1) % _pl.Count;

            return _pl[targetIndex];
        }

        public override void OnEnabled()
        {
            Server.FriendlyFire = true;
            Round.IsLocked = true;
            Respawn.PauseWaves();

            Exiled.Events.Handlers.Player.InteractingDoor += OnInteractingDoor;
            Exiled.Events.Handlers.Player.Spawned += OnSpawned;
            Exiled.Events.Handlers.Player.DroppingItem += OnDroppingItem;
            Exiled.Events.Handlers.Player.DroppingAmmo += OnDroppingAmmo;
            Exiled.Events.Handlers.Player.Shot += OnShot;
            Exiled.Events.Handlers.Player.Hurting += OnHurting;
            Exiled.Events.Handlers.Player.Dying += OnDying;

            _onModeStarted = Timing.RunCoroutine(OnModeStarted());

            _audio = Tools.PlayGlobalAudio("Initial-D-Dancing", 1, true);
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Player.InteractingDoor += OnInteractingDoor;
            Exiled.Events.Handlers.Player.Spawned += OnSpawned;
            Exiled.Events.Handlers.Player.DroppingItem += OnDroppingItem;
            Exiled.Events.Handlers.Player.DroppingAmmo += OnDroppingAmmo;
            Exiled.Events.Handlers.Player.Shot += OnShot;
            Exiled.Events.Handlers.Player.Hurting += OnHurting;
            Exiled.Events.Handlers.Player.Dying += OnDying;

            Timing.KillCoroutines(_onModeStarted);

            _audio.IsPaused = true;
        }

        private IEnumerator<float> OnModeStarted()
        {
            PlayerManager.List.Where(x => !x.IsNPC).CopyTo(_pl);

            foreach (var door in Door.List)
            {
                door.Lock(DoorLockType.Warhead);
            }

            foreach (var player in PlayerManager.List.Where(x => !x.IsNPC))
            {
                player.Role.Set(RoleTypeId.ClassD);
                player.Position = GameObject.Find("[SP] Base").transform.position;
                player.AddItem(ItemType.GunCrossvec);
            }

            _pl.ShuffleList();

            while (true)
            {
                foreach (var player in PlayerManager.List.Where(_pl.Contains))
                {
                    Player target = GetTarget(player);

                    player.AddHint("꼬리 잡기 타겟 알림", $"당신의 타깃 : {target.DisplayNickname}", 1.2f);
                }

                yield return Timing.WaitForSeconds(1f);
            }
        }

        private void OnInteractingDoor(InteractingDoorEventArgs ev)
        {
            ev.IsAllowed = false;
        }

        private void OnSpawned(SpawnedEventArgs ev)
        {
            Server.ExecuteCommand($"/speak {ev.Player.Id} 1");
            IntercomPlayers.Add(ev.Player);
        }

        private void OnDroppingItem(DroppingItemEventArgs ev)
        {
            ev.IsAllowed = false;
        }

        private void OnDroppingAmmo(DroppingAmmoEventArgs ev)
        {
            ev.IsAllowed = false;
        }

        private void OnShot(ShotEventArgs ev)
        {
            ev.Firearm.MagazineAmmo += 1;
        }

        private void OnHurting(HurtingEventArgs ev)
        {
            Player target = GetTarget(ev.Attacker);

            if (ev.Player == target) return;
            ev.Attacker.Hurt(ev.DamageHandler.Damage);

            ev.IsAllowed = false;
        }

        private void OnDying(DyingEventArgs ev)
        {
            if (!_pl.Contains(ev.Player)) return;
            _pl.Remove(ev.Player);

            if (_pl.Count >= 2) return;
            Round.IsLocked = false;

            PlayerManager.List.ToList().ForEach(x => x.AddBroadcast(20, $"승리자 : {_pl[0].DisplayNickname}"));
            Timing.RunCoroutine(Tools.SetWinner([_pl[0]], 5));
        }
    }
}