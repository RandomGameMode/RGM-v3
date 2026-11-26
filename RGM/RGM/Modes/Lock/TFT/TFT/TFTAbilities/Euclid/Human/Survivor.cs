using Exiled.API.Enums;
using Exiled.Events.EventArgs.Player;
using MEC;
using System.Collections.Generic;
using RGM.API.DataBases;
using static RGM.Modes.Lock.TFT.Variables.Base;
using RGM.API.Features;

namespace DAONTFT.Core.TFT.Euclid.Human;

[TFTAbility("생존 전문가", "죽음에 이르는 피해를 받았을 때, 5초 간 생존 효과를 부여하며, 체력을 50% 회복합니다. (쿨타임 2분)", TFTAbilityLevel.Euclid, TFTAbilityCategory.Human, TFTAbilityPoint.Continuous, TFTAbilityType.Survivor, "💝")]
public class Survivor : TFTAbility
{
    private bool _isEnabled;
    private int _cooldown;
    private CoroutineHandle _cooldownProcess;

    public override void OnEnabled()
    {
        Exiled.Events.Handlers.Player.Dying += OnDying;

        _cooldownProcess = Timing.RunCoroutine(CooldownProcess());
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Player.Dying -= OnDying;

        Timing.KillCoroutines(_cooldownProcess);
    }

    private IEnumerator<float> CooldownProcess()
    {
        while (Owner.IsAlive)
        {
            if (_cooldown > 0)
                _cooldown--;

            Data.Description = $"죽음에 이르는 피해를 받았을 때, 3초 간 살아남습니다. ({(_cooldown == 0 ? "사용 가능" : $"재사용까지 {_cooldown}초")})";

            yield return Timing.WaitForSeconds(1f);
        }
    }

    private void OnDying(DyingEventArgs ev)
    {
        if (ev.Player != Owner || IsLifeUsed[Owner] || Datas.BlockDamageTypes.Contains(ev.DamageHandler.Type))
            return;

        if (_cooldown > 0) return;
        if (_isEnabled) return;
        _isEnabled = true;

        ev.IsAllowed = false;

        ev.Player.EnableEffect(EffectType.Invisible, 1, 5);
        ev.Player.AddEffect(EffectType.Ghostly, 1, 5);
        ev.Player.AddEffect(EffectType.MovementBoost, 40, 5);
        ev.Player.ApplyGodMode(5);
        ev.Player.Heal(ev.Player.MaxHealth * 0.5f);

        Timing.CallDelayed(5f, () => { _isEnabled = false; });

        IsLifeUsed[Owner] = true;

        Timing.CallDelayed(Timing.WaitForOneFrame, () => { IsLifeUsed[Owner] = false; });

        _cooldown = 120;
    }
}
