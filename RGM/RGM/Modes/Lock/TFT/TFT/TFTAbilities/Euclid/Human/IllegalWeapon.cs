using Exiled.API.Features.Pickups.Projectiles;
using Exiled.Events.EventArgs.Player;
using MEC;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RGM.Modes.Lock.TFT.Euclid.Human;

[TFTAbility("폭탄병", "고폭 수류탄이 지면에 닿으면 즉시 폭발합니다. 고폭 수류탄을 획득합니다.", TFTAbilityLevel.Euclid, TFTAbilityCategory.Human, TFTAbilityPoint.Continuous, TFTAbilityType.IllegalWeapon, "💢")]
public class IllegalWeapon : TFTAbility
{
    public override void OnEnabled()
    {
        Owner.AddItem(ItemType.GrenadeHE);

        Exiled.Events.Handlers.Player.ThrownProjectile += OnThrownProjectile;
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Player.ThrownProjectile -= OnThrownProjectile;
    }

    private IEnumerator<float> OnThrownProjectile(ThrownProjectileEventArgs ev)
    {
        if (ev.Player != Owner) yield break;
        yield return Timing.WaitForSeconds(0.3f);

        if (ev.Projectile is not ExplosionGrenadeProjectile grenade ||
            ev.Player.Role.Type == PlayerRoles.RoleTypeId.Scp079) yield break;
        while (!grenade.IsAlreadyDetonated)
        {
            if (Physics.OverlapSphere(grenade.Position, 0.3f).Length > 4)
            {
                grenade.Base.Network_syncTargetTime = 0.1f;
            }

            yield return Timing.WaitForOneFrame;
        }
    }
}
