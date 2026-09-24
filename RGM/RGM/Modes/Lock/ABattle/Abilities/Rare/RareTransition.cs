namespace RGM.Modes.Abilities.Rare;

[Ability("하급 변이", """
                  다음 능력 선택창에서 <color=#FF00FF>영웅</color> 능력 등장 확률이 25%로 조정됩니다.
                  추가 모드 [잔칫상] 활성화 시, 확률이 추가로 15%p 증가합니다.
                  """,
    AbilityCategory.Rare, AbilityType.RARE_TRANSITION, RoleAbility.None, true)]
public class RareTransition : Ability;
