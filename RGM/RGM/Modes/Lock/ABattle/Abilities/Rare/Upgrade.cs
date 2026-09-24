using System;
using Exiled.API.Features;
using Scp914;
using Scp914.Processors;

namespace RGM.Modes.Abilities.Rare;

[Ability("강화", "현재 들고 있는 아이템을 강화합니다. (Very Fine 기준)", AbilityCategory.Rare, AbilityType.RARE_UPGRADE)]
public class Upgrade : Ability
{
    public override void OnEnabled()
    {
        try
        {
            var item = Owner.CurrentItem;
            if (item == null || item.Type == ItemType.SCP1344)
                return;

            if (Scp914Upgrader.TryGetProcessor(item.Type, out Scp914ItemProcessor processor))
                processor.UpgradeInventoryItem(Scp914KnobSetting.VeryFine, item.Base);
        }
        catch (Exception e)
        {
            Log.Error($"능력 적용 실패: {e}");
        }
    }

}
