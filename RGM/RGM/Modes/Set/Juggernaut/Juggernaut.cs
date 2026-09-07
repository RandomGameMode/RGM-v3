using Exiled.API.Features.DamageHandlers;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using MEC;
using UnityEngine;
using Exiled.API.Features.Items;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using PlayerRoles;
using RGM.API.Features;
using static RGM.Variables.Variable;
using Respawning;
using Exiled.API.Features.Waves;
using Door = Exiled.API.Features.Doors.Door;
using ChaosMiniWave = Respawning.Waves.ChaosMiniWave;
using NtfMiniWave = Respawning.Waves.NtfMiniWave;
using Player = Exiled.API.Features.Player;
using Round = Exiled.API.Features.Round;
using Server = Exiled.API.Features.Server;
using SpawnableWaveBase = Respawning.Waves.SpawnableWaveBase;
using Warhead = Exiled.API.Features.Warhead;

namespace RGM.Modes
{
    [Mode(ModeCategory.Public, ModeInfo.Set, ModeType.Juggernaut)]
    class Juggernaut : Mode
    {
        public override string Name => "저거너트";
        public override string Description => "모두 힘을 합쳐 외부의 적에 대항하세요.";

        public override string Detail =>
            """
            지금은 우리(SCP, 반란, MTF)끼리 싸울 때가 아닙니다.
            군대를 홀로 섬멸할 수 있는 전력인 저거너트가 재단을 점령하기 위해 왔습니다.

            이제 친구가 될 시간이야.

            * 게임 시작 12분 뒤 <color=red>자동핵</color>이 작동됩니다.
            * 저거너트는 알파 핵탄두가 폭파할 경우 즉시 과부하 프로토콜이 작동합니다.
            """;

        public override string Color => "088A08";

        public static Juggernaut Instance;

        private Player _juggernaut;
        private readonly List<Player> _scpAttackCooldown = [];
        private readonly Dictionary<Player, float> _playerDamages = [];
        private readonly HashSet<SpawnableWaveBase> _juggernautExternalSupportWaves = [];
        private Speaker _speaker;

        private float _stack;

        private CoroutineHandle _onModeStarted;
        private CoroutineHandle _autoWarhead;
        private CoroutineHandle _findLocate;
        private CoroutineHandle _musicAsync;
        private CoroutineHandle _reduceWaveTimer;

        public override void OnEnabled()
        {
            Server.FriendlyFire = true;
            Round.IsLocked = true;

            Exiled.Events.Handlers.Player.Spawned += OnSpawned;
            Exiled.Events.Handlers.Player.SearchingPickup += OnSearchingPickup;
            Exiled.Events.Handlers.Player.DroppingItem += OnDroppingItem;
            Exiled.Events.Handlers.Player.Shooting += OnShooting;
            Exiled.Events.Handlers.Player.Hurting += OnHurting;
            Exiled.Events.Handlers.Player.ReceivingEffect += OnReceivingEffect;
            Exiled.Events.Handlers.Player.Handcuffing += OnHandcuffing;

            Exiled.Events.Handlers.Server.RespawnedTeam += OnRespawnedTeam;

            Exiled.Events.Handlers.Item.ChargingJailbird += OnChargingJailbird;

            _onModeStarted = Timing.RunCoroutine(OnModeStarted());
            _autoWarhead = Timing.RunCoroutine(AutoWarhead());
            _findLocate = Timing.RunCoroutine(FindLocate());
            _musicAsync = Timing.RunCoroutine(MusicAsync());
            _reduceWaveTimer = Timing.RunCoroutine(ReduceWaveTimer());
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Player.Spawned -= OnSpawned;
            Exiled.Events.Handlers.Player.SearchingPickup -= OnSearchingPickup;
            Exiled.Events.Handlers.Player.DroppingItem -= OnDroppingItem;
            Exiled.Events.Handlers.Player.Shooting -= OnShooting;
            Exiled.Events.Handlers.Player.Hurting -= OnHurting;
            Exiled.Events.Handlers.Player.ReceivingEffect -= OnReceivingEffect;
            Exiled.Events.Handlers.Player.Handcuffing -= OnHandcuffing;

            Exiled.Events.Handlers.Server.RespawnedTeam -= OnRespawnedTeam;

            Exiled.Events.Handlers.Item.ChargingJailbird -= OnChargingJailbird;

            _juggernautExternalSupportWaves.Clear();

            Timing.KillCoroutines(_onModeStarted);
            Timing.KillCoroutines(_autoWarhead);
            Timing.KillCoroutines(_findLocate);
            Timing.KillCoroutines(_musicAsync);
            Timing.KillCoroutines(_reduceWaveTimer);

            if (_speaker != null)
                _speaker.Destroy();
        }

        private IEnumerator<float> JuggernautExternalTimer()
        {
            for (int i = 1; i < 75; i++)
            {
                if (Round.IsEnded) yield break;
                PlayerManager.List.ToList()
                    .ForEach(x => x.AddBroadcast(1, $"<size=25>저거너트 외부 지원 호출까지 {75 - i}초</size>"));

                yield return Timing.WaitForSeconds(1f);
            }
        }

        private IEnumerator<float> RoundTimer()
        {
            for (int i = 1; i < 821; i++)
            {
                if (Round.IsEnded || Warhead.IsDetonated)
                    yield break;

                PlayerManager.List.ToList()
                    .ForEach(x => x.AddBroadcast(1, $"<size=25>저거너트 과부하 프로토콜 가동까지 {821 - i}초</size>"));

                yield return Timing.WaitForSeconds(1f);
            }
        }

        private IEnumerator<float> AnnihilationTimer()
        {
            for (int i = 1; i < 120; i++)
            {
                if (Round.IsEnded)
                    yield break;

                PlayerManager.List.ToList().ForEach(x => x.AddBroadcast(1, $"<size=25>초토화 작전 실행까지 {120 - i}초</size>"));

                yield return Timing.WaitForSeconds(1f);
            }
        }

        private bool IsJuggernautTeam(Player player)
        {
            return player == _juggernaut || player.Role.Type == RoleTypeId.Tutorial;
        }

        private bool TryGetExternalSupportWave<T>(out SpawnableWaveBase wave) where T : SpawnableWaveBase
        {
            if (WaveManager.TryGet<T>(out T spawnWave))
            {
                wave = spawnWave;
                return true;
            }

            wave = null;
            return false;
        }

        private bool TryGetRandomSupportWave(out SpawnableWaveBase wave)
        {
            if (Random.Range(1, 3) == 1)
            {
                return TryGetExternalSupportWave<NtfMiniWave>(out wave) ||
                       TryGetExternalSupportWave<ChaosMiniWave>(out wave);
            }

            return TryGetExternalSupportWave<ChaosMiniWave>(out wave) ||
                   TryGetExternalSupportWave<NtfMiniWave>(out wave);
        }

        private void CallJuggernautExternalSupport()
        {
            if (Round.IsEnded || !PlayerManager.List.Any(x => x.IsDead && x.Role.Type != RoleTypeId.Overwatch))
                return;

            if (!TryGetRandomSupportWave(out SpawnableWaveBase wave))
                return;

            _juggernautExternalSupportWaves.Add(wave);
            Timing.CallDelayed(10f, () => _juggernautExternalSupportWaves.Remove(wave));
            WaveManager.Spawn(wave);
        }

        private void CallRegularSupport()
        {
            if (Round.IsEnded || !PlayerManager.List.Any(x => x.IsDead && x.Role.Type != RoleTypeId.Overwatch))
                return;

            if (!TryGetRandomSupportWave(out SpawnableWaveBase wave))
                return;

            Respawn.GrantTokens(wave.TargetFaction, 1);

            if (!WaveTimer.TryGetWaveTimers(wave.TargetFaction, out List<WaveTimer> waves))
                return;

            foreach (var w in waves)
                w.SetTime(0);
        }

        private void TriggerAnnihilation()
        {
            Warhead.Detonate();
            foreach (var player in PlayerManager.List.Where(x => true))
            {
                player.Kill("초토화 작전으로 인해 모든것이 허무로 돌아갑니다...");
            }
        }

        private IEnumerator<float> OnModeStarted()
        {
            if (Random.Range(1, 101) <= 10)
            {
                //10% 확률로 워크스테이션 업그레이드 시작
                Tools.TryInstallMode(ModeType.ABattle);
            }

            Door.Get(DoorType.EscapeFinal).IsOpen = true;

            foreach (var player in PlayerManager.List)
            {
                Spawned(player);
            }

            _juggernaut = PlayerManager.List.ToList().GetRandomValue();
            _juggernaut.Role.Set(RoleTypeId.Tutorial);
            _juggernaut.Scale = new Vector3(1.12f, 1.12f, 1.12f);
            _juggernaut.MaxHealth = 660 * PlayerManager.List.Count;
            _juggernaut.Health = _juggernaut.MaxHealth;
            _juggernaut.IsBypassModeEnabled = true;
            _juggernaut.EnableEffect(EffectType.SinkHole);
            _juggernaut.EnableEffect(EffectType.Slowness, 5);
            _juggernaut.EnableEffect(EffectType.BodyshotReduction, 4);
            _juggernaut.AddBroadcast(10, "<b><size=30>당신은 <color=#298A08>저거너트</color>입니다.</size></b>\n<size=25>본인을 제외한 모두를 사살하십시오.</size>");
            _juggernaut.Position = new Vector3(123.3271f, 288.7908f, 27.01838f);

            List<ItemType> items =
            [
                ItemType.GunLogicer,
                ItemType.Jailbird,
                ItemType.ArmorHeavy
            ];
            foreach (var item in items)
                _juggernaut.AddItem(item);

            Timing.RunCoroutine(MonitorRoundEnd());

            // 이 아래부터 초토화 작전 부 까지, Warhead.IsDetonated 이후 작동되도록 설계해야 함.
            Timing.RunCoroutine(RoundTimer());
            while (!Round.IsEnded && !Warhead.IsDetonated)
                yield return Timing.WaitForSeconds(1f);

            if (Round.IsEnded)
                yield break;

            _juggernaut.DisableAllEffects();
            _juggernaut.EnableEffect(EffectType.BodyshotReduction, 4);
            _juggernaut.EnableEffect(EffectType.Lightweight, 10);
            _juggernaut.EnableEffect(EffectType.NightVision, 70);
            _juggernaut.EnableEffect(EffectType.Scp1344, 1);
            _juggernaut.EnableEffect(EffectType.Scp1853, 1);
            _juggernaut.EnableEffect(EffectType.Scp207, 3);
            _juggernaut.EnableEffect(EffectType.MovementBoost, 5);
            _juggernaut.EnableEffect(EffectType.FogControl, 1);
            _juggernaut.EnableEffect(EffectType.Bleeding, 5);
            _juggernaut.EnableEffect(EffectType.Burned, 1);

            _juggernaut.ClearInventory();
            _juggernaut.AddItem(ItemType.GunLogicer);
            _juggernaut.AddItem(ItemType.ArmorHeavy);
            
            _juggernaut.AddHint("", "과부하의 영항으로 Jailbird가 파괴되었습니다.", 10f);
            
            PlayerManager.List.ToList().ForEach(x => x.AddBroadcast(10,
                "<b><size=30><color=#298A08>저거너트</color>가 과부하 상태에 돌입합니다!</size></b>\n<size=25>모든 능력치가 강화되는 대신 지속 피해를 받으며, 받는 피해가 25% 증가합니다.</size>"));
            yield return Timing.WaitForSeconds(10f);

            // 저거너트 외부 지원 호출 구현(2번에 나눠서)
            Timing.RunCoroutine(JuggernautExternalTimer());
            yield return Timing.WaitForSeconds(75f); // 1차 호출
            CallJuggernautExternalSupport();

            Timing.RunCoroutine(JuggernautExternalTimer());
            yield return Timing.WaitForSeconds(75f); // 2차 호출
            CallJuggernautExternalSupport();

            // 초토화 작전 구현
            Timing.RunCoroutine(AnnihilationTimer());
            yield return Timing.WaitForSeconds(120f); // 외부지원 호출에도 게임이 끝나지 않을 경우
            TriggerAnnihilation();
        }

        private IEnumerator<float> MonitorRoundEnd()
        {
            bool IsEnd = false;
            bool juggernautWon = false;
            List<Player> winners = [];
            while (!IsEnd && !Round.IsEnded)
            {
                if (_juggernaut.IsAlive)
                {
                    List<Player> alivePlayers = PlayerManager.List.Where(x => x.IsAlive && !x.IsNPC).ToList();
                    List<Player> aliveJuggernautTeam = alivePlayers.Where(IsJuggernautTeam).ToList();

                    if (!alivePlayers.Except(aliveJuggernautTeam).Any())
                    {
                        PlayerManager.List.ToList().ForEach(x => x.AddBroadcast(20,
                            "<size=25>더 이상 저지할 수 있는 <b>Site-76 구성원</b>이 없습니다.</size>\n<color=#298A08>저거너트</color>의 승리입니다."));
                        IsEnd = true;
                        juggernautWon = true;
                        winners = aliveJuggernautTeam;
                        Timing.RunCoroutine(Tools.SetWinner(winners, winners.Count == 1 ? 20 : 4));
                    }
                }
                else
                {
                    winners = PlayerManager.List.Where(x => !x.IsNPC && !IsJuggernautTeam(x)).ToList();
                    foreach (var ally in PlayerManager.List.Where(x => x.IsAlive && IsJuggernautTeam(x)))
                        ally.Kill("저거너트가 사망했습니다.");

                    PlayerManager.List.ToList().ForEach(x => x.AddBroadcast(20,
                        "<size=25><color=#298A08>저거너트</color>가 사망했습니다.</size>\n<b>Site-76 구성원</b>들의 승리입니다."));
                    IsEnd = true;
                    Timing.RunCoroutine(Tools.SetWinner(winners, 2));
                }

                yield return Timing.WaitForSeconds(1f);
            }

            if (juggernautWon)
                PlayerManager.List.ToList().ForEach(x => x.Role.Set(RoleTypeId.Tutorial, RoleSpawnFlags.None));
            else
                winners.ForEach(x => x.Role.Set(RoleTypeId.FacilityGuard, RoleSpawnFlags.None));

            Round.IsLocked = false;
        }

        private IEnumerator<float> AutoWarhead()
        {
            yield return Timing.WaitForSeconds(11 * 60);

            if (Warhead.IsDetonated)
                yield break;

            Tools.MessageTranslated("", $"1분 뒤 <color=red>자동핵</color>이 작동됩니다.");

            if (Warhead.IsDetonated)
                yield break;

            yield return Timing.WaitForSeconds(1 * 60);

            DeadmanSwitch.StartWarhead();
        }

        public IEnumerator<float> FindLocate()
        {
            while (!Round.IsEnded)
            {
                if (_juggernaut.TryGetNearestPlayer(out _, out var radius))
                {
                    int alivePlayersCount =
                        PlayerManager.List.Count(x => x.IsAlive && !x.IsNPC && !IsJuggernautTeam(x));
                    _juggernaut.AddHint("저거너트 레이더",
                        $"<b>[ 남은 인원 : {alivePlayersCount}명 | 가장 가까운 유기체와의 거리 : {radius:F1}m ]</b>", 1.1f);
                }
                else
                    _juggernaut.AddHint("저거너트 승리", "당신은 임무를 완수하였습니다.", 1.1f);

                yield return Timing.WaitForSeconds(1f);
            }
        }

        private IEnumerator<float> MusicAsync()
        {
            AudioPlayer audioPlayer = AudioPlayer.CreateOrGet($"Player {_juggernaut.DisplayNickname}",
                condition: (ReferenceHub hub) => { return !MuteBGMPlayers.Contains(Player.Get(hub)); },
                onIntialCreation: (p) =>
                {
                    p.transform.parent = _juggernaut.GameObject.transform;

                    Speaker speaker = p.AddSpeaker("Main", isSpatial: true, minDistance: 5f, maxDistance: 20f);

                    speaker.transform.parent = _juggernaut.GameObject.transform;
                    speaker.transform.localPosition = Vector3.zero;
                });

            audioPlayer.TryPlay("JuggernautTheme", loop: true);

            yield return 0;
        }

        private IEnumerator<float> ReduceWaveTimer()
        {
            while (true)
            {
                foreach (var wave in WaveManager.Waves)
                {
                    bool flag = WaveTimer.TryGetWaveTimers(wave.TargetFaction, out List<WaveTimer> waves);

                    foreach (var w in waves)
                        w.SetTime((int)w.TimeLeft.TotalSeconds - 5);
                }

                yield return Timing.WaitForSeconds(0.5f);
            }
        }

        private void OnSpawned(SpawnedEventArgs ev)
        {
            Spawned(ev.Player);
        }

        private void OnRespawnedTeam(RespawnedTeamEventArgs ev)
        {
            if (!_juggernautExternalSupportWaves.Remove(ev.Wave))
                return;

            EventArgs.ServerEvents.CallTutorialSupport(ev.Players);
        }

        private void Spawned(Player player)
        {
            if (player.IsAlive && player.IsScpRole())
            {
                List<RoleTypeId> scpsList =
                [
                    RoleTypeId.Scp3114,
                    RoleTypeId.Scp079,
                    RoleTypeId.Scp049,
                    RoleTypeId.Flamingo,
                    RoleTypeId.AlphaFlamingo,
                    RoleTypeId.ChaosFlamingo,
                    RoleTypeId.NtfFlamingo,
                    RoleTypeId.ZombieFlamingo
                ];

                if (scpsList.Contains(player.Role))
                {
                    player.Role.Set(Tools.EnumToList<RoleTypeId>()
                        .Where(x => !scpsList.Contains(x) && x.IsScpRole())
                        .ToList().GetRandomValue());
                }
            }
        }

        private void OnSearchingPickup(SearchingPickupEventArgs ev)
        {
            if (ev.Player == _juggernaut)
                ev.IsAllowed = false;
        }

        private void OnDroppingItem(DroppingItemEventArgs ev)
        {
            if (ev.Player == _juggernaut)
                ev.IsAllowed = false;
        }

        private void OnShooting(ShootingEventArgs ev)
        {
            if (ev.Player == _juggernaut)
                ev.Player.CurrentItem.As<Firearm>().MagazineAmmo = 101;
        }

        private IEnumerator<float> OnHurting(HurtingEventArgs ev)
        {
            if (ev.Attacker != null)
            {
                var isPlayerJuggernautTeam = IsJuggernautTeam(ev.Player);
                var isAttackerJuggernautTeam = IsJuggernautTeam(ev.Attacker);

                if (isPlayerJuggernautTeam == isAttackerJuggernautTeam)
                {
                    ev.IsAllowed = false;
                    yield break;
                }

                if (isPlayerJuggernautTeam || isAttackerJuggernautTeam)
                {
                    if (ev.Attacker == _juggernaut && ev.Player != _juggernaut)
                    {
                        if (ev.DamageHandler.CustomBase is FirearmDamageHandler
                            {
                                Hitbox: HitboxType.Headshot
                            } damageHandler)
                            damageHandler.Damage /= 2;

                        ev.DamageHandler.Damage *= 3.1f;
                    }
                    else if (ev.Player == _juggernaut)
                    {
                        if (!_playerDamages.ContainsKey(ev.Attacker))
                            _playerDamages.Add(ev.Attacker, 0);

                        _playerDamages[ev.Attacker] += ev.DamageHandler.Damage;
                        _stack += ev.DamageHandler.Damage;

                        if (_stack > 200)
                        {
                            Respawn.GrantInfluence(Faction.FoundationStaff, 20);
                            Respawn.GrantInfluence(Faction.FoundationEnemy, 20);

                            if (_stack > 2400)
                            {
                                CallRegularSupport();
                                _stack = 0;
                            }
                        }

                        foreach (var wave in WaveManager.Waves)
                        {
                            WaveTimer.TryGetWaveTimers(wave.TargetFaction, out List<WaveTimer> waves);

                            foreach (var w in waves)
                                w.SetTime((int)w.TimeLeft.TotalSeconds - 5);
                        }

                        List<RoleTypeId> listscp =
                        [
                            RoleTypeId.Scp173,
                            RoleTypeId.Scp106,
                            RoleTypeId.Scp096,
                            RoleTypeId.Scp939,
                            RoleTypeId.Scp0492
                        ];

                        if (ev.IsInstantKill || (listscp.Contains(ev.Attacker.Role.Type) &&
                                                 !_scpAttackCooldown.Contains(ev.Attacker)))
                        {
                            ev.IsAllowed = false;
                            ev.Player.Hurt(200f, DamageType.Scp);
                            ev.Attacker.ShowHitMarker(1.5f);

                            _scpAttackCooldown.Add(ev.Attacker);

                            yield return Timing.WaitForSeconds(1.1f);

                            _scpAttackCooldown.Remove(ev.Attacker);
                        }
                    }
                }
            }
        }

        private void OnReceivingEffect(ReceivingEffectEventArgs ev)
        {
            if (ev.Player == _juggernaut && ev.Effect.GetEffectType() != EffectType.SinkHole)
            {
                if (ev.Effect.GetEffectType() == EffectType.PocketCorroding) ev.IsAllowed = false;

                else if (ev.Effect.GetEffectType().GetCategories() == EffectCategory.Harmful ||
                         ev.Effect.GetEffectType().GetCategories() == EffectCategory.Negative)
                {
                    if (Warhead.IsDetonated) return;

                    Timing.CallDelayed(Timing.WaitForOneFrame,
                        () => { ev.Player.DisableEffect(ev.Effect.GetEffectType()); });
                }
            }
        }

        private void OnHandcuffing(HandcuffingEventArgs ev)
        {
            ev.IsAllowed = false;
        }

        private void OnChargingJailbird(Exiled.Events.EventArgs.Item.ChargingJailbirdEventArgs ev)
        {
            if (ev.Player == _juggernaut)
                ev.Item.As<Jailbird>().TotalCharges = 0;
        }
    }
}