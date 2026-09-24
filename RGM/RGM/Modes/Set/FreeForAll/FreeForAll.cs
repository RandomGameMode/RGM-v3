using System.Collections.Generic;
using System.Linq;
using Decals;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Doors;
using Exiled.Events.EventArgs.Player;
using MEC;
using PlayerRoles;
using RGM.API.Features;
using UnityEngine;

namespace RGM.Modes
{
    [Mode(ModeCategory.Public, ModeInfo.Set, ModeType.FreeForAll)]
    class FreeForAll : Mode
    {
        public override string Name => "개인전";
        public override string Description => "가장 먼저 40킬을 달성하세요!";
        public override string Detail =>
"""
랜덤한 전투 맵에서 다른 모든 플레이어와 싸우세요.

먼저 40킬을 달성하거나, 3분 후 가장 많은 킬을 기록한 플레이어가 승리합니다.
""";
        public override string Color => "FA58F4";

        public static FreeForAll Instance;
        
        private static readonly List<string> MapsDm =
        [
            "Battle_Shipment_Halloween2024",
            "Battle_Shipment_Xmas2025",
            "Battle_Shipment"
        ];
        
        private const int TargetKills = 40;
        private const float MatchDuration = 180f;
        private const float RespawnDelay = 5f;

        private readonly List<Player> _players = new();
        private readonly Dictionary<Player, int> _kills = new();
        private List<Vector3> _spawnPositions = new();
        private CoroutineHandle _onModeStarted;
        private CoroutineHandle _leaderboard;
        private CoroutineHandle _cleanupDecals;
        private bool _isMatchEnded;
        private bool _isModeActive;
        private int _modeId;
        private float _matchEndTime;

        private List<ItemType> Items()
        {
            List<ItemType> guns =
            [
                ItemType.GunE11SR,
                ItemType.GunShotgun,
                ItemType.GunCom45,
                ItemType.GunCrossvec,
                ItemType.GunLogicer,
                ItemType.GunFRMG0,
                ItemType.GunAK
            ];
            List<ItemType> items =
            [
                guns.GetRandomValue(),
                ItemType.ArmorLight
            ];

            return items;
        }

        public override void OnEnabled()
        {
            _modeId++;
            _isModeActive = true;
            _isMatchEnded = false;
            _players.Clear();
            _kills.Clear();
            _spawnPositions.Clear();

            Server.FriendlyFire = true;
            Round.IsLocked = true;
            Respawn.PauseWaves();
            
            Exiled.Events.Handlers.Player.Dying += OnDying;
            Exiled.Events.Handlers.Player.Died += OnDied;
            Exiled.Events.Handlers.Player.DroppingItem += OnDroppingItem;
            Exiled.Events.Handlers.Player.DroppingAmmo += OnDroppingAmmo;
            Exiled.Events.Handlers.Player.Shot += OnShot;

            _onModeStarted = Timing.RunCoroutine(OnModeStarted());
            _leaderboard = Timing.RunCoroutine(LeaderboardCoroutine());
            _cleanupDecals = Timing.RunCoroutine(CleanDecals());
        }

        public override void OnDisabled()
        {
            _isModeActive = false;

            Exiled.Events.Handlers.Player.Dying -= OnDying;
            Exiled.Events.Handlers.Player.Died -= OnDied;
            Exiled.Events.Handlers.Player.DroppingItem -= OnDroppingItem;
            Exiled.Events.Handlers.Player.DroppingAmmo -= OnDroppingAmmo;
            Exiled.Events.Handlers.Player.Shot -= OnShot;

            Timing.KillCoroutines(_onModeStarted);
            Timing.KillCoroutines(_leaderboard);
            Timing.KillCoroutines(_cleanupDecals);

            foreach (var door in Door.List)
                door.Unlock();
        }

        private IEnumerator<float> OnModeStarted()
        {
            foreach (var door in Door.List)
            {
                door.IsOpen = true;
                door.Lock(DoorLockType.AdminCommand);
            }

            Tools.LoadMap(MapsDm.GetRandomValue());
            yield return Timing.WaitForSeconds(1f);

            _spawnPositions = Tools.GetSpawnPositions("Spot Random");
            if (_spawnPositions.Count == 0)
            {
                Log.Error("[FreeForAll] 'Spot Random' 스폰 포인트를 찾지 못했습니다.");
                Round.IsLocked = false;
                yield break;
            }

            _matchEndTime = Time.realtimeSinceStartup + MatchDuration;
            foreach (var player in PlayerManager.List.ToList())
            {
                _players.Add(player);
                _kills[player] = 0;
                player.Role.Set(RoleTypeId.NtfSpecialist, RoleSpawnFlags.None);
                yield return Timing.WaitForOneFrame;
                FinishSpawn(player, _modeId);
            }

            yield return Timing.WaitForSeconds(MatchDuration);

            if (!_isMatchEnded)
                EndMatch(GetLeaders());
        }

        private void OnDying(DyingEventArgs ev)
        {
            if (_isModeActive && !_isMatchEnded && _players.Contains(ev.Player))
            {
                ev.Player.ClearInventory();
                ev.Player.ClearAmmo();
            }
        }

        private void OnDied(DiedEventArgs ev)
        {
            if (!_isModeActive || _isMatchEnded || !_players.Contains(ev.Player))
                return;

            ev.Ragdoll?.Destroy();

            if (ev.Attacker != null && ev.Attacker != ev.Player && _kills.ContainsKey(ev.Attacker))
            {
                _kills[ev.Attacker]++;

                if (_kills[ev.Attacker] >= TargetKills)
                {
                    EndMatch([ev.Attacker]);
                    return;
                }
            }

            Timing.RunCoroutine(RespawnPlayer(ev.Player, _modeId));
        }

        private IEnumerator<float> RespawnPlayer(Player player, int modeId)
        {
            yield return Timing.WaitForSeconds(RespawnDelay);

            if (!_isModeActive || _isMatchEnded || modeId != _modeId || !Round.IsLocked || player.IsAlive)
                yield break;

            player.Role.Set(RoleTypeId.NtfSpecialist, RoleSpawnFlags.None);
            yield return Timing.WaitForOneFrame;
            FinishSpawn(player, modeId);
        }

        private void FinishSpawn(Player player, int modeId)
        {
            if (!_isModeActive || _isMatchEnded || modeId != _modeId || !_players.Contains(player) || _spawnPositions.Count == 0)
                return;

            player.Position = _spawnPositions.GetRandomValue();
            player.ApplyGodMode(3);
            player.ClearInventory();

            foreach (var item in Items())
                player.AddItem(item);
        }

        private IEnumerator<float> LeaderboardCoroutine()
        {
            while (_isModeActive && !_isMatchEnded)
            {
                foreach (var player in _players.Where(x => x.IsAlive))
                    player.AddHint("개인전 리더보드", GetLeaderboardText(player), 1.2f);

                yield return Timing.WaitForSeconds(1f);
            }
        }

        private string GetLeaderboardText(Player viewer)
        {
            int remainingSeconds = Mathf.CeilToInt(Mathf.Max(0f, _matchEndTime - Time.realtimeSinceStartup));
            var rankings = _kills
                .OrderByDescending(entry => entry.Value)
                .ThenBy(entry => entry.Key.DisplayNickname)
                .Take(5)
                .Select((entry, index) => $"{index + 1}. {entry.Key.DisplayNickname} <color=#FFD700>{entry.Value}</color>");

            return $"<align=right><size=22><b>개인전 리더보드</b>\n남은 시간: <color=#FF6B6B>{remainingSeconds / 60:00}:{remainingSeconds % 60:00}</color>\n\n{string.Join("\n", rankings)}\n\n내 킬: <color=#00E5FF>{_kills[viewer]}</color> / {TargetKills}</size></align>";
        }

        private List<Player> GetLeaders()
        {
            if (_kills.Count == 0)
                return new List<Player>();

            int highestKills = _kills.Max(entry => entry.Value);
            return _kills
                .Where(entry => entry.Value == highestKills)
                .Select(entry => entry.Key)
                .ToList();
        }

        private void EndMatch(List<Player> winners)
        {
            if (_isMatchEnded || winners.Count == 0)
                return;

            _isMatchEnded = true;
            Round.IsLocked = false;

            string winnerNames = string.Join(", ", winners.Select(player => player.DisplayNickname));
            foreach (var player in PlayerManager.List)
                player.AddBroadcast(10, $"<size=30><b>승리자: <color=#FFD700>{winnerNames}</color></b></size>");

            Timing.RunCoroutine(Tools.SetWinner(winners, 13));
        }
        private IEnumerator<float> CleanDecals()
        {
            while (!_isMatchEnded)
            {
                yield return Timing.WaitForSeconds(10f);

                Exiled.API.Features.Map.Clean(DecalPoolType.Blood);
                Exiled.API.Features.Map.Clean(DecalPoolType.Bullet);
            }
        }

        private void OnDroppingItem(DroppingItemEventArgs ev)
        {
            ev.IsAllowed = false;
        }

        private void OnDroppingAmmo(DroppingAmmoEventArgs ev)
        {
            ev.IsAllowed = false;
        }

        private void OnShot(ShotEventArgs ev)
        {
            ev.Player.AddAmmo(ev.Firearm.AmmoType, 1);
        }
    }
}
