using Exiled.Events.EventArgs.Scp079;

namespace RGM.Modes.Abilities.Unique.Scp079.Rare;

[Ability("기동타격대", "테슬라로 소모되는 에너지가 20 감소합니다.", 
    AbilityCategory.Rare, AbilityType.RARE_SCP079_MOBILESTRIKEFORCE, RoleAbility.Scp079)]
public class MobileStrikeForce : Ability
{
    public override void OnEnabled()
    {
        Exiled.Events.Handlers.Scp079.InteractingTesla += OnInteractingTesla;
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Scp079.InteractingTesla -= OnInteractingTesla;
    }

    private void OnInteractingTesla(InteractingTeslaEventArgs ev)
    {
        if (Owner != ev.Player)
            return;

        ev.AuxiliaryPowerCost -= 20;
    }
}
