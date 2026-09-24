using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using MEC;
using PlayerRoles;

using RGM.API.Features;

using static RGM.Variables.Variable;
using Exiled.Events.EventArgs.Player;
using RGM.Modes.SnakeSystem;
using AFK;

namespace RGM.Modes
{
    // 플레이어와 겹쳐 있을 경우 스네이크 점수를 뺏어오는 버그 있음
    [Mode(ModeCategory.Private, ModeInfo.Set, ModeType.Snake)]
    public class Snake : Mode
    {
        public override string Name => "스네이크";
        public override string Description => "가장 높은 점수를 달성한 유저가 우승합니다.";
        public override string Detail =>
"""
한번 죽으면 그 즉시 점수가 기록되고 기회가 주어지지 않습니다.
    
행운을 빕니다.

+ "슈퍼 스타" 모드 추가
""";
        public override string Color => "3EA724";

        public static Snake Instance;

        Dictionary<Player, (int, int)> playerScore = new();
        int time = 0;

        CoroutineHandle _onModeStarted;

        public override void OnEnabled()
        {
            Server.FriendlyFire = true;
            Round.IsLocked = true;
            Respawn.PauseWaves();
            AFKManager._kickTime = 120500;

            Exiled.Events.Handlers.Player.ChangingItem += OnChangingItem;
            Exiled.Events.Handlers.Player.DroppingItem += OnDroppingItem;
            Exiled.Events.Handlers.Player.Died += OnDied;

            PlayerScoreManager.LoadScores();
            SnakeEventManager.Initialize();
            SnakeGameMonitor.StartMonitoring();

            SnakeEventManager.OnSnakeGameEnd += OnSnakeGameEnd;
            SnakeEventManager.OnSnakeScoreChanged += OnSnakeScoreChanged;

            _onModeStarted = Timing.RunCoroutine(OnModeStarted());

            Tools.TryInstallMode(ModeType.SuperStar);
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Player.ChangingItem -= OnChangingItem;
            Exiled.Events.Handlers.Player.DroppingItem -= OnDroppingItem;
            Exiled.Events.Handlers.Player.Died -= OnDied;

            SnakeEventManager.Cleanup();

            SnakeEventManager.OnSnakeGameEnd -= OnSnakeGameEnd;
            SnakeEventManager.OnSnakeScoreChanged -= OnSnakeScoreChanged;

            Timing.KillCoroutines(_onModeStarted);

            Tools.UnInstallMode(ModeType.SuperStar);
        }

        public IEnumerator<float> OnModeStarted()
        {
            foreach (var player in PlayerManager.List)
            {
                playerScore.Add(player, (0, 0));

                player.Role.Set(RoleTypeId.Tutorial);

                Timing.CallDelayed(1, () =>
                {
                    Server.ExecuteCommand($"/fc {player.Id} tutorial 3");
                    Server.ExecuteCommand($"/give {player.Id} 10");
                });
            }

            while (true)
            {
                time++;

                foreach (var player in playerScore.Keys)
                {
                    player.AddBroadcast(1, 177 - time > 0 ? $"<size=30>{177 - time}초 후 게임이 종료됩니다.</size>" : "게임이 종료되었습니다.");

                    if (player.IsAlive)
                    {
                        int remain = 30 - (time - playerScore[player].Item2);

                        if (remain <= 0)
                        {
                            player.Kill($"최종 점수: {playerScore[player].Item1}점");
                        }
                        else
                        {
                            player.AddBroadcast(1, $"<size=25><b>{remain}초 안에 다음 점수를 획득하세요.</b> 그렇지 않으면 <color=red>사망</color>합니다.</size>");
                        }
                    }
                }
                
                if (time == 177)
                {
                    foreach (var player in PlayerManager.List.Where(x => x.IsAlive))
                    {
                        player.Kill($"제한 시간 초과! (최종 점수: {playerScore[player].Item1}점)");
                    }
                }

                yield return Timing.WaitForSeconds(1);
            }
        }

        void OnChangingItem(ChangingItemEventArgs ev)
        {
            if (ev.Player.CurrentItem != null && ev.Player.CurrentItem.Type == ItemType.KeycardChaosInsurgency)
            {
                ev.IsAllowed = false;
            }
        }
        
        void OnDroppingItem(DroppingItemEventArgs ev)
        {
            if (ev.Player.CurrentItem.Type == ItemType.KeycardChaosInsurgency)
            {
                ev.IsAllowed = false;
            }
        }

        void OnDied(DiedEventArgs ev)
        {
            if (PlayerManager.List.Count(x => x.IsAlive) == 0)
            {
                Round.IsLocked = false;

                List<Player> getResult()
                {
                    int maxScore = int.MinValue;
                    var tops = new List<Player>();

                    foreach (var entry in playerScore)
                    {
                        int score = entry.Value.Item1;
                        if (score > maxScore)
                        {
                            maxScore = score;
                            tops.Clear();
                            tops.Add(entry.Key);
                        }
                        else if (score == maxScore)
                        {
                            tops.Add(entry.Key);
                        }
                    }

                    return tops;
                }

                 List<Player> players = getResult();

                if (players.Count() == 1)
                    Timing.RunCoroutine(Tools.SetWinner(players.ToList(), 5));

                else if (players.Count() > 1)
                    Timing.RunCoroutine(Tools.SetWinner(players.ToList(), 3));
            }
        }

        void OnSnakeGameEnd(Player player, int score)
        {
            player.Kill($"최종 점수: {playerScore[player].Item1}점");
        }

        void OnSnakeScoreChanged(Player player, int score)
        {
            playerScore[player] = (score, time);

            PlayersReport[player.UserId].Damage = score;
        }
    }
}
