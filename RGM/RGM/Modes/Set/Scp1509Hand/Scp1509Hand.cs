using System.Collections.Generic;
using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using MEC;
using PlayerRoles;
using RGM.API.Features;

namespace RGM.Modes
{
    [Mode(ModeCategory.Public, ModeInfo.Set, ModeType.Scp1509Hand)]
    class Scp1509Hand : Mode
    {
        public override string Name => "마체테 클로";
        public override string Description => "상대방을 처치하면 즉시 자신의 팀으로 만듭니다.";
        public override string Detail =>
"""
인간이 처치할 경우, 같은 진영으로 변경됩니다.

SCP가 처치할 경우, SCP-049-2로 변경됩니다.
""";
        public override string Color => "7a7a7a";

        public override void OnEnabled()
        {
            Exiled.Events.Handlers.Player.Died += OnDied;
            Exiled.Events.Handlers.Server.RoundEnded += OnRoundEnded;
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Player.Died -= OnDied;
            Exiled.Events.Handlers.Server.RoundEnded -= OnRoundEnded;
        }

        private static void OnDied(DiedEventArgs ev)
        {
            if (ev.Attacker == null || ev.DamageHandler.Type == DamageType.PocketDimension) return;
            ev.Player.Role.Set(ev.Attacker.IsScp ? RoleTypeId.Scp0492 : ev.Attacker.Role.Type, RoleSpawnFlags.None);
        }
        
        private static void OnRoundEnded(RoundEndedEventArgs ev)
        {
            List<Player> players = [.. PlayerManager.List.Where(x => x.IsAlive && !x.IsNPC)];

            switch (players.Count)
            {
                case 1:
                    Timing.RunCoroutine(Tools.SetWinner([.. players], 5));
                    break;
                case > 1:
                    Timing.RunCoroutine(Tools.SetWinner([.. players], 1));
                    break;
            }
        }
    }
}
