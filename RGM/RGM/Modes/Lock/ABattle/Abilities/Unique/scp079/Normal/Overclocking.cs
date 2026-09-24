using System.Collections.Generic;
using Exiled.API.Features.Roles;
using MEC;
using PlayerRoles;

namespace RGM.Modes.Abilities.Unique.Scp079.Common;

[Ability("오버클럭", "1초마다 전력을 0.4 얻습니다.", AbilityCategory.Normal, AbilityType.NORMAL_SCP079_OVERCLOCKING, RoleAbility.Scp079)]
public class Overclocking : Ability
{
    public override void OnEnabled()
    {
        IEnumerator<float> enumerator()
        {
            while (Owner.Role.Type == RoleTypeId.Scp079)
            {
                if (Owner.Role is Scp079Role scp079)
                    scp079.Energy += 0.4f;

                yield return Timing.WaitForSeconds(1f);
            }
        }

        Timing.RunCoroutine(enumerator());
    }
}
