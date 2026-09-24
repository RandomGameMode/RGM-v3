using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using ProjectMER.Features.Objects;
using ProjectMER.Features;
using MEC;
using RGM.API.Features;
using UnityEngine;

namespace RGM.Modes.Abilities.Legend;

[Ability("Lava Chicken", "6.5m 반경의 적들을 태웁니다.",
    AbilityCategory.Legend, AbilityType.LEGEND_LAVACHICKEN)]
public class LavaChicken : Ability
{
    private CoroutineHandle _onStarted;
    private SchematicObject _lava;

    public override void OnEnabled()
        => _onStarted = Timing.RunCoroutine(OnStarted());

    public override void OnDisabled()
    {
        Timing.KillCoroutines(_onStarted);
        DestroyLava();
    }

    private IEnumerator<float> OnStarted()
    {
        _lava = ObjectSpawner.SpawnSchematic("LavaChicken", new Vector3(1205, 1205, 1205));

        while (IsOwnerValid())
        {
            try
            {
                Vector3 ownerPosition = Owner.Position;

                if (Physics.Raycast(ownerPosition, Vector3.down, out RaycastHit hit, 100, (LayerMask)1))
                {
                    _lava.Position = hit.point;
                }

                foreach (var player in PlayerManager.List.Where(x =>
                             x != null &&
                             x.ReferenceHub != null &&
                             x.IsConnected &&
                             x.IsAlive &&
                             HitboxIdentity.IsEnemy(x.ReferenceHub, Owner.ReferenceHub)))
                {
                    if (Vector3.Distance(player.Position, ownerPosition) > 6.5f) continue;
                    var damage = player.IsScpRole() ? player.MaxHealth * 0.01f : player.MaxHealth * 0.03f;

                    if (player.HasAbility(AbilityType.RARE_UNDINE))
                    {
                        damage /= 1 + (player.HasAbility(AbilityType.SYNERGY_DRUID)
                            ? player.AbilityCount(AbilityType.RARE_UNDINE) * 2
                            : player.AbilityCount(AbilityType.RARE_UNDINE));

                        player.AddHint("운디네",
                            $"<color={ABattle.RatingColor["희귀"]}><b>운디네</b></color>가 화염으로부터 당신을 보호하기 위해 노력하고 있습니다.",
                            0.5f);
                    }

                    Hitmarker.SendHitmarkerDirectly(Owner.ReferenceHub, 0.5f);
                    player.Hit(Owner, damage);
                }
            }
            catch (Exception e)
            {
                Log.Error($"LavaChicken 오류: {e}");
            }

            yield return Timing.WaitForSeconds(0.067f);
        }

        DestroyLava();
    }

    private bool IsOwnerValid()
    {
        return Owner != null &&
               Owner.ReferenceHub != null &&
               Owner.IsConnected &&
               Owner.IsAlive;
    }

    private void DestroyLava()
    {
        if (_lava == null || !_lava)
            return;

        _lava.Destroy();
        _lava = null;
    }
}