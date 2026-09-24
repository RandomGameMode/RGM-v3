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
using static RGM.Variables.Variable;

namespace RGM.Modes
{
    [Mode(ModeCategory.Public, ModeInfo.Set, ModeType.TeamMatch)]
    class TeamMatch : Mode
    {
        public override string Name => "팀 데스매치";
        public override string Description => "팀원과 함께 상대 팀을 무찌르고, 승리하세요.";
        public override string Detail =>
"""
랜덤한 맵에서, 팀 승리를 위해 상대 팀을 무찌르세요.
목표 점수에 먼저 도달한 팀이 승리합니다.

목표 점수는 라운드 시작 시, 현재 서버 인원을 기준으로 결정됩니다.

<b>[Map Credit]</b>
@vasileii, @sleeplessbutter
""";
        public override string Color => "CEECF5";

        public static TeamMatch Instance;

        private List<Player> _teamA = new();
        private List<Player> _teamB = new();
        private List<Player> _losingTeam = new();
        private List<Vector3> _teamASpawns = new();
        private List<Vector3> _teamBSpawns = new();

        // Schematic: Spot A/B | YAML player_spawnpoints: Spawn_ClassD_*/Spawn_Scientist_*
        private static readonly string[] TeamASpawnKeys = { "Spot A", "Spawn_ClassD" };
        private static readonly string[] TeamBSpawnKeys = { "Spot B", "Spawn_Scientist" };

        private static readonly List<string> MapsTdm =
        [
            "Battle",
            "Battle_Xmas2025",
        ];

        private CoroutineHandle _onModeStarted;
        private CoroutineHandle _cleanupDecals;
        private CoroutineHandle _scoreHint;
        private int _targetScore;
        private int _teamAScore;
        private int _teamBScore;
        private bool _isMatchEnded;
        private bool _isModeActive;
        private int _modeId;

        public override void OnEnabled()
        {
            _modeId++;
            _teamAScore = 0;
            _teamBScore = 0;
            _isMatchEnded = false;
            _isModeActive = true;
            _losingTeam.Clear();
            Round.IsLocked = true;
            Respawn.PauseWaves();

            Exiled.Events.Handlers.Player.Died += OnDied;
            Exiled.Events.Handlers.Player.Spawned += OnSpawned;
            Exiled.Events.Handlers.Player.SpawnedRagdoll += OnSpawnedRagdoll;
            Exiled.Events.Handlers.Player.DroppingItem += OnDroppingItem;
            Exiled.Events.Handlers.Player.DroppingAmmo += OnDroppingAmmo;
            Exiled.Events.Handlers.Player.Shot += OnShot;

            _onModeStarted = Timing.RunCoroutine(OnModeStarted());
            _cleanupDecals = Timing.RunCoroutine(CleanDecals());
            _scoreHint = Timing.RunCoroutine(ScoreHintCoroutine());
        }

        public override void OnDisabled()
        {
            _isModeActive = false;

            Exiled.Events.Handlers.Player.Died -= OnDied;
            Exiled.Events.Handlers.Player.Spawned -= OnSpawned;
            Exiled.Events.Handlers.Player.SpawnedRagdoll -= OnSpawnedRagdoll;
            Exiled.Events.Handlers.Player.DroppingItem -= OnDroppingItem;
            Exiled.Events.Handlers.Player.DroppingAmmo -= OnDroppingAmmo;
            Exiled.Events.Handlers.Player.Shot -= OnShot;

            Timing.KillCoroutines(_onModeStarted);
            Timing.KillCoroutines(_cleanupDecals);
            Timing.KillCoroutines(_scoreHint);

            foreach (var door in Door.List)
                door.Unlock();

            _teamASpawns.Clear();
            _teamBSpawns.Clear();
            _losingTeam.Clear();
        }

        private IEnumerator<float> OnModeStarted()
        {
            foreach (var door in Door.List)
            {
                door.IsOpen = true;
                door.Lock(DoorLockType.AdminCommand);
            }

            Tools.LoadMap(MapsTdm.GetRandomValue());

            yield return Timing.WaitForSeconds(1f);

            _teamASpawns = Tools.GetSpawnPositions(TeamASpawnKeys);
            _teamBSpawns = Tools.GetSpawnPositions(TeamBSpawnKeys);

            if (_teamASpawns.Count == 0 || _teamBSpawns.Count == 0)
                Log.Error($"[TeamMatch] 스폰 포인트를 찾지 못했습니다. A={_teamASpawns.Count}, B={_teamBSpawns.Count}");

            var players = PlayerManager.List.ToList();
            players.ShuffleList();

            _targetScore = players.Count * 4 > 100 ? 100 : players.Count * 4; 
            int halfCount = players.Count / 2;

            _teamA = players.Take(halfCount).ToList();
            _teamB = players.Skip(halfCount).ToList();

            foreach (var player in _teamA)
            {
                player.Role.Set(RoleTypeId.ClassD, RoleSpawnFlags.None);
                yield return Timing.WaitForOneFrame;
                player.ClearInventory();
                player.Position = _teamASpawns.GetRandomValue();
                foreach (var item in Items())
                    player.AddItem(item);
            }

            foreach (var player in _teamB)
            {
                player.Role.Set(RoleTypeId.Scientist, RoleSpawnFlags.None);
                yield return Timing.WaitForOneFrame;
                player.ClearInventory();
                player.Position = _teamBSpawns.GetRandomValue();
                foreach (var item in Items())
                    player.AddItem(item);
            }

            foreach (var player in PlayerManager.List)
                player.AddBroadcast(10, $"<size=36><b>목표 점수: <color=#D23265>{_targetScore}</color></b></size>");
        }

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

        private void OnDied(DiedEventArgs ev)
        {
            if (_isMatchEnded || (!_teamA.Contains(ev.Player) && !_teamB.Contains(ev.Player)))
                return;

            Exiled.API.Features.Map.CleanAllItems();

            if (_teamA.Contains(ev.Attacker) && _teamB.Contains(ev.Player))
            {
                _teamAScore++;
                CheckWinner(_teamA);
            }
            else if (_teamB.Contains(ev.Attacker) && _teamA.Contains(ev.Player))
            {
                _teamBScore++;
                CheckWinner(_teamB);
            }

            if (!_isMatchEnded)
                Timing.RunCoroutine(RespawnPlayer(ev.Player, _modeId));
        }

        private void OnSpawnedRagdoll(SpawnedRagdollEventArgs ev)
        {
            ev.Ragdoll?.Destroy();
        }

        private void OnSpawned(SpawnedEventArgs ev)
        {
            if (!_isModeActive || !_isMatchEnded || !_losingTeam.Contains(ev.Player))
                return;

            if (ev.Player.Role.Type is RoleTypeId.Tutorial or RoleTypeId.Spectator or RoleTypeId.Overwatch
                or RoleTypeId.Filmmaker or RoleTypeId.None)
                return;

            ev.Player.Role.Set(RoleTypeId.Tutorial, RoleSpawnFlags.None);
        }

        private IEnumerator<float> RespawnPlayer(Player player, int modeId)
        {
            yield return Timing.WaitForSeconds(5f);

            if (!_isModeActive || modeId != _modeId || _isMatchEnded || !Round.IsLocked || player.IsAlive)
                yield break;

            if (_teamA.Contains(player))
            {
                player.Role.Set(RoleTypeId.ClassD, RoleSpawnFlags.None);
                yield return Timing.WaitForOneFrame;

                if (!_isModeActive || modeId != _modeId || _isMatchEnded)
                {
                    if (_losingTeam.Contains(player) && player.IsAlive && player.Role.Type != RoleTypeId.Tutorial)
                        player.Role.Set(RoleTypeId.Tutorial, RoleSpawnFlags.None);
                    yield break;
                }

                player.Position = _teamASpawns.GetRandomValue();
            }
            else if (_teamB.Contains(player))
            {
                player.Role.Set(RoleTypeId.Scientist, RoleSpawnFlags.None);
                yield return Timing.WaitForOneFrame;

                if (!_isModeActive || modeId != _modeId || _isMatchEnded)
                {
                    if (_losingTeam.Contains(player) && player.IsAlive && player.Role.Type != RoleTypeId.Tutorial)
                        player.Role.Set(RoleTypeId.Tutorial, RoleSpawnFlags.None);
                    yield break;
                }

                player.Position = _teamBSpawns.GetRandomValue();
            }
            else
            {
                yield break;
            }

            player.ClearInventory();
            foreach (var item in Items())
                player.AddItem(item);
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

        private IEnumerator<float> ScoreHintCoroutine()
        {
            while (_isModeActive && !_isMatchEnded)
            {
                string scoreText = $"<size=30><b><color=#FF5533>{_teamAScore}</color> : <color=#FFD700>{_teamBScore}</color></b></size>";

                foreach (var player in PlayerManager.List)
                    player.AddHint("팀 데스매치 점수", scoreText, 1.05f);

                yield return Timing.WaitForSeconds(1f);
            }
        }

        private void CheckWinner(List<Player> winningTeam)
        {
            if (_teamAScore < _targetScore && _teamBScore < _targetScore)
                return;

            _isMatchEnded = true;
            _losingTeam = ReferenceEquals(winningTeam, _teamA) ? _teamB : _teamA;

            foreach (var player in _losingTeam.Where(x => x.IsAlive).ToList())
                player.Kill("패배하였습니다...");

            Round.IsLocked = false;
            Timing.RunCoroutine(Tools.SetWinner(winningTeam, 3));
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
