using Exiled.API.Features;
using Exiled.Events.EventArgs.Scp079;
using System.Linq;

namespace RGM.Modes.Lock.TFT.Keter.Scp079;

[TFTAbility("휴게소", "[경험치 획득]ㅣ생존한 SCP의 체력이 획득한 경험치의 50%만큼 회복됩니다.", TFTAbilityLevel.Keter, TFTAbilityCategory.Scp079,
    TFTAbilityPoint.Continuous, TFTAbilityType.RestArea, "🛄")]
public class RestArea : TFTAbility
{
    public override void OnEnabled()
    {
        Exiled.Events.Handlers.Scp079.GainingExperience += OnGainingExperience;
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Scp079.GainingExperience -= OnGainingExperience;
    }

    private void OnGainingExperience(GainingExperienceEventArgs ev)
    {
        if (Owner != ev.Player)
            return;

        foreach (var scp in Player.List.Where(x => x.IsScp))
        {
            if (scp.Health >= scp.MaxHealth)
                continue;

            scp.Health += ev.Amount * 0.5f;
        }
    }
}
