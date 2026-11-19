using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.Events.EventArgs.Player;
using MEC;
using RGM.API.Features;
using System.Collections.Generic;

namespace RGM.Modes.Sets.AddScp.Scps
{
    public static class Scp005
    {
        public static Item Create()
        {
            var item = Item.Create(ItemType.Coin);

            Timing.RunCoroutine(DisplayDesc());

            Exiled.Events.Handlers.Player.InteractingDoor += OnInteractingDoor;
            Exiled.Events.Handlers.Player.InteractingLocker += OnInteractingLocker;
            Exiled.Events.Handlers.Player.OpeningGenerator += OnOpeningGenerator;
            Exiled.Events.Handlers.Player.ClosingGenerator += OnClosingGenerator;

            return item;

            void OnClosingGenerator(ClosingGeneratorEventArgs ev)
            {
                if (ev.Player.CurrentItem != item) return;
                ev.IsAllowed = false;

                ev.Generator.IsOpen = true;
            }

            void OnOpeningGenerator(OpeningGeneratorEventArgs ev)
            {
                if (ev.Player.CurrentItem != item) return;
                ev.IsAllowed = false;

                ev.Generator.IsOpen = false;
            }

            void OnInteractingLocker(InteractingLockerEventArgs ev)
            {
                if (ev.Player.CurrentItem != item) return;
                ev.IsAllowed = false;

                ev.InteractingChamber.IsOpen = !ev.InteractingChamber.IsOpen;
            }

            void OnInteractingDoor(InteractingDoorEventArgs ev)
            {
                if (ev.Player.CurrentItem != item) return;
                if (ev.Door.Type == DoorType.Scp079First)
                {
                    ev.Player.AddHint("헤비도어", "이 헤비도어는 능력으로 개폐가 불가능합니다.", 1.2f);
                    return;
                }

                if (Warhead.IsInProgress)
                {
                    ev.Player.AddHint("알파 핵탄투", "알파 핵탄투가 작동 중일때는 문을 개폐할 수 없습니다.", 1.2f);
                    return;
                }

                ev.IsAllowed = false;

                ev.Door.IsOpen = !ev.Door.IsOpen;
            }

            IEnumerator<float> DisplayDesc()
            {
                while (true)
                {
                    foreach (var player in PlayerManager.List)
                        if (player.CurrentItem != null && player.CurrentItem.Serial == item.Serial)
                            player.AddHint("SCP-005",
                                """
                                <size=25>이 동전은 <color=red>SCP-005</color>(<color=#a4fc16>Safe</color>)입니다.</size>
                                <size=20>모든 것을 열 수 있습니다.</size>
                                """, 1.05f);

                    yield return Timing.WaitForSeconds(1);
                }
            }
        }
    }
}
