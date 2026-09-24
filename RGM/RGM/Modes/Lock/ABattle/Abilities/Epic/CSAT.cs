using System;
using MEC;
using RGM.API.Features;
using Random = UnityEngine.Random;

namespace RGM.Modes.Abilities.Epic;

[Ability("대학수학능력시험", """
                     20% 확률로 영웅(15% 확률로 전설) 능력을 3개 더 얻습니다.
                     추가 모드 [잔칫상] 활성화 시, 확률이 추가로 8%p 증가합니다.
                     """,
    AbilityCategory.Epic, AbilityType.EPIC_CSAT, RoleAbility.None, true)]
public class CSAT : Ability
{
    public override void OnEnabled()
    {
        Owner.AddHint("대학수학능력시험", "과연 결과는..?");

        Timing.CallDelayed(3.1f, () =>
        {
            if (!Owner.IsAlive) return;
            if (Convert.ToByte(Random.Range(1, 101)) <= 4 * Owner.AbilityCount(AbilityType.NORMAL_STUDY) + 
                (ABattle.CurrentExtraModes.Contains("잔칫상") ? 28 : 20))
            {
                Owner.AddHint("대학수학능력시험 1등급", "<b>능력을 3개 더 얻었습니다!</b>");

                for (int i = 0; i < (Owner.HasAbility(AbilityType.SYNERGY_BRILLIANTMIND) ? 6 : 3); i++) {
                    var category = Convert.ToByte(Random.Range(1, 101)) <= 15 
                        ? Convert.ToByte(Random.Range(1, 101)) <= 50 && Owner.HasAbility(AbilityType.SYNERGY_BRILLIANTMIND)
                            ? AbilityCategory.Mythic : AbilityCategory.Legend : AbilityCategory.Epic;
                    Owner.AddAbility(ABattle.Instance.GetRandomAbilities(Owner, category, 1)[0]);
                }
                Owner.AddAbility(AbilityType.DUMMY_CSATSUCCESS);
            }
            else
            {
                Owner.AddHint("대학수학능력시험 9등급", "다음 기회에..");
                Owner.AddAbility(AbilityType.DUMMY_CSATFAIL);
            };
        });
    }
}