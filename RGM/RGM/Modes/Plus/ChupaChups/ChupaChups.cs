using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using MEC;
using PlayerRoles;
using RGM.API.Features;
using System.Collections.Generic;
using System.Linq;
using CommandSystem.Commands.RemoteAdmin.Inventory;
using LabApi.Events.Arguments.ServerEvents;
using RGM.Patches;

namespace RGM.Modes
{
    [Mode(ModeCategory.Public, ModeInfo.Plus, ModeType.ChupaChups)]
    class ChupaChups : Mode
    {
        public override string Name => "츄파춥스";
        public override string Description => "Jailbird.. 요즘 하나씩은 다 가지고 있죠?";
        public override string Detail =>
"""
스폰하면 즉시 Jailbird를 얻습니다.

* 게임 시작 14분 뒤 <color=red>자동핵</color>이 작동됩니다.
""";
        public override string Color => "2ECCFA";

        public static ChupaChups Instance;

        private CoroutineHandle _onModeStarted;
        private readonly AutoWarhead _autoWarhead = new(14, 1);

        public override void OnEnabled()
        {
            Exiled.Events.Handlers.Server.RespawningTeam += OnRespawningTeam;
            Exiled.Events.Handlers.Player.Spawned += OnSpawned;
            
            _onModeStarted = Timing.RunCoroutine(OnModeStarted());
            _autoWarhead.RunCoroutine();
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Server.RespawningTeam -= OnRespawningTeam;
            Exiled.Events.Handlers.Player.Spawned -= OnSpawned;

            Timing.KillCoroutines(_onModeStarted);
            _autoWarhead.KillCoroutine();
        }

        private IEnumerator<float> OnModeStarted()
        {
            yield return Timing.WaitForSeconds(1f);

            foreach (var player in PlayerManager.List.Where(x => x.IsAlive && x.Role.Type != RoleTypeId.Scp079))
                Spawned(player); 
        }

        private void OnSpawned(SpawnedEventArgs ev)
        {
            Spawned(ev.Player);
        }

        private void Spawned(Player player)
        {
            Timing.WaitForSeconds(1);

            player.AddItem(ItemType.Jailbird);
        }

        private void OnRespawningTeam(RespawningTeamEventArgs ev)
        {
            foreach (var player in PlayerManager.List.Where(x => x.IsAlive && x.IsScpRole() && x.Role.Type != RoleTypeId.Scp079))
                Spawned(player);
        }
    }
}
