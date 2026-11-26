using System.Collections.Generic;
using Exiled.API.Features.Roles;
using MEC;

namespace RGM.Modes.Lock.TFT.Safe.Scp079;

[TFTAbility("응당한 대가", "레벨이 1 증가합니다. 20초 간 전력이 0이 됩니다.", TFTAbilityLevel.Safe, TFTAbilityCategory.Scp079, TFTAbilityPoint.Once, TFTAbilityType.JustPrice, "💸")]
public class JustPrice : TFTAbility
{
    public override void OnEnabled()
    {
        IEnumerator<float> enumerator()
        {
            if (Owner.Role is Scp079Role { Level: < 5 } scp079)
                scp079.Level += 1;

            for (int i = 0; i < 20; i++)
            {
                if (Owner.Role is Scp079Role scp079_)
                    scp079_.Energy = 0;

                yield return Timing.WaitForSeconds(1f);
            }
        }

        Timing.RunCoroutine(enumerator());
    }
}
