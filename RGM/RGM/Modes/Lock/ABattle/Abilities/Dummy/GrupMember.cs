using MEC;

namespace RGM.Modes.Abilities.Dummy;

[Ability("GRU-P 대원", "GRU-P 대원입니다. 기본적으로 강화된 능력치를 가집니다.", 
    AbilityCategory.Dummy, AbilityType.DUMMY_GRUPMENBER)]

public class GrupMember : Ability
{
    private const float AddHealth = 120f;
    public override void OnEnabled()
    {
        Timing.CallDelayed(0.1f, () =>
        {
            Owner.MaxHealth += AddHealth;
            Owner.Health += AddHealth;
            
            Owner.AddAbility(AbilityType.RARE_BULLSEYE);
            Owner.AddAbility(AbilityType.RARE_COLLECTOR);
            Owner.AddAbility(AbilityType.EPIC_SHARPEYES);
            Owner.AddAbility(AbilityType.EPIC_TURTLE);
            Owner.AddAbility(AbilityType.EPIC_HOLYPROTECTION);
        });

    }
}