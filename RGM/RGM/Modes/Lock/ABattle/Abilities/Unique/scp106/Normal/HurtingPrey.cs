using Exiled.API.Enums;
using Exiled.Events.EventArgs.Player;
using RGM.API.Features;

namespace RGM.Modes.Abilities.Unique.Scp106.Normal;

[Ability("사냥감 모색", "공격 성공 후 2초간 속도가 10% 증가합니다.", AbilityCategory.Normal, AbilityType.NORMAL_SCP106_HUNTINGPREY, RoleAbility.Scp106)]
public class HurtingPrey : Ability
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
        if (ev.Attacker == null || ev.Attacker != Owner)
            return;

        ev.Attacker.AddEffect(EffectType.MovementBoost, 10, 2);
    }
}
