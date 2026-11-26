using Exiled.Events.EventArgs.Scp096;

namespace RGM.Modes.Lock.TFT.Euclid.Scp096;

[TFTAbility("지구력", "돌격ㅣ능력의 지속 시간이 50% 증가합니다.", TFTAbilityLevel.Euclid, TFTAbilityCategory.Scp096,
    TFTAbilityPoint.Continuous, TFTAbilityType.Enraging, "📶")]
public class Enraging : TFTAbility
{
    public override void OnEnabled()
    {
        Exiled.Events.Handlers.Scp096.Enraging += OnEnraging;
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Scp096.Enraging -= OnEnraging;
    }

    private static void OnEnraging(EnragingEventArgs ev)
    {
        ev.InitialDuration *= 1.5f;
    }
}
