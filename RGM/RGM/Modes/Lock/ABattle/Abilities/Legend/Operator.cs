using System.Linq;
using Exiled.API.Features.Items;
using Exiled.Events.EventArgs.Player;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.Modules;
using RGM.API.Features;
using RGM.Features;

namespace RGM.Modes.Abilities.Legend;

/*
[Ability("오퍼레이터", 
    """
     강력한 한 방을 날릴 수 있는 리볼버를 지급받습니다. 단, BuckShot Cylinder 장착 시 적용되지 않습니다.
     해당 능력을 가진 리볼버로 조준 사격 시, 『죽음에 이르는 공격』을 가합니다.
     SCP 진영에게는 1236.06의 『관통』 고정 피해를 입힙니다.
     """, 
    AbilityCategory.Legend, AbilityType.LEGEND_OPERATOR)]*/

public class Operator : Ability
{
    private const float FixedDamage = 1236.06f;
    private ushort _revolverSerial;

    public override void OnEnabled()
    {
        Owner.AddItem(ItemType.Ammo44cal, 10);
        
        _revolverSerial = Owner.AddItem(ItemType.GunRevolver).Serial;
        Exiled.Events.Handlers.Player.ChangedItem += OnChangedItem;
        Exiled.Events.Handlers.Player.Hurting += OnHurting;
    }

    private void OnChangedItem(ChangedItemEventArgs ev)
    {
        if (ev.Item?.Serial != _revolverSerial)
            return;

        ev.Player.AddHint("오퍼레이터", $"<b><color={ABattle.RatingColor["전설"]}>오퍼레이터</color></b> 능력이 있는 Revolver 입니다.");
    }
    
    private void OnHurting(HurtingEventArgs ev)
    {
        if (ApplyFixedDamage.IsApplying ||
            ev.Attacker?.CurrentItem?.Serial != _revolverSerial || 
            HasBuckshotCylinder(ev.Attacker.CurrentItem) ||
            !LinearAdsModule.GetAdsTargetForSerial(_revolverSerial) ||
            !HitboxIdentity.IsEnemy(ev.Attacker.ReferenceHub, ev.Player.ReferenceHub)) return;

        ev.IsAllowed = false;

        if (ev.Player.IsScp)
        {
            if (ApplyFixedDamage.Apply(ev.Attacker, ev.Player, FixedDamage)) Owner.ShowHitMarker(1.25f);
        }
        else
        {
            if (ApplyLethalDamage.Apply(ev.Attacker, ev.Player)) Owner.ShowHitMarker(1.5f);
        }
    }

    private static bool HasBuckshotCylinder(Item item) =>
        item.Base is InventorySystem.Items.Firearms.Firearm revolver &&
        revolver.Attachments.Any(attachment =>
            attachment.IsEnabled &&
            attachment.Name == AttachmentName.CylinderMag7);
}