using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using System.Linq;

namespace DAONTFT.Core.TFT.Safe.All;

[TFTAbility("봄버맨", "랜덤한 유저의 위치에 고폭 수류탄을 투척합니다.", TFTAbilityLevel.Safe, TFTAbilityCategory.All, TFTAbilityPoint.Once, TFTAbilityType.BomberMan, "💣")]
public class BomberMan : TFTAbility
{
    public override void OnEnabled()
    {
        var g = (ExplosiveGrenade)Item.Create(ItemType.GrenadeHE, Owner);
        g.FuseTime = 3f;
        g.SpawnActive(Player.List.Where(x => x.IsAlive && x.Role.Team != Owner.Role.Team && Owner != x).GetRandomValue().Position, Owner);
    }
}
