using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using RGM.API.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using PlayerStatsSystem;
using ProjectMER.Features;
using ProjectMER.Features.Objects;
using MEC;
using UnityEngine;

using static RGM.Variables.Variable;

using RGM.API.DataBases;
using Exiled.API.Features.Roles;
using AdminToys;
using InventorySystem.Items.Firearms.ShotEvents;
using InventorySystem.Items;
using Exiled.Events.EventArgs.Player;
using Mirror;

namespace RGM.Donator
{
    public class Main
    {
        public static Main Instance;

        public void OnEnabled()
        {
            Exiled.Events.Handlers.Player.Dying += OnDying;
            Exiled.Events.Handlers.Player.Spawned += OnSpawned;

            Timing.RunCoroutine(CustomermizingRotation());
        }

        public void PlaySound(Vector3 pos, string clip, float volume = 1)
        {
            AudioPlayer audioPlayer = AudioPlayer.CreateOrGet($"{UnityEngine.Random.Range(1, 10000001)}", condition: (ReferenceHub hub) =>
            {
                return !MuteBGMPlayers.Contains(Player.Get(hub));
            }, onIntialCreation: (p) =>
            {
                Speaker speaker = p.AddSpeaker("Main", isSpatial: true, minDistance: 1, maxDistance: 5);

                speaker.transform.position = pos;
            });

            audioPlayer.TryPlay($"Effect-{clip}", volume: volume);
        }

        public IEnumerator<float> KillEffect(string kE, Player attacker, Player player, Role _role, Vector3 _pos)
        {
            Quaternion rot = attacker.Rotation;

            if (kE == "영혼 가출")
            {
                PlaySound(_pos, "1", 4);

                DamageHandlerBase disruptorDamage = new DisruptorDamageHandler(new DisruptorShotEvent(ItemIdentifier.None, attacker.Footprint, InventorySystem.Items.Firearms.Modules.DisruptorActionModule.FiringState.FiringSingle), player.Position, -1);

                Ragdoll.CreateAndSpawn(_role.Type, kE, disruptorDamage, _pos, rot);
            }

            if (kE == "솔라 테라")
            {
                PlaySound(_pos, "2", 10);

                SchematicObject SolarTerra = ObjectSpawner.SpawnSchematic("SolarTerra", _pos, rot);

                Timing.CallDelayed(1.5f, SolarTerra.Destroy);
            }

            if (kE == "Kerfus")
            {
                PlaySound(_pos, "3", 10);

                SchematicObject Kerfus = ObjectSpawner.SpawnSchematic("Kerfusa", _pos + new Vector3(0, 19, 0), rot);

                List<PrimitiveObjectToy> primitiveObjects = new List<PrimitiveObjectToy>();

                void applyPrimitiveFlags(Transform parentTransform)
                {
                    foreach (Transform childTransform in parentTransform)
                    {
                        PrimitiveObjectToy primitiveObject = childTransform.GetComponent<PrimitiveObjectToy>();

                        if (primitiveObject != null)
                        {
                            primitiveObject.PrimitiveFlags = PrimitiveFlags.Visible;
                            primitiveObject.UpdatePositionClient();
                            primitiveObject.UpdatePositionServer();
                        }

                        applyPrimitiveFlags(childTransform);
                    }
                }

                applyPrimitiveFlags(Kerfus.transform);

                for (int i = 1; i < 11; i++)
                {
                    Kerfus.Position += new Vector3(0, -2f, 0);

                    yield return Timing.WaitForSeconds(0.05f);
                }

                yield return Timing.WaitForSeconds(1.5f);

                for (int i = 1; i < 11; i++)
                {
                    Kerfus.Position += new Vector3(0, 2f, 0);

                    yield return Timing.WaitForSeconds(0.05f);
                }

                Kerfus.Destroy();
            }

            if (kE == "은제 말뚝")
            {
                PlaySound(_pos, "4", 7.5f);

                SchematicObject SilverStake = ObjectSpawner.SpawnSchematic("SilverStake", _pos, rot);

                Timing.CallDelayed(1.5f, SilverStake.Destroy);
            }

            if (kE == "KO 사인")
            {
                PlaySound(_pos, "5", 7.5f);

                SchematicObject KO = ObjectSpawner.SpawnSchematic("KO", _pos, rot);

                Timing.CallDelayed(1.5f, KO.Destroy);
            }

            if (kE == "크리스마스 트리")
            {
                PlaySound(_pos, "6", 2);

                SchematicObject XmasTree = ObjectSpawner.SpawnSchematic("XmasTree", new Vector3(_pos.x, _pos.y - 0.9f, _pos.z), rot);

                Timing.CallDelayed(1.9f, XmasTree.Destroy);
            }

            if (kE == "크리스마스 볼")
            {
                PlaySound(_pos, "7", 2);

                SchematicObject XmasTree = ObjectSpawner.SpawnSchematic("XmasBall", new Vector3(_pos.x, _pos.y - 0.9f, _pos.z), rot);

                Timing.CallDelayed(1.9f, XmasTree.Destroy);
            }

            if (kE == "철퇴")
            {
                PlaySound(_pos, "8", 10);

                SchematicObject Hammer = ObjectSpawner.SpawnSchematic("Hammer", new Vector3(_pos.x, _pos.y - 0.9f, _pos.z), rot);

                Timing.CallDelayed(1.5f, Hammer.Destroy);
            }

            if (kE == "수렴형 레이저")
            {
                PlaySound(_pos, "9", 2);

                SchematicObject Raser = ObjectSpawner.SpawnSchematic("Raser", new Vector3(_pos.x, _pos.y - 0.9f, _pos.z), rot);

                Timing.CallDelayed(1.5f, Raser.Destroy);
            }

            if (kE == "5월 5일")
            {
                PlaySound(_pos, "11", 3);

                SchematicObject ChildrenDay = ObjectSpawner.SpawnSchematic("ChildrenDay", new Vector3(_pos.x, _pos.y - 0.9f, _pos.z), rot);

                Timing.CallDelayed(1.5f, ChildrenDay.Destroy);
            }

            if (kE == "카피바라")
            {
                PlaySound(_pos, "12", 3);

                CapybaraToy capybara = PrefabHelper.Spawn(PrefabType.CapybaraToy, _pos).GetComponent<CapybaraToy>();

                capybara.NetworkCollisionsEnabled = false;
                capybara.NetworkMovementSmoothing = 100;
                capybara.NetworkScale = new Vector3(1, 1, 1);

                void update(Vector3 pos, Quaternion rot)
                {
                    capybara.NetworkPosition = pos;
                    capybara.NetworkRotation = rot;
                    capybara.UpdatePositionClient();
                    capybara.UpdatePositionServer();
                }

                Vector3 pos1 = _pos + new Vector3(0, -1, 0);
                Quaternion rot1 = Vector3.Distance(attacker.Position, capybara.NetworkPosition) < 1.2f
                                ? Quaternion.Euler(0, attacker.Rotation.eulerAngles.y, 0)
                                : Quaternion.LookRotation(new Vector3(attacker.Position.x - capybara.transform.position.x, 0, attacker.Position.z - capybara.transform.position.z));

                update(pos1, rot1);

                Timing.CallDelayed(1.5f, () =>
                {
                    NetworkServer.Destroy(capybara.gameObject);
                });

                for (int i = 0; i < 36; i++)
                {
                    update(pos1, rot1 *= Quaternion.Euler(0, 10f, 0));

                    yield return Timing.WaitForSeconds(0.03f);
                }
            }

            if (kE == "찰칵")
            {
                PlaySound(_pos, "13", 3);

                SchematicObject ChildrenDay = ObjectSpawner.SpawnSchematic("Camera", new Vector3(_pos.x, _pos.y, _pos.z), rot);

                Timing.CallDelayed(1.5f, ChildrenDay.Destroy);
            }

            if (kE == "SCP999")
            {
                SchematicObject SCP999 = ObjectSpawner.SpawnSchematic("SCP_999_ke", new Vector3(_pos.x, _pos.y, _pos.z), rot);
                    
                Timing.CallDelayed(0.84f, () => PlaySound(_pos, "14", 3));
                Timing.CallDelayed(3.17f, SCP999.Destroy);
            }

            if (kE == "Lightning")
            {
                PlaySound(_pos, "15", 3);
                SchematicObject Lightning1 = ObjectSpawner.SpawnSchematic("LightningKE", new Vector3(_pos.x, _pos.y, _pos.z), rot);
                yield return Timing.WaitForSeconds(0.25f);
                Lightning1.Destroy();

                SchematicObject Lightning2 = ObjectSpawner.SpawnSchematic("LightningKE", new Vector3(_pos.x, _pos.y, _pos.z), rot * Quaternion.Euler(0f, 180f, 0f));
                yield return Timing.WaitForSeconds(0.25f);
                Lightning2.Destroy();
            }
        }

        public IEnumerator<float> SpawnEffect(string sE, Player player, Vector3 _pos)
        {
            if (sE == "Connected")
            {
                PlaySound(_pos, "10", 2);

                SchematicObject Connected = ObjectSpawner.SpawnSchematic("Connected", _pos);

                Timing.CallDelayed(1.5f, Connected.Destroy);
            }

            yield break;
        }

        public IEnumerator<float> OnDying(DyingEventArgs ev)
        {
            Role _role = ev.Player.Role;
            Vector3 _pos = ev.Player.Position;

            yield return Timing.WaitForOneFrame;

            if (ev.Player.IsDead)
            {
                if (ev.Attacker != null && UsersManager.UsersCache.ContainsKey(ev.Attacker.UserId))
                {
                    List<string> AttackerData = UsersManager.UsersCache[ev.Attacker.UserId];
                    string kE = AttackerData[4];

                    if (AttackerData[15] == "1" && AttackerData[3] != "0")
                    {
                        kE = AttackerData[3].Split('/').GetRandomValue();
                    }

                    if (kE != "0")
                    {
                        Timing.RunCoroutine(KillEffect(kE, ev.Attacker, ev.Player, _role, _pos));

                        string attackerFormatted = $"{Tools.BadgeFormat(ev.Attacker)}<color=#CEF6F5><i>{ev.Attacker.DisplayNickname}</i></color>";
                        string targetFormatted = $"{Tools.BadgeFormat(ev.Player)}<color=#CEF6F5>{ev.Player.DisplayNickname}</color>";

                        string broadcastMsg = kE switch
                        {
                            "SCP999" => $"<size=25>{attackerFormatted}의 {Datas.KillEffectData[kE][0]}가 {targetFormatted}(을)를 꼭 안아줬습니다!</size>",
                            "Lightning" => $"<size=25>{attackerFormatted}(이)가 {targetFormatted}(을)를 {Datas.KillEffectData[kE][1]}시켰습니다!</size>",

                            _ => $"<size=25>{attackerFormatted}(이)가 {Datas.KillEffectData[kE][0]}(으)로 {targetFormatted}(을)를 {Datas.KillEffectData[kE][1]}시켰습니다!</size>"
                        };

                        foreach (var _player in PlayerManager.List.Where(x => x.IsDead || Vector3.Distance(x.Position, _pos) <= 10 || Vector3.Distance(x.Position, ev.Attacker.Position) <= 10))
                        {
                            _player.AddBroadcast(6, broadcastMsg, tag: "kill");
                        }
                    }
                }
            }

            yield break;
        }

        public IEnumerator<float> OnSpawned(SpawnedEventArgs ev)
        {
            if (ev.Player == null || ev.Player.IsNPC || string.IsNullOrEmpty(ev.Player.UserId))
                yield break;

            if (ev.Player.IsAlive)
            {
                if (Physics.Raycast(ev.Player.Position, Vector3.down, out RaycastHit hit, 100, (LayerMask)1))
                {
                    Vector3 _pos = hit.point;

                    if (!UsersManager.UsersCache.TryGetValue(ev.Player.UserId, out List<string> Data))
                        yield break;

                    string sE = Data[20];

                    if (Data[21] == "1" && Data[19] != "0")
                    {
                        sE = Data[19].Split('/').GetRandomValue();
                    }

                    if (sE != "0")
                    {
                        Timing.RunCoroutine(SpawnEffect(sE, ev.Player, _pos));
                    }
                }

                yield break;
            }
        }

        public IEnumerator<float> CustomermizingRotation()
        {
            while (true)
            {
                try
                {
                    foreach (var player in Player.List.Where(x => !x.IsNPC))
                    {
                        if (UsersManager.UsersCache.ContainsKey(player.UserId))
                        {
                            List<string> userValues = UsersManager.UsersCache[player.UserId];

                            if (userValues[5] != "0")
                                player.DisplayNickname = Tools.CustomFormatter(player, userValues[5]);

                            else
                                player.DisplayNickname = "";

                            if (userValues[6] != "0")
                                player.CustomInfo = Tools.CustomFormatter(player, userValues[6]);

                            else
                                player.CustomInfo = "";
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }

                yield return Timing.WaitForSeconds(1f);
            }
        }
    }
}
