// ReSharper disable IteratorNeverReturns

using Exiled.API.Features.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using MEC;
using UnityEngine;
using RGM.API.Features;

using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Scp079;
using Exiled.API.Features.Pickups.Projectiles;
using Exiled.Events.Handlers;
using LabApi.Features.Wrappers;
using PlayerRoles;
using RGM.Patches;
using Item = Exiled.API.Features.Items.Item;
using Player = Exiled.Events.Handlers.Player;

namespace RGM.Modes
{
    [Mode(ModeCategory.Public, ModeInfo.Plus, ModeType.BomberMan)]
    public class BomberMan : Mode
    {
        public override string Name => "봄버맨";
        public override string Description => "무기로는 오직 고폭 수류탄만 사용할 수 있습니다. 무한으로 지급됩니다.";

        public override string Detail =>
            """
            <b>수류탄 지급 규칙</b>

            7초마다 수류탄이 없다면 지급됩니다.
            <color=red>SCP-079</color>의 경우에는 핑을 통해 폭탄을 투하할 수 있습니다. (쿨타임 20초)

            10분에 자동핵이 작동됩니다.
            """;

        public override string Color => "FF8000";
        public override string Suggester => "몬키키";

        public static BomberMan Instance;

        private bool _isScp079Cooldown;

        private CoroutineHandle _onModeStarted;
        private readonly AutoWarhead _autoWarhead = new(10, 1);

        public override void OnEnabled()
        {
            Player.ItemAdded += OnItemAdded;
            Player.PickingUpItem += OnPickingUpItem;
            Player.DroppingItem += OnDroppingItem;
            Player.ThrownProjectile += OnThrownProjectile;

            Scp079.Pinging += OnPinging;

            _onModeStarted = Timing.RunCoroutine(OnModeStarted());
            _autoWarhead.RunCoroutine();
        }

        public override void OnDisabled()
        {
            Player.ItemAdded -= OnItemAdded;
            Player.PickingUpItem -= OnPickingUpItem;
            Player.DroppingItem -= OnDroppingItem;
            Player.ThrownProjectile -= OnThrownProjectile;

            Scp079.Pinging -= OnPinging;

            Timing.KillCoroutines(_onModeStarted);
            _autoWarhead.KillCoroutine();
        }

        private IEnumerator<float> OnModeStarted()
        {
            foreach (var player in PlayerManager.List)
                try
                {
                    foreach (var item in player.Items.Where(x => x.IsFirearm).ToList())
                        player.RemoveItem(item);
                }
                catch (Exception e)
                {
                    Log.Error($"Error on removing items: {e}");
                }

            while (true)
            {
                foreach (var player in PlayerManager.List)
                    if (!player.HasItem(ItemType.GrenadeHE))
                        player.AddItem(ItemType.GrenadeHE);

                yield return Timing.WaitForSeconds(7f);
            }
        }

        private void OnItemAdded(ItemAddedEventArgs ev)
        {
            if (ev.Item is not Firearm firearm) return;
            ev.Player.RemoveItem(ev.Item);

            firearm.Destroy();
        }

        private void OnPickingUpItem(PickingUpItemEventArgs ev)
        {
            if (ev.Pickup.Category == ItemCategory.Firearm)
                ev.IsAllowed = false;
        }

        private void OnDroppingItem(DroppingItemEventArgs ev)
        {
            if (ev.Item.Type == ItemType.GrenadeHE)
                ev.IsAllowed = false;
        }

        private void OnPinging(PingingEventArgs ev)
        {
            if (!_isScp079Cooldown)
                Timing.CallDelayed(0.1f, () =>
                {
                    var g = (ExplosiveGrenade)Item.Create(ItemType.GrenadeHE, ev.Player);
                    g.FuseTime = 3f;
                    g.SpawnActive(ev.Position + Vector3.up, ev.Player);

                    var light = LightSourceToy.Create(ev.Position);
                    light.Position = ev.Position;
                    light.Range = 5;
                    light.Color = new Color(1, 0, 0, 1);
                    light.Rotation = Quaternion.Euler(0, 0, 0);


                    Timing.CallDelayed(5, light.Destroy);

                    _isScp079Cooldown = true;

                    Timing.CallDelayed(20, () => { _isScp079Cooldown = false; });
                });
        }

        private IEnumerator<float> OnThrownProjectile(ThrownProjectileEventArgs ev)
        {
            yield return Timing.WaitForSeconds(0.3f);

            if (ev.Projectile is not ExplosionGrenadeProjectile grenade ||
                ev.Player.Role.Type == RoleTypeId.Scp079) yield break;
            while (!grenade.IsAlreadyDetonated)
            {
                if (Physics.OverlapSphere(grenade.Position, 0.3f).Length > 4)
                    grenade.Base.Network_syncTargetTime = 0.1f;

                yield return Timing.WaitForOneFrame;
            }
        }
    }
}
