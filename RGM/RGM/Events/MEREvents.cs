using AdminToys;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Toys;
using MEC;
using ProjectMER.Events.Arguments;
using RGM.API.Features;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RGM.Events
{
    public static class MEREvents
    {
        private static List<ShootingTargetToy> targets = new();

        public static void OnSchematicSpawned(SchematicSpawnedEventArgs ev)
        {
            /*if (ev.Schematic.Name == "Cafeteria")
            {
                foreach (var targetBlock in ev.Schematic.AttachedBlocks.Where(x => x.name.StartsWith("Target/")))
                {
                    int num = int.Parse(targetBlock.name.Split('/')[1]);

                    PrefabType get()
                    {
                        if (num == 0)
                            return PrefabType.BinaryTarget;

                        if (num == 1)
                            return PrefabType.DBoyTarget;

                        return PrefabType.SportTarget;
                    }

                    ShootingTargetToy target = ShootingTargetToy.Get(PrefabHelper.Spawn(get()).GetComponent<ShootingTarget>());

                    target.Rotation = new Quaternion(0, 180, 0, 0);

                    targets.Add(target);

                    IEnumerator<float> work()
                    {
                        while (targetBlock != null)
                        {
                            Vector3 pos = targetBlock.transform.position;
                            target.Position = pos;

                            yield return Timing.WaitForOneFrame;
                        }
                    }

                    Timing.RunCoroutine(work());
                }
            }
            */

            if (ev.Schematic.Name == "VirtualWorld")
            {
                foreach (var player in PlayerManager.List)
                {
                    player.EnableEffect(EffectType.Lightweight, 1);
                }
            }
        }

        /*
        public static void OnSchematicDestroyed(SchematicDestroyedEventArgs ev) 
        { 
            if (ev.Schematic.Name == "Cafeteria")
            {
                foreach (var target in targets.ToList())
                {
                    target.Destroy();

                    targets.Remove(target);
                }
            }
        }*/
    }
}
