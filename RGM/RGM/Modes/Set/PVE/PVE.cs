using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using MEC;
using RGM.API.Features;
using RGM.Modes.PveExiledSystem;
using RGM.Variables;

namespace RGM.Modes
{
    [Mode(ModeCategory.Public, ModeInfo.Set, ModeType.PVE)]
    class PVE : Mode
    {
        public override string Name => "공성전";
        public override string Description => "AI들의 웨이브를 버텨내세요.";
        public override string Detail =>
"""
나도이게뭔지잘몰?루
""";
        public override string Color => "a0aade";
        public override string Author => "A3인데";


        RoundHandler roundHandler;
        private const bool EnableLoadDebug = true;

        public override void OnEnabled()
        {
            DebugLoad("모드 활성화를 시작합니다.");

            Round.IsLocked = true;
            DebugLoad("라운드를 잠갔습니다.");

            Respawn.PauseWaves();
            DebugLoad("리스폰 웨이브를 일시 정지했습니다.");

            roundHandler = new RoundHandler();
            DebugLoad("RoundHandler를 생성했습니다.");
            
            Exiled.Events.Handlers.Player.SpawnedRagdoll += OnSpawnedRagdoll;
            Exiled.Events.Handlers.Server.EndingRound += OnRoundEnding;
            Exiled.Events.Handlers.Server.EndingRound += roundHandler.OnEndingRound;
            DebugLoad("라운드 종료 이벤트를 등록했습니다.");

            roundHandler.OnRoundStarted();
            DebugLoad("RoundHandler를 시작했습니다.");
        }

        public override void OnDisabled()
        {
            DebugLoad("모드 비활성화를 시작합니다.");
            Exiled.Events.Handlers.Player.SpawnedRagdoll -= OnSpawnedRagdoll;

            Exiled.Events.Handlers.Server.EndingRound -= OnRoundEnding;
            if (roundHandler == null)
            {
                DebugLoad("RoundHandler가 없어 종료 처리를 건너뜁니다.");
                return;
            }

            Exiled.Events.Handlers.Server.EndingRound -= roundHandler.OnEndingRound;

            roundHandler.OnEndingRound();
            DebugLoad("RoundHandler 종료 처리가 완료되었습니다.");
        }

        private static void DebugLoad(string message)
        {
            if (EnableLoadDebug)
                Log.Debug($"[PVE] {message}");
        }
        
        private static void OnSpawnedRagdoll(SpawnedRagdollEventArgs ev)
        {
            ev.Ragdoll?.Destroy();
        }

        private void OnRoundEnding(EndingRoundEventArgs ev)
        {
            List<Player> players = [.. PlayerManager.List.Where(x => !x.IsNPC)];
            if (players.Count == 0 || roundHandler.SelectedDifficulty < 0)
                return;
            

            int reward = -1;
            reward = roundHandler.SelectedDifficulty switch
            {
                0 => roundHandler.CurrentWave switch
                {
                    <= 4 => 1,
                    <= 7 => 2,
                    <= 10 => 3,
                    <= 12 => 4,
                    <= 14 => 5,
                    _ => reward
                },
                1 => roundHandler.CurrentWave switch
                {
                  <= 4 => 1,
                  <= 6 => 2,
                  <= 8 => 3,
                  <= 10 => 4,
                  11 => 5,
                  12 => 6,
                  <= 14 => 7,
                  _ => reward
                },
                2 => roundHandler.CurrentWave switch
                {
                    <= 2 => 3,
                    <= 4 => 4,
                    <= 6 => 5,
                    <= 8 => 6,
                    <= 10 => 7,
                    <= 12 => 8,
                    13 => 9,
                    14 => 10,
                    _ => reward
                },
                _ => reward
            };

            List<Player> wonplayers =
            [
                .. players
                    .Where(p => Variable.PlayersReport.TryGetValue(p.UserId, out var report)
                                && report.Damage >= 3500)
            ];
            
            reward *= roundHandler.AllWavesCleared ? 2 : 1;
            reward = reward <= 0 ? 5 : reward;
            Timing.RunCoroutine(Tools.SetWinner(wonplayers, reward));
        }
    }
}
