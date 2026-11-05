using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Features.Roles;
using Exiled.Events.EventArgs.Player;
using RGM.API.Features;

namespace RGM.Modes.Abilities.Unique.Scp079.Rare;

// [Ability("고대의 존재 압도",
//     "[Q] 또는 [V]ㅣ키를 홀드하는 동안 전력을 사용하여 해당 방에 있는 인간의 속도를 감소시킵니다.",
//     AbilityCategory.Ancient,
//     AbilityType.ANCIENT_SCP079_OVERWHELMING,
     // RoleAbility.Scp079)]
public class OverwhelmingA : Ability
{
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
        if (ev.Player != Owner)
            return;

        foreach (var player in PlayerManager.List.Where(x => !x.IsNPC && !x.IsScpRole() && x.IsAlive))
        {
            if (player.CurrentRoom == ev.Player.CurrentRoom)
            {
                player.EnableEffect(EffectType.Slowness,
                    (byte)(6 * ev.Player.AbilityCount(AbilityType.RARE_SCP079_OVERWHELMING)), 0.1f);
                player.AddEffect(EffectType.CardiacArrest,
                    1, .1f);
            }
        }

        if (ev.Player.Role is Scp079Role scp079)
            scp079.Energy -= 0.05f;
    }
}
