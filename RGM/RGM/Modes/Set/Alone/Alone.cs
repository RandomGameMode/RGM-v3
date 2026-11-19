using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using MEC;

using PlayerRoles;
using RGM.API.Features;

namespace RGM.Modes
{
    [Mode(ModeCategory.Public, ModeInfo.Set, ModeType.Alone)]
    internal class Alone : Mode
    {
        public override string Name => "나 홀로 집에";
        public override string Description => "한명을 제외한 나머지는 모두 SCP가 됩니다.";

        public override string Detail =>
            $"""
             한명은 반드시 <color={RoleTypeId.ClassD.GetColor().ToHex()}>D계급</color>으로 스폰하며, 탈출하는 것이 목표입니다.
             나머지는 모두 <color=red>SCP</color>가 됩니다. (<color={RoleTypeId.Scp079.GetColor().ToHex()}>SCP-079</color> 제외)
             """;

        public override string Color => "F781BE";

        public static Alone Instance;

        private Player _alone;

        private CoroutineHandle _onModeStarted;

        private readonly List<RoleTypeId> _ignoredRoles =
        [
            RoleTypeId.Scp079,
            RoleTypeId.Scp3114,
            RoleTypeId.Scp0492,
            RoleTypeId.Flamingo,
            RoleTypeId.AlphaFlamingo,
            RoleTypeId.ChaosFlamingo,
            RoleTypeId.NtfFlamingo,
            RoleTypeId.ZombieFlamingo,
            RoleTypeId.Overwatch,
            RoleTypeId.Destroyed,
            RoleTypeId.Tutorial,
            RoleTypeId.Filmmaker,
            RoleTypeId.None
        ];

        private readonly List<ItemType> _humanItems =
        [
            ItemType.KeycardO5,
            ItemType.SCP500,
            ItemType.SCP268,
            ItemType.AntiSCP207,
            ItemType.SCP2176,
            ItemType.SCP207,
            ItemType.SCP1344
        ];

        public override void OnEnabled()
        {
            Respawn.PauseWaves();

            Exiled.Events.Handlers.Player.Escaped += OnEscaped;

            Exiled.Events.Handlers.Server.RoundEnded += OnRoundEnded;

            _onModeStarted = Timing.RunCoroutine(OnModeStarted());
        }

        public override void OnDisabled()
        {
            Respawn.ResumeWaves();

            Exiled.Events.Handlers.Player.Escaped -= OnEscaped;
            Exiled.Events.Handlers.Server.RoundEnded -= OnRoundEnded;

            Timing.KillCoroutines(_onModeStarted);
        }

        private IEnumerator<float> OnModeStarted()
        {
            _alone = PlayerManager.List.Where(x => !x.IsNPC).GetRandomValue();

            var scpRoles = Tools.EnumToList<RoleTypeId>()
                .Where(x => x.IsScp() && !_ignoredRoles.Contains(x))
                .ToList();

            _alone.Role.Set(RoleTypeId.ClassD);
            _alone.AddEffect(EffectType.MovementBoost, Math.Min(15 * PlayerManager.List.Count, 255));

            foreach (var item in _humanItems)
                _alone.AddItem(item);

            foreach (var player in PlayerManager.List.Where(x => x != _alone))
                player.Role.Set(scpRoles.GetRandomValue());

            yield break;
        }

        private void OnEscaped(EscapedEventArgs ev)
        {
            if (ev.Player != _alone) return;
            Timing.RunCoroutine(Tools.SetWinner([_alone], PlayerManager.List.Count / 2));

            foreach (var player in PlayerManager.List.Where(x => x != _alone && x.IsAlive))
                player.Role.Set(RoleTypeId.Tutorial);
        }

        private void OnRoundEnded(RoundEndedEventArgs ev)
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
