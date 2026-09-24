using Exiled.API.Enums;
using Exiled.API.Features.Items;
using Exiled.Events.EventArgs.Player;
using MEC;
using RGM.API.Features;

namespace RGM.Modes.Abilities.Mythic;

[Ability("광전사", "공격에 성공했을 때 누적된 차징이 초기화되는 제일버드를 받고, 폭발 데미지에 면역이 됩니다.",
    AbilityCategory.Mythic, AbilityType.MYTHIC_WARGOD)]
public class WarGod : Ability
{
    private ushort _lightWarriorSerial;
    private float _lightWarriorCooldown;

    public override void OnEnabled()
    {
        Item jb = Owner.AddItem(ItemType.Jailbird);
        _lightWarriorSerial = jb.Serial;

        Exiled.Events.Handlers.Player.ChangedItem += OnChangedItem;
        Exiled.Events.Handlers.Player.Hurting += OnHurting;
    }

    private void OnChangedItem(ChangedItemEventArgs ev)
    {
        if (ev.Item == null || ev.Player == null) return;
        if (_lightWarriorSerial != ev.Player.CurrentItem.Serial) return;
        if (_lightWarriorSerial == ev.Item.Serial)
            ev.Player.AddHint("광전사", $"<b><color={ABattle.RatingColor["신화"]}>광전사</color></b> 능력이 있는 <b>제일버드</b>입니다!");
        if (ev.Item?.Serial != _lightWarriorSerial)
            return;
        
        ev.Player.AddHint("광전사", $"<b><color={ABattle.RatingColor["신화"]}>광전사</color></b> 능력이 있는 <b>제일버드</b>입니다!");
    }

    private void OnHurting(HurtingEventArgs ev)
    {
        if (ev.Attacker != null && 
            ev.Player != ev.Attacker &&
            ev.Attacker.CurrentItem != null && 
            _lightWarriorSerial == ev.Attacker.CurrentItem.Serial)
        {
            if (_lightWarriorCooldown <= 0)
            {
                _lightWarriorCooldown = 0.5f;

                if (HitboxIdentity.IsEnemy(ev.Attacker.ReferenceHub, ev.Player.ReferenceHub))
                {
                    if (ev.Attacker.CurrentItem is Jailbird jailbird)
                    {
                        jailbird.TotalCharges = 0;
                        jailbird.TotalDamageDealt = 0;
                    } 

                    var g = (ExplosiveGrenade)Item.Create(ItemType.GrenadeHE, ev.Attacker);
                    g.FuseTime = 0.1f;
                    g.SpawnActive(ev.Attacker.Position, ev.Attacker);
                }

                Timing.CallDelayed(0.5f, () =>
                {
                    _lightWarriorCooldown = 0;
                });
            }
        }

        if (ev.Player != Owner) return;
        if (ev.DamageHandler.Type == DamageType.Explosion)
            ev.IsAllowed = false;
    }
}