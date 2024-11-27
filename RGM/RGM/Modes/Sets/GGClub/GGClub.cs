using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CustomPlayerEffects;
using CustomRendering;
using Exiled.API.Features;
using Exiled.API.Features.Roles;
using MapEditorReborn.API.Features.Objects;
using MapEditorReborn.API.Features.Serializable;
using MEC;
using Mirror;
using PlayerRoles;
using PluginAPI.Events;
using UnityEngine;
using SCPSLAudioApi.AudioCore;
using MultiBroadcast;
using RGM.API.Features;
using MultiBroadcast.API;
using AudioPlayer.Commands.SubCommands;

namespace RGM.Modes
{
    [Mode(ModeCategory.Public, ModeInfo.Set, ModeType.GGClub)]
    class GGClub : Mode
    {
        public override string Name => "GG 클럽";
        public override string Description => "빠르게 황금색 플랫폼을 사수하세요!";
        public override string Detail =>
"""
<b><color=#F00000>페</color><color=#F54900>이</color><color=#FA9300>즈</color></b>가 총 10개로 이루어져 있습니다!

순발력을 마음껏 뽐내 보세요!
""";
        public override string Color => "C8FE2E";

        public static GGClub Instance;

        public List<Player> pl = new List<Player>();
        public List<Transform> ClubLights = new List<Transform>();
        public List<Transform> Pads = new List<Transform>();
        public List<Transform> goldPads = new List<Transform>();

        public bool IsSongStopped = false;
        public int Phase = 1;

        Player dj;

        public override void OnEnabled()
        {
            Round.IsLocked = true;
            foreach (var spawn in Respawning.WaveManager.Waves) spawn.Destroy();

            Exiled.Events.Handlers.Player.Spawned += OnSpawned;
            Exiled.Events.Handlers.Player.Died += OnDied;
            Exiled.Events.Handlers.Player.Hurting += OnHurting;
            Exiled.Events.Handlers.Player.SpawnedRagdoll += OnSpawnedRagdoll;

            Timing.RunCoroutine(OnModeStarted());
        }

        public IEnumerator<float> OnModeStarted()
        {
            Server.ExecuteCommand($"/mp load GGClub");

            dj = Tools.SpawnDJ("dj", RoleTypeId.Tutorial, new Vector3(76.17068f, 1015.741f, -46.65614f), "dj");

            foreach (var player in Player.List.Where(x => !x.IsNPC).ToList())
            {
                player.Role.Set(RoleTypeId.Scp3114);
                player.Position = new Vector3(74.99881f, 1012.823f, -43.1801f);

                Server.ExecuteCommand($"/speak {player.Id} 1");
            }

            Player.List.CopyTo(pl);

            for (int i = 1; i < 11; i++)
            {
                Player.List.ToList().ForEach(x => x.ShowHint($"게임이 {11 - i}초 후에 시작됩니다.", 1.2f));

                yield return Timing.WaitForSeconds(1f);
            }

            Timing.RunCoroutine(gingerbreadHint());
            Timing.RunCoroutine(DJHeadBanging());
            Timing.RunCoroutine(DJ());
            Timing.RunCoroutine(ShowPhase());

            GGUtils.Gtool.PlaySound("dj", "tothemoon", VoiceChat.VoiceChatChannel.Intercom, 25, true);

            ClubLights = Tools.GetObjectList("ClubLight");
            Pads = Tools.GetObjectList("Pad");

            while (Phase < 11)
            {
                yield return Timing.WaitForSeconds(11 - Phase);

                IsSongStopped = true;

                for (int i = 1; i < 12 - Phase; i++)
                {
                    Transform goldPad = Tools.GetRandomValue(Pads);

                    if (!goldPads.Contains(goldPad))
                        goldPads.Add(goldPad);
                }

                yield return Timing.WaitForSeconds(3 - Phase * 0.2f);

                foreach (var player in Player.List.Where(pl.Contains))
                {
                    if (Physics.Raycast(player.Position, Vector3.down, out RaycastHit hit, 3f, (LayerMask)1))
                    {
                        try
                        {
                            if (hit.transform.GetComponent<PrimitiveObject>().Primitive.Color != UnityEngine.Color.yellow)
                            {
                                if (player.IsAlive && !player.IsNPC)
                                    player.Kill("황금색 발판을 밟지 못한 자여.");
                            }
                        }
                        catch (Exception e)
                        {
                            if (player.IsAlive && !player.IsNPC)
                                player.Kill("황금색 발판을 밟지 못한 자여..");
                        }
                    }
                    else
                    {
                        if (player.IsAlive && !player.IsNPC)
                            player.Kill("황금색 발판을 밟지 못한 자여...");
                    }
                }

                foreach (var Pad in Pads)
                {
                    if (Pad.GetComponent<PrimitiveObject>().Primitive.Color == UnityEngine.Color.white)
                        Pad.GetComponent<PrimitiveObject>().Primitive.Color = UnityEngine.Color.red;
                }

                yield return Timing.WaitForSeconds(1.25f);

                IsSongStopped = false;
                goldPads.Clear();

                Phase++;
            }

            Round.IsLocked = false;

            if (pl.Count > 1)
                Player.List.ToList().ForEach(x => x.AddBroadcast(3, "게임이 종료되었습니다. 그나저나 어떻게 사셨"));
        }

        public IEnumerator<float> gingerbreadHint()
        {
            while (true)
            {
                foreach (var player in Player.List)
                    player.ShowHint($"<b>[TIP]</b> <i>gingerbread</i>를 입력하면 춤출 수 있습니다.", 1.2f);

                yield return Timing.WaitForSeconds(1f);
            }
        }

        public IEnumerator<float> DJHeadBanging()
        {
            yield return Timing.WaitForSeconds(1f);

            bool HeadUp = true;

            while (true)
            {
                if (HeadUp)
                {
                    GGUtils.Gtool.Rotate(dj.ReferenceHub, new Vector3(0, -1f, 0));

                    HeadUp = false;

                    yield return Timing.WaitForSeconds(0.2f);
                }
                else
                {
                    GGUtils.Gtool.Rotate(dj.ReferenceHub, new Vector3(0, 1f, 0));

                    HeadUp = true;

                    yield return Timing.WaitForSeconds(0.15f);
                }
            }
        }

        public IEnumerator<float> DJ()
        {
            yield return Timing.WaitForSeconds(1f);

            while (true)
            {

                if (!IsSongStopped)
                {
                    foreach (var Pad in Pads)
                    {
                        Pad.GetComponent<PrimitiveObject>().Primitive.Color = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
                    }

                    foreach (var ClubLight in ClubLights)
                    {
                        ClubLight.GetComponent<Light>().color = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
                    }
                }
                else
                {
                    foreach (var Pad in Pads)
                    {
                        Pad.GetComponent<PrimitiveObject>().Primitive.Color = UnityEngine.Color.white;
                    }

                    foreach (var Pad in goldPads)
                    {
                        Pad.GetComponent<PrimitiveObject>().Primitive.Color = UnityEngine.Color.yellow;
                    }

                    foreach (var ClubLight in ClubLights)
                    {
                        ClubLight.GetComponent<Light>().color = UnityEngine.Color.white;
                    }
                }

                yield return Timing.WaitForSeconds(1.1f - Phase * 0.1f);
            }
        }

        public IEnumerator<float> ShowPhase()
        {
            while (Phase < 11)
            {
                foreach (var player in Player.List)
                {
                    player.ClearPlayerBroadcasts();
                    player.AddBroadcast(2, $"<b><color=#FFF700>P</color><color=#FFC516>h</color><color=#FF942C>a</color><color=#FF6242>s</color><color=#FF3158>e</color></b> {Phase}");
                }

                yield return Timing.WaitForSeconds(1f);
            }
        }

        public void OnSpawned(Exiled.Events.EventArgs.Player.SpawnedEventArgs ev)
        {
            Server.ExecuteCommand($"/speak {ev.Player.Id} 1");
        }

        public void OnDied(Exiled.Events.EventArgs.Player.DiedEventArgs ev)
        {
            Server.ExecuteCommand($"/speak {ev.Player.Id} 0");

            if (pl.Contains(ev.Player))
            {
                pl.Remove(ev.Player);

                if (pl.Count < 3)
                    Round.IsLocked = false;
            }
        }

        public void OnHurting(Exiled.Events.EventArgs.Player.HurtingEventArgs ev)
        {
            if (ev.Player.IsNPC)
                ev.IsAllowed = false;
        }

        public void OnSpawnedRagdoll(Exiled.Events.EventArgs.Player.SpawnedRagdollEventArgs ev)
        {
            ev.Ragdoll.Destroy();
        }
    }
}
