using RGM.Modes.Sets.AddScp.Scps;

namespace RGM.Modes.Abilities.Unique.Human.Legend;

[Ability("SCP-999", "간지럼 괴물, SCP-999로 변경됩니다.", 
    AbilityCategory.Epic, AbilityType.EPIC_HUMAN_SCP999, RoleAbility.Human)]
public class ChangeScp999 : Ability
{
    public override void OnEnabled() => Scp999.Create(Owner);
}
