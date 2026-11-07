using System.Collections.Generic;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.Events.EventArgs.Player;
using MEC;
using PlayerRoles;
using UnityEngine;
using Random = UnityEngine.Random;

using static RGM.Variables.Variable;

namespace RGM.Modes.Abilities.Epic;

[Ability("수어사이드 봄버맨", """
                      사망할 경우 즉시 폭발합니다. 55% 확률로 연쇄 폭발이 일어납니다.
                      연쇄 폭발 시 각 연쇄 마다 폭발 데미지가 20%씩 감소합니다.
                      """, AbilityCategory.Epic, AbilityType.EPIC_SUICIDEBOMBER)]
public class SuicideBomber : Ability
{
    private const float ChainDamageMultiplier = 0.8f;
    private const float ExplosionInterval = 0.1f;
    private const float FuseTime = 0.1f;
    private const float DamageWindowSlack = 0.3f;
    private const int ChainChancePercent = 55;

    // 플레이어가 사망하면 ABattle이 곧바로 능력을 제거(OnDisabled)하기 때문에,
    // 연쇄 폭발 상태와 코루틴은 능력 인스턴스와 분리해서 정적으로 관리한다.
    private static readonly List<ChainSession> Sessions = [];

    public override void OnEnabled()
    {
        Exiled.Events.Handlers.Player.Dying += OnDying;
    }

    public override void OnDisabled()
    {
        // 사망 직후 능력이 제거되더라도 진행 중인 연쇄 폭발(Sessions)은 끝까지 유지한다.
        Exiled.Events.Handlers.Player.Dying -= OnDying;
    }

    private void OnDying(DyingEventArgs ev)
    {
        if (ev.Player != Owner)
            return;

        Player owner = Owner;
        Vector3 pos = owner.Position;
        Team ownerTeam = owner.Role.Team;

        Timing.CallDelayed(Timing.WaitForOneFrame, () =>
        {
            if (owner == null || !owner.IsDead) return;

            var session = new ChainSession(owner, ownerTeam, pos);
            StartSession(session);
            Timing.RunCoroutine(ExplodeInChain(session));
        });
    }

    private static void StartSession(ChainSession session)
    {
        if (Sessions.Count == 0)
            Exiled.Events.Handlers.Player.Hurting += OnHurting;

        Sessions.Add(session);
    }

    private static void EndSession(ChainSession session)
    {
        Sessions.Remove(session);

        if (Sessions.Count == 0)
            Exiled.Events.Handlers.Player.Hurting -= OnHurting;

        if (session.Owner != null && GodModePlayers.Contains(session.Owner))
            GodModePlayers.Remove(session.Owner);
    }

    private static void OnHurting(HurtingEventArgs ev)
    {
        if (ev.Player == null ||
            ev.Attacker != Server.Host ||
            ev.DamageHandler.Type != DamageType.Explosion)
            return;

        ChainSession session = FindSession(ev.Player);
        if (session == null)
            return;

        // Host가 던진 폭발 피해를 취소하고, Owner가 공격자인 피해로 다시 적용해
        // 피해량/처치 판정이 Owner에게 귀속되도록 한다.
        ev.IsAllowed = false;

        Player victim = ev.Player;
        if (victim == session.Owner || !victim.IsAlive)
            return;

        // Owner는 이미 사망 상태라 적 판정을 직접 할 수 없으므로 사망 직전의 팀으로 판정한다.
        if (!Server.FriendlyFire && !HitboxIdentity.IsEnemy(session.OwnerTeam, victim.Role.Team))
            return;

        victim.Hurt(
            amount: ev.DamageHandler.Damage * session.DamageMultiplier,
            damageType: DamageType.Explosion,
            attacker: session.Owner);
    }

    // 현재 폭발 중인(피해 판정 시간 내) 세션 중, 피해자와 가장 가까운 세션을 찾는다.
    private static ChainSession FindSession(Player victim)
    {
        ChainSession best = null;
        float bestDistance = float.MaxValue;
        float now = Time.time;

        foreach (ChainSession session in Sessions)
        {
            if (!session.Owner.IsConnected ||
                now < session.WindowStart ||
                now > session.WindowEnd)
                continue;

            float distance = (victim.Position - session.Position).sqrMagnitude;
            if (distance >= bestDistance)
                continue;

            best = session;
            bestDistance = distance;
        }

        return best;
    }

    private static IEnumerator<float> ExplodeInChain(ChainSession session)
    {
        try
        {
            do
            {
                if (Round.IsLobby || Round.IsEnded || !session.Owner.IsConnected)
                    yield break;

                // 소유자는 이미 사망(관전자) 상태라 투척자로 지정하면 폭발이 피해를 주지 못한다.
                // 따라서 Host를 투척자로 사용하고, 피해는 OnHurting에서 Owner에게 귀속시킨다.
                var grenade = (ExplosiveGrenade)Item.Create(ItemType.GrenadeHE, Server.Host);
                grenade.FuseTime = FuseTime;

                session.WindowStart = Time.time;
                session.WindowEnd = Time.time + FuseTime + DamageWindowSlack;

                grenade.SpawnActive(session.Position, Server.Host);

                yield return Timing.WaitForSeconds(ExplosionInterval);
                session.DamageMultiplier *= ChainDamageMultiplier;
            }
            while (Random.Range(1, 101) <= ChainChancePercent);
        }
        finally
        {
            EndSession(session);
        }
    }

    private sealed class ChainSession(Player owner, Team ownerTeam, Vector3 position)
    {
        public Player Owner { get; } = owner;
        public Team OwnerTeam { get; } = ownerTeam;
        public Vector3 Position { get; } = position;
        public float DamageMultiplier { get; set; } = 1f;
        public float WindowStart { get; set; }
        public float WindowEnd { get; set; }
    }
}
