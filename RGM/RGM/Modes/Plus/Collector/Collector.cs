using System.Collections.Generic;
using System.Linq;
using Exiled.API.Extensions;
using Exiled.API.Features;
using MEC;
using RGM.API.Features;
using PlayerRoles;
using RGM.API.DataBases;
using UnityEngine;

namespace RGM.Modes
{
    [Mode(ModeCategory.Public, ModeInfo.Plus, ModeType.Collector)]
    public class Collector : Mode
    {
        public override string Name => "수집가";
        public override string Description => "SCP 아이템 3개를 가지고 시작합니다.";

        public override string Detail =>
            "이름에 <color=#FE2E2E>SCP</color>가 들어가는 모든 아이템 중에서 랜덤으로 3개를 지급받고 시작합니다.";

        public override string Color => "FFBF00";

        public static Collector Instance;

        CoroutineHandle _onModeStarted;

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
            if (Random.Range(1, 101) <= 10)
            {
                //10% 확률로 워크스테이션 업그레이드 시작
                Tools.TryInstallMode(ModeType.ABattle);
            }

            foreach (var player in PlayerManager.List.Where(x => x.IsAlive && x.Role.Type != RoleTypeId.Scp079))
            {
                Timing.RunCoroutine(Spawned(player));
            }

            yield return 0;
        }

        private static void OnSpawned(Exiled.Events.EventArgs.Player.SpawnedEventArgs ev)
        {
            Timing.RunCoroutine(Spawned(ev.Player));
        }

        private static IEnumerator<float> Spawned(Player player)
        {
            if (player.Role.Type == RoleTypeId.Scp079 || !player.IsAlive || player.IsNonePlayer())
                yield break;

            for (int i = 1; i < 4; i++)
            {
                player.AddItem(Tools.EnumToList<ItemType>()
                    .Where(x => x.ToString().Contains("SCP") && !Datas.ExceptItems.Contains(x)).ToList()
                    .GetRandomValue());

                yield return Timing.WaitForSeconds(1);
            }
        }
    }
}