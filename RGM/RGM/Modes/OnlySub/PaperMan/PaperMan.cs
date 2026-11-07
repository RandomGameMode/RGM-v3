using System.Collections.Generic;
using Exiled.API.Features;
using MEC;
using RGM.API.Features;
using UnityEngine;

namespace RGM.Modes
{
    [Mode(ModeCategory.OnlySub, ModeInfo.Plus, ModeType.PaperMan)]
    internal class PaperMan : Mode
    {
        public override string Name => "종이 인간";
        public override string Description => "종이처럼 펄럭펄럭";

        public override string Detail =>
            """
            xㅣ0.01
            yㅣ1
            zㅣ1
            의 몸을 가졌습니다!
            """;

        public override string Color => "F5A9BC";

        public static PaperMan Instance;

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

        private void OnSpawned(Exiled.Events.EventArgs.Player.SpawnedEventArgs ev)
        {
            Spawned(ev.Player);
        }

        private void Spawned(Player player)
        {
            Timing.CallDelayed(1f, () => { player.Scale = new Vector3(0.01f, 1, 1f); });
        }
    }
}
