using Exiled.API.Extensions;
using Exiled.Events.EventArgs.Player;
using RGM.API.Features;

namespace RGM.Modes.Abilities.Epic;

[Ability("홀리 프로텍션", "자신에게 『상태이상 면역』을 부여합니다.",
    AbilityCategory.Epic, AbilityType.EPIC_HOLYPROTECTION)]
public class HolyProtection : Ability
{
    public override void OnEnabled()
        => Exiled.Events.Handlers.Player.ReceivingEffect += OnReceivingEffect;

    public override void OnDisabled() 
        => Exiled.Events.Handlers.Player.ReceivingEffect -= OnReceivingEffect;

    private void OnReceivingEffect(ReceivingEffectEventArgs ev)
    {
        if (ev.Player != Owner) return;
        var effectType = ev.Effect.GetEffectType();

        // Intensity 0 is the effect's removal request. Allow it so active debuffs can expire normally.
        if (ev.Intensity > 0 && !EffectManager.IsKeptBuff(effectType))
            ev.IsAllowed = false;
    }
}