using System.Linq;
using Exiled.API.Extensions;
using RGM.API.DataBases;
using RGM.API.Features;
using UnityEngine;

namespace RGM.Modes.Abilities.Rare;

[Ability("수집가", "랜덤한 SCP 아이템을 2개 획득합니다. 20% 확률로 2개를 추가로 획득합니다.", AbilityCategory.Rare, AbilityType.RARE_COLLECTOR)]
public class Collector : Ability
{
    public override void OnEnabled()
    {
        var scpItems = Tools.EnumToList<ItemType>()
            .Where(x => x.ToString().Contains("SCP") && !Datas.ExceptItems.Contains(x))
            .ToList();

        int itemCount = Random.Range(1, 101) <= 20 ? 4 : 2;
        for (int i = 0; i < itemCount; i++)
        {
            Owner.AddItem(scpItems.GetRandomValue());
        }
    }
}
