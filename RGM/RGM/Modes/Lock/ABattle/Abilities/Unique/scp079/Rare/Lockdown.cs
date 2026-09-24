using System.Collections.Generic;
using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Doors;
using MEC;

namespace RGM.Modes.Abilities.Unique.Scp079.Rare;

[Ability("봉쇄", "16초 간 모든 문을 닫고, 잠급니다. 모든 방이 정전됩니다.", 
    AbilityCategory.Rare, AbilityType.RARE_SCP079_LOCKDOWN, RoleAbility.Scp079)]
public class Lockdown : Ability
{
    public override void OnEnabled()
    {
        List<Door> doors = [.. Door.List.Where(x => !x.IsElevator && !x.Type.ToString().Contains("Scp079"))];

        foreach (var door in doors)
        {
            door.IsOpen = false;
            door.Lock(1205, DoorLockType.Lockdown079);
        }

        Timing.CallDelayed(16, () =>
        {
            foreach (var door in doors)
            {
                door.Unlock();
            }
        });

        Map.TurnOffAllLights(16);
    }
}
