using System;
using System.Linq;
using Exiled.API.Enums;
using Exiled.Events.EventArgs.Player;
using RGM.API.Features;
using Random = UnityEngine.Random;

namespace RGM.Modes.Abilities.Mythic;

[Ability("조커", """
               사망할 시 부활하며 최대 체력이 4배 증가하고, 10초간 『생존』 효과를 받습니다.
               추가로, 전설(10% 확률로 신화) 능력 5개를 얻습니다.
               """, 
    AbilityCategory.Mythic, AbilityType.MYTHIC_JOKER)]
public class Joker : Ability
{
    public override void OnEnabled()
    {
        Exiled.Events.Handlers.Player.Dying += OnDying;
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Player.Dying -= OnDying;
    }

    private void OnDying(DyingEventArgs ev)
    {
        if (ev.Player != Owner)
            return;

        ev.IsAllowed = false;

        ev.Player.AddEffect(EffectType.Invisible,1,10);
        ev.Player.AddEffect(EffectType.Ghostly,1,10);
        ev.Player.AddEffect(EffectType.MovementBoost,40,10);
        ev.Player.ApplyGodMode(10);

        ev.Player.MaxHealth *= 4;
        ev.Player.Heal(ev.Player.MaxHealth);
        
        for (int i = 0; i < 5; i++)
        {
            var category = Convert.ToByte(Random.Range(1, 101)) <= 10 ? AbilityCategory.Mythic : AbilityCategory.Legend;
            ev.Player.AddAbility(ABattle.Instance.GetRandomAbilities(Owner, category, 1,
                [AbilityType.MYTHIC_JOKER, AbilityType.LEGEND_CATACLYSMGENERATOR,
                AbilityType.LEGEND_REFLECTOR, AbilityType.LEGEND_REPLICATION]).First());
        }

        OnDisabled();
    }
}