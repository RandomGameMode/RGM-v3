using Exiled.API.Features;
using System;
using System.Collections.Generic;

namespace RGM.Modes;

public static class EchoInfo
{
    public const int MaxEquippedEchoes = 5;
    public const int MaxTotalCost = 12;
    public const int MaxLevel = 25;
    public const int InitialApplyDelaySeconds = 30;
    public const int RespawnApplyDelaySeconds = 10;

    public static Dictionary<EchoType, EchoData> Echoes = new();
    public static Dictionary<Player, EchoLoadout> PlayerLoadouts = new();
    public static Dictionary<Player, List<Echo>> PlayerEchoes = new();
    public static Dictionary<Player, EchoStatSnapshot> PlayerStats = new();
    public static Dictionary<Player, bool> PlayerShowHints = new();

    /// <summary>역할 기본 MaxHealth. 레벨업 재적용 시 HP 복리 방지용.</summary>
    public static Dictionary<Player, float> PlayerBaseMaxHealth = new();

    /// <summary>역할 기본 MaxHumeShield. 재적용 시 HS 복리 방지용.</summary>
    public static Dictionary<Player, float> PlayerBaseMaxHs = new();

    /// <summary>직전에 적용한 패시브 이펙트 intensity. 재적용 전 제거용.</summary>
    public static Dictionary<Player, EchoPassiveEffectState> PlayerPassiveEffects = new();


    public static readonly Dictionary<EchoType, (string Name, string Description)> EchoCatalog = new()
    {
        { EchoType.Gnome, ("노움", "사용 시 10초간 데미지 감소 40%, 재사용 대기시간 60초") },
        { EchoType.Salamandra, ("살라만드라", "사용 시 자신이 공격하는 모든 공격에 화상 효과 적용. 지속 시간 30초, 재사용 대기시간 60초") },
        { EchoType.Undine, ("운디네", "사용 시 주변 15m 내에 있는 모든 상대 3초간 40% 감속, 재사용 대기시간 60초") },
        { EchoType.Sylph, ("실프", "12초간 이동 속도 50% 증가 및 Ghostly, Fade 효과 적용, 재사용 대기시간 60초") },
        { EchoType.Capybara, ("카피바라", "사용 시 20초간 최대 체력의 3%씩 회복, 재사용 대기시간 60초") },
        { EchoType.Berserker, ("광전사", "사용 시 다음 공격은 자신의 최대 체력의 50%만큼 추가 데미지, 재사용 대기시간 60초") },
        { EchoType.Chibi939, ("쁘띠 댕댕이", "사용 시 15초간 시야 개선 및 스태미너 무제한, 재사용 대기시간 60초") },
        { EchoType.GoldenPig, ("황금 돼지", "사용 시 체력 1205 회복(최대치 초과 불가), 재사용 대기시간 60초") },
    };

    public static IEnumerable<EchoType> GetEchoesByCost(EchoCost cost)
    {
        foreach (var pair in Echoes)
        {
            if (pair.Value.Cost == cost)
                yield return pair.Key;
        }
    }
}

public class EchoLoadout
{
    public EchoType? MainSlot { get; set; }
    public EchoType?[] SubSlots { get; set; } = new EchoType?[4];

    /// <summary>장착 전용무기. Server-Specific에서 메인 Echo 위에 표시.</summary>
    public ExclusiveWeaponType? EquippedWeapon { get; set; }

    /// <summary>슬롯별 메인 스탯 선택. Echo와 1:1로 대응합니다.</summary>
    private EchoMainStatType? MainSlotStat { get; set; }

    private EchoMainStatType?[] SubSlotStats { get; set; } = new EchoMainStatType?[4];

    public Dictionary<EchoType, int> Levels { get; set; } = new();
    /// <summary>누적 경험치. 피해량 기반 보상을 정확히 반영하기 위해 소수점도 보존합니다.</summary>
    public Dictionary<EchoType, float> Experience { get; set; } = new();

    /// <summary>
    /// Echo별 부가 옵션. 레벨업/재적용 시 기존 옵션은 유지하고 해금분만 추가합니다.
    /// </summary>
    public Dictionary<EchoType, List<EchoSubOptionInstance>> SubOptions { get; set; } = new();

    public IEnumerable<EchoType> GetEquipped()
    {
        if (MainSlot.HasValue)
            yield return MainSlot.Value;

        foreach (var slot in SubSlots)
        {
            if (slot.HasValue)
                yield return slot.Value;
        }
    }

    public int GetTotalCost()
    {
        int total = 0;
        foreach (var type in GetEquipped())
        {
            var data = type.GetData();
            if (data != null)
                total += (int)data.Cost;
        }

        return total;
    }

    public int GetEquippedCount()
    {
        int count = 0;
        foreach (var _ in GetEquipped())
            count++;
        return count;
    }

    public int GetLevel(EchoType type)
    {
        if (Levels.TryGetValue(type, out int level))
            return EchoStats.Clamp(level, 1, EchoInfo.MaxLevel);

        return 1;
    }

    public bool Contains(EchoType type)
    {
        if (MainSlot == type)
            return true;

        foreach (var slot in SubSlots)
        {
            if (slot == type)
                return true;
        }

        return false;
    }

    public void SanitizeMainSlot()
    {
        if (!MainSlot.HasValue)
            return;

        var data = MainSlot.Value.GetData();
        if (data == null || (data.Cost != EchoCost.Cost4 && data.Cost != EchoCost.Cost3))
        {
            MainSlot = null;
            MainSlotStat = null;
        }
    }

    private EchoMainStatType? GetSlotMainStat(int slotIndex)
    {
        // 0 = Main, 1~4 = Sub
        if (slotIndex == 0)
            return MainSlotStat;

        int sub = slotIndex - 1;
        if (sub < 0 || sub >= SubSlotStats.Length)
            return null;

        return SubSlotStats[sub];
    }

    public void SetSlotMainStat(int slotIndex, EchoMainStatType? stat)
    {
        if (slotIndex == 0)
        {
            MainSlotStat = stat;
            return;
        }

        int sub = slotIndex - 1;
        if (sub < 0 || sub >= SubSlotStats.Length)
            return;

        SubSlotStats[sub] = stat;
    }

    private EchoType? GetSlotEcho(int slotIndex)
    {
        if (slotIndex == 0)
            return MainSlot;

        int sub = slotIndex - 1;
        if (sub < 0 || sub >= SubSlots.Length)
            return null;

        return SubSlots[sub];
    }

    /// <summary>
    /// 슬롯에 적용할 메인 스탯. 선택값이 Cost에 유효하면 그대로, 아니면 Echo 기본값.
    /// Cost에 없는 스탯은 절대 반환하지 않습니다.
    /// </summary>
    public EchoMainStatType ResolveMainStat(int slotIndex, EchoData data)
    {
        if (data == null)
            return EchoMainStatType.None;

        var selected = GetSlotMainStat(slotIndex);
        if (selected.HasValue
            && selected.Value != EchoMainStatType.None
            && EchoStats.IsMainStatAvailable(data.Cost, selected.Value))
            return selected.Value;

        // 무효한 잔존 선택값은 제거
        if (selected.HasValue && selected.Value != EchoMainStatType.None)
            SetSlotMainStat(slotIndex, null);

        if (EchoStats.IsMainStatAvailable(data.Cost, data.MainStatType))
            return data.MainStatType;

        var available = EchoStats.GetAvailableMainStats(data.Cost);
        return available.Count > 0 ? available[0] : EchoMainStatType.None;
    }

    /// <summary>모든 슬롯의 Cost 불일치 메인 스탯을 제거합니다.</summary>
    public void SanitizeAllMainStats()
    {
        for (int i = 0; i < 5; i++)
        {
            var echoType = GetSlotEcho(i);
            if (!echoType.HasValue)
            {
                SetSlotMainStat(i, null);
                continue;
            }

            var data = echoType.Value.GetData();
            if (data == null)
            {
                SetSlotMainStat(i, null);
                continue;
            }

            var current = GetSlotMainStat(i);
            if (!current.HasValue || current.Value == EchoMainStatType.None)
                continue;

            if (!EchoStats.IsMainStatAvailable(data.Cost, current.Value))
                SetSlotMainStat(i, null);
        }
    }

    /// <summary>
    /// 장착 중인 Echo가 하나라도 MaxLevel 미만이면 true.
    /// 미장착이거나 전부 Max면 false.
    /// </summary>
    public bool HasGrowableEquipped()
    {
        foreach (var type in GetEquipped())
        {
            if (GetLevel(type) < EchoInfo.MaxLevel)
                return true;
        }

        return false;
    }

    public bool HasEquippedWeapon() => EquippedWeapon.HasValue && EquippedWeapon.Value != ExclusiveWeaponType.None;
}

/// <summary>
/// ApplyPassiveEffects가 건 이펙트 intensity 스냅샷.
/// 재적용 시 동일 intensity만 제거해 액티브 스킬 이펙트와 충돌을 줄입니다.
/// </summary>
public class EchoPassiveEffectState
{
    public byte DefenseReduction;
    public byte MovementBoost;
    public byte Lightweight;
    public bool StaminaDrainToggled;
    public float SizeReduction;
    public bool Scp173BlinkCooldownModified;

    /// <summary>스탯으로 지급한 AHP 프로세스 KillCode.</summary>
    public int? EchoAhpKillCode;
}
