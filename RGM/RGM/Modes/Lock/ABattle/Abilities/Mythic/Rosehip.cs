using System;
using System.Collections.Generic;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.Events.EventArgs.Player;
using MEC;
using PlayerRoles;
using PlayerStatsSystem;
using RGM.API.Features;
using Random = UnityEngine.Random;

namespace RGM.Modes.Abilities.Mythic;

[Ability("장미칼", """
                이 명검은 무한으로 발산하는 힘을 가지고 있습니다...
                50% 확률로 진영을 변경하며, 변경 실패 시 대상을 『사망』시킵니다.
                """, AbilityCategory.Mythic, AbilityType.MYTHIC_ROSEHIP)]
public class Rosehip : Ability
{
    private const int SideChangeChance = 50;

    // 한 번의 공격(스윙)에서 같은 대상에게 여러 번 Hurting 이 발생해도 확률 판정은 한 번만 하도록 하는 시간.
    // 서버 TPS 와 무관하게 동작해야 하므로 프레임이 아닌 실제 시간(초)을 사용한다.
    private const float ResolveLockDuration = 0.5f;

    private ushort _serial;
    private readonly Dictionary<Player, float> _resolvedUntil = [];

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
        if (ev.Attacker != Owner ||
            ev.Player == null ||
            ev.Player == ev.Attacker ||
            ev.Attacker.CurrentItem == null ||
            ev.Attacker.CurrentItem.Serial != _serial) return;

        // SCP-1509 의 근접 공격만 판정한다.
        // 장미칼을 들고 있는 동안 발생하는 다른 모든 피해(폭발, 지속 피해, 다른 능력의 피해 등)와
        // ApplyInstantKill 이 내부적으로 일으키는 Crushed 피해에도 확률 판정이 반복되던 문제를 막는다.
        if (ev.DamageHandler.Type != DamageType.Scp1509) return;

        // 이미 다른 효과로 취소된 공격이나 아군에 대한 공격은 판정하지 않는다.
        if (!ev.IsAllowed ||
            (!HitboxIdentity.IsEnemy(ev.Attacker.ReferenceHub, ev.Player.ReferenceHub) && !Server.FriendlyFire))
            return;

        float now = UnityEngine.Time.realtimeSinceStartup;

        // 이미 판정이 끝난 대상에 대한 추가 타격은 판정 없이 막는다.
        if (_resolvedUntil.TryGetValue(ev.Player, out float until) && now < until)
        {
            ev.IsAllowed = false;
            return;
        }

        PruneResolved(now);
        _resolvedUntil[ev.Player] = now + ResolveLockDuration;

        ev.IsAllowed = false;
        if (Random.Range(0, 100) < SideChangeChance)
        {
            Side attackerSide = ev.Attacker.Role.Type.GetSide();
            ev.Player.Role.Set(
                Tools.EnumToList<RoleTypeId>().GetRandomValue(x => x.GetSide() == attackerSide),
                RoleSpawnFlags.None);
            return;
        }

        ApplyLethalDamage.Apply(ev.Attacker, ev.Player, DeathTranslations.Crushed);
        ev.Player.Role.Set(RoleTypeId.Spectator);
    }

    private void PruneResolved(float now)
    {
        if (_resolvedUntil.Count == 0)
            return;

        List<Player> expired = null;
        foreach (KeyValuePair<Player, float> pair in _resolvedUntil)
        {
            if (pair.Value > now)
                continue;

            expired ??= [];
            expired.Add(pair.Key);
        }

        if (expired == null)
            return;

        foreach (Player player in expired)
            _resolvedUntil.Remove(player);
    }
}