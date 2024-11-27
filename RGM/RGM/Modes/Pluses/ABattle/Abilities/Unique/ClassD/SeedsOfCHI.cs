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

namespace RGM.Modes.Abilities.Unique.ClassD;

[Ability("반란의 씨앗", "카오스 티켓을 전체 인원 수의 20% 만큼 추가합니다.", AbilityCategory.ClassD, AbilityType.CLASSD_SEEDSOFCHI)]
public class SeedsOfCHI : Ability
{
    public override void OnEnabled()
    {
        Respawn.GrantTickets(PlayerRoles.Faction.FoundationEnemy, (int)(Player.List.Count() * 0.2));
    }

    public override void OnDisabled()
    {
    }
}
