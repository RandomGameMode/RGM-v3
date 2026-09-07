using MEC;
using PlayerRoles;
using RGM.API.Features;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Exiled.API.Features;

namespace RGM.Modes;

[Mode(ModeCategory.OnlySub, ModeInfo.Plus, ModeType.DistractedDriver)]
public class DistractedDriver : Mode
{
    public override string Name => "전방주시태만";
    public override string Description => "앞 좀 보고 다니세요!";
    public override string Detail => "모든 플레이어들은 아래를 쳐다보게됩니다.";
    public override string Color => "FF3333";
    public override string Author => "Ragdoll";
    
    CoroutineHandle _onModeStarted;
    public override void OnEnabled()
    {
        _onModeStarted = Timing.RunCoroutine(OnModeStarted());
    }
    public override void OnDisabled()
    {
        Timing.KillCoroutines(_onModeStarted);
    }

    IEnumerator<float> OnModeStarted()
    {
        yield return Timing.WaitForSeconds(1f);

        while (!Round.IsEnded)
        {
            foreach (var player in PlayerManager.List
            .Where(p => p != null && p.IsAlive && p.Role != RoleTypeId.Scp079))
            {
                player.ForceLookAt(player.Position + (Vector3.down * 100f));
            }
            yield return Timing.WaitForOneFrame;
        }

    }
}
