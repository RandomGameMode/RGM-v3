using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using MEC;
using System;
using System.Collections.Generic;

namespace RGM.Modes;

public abstract class Echo
{
    public abstract void OnEnabled();
    public abstract void OnActiveEffect();

    public EchoData Data { get; set; }
    public Player Owner { get; set; }
    public int Level { get; set; } = 1;
    public bool IsMainSlot { get; set; }

    /// <summary>플레이어가 Server-Specific에서 고른 메인 스탯. 미선택 시 Data.MainStatType 사용.</summary>
    public EchoMainStatType SelectedMainStat { get; set; } = EchoMainStatType.None;

    public List<EchoSubOptionInstance> SubOptions { get; set; } = new();
}

/// <summary>
/// 메인 슬롯 Echo의 액티브 스킬. Noclip(ALT) 키로 발동합니다.
/// </summary>
public abstract class EchoActiveAbility : Echo
{
    public bool IsOnCooldown { get; private set; }
    public float RemainingDuration { get; protected set; }
    public float RemainingCooldown { get; protected set; }

    public abstract float Duration { get; }
    public abstract float Cooldown { get; }
    public abstract string ActiveDescription { get; }

    public override void OnEnabled()
    {
        if (IsMainSlot)
            Exiled.Events.Handlers.Player.TogglingNoClip += OnTogglingNoClip;
    }

    public override void OnActiveEffect()
    {
        if (IsMainSlot)
            Exiled.Events.Handlers.Player.TogglingNoClip -= OnTogglingNoClip;

        Timing.KillCoroutines($"EchoActive_{Owner?.UserId}_{Data?.EchoType}");
    }

    private void OnTogglingNoClip(TogglingNoClipEventArgs ev)
    {
        if (ev.Player != Owner || !IsMainSlot || IsOnCooldown)
            return;

        if (!CanUseActive())
            return;

        ev.IsAllowed = false;
        OnActiveUsed();

        Timing.RunCoroutine(CooldownRoutine(), $"EchoActive_{Owner.UserId}_{Data.EchoType}");
    }

    protected virtual bool CanUseActive() => Owner != null && Owner.IsAlive;

    protected abstract void OnActiveUsed();

    private IEnumerator<float> CooldownRoutine()
    {
        IsOnCooldown = true;
        RemainingDuration = Duration;
        RemainingCooldown = Cooldown;

        float elapsed = 0f;
        while (elapsed < Duration)
        {
            RemainingDuration = Duration - elapsed;
            RemainingCooldown = Cooldown - elapsed;
            elapsed += 0.2f;
            yield return Timing.WaitForSeconds(0.2f);
        }

        RemainingDuration = 0f;

        while (elapsed < Cooldown)
        {
            RemainingCooldown = Cooldown - elapsed;
            elapsed += 0.2f;
            yield return Timing.WaitForSeconds(0.2f);
        }

        RemainingCooldown = 0f;
        IsOnCooldown = false;
    }
}

public class EchoData
{
    public Type Type { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Emoji { get; set; } = "🎲";
    public EchoType EchoType { get; set; }
    public EchoCost Cost { get; set; }
    public EchoMainStatType MainStatType { get; set; }

    public string GetFormattedName()
    {
        return $"<color={Cost.GetColor()}>[Cost{(int)Cost}]</color> {Emoji} {Name}";
    }
}

[AttributeUsage(AttributeTargets.Class)]
public class EchoAttribute(
    string name,
    string description,
    EchoType type,
    EchoCost cost,
    EchoMainStatType mainStatType,
    string emoji = "🎲") : Attribute
{
    public string Name { get; } = name;
    public string Description { get; } = description;
    public string Emoji { get; set; } = emoji;
    public EchoType Type { get; set; } = type;
    public EchoCost Cost { get; set; } = cost;
    public EchoMainStatType MainStatType { get; set; } = mainStatType;
}

public static class EchoTypeExtensions
{
    public static EchoData GetData(this EchoType type)
    {
        if (!EchoInfo.Echoes.TryGetValue(type, out var echo))
            return null;

        return echo;
    }
}

public static class EchoCostExtensions
{
    public static string GetColor(this EchoCost cost)
    {
        return cost switch
        {
            EchoCost.Cost4 => "#f43838",
            EchoCost.Cost3 => "#f4ee38",
            EchoCost.Cost1 => "#80f537",
            _ => "white"
        };
    }

    public static string GetTranslation(this EchoCost cost)
    {
        return cost switch
        {
            EchoCost.Cost4 => "Cost 4",
            EchoCost.Cost3 => "Cost 3",
            EchoCost.Cost1 => "Cost 1",
            _ => "?"
        };
    }
}

public enum EchoCost
{
    None = 0,
    Cost1 = 1,
    Cost3 = 3,
    Cost4 = 4
}

public enum EchoMainStatType
{
    None,
    AttackPercent,
    HpPercent,
    Defense,
    ScpDamagePercent,
    HumanDamagePercent,
    CriticalChance,
    MoveSpeedAndJump,
    StaminaDrainReduction,
    HeadshotDamage,
    AhpRegenAndMax,
    SizeReduction,
    CriticalDamage
}

public enum EchoSubStatType
{
    None,
    AttackFlat,
    HpFlat,
    HealingBonus
}

public enum EchoSubOptionType
{
    None,
    AttackPercent,
    AttackFlat,
    DefensePercent,
    DefenseFlat,
    HpPercent,
    HpFlat,
    CriticalChance,
    ScpDamagePercent,
    HumanDamagePercent,
    MoveSpeed,
    JumpPower,
    StaminaDrainReduction,
    HeadshotDamage,
    SizeReduction,
    HealingBonus,
    CriticalDamage
}

public enum EchoType
{
    None,

    // Cost4
    Gnome,
    Salamandra,
    Undine,
    Sylph,
    Capybara,
    Berserker,

    // Cost3
    Chibi049,
    Chibi096,
    Chibi106,
    Chibi173,
    Chibi939,
    GoldenPig,

    // Cost1
    ClassD,
    Scientist,
    FacilityGuard,
    Chaos,
    Mtf
}
