using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using MEC;
using Exiled.API.Enums;
using RGM.API.Features;
using PlayerRoles;
using Exiled.API.Extensions;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;

namespace RGM.Modes
{
    [Mode(ModeCategory.Public, ModeInfo.Set, ModeType.ChangedFate)]
    public class ChangedFate : Mode
    {
        public override string Name => "뒤바뀐 운명";
        public override string Description => "각 진영의 운명이 뒤바뀐다는 예언이 있었습니다.";

        public override string Detail =>
            $"""
             <b>각 진영의 스폰 위치는 같지만, 역할군이 변경됩니다.</b>

             <color={RoleTypeId.ClassD.GetColor().ToHex()}>D계급</color> <-> <color={RoleTypeId.Scientist.GetColor().ToHex()}>과학자</color>
             <color={RoleTypeId.FacilityGuard.GetColor().ToHex()}>시설 경비</color> <-> <color={RoleTypeId.Scp079.GetColor().ToHex()}>SCP</color>
             <color={RoleTypeId.NtfSergeant.GetColor().ToHex()}>NTF</color> <-> <color={RoleTypeId.ChaosMarauder.GetColor().ToHex()}>혼돈의 반란</color>
             """;

        public override string Color => "B40486";
        public override string Suggester => "A";

        public static ChangedFate Instance;

        private static readonly List<RoleTypeId> IgnoredRoles =
        [
            RoleTypeId.Scp3114,
            RoleTypeId.Scp0492,
            RoleTypeId.Flamingo,
            RoleTypeId.AlphaFlamingo,
            RoleTypeId.ChaosFlamingo,
            RoleTypeId.NtfFlamingo,
            RoleTypeId.ZombieFlamingo,
            RoleTypeId.Destroyed,
            RoleTypeId.Overwatch,
            RoleTypeId.Filmmaker,
            RoleTypeId.None
        ];

        private readonly Dictionary<RoleTypeId, List<RoleTypeId>> _roleTypeIds = new()
        {
            { RoleTypeId.ClassD, [RoleTypeId.Scientist] },
            { RoleTypeId.Scientist, [RoleTypeId.ClassD] },
            {
                RoleTypeId.FacilityGuard,
                [.. Tools.EnumToList<RoleTypeId>().Where(x => x.IsScpRole() && !IgnoredRoles.Contains(x))]
            }
        };

        private RoleTypeId SelectRole(Player player)
        {
            if (_roleTypeIds.TryGetValue(player.Role.Type, out var roles)) return roles.GetRandomValue();

            if (player.IsScpRole())
                return RoleTypeId.FacilityGuard;

            return player.Role.Type switch
            {
                RoleTypeId.NtfCaptain => RoleTypeId.ChaosRepressor,
                RoleTypeId.NtfSergeant => RoleTypeId.ChaosMarauder,
                RoleTypeId.NtfPrivate => RoleTypeId.ChaosRifleman,
                RoleTypeId.ChaosRepressor => RoleTypeId.NtfCaptain,
                RoleTypeId.ChaosMarauder => RoleTypeId.NtfSergeant,
                RoleTypeId.ChaosRifleman => RoleTypeId.NtfPrivate,
                _ => RoleTypeId.Tutorial
            };
        }

        private CoroutineHandle _onModeStarted;

        public override void OnEnabled()
        {
            Exiled.Events.Handlers.Player.Spawned += OnSpawned;
            Exiled.Events.Handlers.Server.RoundEnded += OnRoundEnded;

            _onModeStarted = Timing.RunCoroutine(OnModeStarted());
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Player.Spawned -= OnSpawned;
            Exiled.Events.Handlers.Server.RoundEnded -= OnRoundEnded;

            Timing.KillCoroutines(_onModeStarted);
        }

        private IEnumerator<float> OnModeStarted()
        {
            foreach (var player in PlayerManager.List)
            {
                var roleType = SelectRole(player);
                player.Role.Set(roleType, SpawnReason.ItemUsage, RoleSpawnFlags.AssignInventory);
            }

            yield break;
        }

        private void OnSpawned(SpawnedEventArgs ev)
        {
            if (ev.Player.IsAlive && ev.Reason != SpawnReason.ItemUsage)
            {
                var roleType = SelectRole(ev.Player);
                ev.Player.Role.Set(roleType, SpawnReason.ItemUsage, RoleSpawnFlags.AssignInventory);
            }
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
