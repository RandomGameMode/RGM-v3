using AFK;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Enums;
using Exiled.Events.EventArgs.Player;
using MEC;
using PlayerRoles;
using RGM.API.Features;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static RGM.Variables.Variable;

namespace RGM.Modes
{
    [Mode(ModeCategory.Public, ModeInfo.Set, ModeType.Stroking999)]
    public class Stroking999 : Mode
    {
        public override string Name => "999 데스매치";
        public override string Description => "SCP-999를 최대한 많이 쓰다듬어 승리하세요.";
        public override string Color => "fb9517";
        public override string Author => "Ragdoll";

        public override string Detail =>
            """
            가운데에 있는 Scp999를 보고 alt를 누르면 점수가 올라갑니다.
            """;

        private const float RaycastDistance = 5f;

        private int _timeLimit;
        private int _seconds;
        private const string TargetObjectName = "Scp999"; // 목표 오브젝트 이름

        private const int RaycastMask = 1 << 0;
        private const float CpsWindowSeconds = 1f;
        private const float IllegalCpsThreshold = 15.57f;
        private const float IllegalCpsDurationSeconds = 1.2f;
        private readonly Dictionary<Player, int> _scores = new();
        private readonly Dictionary<Player, Queue<float>> _strokeTimes = new();
        private readonly Dictionary<Player, float> _highCpsStartedAt = new();
        private readonly HashSet<Player> _disqualifiedPlayers = new();
        private bool _gameStarted;
        private bool _gameEnded;
        private int _time;
        private CoroutineHandle _timerHandle;

        public override void OnEnabled()
        {
            Server.FriendlyFire = true;
            Round.IsLocked = true;
            Respawn.PauseWaves();
            AFKManager._kickTime = 120500;

            _seconds = Random.Range(15, 57);
            _timeLimit = _seconds * 5;

            foreach (var player in PlayerManager.List)
            {
                _scores[player] = 0;
                _strokeTimes[player] = new Queue<float>();
            }

            Exiled.Events.Handlers.Player.TogglingNoClip += OnTogglingNoClip;

            _timerHandle = Timing.RunCoroutine(OnModeStarted());
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Player.TogglingNoClip -= OnTogglingNoClip;
            Timing.KillCoroutines(_timerHandle);

            _scores.Clear();
            _strokeTimes.Clear();
            _highCpsStartedAt.Clear();
            _disqualifiedPlayers.Clear();
        }

        private void OnTogglingNoClip(TogglingNoClipEventArgs ev)
        {
            var player = ev.Player;
            if (!_scores.ContainsKey(player))
                return;
            if (!_gameStarted) return;

            var cam = player.ReferenceHub.PlayerCameraReference;
            var origin = cam.position;

            if (!Physics.Raycast(origin, cam.forward, out var hit, RaycastDistance, RaycastMask,
                    QueryTriggerInteraction.Collide))
                return;

            if (!IsTargetObject(hit.collider.gameObject))
                return;

            if (_gameEnded)
            {
                Tools.PlaySound(player.Transform, "scp-999", 2);
                return;
            }

            if (_disqualifiedPlayers.Contains(player))
                return;

            _scores[player]++;
            _strokeTimes[player].Enqueue(Time.realtimeSinceStartup);
            CheckIsIllegal();
            player.ShowHitMarker();

            PlayersReport[player.UserId].Damage = _scores[player];
        }

        private static bool IsTargetObject(GameObject obj)
        {
            return obj.name == TargetObjectName;
        }

        private IEnumerator<float> OnModeStarted()
        {
            yield return Timing.WaitForSeconds(1f);
            Tools.LoadMap("Stroking999");

            yield return Timing.WaitForSeconds(0.1f);
            foreach (var player in PlayerManager.List)
            {
                player.Role.Set(RoleTypeId.Tutorial);
                player.Position = Tools.GetObjectList("SpawnSpot").GetRandomValue().position;
                player.EnableEffect(EffectType.Fade, 100);
            }

            for (var countdown = 10; countdown > 0; countdown--)
            {
                foreach (var player in PlayerManager.List.Where(p => p != null))
                    player.AddBroadcast(1, $"게임이 {countdown}초 후 시작됩니다!\n<size=25>alt를 눌러 999를 쓰다듬을 수 있습니다.</size>");

                yield return Timing.WaitForSeconds(1);
            }

            _gameStarted = true;

            for (_time = 0; _time < _timeLimit; _time++)
            {
                CheckIsIllegal();

                Player leader = null;
                var leaderScore = 0;

                foreach (var entry in _scores)
                {
                    if (_disqualifiedPlayers.Contains(entry.Key))
                        continue;

                    if (entry.Value > leaderScore)
                    {
                        leaderScore = entry.Value;
                        leader = entry.Key;
                    }
                }

                var leaderText = leader != null
                    ? $"현재 1위: {leader.DisplayNickname} ({leaderScore}회)"
                    : "현재 1위: 아직 기록 없음";

                var remain = (_timeLimit - _time) / 5;

                foreach (var player in PlayerManager.List.Where(p => p != null))
                {
                    var score = _scores.TryGetValue(player, out var value) ? value : 0;
                    var cps = GetCps(player, Time.realtimeSinceStartup);
                    player.AddHint("점수",
                        $" <size=20>{remain}초 남음 | 클릭 수: {score}회\n{leaderText}\n당신의 초당 클릭 수: {cps:0.##}</size>", 0.2f);
                }

                yield return Timing.WaitForSeconds(0.2f);
            }

            AnnounceWinner();
        }

        private void CheckIsIllegal()
        {
            var now = Time.realtimeSinceStartup;

            foreach (var entry in _strokeTimes)
            {
                var player = entry.Key;
                if (_disqualifiedPlayers.Contains(player) || !player.IsAlive)
                    continue;

                var cps = GetCps(player, now);
                if (cps < IllegalCpsThreshold)
                {
                    _highCpsStartedAt.Remove(player);
                    continue;
                }

                if (!_highCpsStartedAt.TryGetValue(player, out var startedAt))
                {
                    _highCpsStartedAt[player] = now;
                    continue;
                }

                if (now - startedAt < IllegalCpsDurationSeconds)
                    continue;

                _disqualifiedPlayers.Add(player);
                _highCpsStartedAt.Remove(player);
                player.Kill("꼼수를 쓰면 안되죠!");
            }
        }

        private float GetCps(Player player, float now)
        {
            if (!_strokeTimes.TryGetValue(player, out var strokeTimes))
                return 0f;

            while (strokeTimes.Count > 0 && now - strokeTimes.Peek() > CpsWindowSeconds)
                strokeTimes.Dequeue();

            return strokeTimes.Count / CpsWindowSeconds;
        }

        private void AnnounceWinner()
        {
            _gameEnded = true;
            var maxScore = int.MinValue;
            var tops = new List<Player>();

            foreach (var entry in _scores)
            {
                if (_disqualifiedPlayers.Contains(entry.Key))
                    continue;

                if (entry.Value > maxScore)
                {
                    maxScore = entry.Value;
                    tops.Clear();
                    tops.Add(entry.Key);
                }
                else if (entry.Value == maxScore)
                {
                    tops.Add(entry.Key);
                }
            }

            foreach (var player in _scores.Keys.Where(p => p != null && !tops.Contains(p) && p.IsAlive))
                player.Kill($"당신은 999에게 선택받지 못했습니다.\n당신의 평균 초당 클릭 수:{_scores[player] / (float)_seconds:0.##}");

            Round.IsLocked = false;

            switch (tops.Count)
            {
                case 1:
                    Timing.RunCoroutine(Tools.SetWinner(tops, PlayerManager.List.Count / 3));
                    break;
                case > 1:
                    Timing.RunCoroutine(Tools.SetWinner(tops, 3));
                    break;
            }
        }
    }
}