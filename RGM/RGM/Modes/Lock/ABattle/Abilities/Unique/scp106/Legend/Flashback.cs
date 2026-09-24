using CustomPlayerEffects;
using Exiled.API.Enums;
using Exiled.Events.EventArgs.Player;
using PlayerRoles.PlayableScps.Scp106;
using RGM.API.Features;
using Scp106Role = Exiled.API.Features.Roles.Scp106Role;

namespace RGM.Modes.Abilities.Unique.Scp106.Legend;

[Ability("회상", 
    """
               대상에게 『죽음에 이르는 공격』을 가합니다. 추가로, 자신의 이동 속도가 20% 증가합니다.
               <color=#FF2400>[전용 신화]</color> 회고를 보유 중인 경우 해당 능력은 획득할 수 없습니다.
               """, 
    AbilityCategory.Legend,
    AbilityType.LEGEND_SCP106_FLASHBACK,
    RoleAbility.Scp106)]

public class Flashback : Ability
{
    private bool _isExecuting;

    public override void OnEnabled()
    {
        Owner.AddEffect(EffectType.MovementBoost, 20);
        Exiled.Events.Handlers.Player.Hurting += OnHurting;
    }

    public override void OnDisabled()
    {
        Owner.RemoveEffect(EffectType.MovementBoost, 20);
        Exiled.Events.Handlers.Player.Hurting -= OnHurting;
    }

    private void OnHurting(HurtingEventArgs ev)
    {
        if (Owner.Role is not Scp106Role scp106) return;
        if (ev.Attacker == null || ev.Attacker.ReferenceHub != scp106.Owner.ReferenceHub) return;
        if (ev.DamageHandler.Type != DamageType.Scp106) return;
        if (_isExecuting) return;
        
        // [영웅] 신성방어 능력을 가지고 있다면 최대 HP의 100%만큼 추가 피해
        if (ev.Player.HasAbility(AbilityType.EPIC_HOLYPROTECTION))
            ApplyLethalDamage.Apply(Owner, ev.Player);
        
        // ReceivingEffect 이벤트를 거치지 않아 디버프 면역 능력이 차단할 수 없다.
        if (!ev.Player.TryGetEffect(EffectType.Traumatized, out StatusEffectBase traumatized))
            return;

        traumatized.ServerSetState(1);

        if (!scp106.SubroutineModule.TryGetSubroutine<Scp106Attack>(out var attack))
            return;

        _isExecuting = true;
        try
        {
            attack.ServerShoot();
        }
        finally
        {
            _isExecuting = false;
        }
    }
}