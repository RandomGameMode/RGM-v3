using System;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.Events.EventArgs.Player;
using MEC;
using RGM.API.Features;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PlayerRoles;
using Random = UnityEngine.Random;

namespace RGM.Modes.Abilities.Mythic;

[Ability("BALLISTA EM-3",
    """
    10초마다 탄약이 하나 추가되고, 90% 확률로 능력을 삭제하며, 벽을 관통하고, 1300 데미지를 입히는 입자 분열기를 받습니다.
    능력 삭제 시도에 실패하거나 능력이 없는 대상을 공격 시, 『죽음에 이르는 공격』을 가합니다.
    추가로, [영웅] 투시를 받습니다.
    """,
    AbilityCategory.Mythic, AbilityType.MYTHIC_BALLISTAEM3)]
public class BallistaEM3 : Ability
{
    private readonly Mutex _mutex = new();
    private ushort _serial;
    private bool _isActive;
    private const float Damage = 1300 * 2.5f;

    public override void OnEnabled()
    {
        Owner.AddAbility(AbilityType.EPIC_SCP1344);

        var item = Owner.AddItem(ItemType.ParticleDisruptor);
        _serial = item.Serial;

        Exiled.Events.Handlers.Player.ChangedItem += OnChangedItem;
        Exiled.Events.Handlers.Player.Hurting += OnHurting;

        Timing.RunCoroutine(PlanBAmmo());
    }
    
    private IEnumerator<float> PlanBAmmo()
    {
        while (true)
        {
            yield return Timing.WaitForSeconds(10f);

            if (Owner == null || !Owner.IsAlive || Owner.CurrentItem == null)
                continue;

            if (Item.Get(_serial) is not Firearm firearm)
                continue;

            if (firearm.MagazineAmmo < firearm.MaxMagazineAmmo)
                firearm.MagazineAmmo += 1;
        }
    }

    private void OnChangedItem(ChangedItemEventArgs ev)
    {
        if (ev.Item == null || ev.Player == null) return;
        if (ev.Item.Serial != _serial)
            return;
        
        ev.Player.AddHint("BALLISTA EM-3",  $"<b><color={ABattle.RatingColor["신화"]}>BALLISTA EM-3</color></b> 능력이 있는 <b>입자 분열기</b>입니다!");
    }
    
    private void OnHurting(HurtingEventArgs ev)
    {
        if (ev.Player == null 
            || ev.Attacker == null 
            || ev.DamageHandler == null
            || ev.Attacker.CurrentItem == null
            || !_mutex.WaitOne(2000)) return;
        
        if (_isActive) return;
        if (ev.Attacker != Owner || ev.Attacker.CurrentItem.Serial != _serial) return;
        
        if (!Tools.TryGetLookPlayers(ev.Attacker, 75f, out List<Player> players, out _)) return;

        Log.Info(players.Count);
        
        foreach (var player in players.Where(player => HitboxIdentity.IsEnemy(ev.Attacker.ReferenceHub,
                         player.ReferenceHub))
                     .Where(player => player.Role != RoleTypeId.Spectator))
        {
            if (player.IsNPC)
            {
                Hit(player.ReferenceHub, ev.Attacker.ReferenceHub, 1.5f);
                continue;
            }          
            
            if (!ABattle.Instance.PlayerAbilities.TryGetValue(player, out var ability) || ability.Count <= 0)
                Hit(player.ReferenceHub, ev.Attacker.ReferenceHub);
            
            else if (Convert.ToByte(Random.Range(1, 101)) <= 10)
            {
                Hit(player.ReferenceHub, ev.Attacker.ReferenceHub);
                
                player.DisableAllEffects();
                player.RemoveAllAbilities();
                    
                if (ABattle.Instance.PlayerWorkstations.TryGetValue(player, out var workstations))
                    workstations.Clear();
            }
            else
                Hit(player.ReferenceHub, ev.Attacker.ReferenceHub);

            Timing.CallDelayed(1.5f, _mutex.ReleaseMutex);
        }
    }

    private bool _isHitActive; 
    private void Hit(ReferenceHub victim, ReferenceHub attacker, float size = 3f)
    {
        if (!Player.TryGet(victim, out var player)) return;
        if (!Player.TryGet(attacker, out var attack)) return;
        if (_isHitActive)
        {
            Timing.CallDelayed(0.1f, () => Hit(victim, attacker, size));
            return;
        }
        
        _isHitActive = true;
        player.Hit(attack, Damage);
        Hitmarker.SendHitmarkerDirectly(attacker, size);
        _isHitActive = false;
    }
}