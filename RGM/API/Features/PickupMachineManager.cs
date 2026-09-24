using CommandSystem;
using Exiled.Events.EventArgs.Player;
using MEC;
using ProjectMER.Features;
using ProjectMER.Features.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RGM.API.Features
{
    /**
     * <summary>뽑기 기계 명령어</summary>
     */
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class SetPickupMachine : ICommand
    {
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            PickupMachineManager.OnEnabled();

            response = $"뽑기 기계를 추가하는데 성공했습니다.";
            return true;
        }

        public string Command => "setpm";

        public string[] Aliases { get; } = ["spm"];

        public string Description => "뽑기 기계ㅣ뽑기 기계를 추가합니다.";
    }
    
    /**
     * <summary>뽑기 기계와 관련된 작업을 처리합니다</summary>
     */
    public static class PickupMachineManager
    {
        /**
         * <summary>시작 시 로직</summary>
         */
        public static void OnEnabled()
        {
            Create(new Vector3(40f, 314.08f, -32.6f));
        }
        
        /**
         * <summary>스키마를 기반으로 뽑기 기계를 주어진 위치에 생성하고 관련된 로직을 처리합니다</summary>
         * <param name="pos">뽑기 기계가 위치할 3차원 위치</param>
         */
        static void Create(Vector3 pos)
        {
            SchematicObject pickupMachine = ObjectSpawner.SpawnSchematic("DollGrabber", pos);
            GameObject grabber = pickupMachine.AttachedBlocks.First(x => x.name == "Grabber");
            int chance = 0;
            int remain = 0;
            bool isRunning = false;

            IEnumerator<float> onCoinInserted()
            {
                while (true)
                {
                    if (isRunning)
                    {
                        remain -= 1;

                        if (remain <= 0)
                        {
                            isRunning = false;

                            yield return Timing.WaitForSeconds(5);
                        }
                    }

                    if (chance > 0 && !isRunning)
                    {
                        chance -= 1;
                        isRunning = true;
                        remain = 20;
                    }

                    yield return Timing.WaitForSeconds(1);
                }
            }

            void OnSearchingPickup(SearchingPickupEventArgs ev)
            {
                if (ev.Pickup.Transform.name == "←")
                {

                }

                if (ev.Pickup.Transform.name.StartsWith("DollGrabberInput"))
                {
                    if (ev.Player.CurrentItem is { Type: ItemType.Coin })
                    {
                        ev.IsAllowed = false;

                        ev.Player.RemoveItem(ev.Player.CurrentItem);

                        Tools.PlaySound(ev.Pickup.Transform, "vm_insert_success", 2);

                        chance += 1;
                    }
                    else
                    {
                        Tools.PlaySound(ev.Pickup.Transform, "vm_insert_fail", 2);
                    }
                }
            }

            Timing.RunCoroutine(onCoinInserted());

            Exiled.Events.Handlers.Player.SearchingPickup += OnSearchingPickup;
        }
    }
}