using Exiled.API.Features;
using MEC;
using RGM.API.Features;
using System.Collections.Generic;
using UnityEngine;
using System;
using Exiled.API.Features.Roles;

namespace RGM.Modes.Abilities.Unique.Scp079.Legend;

[Ability("감시 카메라", "자신이 보는 대상에게 피해를 입힙니다.(0.2초당 대상 최대 HP의 2% 데미지)",
    AbilityCategory.Legend, AbilityType.LEGEND_SCP079_SECURITYCAMERA, RoleAbility.Scp079)]
public class SecurityCamera : Ability
{
    CoroutineHandle SecurityCameraHandle;
    public override void OnEnabled()
        => SecurityCameraHandle = Timing.RunCoroutine(SecurityCameraSystem());
    

    public override void OnDisabled()
        => Timing.KillCoroutines(SecurityCameraHandle);


    private IEnumerator<float> SecurityCameraSystem()
    {
        while (Owner.IsAlive)
        {
            try
            {
                var Targets = Scan(Owner);
                if (Targets.Count > 0)
                {
                    foreach (var player in Targets)
                    {
                        if (player == null) continue;
                        if (!HitboxIdentity.IsEnemy(Owner.ReferenceHub, player.ReferenceHub)) continue;

                        var damage = player.MaxHealth * 0.02f;

                        player.AddHint("알림", $"<color=red>SCP-079</color>에게 감시당하고 있습니다...", 0.5f);

                        Hitmarker.SendHitmarkerDirectly(Owner.ReferenceHub, 0.5f);
                        player.Hit(Owner, damage);
                    }
                }
            }
            
            catch (Exception e)
            {
                Log.Error($"SecurityCamera Error : {e}");
            }

            yield return Timing.WaitForSeconds(0.2f);
        }
    }

    private List<Player> Scan(Player player) 
    {
        Vector3 pos = new Vector3(0, 0, 0);
        Vector3 dir = new Vector3(0, 0, 0);

        List<Player> TargetPlayers = new();

        if (player.Role is Scp079Role scp079)
        {
            var cam = scp079.Base.CurrentCamera;

            if (cam != null)
            {
                pos = cam.CameraPosition;
                dir = cam.CameraAnchor.forward;
            }
        }
        
        foreach (var target in PlayerManager.List)
        {
            if (Tools.IsLookingAt(dir, pos, target, 100f)) TargetPlayers.Add(target);
        }

        return TargetPlayers;
    }
}