using System;
using Exiled.API.Extensions;
using Exiled.Events.EventArgs.Player;
using MEC;
using static RGM.Variables.Variable;
using Random = UnityEngine.Random;

namespace RGM.Modes.Abilities.Mythic;

[Ability("조커", """
               사망할 시 부활하며 5초 무적, 최대 체력 3~6배, 상대방의 능력 1개를 삭제하며,
               전설(15% 확률로 신화) 능력 4개를 얻습니다.
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

        GodModePlayers.Add(ev.Player);
        
        Timing.CallDelayed(5f, () =>
        {
            if (GodModePlayers.Contains(ev.Player))
                GodModePlayers.Remove(ev.Player);
        });

        ev.Player.MaxHealth = Random.Range(3, 7) * ev.Player.MaxHealth;
        ev.Player.Heal(ev.Player.MaxHealth);
        
        if (ev.Attacker != null)
            ev.Attacker.RemoveAbility(ev.Attacker.GetAbilities().GetRandomValue());
        
        for (int i = 0; i < 4; i++)
        {
            var category = Convert.ToByte(Random.Range(1, 101)) <= 15 ? AbilityCategory.Mythic : AbilityCategory.Legend;
            ev.Player.AddAbility(ABattle.Instance.GetRandomAbilities(Owner, category, 1)[0]);
        }

        OnDisabled();
    }
}