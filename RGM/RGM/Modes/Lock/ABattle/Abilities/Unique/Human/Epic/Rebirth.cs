using System;
using Exiled.API.Features;
using MEC;
using PlayerRoles;
using Random = UnityEngine.Random;

namespace RGM.Modes.Abilities.Unique.Human.Epic;

/*[Ability("환생", """
               즉시 본인 진영의 최초 스폰 지점으로 이동하며, 워크스테이션 이용 기록을 초기화합니다.
               환생 시, [영웅] 매드 사이언티스트를 발동한 것으로 간주합니다.
               """,
    AbilityCategory.Epic, AbilityType.EPIC_HUMAN_REBIRTH, RoleAbility.Human)]*/
public class Rebirth : Ability
{
    public override void OnEnabled()
    {
        RoleTypeId roleId = Owner.Role.Type;

        Timing.CallDelayed(0.1f, () =>
        {
            Owner.Role.Set(roleId,
                Owner.Role.Type == RoleTypeId.Tutorial ? RoleSpawnFlags.None : RoleSpawnFlags.UseSpawnpoint);

            Owner.AddAbility(AbilityType.EPIC_LUCKYVIKEY);

            Timing.CallDelayed(Timing.WaitForOneFrame, () =>
            {
                for (int i = 0; i < 9; i++)
                {
                    try
                    {
                        var rand = Convert.ToInt16(Random.Range(1, 501));
                        switch (rand)
                        {
                            case 1: // 0.20%
                                Owner.AddAbility(ABattle.Instance.GetRandomAbilities(Owner, AbilityCategory.Mythic, 1)[0]);
                                break;

                            case <= 9: // 1.80%
                                Owner.AddAbility(ABattle.Instance.GetRandomAbilities(Owner, AbilityCategory.Legend, 1)[0]);
                                break;

                            case <= 75: // 15.0%
                                Owner.AddAbility(ABattle.Instance.GetRandomAbilities(Owner, AbilityCategory.Epic, 1)[0]);
                                break;

                            case <= 175: // 35.0%
                                Owner.AddAbility(ABattle.Instance.GetRandomAbilities(Owner, AbilityCategory.Rare, 1)[0]);
                                break;

                            default: // 48.0%
                                Owner.AddAbility(ABattle.Instance.GetRandomAbilities(Owner, AbilityCategory.Normal, 1)[0]);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Failed to add ability to Mad Scientist: {ex}");
                    }
                }
            });
        });
        
        Timing.CallDelayed(1f, () =>
        {
            Owner.RemoveAbility(this);
            Owner.AddAbility(AbilityType.DUMMY_REBIRTHCOMPLETE);
        });
    }
}