using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Exiled.API.Enums;
using Exiled.API.Features.Items;
using Exiled.Events.EventArgs.Player;
using MEC;
using PlayerRoles.PlayableScps;
using ProjectMER.Features;
using ProjectMER.Features.Objects;
using RGM.API.Components;
using RGM.API.Features;
using UnityEngine;

namespace RGM.Modes.Abilities.Rare;

[Ability("크레모아", "크레모아를 설치할 수 있는 동전을 지급받습니다.", AbilityCategory.Rare, AbilityType.RARE_CLAYMORE)]
public class Claymore : Ability
{
    private ushort _claymoreCoinSerial;
    private CoroutineHandle _handle;
    private SchematicObject _schematic;
    private static readonly int excludeMask = ~((1 << 8) | (1 << 2)) & LayerMask.GetMask("Door", "Default");
    private static readonly float distance = 5f;
    private static readonly float ForwardRange = 5.5f;
    private static readonly float BackwardRange = 2f;
    private float _health = 50f;

    private static readonly Dictionary<FirearmType, int> Firearm = new()
    {
        { FirearmType.Com15, 25 },
        { FirearmType.Com18, 25 },
        { FirearmType.FSP9, 22 },
        { FirearmType.Crossvec, 23 },
        { FirearmType.E11SR, 26 },
        { FirearmType.FRMG0, 24 },
        { FirearmType.Revolver, 51 },
        { FirearmType.Shotgun, 8 },
        { FirearmType.AK, 35 },
        { FirearmType.Logicer, 25 },
        { FirearmType.A7, 26 },
        { FirearmType.Com45, 25 },
        { FirearmType.ParticleDisruptor, 250 },
        { FirearmType.Scp127, 30 }
    };

    public override void OnEnabled()
    {
        Item cc = Owner.AddItem(ItemType.Coin);
        _claymoreCoinSerial = cc.Serial;

        Exiled.Events.Handlers.Player.ChangedItem += OnChangedItem;
        Exiled.Events.Handlers.Player.FlippingCoin += OnFlippingCoin;
        Exiled.Events.Handlers.Player.Shot += OnShot;
        Exiled.Events.Handlers.Player.UsingMicroHIDEnergy += OnUsingMicroHIDEnergy;
        Exiled.Events.Handlers.Item.Swinging += OnSwining;
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Player.ChangedItem -= OnChangedItem;
        Exiled.Events.Handlers.Player.FlippingCoin -= OnFlippingCoin;
        Exiled.Events.Handlers.Player.Shot -= OnShot;
        Exiled.Events.Handlers.Player.UsingMicroHIDEnergy -= OnUsingMicroHIDEnergy;
        Exiled.Events.Handlers.Item.Swinging -= OnSwining;

        Timing.KillCoroutines(_handle);

        if (_schematic != null && _schematic)
            _schematic.Destroy();

        _schematic = null;
    }

    private void OnChangedItem(ChangedItemEventArgs ev)
    {
        if (ev.Item?.Serial != _claymoreCoinSerial) return;

        ev.Player.AddHint("동전 사용 설명",
            $"이 동전을 튕기면 <b><color={ABattle.RatingColor["희귀"]}>크레모아</color></color></b> 능력을 사용할 수 있습니다.");
    }

    private void OnFlippingCoin(FlippingCoinEventArgs ev)
    {
        if (_claymoreCoinSerial != ev.Item.Serial) return;

        if (!Tools.TryGetLookFirstPoint(ev.Player, distance, Tools.SurfaceType.Floor, out Vector3 point,
                layerMask: excludeMask))
        {
            ev.Player.AddHint("동전 사용 설명", "벽이나 가파른 경사에는 설치하실 수 없습니다.");
            return;
        }

        ev.Item.Destroy();

        SchematicObject claymore = ObjectSpawner.SpawnSchematic("Claymore", point,
            Quaternion.Euler(0, ev.Player.Rotation.eulerAngles.y, 0));
        _schematic = claymore;
        // claymore.gameObject.AddComponent<BoxColliderThingsComponent>();
        _handle = Timing.CallDelayed(5f, () =>
        {
            // var component = claymore.GetComponent<BoxColliderThingsComponent>();

            // n초 후에 이 블록 안의 코드가 실행됩니다.
            _handle = Timing.RunCoroutine(Corutine(point, claymore));
            // component.TriggerEnter += (sender, args) =>
            // {
            //     var collider = args.Collider;
            //
            //     if (collider.gameObject.GetType() == typeof())
            //     {
            //         
            //     }
            // };
        });
    }

    private IEnumerator<float> Corutine(Vector3 position, SchematicObject schematic)
    {
        Vector3 forward = schematic.transform.forward;
        float maxRange = Mathf.Max(ForwardRange, BackwardRange);
        int visionMask = VisionInformation.VisionLayerMask;

        while (true)
        {
            if (schematic == null || !schematic)
                yield break;

            ExplosiveGrenade eg = (ExplosiveGrenade)Item.Create(ItemType.GrenadeHE);
            eg.FuseTime = 0f;
            eg.MaxRadius = 4.5f;
            if (_health <= 0f)
            {
                Vector3 explosionPosition = schematic.Position;
                schematic.Destroy();
                eg.MaxRadius = 0f;
                eg.SpawnActive(explosionPosition);
                yield break;
            }

            bool ownerValid = Owner != null &&
                              Owner.ReferenceHub != null &&
                              Owner.IsConnected &&
                              Owner.IsAlive;

            foreach (var player in PlayerManager.List.Where(x =>
                         x != null &&
                         x.ReferenceHub != null &&
                         x.IsConnected &&
                         x.IsAlive &&
                         Vector3.Distance(x.Position, position) <= maxRange))
            {
                if (ownerValid)
                {
                    if (player == Owner || !HitboxIdentity.IsEnemy(Owner.ReferenceHub, player.ReferenceHub))
                        continue;
                }

                Vector3 offset = player.Position - position;
                float forwardDistance = Vector3.Dot(forward, offset);
                bool inRange = forwardDistance >= 0
                    ? forwardDistance <= ForwardRange
                    : -forwardDistance <= BackwardRange;

                if (!inRange)
                    continue;

                if (Physics.Linecast(position, player.Position, visionMask))
                    continue;

                eg.SpawnActive(Tools.GetPointForward(schematic.transform, 2.5f), ownerValid ? Owner : null);

                schematic.Destroy();
                yield break;
            }

            yield return Timing.WaitForOneFrame;
        }
    }


    // 데미지 관련 핸들러
    private void OnShot(ShotEventArgs ev)
    {
        if (_schematic == null || !_schematic)
            return;

        if (Physics.Raycast(
                ev.Player.ReferenceHub.PlayerCameraReference.position +
                ev.Player.ReferenceHub.PlayerCameraReference.forward * 0.2f,
                ev.Player.ReferenceHub.PlayerCameraReference.forward, out RaycastHit hit, 1000, (LayerMask)1) &&
            hit.transform.IsChildOf(_schematic.transform))
        {
            _health -= Firearm[ev.Firearm.FirearmType];
            ev.Player.ShowHitMarker();
        }
    }

    private void OnUsingMicroHIDEnergy(UsingMicroHIDEnergyEventArgs ev)
    {
        if (_schematic == null || !_schematic)
            return;

        if (ev.MicroHID.State == InventorySystem.Items.MicroHID.Modules.MicroHidPhase.Firing)
        {
            if (Physics.Raycast(
                    ev.Player.ReferenceHub.PlayerCameraReference.position +
                    ev.Player.ReferenceHub.PlayerCameraReference.forward * 0.2f,
                    ev.Player.ReferenceHub.PlayerCameraReference.forward, out RaycastHit hit, 5, (LayerMask)1) &&
                hit.transform.IsChildOf(_schematic.transform))
            {
                _health -= 120;
                ev.Player.ShowHitMarker();
            }
        }
    }

    private async void OnSwining(Exiled.Events.EventArgs.Item.SwingingEventArgs ev)
    {
        if (_schematic == null || !_schematic)
            return;

        await Task.Delay(300);

        if (_schematic == null || !_schematic ||
            ev.Player == null || ev.Player.ReferenceHub == null || !ev.Player.IsConnected)
            return;

        if (Physics.Raycast(
                ev.Player.ReferenceHub.PlayerCameraReference.position +
                ev.Player.ReferenceHub.PlayerCameraReference.forward * 0.2f,
                ev.Player.ReferenceHub.PlayerCameraReference.forward, out RaycastHit hit, 3, (LayerMask)1) &&
            hit.transform.IsChildOf(_schematic.transform))
        {
            _health -= 50;
            ev.Player.ShowHitMarker();
        }
    }
}