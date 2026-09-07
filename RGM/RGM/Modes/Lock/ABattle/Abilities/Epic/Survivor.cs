using CustomPlayerEffects;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Scp106;
using Exiled.Events.EventArgs.Warhead;
using MapGeneration.Holidays;
using MEC;
using RGM.API.Features;
using RGM.Modes.Abilities.Synergy;

namespace RGM.Modes.Abilities.Epic;

[Ability("구사일생", "사망 판정을 받을 경우, 2.5초간 투명 상태와 무적이 되며, 체력을 27% 회복합니다. (최대 3번)", AbilityCategory.Epic, AbilityType.EPIC_SURVIVOR)]
public class Survivor : Ability
{
    private const float InvincibilityDuration = 2.5f;

    private static bool _isDetonatingState;

    private int _power = 3;
    private bool _isEnabled;
    private int _version;

    public override void OnEnabled()
    {
        Exiled.Events.Handlers.Player.Dying += OnDying;
        Exiled.Events.Handlers.Player.Hurting += OnHurting;
        Exiled.Events.Handlers.Scp106.Attacking += OnScp106Attacking;
        Exiled.Events.Handlers.Warhead.Detonating += OnDetonating;
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Player.Dying -= OnDying;
        Exiled.Events.Handlers.Player.Hurting -= OnHurting;
        Exiled.Events.Handlers.Scp106.Attacking -= OnScp106Attacking;
        Exiled.Events.Handlers.Warhead.Detonating -= OnDetonating;

        _version++;
        _isEnabled = false;
    }

    private void OnDetonating(DetonatingEventArgs _)
    {
        if (_isDetonatingState)
            return;

        _isDetonatingState = true;
        Timing.CallDelayed(Timing.WaitForOneFrame, () => _isDetonatingState = false);
    }

    private void OnDying(DyingEventArgs ev)
    {
        if (ev.Player != Owner || IsExemptDamage(ev.Player, ev.DamageHandler.Type) ||
            WeakPointAttack.ShouldIgnoreDefenses(ev.Attacker))
            return;

        if (_isEnabled)
        {
            ev.IsAllowed = false;
            return;
        }

        if (TrySurvive())
            ev.IsAllowed = false;
    }

    private void OnHurting(HurtingEventArgs ev)
    {
        if (WeakPointAttack.ShouldIgnoreDefenses(ev.Attacker))
            return;

        if (ev.Player == Owner &&
            !_isEnabled &&
            ev.IsAllowed &&
            !IsExemptDamage(ev.Player, ev.DamageHandler.Type) &&
            IsLethalDamage(ev) &&
            TrySurvive())
        {
            ev.IsAllowed = false;
            ev.DamageHandler.Damage = 0f;
            return;
        }

        if (_isEnabled &&
            ev.Player == Owner &&
            (!IsExemptDamage(ev.Player, ev.DamageHandler.Type) ||
             ev.DamageHandler.Type == DamageType.PocketDimension && !ev.IsInstantKill))
            ev.IsAllowed = false;
    }

    private void OnScp106Attacking(AttackingEventArgs ev)
    {
        if (ev.Target != Owner || !_isEnabled || WeakPointAttack.ShouldIgnoreDefenses(ev.Player))
            return;

        ev.IsAllowed = false;
    }

    private bool TrySurvive()
    {
        if (!ABattle.Instance.IsLifeUsed.TryGetValue(Owner, out bool isLifeUsed))
            ABattle.Instance.IsLifeUsed[Owner] = false;
        else if (isLifeUsed)
            return false;

        ABattle.Instance.IsLifeUsed[Owner] = true;
        Timing.CallDelayed(Timing.WaitForOneFrame, () =>
        {
            if (ABattle.Instance.IsLifeUsed.ContainsKey(Owner))
                ABattle.Instance.IsLifeUsed[Owner] = false;
        });

        ActivateSurvivor();
        return true;
    }

    private bool IsLethalDamage(HurtingEventArgs ev)
    {
        float damage = ev.DamageHandler.Damage;
        if (damage <= 0f && !ev.IsInstantKill)
            return false;

        float totalHealth = Owner.Health + Owner.ArtificialHealth + Owner.HumeShield;
        return ev.IsInstantKill || damage >= totalHealth;
    }

    private void ActivateSurvivor()
    {
        _isEnabled = true;

        Owner.EnableEffect(EffectType.Invisible, 1, InvincibilityDuration);
        Owner.EnableEffect(EffectType.Ghostly, 1, InvincibilityDuration);
        Owner.AddEffect(EffectType.MovementBoost, 30, InvincibilityDuration);
        Owner.Heal(Owner.MaxHealth * 0.27f);

        int remaining = _power - 1;
        int version = ++_version;
        bool removeAfter = _power == 1;
        if (!removeAfter)
            _power--;

        Timing.CallDelayed(InvincibilityDuration, () =>
        {
            if (_version != version)
                return;

            _isEnabled = false;

            if (removeAfter)
                Owner.RemoveAbility(this);
        });

        Owner.AddHint("구사일생", $"<color={ABattle.RatingColor["영웅"]}>구사일생</color> 능력으로 인해 3초간 죽음을 피합니다. ({remaining}번 남음)");
    }

    private static bool IsExemptDamage(Player player, DamageType damageType)
    {
        if (_isDetonatingState ||
            damageType is DamageType.Warhead or DamageType.PocketDimension or DamageType.Crushed)
            return true;

        // PlayerEvents와 동일: Lightweight 중 Falldown은 피해가 적용되지 않음
        if (damageType == DamageType.Falldown &&
            player.TryGetEffect(EffectType.Lightweight, out StatusEffectBase lightweight) &&
            lightweight.IsEnabled)
        {
            if (HolidayUtils.IsHolidayActive(HolidayType.Halloween) &&
                player.TryGetEffect(EffectType.Metal, out StatusEffectBase metal) &&
                metal.IsEnabled)
                return false;

            return true;
        }

        return false;
    }
}
