using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using MEC;
using RGM.API.Features;
using System.Collections.Generic;
using System.Linq;

namespace RGM.Modes.Lock.TFT.Keter.Scp096;

[TFTAbility("괴성", "능력 키(ALT)를 눌러 모든 적들을 잠시 패닉 상태에 빠지게 만듭니다.", TFTAbilityLevel.Keter, TFTAbilityCategory.Scp096, TFTAbilityPoint.ALT, TFTAbilityType.RoaringSound, "😱")]
public class RoaringSound : TFTAbility
{
    CoroutineHandle _cooldown;
    private const int RoaringSoundCooldownDuration = 60;
    private const float DisarmDuration = 15f;
    private int _roaringSoundCooldown;
    private readonly HashSet<Player> _disarmedPlayers = new();

    public override void OnEnabled()
    {
        Exiled.Events.Handlers.Player.TogglingNoClip += OnTogglingNoClip;
        Exiled.Events.Handlers.Player.ChangingItem += OnChangingItem;

        _cooldown = Timing.RunCoroutine(cooldown());
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Player.TogglingNoClip -= OnTogglingNoClip;
        Exiled.Events.Handlers.Player.ChangingItem -= OnChangingItem;

        Timing.KillCoroutines(_cooldown);
        _disarmedPlayers.Clear();
    }
    
    private void OnChangingItem(ChangingItemEventArgs ev)
    {
        if (_disarmedPlayers.Contains(ev.Player))
            ev.IsAllowed = false;
    }
    
    private IEnumerator<float> cooldown()
    {
        while (true)
        {
            for (int i = 0; i < 60; i++)
            {
                if (_roaringSoundCooldown > 0)
                    _roaringSoundCooldown--;

                Data.Description = $"능력 키(ALT)를 눌러 모든 적들을 잠시 패닉 상태에 빠지게 만듭니다. ({(_roaringSoundCooldown == 0 ? "사용 가능" : $"{_roaringSoundCooldown}초 남음")})";

                yield return Timing.WaitForSeconds(1f);
            }
        }
    }

    private IEnumerator<float> OnTogglingNoClip(TogglingNoClipEventArgs ev)
    {
        if (ev.Player != Owner)
            yield break;

        if (_roaringSoundCooldown > 0) yield break;
        
        _roaringSoundCooldown = RoaringSoundCooldownDuration;
        Timing.CallDelayed(RoaringSoundCooldownDuration, () => _roaringSoundCooldown = 0);

        Tools.PlayGlobalAudio("GmanRoaringSound");
        
        _disarmedPlayers.Clear();

        foreach (var player in Player.List.Where(x => !x.IsNPC && HitboxIdentity.IsEnemy(ev.Player.ReferenceHub, x.ReferenceHub) && x.IsAlive))
        {
            player.EnableEffect(EffectType.Ensnared, 1, 1.5f);
            player.EnableEffect(EffectType.Flashed, 1, 3f);
            player.EnableEffect(EffectType.Blurred, 1, 15f);
            player.EnableEffect(EffectType.Deafened, 1, 15f);
            player.EnableEffect(EffectType.SinkHole, 1, 10f);
            player.EnableEffect(EffectType.Slowness, 120, 6f);
            player.EnableEffect(EffectType.Disabled, 1, 15f);
            player.EnableEffect(EffectType.Stained, 1, 5f);
            player.EnableEffect(EffectType.AmnesiaItems, 1, 15f);
            player.EnableEffect(EffectType.Bleeding, 1, 30f);
                
            player.CurrentItem = null;
            _disarmedPlayers.Add(player);
        }

        Timing.CallDelayed(DisarmDuration, () => _disarmedPlayers.Clear());
        
        yield return Timing.WaitForSeconds(0.65f);
        PlayerManager.List.ToList().ForEach(x => x.AddHint("저주받은 괴성", "<b><color=#B08A03>저주받은 괴성</color></b>", 5));
    }
}
