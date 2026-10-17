using Exiled.API.Enums;
using Exiled.Events.EventArgs.Player;
using PlayerStatsSystem;
using RGM.Features;

namespace RGM.Modes.Abilities.Unique.Scp049.Epic;

[Ability("역병 저주",
    "상대가 [영웅] 신성방어 능력을 보유 중인 경우, 대상 최대 HP의 55%만큼 관통 고정 피해를 가합니다.",
    AbilityCategory.Epic,
    AbilityType.EPIC_SCP049_PLAGUECURSE,
    RoleAbility.Scp049)]

public class PlagueCurse : Ability
{
    public override void OnEnabled()
    {
        Exiled.Events.Handlers.Player.Hurting += OnHurting;
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Player.Hurting -= OnHurting;
    }
    
    private void OnHurting(HurtingEventArgs ev)
    {
        if (ev.Attacker != Owner || ev.Player == ev.Attacker) return;
        if (ev.DamageHandler.Type != DamageType.Scp049) return;
        if (!ev.Player.HasAbility(AbilityType.EPIC_HOLYPROTECTION)) return;
        if (ApplyFixedDamage.IsApplying) return;
        
        ApplyFixedDamage.Apply(Owner, ev.Player, ev.Player.MaxHealth * 0.55f);
    }
}