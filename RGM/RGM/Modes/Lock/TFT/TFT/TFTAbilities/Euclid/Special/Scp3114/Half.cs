using Exiled.API.Enums;
using Exiled.Events.EventArgs.Player;
using RGM.API.Features;

namespace RGM.Modes.Lock.TFT.Euclid.Scp3114;

[TFTAbility("반블럭", "공격에 맞으면 이동 속도가 2초 동안 1% 증가합니다. (중첩 가능)", TFTAbilityLevel.Euclid, TFTAbilityCategory.Scp3114,
    TFTAbilityPoint.Continuous, TFTAbilityType.Half, "📘")]
public class Half : TFTAbility
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
        if (ev.Attacker != null && Owner == ev.Player) Owner.AddEffect(EffectType.MovementBoost, 1, 2);
    }
}
