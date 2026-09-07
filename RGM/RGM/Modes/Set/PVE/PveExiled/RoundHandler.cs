using CentralAuth;
using CustomPlayerEffects;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Doors;
using Exiled.API.Features.Pickups;
using InventorySystem;
using MEC;
using Mirror;
using NetworkManagerUtils.Dummies;
using PlayerRoles;
using System;
using System.Collections.Generic;
using System.Linq;
using RGM.API.Features;
using UnityEngine;
using UnityEngine.AI;
using mbc = MultiBroadcast;
using PlayerEvents = Exiled.Events.Handlers.Player;
using ServerEventArgs = Exiled.Events.EventArgs.Server;

namespace RGM.Modes.PveExiledSystem
{
    public class RoundHandler
    {
        private bool roundStarted = false;
        private WaveConfig waveConfig;

        public int CurrentWave { get; private set; } = -1;
        public bool AllWavesCleared { get; private set; }
        public int SelectedDifficulty { get; private set; } = -1;

        private NavMeshDataInstance navMesh;
        public Vector3 playerSpawnPoint;
        public List<Vector3> enemySpawnPoints = new List<Vector3>();
        public Dictionary<int, Enemy> enemies = new Dictionary<int, Enemy>();//퍼블릭으로 바꿈
                                                                             //public string AudioFolder = "";

        private CoroutineHandle runningRound;
        private SpecialWave specialWave;
        private int cursor = 0;

        private AudioClipPlayback _globalBGM;

        public void OnRoundStarted()
        {
            Tools.LoadMap("PVE");
            OnEndingRound();

            roundStarted = true;
            AllWavesCleared = false;
            SelectedDifficulty = -1;

            NavMesh.RemoveAllNavMeshData();

            Round.IsLocked = true;
            foreach (Door door in Door.List)//문잠금
            {
                if (door.Zone != ZoneType.Entrance && door.Zone != ZoneType.Surface) continue;
                door.Lock(10000000, DoorLockType.Regular079);

                ElevatorDoor Edoor = door as ElevatorDoor;
                BreakableDoor Bdoor = door as BreakableDoor;
                if (Edoor != null) { door.IsOpen = false; continue; }
                door.IsOpen = (door.Name != "INTERCOM" && door.Name != "Unsecured" && door.Type != DoorType.EscapePrimary);
                if (Bdoor != null && !door.IsOpen)
                {
                    Bdoor.IsDestroyed = false;
                }
            }
            foreach (Room room in Room.List)//Room for
            {
                if (room.Zone != ZoneType.Entrance) continue;
                switch (room.Type)
                {
                    case RoomType.EzVent or RoomType.EzCollapsedTunnel or RoomType.EzShelter:
                    {
                        if (room.Type is RoomType.EzCollapsedTunnel or RoomType.EzShelter)
                        {
                            enemySpawnPoints.Add(room.Doors.First().Position + Vector3.up);
                        }
                        else
                        {
                            enemySpawnPoints.Add(room.Position + Vector3.up);
                        }
                        continue;
                    }
                    case RoomType.EzUpstairsPcs:
                        playerSpawnPoint = room.Position + Vector3.up;
                        continue;
                    case RoomType.EzCheckpointHallwayA:
                    case RoomType.EzCheckpointHallwayB:
                    case RoomType.EzGateA:
                    case RoomType.EzGateB:
                    {
                        foreach (Door door in room.Doors)
                        {
                            door.IsOpen = false;
                        }

                        break;
                    }
                }
            }
            foreach (Pickup pickup in Pickup.List) pickup.Destroy();
            Warhead.DeadmanSwitchEnabled = false;
            Warhead.IsLocked = true;

            var bounds = new Bounds(Vector3.zero, new Vector3(2000, 2000, 2000));
            var markups = new List<NavMeshBuildMarkup>();
            var sources = new List<NavMeshBuildSource>();

            int maskInvCol = LayerMask.GetMask("Default", "InvisibleCollider");
            NavMeshBuilder.CollectSources(bounds, maskInvCol, NavMeshCollectGeometry.PhysicsColliders, 0, markups, sources);

            var settings = NavMesh.CreateSettings();
            settings.agentRadius = .17f;
            settings.agentHeight = 1f;
            settings.agentClimb = .24f;
            settings.agentSlope = 45;

            var data = NavMeshBuilder.BuildNavMeshData(settings, sources, bounds, Vector3.zero, Quaternion.identity);
            navMesh = NavMesh.AddNavMeshData(data);

            runningRound = Timing.RunCoroutine(DoRound());
        }
        public void OnEndingRound()//라운드종료
        {
            if (!roundStarted) return;
            roundStarted = false;
            Round.IsLocked = false;

            enemySpawnPoints.Clear();

            if (_globalBGM != null)
                _globalBGM.IsPaused = true;
            _globalBGM = null;

            //이벤트해제
            if (waveConfig != null)
            {
                SelectedDifficulty = waveConfig.Difficulty;
                PlayerEvents.Hurting -= waveConfig.OnHurting;
                Exiled.Events.Handlers.Map.PickupAdded -= waveConfig.OnPickupAdded;
                Exiled.Events.Handlers.Map.PlacingBulletHole -= waveConfig.OnPlacingBulletHole;
                Exiled.Events.Handlers.Server.RespawningTeam -= waveConfig.OnRespawningTeam;
                Exiled.Events.Handlers.Map.AnnouncingScpTermination -= waveConfig.OnAnnouncingScpTermination;
                PlayerEvents.ChangingRole -= waveConfig.OnChangingRole;
                PlayerEvents.ThrownProjectile -= waveConfig.OnThrownProjectile;
            }
            waveConfig = null;

            NavMesh.RemoveAllNavMeshData();
            if (specialWave != null)
            {
                specialWave.Disable();
                specialWave = null;
            }
            Timing.KillCoroutines(runningRound);

            foreach (Enemy enemy in enemies.Values.ToList())
            {
                enemy.RemoveEnemy();
            }
            enemies.Clear();
            DummyUtils.DestroyAllDummies();
        }
        public void OnEndingRound(ServerEventArgs.EndingRoundEventArgs _) => OnEndingRound();

        private IEnumerator<float> DoRound()//라운드진행
        {
            yield return Timing.WaitForSeconds(3);

            {//투표블럭
                if (UnityEngine.Random.value < 0.2f)
                {
                    foreach (Player player in Player.List)//투표스폰
                    {
                        if (!IsValidPlayer(player)) continue;
                        player.Role.Set(RoleTypeId.Tutorial, SpawnReason.ForceClass);
                    }
                    for (int i = 5; i > 0; i--)
                    {
                        mbc.API.MultiBroadcast.AddMapBroadcast(1, $"<color=\"red\">투표권은 없습니다: {i}</color>");
                        yield return Timing.WaitForSeconds(1);
                    }
                    waveConfig = new Difficulty.Insane();
                }
                else
                {
                    Vector3 spawnPoint = Room.Get(RoomType.Lcz914).Position + Vector3.up;
                    Vector3 hang = Room.Get(RoomType.Lcz914).transform.forward;

                    foreach (Player player in Player.List)//투표스폰
                    {
                        if (!IsValidPlayer(player)) continue;
                        player.Role.Set(RoleTypeId.Tutorial);
                        Timing.CallDelayed(1f, () =>
                        {
                            if (player == null || player.Role.Type != RoleTypeId.Tutorial) return;
                            player.Position = spawnPoint;
                        });
                    }

                    Dictionary<int, int> plrVote = new Dictionary<int, int>();
                    int[] difficultyVote = new int[3] { 0, 0, 0 };
                    ReferenceHub[] dummyList = new ReferenceHub[3] { DummyUtils.SpawnDummy("일반"), DummyUtils.SpawnDummy("어려움"), DummyUtils.SpawnDummy("지옥") };
                    dummyList[0].authManager.syncMode = (SyncMode)ClientInstanceMode.Dummy;
                    dummyList[1].authManager.syncMode = (SyncMode)ClientInstanceMode.Dummy;
                    dummyList[2].authManager.syncMode = (SyncMode)ClientInstanceMode.Dummy;
                    Player.Get(dummyList[0]).Role.Set(RoleTypeId.ClassD);
                    Player.Get(dummyList[1]).Role.Set(RoleTypeId.ChaosMarauder);
                    Player.Get(dummyList[1]).ClearInventory();
                    Player.Get(dummyList[2]).Role.Set(RoleTypeId.Scp173);
                    dummyList[0].transform.position = spawnPoint + hang * 5;
                    dummyList[1].transform.position = spawnPoint;
                    dummyList[2].transform.position = spawnPoint + hang * -5;

                    foreach (Player player in Player.List)
                    {
                        if (!IsValidPlayer(player) || player.Role.Type != RoleTypeId.Tutorial) continue;
                        plrVote.Add(player.Id, 3);
                    }
                    for (int i = 15; i > 0; i--)
                    {
                        mbc.API.MultiBroadcast.AddMapBroadcast(1, $"투표 종료까지 남은 시간: {i}");

                        foreach (Player player in Player.List)
                        {
                            if (!IsValidPlayer(player) || player.Role.Type != RoleTypeId.Tutorial) continue;
                            int old = plrVote[player.Id];
                            int vote = 3;
                            if ((dummyList[0].transform.position - player.Position).sqrMagnitude < 5)
                            {
                                vote = 0;
                            }
                            else if ((dummyList[1].transform.position - player.Position).sqrMagnitude < 5)
                            {
                                vote = 1;
                            }
                            else if ((dummyList[2].transform.position - player.Position).sqrMagnitude < 5)
                            {
                                vote = 2;
                            }
                            if (old != vote)
                            {
                                if (old <= 2) difficultyVote[old]--;
                                if (vote <= 2) difficultyVote[vote]++;
                                plrVote[player.Id] = vote;
                            }
                        }

                        Map.ShowHint($"""
                                      <color="orange">보통</color>: {difficultyVote[0]}

                                      <color="green">어려움</color>: {difficultyVote[1]}

                                      <color="red">지옥</color>: {difficultyVote[2]}
                                      """, 1.01f);

                        yield return Timing.WaitForSeconds(1f);
                    }
                    DummyUtils.DestroyAllDummies();
                    int selected = 0;
                    if (difficultyVote[1] >= difficultyVote[selected]) selected = 1;
                    if (difficultyVote[2] >= difficultyVote[selected]) selected = 2;
                    switch (selected)
                    {
                        case 0: waveConfig = new Difficulty.Normal(); break;
                        case 1: waveConfig = new Difficulty.Hard(); break;
                        default: waveConfig = new Difficulty.Insane(); break;
                    }
                }
                mbc.API.MultiBroadcast.AddMapBroadcast(5, $"선택된 난이도: {waveConfig.DifficultyName}");
                //이벤트연결
                PlayerEvents.Hurting += waveConfig.OnHurting;
                Exiled.Events.Handlers.Map.PickupAdded += waveConfig.OnPickupAdded;
                Exiled.Events.Handlers.Map.PlacingBulletHole += waveConfig.OnPlacingBulletHole;
                Exiled.Events.Handlers.Server.RespawningTeam += waveConfig.OnRespawningTeam;
                Exiled.Events.Handlers.Map.AnnouncingScpTermination += waveConfig.OnAnnouncingScpTermination;
                PlayerEvents.ChangingRole += waveConfig.OnChangingRole;
                PlayerEvents.ThrownProjectile += waveConfig.OnThrownProjectile;

            }
            yield return Timing.WaitForSeconds(5);
            bool won = true;
            for (int wave = 0; wave < waveConfig.Waves.Length; wave++)//웨이브 진행 루프
            {
                WaveConfig.WaveInfo waveInfo = waveConfig.Waves[wave];
                CurrentWave = wave;
                Map.Clean(Decals.DecalPoolType.Blood);
                Map.Clean(Decals.DecalPoolType.GlassCrack);
                Map.Clean(Decals.DecalPoolType.Bullet);
                Map.Clean(Decals.DecalPoolType.Buckshot);

                DummyUtils.DestroyAllDummies();
                if (wave == 0 || (wave + 1) % 3 == 0)
                {
                    SpawnPlayers();
                    Map.CleanAllItems();
                }
                yield return Timing.WaitForSeconds(1);
                waveConfig.MulCount = GetAlivePlayerCount() - 1;

                int mulCount = waveConfig.MulCount;
                Map.ShowHint("살아있는 플레이어 수: " + (mulCount + 1), duration: 10);
                foreach (WaveConfig.SupplySpawnInfo itemInfo in waveInfo.SupplySpawnInfos)//보급품
                {
                    int imax = (int)(itemInfo.Amount + mulCount * itemInfo.ItemPerPlayer);
                    if (imax <= 0) continue;
                    for (int i = 0; i < imax; i++)
                    {
                        Pickup pickup = Pickup.Create(itemInfo.Type);
                        pickup.Position = playerSpawnPoint + new Vector3(UnityEngine.Random.value * 5 - 2.5f, 3, UnityEngine.Random.value * 5 - 2.5f);
                        pickup.Spawn();
                    }
                }
                Timing.CallDelayed(0.5f, () => {//템지급
                    if (waveInfo.SupplyGiveInfos != null)
                    {
                        foreach (ItemType itemType in waveInfo.SupplyGiveInfos)
                        {
                            foreach (Player player in Player.List)
                            {
                                if (!IsAlivePlayer(player)) continue;
                                player.AddItem(itemType);
                            }
                        }
                    }
                });

                for (int i = waveInfo.IntermissionTime; i > 0; i--)//타이머
                {
                    //mbc.API.MultiBroadcast.AddMapBroadcast(duration: 1, text: $"다음 웨이브까지 남은 시간: {i}");
                    mbc.API.MultiBroadcast.AddMapBroadcast(1, $"<size=20>다음 웨이브까지 남은 시간: {i}</size>");
                    yield return Timing.WaitForSeconds(1f);
                }
                if (waveConfig.IsSpecial)
                {
                    mbc.API.MultiBroadcast.AddMapBroadcast(duration: 10, text: waveInfo.BCtext);

                    List<string> spawnQueue = new List<string>();
                    int maxEnemy = (int)(waveInfo.MaxEnemyCount + waveConfig.MulCount * waveInfo.MaxEnemyPerPlayer);
                    int minEnemy = (int)(waveInfo.MinEnemyCount + waveConfig.MulCount * waveInfo.MinEnemyPerPlayer);
                    if (minEnemy >= maxEnemy) minEnemy = maxEnemy - 2;
                    foreach (WaveConfig.EnemySpawnInfo spawnInfo in waveInfo.EnemySpawnInfos)//적 스폰
                    {
                        for (int i = 0; i < (int)(spawnInfo.Amount + mulCount * spawnInfo.EnemyPerPlayer); i++)
                        {
                            spawnQueue.Add(spawnInfo.EnemyName);
                        }
                    }
                    while (true)
                    {
                        if (spawnQueue.Count <= 0) break;
                        string random = spawnQueue.Last();
                        spawnQueue.RemoveAt(spawnQueue.Count - 1);
                        SpawnEnemy(random);
                        if (enemies.Count >= maxEnemy)
                        {
                            while (enemies.Count > minEnemy && GetAlivePlayerCount() > 0) yield return Timing.WaitForSeconds(5);
                            if (GetAlivePlayerCount() <= 0) { yield return Timing.WaitForSeconds(1); break; }
                        }
                        else yield return Timing.WaitForSeconds(0.8f);
                    }
                    while (enemies.Count > 0 && GetAlivePlayerCount() > 0) yield return Timing.WaitForSeconds(5);//ㄱㄷ
                }
                else if (wave < waveConfig.Waves.Length - 1 && UnityEngine.Random.value < 0.3f)
                {
                    //스페셜웨이브
                    Type[] types = [
                    typeof(SpecialWaves.Blackout),
                    typeof(SpecialWaves.DemolisherRush),
                    typeof(SpecialWaves.Spirit),
                    typeof(SpecialWaves.Mirrored),
                    typeof(SpecialWaves.Upgraded)
                    ];

                    Type selectedType = types[UnityEngine.Random.Range(0, types.Length)];
                    specialWave = (SpecialWave)Activator.CreateInstance(selectedType);

                    mbc.API.MultiBroadcast.AddMapBroadcast(duration: 10, text: $"스페셜 웨이브: {specialWave.SpecialWaveName}");

                    Tools.PlayGlobalAudio("SpecialWaveSound", volume:1.5f, isNoNotice: true);

                    yield return Timing.WaitForSeconds(7f);

                    _globalBGM = Tools.PlayGlobalAudio(specialWave.SoundtrackName, volume:1.5f, loop: true, isNoNotice: true);
                    specialWave.Enable(this, waveConfig, waveInfo);
                    while (!specialWave.Ended) yield return Timing.WaitForSeconds(1);//ㄱㄷ22
                    _globalBGM.IsPaused = true;
                    _globalBGM = null;
                    specialWave = null;
                }
                else
                {
                    mbc.API.MultiBroadcast.AddMapBroadcast(duration: 10, text: waveInfo.BCtext);
                    Tools.PlayGlobalAudio("WaveStartSound", volume:1.5f, isNoNotice: true);

                    List<string> spawnQueue = new List<string>();
                    int maxEnemy = (int)(waveInfo.MaxEnemyCount + waveConfig.MulCount * waveInfo.MaxEnemyPerPlayer);
                    int minEnemy = (int)(waveInfo.MinEnemyCount + waveConfig.MulCount * waveInfo.MinEnemyPerPlayer);
                    if (minEnemy >= maxEnemy) minEnemy = maxEnemy - 2;
                    foreach (WaveConfig.EnemySpawnInfo spawnInfo in waveInfo.EnemySpawnInfos)//적 스폰
                    {
                        for (int i = 0; i < (int)(spawnInfo.Amount + mulCount * spawnInfo.EnemyPerPlayer); i++)
                        {
                            spawnQueue.Add(spawnInfo.EnemyName);
                        }
                    }
                    while (true)
                    {
                        if (spawnQueue.Count <= 0) break;
                        string random = spawnQueue.RandomItem();
                        spawnQueue.Remove(random);
                        SpawnEnemy(random);
                        if (enemies.Count >= maxEnemy)
                        {
                            while (enemies.Count > minEnemy && GetAlivePlayerCount() > 0) yield return Timing.WaitForSeconds(5);
                            if (GetAlivePlayerCount() <= 0) { yield return Timing.WaitForSeconds(1); break; }
                        }
                        else yield return Timing.WaitForSeconds(0.8f);
                    }
                    while (enemies.Count > 0 && GetAlivePlayerCount() > 0) yield return Timing.WaitForSeconds(5);//ㄱㄷ
                }

                if (GetAlivePlayerCount() <= 0) { won = false; break; }
                Tools.PlayGlobalAudio("WaveEndSound", volume:1.5f, isNoNotice: true);
            }

            mbc.API.MultiBroadcast.AddMapBroadcast(duration: 10,
                text: won
                    ? "<color=#1010ff>Site-02</color> 구성원들이 시설 방어에 성공했습니다."
                    : "<color=#1010ff>Site-02</color> 시설이 <color=#80ff80>혼돈의 반란</color> 세력에게 점령당했습니다.");
            AllWavesCleared = won;
            OnEndingRound();
        }

        private void SpawnPlayers()
        {
            foreach (Player player in Player.List)
            {
                if (!IsValidPlayer(player)) continue;
                if (player.Role.Type == RoleTypeId.Scientist) continue;
                player.Role.Set(RoleTypeId.Scientist);
                Timing.CallDelayed(0.25f, () =>
                {
                    if (player == null || player.Role.Type != RoleTypeId.Scientist) return;
                    player.ClearInventory();
                    player.Position = playerSpawnPoint;
                    if (!waveConfig.IsSpecial)
                    {
                        if (CurrentWave >= 5)
                        {
                            player.AddItem(ItemType.GunFSP9);
                            player.AddItem(ItemType.Ammo9x19, 10);
                        }
                        else
                        {
                            player.AddItem(ItemType.GunCOM18);
                            player.AddItem(ItemType.Ammo9x19, 5);
                        }
                    }
                    player.EnableEffect(EffectType.HeavyFooted, 100, -1);
                });
            }
        }
        public void SpawnEnemy(string enemyName)//priv->pub
        {
            Enemy enemy;
            switch (enemyName)
            {
                case "Gunner": enemy = new Enemies.Gunner(enemyName, enemySpawnPoints.RandomItem<Vector3>(), cursor, enemies, waveConfig); break;
                case "ClassD": enemy = new Enemies.ClassD(enemyName, enemySpawnPoints.RandomItem<Vector3>(), cursor, enemies, waveConfig); break;
                case "Scout": enemy = new Enemies.Scout(enemyName, enemySpawnPoints.RandomItem<Vector3>(), cursor, enemies, waveConfig); break;
                case "Cloaker": enemy = new Enemies.Cloaker(enemyName, enemySpawnPoints.RandomItem<Vector3>(), cursor, enemies, waveConfig); break;
                case "Pyromancer": enemy = new Enemies.Pyromancer(enemyName, enemySpawnPoints.RandomItem<Vector3>(), cursor, enemies, waveConfig); break;
                case "Juggernaut": enemy = new Enemies.Juggernaut(enemyName, enemySpawnPoints.RandomItem<Vector3>(), cursor, enemies, waveConfig); break;
                case "Demolisher": enemy = new Enemies.Demolisher(enemyName, enemySpawnPoints.RandomItem<Vector3>(), cursor, enemies, waveConfig); break;
                case "Tranquilizer": enemy = new Enemies.Tranquilizer(enemyName, enemySpawnPoints.RandomItem<Vector3>(), cursor, enemies, waveConfig); break;
                case "Rifleman": enemy = new Enemies.Rifleman(enemyName, enemySpawnPoints.RandomItem<Vector3>(), cursor, enemies, waveConfig); break;
                case "Zombie": enemy = new Enemies.Zombie(enemyName, enemySpawnPoints.RandomItem<Vector3>(), cursor, enemies, waveConfig); break;
                case "Assassin": enemy = new Enemies.Assassin(enemyName, enemySpawnPoints.RandomItem<Vector3>(), cursor, enemies, waveConfig); break;
                case "Striker": enemy = new Enemies.Striker(enemyName, enemySpawnPoints.RandomItem<Vector3>(), cursor, enemies, waveConfig); break;
                case "Sniper": enemy = new Enemies.Sniper(enemyName, enemySpawnPoints.RandomItem<Vector3>(), cursor, enemies, waveConfig); break;
                default: enemy = new Enemies.ClassD(enemyName, enemySpawnPoints.RandomItem<Vector3>(), cursor, enemies, waveConfig); break;
            }
            enemies.Add(cursor, enemy);
            cursor++;
        }
        public int GetAlivePlayerCount()//priv->pub
        {
            int count = 0;
            foreach (Player player in Player.List)
            {
                if (!IsValidPlayer(player)) continue;
                if (player.Role.Type != RoleTypeId.Scientist) continue;
                count++;
            }
            return count;
        }
        private bool IsValidPlayer(Player player)
        {
            if (player == null) return false;
            if (player.UserId == "ID_Dedicated" || player.UserId == "ID_Dummy" || player.IsNPC)
            {
                if (player.Nickname != "Tester") return false;
            }
            return true;
        }
        private bool IsAlivePlayer(Player player)
        {
            if (player == null) return false;
            if (player.UserId == "ID_Dedicated" || player.UserId == "ID_Dummy" || player.IsNPC)
            {
                if (player.Nickname != "Tester") return false;
            }
            if (player.Role.Type != RoleTypeId.Scientist) return false;
            return true;
        }
    }
}