using System.Collections.Generic;
using Exiled.API.Extensions;
using Exiled.API.Features.Items;
using RGM.API.Features;

namespace RGM.Modes.Abilities.Epic;

[Ability("랜덤상자", "랜덤하지만 좋은 아이템을 3개 지급받습니다.", AbilityCategory.Epic, AbilityType.EPIC_RANDOMCHEST)]
public class RandomChest : Ability
{
    private readonly List<ItemType> _randomChest =
    [
        ItemType.ParticleDisruptor,
        ItemType.Jailbird,
        ItemType.MicroHID,
        ItemType.SCP018,
        ItemType.SCP1576,
        ItemType.SCP2176,
        ItemType.SCP207,
        ItemType.AntiSCP207,
        ItemType.SCP268,
        ItemType.SCP500,
        ItemType.KeycardO5,
        ItemType.SCP1344
    ];
    
    public override void OnEnabled()
    {
        for (int i = 1; i < 4; i++)
        {
            Owner.AddItem(_randomChest.GetRandomValue());
        }
    }
}
