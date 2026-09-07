using System.Collections.Generic;
using System.Linq;
using Exiled.API.Extensions;
using Exiled.Events.EventArgs.Player;
using MEC;

namespace RGM.Modes.Abilities.Epic;

[Ability("도굴꾼", "사망한 아군의 능력 중 하나를 랜덤으로 획득합니다.", 
    AbilityCategory.Epic, AbilityType.EPIC_GRAVEROBBER)]
public class GraveRobber : Ability
{
    public override void OnEnabled() 
        => Exiled.Events.Handlers.Player.Dying += OnDying;

    public override void OnDisabled() 
        => Exiled.Events.Handlers.Player.Dying -= OnDying;

    private void OnDying(DyingEventArgs ev)
    {
        if (ev.Player.LeadingTeam != Owner.LeadingTeam || ev.Player.GetAbilities().Count == 0)
            return;

        List<AbilityType> abilityTypes = ev.Player.GetAbilities().Where(x=> x.Data.RoleAbility == RoleAbility.None).Select(x => x.Data.AbilityType).ToList();
        if (abilityTypes.Count == 0)
            return;

        Timing.CallDelayed(Timing.WaitForOneFrame, () =>
        {
            if (!ev.Player.IsDead) return;
            Owner.AddAbility(abilityTypes.GetRandomValue());
        });
    }
}
