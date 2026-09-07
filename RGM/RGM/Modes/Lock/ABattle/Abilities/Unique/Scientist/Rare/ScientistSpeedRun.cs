using Exiled.API.Enums;
using Exiled.API.Features.Doors;
using MEC;
using UnityEngine;

namespace RGM.Modes.Abilities.Unique.Scientist.Rare;

[Ability("스피드런", "즉시 지상으로 순간이동합니다.", 
    AbilityCategory.Rare, AbilityType.RARE_SCIENTIST_SCIENTISTSPEEDRUN, RoleAbility.Scientist)]

public class ScientistSpeedRun : Ability
{
    public override void OnEnabled()
    {
        Owner.Position = Door.Get(DoorType.EscapeSecondary).Position + Vector3.up * 1.5f;
        Timing.CallDelayed(Timing.WaitForOneFrame, () => Owner.RemoveAbility(this));
    }
}