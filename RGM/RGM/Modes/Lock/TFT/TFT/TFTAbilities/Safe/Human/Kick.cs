using Exiled.Events.EventArgs.Player;
using MEC;
using RGM.API.Features;

namespace RGM.Modes.Lock.TFT.Safe.Human;

[TFTAbility("태권도", "능력 키(ALT)를 눌러 발차기를 할 수 있습니다. 부위별로 데미지가 다르게 적용됩니다.", TFTAbilityLevel.Safe, TFTAbilityCategory.Human, TFTAbilityPoint.ALT, TFTAbilityType.Kick, "💫")]
public class Kick : TFTAbility
{
    private const float MeleeDamage = 750f;
    private static int _meleeCooldown;

    public override void OnEnabled()
    {
        Exiled.Events.Handlers.Player.TogglingNoClip += OnTogglingNoClip;
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Player.TogglingNoClip -= OnTogglingNoClip;
    }

    private void OnTogglingNoClip(TogglingNoClipEventArgs ev)
    {
        if (ev.Player != Owner)
            return;

        if (!ev.Player.TryGetLookPlayer(4.5f, out var player, out var hit)) return;
        if (ev.Player == player || _meleeCooldown > 0 ||
            !HitboxIdentity.IsEnemy(ev.Player.ReferenceHub, player.ReferenceHub)) return;

        var damage = DmgCalc(hit?.transform.name);

        if (player.IsScp) damage *= 4;

        Hitmarker.SendHitmarkerDirectly(ev.Player.ReferenceHub, damage / MeleeDamage);
        player.Hit(ev.Player, damage);

        _meleeCooldown = 1;

        Timing.CallDelayed(1f, () => _meleeCooldown = 0);
        return;

        float DmgCalc(string pos)
        {
            switch (pos)
            {
                case "Head":
                    return MeleeDamage * 2f;

                case "Chest":
                    return MeleeDamage;

                default:
                    return MeleeDamage * 0.7f;
            }
        }
    }
}
