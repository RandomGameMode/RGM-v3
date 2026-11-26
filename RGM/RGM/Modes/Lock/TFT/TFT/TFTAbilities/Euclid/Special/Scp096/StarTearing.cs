using Exiled.Events.EventArgs.Player;
using RGM.API.Features;
using static RGM.Variables.Variable;

namespace RGM.Modes.Lock.TFT.Euclid.Scp096;

[TFTAbility("별자리 찢기", "공격 시 30% 확률로 대상을 즉시 사망시킵니다.", 
    TFTAbilityLevel.Euclid, TFTAbilityCategory.Scp096, TFTAbilityPoint.Continuous, TFTAbilityType.StarTearing, "🌟")]
public class StarTearing : TFTAbility
{
    public override void OnEnabled()
    {
        Exiled.Events.Handlers.Player.Hurting += OnHurting;
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Player.Hurting -= OnHurting;
    }

    private void OnHurting(HurtingEventArgs ev)
    {
        if (ev.Attacker == null || ev.Attacker != Owner)
            return;

        if (UnityEngine.Random.Range(1, 101) > 30) return;
        if (GodModePlayers.Contains(ev.Player))
            GodModePlayers.Remove(ev.Player);

        ev.Player.Hit(ev.Attacker, ev.Player.MaxHealth);
    }
}
