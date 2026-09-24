using System;
using System.Collections.Generic;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.Events.EventArgs.Player;
using MEC;
using PlayerRoles;
using RGM.API.Features;
using Random = UnityEngine.Random;

namespace RGM.Modes.Abilities.Mythic;

[Ability("장미칼", """
                이 명검은 무한으로 발산하는 힘을 가지고 있습니다...
                50% 확률로 진영을 변경하며, 변경 실패 시 대상을 『사망』시킵니다.
                """, AbilityCategory.Mythic, AbilityType.MYTHIC_ROSEHIP)]
public class Rosehip : Ability
{
    private ushort _serial;
    private readonly HashSet<Player> _sideChangedTargets = [];
    private readonly HashSet<Player> _lethalAttackTargets = [];

    public override void OnEnabled()
    {
        Item item = Owner.AddItem(ItemType.SCP1509);
        _serial = item.Serial;

        Exiled.Events.Handlers.Player.ChangedItem += OnChangedItem;
        Exiled.Events.Handlers.Player.Hurting += OnHurting;
    }
    
    private void OnChangedItem(ChangedItemEventArgs ev)
    {
        if (ev.Item?.Serial != _serial)
            return;
            
        ev.Player.AddHint("장미칼", $"<b><color={ABattle.RatingColor["신화"]}>장미칼</color></b> 능력이 있는 <b>SCP-1509</b>입니다!");
    }

    private void OnHurting(HurtingEventArgs ev)
    {
        if (ev.Attacker == null ||
            ev.Attacker.CurrentItem == null ||
            ev.Attacker.CurrentItem.Serial != _serial) return;

        if (_sideChangedTargets.Contains(ev.Player))
        {
            ev.IsAllowed = false;
            return;
        }

        if (_lethalAttackTargets.Remove(ev.Player))
            return;

        ev.IsAllowed = false;
        if (Convert.ToByte(Random.Range(1, 101)) <= 50)
        {
            _sideChangedTargets.Add(ev.Player);
            ev.Player.Role.Set(Tools.EnumToList<RoleTypeId>().GetRandomValue(x => x.GetSide() == ev.Attacker.Role.Type.GetSide()), RoleSpawnFlags.None);
            Timing.CallDelayed(Timing.WaitForOneFrame, () => _sideChangedTargets.Remove(ev.Player));
            return;
        }

        _lethalAttackTargets.Add(ev.Player);
        ApplyInstantKill.Apply(ev.Attacker, ev.Player);
        Timing.CallDelayed(Timing.WaitForOneFrame, () => _lethalAttackTargets.Remove(ev.Player));
    }
}