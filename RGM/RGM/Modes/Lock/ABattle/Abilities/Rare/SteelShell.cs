using Exiled.API.Enums;
using RGM.API.Features;

namespace RGM.Modes.Abilities.Rare;

[Ability("강철 껍질", """
                  데미지 경감 효과가 5%p 추가됩니다.
                  자신이 SCP 진영일 경우 효율이 40% 감소합니다.
                  """, 
    AbilityCategory.Rare, AbilityType.RARE_STEELSHELL)]
public class SteelShell : Ability
{
    public override void OnEnabled()
    {
        var value = Owner.IsScpRole() ? 6 : 10;
        Owner.AddEffect(EffectType.DamageReduction, value);
    }
}
