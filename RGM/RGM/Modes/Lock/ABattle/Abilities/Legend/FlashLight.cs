using System.Collections.Generic;
using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Features.Items;
using Exiled.Events.EventArgs.Player;
using MEC;
using RGM.API.Features;

namespace RGM.Modes.Abilities.Legend;

[Ability("플래시라이트", "지급된 손전등을 들고 상대를 쳐다보면 눈뽕 공격을 가할 수 있습니다.", AbilityCategory.Legend, AbilityType.LEGEND_FLASHLIGHT)]
public class FlashLight : Ability
{
    private CoroutineHandle _onStarted;
    private ushort _flashLightSerial;

    public override void OnEnabled()
    {
        Item fl = Owner.AddItem(ItemType.Flashlight);
        _flashLightSerial = fl.Serial;

        Exiled.Events.Handlers.Player.ChangedItem += OnChangedItem;

        if (Timing.IsRunning(_onStarted)) return;
        _onStarted = Timing.RunCoroutine(OnStarted());
    }

    private void OnChangedItem(ChangedItemEventArgs ev)
    {
        if (ev.Item?.Serial != _flashLightSerial)
            return;
        
        ev.Player.AddHint("플래시라이트", $"손전등을 상대에게 비추면 <b><color={ABattle.RatingColor["전설"]}>플래시라이트</color></b> 능력을 사용할 수 있습니다.");
    }

    private IEnumerator<float> OnStarted()
    {
        while (true)
        {
            foreach (var player in PlayerManager.List.Where(player =>
             player.IsAlive &&
             player.CurrentItem != null &&
             _flashLightSerial == player.CurrentItem.Serial))
            {
                foreach (var target in PlayerManager.List.Where(p => p != null && p.IsAlive))
                {
                    if (target == player) continue;
                    if (!HitboxIdentity.IsEnemy(player.ReferenceHub, target.ReferenceHub)) continue;

                    if (!player.IsLookingAt(target, fov: 10)) continue;

                    float damage = 3f;
                    if (player.HasAbility(AbilityType.SYNERGY_REFLECTEDLIGHT))
                    {
                        target.Hit(player, damage);
                        target.EnableEffect(EffectType.Burned, 1, 10f);
                    }
                    Hitmarker.SendHitmarkerDirectly(player.ReferenceHub, 1f);
                    target.EnableEffect(EffectType.Flashed, 1, 1);
                }
            }

            yield return Timing.WaitForSeconds(0.05f);
        }
    }
}
