using System.Linq;
using Exiled.API.Extensions;
using Exiled.API.Features;
using MEC;
using RGM.API.Features;
using PlayerRoles;

namespace RGM.Modes
{
    [Mode(ModeCategory.OnlySub, ModeInfo.Plus, ModeType.AdditionalWave)]
    internal class AdditionalWave : Mode
    {
        public override string Name => "추가 지원";
        public override string Description => "지원의 형태가 추가되고, 추가 아이템이 지급됩니다.";
        public override string Detail =>
"""
[지원 형태]
<color=#F781F3>뱀의 손</color>
<color=red>SCP-049-2</color>

[지원 아이템 목록]
최대 3개까지 모든 아이템 중 랜덤으로 선택됨
""";
        public override string Color => "F5D0A9";

        public static AdditionalWave Instance;

        public override void OnEnabled()
        {
            Exiled.Events.Handlers.Server.RespawningTeam += OnRespawningTeam;
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Server.RespawningTeam -= OnRespawningTeam;
        }

        private void OnRespawningTeam(Exiled.Events.EventArgs.Server.RespawningTeamEventArgs ev)
        {
            Timing.CallDelayed(Timing.WaitForOneFrame, () => 
            {
                switch (UnityEngine.Random.Range(1, 6))
                {
                    case 1:
                        Server.ExecuteCommand($"/fc {string.Join(".", PlayerManager.List.Where(x => x.IsDead && x.Role.Type != RoleTypeId.Overwatch).Select(x => x.Id))}. Scp0492");
                        break;

                    case 2:
                        Tools.CallSnakeHand(null, PlayerManager.List.Where(x => x.IsDead && x.Role.Type != RoleTypeId.Overwatch).ToList());
                        break;
                }
            });

            for (int i = 1; i < 4; i++)
            {
                foreach (var player in ev.Players)
                {
                    if (UnityEngine.Random.Range(1, 4) == 1)
                        player.AddItem(Tools.EnumToList<ItemType>().GetRandomValue());
                }
            }
        }
    }
}
