using System.Collections.Generic;
using Exiled.API.Features;
using MEC;
using RGM.API.Features;
using Exiled.Events.EventArgs.Player;
using Exiled.API.Enums;

namespace RGM.Modes
{
    [Mode(ModeCategory.OnlySub, ModeInfo.Plus, ModeType.Moon)]
    public class Moon : Mode
    {
        public override string Name => "달";
        public override string Description => "점프력이 증가합니다.";

        public override string Detail =>
            "점프력이 증가하며, 낙사 데미지를 받지 않습니다.";

        public override string Color => "d1d6c6";

        private CoroutineHandle _onModeStarted;

        public override void OnEnabled()
        {
            Exiled.Events.Handlers.Player.Spawned += OnSpawned;

            _onModeStarted = Timing.RunCoroutine(OnModeStarted());
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Player.Spawned -= OnSpawned;

            Timing.KillCoroutines(_onModeStarted);
        }

        private IEnumerator<float> OnModeStarted()
        {
            foreach (var player in PlayerManager.List) Spawned(player);

            yield break;
        }

        private void OnSpawned(SpawnedEventArgs ev)
        {
            Spawned(ev.Player);
        }

        private void Spawned(Player player)
        {
            if (player.IsAlive) player.EnableEffect(EffectType.Lightweight, 255);
        }
    }
}
