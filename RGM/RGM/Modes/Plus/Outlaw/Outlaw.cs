using Exiled.API.Features.Items;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using MEC;
using RGM.API.Features;
using PlayerRoles;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.Events.EventArgs.Server;
using Exiled.Events.EventArgs.Player;
using RGM.Patches;
using UnityEngine;

namespace RGM.Modes
{
    [Mode(ModeCategory.Public, ModeInfo.Plus, ModeType.Outlaw)]
    public class Outlaw : Mode
    {
        public override string Name => "무법자";
        public override string Description => "모두가 총기 하나를 가지고 시작합니다.";
        public override string Detail =>
"""
무기 아이템 중에서 랜덤으로 지급받습니다.

SCP는 매 지원마다 새로운 무기를 받습니다.

* 게임 시작 14분 뒤 <color=red>자동핵</color>이 작동됩니다.
""";
        public override string Color => "9F81F7";

        public static Outlaw Instance;

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
            if (Random.Range(1, 101) <= 10) { //10% 확률로 워크스테이션 업그레이드 시작
                Tools.TryInstallMode(ModeType.ABattle);
            }
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
            Timing.CallDelayed(Timing.WaitForOneFrame, () =>
            {
                if (!player.IsAlive || player.Role.Type == RoleTypeId.Scp079) return;
                Item weapon = player.AddItem(Tools.EnumToList<ItemType>()
                    .Where(x => x.ToString().Contains("Gun") && x != ItemType.GunFSP9).ToList().GetRandomValue());

                if (weapon.Type == ItemType.GrenadeHE)
                    player.AddItem(ItemType.GrenadeHE, 2);

                if (weapon is not Firearm firearm) return;
                if (firearm.AmmoType == AmmoType.None) return;
                for (int i = 0; i < 3; i++)
                    player.AddItem(firearm.AmmoType.GetItemType());
            });
        }

        private void OnRespawningTeam(RespawningTeamEventArgs ev)
        {
            foreach (var player in PlayerManager.List.Where(x => x.IsAlive && x.IsScpRole() && x.Role.Type != RoleTypeId.Scp079))
                Spawned(player);
        }
    }
}
