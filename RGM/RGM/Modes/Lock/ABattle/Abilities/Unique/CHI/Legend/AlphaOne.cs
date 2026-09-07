using System.Collections.Generic;
using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Features;
using MEC;
using PlayerRoles;

namespace RGM.Modes.Abilities.Unique.CHI.Legend;

[Ability("ALPHA-1, Red Right Hand", """
                                    재단을 배반한 단체, ALPHA-1 지원을 호출합니다.
                                    ALPHA-1 대원은 기본적으로 강화된 능력치를 가집니다.
                                    """, 
    AbilityCategory.Legend, AbilityType.LEGEND_CHI_ALPHAONE, RoleAbility.CHI)]

public class AlphaOne : Ability
{
    private const float RoleChangeDelay = 0.1f;
    private CoroutineHandle _summonCoroutine;
    
    public override void OnEnabled()
    {
        _summonCoroutine = Timing.RunCoroutine(SummonAlphaOneMembers());
    }

    public override void OnDisabled()
    {
        Timing.KillCoroutines(_summonCoroutine);
    }
    
    private static IEnumerator<float> SummonAlphaOneMembers()
    {
        List<Player> targets = Player.List.Where(player => player.IsDead).ToList();

        foreach (Player target in targets)
        {
            if (!target.IsDead)
                continue;

            target.Role.Set(RoleTypeId.ChaosRepressor, SpawnReason.ForceClass, RoleSpawnFlags.All);
            yield return Timing.WaitForSeconds(RoleChangeDelay);

            if (target.IsAlive && target.Role.Type == RoleTypeId.ChaosRepressor)
                target.AddAbility(AbilityType.DUMMY_ALPHAONEMENBER);
        }
    }
}