using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using MEC;
using RGM.API.Features;
using UnityEngine;
using Exiled.API.Features.Items;
using Exiled.API.Extensions;
using Exiled.Events.EventArgs.Player;
using InventorySystem.Items.Usables.Scp330;
using Exiled.API.Features.Pickups;

namespace RGM.Modes.Sets.AddScp.Scps
{
    public static class Scp294
    {
        private static Dictionary<ItemType, float> VandingMachineChances { get; set; } = new()
        {
            { ItemType.SCP330, 80 },
            { ItemType.SCP207, 7 },
            { ItemType.Jailbird, 6 },
            { ItemType.AntiSCP207, 6 },
            { ItemType.MicroHID, 1 }
        };

        private static IEnumerator<float> Start(Player player, Transform transform)
        {
            var sound = transform.parent.parent.GetChild(3).GetChild(2);

            if (player.CurrentItem is { Type: ItemType.Coin })
            {
                Tools.PlaySound(sound, "vm_insert_success", 2);

                player.RemoveItem(player.CurrentItem);

                var output = transform.parent.parent.GetChild(3).GetChild(0);

                List<ItemType> list = new();

                foreach (var chance in VandingMachineChances)
                    for (var i = 0; i < chance.Value; i++)
                        list.Add(chance.Key);

                yield return Timing.WaitForSeconds(0.5f);

                Tools.PlaySound(output, "vm_drop", 2);

                yield return Timing.WaitForSeconds(0.5f);

                var itemType = list.GetRandomValue();

                if (player.IsScpRole())
                {
                    player.AddItem(itemType);
                }
                else
                {
                    if (itemType == ItemType.SCP330)
                    {
                        var scp330 = (Scp330)Item.Create(ItemType.SCP330);
                        scp330.AddCandy(Tools.EnumToList<CandyKindID>().GetRandomValue());
                        scp330.RemoveCandy(scp330.Candies.ToList()[0]);

                        scp330.CreatePickup(output.position);
                    }
                    else
                    {
                        Pickup.CreateAndSpawn(itemType, output.position);
                    }
                }
            }
            else
            {
                Tools.PlaySound(sound, "vm_insert_fail", 2);
            }
        }

        public static void OnEnabled()
        {
            Exiled.Events.Handlers.Player.SearchingPickup += OnSearchingPickup;
            Exiled.Events.Handlers.Player.FlippingCoin += OnFlippingCoin;
            Exiled.Events.Handlers.Player.DroppingItem += OnDroppingItem;

            Timing.RunCoroutine(OnStarted());
        }

        private static IEnumerator<float> OnStarted()
        {
            Tools.LoadMap("vm");

            foreach (var pickup in Pickup.List.ToList())
                try
                {
                    if (UnityEngine.Random.Range(1, 11) != 1) continue;
                    var coin = Item.Create(ItemType.Coin);
                    coin.CreatePickup(pickup.Transform.position + new Vector3(0, 0.1f, 0));
                }
                catch (Exception e)
                {
                    Log.Error($"[PlanB] Error while creating coin: {e}");
                }

            yield return 0;
        }

        private static void OnSearchingPickup(SearchingPickupEventArgs ev)
        {
            if (!ev.Pickup.Transform.name.StartsWith("Input")) return;
            ev.IsAllowed = false;

            Timing.RunCoroutine(Start(ev.Player, ev.Pickup.Transform));
        }

        private static void OnFlippingCoin(FlippingCoinEventArgs ev)
        {
            if (!Physics.Raycast(
                    ev.Player.ReferenceHub.PlayerCameraReference.position +
                    ev.Player.ReferenceHub.PlayerCameraReference.forward * 0.2f,
                    ev.Player.ReferenceHub.PlayerCameraReference.forward, out var hit, 10)) return;
            var transform = hit.transform;

            if (!transform.name.StartsWith("Input")) return;
            ev.IsAllowed = false;

            Timing.RunCoroutine(Start(ev.Player, transform));
        }

        private static void OnDroppingItem(DroppingItemEventArgs ev)
        {
            if (!Physics.Raycast(
                    ev.Player.ReferenceHub.PlayerCameraReference.position +
                    ev.Player.ReferenceHub.PlayerCameraReference.forward * 0.2f,
                    ev.Player.ReferenceHub.PlayerCameraReference.forward, out var hit, 10)) return;
            var transform = hit.transform;

            if (!transform.name.StartsWith("Input")) return;
            ev.IsAllowed = false;

            Timing.RunCoroutine(Start(ev.Player, transform));
        }
    }
}
