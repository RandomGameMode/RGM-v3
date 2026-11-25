using Exiled.Events.EventArgs.Player;

namespace DAONTFT.Core.TFT.Euclid.Human;

[TFTAbility("흡혈귀", "남에게 가한 피해량의 70%만큼 회복합니다.", 
    TFTAbilityLevel.Euclid, TFTAbilityCategory.Human, TFTAbilityPoint.Continuous, TFTAbilityType.Vampire, "🔱")]
public class Vampire : TFTAbility
{
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
        if (ev.Attacker != null && 
            ev.Attacker == Owner && 
            HitboxIdentity.IsEnemy(ev.Attacker.ReferenceHub, ev.Player.ReferenceHub))
        {
            Owner.Heal(ev.Amount * 0.7f);
        }
    }
}
