using RGM.API.Features;

namespace RGM.Modes.Abilities.Rare;

[Ability("하이퍼 바디", "HP가 60% 증가합니다. 자신이 SCP 진영일 경우 효율이 50% 감소합니다.", AbilityCategory.Rare, AbilityType.RARE_HYPERBODY)]

public class HyperBody : Ability
{
    private float _healthMultiplier;
    
    public override void OnEnabled()
    {
        _healthMultiplier = Owner.IsScpRole() ? 1.3f : 1.6f;
        Owner.MaxHealth *= _healthMultiplier;
        Owner.Health *= _healthMultiplier;
    }
}