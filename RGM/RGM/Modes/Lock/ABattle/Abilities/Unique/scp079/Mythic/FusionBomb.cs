using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.Events.EventArgs.Scp079;
using MEC;
using System.Collections.Generic;
using UnityEngine;
using System;
using ProjectMER.Features.Objects;
using ProjectMER.Features;

namespace RGM.Modes.Abilities.Unique.Scp079.Mythic;


[Ability("융단 폭격",
    """
    핑을 찍은 지점 20m 이내에서 10초간 0.1초 간격으로 미사일이 쏟아집니다. (쿨타임 15초, 중복 불가)
    해당 능력으로 적 처치 시 대상을 049-2로 변환합니다.
    """,
    AbilityCategory.Mythic,
    AbilityType.MYTHIC_SCP079_FUSIONBOMB,
    RoleAbility.Scp079)]
public class FusionBomb : Ability
{
    private static bool _isScp079Cooldown;
    
    public override void OnEnabled()
    {
        Owner.AddAbility(AbilityType.NORMAL_SCP0492_INFECTION);
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
                    Vector3 centerPos = ev.Position + new Vector3(0, 0.1f, 0);
                    Timing.RunCoroutine(StartBombardment(centerPos));

                    Timing.CallDelayed(15f, () =>
                    {
                        _isScp079Cooldown = false;
                    });
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
        float duration = 10f;
        float interval = 0.1f;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * 20f;
            Vector3 randomTargetPos1 = centerPos + new Vector3(randomCircle.x, 0f, randomCircle.y);
            Vector3 randomTargetPos2 = centerPos + new Vector3(randomCircle.x, 0f, randomCircle.y);

            Timing.RunCoroutine(Boom(randomTargetPos1));
            Timing.RunCoroutine(Boom(randomTargetPos2));

            yield return Timing.WaitForSeconds(interval);
            elapsedTime += interval;
        }
    }

    private IEnumerator<float> Boom(Vector3 pos)
    {
        Vector3 RealPosition = pos;

        if (Physics.Raycast(pos, Vector3.down, out RaycastHit hitDown, 100f, (LayerMask)1))
        {
            RealPosition = hitDown.point;
        }

        else if (Physics.Raycast(pos, Vector3.up, out RaycastHit hitUp, 100f, (LayerMask)1))
        {
            RealPosition = hitUp.point;
        }

        SchematicObject Missile = ObjectSpawner.SpawnSchematic("Missile", RealPosition);
        yield return Timing.WaitForSeconds(0.15f);
        if (Missile != null) 
            Missile.Destroy();

        var g = (ExplosiveGrenade)Item.Create(ItemType.GrenadeHE, Owner);
        if (g != null)
        {
            g.FuseTime = 0.1f;
            g.SpawnActive(RealPosition, Owner);
        }
    }
}