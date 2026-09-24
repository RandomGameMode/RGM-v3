using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using MEC;
using PlayerRoles;
using Exiled.API.Features.Roles;
using RGM.API.Features;
using static RGM.Variables.Variable;
using UnityEngine;
using AdminToys;
using ProjectMER.Features;
using Exiled.API.Extensions;
using ProjectMER.Features.Objects;

using Exiled.API.Enums;
using Mirror;

namespace RGM.Modes
{
    [Mode(ModeCategory.Public, ModeInfo.Set, ModeType.RUN)]
    public class RUN : Mode
    {
        public override string Name => "RUN FOR YOUR LIFE";
        public override string Description => "뛰세요, 따라잡히기 전에!";
        public override string Detail =>
"""
모든 플레이어는 ClassD 가 되며, 뒤따라오는 무언가에 잡히지 않도록 도망가야 합니다.
하지만 앞은 장애물들이 가로막고 있죠. 과연 무사히 도착 지점으로 갈 수 있을까요?
""";
        public override string Color => "da0101";
        public override string Map => "Run!";

        public static RUN Instance;

        List<Transform> lightSources;
        List<string> objects = new()
        {
            "Babel",
            "Bed",
            "Bench",
            "BigDesk",
            "Bin",
            "Box",
            "CabinetHanger",
            "CabinetSink",
            "Chair",
            "ChemicalsCabinet",
            "ControlPanel",
            "Desk",
            "DrawCabinet",
            "Freeze",
            "Lift",
            "OvenWithGasGas",
            "RoundChair",
            "Seat",
            "Sofa",
            "Table",
            "TallBox",
            "ThreadMill",
            "TV",
            "Washer",
            "Whiteboard",
        };
        List<SchematicObject> spawnedObjects = new();

        Vector3 pos;
        Vector3 finalDoor;
        bool hellMode = false;

        CoroutineHandle _onModeStarted;

        AudioClipPlayback audio = null;

        public override void OnEnabled()
        {
            Server.FriendlyFire = true;
            Round.IsLocked = true;
            Respawn.PauseWaves();

            Exiled.Events.Handlers.Player.Died += OnDied;

            _onModeStarted = Timing.RunCoroutine(OnModeStarted());
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Player.Died -= OnDied;

            Timing.KillCoroutines(_onModeStarted);

            if (audio != null) audio.IsPaused = true;

            foreach (var obj in spawnedObjects)
                NetworkServer.Destroy(obj.gameObject);
        }

        private IEnumerator<float> OnModeStarted()
        {
            if (Random.Range(1, 101) <= 44)
            {
                hellMode = true;

                foreach (var player in PlayerManager.List)
                {
                    player.AddBroadcast(10, "<color=red><b><size=25>지옥 모드 활성화</size></b></color>");
                }
            }

            for (int i = 0; i < 400; i++)
            {
                SchematicObject schematic = ObjectSpawner.SpawnSchematic(
                    $"{objects.GetRandomValue()}", 
                    new Vector3(Random.Range(-235.5703f, 121.7918f), Random.Range(336.903f, 346.5427f), Random.Range(-42.98623f, -51.62109f)), 
                    new Quaternion(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360))
                );
                spawnedObjects.Add(schematic);
            }

            lightSources = Tools.GetObjectList("LightSource");
            pos = GameObject.Find("[SP] Base").transform.position;
            finalDoor = GameObject.Find("Final Door").transform.position;

            foreach (var player in PlayerManager.List)
            {
                player.Role.Set(RoleTypeId.ClassD);
                player.EnableEffect(EffectType.MovementBoost, 50);
                player.EnableEffect(EffectType.Lightweight, 50);
                player.Position = pos;
            }

            yield return Timing.WaitForSeconds(9);

            Timing.RunCoroutine(Loop());
            Timing.RunCoroutine(Laser());
            Timing.RunCoroutine(LaserCheck());

            foreach (var player in PlayerManager.List)
            {
                if (player.Role is Scp096Role scp096)
                {
                    scp096.Enrage(1205);
                }
            }

            audio = GlobalPlayer.TryPlay("RUN FOR IT");

            yield return Timing.WaitForSeconds(130);

            Round.IsLocked = false;

            List<Player> players = [.. PlayerManager.List.Where(x => x.IsAlive && !x.IsNPC)];

            switch (players.Count)
            {
                case 1 when hellMode:
                    Timing.RunCoroutine(Tools.SetWinner(players.ToList(), 15));
                    break;
                case > 1 when hellMode:
                    Timing.RunCoroutine(Tools.SetWinner(players.ToList(), 3));
                    break;
                case 1:
                    Timing.RunCoroutine(Tools.SetWinner(players.ToList(), 5));
                    break;
                default:
                    Timing.RunCoroutine(Tools.SetWinner(players.ToList(), 1));
                    break;
            }
        }

        private IEnumerator<float> Loop()
        {
            while (!Round.IsEnded)
            {
                foreach (var light in lightSources)
                {
                    light.GetComponent<LightSourceToy>().NetworkLightColor = new Color(2.55f, 2.55f, 2.55f);
                }

                yield return Timing.WaitForSeconds(1);

                foreach (var light in lightSources)
                {
                    light.GetComponent<LightSourceToy>().NetworkLightColor = new Color(2.18f, 0.01f, 0.01f);
                }

                yield return Timing.WaitForSeconds(1);
            }
        }

        private IEnumerator<float> Laser()
        {
            while (!Round.IsEnded)
            {
                if (hellMode || Random.Range(1, 3) == 1)
                {
                    SchematicObject laser = ObjectSpawner.SpawnSchematic(
                        $"Raser{Random.Range(1, 10)}",
                        new Vector3(finalDoor.x, finalDoor.y - 2.5f, finalDoor.z),
                        new Quaternion(0, new List<int> { 0, 180 }.GetRandomValue(), 0, 0)
                    );

                    spawnedObjects.Add(laser);

                    foreach (var block in laser.AttachedBlocks)
                    {
                        if (block.name == "Oh no")
                            block.GetComponent<PrimitiveObjectToy>().NetworkMaterialColor = new Color(12.5f, 0, 0);
                    }

                    IEnumerator<float> Enumerator()
                    {
                        while (!Round.IsEnded)
                        {
                            laser.Position += new Vector3(hellMode ? -0.12f : -0.1f, 0, 0);

                            yield return Timing.WaitForOneFrame;
                        }
                    }

                    Timing.RunCoroutine(Enumerator());
                }

                yield return Timing.WaitForSeconds(0.8f);
            }
        }

        private static IEnumerator<float> LaserCheck()
        {
            while (!Round.IsEnded)
            {
                foreach (var player in PlayerManager.List.Where(x => x.IsAlive))
                {
                    foreach (var vector in new List<Vector3> { Vector3.down, Vector3.forward, Vector3.back, Vector3.left, Vector3.right })
                    {
                        if (!Physics.Raycast(player.Position, vector, out RaycastHit hit, 0.8f)) continue;
                        if (new List<string> { "Oh no", "BorderForSomething" }.Contains(hit.transform.name))
                            player.Kill("안타깝군요..");
                    }
                }

                yield return Timing.WaitForOneFrame;
            }
        }

        private void OnDied(Exiled.Events.EventArgs.Player.DiedEventArgs ev)
        {
            Timing.CallDelayed(Timing.WaitForOneFrame, () =>
            {
                var players = PlayerManager.List.Where(x => x.IsAlive && !x.IsNPC);

                if (!players.Any())
                {
                    Round.IsLocked = false;
                }
            });
        }
    }
}
