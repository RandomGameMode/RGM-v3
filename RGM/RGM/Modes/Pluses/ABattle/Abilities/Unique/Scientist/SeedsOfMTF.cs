using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.Events.EventArgs.Player;
using InventorySystem.Items.Usables.Scp330;
using MEC;
using RGM.API.Features;
using UnityEngine;

namespace RGM.Modes.Abilities.Unique.Scientist;

[Ability("구미호의 씨앗", "NTF 티켓을 진체 인원 수의 20% 만큼 추가합니다.", AbilityCategory.Scientist, AbilityType.SCIENTIST_SEEDSOFMTF)]
public class SeedsOfMTF : Ability
{
    public override void OnEnabled()
    {
        Respawn.GrantTickets(PlayerRoles.Faction.FoundationStaff, (int)(Player.List.Count() * 0.2));
    }

    public override void OnDisabled()
    {
    }
}
