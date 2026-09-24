using MEC;
using Exiled.Events.EventArgs.Player;
using RGM.API.Features;

namespace RGM.Modes
{
    [Mode(ModeCategory.OnlySub, ModeInfo.Plus, ModeType.Ascension)]
    public class Ascension : Mode
    {
        public override string Name => "승천";
        public override string Description => "점프하면 승천합니다, 너무 멀리요.";
        public override string Detail =>
"""
점프하면 하늘 나라까지 갈 수 있데요!
""";
        public override string Color => "F5DA81";

        public static Ascension Instance;

        public override void OnEnabled()
        {
            Exiled.Events.Handlers.Player.Jumping += OnJumping;
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Player.Jumping -= OnJumping;
        }

        private static void OnJumping(JumpingEventArgs ev)
        {
            if (ev.Player.IsNonePlayer())
                return;

            Timing.RunCoroutine(Tools.DoRocket(ev.Player, ev.Player, 1, isInstantKill:true));
        }
    }
}
