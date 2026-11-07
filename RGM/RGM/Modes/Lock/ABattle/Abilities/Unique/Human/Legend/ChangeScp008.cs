using RGM.Modes.Sets.AddScp.Scps;

namespace RGM.Modes.Abilities.Unique.Human.Legend;

[Ability("SCP-008", "좀비 전염병, SCP-008로 변경됩니다.", 
    AbilityCategory.Legend, AbilityType.LEGEND_HUMAN_SCP008, RoleAbility.Human)]
public class ChangeScp008 : Ability
{
    public override void OnEnabled() => Scp008.Create(Owner);
}
