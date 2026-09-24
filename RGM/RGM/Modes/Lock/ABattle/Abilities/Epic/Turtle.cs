using Exiled.API.Enums;
using Exiled.Events.EventArgs.Player;
using RGM.Modes.Abilities.Legend;
using RGM.Modes.Abilities.Synergy;

namespace RGM.Modes.Abilities.Epic;

[Ability("거북 도사", "『피격 제한』이 35까지 적용됩니다.", 
    AbilityCategory.Epic, AbilityType.EPIC_TURTLE)]
public class Turtle : Ability
{
    private const float MaxDamage = 35f;

    public override void OnEnabled()
    {
        Exiled.Events.Handlers.Player.Hurting += OnHurting;
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Player.Hurting -= OnHurting;
    }

    private void OnHurting(HurtingEventArgs ev)
    {
        if (ev.Player != Owner ||
            ev.DamageHandler.Type == DamageType.Crushed ||
            WeakPointAttack.ShouldIgnoreDefenses(ev.Attacker) || 
            ApplyFixedDamage.IsApplying)
            return;

        if (ev.IsInstantKill)
        {
            ev.IsAllowed = false;
            ev.Player.Hurt(MaxDamage, ev.DamageHandler.Type);
            
            return;
        }

        if (ev.DamageHandler.Damage > MaxDamage) {
            ev.DamageHandler.Damage = MaxDamage;
        }
    }
}