using Exiled.API.Features;
using Exiled.API.Features.Doors;
using Exiled.Events.EventArgs.Player;
using MEC;
using RGM.API.Features;
using RGM.Commands.RemoteAdminCommands;
using System.Collections.Generic;
using System.Linq;
using MapGeneration.Holidays;
using RGM.Patches;
using UnityEngine;
using static RGM.Variables.Variable;

namespace RGM.Modes
{
    [Mode(ModeCategory.Public, ModeInfo.Plus, ModeType.TrickorTreat)]
    public class TrickorTreat : Mode
    {
        public override string Name => "트릭 오어 트릿";
        public override string Description => "재단에 사탕 파티가 열렸습니다!";

        public override string Detail =>
            """
            <b><color=#E65000>무</color><color=#E5560F>작</color><color=#E55D1F>위</color> <color=#E46A3E>사</color><color=#E4714D>탕</color> <color=#E37E6C>4</color><color=#E3857C>개</color></b>를 획득합니다.

            가끔씩 바닥에 꽁짜 사탕이 떨어질 수도 있겠죠.
            아니면 누군가를 죽이거나..

            * 게임 시작 12분 뒤 <color=red>자동핵</color>이 작동됩니다.
            * 할로윈 이벤트 중에는 모드가 강화됩니다.
            """;

        public override string Color => "5F04B4";

        public static TrickorTreat Instance;

        private CoroutineHandle _onModeStarted;
        private readonly AutoWarhead _autoWarhead = new(12, 1);
        private readonly bool _isHalloween = HolidayUtils.IsHolidayActive(HolidayType.Halloween);

        public override void OnEnabled()
        {
            Exiled.Events.Handlers.Player.Spawned += OnSpawned;
            Exiled.Events.Handlers.Player.Dying += OnDying;

            _onModeStarted = Timing.RunCoroutine(OnModeStarted());
            _autoWarhead.RunCoroutine();
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Player.Spawned -= OnSpawned;
            Exiled.Events.Handlers.Player.Dying -= OnDying;

            Timing.KillCoroutines(_onModeStarted);
            _autoWarhead.KillCoroutine();
        }

        private IEnumerator<float> OnModeStarted()
        {
            foreach (var player in PlayerManager.List)
            {
                Spawned(player);
            }

            while (true)
            {
                foreach (var door in Door.List)
                {
                    if (Random.Range(1, 101) > (_isHalloween ? 9 : 3)) continue;
                    for (int i = 0; i < Random.Range(1, _isHalloween ? 30 : 10); i++)
                        CandyParty.Create(Tools.PickRandomCandy(), Random.Range(0.8f, 16),
                            door.Position + new Vector3(0, 2, 0));
                }

                foreach (var player in PlayerManager.List.Where(_ => Random.Range(1, 101) <= (_isHalloween ? 9 : 3)))
                {
                    CandyParty.Create(Tools.PickRandomCandy(), Random.Range(0.8f, 16), player.Position);
                }

                GlobalPlayer.TryPlay("treat or treat", 1.5f);

                yield return Timing.WaitForSeconds(_isHalloween ? Random.Range(15, 60) : Random.Range(30, 150));
            }
        }

        private void OnSpawned(SpawnedEventArgs ev)
        {
            Spawned(ev.Player);
        }

        private void Spawned(Player player)
        {
            if (player.IsAlive && !player.IsNonePlayer())
            {
                Timing.CallDelayed(1f, () =>
                {
                    for (int i = 0; i < (_isHalloween ? 6 : 4); i++)
                    {
                        player.AddRandomCandy();
                    }
                });
            }
        }

        private void OnDying(DyingEventArgs ev)
        {
            if (ev.Attacker == null)
                return;

            for (int i = 0; i < (_isHalloween ? 6 : 1); i++)
            {
                ev.Attacker.AddRandomCandy();
            }
        }
    }
};