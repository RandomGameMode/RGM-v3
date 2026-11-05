// ReSharper Disable IteratorNeverReturns

using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.Events.EventArgs.Player;
using InventorySystem.Items.MicroHID.Modules;
using MEC;
using RGM.API.Features;

namespace RGM.Modes.Abilities.Legend;

[Ability("화염 방사기", """
                   자동으로 충전되는 Micro H.I.D를 받습니다. 해당 아이템은 최종 데미지가 38% 감소합니다.
                   피격된 대상은 5초 동안 초당 최대 HP의 1%만큼 지속 화상 피해를 입습니다.
                   """,
    AbilityCategory.Legend, AbilityType.LEGEND_FLAMETHROWER)]
public class FlameThrower : Ability
{
    private const float BurnDuration = 5f;
    private const float BurnDamageRatio = 0.01f;

    private ushort _flamethrowerSerial;
    private CoroutineHandle _energyRecharge;
    private readonly Dictionary<Player, CoroutineHandle> _burnRoutines = new();

    public override void OnEnabled()
    {
        Item ft = Owner.AddItem(ItemType.MicroHID);
        _flamethrowerSerial = ft.Serial;

        Exiled.Events.Handlers.Player.ChangedItem += OnChangedItem;
        Exiled.Events.Handlers.Player.ChangingMicroHIDState += OnChangingMicroHIDState;
        Exiled.Events.Handlers.Player.Hurting += OnHurting;

        _energyRecharge = Timing.RunCoroutine(OnStarted());
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Player.ChangedItem -= OnChangedItem;
        Exiled.Events.Handlers.Player.ChangingMicroHIDState -= OnChangingMicroHIDState;
        Exiled.Events.Handlers.Player.Hurting -= OnHurting;

        Timing.KillCoroutines(_energyRecharge);
        foreach (var burnRoutine in _burnRoutines.Values)
            Timing.KillCoroutines(burnRoutine);

        _burnRoutines.Clear();
    }

    private IEnumerator<float> OnStarted()
    {
        while (true)
        {
            foreach (var item in Item.List.Where(x => x.Type == ItemType.MicroHID))
            {
                if (_flamethrowerSerial != item.Serial) continue;
                var microHid = (MicroHid)item;

                if (microHid.Energy < 1 && microHid.State == MicroHidPhase.Standby)
                    microHid.Energy += 0.04f;
            }

            yield return Timing.WaitForSeconds(0.5f);
        }
    }

    private void OnChangedItem(ChangedItemEventArgs ev)
    {
        if (ev.Item?.Serial != _flamethrowerSerial)
            return;

        ev.Player.AddHint("화염 방사기",
            $"<b><color={ABattle.RatingColor["전설"]}>화염 방사기</color></b> 능력이 있는 <b>마이크로 H.I.D</b>입니다!");
    }

    private void OnChangingMicroHIDState(ChangingMicroHIDStateEventArgs ev)
    {
        if (_flamethrowerSerial == ev.Item.Serial && ev.NewPhase == MicroHidPhase.WindingUp)
            ev.NewPhase = MicroHidPhase.Firing;
    }

    private void OnHurting(HurtingEventArgs ev)
    {
        if (ApplyFixedDamage.IsApplying || ev.Attacker == null || ev.Player == ev.Attacker)
            return;

        if (ev.Attacker.CurrentItem == null || _flamethrowerSerial != ev.Attacker.CurrentItem.Serial) return;
        ev.DamageHandler.Damage *= 0.62f;
        ApplyBurn(ev.Player);
    }

    private void ApplyBurn(Player target)
    {
        if (_burnRoutines.TryGetValue(target, out CoroutineHandle existingRoutine))
        {
            _burnRoutines.Remove(target);
            Timing.KillCoroutines(existingRoutine);
        }

        _burnRoutines[target] = Timing.RunCoroutine(Burn(target));
    }

    private IEnumerator<float> Burn(Player target)
    {
        for (int i = 0; i < BurnDuration; i++)
        {
            yield return Timing.WaitForSeconds(1f);

            if (target == null || !target.IsConnected || !target.IsAlive)
                break;

            ApplyFixedDamage.Apply(Owner, target, target.MaxHealth * BurnDamageRatio);
        }

        if (target != null) _burnRoutines.Remove(target);
    }
}