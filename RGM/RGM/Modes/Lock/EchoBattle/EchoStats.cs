using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Roles;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Scp049;
using Exiled.Events.EventArgs.Scp096;
using Exiled.Events.EventArgs.Scp173;
using InventorySystem.Items.Firearms.Modules;
using MEC;
using PlayerRoles;
using PlayerRoles.PlayableScps.HumeShield;
using PlayerStatsSystem;
using RGM.API.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RGM.Modes;

/// <summary>
/// Echo Main/Sub 스탯 및 부가 옵션 수치 계산/적용.
/// 레벨 1~25 선형 증가. 부가 옵션은 레벨 5의 배수마다 1개 해금.
/// </summary>
public static class EchoStats
{
    private static readonly RoleTypeId[] AttackFlagIgnoredRoles =
    [
        RoleTypeId.Scp173,
        RoleTypeId.Scp079
    ];

    public static bool AreAttackModifiersIgnored(Player player)
    {
        return player != null && AttackFlagIgnoredRoles.Contains(player.Role.Type);
    }

    private static readonly DamageType[] DefenseFlatIgnoredDamageTypes =
    [
        DamageType.Warhead,
        DamageType.Crushed,
        DamageType.PocketDimension,
        DamageType.Falldown,
        DamageType.Scp106
    ];

    private static readonly float[] SubOptionGradeWeights = [11, 19, 25, 21, 14, 10];
    private static readonly System.Random SubOptionRandom = new();
    private static readonly object SubOptionRandomLock = new();
    private static readonly HashSet<Player> FixedDamageTargets = new();
    private static readonly Dictionary<Player, PendingScp173HsFix> PendingScp173HsFixes = new();
    private const float Scp173BaseBlinkCooldown = 3f;
    private const float Scp173MinimumBlinkCooldown = 1f;
    private const float Scp173MaximumMoveSpeed = 100f;
    private const float Scp173BlinkCooldownReductionPerMoveSpeed = 0.014f;

    /// <summary>바닐라 SCP-049 Good Sense 대상 소생 HS 보상. <c>Scp049ResurrectAbility.ResurrectTargetReward</c></summary>
    private const float Scp049SenseResurrectHsReward = 200f;
    /// <summary>바닐라 SCP-173 Tantrum 처치 HS 보상.</summary>
    private const float Scp173TantrumHsReward = 400f;

    private const float Scp173TantrumFlamingoHsReward = 100f;
    private const float HsClampMatchTolerance = 1f;

    private readonly struct PendingScp173HsFix(float savedHs, float providerMax, float reward)
    {
        public readonly float SavedHs = savedHs;
        public readonly float ProviderMax = providerMax;
        public readonly float Reward = reward;
    }

    private static readonly Dictionary<EchoSubOptionType, float[]> SubOptionValues = new()
    {
        { EchoSubOptionType.AttackPercent, [7.1f, 7.8f, 8.5f, 9.2f, 9.9f, 10.6f] },
        { EchoSubOptionType.AttackFlat, [6f, 8f, 10f, 12f, 14f, 16f] },
        { EchoSubOptionType.DefensePercent, [10.0f, 11.0f, 12.0f, 13.0f, 14.0f, 15.0f] },
        { EchoSubOptionType.DefenseFlat, [10.0f, 10.8f, 11.6f, 12.4f, 13.2f, 14.0f] },
        { EchoSubOptionType.HpPercent, [10.5f, 11.6f, 12.7f, 13.8f, 14.9f, 16.0f] },
        { EchoSubOptionType.HpFlat, [90f, 102f, 114f, 126f, 138f, 150f] },
        { EchoSubOptionType.CriticalChance, [7.5f, 8.1f, 8.7f, 9.3f, 9.9f, 10.5f] },
        { EchoSubOptionType.ScpDamagePercent, [8.3f, 9.6f, 10.9f, 12.2f, 13.5f, 14.8f] },
        { EchoSubOptionType.HumanDamagePercent, [8.3f, 9.6f, 10.9f, 12.2f, 13.5f, 14.8f] },
        { EchoSubOptionType.MoveSpeed, [9.0f, 10.0f, 11.0f, 12.0f, 13.0f, 14.0f] },
        { EchoSubOptionType.JumpPower, [5.0f, 6.0f, 7.0f, 8.0f, 9.0f, 10.0f] },
        { EchoSubOptionType.StaminaDrainReduction, [10.7f, 11.5f, 12.3f, 13.1f, 13.9f, 14.7f] },
        { EchoSubOptionType.HeadshotDamage, [23.8f, 26.1f, 28.4f, 30.7f, 33.0f, 35.3f] },
        { EchoSubOptionType.SizeReduction, [4.8f, 5.5f, 6.2f, 6.9f, 7.6f, 8.3f] },
        { EchoSubOptionType.HealingBonus, [55.0f, 64.0f, 73.0f, 82.0f, 91.0f, 100.0f] },
        { EchoSubOptionType.CriticalDamage, [15.0f, 16.2f, 17.4f, 18.6f, 19.8f, 21.0f] }
    };

    public static float LerpStat(float min, float max, int level)
    {
        level = Clamp(level, 1, EchoInfo.MaxLevel);
        float t = (level - 1) / (float)(EchoInfo.MaxLevel - 1);
        return min + (max - min) * t;
    }

    public static int Clamp(int value, int min, int max)
    {
        if (value < min) return min;
        return value > max ? max : value;
    }

    public static float GetMainStatValue(EchoCost cost, EchoMainStatType type, int level)
    {
        return (cost, type) switch
        {
            (EchoCost.Cost4, EchoMainStatType.AttackPercent) => LerpStat(6.6f, 33.0f, level),
            (EchoCost.Cost4, EchoMainStatType.HpPercent) => LerpStat(13.6f, 68.0f, level),
            (EchoCost.Cost4, EchoMainStatType.Defense) => LerpStat(5.0f, 25.0f, level),
            (EchoCost.Cost4, EchoMainStatType.CriticalChance) => LerpStat(4.4f, 22.0f, level),
            (EchoCost.Cost4, EchoMainStatType.MoveSpeedAndJump) => LerpStat(4.0f, 20.0f, level),
            (EchoCost.Cost4, EchoMainStatType.StaminaDrainReduction) => LerpStat(12.0f, 60.0f, level),
            (EchoCost.Cost4, EchoMainStatType.CriticalDamage) => LerpStat(8.8f, 44.0f, level),

            (EchoCost.Cost3, EchoMainStatType.AttackPercent) => LerpStat(5.6f, 28.0f, level),
            (EchoCost.Cost3, EchoMainStatType.HpPercent) => LerpStat(8.6f, 43.0f, level),
            (EchoCost.Cost3, EchoMainStatType.Defense) => LerpStat(3.0f, 15.0f, level),
            (EchoCost.Cost3, EchoMainStatType.ScpDamagePercent) => LerpStat(8.3f, 41.5f, level),
            (EchoCost.Cost3, EchoMainStatType.HumanDamagePercent) => LerpStat(8.3f, 41.5f, level),
            (EchoCost.Cost3, EchoMainStatType.HeadshotDamage) => LerpStat(21.0f, 105.0f, level),
            (EchoCost.Cost3, EchoMainStatType.AhpRegenAndMax) => LerpStat(4.0f, 20.0f, level),
            (EchoCost.Cost3, EchoMainStatType.SizeReduction) => LerpStat(3.3f, 16.5f, level),

            (EchoCost.Cost1, EchoMainStatType.AttackPercent) => LerpStat(3.4f, 18.0f, level),
            (EchoCost.Cost1, EchoMainStatType.HpPercent) => LerpStat(6.0f, 30.0f, level),
            (EchoCost.Cost1, EchoMainStatType.Defense) => LerpStat(2.0f, 10.0f, level),

            _ => 0f
        };
    }

    /// <summary>Cost별로 선택 가능한 메인 스탯 목록.</summary>
    public static IReadOnlyList<EchoMainStatType> GetAvailableMainStats(EchoCost cost)
    {
        return cost switch
        {
            EchoCost.Cost4 => Cost4MainStats,
            EchoCost.Cost3 => Cost3MainStats,
            EchoCost.Cost1 => Cost1MainStats,
            _ => []
        };
    }

    private static readonly EchoMainStatType[] Cost4MainStats =
    [
        EchoMainStatType.AttackPercent,
        EchoMainStatType.HpPercent,
        EchoMainStatType.Defense,
        EchoMainStatType.CriticalChance,
        EchoMainStatType.CriticalDamage,
        EchoMainStatType.MoveSpeedAndJump,
        EchoMainStatType.StaminaDrainReduction
    ];

    private static readonly EchoMainStatType[] Cost3MainStats =
    [
        EchoMainStatType.AttackPercent,
        EchoMainStatType.HpPercent,
        EchoMainStatType.Defense,
        EchoMainStatType.ScpDamagePercent,
        EchoMainStatType.HumanDamagePercent,
        EchoMainStatType.HeadshotDamage,
        EchoMainStatType.AhpRegenAndMax,
        EchoMainStatType.SizeReduction
    ];

    private static readonly EchoMainStatType[] Cost1MainStats =
    [
        EchoMainStatType.AttackPercent,
        EchoMainStatType.HpPercent,
        EchoMainStatType.Defense
    ];

    public static bool IsMainStatAvailable(EchoCost cost, EchoMainStatType type)
    {
        return type != EchoMainStatType.None && GetAvailableMainStats(cost).Any(available => available == type);
    }

    /// <summary>Server-Specific 드롭다운용: 모든 Cost에서 등장하는 메인 스탯 합집합.</summary>
    public static IReadOnlyList<EchoMainStatType> GetAllSelectableMainStats()
    {
        return AllSelectableMainStats;
    }

    private static readonly EchoMainStatType[] AllSelectableMainStats =
    [
        EchoMainStatType.AttackPercent,
        EchoMainStatType.HpPercent,
        EchoMainStatType.Defense,
        EchoMainStatType.ScpDamagePercent,
        EchoMainStatType.HumanDamagePercent,
        EchoMainStatType.CriticalChance,
        EchoMainStatType.CriticalDamage,
        EchoMainStatType.MoveSpeedAndJump,
        EchoMainStatType.StaminaDrainReduction,
        EchoMainStatType.HeadshotDamage,
        EchoMainStatType.AhpRegenAndMax,
        EchoMainStatType.SizeReduction
    ];

    public static float GetSubStatValue(EchoCost cost, int level)
    {
        return cost switch
        {
            EchoCost.Cost4 => LerpStat(3f, 75f, level),
            EchoCost.Cost3 => LerpStat(50f, 250f, level),
            EchoCost.Cost1 => LerpStat(46f, 228f, level),
            _ => 0f
        };
    }

    public static EchoSubStatType GetSubStatType(EchoCost cost)
    {
        return cost switch
        {
            EchoCost.Cost1 => EchoSubStatType.HpFlat,
            EchoCost.Cost3 => EchoSubStatType.HealingBonus,
            EchoCost.Cost4 => EchoSubStatType.AttackFlat,
            _ => EchoSubStatType.None
        };
    }

    public static int GetUnlockedSubOptionCount(int level)
    {
        return EchoStats.Clamp(level / 5, 0, 5);
    }

    private static List<EchoSubOptionType> GetAllSubOptionTypes()
    {
        return Enum.GetValues(typeof(EchoSubOptionType)).Cast<EchoSubOptionType>()
            .Where(x => x != EchoSubOptionType.None)
            .ToList();
    }

    /// <summary>
    /// 로드아웃에 저장된 부가 옵션을 유지한 채, 해금 개수만큼만 새로 굴립니다.
    /// 단일 Echo 안에서는 같은 종류의 부가 옵션이 중복되지 않습니다.
    /// </summary>
    public static List<EchoSubOptionInstance> EnsureSubOptions(EchoLoadout loadout, EchoType type, int level)
    {
        if (!loadout.SubOptions.TryGetValue(type, out var existing) || existing == null)
        {
            existing = new List<EchoSubOptionInstance>();
            loadout.SubOptions[type] = existing;
        }

        // 이미 저장된 중복 타입은 첫 번째만 유지
        DeduplicateSubOptions(existing);

        int targetCount = GetUnlockedSubOptionCount(level);

        while (existing.Count > targetCount)
            existing.RemoveAt(existing.Count - 1);

        var used = new HashSet<EchoSubOptionType>(existing.Select(x => x.Type));
        var pool = GetAllSubOptionTypes().Where(x => !used.Contains(x)).ToList();

        while (existing.Count < targetCount && pool.Count > 0)
        {
            EchoSubOptionInstance option;
            lock (SubOptionRandomLock)
            {
                int pick = SubOptionRandom.Next(pool.Count);
                var optionType = pool[pick];
                pool.RemoveAt(pick);

                int grade = RollGrade(SubOptionRandom);
                option = new EchoSubOptionInstance
                {
                    Type = optionType,
                    Grade = grade,
                    Value = SubOptionValues[optionType][grade - 1]
                };
            }

            existing.Add(option);
        }

        return existing.Select(CloneSubOption).ToList();
    }

    /// <summary>같은 Echo 내 중복 타입은 먼저 해금된(앞쪽) 옵션만 남깁니다.</summary>
    private static void DeduplicateSubOptions(List<EchoSubOptionInstance> options)
    {
        var seen = new HashSet<EchoSubOptionType>();
        for (int i = 0; i < options.Count;)
        {
            if (!seen.Add(options[i].Type))
                options.RemoveAt(i);
            else
                i++;
        }
    }

    public static EchoSubOptionInstance CloneSubOption(EchoSubOptionInstance option)
    {
        return new EchoSubOptionInstance
        {
            Type = option.Type,
            Grade = option.Grade,
            Value = option.Value
        };
    }

    public static List<EchoSubOptionInstance> GenerateSubOptions(int level, int? seed = null)
    {
        var result = new List<EchoSubOptionInstance>();
        int count = GetUnlockedSubOptionCount(level);
        var rng = seed.HasValue ? new System.Random(seed.Value) : null;
        var pool = GetAllSubOptionTypes();

        for (int i = 0; i < count && pool.Count > 0; i++)
        {
            EchoSubOptionInstance option;
            lock (SubOptionRandomLock)
            {
                var activeRng = rng ?? SubOptionRandom;
                int pick = activeRng.Next(pool.Count);
                var optionType = pool[pick];
                pool.RemoveAt(pick);

                int grade = RollGrade(activeRng);
                option = new EchoSubOptionInstance
                {
                    Type = optionType,
                    Grade = grade,
                    Value = SubOptionValues[optionType][grade - 1]
                };
            }

            result.Add(option);
        }

        return result;
    }

    private static int RollGrade(System.Random rng)
    {
        float total = SubOptionGradeWeights.Sum();
        float roll = (float)(rng.NextDouble() * total);
        float cumulative = 0f;

        for (int i = 0; i < SubOptionGradeWeights.Length; i++)
        {
            cumulative += SubOptionGradeWeights[i];
            if (roll <= cumulative)
                return i + 1;
        }

        return 1;
    }

    public static EchoStatSnapshot BuildSnapshot(Player player, List<Echo> echoes)
    {
        var snapshot = new EchoStatSnapshot();

        foreach (var echo in echoes)
        {
            if (echo?.Data == null)
                continue;

            int level = echo.Level;
            var cost = echo.Data.Cost;

            // Cost에 등록된 메인 스탯만 적용. 미등록/잔존 값은 무시.
            var mainType = echo.SelectedMainStat;
            if (!IsMainStatAvailable(cost, mainType))
                mainType = IsMainStatAvailable(cost, echo.Data.MainStatType)
                    ? echo.Data.MainStatType
                    : EchoMainStatType.None;

            if (mainType != EchoMainStatType.None)
            {
                float mainValue = GetMainStatValue(cost, mainType, level);
                if (mainValue > 0f || mainType == EchoMainStatType.AhpRegenAndMax)
                    ApplyMainStat(snapshot, mainType, mainValue, cost, level);
            }

            ApplySubStat(snapshot, cost, level, player);

            foreach (var option in echo.SubOptions)
                ApplySubOption(snapshot, option, player);
        }

        return snapshot;
    }

    private static void ApplyMainStat(EchoStatSnapshot snapshot, EchoMainStatType type, float value, EchoCost cost, int level)
    {
        // 이중 방어: Cost에 없는 타입은 절대 반영하지 않음
        if (!IsMainStatAvailable(cost, type))
            return;

        switch (type)
        {
            case EchoMainStatType.AttackPercent:
                snapshot.AttackPercent += value;
                break;
            case EchoMainStatType.HpPercent:
                snapshot.HpPercent += value;
                break;
            case EchoMainStatType.Defense:
                snapshot.DefensePercent += value;
                break;
            case EchoMainStatType.ScpDamagePercent:
                snapshot.ScpDamagePercent += value;
                break;
            case EchoMainStatType.HumanDamagePercent:
                snapshot.HumanDamagePercent += value;
                break;
            case EchoMainStatType.CriticalChance:
                snapshot.CriticalChance += value;
                break;
            case EchoMainStatType.CriticalDamage:
                snapshot.CriticalDamage += value;
                break;
            case EchoMainStatType.MoveSpeedAndJump:
                snapshot.MoveSpeed += value;
                snapshot.JumpPower += Mathf.Floor(value * 0.5f);
                break;
            case EchoMainStatType.StaminaDrainReduction:
                snapshot.StaminaDrainReduction += value;
                break;
            case EchoMainStatType.HeadshotDamage:
                snapshot.HeadshotDamage += value;
                break;
            case EchoMainStatType.AhpRegenAndMax:
                // Cost3 전용. regen value + max 테이블
                snapshot.AhpRegen += value;
                snapshot.AhpMax += LerpStat(35f, 350f, level);
                snapshot.HsRegen += LerpStat(15f, 75f, level);
                snapshot.HsMax += LerpStat(360f, 1800f, level);
                break;
            case EchoMainStatType.SizeReduction:
                snapshot.SizeReduction += value;
                break;
        }
    }

    private static void ApplySubStat(EchoStatSnapshot snapshot, EchoCost cost, int level, Player player)
    {
        float value = GetSubStatValue(cost, level);

        if (cost == EchoCost.Cost1)
        {
            float hp = value;
            if (player.IsScp)
                hp *= 8f;
            snapshot.HpFlat += hp;
        }
        else if (cost == EchoCost.Cost3)
        {
            snapshot.HealingBonus += value;
        }
        else
        {
            if (AttackFlagIgnoredRoles.Contains(player.Role.Type))
                return;

            float attack = value;
            if (player.Role.Type is RoleTypeId.Scp049 or RoleTypeId.Scp106)
                attack *= 0.45f;
            snapshot.AttackFlat += attack;
        }
    }

    private static void ApplySubOption(EchoStatSnapshot snapshot, EchoSubOptionInstance option, Player player)
    {
        switch (option.Type)
        {
            case EchoSubOptionType.AttackPercent:
                snapshot.AttackPercent += option.Value;
                break;
            case EchoSubOptionType.AttackFlat:
                if (AttackFlagIgnoredRoles.Contains(player.Role.Type))
                    break;
                float attack = option.Value;
                if (player.Role.Type is RoleTypeId.Scp049 or RoleTypeId.Scp106)
                    attack *= 0.45f;
                snapshot.AttackFlat += attack;
                break;
            case EchoSubOptionType.DefensePercent:
                snapshot.DefensePercent += option.Value;
                break;
            case EchoSubOptionType.DefenseFlat:
                snapshot.DefenseFlat += option.Value;
                break;
            case EchoSubOptionType.HpPercent:
                snapshot.HpPercent += option.Value;
                break;
            case EchoSubOptionType.HpFlat:
                snapshot.HpFlat += player.IsScp ? option.Value * 8f : option.Value;
                break;
            case EchoSubOptionType.CriticalChance:
                snapshot.CriticalChance += option.Value;
                break;
            case EchoSubOptionType.CriticalDamage:
                snapshot.CriticalDamage += option.Value;
                break;
            case EchoSubOptionType.ScpDamagePercent:
                snapshot.ScpDamagePercent += option.Value;
                break;
            case EchoSubOptionType.HumanDamagePercent:
                snapshot.HumanDamagePercent += option.Value;
                break;
            case EchoSubOptionType.MoveSpeed:
                snapshot.MoveSpeed += option.Value;
                break;
            case EchoSubOptionType.JumpPower:
                snapshot.JumpPower += option.Value;
                break;
            case EchoSubOptionType.StaminaDrainReduction:
                snapshot.StaminaDrainReduction += option.Value;
                break;
            case EchoSubOptionType.HeadshotDamage:
                snapshot.HeadshotDamage += option.Value;
                break;
            case EchoSubOptionType.SizeReduction:
                snapshot.SizeReduction += option.Value;
                break;
            case EchoSubOptionType.HealingBonus:
                snapshot.HealingBonus += player.Role.Type == RoleTypeId.Scp0492 ? option.Value * 4f : option.Value;
                break;
        }
    }

    /// <summary>
    /// 바닐라 SCP 스킬이 <see cref="HumeShieldModuleBase.HsMax"/>로 현재 HS를 클램프하는 것을
    /// Echo <c>AhpRegenAndMax</c> 최대치 기준으로 보정합니다.
    /// </summary>
    public static void RegisterHsSkillGuards()
    {
        Exiled.Events.Handlers.Scp049.FinishingRecall += OnScp049FinishingRecallHsGuard;
        Exiled.Events.Handlers.Scp096.Enraging += OnScp096EnragingHsGuard;
        Exiled.Events.Handlers.Scp096.CalmingDown += OnScp096CalmingDownHsGuard;
        Exiled.Events.Handlers.Player.Dying += OnDyingScp173HsGuard;
        Exiled.Events.Handlers.Player.Died += OnDiedScp173HsGuard;
    }

    public static void UnregisterHsSkillGuards()
    {
        Exiled.Events.Handlers.Scp049.FinishingRecall -= OnScp049FinishingRecallHsGuard;
        Exiled.Events.Handlers.Scp096.Enraging -= OnScp096EnragingHsGuard;
        Exiled.Events.Handlers.Scp096.CalmingDown -= OnScp096CalmingDownHsGuard;
        Exiled.Events.Handlers.Player.Dying -= OnDyingScp173HsGuard;
        Exiled.Events.Handlers.Player.Died -= OnDiedScp173HsGuard;
        PendingScp173HsFixes.Clear();
    }

    public static void ClearPassiveEffects(Player player)
    {
        if (player == null)
            return;

        Timing.KillCoroutines($"EchoRegen_{player.UserId}");
        Timing.KillCoroutines($"EchoStamina_{player.UserId}");
        PendingScp173HsFixes.Remove(player);

        if (!EchoInfo.PlayerPassiveEffects.TryGetValue(player, out var prev))
            return;

        if (prev.DefenseReduction > 0)
            player.RemoveEffect(EffectType.DamageReduction, prev.DefenseReduction);
        if (prev.MovementBoost > 0)
            player.RemoveEffect(EffectType.MovementBoost, prev.MovementBoost);
        if (prev.Lightweight > 0)
            player.RemoveEffect(EffectType.Lightweight, prev.Lightweight);
        if (prev.StaminaDrainToggled)
            player.IsUsingStamina = true;
        if (prev.SizeReduction > 0f)
            player.Scale += Vector3.one * prev.SizeReduction;
        if (prev.Scp173BlinkCooldownModified)
            Exiled.Events.Handlers.Scp173.Blinking -= OnScp173Blinking;

        if (prev.EchoAhpKillCode.HasValue)
            KillEchoAhpProcess(player, prev.EchoAhpKillCode.Value);

        EchoInfo.PlayerPassiveEffects.Remove(player);
    }

    private static void KillEchoAhpProcess(Player player, int killCode)
    {
        if (player?.ReferenceHub?.playerStats == null)
            return;

        var ahpStat = player.ReferenceHub.playerStats.GetModule<AhpStat>();
        ahpStat?.ServerKillProcess(killCode);
    }

    private static bool TryGetEchoAhpProcess(Player player, int? killCode, out AhpProcess process)
    {
        process = null;
        if (!killCode.HasValue || player?.ReferenceHub?.playerStats == null)
            return false;

        var ahpStat = player.ReferenceHub.playerStats.GetModule<AhpStat>();
        return ahpStat != null && ahpStat.ServerTryGetProcess(killCode.Value, out process);
    }

    /// <summary>
    /// ClearPassiveEffects / RemoveAllEchoes 호출 전에 스탯 AHP 현재량을 읽습니다.
    /// </summary>
    public static bool TryPeekEchoAhpAmount(Player player, out float amount)
    {
        amount = 0f;
        if (player == null
            || !EchoInfo.PlayerPassiveEffects.TryGetValue(player, out var state)
            || !TryGetEchoAhpProcess(player, state.EchoAhpKillCode, out var process))
            return false;

        amount = process.CurrentAmount;
        return true;
    }

    public static void ApplyPassiveEffects(Player player, EchoStatSnapshot snapshot, float? preservedEchoAhp = null)
    {
        // 호출 측에서 넘기지 않은 경우(직접 재적용)에만 여기서 보존 시도
        bool hadEchoAhp = preservedEchoAhp.HasValue;
        float echoAhpAmount = preservedEchoAhp ?? 0f;
        if (!hadEchoAhp && TryPeekEchoAhpAmount(player, out float peeked))
        {
            hadEchoAhp = true;
            echoAhpAmount = peeked;
        }

        // Clear/HP 재계산 전에 현재 HS를 보존 (바닐라 모듈 HsMax 클램프와 별개로 재적용 손실 방지)
        float? preservedHs = player.IsScp ? player.HumeShield : null;

        ClearPassiveEffects(player);

        // HP: 역할 기본 MaxHealth 기준으로만 계산 (레벨업 재적용 시 복리 방지)
        if (!EchoInfo.PlayerBaseMaxHealth.TryGetValue(player, out float baseMax) || baseMax <= 0f)
        {
            baseMax = player.MaxHealth;
            EchoInfo.PlayerBaseMaxHealth[player] = baseMax;
        }

        float newMax = (baseMax + snapshot.HpFlat) * (1f + snapshot.HpPercent / 100f);
        float ratio = player.Health / Math.Max(1f, player.MaxHealth);
        player.MaxHealth = newMax;
        player.Health = Mathf.Clamp(newMax * ratio, 1f, newMax);

        var effectState = new EchoPassiveEffectState();

        // 이동속도 / 점프력: 스냅샷 합산값을 이펙트로 1회 적용
        if (snapshot.MoveSpeed > 0)
        {
            effectState.MovementBoost = (byte)Mathf.Clamp(Mathf.RoundToInt(snapshot.MoveSpeed), 1, 255);
            player.AddEffect(EffectType.MovementBoost, effectState.MovementBoost);
        }

        if (player.Role is Scp173Role && snapshot.MoveSpeed > 0f)
        {
            // BlinkCooldown은 현재 진행 중인 대기시간입니다. 여기서 변경하면 레벨업
            // 재적용 시 대기시간이 초기화되므로, Blinking 이벤트에서 다음 대기시간만 변경합니다.
            effectState.Scp173BlinkCooldownModified = true;
            Exiled.Events.Handlers.Scp173.Blinking += OnScp173Blinking;
        }

        if (snapshot.JumpPower > 0)
        {
            effectState.Lightweight = (byte)Mathf.Clamp(Mathf.RoundToInt(snapshot.JumpPower), 1, 255);
            player.AddEffect(EffectType.Lightweight, effectState.Lightweight);
        }

        if (snapshot.StaminaDrainReduction > 0)
        {
            effectState.StaminaDrainToggled = true;
            Timing.RunCoroutine(StaminaDrainReductionRoutine(player, snapshot.StaminaDrainReduction), $"EchoStamina_{player.UserId}");
        }

        if (snapshot.SizeReduction > 0f)
        {
            effectState.SizeReduction = snapshot.SizeReduction / 100f;
            player.Scale -= Vector3.one * effectState.SizeReduction;
        }

        EchoInfo.PlayerPassiveEffects[player] = effectState;

        // AHP / HS: 역할 기본값 + 스냅샷 증가량 (재적용 시 Max/현재값 복리 방지)
        if (player.IsScp)
        {
            // Role/SCP별 기본 HS를 최초 1회 저장한 뒤, 증가량만 더함
            if (!EchoInfo.PlayerBaseMaxHs.TryGetValue(player, out float baseHs))
            {
                baseHs = player.MaxHumeShield;
                EchoInfo.PlayerBaseMaxHs[player] = baseHs;
            }

            float targetHsMax = baseHs + snapshot.HsMax;
            player.MaxHumeShield = targetHsMax;
            float currentHs = preservedHs ?? player.HumeShield;
            player.HumeShield = Math.Min(currentHs, player.MaxHumeShield);
        }
        else if (snapshot.AhpMax > 0)
        {
            // 스탯 AHP만 decay=0 별도 프로세스로 지급. 아드레날린 등 다른 AHP는 그대로 decay.
            float amount = hadEchoAhp
                ? Mathf.Min(echoAhpAmount, snapshot.AhpMax)
                : snapshot.AhpMax;

            var ahpStat = player.ReferenceHub.playerStats.GetModule<AhpStat>();
            var process = ahpStat.ServerAddProcess(amount, snapshot.AhpMax, 0f, 0.7f, 0f, true);
            effectState.EchoAhpKillCode = process.KillCode;
        }

        if (snapshot.AhpRegen > 0 || snapshot.HsRegen > 0)
            Timing.RunCoroutine(RegenRoutine(player, snapshot), $"EchoRegen_{player.UserId}");
    }

    private static void OnScp173Blinking(BlinkingEventArgs ev)
    {
        if (ev?.Player == null
            || !EchoInfo.PlayerStats.TryGetValue(ev.Player, out var snapshot)
            || snapshot.MoveSpeed <= 0f)
            return;

        ev.BlinkCooldown = GetScp173BlinkCooldown(snapshot.MoveSpeed);
    }

    private static bool HasEchoHsBonus(Player player)
    {
        return player != null
               && EchoInfo.PlayerStats.TryGetValue(player, out var stats)
               && stats.HsMax > 0f;
    }

    private static bool TryGetProviderHsMax(Player player, out float providerMax)
    {
        providerMax = 0f;
        if (player?.ReferenceHub?.roleManager?.CurrentRole is not IHumeShieldedRole { HumeShieldModule: { } module })
            return false;

        providerMax = module.HsMax;
        return true;
    }

    /// <summary>
    /// 바닐라가 provider HsMax로 클램프한 HS 보상을 Echo MaxHumeShield 기준으로 다시 적용합니다.
    /// </summary>
    private static void RestoreHsRewardAgainstEchoMax(Player player, float savedHs, float providerMax, float reward)
    {
        if (player == null || !player.IsAlive || !HasEchoHsBonus(player))
            return;

        float expectedVanilla = Mathf.Min(savedHs + reward, providerMax);
        if (Mathf.Abs(player.HumeShield - expectedVanilla) > HsClampMatchTolerance)
            return;

        player.HumeShield = Mathf.Min(savedHs + reward, player.MaxHumeShield);
    }

    /// <summary>
    /// SCP-096 분노 전환 시 바닐라가 provider HsMax로 현재 HS를 잘라내는 것을 복구합니다.
    /// </summary>
    private static void RestoreHsAfterProviderClamp(Player player, float savedHs, float providerMax)
    {
        if (player == null || !player.IsAlive || !HasEchoHsBonus(player))
            return;

        float expectedVanilla = Mathf.Min(savedHs, providerMax);
        if (Mathf.Abs(player.HumeShield - expectedVanilla) > HsClampMatchTolerance)
            return;

        player.HumeShield = Mathf.Min(savedHs, player.MaxHumeShield);
    }

    private static void ScheduleHsRestore(Player player, float savedHs, float providerMax, float reward)
    {
        Timing.CallDelayed(Timing.WaitForOneFrame, () =>
            RestoreHsRewardAgainstEchoMax(player, savedHs, providerMax, reward));
    }

    private static void ScheduleHsClampRestore(Player player, float savedHs, float providerMax)
    {
        Timing.CallDelayed(Timing.WaitForOneFrame, () =>
            RestoreHsAfterProviderClamp(player, savedHs, providerMax));
    }

    private static void OnScp049FinishingRecallHsGuard(FinishingRecallEventArgs ev)
    {
        // Good Sense 대상 소생 시: HsCurrent = Min(current + 200, HumeShieldModule.HsMax)
        // Echo 최대치가 모듈 기본값보다 크면 현재 HS가 역할 초기 최대치로 잘린다.
        if (ev?.Player == null || !ev.IsAllowed || !HasEchoHsBonus(ev.Player))
            return;
        if (!TryGetProviderHsMax(ev.Player, out float providerMax))
            return;

        ScheduleHsRestore(ev.Player, ev.Player.HumeShield, providerMax, Scp049SenseResurrectHsReward);
    }

    private static void OnScp096EnragingHsGuard(EnragingEventArgs ev)
    {
        if (ev?.Player == null || !ev.IsAllowed || !HasEchoHsBonus(ev.Player))
            return;
        if (!TryGetProviderHsMax(ev.Player, out float providerMax))
            return;

        ScheduleHsClampRestore(ev.Player, ev.Player.HumeShield, providerMax);
    }

    private static void OnScp096CalmingDownHsGuard(CalmingDownEventArgs ev)
    {
        if (ev?.Player == null || !ev.IsAllowed || !HasEchoHsBonus(ev.Player))
            return;
        if (!TryGetProviderHsMax(ev.Player, out float providerMax))
            return;

        ScheduleHsClampRestore(ev.Player, ev.Player.HumeShield, providerMax);
    }

    private static void OnDyingScp173HsGuard(DyingEventArgs ev)
    {
        // Tantrum(Stained) 처치 보상도 provider HsMax로 클램프된다.
        var attacker = ev?.Attacker;
        if (attacker == null
            || attacker.Role.Type != RoleTypeId.Scp173
            || !HasEchoHsBonus(attacker)
            || !TryGetProviderHsMax(attacker, out float providerMax))
            return;

        if (ev.Player == null
            || !ev.Player.TryGetEffect(EffectType.Stained, out var stained)
            || !stained.IsEnabled)
            return;

        float reward = ev.Player.Role.Type.IsFlamingo()
            ? Scp173TantrumFlamingoHsReward
            : Scp173TantrumHsReward;

        PendingScp173HsFixes[attacker] = new PendingScp173HsFix(attacker.HumeShield, providerMax, reward);
    }

    private static void OnDiedScp173HsGuard(DiedEventArgs ev)
    {
        var attacker = ev?.Attacker;
        if (attacker == null || !PendingScp173HsFixes.TryGetValue(attacker, out var pending))
            return;

        PendingScp173HsFixes.Remove(attacker);
        ScheduleHsRestore(attacker, pending.SavedHs, pending.ProviderMax, pending.Reward);
    }

    /// <summary>
    /// SCP-173의 이동속도 1당 순간이동 쿨타임을 1.4% 줄이며, 1초보다 짧아지지 않게 합니다.
    /// </summary>
    private static float GetScp173BlinkCooldown(float moveSpeed)
    {
        float clampedMoveSpeed = Mathf.Clamp(moveSpeed, 0f, Scp173MaximumMoveSpeed);
        float cooldown = Scp173BaseBlinkCooldown
            * (1f - clampedMoveSpeed * Scp173BlinkCooldownReductionPerMoveSpeed);
        return Mathf.Max(Scp173MinimumBlinkCooldown, cooldown);
    }

    private static IEnumerator<float> StaminaDrainReductionRoutine(Player player, float reductionPercent)
    {
        const float cycleSeconds = 1f;
        float noDrainSeconds = cycleSeconds * Mathf.Clamp(reductionPercent, 0f, 100f) / 100f;
        float drainSeconds = cycleSeconds - noDrainSeconds;

        while (player != null && player.IsAlive && EchoInfo.PlayerStats.ContainsKey(player))
        {
            if (noDrainSeconds > 0f)
            {
                player.IsUsingStamina = false;
                yield return Timing.WaitForSeconds(noDrainSeconds);
            }

            if (drainSeconds > 0f)
            {
                player.IsUsingStamina = true;
                yield return Timing.WaitForSeconds(drainSeconds);
            }
        }

        if (player != null)
            player.IsUsingStamina = true;
    }

    private static IEnumerator<float> RegenRoutine(Player player, EchoStatSnapshot snapshot)
    {
        while (player != null && player.IsAlive && EchoInfo.PlayerStats.ContainsKey(player))
        {
            if (player.IsScp && snapshot.HsRegen > 0)
                player.HumeShield = Math.Min(player.HumeShield + snapshot.HsRegen, player.MaxHumeShield);
            else if (!player.IsScp && snapshot.AhpRegen > 0
                     && EchoInfo.PlayerPassiveEffects.TryGetValue(player, out var state)
                     && TryGetEchoAhpProcess(player, state.EchoAhpKillCode, out var echoAhp))
            {
                echoAhp.CurrentAmount = Math.Min(echoAhp.CurrentAmount + snapshot.AhpRegen, echoAhp.Limit);
            }

            yield return Timing.WaitForSeconds(1f);
        }
    }

    public static void OnHurting(HurtingEventArgs ev)
    {
        // 별도 고정 피해는 공격력 증가와 대상의 에코 피해 감소를 모두 무시한다.
        if (IsApplyingFixedDamage(ev.Player))
            return;

        // Crushed는 수치 기반 피해가 아닌 게임의 특수 사망 판정이므로,
        // 핸들러의 Damage 값을 읽거나 다시 할당하지 않고 원본 처리를 유지한다.
        if (ev.DamageHandler?.Type == DamageType.Crushed)
            return;

        bool ignoresAttackModifiers = AreAttackModifiersIgnored(ev.Attacker);

        if (ev.Attacker == null || !EchoInfo.PlayerStats.TryGetValue(ev.Attacker, out var atkStats))
        {
            // defender-only path
        }
        else if (ev.Attacker != null && HitboxIdentity.IsEnemy(ev.Attacker.ReferenceHub, ev.Player.ReferenceHub))
        {
            if (!ignoresAttackModifiers)
            {
                float damage = ev.DamageHandler.Damage;
                int attackFlatHitScale = GetBuckshotAttackFlatHitScale(ev);

                damage += atkStats.AttackFlat / attackFlatHitScale;
                damage *= 1f + atkStats.AttackPercent / 100f;

                if (ev.Player.IsScp)
                    damage *= 1f + atkStats.ScpDamagePercent / 100f;
                else
                    damage *= 1f + atkStats.HumanDamagePercent / 100f;

                // Headshot: 기존 200%에 합산 적용 (ABattle BullsEye 참고)
                // PlayerStatsSystem.FirearmDamageHandler와 모호하므로 Exiled 타입을 명시
                if (atkStats.HeadshotDamage > 0
                    && ev.DamageHandler.CustomBase is Exiled.API.Features.DamageHandlers.FirearmDamageHandler
                    {
                        Hitbox: HitboxType.Headshot
                    })
                {
                    damage *= 1f + atkStats.HeadshotDamage / 200f;
                }

                // Critical (Ambush style) + 에코/전용무기 크리티컬 데미지 보너스
                if (atkStats.CriticalChance > 0 && UnityEngine.Random.Range(0f, 100f) < atkStats.CriticalChance)
                {
                    float critMult = 1.5f + atkStats.CriticalDamage / 100f;
                    if (ExclusiveWeaponInfo.PlayerWeapons.TryGetValue(ev.Attacker, out var weapon) && weapon != null)
                        critMult += weapon.GetCriticalDamageBonus(ev.Player) / 100f;

                    damage *= critMult;
                    Timing.CallDelayed(Timing.WaitForOneFrame, () => ev.Attacker.ShowHitMarker(2));
                }

                ev.DamageHandler.Damage = damage;
            }
        }

        if (EchoInfo.PlayerStats.TryGetValue(ev.Player, out var defStats)
            && !ignoresAttackModifiers)
        {
            float dmg = ev.DamageHandler.Damage;
            bool ignoresDefensePercent = ev.DamageHandler.Type == DamageType.PocketDimension;

            // PocketDimension은 탈출 실패 판정 피해이므로 방어력%로 감소시키지 않는다.
            if (defStats.DefensePercent > 0f && !ignoresDefensePercent)
                dmg *= Mathf.Max(0f, 1f - defStats.DefensePercent / 100f);

            if (!DefenseFlatIgnoredDamageTypes.Contains(ev.DamageHandler.Type))
                dmg = Math.Max(0f, dmg - defStats.DefenseFlat);

            ev.DamageHandler.Damage = dmg;
        }
    }

    public static bool IsApplyingFixedDamage(Player player)
    {
        return player != null && FixedDamageTargets.Contains(player);
    }

    public static void DealFixedDamage(Player target, Player attacker, float amount)
    {
        if (target == null || !target.IsAlive || amount <= 0f)
            return;

        FixedDamageTargets.Add(target);
        try
        {
            target.Hurt(attacker: attacker, amount: amount, damageType: DamageType.Custom);
        }
        finally
        {
            FixedDamageTargets.Remove(target);
        }
    }

    private static int GetBuckshotAttackFlatHitScale(HurtingEventArgs ev)
    {
        if (ev.DamageHandler?.CustomBase is not Exiled.API.Features.DamageHandlers.FirearmDamageHandler
            {
                Item: Exiled.API.Features.Items.Firearm firearm
            })
            return 1;

        if (!firearm.Base.TryGetModule<IHitregModule>(out var hitreg))
            return 1;

        if (hitreg is AttachmentDependentHitreg attachmentDependent)
            hitreg = attachmentDependent.TargetModule;

        if (hitreg is not BuckshotHitreg buckshot)
            return 1;

        int hitScale = Math.Max(1, buckshot.ActivePattern.MaxHits);

        // Shotgun Double-shot은 한 번의 공격에서 두 Buckshot 패턴을 발사한다.
        if (firearm.Type == ItemType.GunShotgun
            && firearm.Base.TryGetModule<PumpActionModule>(out var pumpAction))
        {
            hitScale *= Math.Max(1, pumpAction.ShotsPerTriggerPull);
        }

        return hitScale;
    }

    public static void OnHealing(HealingEventArgs ev)
    {
        if (ev.Player == null || !EchoInfo.PlayerStats.TryGetValue(ev.Player, out var stats))
            return;

        if (stats.HealingBonus <= 0f)
            return;

        ev.Amount *= 1f + stats.HealingBonus / 100f;
    }

    public static string GetMainStatDisplayName(EchoMainStatType type)
    {
        return type switch
        {
            EchoMainStatType.AttackPercent => "공격력%",
            EchoMainStatType.HpPercent => "HP%",
            EchoMainStatType.Defense => "방어력%",
            EchoMainStatType.ScpDamagePercent => "SCP 대상 데미지%",
            EchoMainStatType.HumanDamagePercent => "인간 대상 데미지%",
            EchoMainStatType.CriticalChance => "크리티컬 확률%",
            EchoMainStatType.CriticalDamage => "크리티컬 데미지%",
            EchoMainStatType.MoveSpeedAndJump => "이동속도/점프력",
            EchoMainStatType.StaminaDrainReduction => "스태미나 소모 감소%",
            EchoMainStatType.HeadshotDamage => "헤드샷 데미지%",
            EchoMainStatType.AhpRegenAndMax => "AHP/HS 회복",
            EchoMainStatType.SizeReduction => "크기 감소%",
            _ => "?"
        };
    }

    public static string GetSubOptionDisplayName(EchoSubOptionType type)
    {
        return type switch
        {
            EchoSubOptionType.AttackPercent => "공격력%",
            EchoSubOptionType.AttackFlat => "공격력",
            EchoSubOptionType.DefensePercent => "방어력%",
            EchoSubOptionType.DefenseFlat => "방어력",
            EchoSubOptionType.HpPercent => "HP%",
            EchoSubOptionType.HpFlat => "HP",
            EchoSubOptionType.CriticalChance => "크리티컬%",
            EchoSubOptionType.CriticalDamage => "크리티컬 데미지%",
            EchoSubOptionType.ScpDamagePercent => "SCP 대상 데미지%",
            EchoSubOptionType.HumanDamagePercent => "인간 대상 데미지%",
            EchoSubOptionType.MoveSpeed => "이동속도%",
            EchoSubOptionType.JumpPower => "점프력%",
            EchoSubOptionType.StaminaDrainReduction => "스태미나 소모 감소%",
            EchoSubOptionType.HeadshotDamage => "헤드샷 데미지%",
            EchoSubOptionType.SizeReduction => "크기 감소%",
            EchoSubOptionType.HealingBonus => "치료 효과 보너스%",
            _ => "?"
        };
    }
}

public class EchoSubOptionInstance
{
    public EchoSubOptionType Type { get; set; }
    public int Grade { get; set; }
    public float Value { get; set; }
}

public class EchoStatSnapshot
{
    public float AttackPercent;
    public float AttackFlat;
    public float HpPercent;
    public float HpFlat;
    public float DefensePercent;
    public float DefenseFlat;
    public float ScpDamagePercent;
    public float HumanDamagePercent;
    public float CriticalChance;
    public float CriticalDamage;
    public float MoveSpeed;
    public float JumpPower;
    public float StaminaDrainReduction;
    public float HeadshotDamage;
    public float SizeReduction;
    public float HealingBonus;
    public float AhpRegen;
    public float AhpMax;
    public float HsRegen;
    public float HsMax;

    /// <summary>
    /// 같은 이름 스탯을 합산하여 표시용 딕셔너리로 반환합니다.
    /// </summary>
    public Dictionary<string, float> GetAggregatedDisplay()
    {
        var dict = new Dictionary<string, float>();

        void add(string name, float value)
        {
            if (Math.Abs(value) < 0.01f)
                return;
            if (dict.ContainsKey(name))
                dict[name] += value;
            else
                dict[name] = value;
        }

        add("공격력%", AttackPercent);
        add("공격력", AttackFlat);
        add("HP%", HpPercent);
        add("HP", HpFlat);
        add("방어력%", DefensePercent);
        add("방어력", DefenseFlat);
        add("SCP 대상 데미지%", ScpDamagePercent);
        add("인간 대상 데미지%", HumanDamagePercent);
        add("크리티컬 확률%", CriticalChance);
        add("크리티컬 데미지%", CriticalDamage);
        add("이동속도%", MoveSpeed);
        add("점프력%", JumpPower);
        add("스태미나 소모 감소%", StaminaDrainReduction);
        add("헤드샷 데미지%", HeadshotDamage);
        add("크기 감소%", SizeReduction);
        add("치료 효과 보너스%", HealingBonus);
        add("AHP 회복", AhpRegen);
        add("AHP 최대", AhpMax);
        add("HS 회복", HsRegen);
        add("HS 최대", HsMax);

        return dict;
    }
}
