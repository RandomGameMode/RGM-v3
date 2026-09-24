using System.Linq;
using Exiled.API.Extensions;
using Exiled.API.Features.Items;
using RGM.API.Features;

namespace RGM.Modes.Abilities.Rare;

[Ability("봄버맨", "랜덤한 유저의 위치에 고폭 수류탄을 투척합니다.", AbilityCategory.Rare, AbilityType.RARE_BOMBERMAN, RoleAbility.None, true)]
public class BomberMan : Ability
{
    public override void OnEnabled()
    {
        var g = (ExplosiveGrenade)Item.Create(ItemType.GrenadeHE, Owner);
        g.FuseTime = 2f;
        g.SpawnActive(PlayerManager.List.ToList().Where(x => x.IsAlive && x.Role.Team != Owner.Role.Team && Owner != x).ToList().GetRandomValue().Position, Owner);
    }
}
