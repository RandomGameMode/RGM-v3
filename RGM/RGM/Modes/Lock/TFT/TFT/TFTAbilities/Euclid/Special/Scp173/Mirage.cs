using Exiled.API.Enums;
using Exiled.Events.EventArgs.Player;
using RGM.API.Features;

namespace DAONTFT.Core.TFT.Euclid.Scp173;

[TFTAbility("신기루", "공격에 성공할 경우, 2초 동안 투명화 상태가 됩니다.", TFTAbilityLevel.Euclid, TFTAbilityCategory.Scp173,
    TFTAbilityPoint.Continuous, TFTAbilityType.Mirage, "😧")]
public class Mirage : TFTAbility
{
    public override void OnEnabled()
    {
        Exiled.Events.Handlers.Player.Died += OnDied;
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Player.Died -= OnDied;
    }

    private void OnDied(DiedEventArgs ev)
    {
        if (ev.Attacker != null && ev.Attacker == Owner) Owner.AddEffect(EffectType.Invisible, 1, 2);
    }
}
