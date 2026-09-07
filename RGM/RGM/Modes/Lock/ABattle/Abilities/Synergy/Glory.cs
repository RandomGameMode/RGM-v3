using Exiled.API.Enums;
using LabApi.Features.Wrappers;
using MEC;
using RGM.API.Features;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace RGM.Modes.Abilities.Synergy;

[RequiresAbility(AbilityType.LEGEND_FLASHLIGHT, AbilityType.NORMAL_TORCH)]
[Ability("광휘", "<플래시라이트, 횃불> 당신을 쳐다보는 눈은 멀어버릴 것입니다.", AbilityCategory.Synergy, AbilityType.SYNERGY_GLORY)]
public class Glory : Ability
{
    private CoroutineHandle _radiation;

    public override void OnEnabled()
    {
        _radiation = Timing.RunCoroutine(Radiation());
    }

    public override void OnDisabled()
    {
        Timing.KillCoroutines(_radiation);
    }

    private IEnumerator<float> Radiation()
    {
        LightSourceToy lightSource = LightSourceToy.Create();
        lightSource.Color = Color.yellow;
        lightSource.Intensity = 10;
        lightSource.Range = 25;

        while (Owner.IsAlive)
        {
                foreach (var player in PlayerManager.List)
                {
                    if (player == Owner || !player.IsAlive) continue;
                    if (!HitboxIdentity.IsEnemy(player.ReferenceHub, Owner.ReferenceHub)) continue;

                    lightSource.Position = Owner.Position;

                    if (!player.IsLookingAt(Owner, fov: 20)) continue;

                    float damage = 3f;
                    if (Owner.HasAbility(AbilityType.SYNERGY_REFLECTEDLIGHT))
                    {
                        player.Hit(Owner, damage);
                        player.EnableEffect(EffectType.Burned, 1, 10f);
                    }
                    Hitmarker.SendHitmarkerDirectly(Owner.ReferenceHub, 1f);
                    player.EnableEffect(EffectType.Flashed, 1, 1.5f);
                }
                lightSource.Position = Owner.Position;
                yield return Timing.WaitForSeconds(0.05f);
        }
    }
}
