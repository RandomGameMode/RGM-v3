using System;
using Exiled.API.Features;
using Scp914.Processors;
using Scp914;

namespace RGM.Modes.Abilities.Normal;

[Ability("교환", "현재 들고 있는 아이템을 강화합니다. (1:1 기준)", AbilityCategory.Normal, AbilityType.NORMAL_EXCHANGE)]
public class Exchange : Ability
{
    public override void OnEnabled()
    {
        try
        {
            var item = Owner.CurrentItem;
            if (item == null || item.Type == ItemType.SCP1344)
                return;

            if (Scp914Upgrader.TryGetProcessor(item.Type, out Scp914ItemProcessor processor))
                processor.UpgradeInventoryItem(Scp914KnobSetting.OneToOne, item.Base);
        }
        catch (Exception e)
        {
            Log.Error($"능력 적용 실패: {e}");
        }
    }
}
