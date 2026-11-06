using System.Collections.Generic;
using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Roles;
using Exiled.Events.EventArgs.Player;
using MEC;
using RGM.API.Features;

namespace RGM.Modes.Abilities.Unique.Scp079.Rare;

/*
[Ability("고대의 존재 압도+",
    "[Q] 또는 [V]ㅣ키를 홀드하는 동안 전력을 사용하여 해당 방에 있는 인간에게 더욱 강화된 디버프를 지급합니다.",
    AbilityCategory.Ancient,
    AbilityType.ANCIENT_SCP079_OVERWHELMING,
     RoleAbility.Scp079)]*/
public class OverwhelmingB : Ability
{
    private HashSet<Player> RemovingPlayers = [];
    private CoroutineHandle _handle;
    
    public override void OnEnabled()
    {
        Exiled.Events.Handlers.Player.VoiceChatting += OnVoiceChatting;
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Player.VoiceChatting -= OnVoiceChatting;
    }

    private void OnVoiceChatting(VoiceChattingEventArgs ev)
    {
        if (ev.Player != Owner) return;

        foreach (var player in PlayerManager.List.Where(x => !x.IsNPC && !x.IsScpRole() && x.IsAlive))
        {
            if (player.CurrentRoom == ev.Player.CurrentRoom)
            {
                player.EnableEffect(EffectType.Slowness,
                    (byte)(6 * ev.Player.AbilityCount(AbilityType.RARE_SCP079_OVERWHELMING)), 0.1f);
                player.AddEffect(EffectType.Decontaminating, 1, .1f);
                if (RemovingPlayers.Add(player))
                    player.GetAbilities().GetRandomValue().OnDisabled();
                if (!Timing.IsRunning(_handle))
                    _handle = Timing.RunCoroutine(RemoveCoroutine());
            }
        }

        if (ev.Player.Role is Scp079Role scp079)
            scp079.Energy -= 0.05f;
    }

    private IEnumerator<float> RemoveCoroutine()
    {
        yield return Timing.WaitForSeconds(2f);
        
        RemovingPlayers.Clear();
    }
}
