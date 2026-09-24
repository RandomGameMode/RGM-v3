using Exiled.API.Features;
using MEC;
using RGM.API.Features;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Exiled.API.Enums;
using Exiled.Events.EventArgs.Player;
using static RGM.Variables.Variable;

namespace RGM.Modes.Abilities.Mythic;

[Ability("패왕색 패기", "누군가가 당신을 쳐다본다면, 그 사람은 이제 없는 존재가 되겠지요!", 
    AbilityCategory.Mythic, AbilityType.MYTHIC_KINGSCOLOR)]
public class KingsColor : Ability
{
    private CoroutineHandle _king;
    private int _count;
    private LabApi.Features.Wrappers.LightSourceToy _lightSource;

    public override void OnEnabled()
    {
        Exiled.Events.Handlers.Player.Died += OnDied;

        _king = Timing.RunCoroutine(Kingscolor());
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Player.Died -= OnDied;

        Timing.KillCoroutines(_king);

        if (_lightSource == null) return;
        _lightSource.Destroy();
        _lightSource = null;
    }

    private IEnumerator<float> Kingscolor()
    {
        _lightSource = LabApi.Features.Wrappers.LightSourceToy.Create();
        _lightSource.Color = Color.red;
        _lightSource.Intensity = 40;
        _lightSource.Range = 10;

        while (Owner.IsAlive)
        {
            foreach (var player in PlayerManager.List.Where(x => x.IsAlive && x != Owner))
            {
                if (!player.TryGetLookPlayer(50f, out Player target, out var hit))
                    continue;

                if (Owner != target || !HitboxIdentity.IsEnemy(player.ReferenceHub, target.ReferenceHub))
                    continue;

                _lightSource.Position = Owner.Position;

                Hitmarker.SendHitmarkerDirectly(Owner.ReferenceHub, 1f);
                player.EnableEffect(EffectType.Slowness, 80, 1f);
                player.CurrentItem = null;
                player.Hit(Owner, target.IsScpRole() ? target.MaxHealth * 0.06f : target.MaxHealth * 0.21f);
            }

            yield return Timing.WaitForSeconds(0.05f);
        }

        if (_lightSource == null) yield break;
        _lightSource.Destroy();
        _lightSource = null;
    }

    private void OnDied(DiedEventArgs ev)
    {
        if (ev.Attacker == null || ev.Attacker != Owner)
            return;

        _count++;

        switch (_count)
        {
            case 5:
            {
                Timing.CallDelayed(Timing.WaitForOneFrame, () =>
                {
                    if (Owner == null || !Owner.IsAlive)
                        return;

                    Tools.PlayGlobalAudio("시산혈해 (屍山血海)", 2.5f);

                    Owner.RankName = "시산혈해 (屍山血海)";
                    Owner.RankColor = "red";

                    foreach (var player in PlayerManager.List.Where(x => x.IsAlive && Owner.LeadingTeam != x.LeadingTeam))
                    {
                        player.EnableEffect(EffectType.SinkHole, 1, 3);
                        player.EnableEffect(EffectType.Blinded, 1, 3);
                    }
                });

                break;
            }
            case 30:
            {
                Timing.CallDelayed(Timing.WaitForOneFrame, () =>
                {
                    if (Owner == null || !Owner.IsAlive)
                        return;

                    foreach (var player in PlayerManager.List.Where(x => x.IsAlive && x != Owner))
                    {
                        player.RemoveAllAbilities();
                        if (GodModePlayers.Contains(player)) GodModePlayers.Remove(player);
                        player.Kill("패기에 의해 공중분해 되었습니다");
                    }
                });

                break;
            }
        }
    }
}
