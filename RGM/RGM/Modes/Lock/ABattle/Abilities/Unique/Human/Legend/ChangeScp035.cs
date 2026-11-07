using RGM.Modes.Sets.AddScp.Scps;

namespace RGM.Modes.Abilities.Unique.Human.Legend;

[Ability("SCP-035", "빙의 가면, SCP-035로 변경됩니다.",
    AbilityCategory.Legend, AbilityType.LEGEND_HUMAN_SCP035, RoleAbility.Human)]
public class ChangeScp035 : Ability
{
    public override void OnEnabled() => Scp035.Create(Owner);
}
