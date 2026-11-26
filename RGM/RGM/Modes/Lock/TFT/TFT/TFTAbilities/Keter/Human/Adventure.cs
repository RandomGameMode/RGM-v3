using System;
using Exiled.API.Extensions;
using Exiled.API.Features.Items;
using Exiled.Events.EventArgs.Player;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Enums;
using RGM.API.Features;
using Random = UnityEngine.Random;

namespace RGM.Modes.Lock.TFT.Keter.Human;

[TFTAbility("고귀한 모험", "100 데미지를 가할 때 마다 보상을 획득합니다.",
    TFTAbilityLevel.Keter, TFTAbilityCategory.Human, TFTAbilityPoint.Continuous, TFTAbilityType.Adventure, "🚩")]
public class Adventure : TFTAbility
{
    private float _damage;
    private byte _rand;
    private ushort _itemCount;
    private float _totalHealth;
    private float _totalDamage;
    private ushort _totalSpeed;

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
        ItemType.SCP268,
        ItemType.Jailbird,
        ItemType.MicroHID,
        ItemType.ParticleDisruptor,
        ItemType.GunLogicer,
        ItemType.GunFRMG0
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

        Data.Description =
            $"100 데미지를 가할 때 마다 보상 획득(현재: {_damage} / 100 | Item {_itemCount}개 | HP+ {_totalHealth} | ATK+ {_totalDamage} | SPD+ {_totalSpeed})";

        if (_damage >= 100)
        {
            _damage -= 100;
            _rand = Convert.ToByte(Random.Range(1, 5));
        }

        foreach (var item in Item.List.Where(x => x.IsAmmo)) Owner.AddItem(item.Type, 1);

        switch (_rand)
        {
            case 1:
                do
                {
                    Owner.AddItem(_items.GetRandomValue());
                    _itemCount++;
                } while (Random.Range(1, 101) <= 25);

                break;

            case 2:
                var addHealth = Random.Range(1, 101) <= 40 ? Random.Range(1, 101) <= 25 ? 400 : 150 : 50;
                _totalHealth += addHealth;
                Owner.MaxHealth += addHealth;
                Owner.Heal(Owner.MaxHealth);
                break;

            case 3:
                _totalDamage += Random.Range(1, 101) <= 40 ? Random.Range(1, 101) <= 25 ? 6 : 3 : 1;
                ev.DamageHandler.Damage += _totalDamage;
                break;

            case 4:
                var addSpeed = Convert.ToUInt16(Random.Range(1, 101) <= 40 ? Random.Range(1, 101) <= 25 ? 20 : 12 : 5);
                Owner.AddEffect(EffectType.MovementBoost, addSpeed);
                _totalSpeed += addSpeed;
                break;
        }
    }
}
