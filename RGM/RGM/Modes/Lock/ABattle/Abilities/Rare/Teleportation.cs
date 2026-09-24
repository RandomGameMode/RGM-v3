using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using MEC;
using PlayerRoles;
using RGM.API.Features;

namespace RGM.Modes.Abilities.Rare;

[Ability("순간이동", """
                 랜덤한 유저의 위치로 순간이동합니다. 
                 순간이동 후, 5초간 생존 보정을 받습니다.
                 """,
    AbilityCategory.Rare, AbilityType.RARE_TELEPORTATION)]
public class Teleportation : Ability
{
    public override void OnEnabled()
    {
        Player target = PlayerManager.List.Where(x => x != Owner && x.IsAlive && x.Role.Type != RoleTypeId.Scp079).ToList().GetRandomValue();
        Owner.Position = target.Position;
        Owner.AddEffect(EffectType.Invisible, 1, 5);
        Owner.AddEffect(EffectType.Ghostly, 1, 5);
        Owner.ApplyGodMode(5);

        Timing.CallDelayed(0.5f, () =>
        {
            Owner.RemoveAbility(this);
            Owner.AddAbility(AbilityType.DUMMY_TELEPORTED);
        });
    }
}
