using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using MEC;
using PlayerRoles;
using Exiled.API.Features.Roles;
using Exiled.Events.EventArgs.Server;
using RGM.API.Features;
using Exiled.API.Extensions;
using RGM.Patches;
using UnityEngine;

namespace RGM.Modes
{
    [Mode(ModeCategory.Public, ModeInfo.Set, ModeType.ScpRush)]
    public class ScpRush : Mode
    {
        public override string Name => "SCP 러쉬";
        public override string Description => "모든 SCP가 한 개체로 통일됩니다.";
        public override string Detail =>
"""
SCP-3114도 동일한 확률로 러쉬에 참여할 수 있습니다.

* 게임 시작 14분 뒤 <color=red>자동핵</color>이 작동됩니다.
""";
        public override string Color => "FE2E2E";

        public static ScpRush Instance;

        private CoroutineHandle _onModeStarted;
        private readonly AutoWarhead _autoWarhead = new(14, 1);
        
        private readonly List<RoleTypeId> _scpRoles =
        [
            RoleTypeId.Scp049,
            RoleTypeId.Scp096,
            RoleTypeId.Scp106,
            RoleTypeId.Scp173,
            RoleTypeId.Scp939,
            RoleTypeId.Scp079,
            RoleTypeId.Scp3114
        ];

        public override void OnEnabled()
        {
            Exiled.Events.Handlers.Server.RoundEnded += OnRoundEnded;

            _onModeStarted = Timing.RunCoroutine(OnModeStarted());
            _autoWarhead.RunCoroutine();
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Server.RoundEnded -= OnRoundEnded;

            Timing.KillCoroutines(_onModeStarted);
            _autoWarhead.KillCoroutine();
        }

        private IEnumerator<float> OnModeStarted()
        {
            if (Random.Range(1, 101) <= 10) { //10% 확률로 워크스테이션 업그레이드 시작
                Tools.TryInstallMode(ModeType.ABattle);
            }
            
            RoleTypeId randomScpRole = _scpRoles.GetRandomValue();

            foreach (var player in PlayerManager.List.Where(x => x.IsScpRole() && x.Role.Type != randomScpRole))
            {
                player.Role.Set(randomScpRole);

                if (player.Role is Scp079Role scp079)
                    scp079.Level = 4;
            }

            yield break;
        }

        private static void OnRoundEnded(RoundEndedEventArgs ev)
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
