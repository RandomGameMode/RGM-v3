using System.Collections.Generic;
using Exiled.API.Features;
using MEC;
using RGM.API.Features;
using Exiled.API.Extensions;
using Exiled.Events.EventArgs.Player;

namespace RGM.Modes
{
    [Mode(ModeCategory.OnlySub, ModeInfo.Plus, ModeType.Doppelganger)]
    public class Doppelganger : Mode
    {
        public override string Name => "도플갱어";
        public override string Description => "모두의 이름과 칭호가 살아있는 한 사람의 것으로 변경됩니다.";

        public override string Detail =>
            """
            도플갱어
            도플갱어

            모두의 이름과 칭호가 살아있는 한 사람의 것으로 변경됩니다.
            모두의 이름과 칭호가 살아있는 한 사람의 것으로 변경됩니다.
            """;

        public override string Color => "9453d9";

        public static Doppelganger Instance;

        private Player _owner;

        private CoroutineHandle _onModeStarted;

        public override void OnEnabled()
        {
            Exiled.Events.Handlers.Player.Died += OnDied;

            _onModeStarted = Timing.RunCoroutine(OnModeStarted());
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Player.Died -= OnDied;

            Timing.KillCoroutines(_onModeStarted);
        }

        private IEnumerator<float> OnModeStarted()
        {
            _owner = PlayerManager.List.GetRandomValue(x => x.IsAlive);

            while (!Round.IsEnded)
            {
                foreach (var player in PlayerManager.List)
                    if (player == _owner)
                    {
                        player.DisplayNickname = player.Nickname;
                    }
                    else
                    {
                        player.DisplayNickname = _owner.Nickname;
                        player.CustomInfo = _owner.CustomInfo;
                        player.RankName = _owner.RankName;
                        player.RankColor = _owner.RankColor;
                    }

                yield return Timing.WaitForOneFrame;
            }
        }

        private void OnDied(DiedEventArgs ev)
        {
            if (ev.Player == _owner) _owner = PlayerManager.List.GetRandomValue(x => x.IsAlive);
        }
    }
}
