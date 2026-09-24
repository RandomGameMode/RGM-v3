using System;
using Exiled.API.Enums;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Scp106;
using CustomPlayerEffects;
using PlayerRoles.PlayableScps.Scp106;
using PlayerStatsSystem;
using RGM.API.Features;
using RGM.API.DataBases;
using Random = UnityEngine.Random;
using Scp106Role = Exiled.API.Features.Roles.Scp106Role;

namespace RGM.Modes.Abilities.Synergy;

[RequiresAbility(AbilityType.RARE_SALAMANDRA, AbilityType.RARE_UNDINE, AbilityType.RARE_GNOME, AbilityType.RARE_SYLPH)]
[Ability("드루이드",
    """
    <살라만드라, 운디네, 노움, 실프> 4대 정령의 가호가 당신과 함께합니다.
    76% 확률(<color=red>SCP</color>의 경우 49%)로 상대방의 공격을 반사합니다.
    추가로, 4대 정령에 특수 능력이 부여됩니다.
    """,
    AbilityCategory.Synergy, AbilityType.SYNERGY_DRUID)]
public class Druid : Ability
{
    private static bool _isReflecting;

    public override void OnEnabled()
    {
        Exiled.Events.Handlers.Player.Hurting += OnHurting;
        Exiled.Events.Handlers.Scp106.Attacking += OnScp106Attacking;
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Player.Hurting -= OnHurting;
        Exiled.Events.Handlers.Scp106.Attacking -= OnScp106Attacking;
    }

    private void OnHurting(HurtingEventArgs ev)
    {
        if (_isReflecting ||
            ev.Player != Owner ||
            ev.Attacker == null ||
            !HitboxIdentity.IsEnemy(ev.Attacker.ReferenceHub, ev.Player.ReferenceHub) ||
            Datas.BlockDamageTypes.Contains(ev.DamageHandler.Type) ||
            WeakPointAttack.ShouldIgnoreDefenses(ev.Attacker) || 
            ApplyFixedDamage.IsApplying)
            return;

        float reflectChance = ev.Player.IsScpRole() ? 49 : 76;

        if (!(Convert.ToByte(Random.Range(1, 101)) <= reflectChance)) return;
        ev.IsAllowed = false;
        
        _isReflecting = true;
        try
        {
            ev.Attacker.Hit(ev.Player, ev.Amount);
            ev.Attacker.AddHint("드루이드", "당신의 공격이 반사되었습니다.");
            ev.Player.AddHint("드루이드", $"상대의 공격이 반사되었습니다.");
        }
        finally
        {
            _isReflecting = false;
        }
    }

    private void OnScp106Attacking(AttackingEventArgs ev)
    {
        if (_isReflecting ||
            ev.Target != Owner ||
            ev.Player.Role is not Scp106Role scp106 ||
            !HitboxIdentity.IsEnemy(ev.Player.ReferenceHub, ev.Target.ReferenceHub) ||
            WeakPointAttack.ShouldIgnoreDefenses(ev.Player) || 
            ApplyFixedDamage.IsApplying)
            return;

        bool isCorroding = ev.Player.IsEffectActive<Corroding>();
        int attackDamage = 0;
        if (!isCorroding)
        {
            if (!scp106.SubroutineModule.TryGetSubroutine(out Scp106Attack attack))
                return;

            attackDamage = attack._damage;
        }

        float reflectChance = ev.Target.IsScpRole() ? 49 : 76;
        if (Convert.ToByte(Random.Range(1, 101)) > reflectChance)
            return;

        ev.IsAllowed = false;
        _isReflecting = true;
        try
        {
            if (isCorroding)
            {
                ev.Player.EnableEffect(EffectType.PocketCorroding, 1);
            }
            else
            {
                ev.Player.Hurt(new ScpDamageHandler(
                    Owner.ReferenceHub,
                    attackDamage,
                    DeathTranslations.PocketDecay));
                ev.Player.AddEffect(EffectType.Corroding, 1, Scp106Attack.CorrodingTime);
            }

            ev.Player.AddHint("드루이드", "당신의 공격이 반사되었습니다.");
            ev.Target.AddHint("드루이드", "상대의 공격이 반사되었습니다.");
        }
        finally
        {
            _isReflecting = false;
        }
    }
}
