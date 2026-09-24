using System.Collections.Generic;
using Exiled.API.Features.Roles;
using MEC;

namespace RGM.Modes.Abilities.Unique.Scp106.Rare;

[Ability("에너자이저", "초당 2%의 기력을 회복합니다.", 
    AbilityCategory.Rare, AbilityType.RARE_SCP106_ENERGIZER, RoleAbility.Scp106)]

public class Energizer : Ability
{
    private const float RegenValue = 0.02f;
    private CoroutineHandle _regenVigor;
    
    public override void OnEnabled()
    {
        _regenVigor = Timing.RunCoroutine(RegenVigor());
    }

    public override void OnDisabled()
    {
        Timing.KillCoroutines(_regenVigor);
    }

    private IEnumerator<float> RegenVigor()
    {
        while (true)
        {
            if (Owner.Role is Scp106Role scp106 && scp106.Vigor < scp106.VigorAbility.Vigor.MaxValue)
                scp106.Vigor += scp106.VigorAbility.Vigor.MaxValue * RegenValue;
            
            yield return Timing.WaitForSeconds(1f);
        }
    }
}