using MEC;

namespace RGM.Modes.Abilities.Dummy;

[Ability("U.N.G.O.C 대원", "U.N.G.O.C 대원입니다. 기본적으로 강화된 능력치를 가집니다.", 
    AbilityCategory.Dummy, AbilityType.DUMMY_GOCMEMBER)]

public class GOCMember : Ability
{
    private const float AddHealth = 140f;
    public override void OnEnabled()
    {
        Timing.CallDelayed(0.1f, () =>
        {
            Owner.MaxHealth += AddHealth;
            Owner.Health += AddHealth;
            
            Owner.AddAbility(AbilityType.EPIC_FALLENKINGSSWORD);
            Owner.AddAbility(AbilityType.EPIC_CONTEXPERT);
            Owner.AddAbility(AbilityType.EPIC_TURTLE);
            Owner.AddAbility(AbilityType.EPIC_HOLYPROTECTION);
        });

    }
}