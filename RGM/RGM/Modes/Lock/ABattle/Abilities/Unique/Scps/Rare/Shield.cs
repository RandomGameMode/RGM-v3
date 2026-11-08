using System.Collections.Generic;
using Exiled.Events.EventArgs.Player;
using MEC;
using RGM.API.Features;

namespace RGM.Modes.Abilities.Unique.Scp0492.Rare;

[Ability("보호막",
    "자신에개 『보호막』 효과를 부여합니다.",
    AbilityCategory.Rare, AbilityType.RARE_SCPS_SHIELD, RoleAbility.Scps)]
public class Shield : Ability
{
    private const float AddHS = 400f;
    private const float AddHSRegen = 5f;
    private const float RegenInterval = 0.2f;
    private const float RegenBlockDuration = 5f;

    private CoroutineHandle _regenerationCoroutine;
    private bool _isRegenerationBlocked;
    private int _regenerationBlockVersion;

    public override void OnEnabled()
    {
        Owner.MaxHumeShield += Owner.IsScpRole() ? AddHS : AddHS * 0.2f;
        Exiled.Events.Handlers.Player.Hurt += OnHurt;
        _regenerationCoroutine = Timing.RunCoroutine(RegenerateHumeShield());
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Player.Hurt -= OnHurt;
        Timing.KillCoroutines(_regenerationCoroutine);

        Owner.MaxHumeShield -= Owner.IsScpRole() ? AddHS : AddHS * 0.2f;
        if (Owner.HumeShield > Owner.MaxHumeShield)
            Owner.HumeShield = Owner.MaxHumeShield;

        _isRegenerationBlocked = false;
        _regenerationBlockVersion++;
    }

    private IEnumerator<float> RegenerateHumeShield()
    {
        while (true)
        {
            if (!_isRegenerationBlocked && Owner.IsAlive && Owner.HumeShield < Owner.MaxHumeShield)
                Owner.HumeShield = System.Math.Min(Owner.HumeShield + (Owner.IsScpRole() ? AddHSRegen : AddHSRegen * 0.2f), Owner.MaxHumeShield);

            yield return Timing.WaitForSeconds(RegenInterval);
        }
    }

    private void OnHurt(HurtEventArgs ev)
    {
        if (ev.Player != Owner || ev.DamageHandler.Damage <= 0f)
            return;

        _isRegenerationBlocked = true;
        var blockVersion = ++_regenerationBlockVersion;

        Timing.CallDelayed(RegenBlockDuration, () =>
        {
            if (_regenerationBlockVersion == blockVersion)
                _isRegenerationBlocked = false;
        });
    }
}