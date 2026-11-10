using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Server;
using MEC;
using RGM.API.Features;

namespace RGM.Modes
{
    [Mode(ModeCategory.Public, ModeInfo.Set, ModeType.Original)]
    public class Original : Mode
    {
        public override string Name => "오리지널";
        public override string Description => "가끔은 도파민이 없는 기본을 해보시는 건 어떠신지..?";
        public override string Detail => "모드가 존재하지 않습니다.";
        public override string Color => "FFFFFF";

        public static Original Instance;

        public override void OnEnabled()
        {
            Exiled.Events.Handlers.Server.RoundEnded += OnRoundEnded;
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Server.RoundEnded += OnRoundEnded;
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
    }
}
