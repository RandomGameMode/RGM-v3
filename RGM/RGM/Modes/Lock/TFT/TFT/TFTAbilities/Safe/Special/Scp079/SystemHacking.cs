using Exiled.API.Enums;
using Exiled.API.Features;
using PlayerRoles;
using System.Linq;
using RGM.API.Features;

namespace DAONTFT.Core.TFT.Safe.Scp079;

[TFTAbility("시스템 해킹", "아군 SCP의 투명도를 2분 간 40% 증가시킵니다.", TFTAbilityLevel.Safe, TFTAbilityCategory.Scp079, TFTAbilityPoint.Once, TFTAbilityType.SystemHacking, "💀")]
public class SystemHacking : TFTAbility
{
    public override void OnEnabled()
    {
        foreach (var scp in Player.List.Where(x => x.IsScp && x.Role.Type != RoleTypeId.Scp079))
        {
            scp.AddEffect(EffectType.Fade, 102, 120);
        }
    }
}
