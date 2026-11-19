using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using MEC;
using RGM.API.Features;
using UnityEngine;
using RGM.Modes.Sets.AddScp.Scps;
using Exiled.API.Extensions;
using Exiled.API.Features.Doors;
using Exiled.API.Enums;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;

namespace RGM.Modes
{
    [Mode(ModeCategory.Public, ModeInfo.Set, ModeType.AddScpMode)]
    internal class AddScpMode : Mode
    {
        public override string Name => "추가 SCP";
        public override string Description => "새로운 SCP들이 추가됩니다.";

        public override string Detail =>
            """
            새로운 SCP..?

            <color=red>SCP-005</color> : 만능 열쇠 (10+)
            <color=red>SCP-008</color> : 좀비 전염병 (10+)
            <color=red>SCP-035</color> : 빙의 가면 (10+)
            <color=red>SCP-294</color> : 자판기
            <color=red>SCP-457</color> : 불타는 사람 (15+)
            <color=red>SCP-966</color> : 잠을 죽이는 자 (15+)
            <color=red>SCP-999</color> : 간지럼 괴물 (5+)
            <color=red>SCP-1162</color> : 벽 속의 구멍
            """;

        public override string Color => "fd0101";

        public static AddScpMode Instance;

        public static readonly List<Player> SpecialScps = [];

        private CoroutineHandle _onModeStarted;

        public override void OnEnabled()
        {
            Exiled.Events.Handlers.Server.RoundEnded += OnRoundEnded;

            Exiled.Events.Handlers.Player.Spawned += OnSpawned;

            _onModeStarted = Timing.RunCoroutine(OnModeStarted());
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Server.RoundEnded -= OnRoundEnded;

            Exiled.Events.Handlers.Player.Spawned -= OnSpawned;

            Timing.KillCoroutines(_onModeStarted);
        }

        private static IEnumerator<float> OnModeStarted()
        {
            if (Random.Range(1, 101) <= 10) //10% 확률로 워크스테이션 업그레이드 시작
                Tools.TryInstallMode(ModeType.ABattle);
            Scp294.OnEnabled();
            Scp1162.OnEnabled();

            yield return Timing.WaitForSeconds(1f);

            if (Server.PlayerCount >= 5)
            {
                var scp999 =
                    PlayerManager.List.GetRandomValue(x => !x.IsScpRole() && x.IsAlive && !SpecialScps.Contains(x));
                SpecialScps.Add(scp999);

                var door = Door.Get(DoorType.HIDLab);
                scp999.Position = door.Position + new Vector3(0, 2, 0);

                Scp999.Create(scp999);
            }

            if (Server.PlayerCount >= 10)
            {
                var item = Scp005.Create();
                var lucky = PlayerManager.List.GetRandomValue(x =>
                    !x.IsScpRole() && x.IsAlive && !SpecialScps.Contains(x));
                lucky.AddItem(item);

                var scp008 =
                    PlayerManager.List.GetRandomValue(x => !x.IsScpRole() && x.IsAlive && !SpecialScps.Contains(x));
                SpecialScps.Add(scp008);

                Scp008.Create(scp008);

                var scp035 =
                    PlayerManager.List.GetRandomValue(x => !x.IsScpRole() && x.IsAlive && !SpecialScps.Contains(x));
                SpecialScps.Add(scp035);

                var door = Door.Get(DoorType.ElevatorNuke);
                scp035.Position = door.Position + new Vector3(0, 2, 0);

                Scp035.Create(scp035);
            }

            if (Server.PlayerCount >= 15)
            {
                var scp457 =
                    PlayerManager.List.GetRandomValue(x => x.IsScpRole() && x.IsAlive && !SpecialScps.Contains(x));
                SpecialScps.Add(scp457);

                Scp457.Create(scp457);

                var scp966 =
                    PlayerManager.List.GetRandomValue(x => x.IsScpRole() && x.IsAlive && !SpecialScps.Contains(x));
                SpecialScps.Add(scp966);

                Scp966.Create(scp966);
            }

            yield return Timing.WaitForOneFrame;
        }

        private static void OnSpawned(SpawnedEventArgs ev)
        {
            if (!ev.Player.IsAlive) return;
            if (Random.Range(1, 101) <= 25) 
                ev.Player.AddItem(ItemType.Coin);
        }

        private static void OnRoundEnded(RoundEndedEventArgs ev)
        {
            List<Player> players = [.. PlayerManager.List.Where(x => x.IsAlive && !x.IsNPC)];

            switch (players.Count())
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
