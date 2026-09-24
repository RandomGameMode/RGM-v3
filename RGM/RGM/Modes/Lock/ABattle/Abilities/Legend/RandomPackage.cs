using System;
using System.Collections.Generic;
using Exiled.API.Extensions;
using MEC;
using Random = UnityEngine.Random;

namespace RGM.Modes.Abilities.Legend;

[Ability("랜덤택배", "30초마다 고가치 아이템 1개를 획득합니다. 10% 확률로 특수 아이템을 획득할 수 있습니다.",
    AbilityCategory.Legend, AbilityType.LEGEND_RANDOMPACKAGE)]
public class RandomPackage : Ability
{
    private readonly List<ItemType> _highvalueitems =
    [
        ItemType.ParticleDisruptor,
        ItemType.Jailbird,
        ItemType.MicroHID,
        ItemType.SCP1509,
        ItemType.SCP1853,
        ItemType.AntiSCP207,
        ItemType.SCP268,
        ItemType.SCP500,
        ItemType.KeycardO5,
        ItemType.SCP1344,
        ItemType.GunLogicer,
        ItemType.GunE11SR,
        ItemType.ArmorHeavy
    ];

    private readonly List<AbilityType> _specials =
    [
        AbilityType.RARE_SPACETRAVEL,
        AbilityType.EPIC_RAMBO,
        AbilityType.EPIC_TERRORISTREMAINS,
        AbilityType.LEGEND_FLASHLIGHT,
        AbilityType.LEGEND_FLAMETHROWER,
        AbilityType.LEGEND_OTHERWORLDLIGHT
    ];

    private CoroutineHandle _randomitem;
    
    public override void OnEnabled()
    {
        _randomitem = Timing.RunCoroutine(RandomItem());
    }

    public override void OnDisabled()
    {
        Timing.KillCoroutines(_randomitem);
    }

    private IEnumerator<float> RandomItem()
    {
        while (true)
        {
            if (Convert.ToByte(Random.Range(1, 101)) <= 10)
            {
                Owner.AddAbility(_specials.GetRandomValue());
            }
            Owner.AddItem(_highvalueitems.GetRandomValue());
            yield return Timing.WaitForSeconds(30f);
        }
    }
}
