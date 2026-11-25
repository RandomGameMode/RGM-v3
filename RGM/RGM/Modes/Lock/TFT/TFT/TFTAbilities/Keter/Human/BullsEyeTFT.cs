using Exiled.API.Features.DamageHandlers;
using Exiled.Events.EventArgs.Player;
using PlayerRoles;

namespace DAONTFT.Core.TFT.Keter.Human;

[TFTAbility("불스아이", "헤드샷 단 한발로 적을 처치할 수 있습니다. 049-2를 제외한 SCP가 상대일 경우, 상대의 약점을 파악하여 최종 데미지가 100% 증가합니다.", 
    TFTAbilityLevel.Keter, TFTAbilityCategory.Human, TFTAbilityPoint.Continuous, TFTAbilityType.Bullseye, "🚩")]
public class BullsEyeTFT : TFTAbility
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
        if (ev.Attacker != Owner || ev.DamageHandler.CustomBase is not FirearmDamageHandler damageHandler)
            return;

        if (damageHandler.Hitbox == HitboxType.Headshot)
            damageHandler.Damage *= 77f;
        
        if (ev.Player.IsScp && ev.Player.Role.Type != RoleTypeId.Scp0492)
            damageHandler.Damage *= 2f;
    }
}
