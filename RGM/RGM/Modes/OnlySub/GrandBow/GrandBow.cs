using System.Collections.Generic;
using Exiled.API.Features;
using MEC;
using RGM.API.Features;
using Exiled.Events.EventArgs.Player;

namespace RGM.Modes
{
    [Mode(ModeCategory.OnlySub, ModeInfo.Plus, ModeType.GrandBow)]
    public class GrandBow : Mode
    {
        public override string Name => "그랜절";
        public override string Description => "세상에 돌아간 건가, 아님 내 정신이 돌아간 걸까?";

        public override string Detail =>
            """
            Grand + 절
            """;

        public override string Color => "00FF80";


        public static GrandBow Instance;

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
            Timing.CallDelayed(Timing.WaitForOneFrame, () => { player.Scale = new UnityEngine.Vector3(-1, -1, -1); });
        }
    }
}
