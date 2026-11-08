using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Warhead;
using MEC;
using Mirror;

using PlayerRoles;
using RGM.API.Features;
using UnityEngine;
using Warhead = Exiled.Events.Handlers.Warhead;

namespace RGM.Modes
{
    [Mode(ModeCategory.Private, ModeInfo.Set, ModeType.SpeedRun)]
    class SpeedRun : Mode
    {
        public override string Name => "스피드런";
        public override string Description => "누구보다 빠르게 시설에서 탈출하세요. 아, 그리고 핵을 중지하려 시도하지 마세요!";

        public override string Detail =>
            """
            아이템이 <color=#C8FE2E>랜덤</color>하게 지급됩니다.

            모든 방법을 총동원하여 1등으로 시설에서 탈출하세요.
            그것 뿐입니다.
            """;

        public override string Color => "58FAAC";

        public static SpeedRun Instance;

        private readonly List<Player> _pl = [];

        private readonly List<ItemType> _standards =
        [
            ItemType.Flashlight
        ];

        private readonly List<ItemType> _extensions =
        [
            ItemType.KeycardO5,
            ItemType.Adrenaline,
            ItemType.Coin,
            ItemType.Lantern,
            ItemType.Medkit,
            ItemType.Painkillers
        ];

        private readonly List<ItemType> _itemsList = [];

        private bool _isEnd = false;

        private CoroutineHandle _onModeStarted;

        public override void OnEnabled()
        {
            Round.IsLocked = true;

            Exiled.Events.Handlers.Player.Spawned += OnSpawned;
            Exiled.Events.Handlers.Player.Dying += OnDying;
            Exiled.Events.Handlers.Player.Escaping += OnEscaping;
            Warhead.Stopping += OnStopping;

            _onModeStarted = Timing.RunCoroutine(OnModeStarted());
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Player.Spawned -= OnSpawned;
            Exiled.Events.Handlers.Player.Dying -= OnDying;
            Exiled.Events.Handlers.Player.Escaping -= OnEscaping;

            Warhead.Stopping -= OnStopping;

            Timing.KillCoroutines(_onModeStarted);
        }

        private IEnumerator<float> OnModeStarted()
        {
            PlayerManager.List.ToList().CopyTo(_pl);

            Exiled.API.Features.Warhead.Start();

            foreach (var item in _standards)
                _itemsList.Add(item);

            foreach (var item in _extensions)
                if (Random.Range(1, 3) == 1)
                    _itemsList.Add(item);

            Tools.TryInstallMode(ModeType.FriendlyFire);

            foreach (var player in PlayerManager.List)
                Spawned(player);

            yield break;
        }

        private void OnSpawned(SpawnedEventArgs ev)
        {
            Spawned(ev.Player);
        }

        private void Spawned(Player player)
        {
            if (!_pl.Contains(player)) return;
            if (player.Role.Type == RoleTypeId.ChaosConscript)
                return;

            if (player.Role.Type != RoleTypeId.ClassD)
                player.Role.Set(RoleTypeId.ClassD, RoleSpawnFlags.All);

            Timing.CallDelayed(0.1f, () =>
            {
                foreach (var item in _itemsList)
                    player.AddItem(item);
            });
        }

        private void OnDying(DyingEventArgs ev)
        {
            if (!_pl.Contains(ev.Player)) return;
            _pl.Remove(ev.Player);

            if (_pl.Count >= 2) return;
            if (_isEnd) return;
            Round.IsLocked = false;

            PlayerManager.List.ToList().ForEach(x =>
                x.AddBroadcast(20, $"<b><size=30><생존자 : {_pl[0].DisplayNickname}></size></b>"));
            Timing.RunCoroutine(Tools.SetWinner([_pl[0]], 5));
            _isEnd = true;
        }

        private void OnEscaping(EscapingEventArgs ev)
        {
            if (ev.Player.Role.Type != RoleTypeId.ClassD) return;
            if (_isEnd) return;
            Round.IsLocked = false;

            PlayerManager.List.ToList().ForEach(x =>
                x.AddBroadcast(20, $"<b><size=30><최초 탈출자 : {ev.Player.DisplayNickname}></size></b>"));
            Timing.RunCoroutine(Tools.SetWinner([ev.Player], 5));
            _isEnd = true;
        }

        private void OnStopping(StoppingEventArgs ev)
        {
            PlayerManager.List.ToList().ForEach(x => x.AddBroadcast(20,
                $"<size=30>{ev.Player.DisplayNickname}(으)로 인해 20초 뒤 <color=red>핵</color>이 강제로 폭파됩니다.</size>"));

            Timing.CallDelayed(20, Exiled.API.Features.Warhead.Detonate);
        }
    }
}
