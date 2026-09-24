using System.Collections.Generic;
using Exiled.API.Features.Roles;
using MEC;

namespace RGM.Modes.Abilities.Unique.Scp079.Common;

[Ability("응당한 대가", """
                   30초동안 전력을 사용할 수 없습니다.
                   지속시간 이후 15초마다 10의 경험치를 10회 획득합니다.
                   """,
    AbilityCategory.Normal, AbilityType.NORMAL_SCP079_JUSTPRICE, RoleAbility.Scp079)]
public class JustPrice : Ability
{
    private CoroutineHandle _justPriceHandle;
    public override void OnEnabled()
    {
        _justPriceHandle = Timing.RunCoroutine(Enumerator());
        return;

        IEnumerator<float> Enumerator()
        {
            if (Owner.Role is Scp079Role scp079)
            {
                for (int i = 0; i < 30; i++)
                {
                    scp079.Energy = 0;
                    Timing.WaitForSeconds(1f);
                }
                for (int k = 0; k < 10; k++)
                {
                    scp079.AddExperience(10);
                    yield return Timing.WaitForSeconds(15f);
                }
            }
        }
    }

    public override void OnDisabled()
    {
        Timing.KillCoroutines(_justPriceHandle);
    }
}
