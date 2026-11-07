using System.Collections.Generic;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using MEC;
using RGM.API.Features;
using Exiled.API.Enums;

namespace RGM.Modes
{
    [Mode(ModeCategory.OnlySub, ModeInfo.Plus, ModeType.Clairvoyance)]
    internal class Clairvoyance : Mode
    {
        public override string Name => "야간 작전";
        public override string Description => "모두가 SCP-1344 효과를 받습니다.";
        public override string Detail => "SCP-1344 효과 - 투시";
        public override string Color => "F4FA58";

        public static Clairvoyance Instance;

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

        private static void OnSpawned(SpawnedEventArgs ev)
        {
            Spawned(ev.Player);
        }

        private static void Spawned(Player player)
        {
            if (player.IsAlive)
                Timing.CallDelayed(Timing.WaitForOneFrame, () => player.EnableEffect(EffectType.Scp1344));
        }
    }
}
