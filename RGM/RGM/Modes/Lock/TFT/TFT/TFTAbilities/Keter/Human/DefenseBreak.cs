using Exiled.Events.EventArgs.Player;
using RGM.API.Features;

namespace RGM.Modes.Lock.TFT.Keter.Human;

[TFTAbility("디펜스 브레이크", "자신의 모든 공격이 방어 무시 데미지로 변환됩니다.",
    TFTAbilityLevel.Keter, TFTAbilityCategory.Human, TFTAbilityPoint.Continuous, TFTAbilityType.DefenseBreak)]
public class DefenseBreak : TFTAbility
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
        if (ev.Attacker == null ||
            ev.Attacker != Owner ||
            !HitboxIdentity.IsEnemy(ev.Attacker.ReferenceHub, ev.Player.ReferenceHub) ||
            ApplyFixedDamage.IsApplying) return;

        var damage = ev.Amount;
        ev.IsAllowed = false;
        if (ApplyFixedDamage.Apply(ev.Attacker, ev.Player, damage)) ev.Attacker.ShowHitMarker();
    }
}