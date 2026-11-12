using Exiled.API.Features;
using Exiled.Events.EventArgs.Scp1509;
using MEC;
using System.Collections.Generic;
using RGM.API.Features;

namespace RGM.Modes
{
    [Mode(ModeCategory.Private, ModeInfo.Set, ModeType.Develop)]
    internal class Develop : Mode
    {
        public override string Name => "필독";
        public override string Description => "[📃 전체 모드] 드롭다운을 활용하여 모드 설명을 읽어 보세요!";

        public override string Detail =>
            """
            여기는 자세한 설명이 쓰이는 자리입니다.
            """;

        public override string Color => "FFFFFF";

        public static Develop Instance;

        private CoroutineHandle _onModeStarted;

        public override void OnEnabled()
        {
            Round.IsLocked = true;
            Respawn.PauseWaves();
            Exiled.API.Features.Map.IsDecontaminationEnabled = false;

            Exiled.Events.Handlers.Scp1509.Resurrecting += OnResurrecting;
            Exiled.Events.Handlers.Player.Kicking += OnKicking;

            _onModeStarted = Timing.RunCoroutine(OnModeStarted());
            Tools.TryInstallMode(ModeType.SuperStar);
        }

        public override void OnDisabled()
        {
            Round.IsLocked = false;
            Respawn.ResumeWaves();
            Exiled.API.Features.Map.IsDecontaminationEnabled = true;

            Exiled.Events.Handlers.Scp1509.Resurrecting -= OnResurrecting;
            Exiled.Events.Handlers.Player.Kicking -= OnKicking;

            Timing.KillCoroutines(_onModeStarted);
            Tools.UnInstallMode(ModeType.SuperStar);
        }

        private IEnumerator<float> OnModeStarted()
        {
            yield return 0f;
        }

        private void OnResurrecting(ResurrectingEventArgs ev)
        {
            ev.IsAllowed = false;
        }

        private void OnKicking(Exiled.Events.EventArgs.Player.KickingEventArgs ev)
        {
            if (ev.Reason.ToLower().Contains("afk"))
                ev.IsAllowed = false;
        }
    }
}
