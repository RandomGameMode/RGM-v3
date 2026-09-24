using Exiled.Events.EventArgs.Player;

namespace RGM.Modes.Abilities.Legend;

[Ability("마술사", "피해를 입으면 피해량의 85%만큼 최대 HP가 늘어납니다.",
    AbilityCategory.Legend, AbilityType.LEGEND_MAGICIAN)]
public class Magician : Ability
{
    public override void OnEnabled()
    {
        Exiled.Events.Handlers.Player.Hurt += OnHurt;
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Player.Hurt -= OnHurt;
    }

    private void OnHurt(HurtEventArgs ev)
    {
        if (ev.Player != Owner || ev.Attacker == null || !HitboxIdentity.IsEnemy(ev.Player.ReferenceHub, ev.Attacker.ReferenceHub))
            return;

        var add = ev.DamageHandler.Damage * 0.85f;
        ev.Player.MaxHealth += add;
        ev.Player.Health += add;
    }
}
