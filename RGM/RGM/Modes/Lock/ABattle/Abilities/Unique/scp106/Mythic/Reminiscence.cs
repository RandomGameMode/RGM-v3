using System.Collections.Generic;
using System.Linq;
using CustomPlayerEffects;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Scp106;
using Exiled.Events.EventArgs.Player;
using MEC;
using PlayerRoles.PlayableScps.Scp106;
using PlayerStatsSystem;
using RGM.API.Features;
using Scp106Role = Exiled.API.Features.Roles.Scp106Role;
using UnityEngine;

namespace RGM.Modes.Abilities.Unique.Scp106.Mythic;

[Ability("회고", 
    """
    스토킹에서 나올 때와 텔레포트 후 나오는 지점 반경 14m 내의 모든 적을 『사망』시킵니다.
    추가로, 기본 공격이 대상에게 『죽음에 이르는 공격』을 가하며, 자신의 이동 속도가 60% 증가합니다.
    능력 획득 시, <color=#FFC000>[전용 전설]</color> 회상이 있다면 <color=#FF2400>[전용 신화]</color> 회고로 대체됩니다.
    """, 
    AbilityCategory.Mythic,
    AbilityType.MYTHIC_SCP106_REMINISCENCE,
    RoleAbility.Scp106)]

public class Reminiscence : Ability
{
    private const float AttackRange = 14f;
    private const float Scp106AttackDamage = 40f;

    private static readonly HashSet<Player> DefenseIgnoringAttackers = [];

    private readonly HashSet<Player> _flashbackTargets = [];
    private bool _isEnabled;
    private bool _isExecuting;

    public static bool IsIgnoringDefenses(Player attacker) =>
        attacker != null && DefenseIgnoringAttackers.Contains(attacker);

    public override void OnEnabled()
    {
        _isEnabled = true;

        if (Owner.HasAbility(AbilityType.LEGEND_SCP106_FLASHBACK))
            Owner.RemoveAbility(AbilityType.LEGEND_SCP106_FLASHBACK);

        Owner.AddEffect(EffectType.MovementBoost, 60);
        Exiled.Events.Handlers.Scp106.ExitStalking += OnExitStalking;
        Exiled.Events.Handlers.Scp106.Teleporting += OnTeleporting;
        Exiled.Events.Handlers.Player.Hurting += OnHurting;
        ABattle.Instance.AddingAbility += OnAddingAbility;
    }

    public override void OnDisabled()
    {
        _isEnabled = false;

        Owner.RemoveEffect(EffectType.MovementBoost, 60);
        Exiled.Events.Handlers.Scp106.ExitStalking -= OnExitStalking;
        Exiled.Events.Handlers.Scp106.Teleporting -= OnTeleporting;
        Exiled.Events.Handlers.Player.Hurting -= OnHurting;
        ABattle.Instance.AddingAbility -= OnAddingAbility;

        _flashbackTargets.Clear();
    }

    private void OnExitStalking(ExitStalkingEventArgs ev)
    {
        if (ev.Player != Owner || !ev.IsAllowed)
            return;

        Timing.CallDelayed(Timing.WaitForOneFrame, () =>
        {
            if (_isEnabled && Owner.IsAlive)
                ApplyFlashbackAttack(Owner.Position);
        });
    }

    private void OnTeleporting(TeleportingEventArgs ev)
    {
        if (ev.Player != Owner || !ev.IsAllowed)
            return;

        ApplyFlashbackAttack(ev.Position);
    }

    private void ApplyFlashbackAttack(Vector3 position)
    {
        foreach (Player target in PlayerManager.List.Where(player =>
                     player.IsAlive &&
                     HitboxIdentity.IsEnemy(Owner.ReferenceHub, player.ReferenceHub) &&
                     Vector3.Distance(player.Position, position) <= AttackRange))
        {
            _flashbackTargets.Add(target);
            DefenseIgnoringAttackers.Add(Owner);
            try
            {
                target.Hurt(new ScpDamageHandler(
                    Owner.ReferenceHub,
                    Scp106AttackDamage,
                    DeathTranslations.PocketDecay));
            }
            finally
            {
                DefenseIgnoringAttackers.Remove(Owner);
                _flashbackTargets.Remove(target);
            }
        }
    }

    private void OnAddingAbility(AddingAbilityEventArgs ev)
    {
        if (ev.Player == Owner && ev.AbilityType == AbilityType.LEGEND_SCP106_FLASHBACK)
            ev.IsAllowed = false;
    }

    private void OnHurting(HurtingEventArgs ev)
    {
        if (Owner.Role is not Scp106Role scp106 ||
            ev.Attacker == null ||
            ev.Attacker.ReferenceHub != scp106.Owner.ReferenceHub ||
            ev.DamageHandler.Type != DamageType.Scp106 ||
            _isExecuting)
        {
            return;
        }

        // [영웅] 신성방어 능력을 가지고 있다면 최대 HP의 100%만큼 추가 피해
        if (ev.Player.HasAbility(AbilityType.EPIC_HOLYPROTECTION))
            ApplyLethalDamage.Apply(Owner, ev.Player);

        // ReceivingEffect 이벤트를 거치지 않아 디버프 면역 능력이 차단할 수 없다.
        if (!ev.Player.TryGetEffect(EffectType.Traumatized, out StatusEffectBase traumatized))
            return;

        traumatized.ServerSetState(1);

        // ServerShoot은 SCP-106이 현재 조준 중인 대상만 처리하므로, 범위 공격은
        // 회상과 동일하게 외상을 강제한 뒤 대상에게 직접 처치 판정을 적용한다.
        if (_flashbackTargets.Contains(ev.Player))
        {
            ev.Player.Kill(new ScpDamageHandler(
                Owner.ReferenceHub,
                DeathTranslations.PocketDecay));
            return;
        }

        if (!scp106.SubroutineModule.TryGetSubroutine<Scp106Attack>(out var attack))
            return;

        _isExecuting = true;
        try
        {
            attack.ServerShoot();
        }
        finally
        {
            _isExecuting = false;
        }
    }
}