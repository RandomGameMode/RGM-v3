using Exiled.API.Features;
using Exiled.Events.EventArgs.Scp096;
using System.Linq;
using UnityEngine;

namespace RGM.Modes.Lock.TFT.Euclid.Scp096;

[TFTAbility("천리안", "분노 시 인근에 있는 인간들을 목격자에 포함시킵니다. (최대 20명)", TFTAbilityLevel.Euclid, TFTAbilityCategory.Scp096,
    TFTAbilityPoint.Continuous, TFTAbilityType.Seer, "🔭")]
public class Seer : TFTAbility
{
    public override void OnEnabled()
    {
        Exiled.Events.Handlers.Scp096.Enraging += OnEnraging;
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Scp096.Enraging -= OnEnraging;
    }

    private void OnEnraging(EnragingEventArgs ev)
    {
        if (ev.Player != Owner)
            return;

        var Stack = 0;

        foreach (var player in Player.List.Where(x => x.IsHuman))
        {
            if (Stack == 20)
                break;

            if (!(Vector3.Distance(player.Position, ev.Player.Position) < 61)) continue;
            Stack += 1;

            ev.Scp096.AddTarget(player);
        }
    }
}
