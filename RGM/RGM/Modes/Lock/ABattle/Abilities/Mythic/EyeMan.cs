using Exiled.API.Features;
using MEC;
using RGM.API.Features;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using Exiled.API.Enums;
using ProjectMER.Features.Objects;
using ProjectMER.Features;
using Mirror;
using LabApi.Features.Wrappers;

namespace RGM.Modes.Abilities.Mythic;

[Ability("눈빛맨", "상대는 눈에 띄는 것만으로도 압도당할 것입니다!", 
    AbilityCategory.Mythic, AbilityType.MYTHIC_EYEMAN)]
public class EyeMan : Ability
{
    private CoroutineHandle _twinkle;

    public override void OnEnabled()
    {
        _twinkle = Timing.RunCoroutine(Twinkle());
    }

    public override void OnDisabled()
    {
        Timing.KillCoroutines(_twinkle);
    }

    private IEnumerator<float> Twinkle()
    {
        SchematicObject beam = ObjectSpawner.SpawnSchematic("눈빛맨", new Vector3(1205, 1205, 1205));
        beam.GetComponentsInChildren<PrimitiveObjectToy>().ToList().ForEach(x => x.MovementSmoothing = 0);

        while (Owner.IsAlive)
        {
            try
            {
                if (Owner.TryGetLookPlayer(100f, out var target, out _))
                {
                    if (Owner != target && HitboxIdentity.IsEnemy(Owner.ReferenceHub, target.ReferenceHub))
                    {
                        beam.Position = Owner.CameraTransform.position + Owner.CameraTransform.forward * 0.3f;
                        beam.Rotation = Quaternion.LookRotation(Owner.CameraTransform.forward);
                        target.EnableEffect(EffectType.SinkHole, 1, 0.5f);
                        target.EnableEffect(EffectType.Blinded, 1, 0.5f);
                        target.CurrentItem = null;
                        ApplyFixedDamage.Apply(Owner, target, target.IsScpRole() ? target.MaxHealth * 0.05f : target.MaxHealth * 0.15f);
                        Hitmarker.SendHitmarkerDirectly(Owner.ReferenceHub, 0.5f);
                    }
                    else
                        beam.Position = new Vector3(1205, 1205, 1205);
                }
                else
                    beam.Position = new Vector3(1205, 1205, 1205);
            }
            catch (Exception e)
            {
                Log.Error($"눈빛맨 오류: {e}");
            }

            yield return Timing.WaitForSeconds(0.05f);
        }

        NetworkServer.Destroy(beam.gameObject);
        beam.Destroy();
    }
}