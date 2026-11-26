using Exiled.Events.EventArgs.Player;
using MEC;
using PlayerRoles;

namespace RGM.Modes.Lock.TFT.Keter.Scp049;

[TFTAbility("명의", "적을 처치하면 그 대상을 049-2로 만들고, 대상의 최대 HP를 50% 증가시킵니다.", TFTAbilityLevel.Keter, TFTAbilityCategory.Scp049, TFTAbilityPoint.Continuous, TFTAbilityType.BestDoctor, "💉")]
public class BestDoctor : TFTAbility
{
    public override void OnEnabled()
    {
        Exiled.Events.Handlers.Player.Died += OnDied;
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Player.Died -= OnDied;
    }

    private void OnDied(DiedEventArgs ev)
    {
        if (ev.Attacker != Owner || ev.Player == Owner)
            return;

        Timing.CallDelayed(0.1f, () =>
        {
            ev.Player.Role.Set(RoleTypeId.Scp0492, RoleSpawnFlags.None);
            ev.Player.MaxHealth *= 1.5f;
        });
    }
}
