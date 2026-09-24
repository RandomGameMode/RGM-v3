using Exiled.API.Enums;
using Exiled.Events.EventArgs.Player;
using MEC;

namespace RGM.Modes.Abilities.Epic;

[Ability("극독", "죽인 자에게 60초 동안 치명적 상태이상을 부여합니다.", AbilityCategory.Epic, AbilityType.EPIC_EXTREMEPOISON)]
public class ExtremePoison : Ability
{
    private const float Duration = 60f;
    
    public override void OnEnabled() 
        => Exiled.Events.Handlers.Player.Dying += OnDying;

    public override void OnDisabled()
        => Exiled.Events.Handlers.Player.Dying -= OnDying;

    private void OnDying(DyingEventArgs ev)
    {
        if (ev.Player != Owner || ev.Attacker == null)
            return;

        Timing.CallDelayed(Timing.WaitForOneFrame, () =>
        {
            if (!Owner.IsDead) return;
            ev.Attacker.EnableEffect(EffectType.CardiacArrest, 1,
                Duration * Owner.AbilityCount(AbilityType.EPIC_EXTREMEPOISON));
            ev.Attacker.EnableEffect(EffectType.Poisoned, 1,
                Duration * Owner.AbilityCount(AbilityType.EPIC_EXTREMEPOISON));
            ev.Attacker.EnableEffect(EffectType.Bleeding, 1,
                Duration * Owner.AbilityCount(AbilityType.EPIC_EXTREMEPOISON));
            ev.Attacker.EnableEffect(EffectType.Exhausted, 1,
                Duration * Owner.AbilityCount(AbilityType.EPIC_EXTREMEPOISON));
            ev.Attacker.EnableEffect(EffectType.Corroding, 1,
                Duration * Owner.AbilityCount(AbilityType.EPIC_EXTREMEPOISON));
        });
    }
}