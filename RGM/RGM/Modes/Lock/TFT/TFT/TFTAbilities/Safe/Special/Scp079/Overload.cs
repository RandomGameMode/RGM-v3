using System.Collections.Generic;
using Exiled.API.Features.Roles;
using MEC;

namespace RGM.Modes.Lock.TFT.Safe.Scp079;

[TFTAbility("과부화", "1분 동안 전력이 무제한이 됩니다.", TFTAbilityLevel.Safe, TFTAbilityCategory.Scp079, TFTAbilityPoint.Once, TFTAbilityType.Overload, "🔮")]
public class Overload : TFTAbility
{
    public override void OnEnabled()
    {
        IEnumerator<float> _overload()
        {
            for (int i = 0; i < 60; i++)
            {
                if (Owner.Role is Scp079Role scp079)
                {
                    scp079.Energy = scp079.MaxEnergy;
                }

                yield return Timing.WaitForSeconds(1);
            }
        }

        Timing.RunCoroutine(_overload());
    }
}
