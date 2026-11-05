using Exiled.Events.EventArgs.Scp173;

namespace RGM.Modes.Abilities.Unique.Scp173.Epic;

[Ability("전광석화",
    "SCP-173의 기본 텔레포트 쿨타임이 50% 감소합니다.",
    AbilityCategory.Epic, AbilityType.EPIC_SCP173_LIGHTNINGFAST, RoleAbility.Scp173, isUnique: true)]
public class LightningFast : Ability
{
    private bool _isEnabled;

    public override void OnEnabled()
    {
        _isEnabled = true;
        Exiled.Events.Handlers.Scp173.Blinking += OnBlinking;
    }

    public override void OnDisabled()
    {
        _isEnabled = false;
        Exiled.Events.Handlers.Scp173.Blinking -= OnBlinking;
    }

    private void OnBlinking(BlinkingEventArgs ev)
    {
        if (ev.Player != Owner || !ev.IsAllowed) return;

        if (_isEnabled)
            ev.BlinkCooldown = 1.5f;
    }
}