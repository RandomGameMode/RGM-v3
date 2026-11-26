using System;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using MEC;
using Mirror;

using NetworkManagerUtils.Dummies;
using PlayerRoles;
using RemoteAdmin;
using RGM.API.Features;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RGM.Modes.Lock.TFT.Variables;
using RGM.Modes.Lock.TFT;
using UnityEngine;
using UserSettings.ServerSpecific;
using static RGM.Variables.Variable;
using Random = UnityEngine.Random;

namespace RGM.Modes;

[Mode(ModeCategory.Public, ModeInfo.Lock, ModeType.TFT)]
public class TFT : Mode
{
    public override string Name => "전략적 팀 전투";
    public override string Description => "전략적인 빌드를 구성하여 팀원을 승리로 이끄십시오.";

    public override string Detail =>
        """
        증강은 한 사람당 총 5개를 확보할 수 있으며,
        처음 라운드 시작시 40초 후에,
        그 다음 100초마다 지급됩니다.
        """;

    public override string Color => "ffd700";

    private CoroutineHandle _onModeStarted;
    private CoroutineHandle _upgradeTimerLoop;
    private readonly Dictionary<string, float> _upgradeRemainingTimes = new();
    private readonly HashSet<string> _playersWaitingRespawn = new();
    private readonly List<TFTAbilityLevel> _upgradeLevelSequence = new();
    private int _upgradeWaitTime = 100;
    private bool _isUpgradeTimerReady;
    private const bool DebugUpgradeTimer = false;

    public override void OnEnabled()
    {
        Exiled.Events.Handlers.Server.RoundEnded += OnRoundEnded;

        Exiled.Events.Handlers.Player.Verified += OnVerified;
        Exiled.Events.Handlers.Player.ChangingRole += OnChangingRole;
        Exiled.Events.Handlers.Player.Died += OnDied;
        Exiled.Events.Handlers.Player.Spawned += OnSpawned;

        _onModeStarted = Timing.RunCoroutine(OnModeStarted());
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Server.RoundEnded -= OnRoundEnded;

        Exiled.Events.Handlers.Player.Verified -= OnVerified;
        Exiled.Events.Handlers.Player.ChangingRole -= OnChangingRole;
        Exiled.Events.Handlers.Player.Died -= OnDied;
        Exiled.Events.Handlers.Player.Spawned -= OnSpawned;

        Timing.KillCoroutines(_onModeStarted);
        Timing.KillCoroutines(_upgradeTimerLoop);

        _upgradeRemainingTimes.Clear();
        _playersWaitingRespawn.Clear();
        _upgradeLevelSequence.Clear();
        _isUpgradeTimerReady = false;
    }

    private IEnumerator<float> OnModeStarted()
    {
        foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
        {
            var abilityAttribute = type.GetCustomAttribute<TFTAbilityAttribute>();

            if (abilityAttribute == null)
                continue;

            if (!typeof(TFTAbility).IsAssignableFrom(type))
                continue;

            Base.TFTAbilities.Add(abilityAttribute.Type, new TFTAbilityData
            {
                Type = type,
                Name = abilityAttribute.Name,
                Description = abilityAttribute.Description,
                Emoji = abilityAttribute.Emoji,
                Level = abilityAttribute.Level,
                Category = abilityAttribute.Category,
                Point = abilityAttribute.Point,
                TFTAbilityType = abilityAttribute.Type
            });
        }

        QueryProcessor.DotCommandHandler.RegisterCommand(
            new Lock.TFT.Commands.ClientCommands.basicfeatures.SelectTFTFirst());
        QueryProcessor.DotCommandHandler.RegisterCommand(
            new Lock.TFT.Commands.ClientCommands.basicfeatures.SelectTFTSecond());
        QueryProcessor.DotCommandHandler.RegisterCommand(
            new Lock.TFT.Commands.ClientCommands.basicfeatures.RerollTFTFirst());
        QueryProcessor.DotCommandHandler.RegisterCommand(
            new Lock.TFT.Commands.ClientCommands.basicfeatures.RerollTFTSecond());

        foreach (var player in PlayerManager.List)
            Lock.TFT.EventArgs.PlayerEvents.Verified(player);

        TFTSetting.Init();

        ServerSpecificSettingsSync.ServerOnSettingValueReceived += TFTSetting.OnSSInput;

        // --------------------------------------------------

        MultiBroadcast.API.MultiBroadcast.ClearAllBroadcasts();

        GlobalPlayer.TryPlay("게임 시작", 2);

        Round.IsLocked = true;

        Dictionary<Player, RoleTypeId> role = new();

        var encounter = Base.Encounters.GetRandomValue();
        Base.Encounter = encounter.Value.Item1;

        if (encounter.Value.Item1 != RoleTypeId.None)
        {
            var dummy = Player.Get(DummyUtils.SpawnDummy(encounter.Key));
            dummy.Role.Set(encounter.Value.Item1);
            dummy.Health = 99999;
            dummy.Scale = new Vector3(5, 5, 5);
            dummy.Position = new Vector3(139.8427f, 335.6814f, 67.04181f);

            Timing.CallDelayed(11, () => { NetworkServer.Destroy(dummy.GameObject); });
        }

        foreach (var player in PlayerManager.List)
        {
            role.Add(player, player.Role.Type);

            player.Role.Set(RoleTypeId.Tutorial);
            player.Position = new Vector3(137.8167f, 304.3213f, 71.88593f);
            player.AddEffect(EffectType.NightVision, 50);

            Timing.CallDelayed(1, () => { player.AddBroadcast(10, $"<size=25>{encounter.Value.Item2}</size>"); });
        }

        Timing.CallDelayed(11, () =>
        {
            foreach (var player in Player.List)
                player.Role.Set(role.TryGetValue(player, out var value) ? value : RoleTypeId.ClassD);

            try
            {
                switch (Base.Encounter)
                {
                    case RoleTypeId.ChaosRepressor:
                    {
                        foreach (var player in Player.List)
                            player.AddItem(Tools.EnumToList<ItemType>().Where(x => x.IsWeapon()).GetRandomValue());
                        break;
                    }
                    case RoleTypeId.ChaosMarauder:
                    {
                        foreach (var player in Player.List)
                            player.AddItem(Random.Range(1, 3) == 1 ? ItemType.GrenadeFlash : ItemType.GrenadeHE);
                        break;
                    }
                    case RoleTypeId.ChaosConscript:
                    {
                        foreach (var player in Player.List)
                            player.AddItem(Tools.EnumToList<ItemType>().Where(x => x.ToString().Contains("SCP"))
                                .GetRandomValue());
                        break;
                    }
                    case RoleTypeId.ChaosRifleman:
                    {
                        foreach (var player in Player.List)
                            player.AddItem(Tools.EnumToList<ItemType>().GetRandomValue());
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error($"Encounter Exception: {e}");
            }

            Round.IsLocked = false;
        });

        _upgradeWaitTime = GetTime();
        _isUpgradeTimerReady = true;
        _upgradeTimerLoop = Timing.RunCoroutine(UpgradeTimerLoop());
        DebugBroadcast($"타이머 루프 시작 / 간격: {_upgradeWaitTime}초 / 대상: {PlayerManager.List.Count()}명");

        foreach (var player in PlayerManager.List)
            StartUpgradeTimer(player);

        yield break;

        // --------------------------------------------------
        /*
         * 증강 지급 시간 로직 변경
         *
         * 새로 리스폰하는 유저들은 지급 시간을 초기화하도록 변경.
         * 즉, 유저마다 증강 지급 시간을 개별로 적용함.
         *
         * 기존 유저는 시작 40초 후 획득, 이후 100(또는 다른 시간)초마다 획득
         * 중간에 리스폰 한 유저는 리스폰 한 시점 부터 40초 후 획득, 이후 100(또는 다른 시간)초마다 획득
         */
        int GetTime()
        {
            return Base.Encounter switch
            {
                RoleTypeId.ClassD => 60, // 0.6
                RoleTypeId.Scientist => 45, // 0.4
                RoleTypeId.FacilityGuard => 80, // 0.8
                _ => 100
            };
        }
    }

    private void StartUpgradeTimer(Player player)
    {
        var key = GetPlayerKey(player);
        if (key == null)
        {
            DebugBroadcast($"타이머 시작 실패 / key null / player: {player?.Nickname ?? "null"}");
            return;
        }

        _playersWaitingRespawn.Remove(key);
        _upgradeRemainingTimes[key] = 40;
        DebugBroadcast($"타이머 시작 / {player.Nickname} / 40초", player);
    }

    private void StopUpgradeTimer(Player player)
    {
        var key = GetPlayerKey(player);
        if (key == null)
        {
            DebugBroadcast($"타이머 중지 실패 / key null / player: {player?.Nickname ?? "null"}");
            return;
        }

        _upgradeRemainingTimes.Remove(key);
        DebugBroadcast($"타이머 중지 / {player.Nickname}", player);
    }

    private IEnumerator<float> UpgradeTimerLoop()
    {
        while (_isUpgradeTimerReady && !Round.IsEnded)
        {
            yield return Timing.WaitForSeconds(1);
            DebugBroadcast($"루프 tick / 등록 타이머: {_upgradeRemainingTimes.Count}명");

            foreach (var key in _upgradeRemainingTimes.Keys.ToList())
                // 한 플레이어 처리 중 예외가 발생해도 루프(=전원 지급)가 멈추지 않도록 개별 보호
                try
                {
                    var player = GetPlayerByKey(key);

                    if (!CanReceiveUpgrade(player))
                    {
                        DebugBroadcast($"타이머 제거 / {key} / 사유: {GetCannotReceiveReason(player)}", player);
                        _upgradeRemainingTimes.Remove(key);
                        continue;
                    }

                    _upgradeRemainingTimes[key] -= 1;
                    var remaining = Mathf.CeilToInt(_upgradeRemainingTimes[key]);

                    if (remaining % 10 == 0 || remaining <= 5)
                        DebugBroadcast($"타이머 진행 / {player.Nickname} / 남은 시간: {remaining}초", player);

                    if (_upgradeRemainingTimes[key] > 0)
                        continue;

                    var abilityCount = TFTBattle.GetAbilities(player).Count();
                    var level = GetUpgradeLevel(abilityCount);
                    DebugBroadcast($"증강 지급 시도 / {player.Nickname} / 차수: {abilityCount + 1} / 등급: {level}", player);

                    var isUpgradeStarted = TFTBattle.StartUpgrade(new List<Player> { player }, level);

                    if (isUpgradeStarted)
                    {
                        _upgradeRemainingTimes[key] = _upgradeWaitTime;
                        DebugBroadcast($"증강 지급 완료 / {player.Nickname} / 다음 대기: {_upgradeWaitTime}초", player);
                    }
                    else
                    {
                        // 현재 등급에 지급 가능한 증강이 없을 때, 캐시된 등급을 다시 추첨해
                        // 같은 등급으로 영구히 재시도(= 지급 정지)하는 것을 방지
                        _upgradeLevelSequence[abilityCount] = TFTBattle.GetRandomAbilityLevel();
                        _upgradeRemainingTimes[key] = 5;
                        DebugBroadcast($"증강 지급 실패 / {player.Nickname} / 선택지 없음 / 등급 재추첨 후 5초 뒤 재시도", player);
                    }
                }
                catch (System.Exception e)
                {
                    _upgradeRemainingTimes[key] = 5;
                    DebugBroadcast($"타이머 처리 예외 / {key} / {e.GetType().Name}: {e.Message}");
                    Log.Error($"[TFT] UpgradeTimerLoop 처리 중 예외 / key: {key}\n{e}");
                }
        }
    }

    private TFTAbilityLevel GetUpgradeLevel(int upgradeIndex)
    {
        while (_upgradeLevelSequence.Count <= upgradeIndex)
            _upgradeLevelSequence.Add(TFTBattle.GetRandomAbilityLevel());

        return _upgradeLevelSequence[upgradeIndex];
    }

    private bool CanReceiveUpgrade(Player player)
    {
        return player != null &&
               player.IsAlive &&
               !player.IsNPC &&
               TFTBattle.GetAbilities(player).Count <
               (Base.Encounter == RoleTypeId.Scp0492 ? 8 : 5);
    }

    private string GetCannotReceiveReason(Player player)
    {
        if (player == null)
            return "player null";

        if (!player.IsAlive)
            return "not alive";

        if (player.IsNPC)
            return "npc";

        var maxAbilityCount = Base.Encounter == RoleTypeId.Scp0492 ? 8 : 5;

        return TFTBattle.GetAbilities(player).Count >= maxAbilityCount ? $"max abilities ({TFTBattle.GetAbilities(player).Count}/{maxAbilityCount})" : "unknown";
    }

    private string GetPlayerKey(Player player)
    {
        if (player == null || player.IsNPC || string.IsNullOrEmpty(player.UserId))
            return null;

        return player.UserId;
    }

    private Player GetPlayerByKey(string key)
    {
        return PlayerManager.List.FirstOrDefault(x => !x.IsNPC && x.UserId == key);
    }

    private void OnVerified(VerifiedEventArgs ev)
    {
        Lock.TFT.EventArgs.PlayerEvents.Verified(ev.Player);
        DebugBroadcast($"Verified / {ev.Player.Nickname} / ready: {_isUpgradeTimerReady} / alive: {ev.Player.IsAlive}",
            ev.Player);

        if (_isUpgradeTimerReady && ev.Player.IsAlive)
            StartUpgradeTimer(ev.Player);
    }

    private void OnChangingRole(ChangingRoleEventArgs ev)
    {
        DebugBroadcast(
            $"ChangingRole / {ev.Player.Nickname} / oldDead: {ev.Player.IsDead} / new: {ev.NewRole} / newDead: {ev.NewRole.IsDead()}",
            ev.Player);

        if (ev.Player.IsDead || ev.NewRole.IsDead() || TFTBattle.GetAbilities(ev.Player).Count == 0)
        {
            if (ev.NewRole.IsDead())
            {
                var key = GetPlayerKey(ev.Player);
                if (key != null)
                    _playersWaitingRespawn.Add(key);

                StopUpgradeTimer(ev.Player);
            }

            Timing.CallDelayed(Timing.WaitForOneFrame, () => { TFTBattle.Reset(ev.Player); });
        }
    }

    private void OnDied(DiedEventArgs ev)
    {
        var key = GetPlayerKey(ev.Player);
        if (key == null)
        {
            DebugBroadcast($"Died / key null / {ev.Player.Nickname}", ev.Player);
            return;
        }

        _playersWaitingRespawn.Add(key);
        DebugBroadcast($"Died / {ev.Player.Nickname} / respawn 대기 등록", ev.Player);
        StopUpgradeTimer(ev.Player);
    }

    private void OnSpawned(SpawnedEventArgs ev)
    {
        var key = GetPlayerKey(ev.Player);
        DebugBroadcast(
            $"Spawned / {ev.Player.Nickname} / key: {key ?? "null"} / ready: {_isUpgradeTimerReady} / locked: {Round.IsLocked} / waiting: {key != null && _playersWaitingRespawn.Contains(key)} / hasTimer: {key != null && _upgradeRemainingTimes.ContainsKey(key)}",
            ev.Player);

        // Round.IsLocked 로 게이팅하지 않는다.
        // (테스트 등으로 라운드가 계속 잠겨 있어도 리스폰 시 타이머가 재등록되어야 함)
        // 초기 라운드 시작(역할 세팅) 단계는 _isUpgradeTimerReady 가 false 이거나
        // 아래의 "이미 타이머가 있고 리스폰 대기 상태가 아님" 조건으로 걸러진다.
        if (key == null || !_isUpgradeTimerReady)
            return;

        if (!_playersWaitingRespawn.Remove(key) && _upgradeRemainingTimes.ContainsKey(key))
            return;

        Timing.CallDelayed(Timing.WaitForOneFrame, () => { StartUpgradeTimer(ev.Player); });
    }

    private void DebugBroadcast(string message, Player player = null)
    {
        if (!DebugUpgradeTimer) return;

        /*try
        {
            string text = $"<size=18><color=#00ffff>[TFT DEBUG]</color> {message}</size>";

            if (player != null)
            {
                player.AddBroadcast(3, text);
                return;
            }

            foreach (var target in PlayerManager.List.Where(x => !x.IsNPC))
                target.AddBroadcast(3, text);
        }
        catch { }*/
    }

    private void OnRoundEnded(RoundEndedEventArgs ev)
    {
        // 라운드 종료 시 증강 지급 타이머 및 모든 TFT 힌트 표시(증강 리스트/선택창)를 중단하여
        // SetWinner 결과 화면이 가려지지 않도록 한다. (각 힌트 코루틴은 Round.IsEnded 를 감지해 정리됨)
        _isUpgradeTimerReady = false;
        Timing.KillCoroutines(_upgradeTimerLoop);
        _upgradeRemainingTimes.Clear();
        TFTBattle.ClearAllHints();

        List<Player> players = [.. PlayerManager.List.Where(x => x.IsAlive && !x.IsNPC)];

        switch (players.Count)
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
