using Scp914;
using Scp914.Processors;

namespace DAONTFT.Core.TFT.Safe.Human;

[TFTAbility("간이 914", "들고 있는 아이템을 강화합니다. (매우 고움 기준)", TFTAbilityLevel.Safe, TFTAbilityCategory.Human, TFTAbilityPoint.Once, TFTAbilityType.InventoryUpgrade, "⚒️")]
public class InventoryUpgrade : TFTAbility
{
    public override void OnEnabled()
    {
        if (Owner.CurrentItem != null)
        {
            if (Scp914Upgrader.TryGetProcessor(Owner.CurrentItem.Type, out Scp914ItemProcessor processor))
                processor.UpgradeInventoryItem(Scp914KnobSetting.VeryFine, Owner.CurrentItem.Base);
        }
    }
}
