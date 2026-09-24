namespace RGM.Modes.Abilities.Epic;

[Ability("럭키비키", "워크스테이션 이용 기록이 초기화됩니다.",
    AbilityCategory.Epic, AbilityType.EPIC_LUCKYVIKEY, _79Allowed:true)]
public class LuckyVicky : Ability
{
    public override void OnEnabled() => ABattle.Instance.PlayerWorkstations[Owner].Clear();
}
