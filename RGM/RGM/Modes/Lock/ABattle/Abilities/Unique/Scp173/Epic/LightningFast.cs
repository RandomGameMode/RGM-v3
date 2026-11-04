using Exiled.Events.EventArgs.Scp173;

namespace RGM.Modes.Abilities.Unique.Scp173.Epic;

[Ability("전광석화", 
    "SCP-173의 기본 텔레포트 쿨타임이 고정 0.5초로 설정됩니다.", 
    AbilityCategory.Epic, AbilityType.EPIC_SCP173_LIGHTNINGFAST, RoleAbility.Scp173, isUnique:true)]

public class LightningFast : Ability
{
    public override void OnEnabled()
    {
        Exiled.Events.Handlers.Scp173.Blinking += OnBlinking;
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Scp173.Blinking -= OnBlinking;
    }
    
    private void OnBlinking(BlinkingEventArgs ev)
    {
        if (ev.Player == null || ev.Player != Owner) return;
        
        ev.BlinkCooldown = 0.5f;
    }
}