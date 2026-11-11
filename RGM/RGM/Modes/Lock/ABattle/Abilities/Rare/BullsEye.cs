using Exiled.API.Features.DamageHandlers;
using Exiled.Events.EventArgs.Player;

namespace RGM.Modes.Abilities.Rare;

[Ability("불스아이", "헤드샷 데미지 배율이 75%p 증가합니다.", AbilityCategory.Rare, AbilityType.RARE_BULLSEYE)]
public class BullsEye : Ability
{
    public const float HeadshotBonus = 0.75f;

    public override void OnEnabled()
        => Exiled.Events.Handlers.Player.Hurting += OnHurting;

    public override void OnDisabled()
        => Exiled.Events.Handlers.Player.Hurting -= OnHurting;

    private void OnHurting(HurtingEventArgs ev)
    {
        if (ev.Attacker != Owner || ev.DamageHandler.CustomBase is not FirearmDamageHandler damageHandler)
            return;

        if (damageHandler.Hitbox == HitboxType.Headshot)
            damageHandler.Damage *= 1f + HeadshotBonus * Owner.AbilityCount(AbilityType.RARE_BULLSEYE);
    }
}