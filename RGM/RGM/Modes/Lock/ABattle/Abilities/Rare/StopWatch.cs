using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Scp106;
using Exiled.Events.EventArgs.Warhead;
using MEC;
using RGM.API.Features;
using RGM.Modes.Abilities.Synergy;

namespace RGM.Modes.Abilities.Rare;

[Ability("회중시계", "지급된 동전을 튕기면 3초간 움직일 수 없는 대신에 6초간 무적 상태가 됩니다.", AbilityCategory.Rare, AbilityType.RARE_STOPWATCH)]
public class StopWatch : Ability
{
    private const float Duration = 6f;

    private static bool _isDetonatingState;

    private ushort _clockCoinSerial;
    private Player _protectedPlayer;
    private bool _isActive;
    private int _version;

    public override void OnEnabled()
    {
        Item cc = Owner.AddItem(ItemType.Coin);
        _clockCoinSerial = cc.Serial;

        Exiled.Events.Handlers.Player.ChangedItem += OnChangedItem;
        Exiled.Events.Handlers.Player.FlippingCoin += OnFlippingCoin;
        Exiled.Events.Handlers.Player.Dying += OnDying;
        Exiled.Events.Handlers.Player.Hurting += OnHurting;
        Exiled.Events.Handlers.Scp106.Attacking += OnScp106Attacking;
        Exiled.Events.Handlers.Warhead.Detonating += OnDetonating;
    }

    public override void OnDisabled()
    {
    }

    private void OnChangedItem(ChangedItemEventArgs ev)
    {
        if (ev.Item?.Serial != _clockCoinSerial)
            return;

        ev.Player.AddHint("동전 사용 설명", $"이 동전을 튕기면 <b><color={ABattle.RatingColor["희귀"]}>회중시계</color></color></b> 능력을 사용할 수 있습니다.");
    }

    private void OnFlippingCoin(FlippingCoinEventArgs ev)
    {
        if (_clockCoinSerial != ev.Item.Serial)
            return;

        Player player = ev.Player;
        ev.Item.Destroy();

        player.EnableEffect(EffectType.Ensnared, 1, 3);

        _protectedPlayer = player;
        _isActive = true;
        int version = ++_version;

        Timing.CallDelayed(Duration, () =>
        {
            if (_isActive && _version == version)
                EndInvincibility();
        });
    }

    private void EndInvincibility()
    {
        if (!_isActive)
            return;

        _isActive = false;
        _protectedPlayer = null;
        _version++;
    }

    private void OnDetonating(DetonatingEventArgs _)
    {
        if (_isDetonatingState)
            return;

        _isDetonatingState = true;
        Timing.CallDelayed(Timing.WaitForOneFrame, () => _isDetonatingState = false);
    }

    private void OnDying(DyingEventArgs ev)
    {
        if (ev.Player != _protectedPlayer || !_isActive || IsExemptDamage(ev.DamageHandler.Type) ||
            WeakPointAttack.ShouldIgnoreDefenses(ev.Attacker))
            return;

        ev.IsAllowed = false;
    }

    private void OnHurting(HurtingEventArgs ev)
    {
        if (_isActive &&
            ev.Player == _protectedPlayer &&
            !WeakPointAttack.ShouldIgnoreDefenses(ev.Attacker) &&
            (!IsExemptDamage(ev.DamageHandler.Type) ||
             ev.DamageHandler.Type == DamageType.PocketDimension && !ev.IsInstantKill))
            ev.IsAllowed = false;
    }

    private void OnScp106Attacking(AttackingEventArgs ev)
    {
        if (ev.Target != _protectedPlayer || !_isActive || WeakPointAttack.ShouldIgnoreDefenses(ev.Player))
            return;

        ev.IsAllowed = false;
    }

    private static bool IsExemptDamage(DamageType damageType)
    {
        return _isDetonatingState ||
               damageType is DamageType.Warhead or DamageType.PocketDimension or DamageType.Crushed;
    }
}
