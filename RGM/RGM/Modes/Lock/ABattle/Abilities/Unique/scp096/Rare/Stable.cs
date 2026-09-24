using System.Collections.Generic;
using Exiled.API.Features.Roles;
using MEC;

namespace RGM.Modes.Abilities.Unique.Scp096.Rare;

[Ability("안정", "SCP-096의 TryingNotToCry 스킬 사용 시 초당 최대 HP의 0.5%만큼 회복합니다.",
    AbilityCategory.Rare, AbilityType.RARE_SCP096_STABLE, RoleAbility.Scp096)]

public class Stable : Ability
{
    private CoroutineHandle _usingTryNotToCrySkill;

    public override void OnEnabled()
    {
        _usingTryNotToCrySkill = Timing.RunCoroutine(UsingTryNotToCrySkill());
    }

    public override void OnDisabled()
    {
        Timing.KillCoroutines(_usingTryNotToCrySkill);
    }

    private IEnumerator<float> UsingTryNotToCrySkill()
    {
        while (true)
        {
            if (Owner.Role is Scp096Role scp096 && Owner.IsAlive && Owner.Health < Owner.MaxHealth)
            {
                if (scp096.TryNotToCryActive)
                {
                    Owner.Heal(Owner.MaxHealth * 0.005f);
                }
            }
            yield return Timing.WaitForSeconds(1);
        }
    }
}