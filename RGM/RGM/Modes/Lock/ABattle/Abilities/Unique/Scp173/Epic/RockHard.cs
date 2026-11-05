using Exiled.Events.EventArgs.Player;

namespace RGM.Modes.Abilities.Unique.Scp173.Epic;

[Ability("경화", "받는 데미지가 55% 감소합니다.",
    AbilityCategory.Epic, AbilityType.EPIC_SCP173_ROCKHARD, RoleAbility.Scp173, isUnique:true)]

public class RockHard : Ability
{
    public override void OnEnabled()
    {
        Exiled.Events.Handlers.Player.Hurt += OnHurt;
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Player.Hurt -= OnHurt;
    }

    private void OnHurt(HurtEventArgs ev)
    {
        if (ev.Player != Owner || ev.Attacker == null) return;
        
        ev.DamageHandler.Damage *= 0.45f;
    }
}