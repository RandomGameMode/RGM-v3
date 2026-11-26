using Exiled.Events.EventArgs.Scp079;

namespace DAONTFT.Core.TFT.Keter.Scp079;

[TFTAbility("휴대용 정전기", "핑ㅣ해당 방이 2.5초 간 정전이 됩니다.", TFTAbilityLevel.Keter, TFTAbilityCategory.Scp079, TFTAbilityPoint.Continuous, TFTAbilityType.PingRemote, "🔲")]
public class PingRemote : TFTAbility
{
    public override void OnEnabled()
    {
        Exiled.Events.Handlers.Scp079.Pinging += OnPinging;
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Scp079.Pinging -= OnPinging;
    }

    private void OnPinging(PingingEventArgs ev)
    {
        if (ev.Player != Owner)
            return;

        if (!ev.Room.AreLightsOff)
            ev.Room.TurnOffLights(2.5f);
    }
}
