using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.Events.EventArgs.Scp079;
using MEC;
using System.Collections.Generic;
using UnityEngine;
using System;
using ProjectMER.Features;

namespace RGM.Modes.Abilities.Unique.Scp079.Mythic;

[Ability("융단 폭격",
    """
    핑을 찍은 지점 16m 이내에서 8초간 0.16초 간격으로 미사일이 쏟아집니다. (쿨타임 16초, 중복 불가)
    해당 능력은 『관통』 효과가 적용되며, 적 처치 시 대상을 049-2로 변환합니다.
    """,
    AbilityCategory.Mythic, AbilityType.MYTHIC_SCP079_FUSIONBOMB, RoleAbility.Scp079, isUnique: true)]
public class ClusterBomb : Ability
{
    private static bool _isScp079Cooldown;

    public override void OnEnabled()
    {
        Timing.CallDelayed(0.1f, () =>
        {
            Owner.AddAbility(AbilityType.NORMAL_SCP0492_INFECTION);
            Owner.AddAbility(AbilityType.EPIC_SHARPEYES);
            Owner.AddAbility(AbilityType.EPIC_SHARPEYES);
            Owner.AddAbility(AbilityType.RARE_BULLSEYE);
        });
        Exiled.Events.Handlers.Scp079.Pinging += OnPinging;
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Scp079.Pinging -= OnPinging;
    }

    private void OnPinging(PingingEventArgs ev)
    {
        if (ev.Player == null && ev.Player != Owner) return;

        try
        {
            if (!_isScp079Cooldown)
            {
                _isScp079Cooldown = true;
                Timing.CallDelayed(0.1f, () =>
                {
                    var centerPos = ev.Position + new Vector3(0, 0.1f, 0);
                    Timing.RunCoroutine(StartBombardment(centerPos));

                    Timing.CallDelayed(16f, () => { _isScp079Cooldown = false; });
                });
            }
        }
        catch (Exception e)
        {
            Log.Error($"융단 폭격 오류: {e}");
        }
    }

    private IEnumerator<float> StartBombardment(Vector3 centerPos)
    {
        var duration = 8f;
        var interval = 0.16f;
        var elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            var randomCircle = UnityEngine.Random.insideUnitCircle * 16f;
            var randomTargetPos1 = centerPos + new Vector3(randomCircle.x, 0f, randomCircle.y);
            var randomTargetPos2 = centerPos + new Vector3(randomCircle.x, 0f, randomCircle.y);

            Timing.RunCoroutine(Boom(randomTargetPos1));
            Timing.RunCoroutine(Boom(randomTargetPos2));

            yield return Timing.WaitForSeconds(interval);
            elapsedTime += interval;
        }
    }

    private IEnumerator<float> Boom(Vector3 pos)
    {
        var RealPosition = pos;

        if (Physics.Raycast(pos, Vector3.down, out var hitDown, 100f, (LayerMask)1))
            RealPosition = hitDown.point;

        else if (Physics.Raycast(pos, Vector3.up, out var hitUp, 100f, (LayerMask)1)) RealPosition = hitUp.point;

        var Missile = ObjectSpawner.SpawnSchematic("Missile", RealPosition);
        yield return Timing.WaitForSeconds(0.16f);
        if (Missile != null)
            Missile.Destroy();

        var g = (ExplosiveGrenade)Item.Create(ItemType.GrenadeHE, Owner);
        if (g != null)
        {
            g.FuseTime = 0.05f;
            g.SpawnActive(RealPosition, Owner);
        }
    }
}