using System.Collections.Generic;
using MEC;
using RGM.API.Features;
using UnityEngine;
using Exiled.API.Features.Items;
using Exiled.Events.EventArgs.Player;
using Exiled.API.Enums;
using Exiled.API.Extensions;

namespace RGM.Modes.Sets.AddScp.Scps
{
    public static class Scp1162
    {
        public static void OnEnabled()
        {
            Exiled.Events.Handlers.Player.DroppedItem += OnDroppedItem;

            Timing.RunCoroutine(OnStarted());
        }

        private static IEnumerator<float> OnStarted()
        {
            Tools.LoadMap("Scp1162");

            yield return 0;
        }

        private static void OnDroppedItem(DroppedItemEventArgs ev)
        {
            if (!Physics.Raycast(
                    ev.Player.ReferenceHub.PlayerCameraReference.position +
                    ev.Player.ReferenceHub.PlayerCameraReference.forward * 0.2f,
                    ev.Player.ReferenceHub.PlayerCameraReference.forward, out var hit, 2)) return;
            if (hit.transform.name != "Scp1162") return;
            var itemList = Tools.EnumToList<ItemType>();
            var item = itemList.GetRandomValue();

            var rand = Random.Range(1, 101);

            if (rand == 77)
            {
                ev.Player.Kill("탕진했습니다...");
            }
            else
            {
                ev.Pickup.Destroy();
                var currentItem = ev.Player.AddItem(item);
                ev.Player.DropItem(currentItem);
            }
        }
    }
}
