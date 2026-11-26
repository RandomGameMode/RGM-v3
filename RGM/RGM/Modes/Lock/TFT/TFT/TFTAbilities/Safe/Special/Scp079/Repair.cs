using Exiled.API.Features.Doors;

namespace DAONTFT.Core.TFT.Safe.Scp079;

[TFTAbility("수리 업체", "부서진 모든 문이 복구됩니다.", TFTAbilityLevel.Safe, TFTAbilityCategory.Scp079, TFTAbilityPoint.Once, TFTAbilityType.Repair, "🔧")]
public class Repair : TFTAbility
{
    public override void OnEnabled()
    {
        foreach (var door in Door.List)
        {
            if (door is BreakableDoor breakableDoor)
                breakableDoor.Repair();
        }
    }
}
