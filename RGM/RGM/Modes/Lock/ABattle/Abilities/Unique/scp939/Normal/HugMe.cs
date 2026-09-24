using Exiled.API.Enums;
using Exiled.Events.EventArgs.Player;
using RGM.API.Features;

namespace RGM.Modes.Abilities.Unique.Scp939.Normal;

[Ability("그 시절 댕댕이", "피격 시 3초간 이동 속도가 15% 증가합니다.",
    AbilityCategory.Normal, AbilityType.NORMAL_SCP939_HUGME, RoleAbility.Scp939)]
public class HugMe : Ability
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
        if (ev.Player != Owner)
            return;

        // 피격될 때마다 지속 시간만 갱신하고, 이동 속도 증가 효과를 중첩하지 않습니다.
        ev.Player.AddEffect(EffectType.MovementBoost, 15, 3);
    }
}
