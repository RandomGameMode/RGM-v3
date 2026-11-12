using System.Collections.Generic;
using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using MEC;
using PlayerRoles;
using RGM.API.Features;
using UnityEngine;

namespace RGM.Modes.Abilities.Legend;

[Ability("괴성", """
               적을 보고 있을 때 마이크를 키면 모든 적군을 3초간 『기절』 상태로 만들고, 5초간 대상의 이동 속도를 80% 감소하며, 12초간 『무장 해제』 효과를 부여합니다.
               이후 영향을 받은 대상에게 25초간 출혈 효과를 부여합니다.
               스킬 사용 시 재사용 대기시간 60초가 적용됩니다.
               """, AbilityCategory.Legend, AbilityType.LEGEND_SCREAM, isUnique:true)]
public class Scream : Ability
{
    private const int RoaringSoundCooldownDuration = 60;
    private const float DisarmDuration = 12f;
    private int _roaringSoundCooldown;
    private readonly HashSet<Player> _disarmedPlayers = new();

    public override void OnEnabled()
    {
        Exiled.Events.Handlers.Player.VoiceChatting += OnVoiceChatting;
        Exiled.Events.Handlers.Player.ChangingItem += OnChangingItem;
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Player.VoiceChatting -= OnVoiceChatting;
        Exiled.Events.Handlers.Player.ChangingItem -= OnChangingItem;
        _disarmedPlayers.Clear();
    }

    private void OnChangingItem(ChangingItemEventArgs ev)
    {
        if (_disarmedPlayers.Contains(ev.Player))
            ev.IsAllowed = false;
    }

    private IEnumerator<float> OnVoiceChatting(VoiceChattingEventArgs ev)
    {
        if (ev.Player != Owner)
            yield break;

        if (_roaringSoundCooldown > 0)
            yield break;

        if (!ev.Player.TryGetLookPlayer(36f, out Player target, out _) ||
            !HitboxIdentity.IsEnemy(ev.Player.ReferenceHub, target.ReferenceHub)) yield break;
        
        _roaringSoundCooldown = RoaringSoundCooldownDuration;
        Timing.CallDelayed(RoaringSoundCooldownDuration, () => _roaringSoundCooldown = 0);

        Tools.PlayGlobalAudio("GmanRoaringSound");

        _disarmedPlayers.Clear();

        foreach (var player in PlayerManager.List.Where(x => !x.IsNPC && HitboxIdentity.IsEnemy(ev.Player.ReferenceHub, x.ReferenceHub) && x.IsAlive))
        {
            player.EnableEffect(EffectType.Ensnared, 1, 3f);
            player.EnableEffect(EffectType.Flashed, 1, 3f);
            player.EnableEffect(EffectType.SinkHole, 1, 8f);
            player.EnableEffect(EffectType.Slowness, 120, 8f);
            player.EnableEffect(EffectType.Stained, 1, 8f);
            player.EnableEffect(EffectType.Disabled, 1, 12f);
            player.EnableEffect(EffectType.Blurred, 1, 12f);
            player.EnableEffect(EffectType.Deafened, 1, 12f);
            player.EnableEffect(EffectType.AmnesiaItems, 1, 12f);
            if (player.Role.Type != RoleTypeId.Scp079)
                player.EnableEffect(EffectType.Bleeding, 1, 25f);

            player.CurrentItem = null;
            _disarmedPlayers.Add(player);
        }

        Timing.CallDelayed(DisarmDuration, () => _disarmedPlayers.Clear());

        yield return Timing.WaitForSeconds(0.2f);

        PlayerManager.List.ToList().ForEach(x => x.AddHint("저주받은 괴성", "<b><color=#B08A03>저주받은 괴성</color></b>", 5));
    }
}
