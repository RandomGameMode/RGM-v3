using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.Events.EventArgs.Player;
using InventorySystem.Items.Usables.Scp330;
using MEC;
using RGM.API.DataBases;
using RGM.API.Features;
using UnityEngine;

namespace RGM.Modes.Abilities.Unique.Tutorial;

[Ability("SCP 연구자", "SCP 아이템 중 하나를 지급받습니다.", AbilityCategory.Tutorial, AbilityType.TUTORIAL_RESEARCHER)]
public class Researcher : Ability
{
    public override void OnEnabled()
    {

        Item SCPItem = Owner.AddItem(Tools.GetRandomValue(Tools.EnumToList<ItemType>().Where(x => x.ToString().Contains("SCP")).ToList()));
    }

    public override void OnDisabled()
    {
    }
}
