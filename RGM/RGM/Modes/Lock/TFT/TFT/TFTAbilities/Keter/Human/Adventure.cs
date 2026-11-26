using Exiled.API.Extensions;
using Exiled.API.Features.Items;
using Exiled.API.Features.Pickups;
using Exiled.Events.EventArgs.Player;
using System.Collections.Generic;
using System.Linq;

namespace RGM.Modes.Lock.TFT.Keter.Human;

[TFTAbility("고귀한 모험", "500 데미지를 가할 때 마다 보상을 획득합니다.",
    TFTAbilityLevel.Keter, TFTAbilityCategory.Human, TFTAbilityPoint.Continuous, TFTAbilityType.Adventure, "🚩")]
public class Adventure : TFTAbility
{
    private float _damage;

    private readonly List<ItemType> _weapons =
    [
        ItemType.Jailbird,
        ItemType.MicroHID,
        ItemType.ParticleDisruptor,
        ItemType.GunLogicer,
        ItemType.GunFRMG0
    ];

    private readonly List<ItemType> _items =
    [
        ItemType.ArmorHeavy,
        ItemType.SCP500,
        ItemType.GrenadeFlash,
        ItemType.GrenadeHE,
        ItemType.KeycardO5,
        ItemType.AntiSCP207,
        ItemType.SCP207,
        ItemType.SCP1853,
        ItemType.SCP1344,
        ItemType.SCP268
    ];

    public override void OnEnabled()
    {
        Exiled.Events.Handlers.Player.Hurting += OnHurting;
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Player.Hurting -= OnHurting;
    }

    private void OnHurting(HurtingEventArgs ev)
    {
        if (ev.Attacker == null || ev.Attacker != Owner) return;
        _damage += ev.Amount;

        Data.Description = $"500 데미지를 가할 때 마다 보상을 획득합니다. (현재: {_damage} / 500)";

        if (_damage >= 500) _damage -= 500;

        Pickup.CreateAndSpawn(_weapons.GetRandomValue(), Owner.Position);

        for (var i = 0; i < 5; i++) Pickup.CreateAndSpawn(_items.GetRandomValue(), Owner.Position);

        foreach (var item in Item.List.Where(x => x.IsAmmo)) Owner.AddItem(item.Type, 3);
    }
}
