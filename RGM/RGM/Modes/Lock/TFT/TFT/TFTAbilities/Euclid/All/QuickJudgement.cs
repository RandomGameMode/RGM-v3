using Exiled.Events.EventArgs.Player;
using RGM.API.Features;

namespace RGM.Modes.Lock.TFT.Euclid.All;

[TFTAbility("빠른 판단", "1분 동안 무적 상태(일부 데미지 제외)가 되며, 체포되지 않습니다.", TFTAbilityLevel.Euclid, TFTAbilityCategory.All, TFTAbilityPoint.Once, TFTAbilityType.QuickJudgement, "💦")]
public class QuickJudgement : TFTAbility
{
    public override void OnEnabled()
    {
        Owner.ApplyGodMode(60);

        Owner.Cuffer = null;

        Exiled.Events.Handlers.Player.Handcuffing += OnHandcuffing;
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Player.Handcuffing -= OnHandcuffing;
    }

    private void OnHandcuffing(HandcuffingEventArgs ev)
    {
        if (ev.Target == Owner)
            ev.IsAllowed = false;
    }
}
