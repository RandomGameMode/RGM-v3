using Exiled.API.Features.Roles;
using Exiled.Events.EventArgs.Player;

namespace DAONTFT.Core.TFT.Euclid.Scp939;

[TFTAbility("흡혈 발톱", "갈고리 발톱ㅣ공격 시 30의 HS를 회복합니다.(최대 1200)", TFTAbilityLevel.Euclid, TFTAbilityCategory.Scp939, TFTAbilityPoint.Once, TFTAbilityType.Attack, "⚔️")]
public class Attack : TFTAbility
{
    public override void OnEnabled()
    {
        Exiled.Events.Handlers.Player.Hurting += OnHurting;
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Player.Hurting += OnHurting;
    }
    
    private void OnHurting(HurtingEventArgs ev)
    {
        if (Owner.Role is not Scp939Role scp939) return;
        if (ev.Attacker.ReferenceHub == scp939.Owner.ReferenceHub && scp939.Owner.HumeShield < 1200)
            scp939.Owner.HumeShield += 30;

    }
}
