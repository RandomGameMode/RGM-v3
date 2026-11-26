using System.Linq;
using Exiled.API.Extensions;
using RGM.API.Features;

namespace RGM.Modes.Lock.TFT.Euclid.Human;

[TFTAbility("탄약고", "모든 종류의 탄약을 각각 8세트씩 받습니다.", TFTAbilityLevel.Euclid, TFTAbilityCategory.Human, TFTAbilityPoint.Once, TFTAbilityType.AmmoBox, "🔫")]
public class CandyAddict : TFTAbility
{
    public override void OnEnabled()
    {
        foreach (var ammo in Tools.EnumToList<ItemType>().Where(x => x.IsAmmo()))
        {
            for (int i = 0; i < 8; i++)
            {
                Owner.AddItem(ammo);
            }
        }
    }

    public override void OnDisabled()
    {
    }
}
