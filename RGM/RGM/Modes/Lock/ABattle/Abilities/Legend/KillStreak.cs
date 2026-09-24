using System.Linq;
using Exiled.Events.EventArgs.Player;
using RGM.API.Features;

namespace RGM.Modes.Abilities.Legend;

[Ability("킬스트릭", "적을 처치할 때마다 새로운 능력을 얻습니다. (인간 진영의 경우 능력 15개를 지급받습니다.)",
    AbilityCategory.Legend, AbilityType.LEGEND_KILLSTREAK)]
public class KillStreak : Ability
{

    public override void OnEnabled()
    {
        if (!Owner.IsScpRole()) {
            for (int i = 0; i < 15; i++)
                Owner.AddAbility(ABattle.Instance.GetRandomAbilities(Owner,
                    ABattle.GetCategory(Owner,
                        allowAncient: false),
                    1,
                    [AbilityType.NORMAL_FRIENDSHIP, AbilityType.NORMAL_REROLL, AbilityType.RARE_TELEPORTATION, AbilityType.RARE_DND]).First());
        }
        Exiled.Events.Handlers.Player.Died += OnDied;
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Player.Died -= OnDied;
    }

    private void OnDied(DiedEventArgs ev)
    {
        if (ev.Attacker == null || ev.Attacker != Owner)
            return;

        ABattle.Instance.StartSelect(ev.Attacker);
    }
}
