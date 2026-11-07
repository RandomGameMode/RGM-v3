using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using MEC;
using Exiled.Events.EventArgs.Player;
using Exiled.API.Extensions;
using RGM.API.Features;
using PlayerRoles;
using RGM.API.DataBases;
using UnityEngine;

namespace RGM.Modes
{
    [Mode(ModeCategory.OnlySub, ModeInfo.Plus, ModeType.Disguise)]
    public class Disguise : Mode
    {
        public override string Name => "변장";
        public override string Description => "일부 유저의 모습이 랜덤하게 변경됩니다.";

        public override string Detail =>
            """
            * 근접하면 실제 역할군이 표시됩니다.

            <b>변장 역할군 제외 목록</b>
            SCP-079
            관전자
            오버워치
            필름메이커
            """;

        public override string Color => "F1F8E0";

        public static Disguise Instance;

        private static readonly Dictionary<Player, RoleTypeId> DisguisedList = new();

        private static readonly List<RoleTypeId> BlockedRoles =
        [
            RoleTypeId.Spectator,
            RoleTypeId.Scp079,
            RoleTypeId.Overwatch,
            RoleTypeId.Filmmaker,
            RoleTypeId.CustomRole,
            RoleTypeId.Destroyed,
            RoleTypeId.Scp3114,
            RoleTypeId.None
        ];

        private readonly List<RoleTypeId> _roleList =
            [.. Tools.EnumToList<RoleTypeId>().Where(x => !BlockedRoles.Contains(x))];

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
            foreach (var player in PlayerManager.List)
            {
                Verified(player);
                Spawned(player);
            }

            yield return Timing.WaitForSeconds(2);

            while (true)
            {
                foreach (var p in PlayerManager.List.Where(x => x.IsAlive && DisguisedList.ContainsKey(x)))
                    if (p.TryGetLookPlayer(3, out var t, out _) && DisguisedList.ContainsKey(t))
                        p.AddHint("변장 탐색",
                            $"<size=25><b>진실의 눈은 그를 <color={t.Role.Color.ToHex()}>{Trans.Role[t.Role.Type]}</color>(으)로 판별했습니다.</b></size>",
                            1.2f);

                    else if (p.Role.Type == DisguisedList[p])
                        p.AddHint("변장 탐색", $"<size=25><b>당신은 변장하지 않았습니다.</b></size>", 1.2f);

                    else
                        p.AddHint("정체 확인",
                            $"<size=25><b>당신의 정체는 <color={p.Role.Color.ToHex()}>{Trans.Role[p.Role.Type]}</color>이고, <color={DisguisedList[p].GetRoleBase().RoleColor.ToHex()}>{Trans.Role[DisguisedList[p]]}</color>(으)로 변장했습니다.</b></size>",
                            1.2f);

                yield return Timing.WaitForSeconds(1f);
            }
        }

        public void OnVerified(VerifiedEventArgs ev)
        {
            Verified(ev.Player);
        }

        private void Verified(Player player)
        {
            if (!DisguisedList.ContainsKey(player))
                DisguisedList.Add(player, RoleTypeId.Spectator);
        }

        private void OnSpawned(SpawnedEventArgs ev)
        {
            Spawned(ev.Player);
        }

        private void Spawned(Player player)
        {
            if (BlockedRoles.Contains(player.Role) || !DisguisedList.ContainsKey(player))
                return;

            Timing.CallDelayed(1f, () =>
            {
                if (Random.Range(1, 101) <= 44)
                {
                    var roleId = _roleList.GetRandomValue();

                    DisguisedList[player] = roleId;

                    player.ChangeAppearance(roleId);
                }
                else
                {
                    DisguisedList[player] = player.Role.Type;
                }
            });
        }
    }
}
