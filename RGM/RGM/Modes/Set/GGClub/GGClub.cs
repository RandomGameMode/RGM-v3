using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using MEC;
using Mirror;
using PlayerRoles;
using UnityEngine;
using RGM.API.Features;

using Exiled.Events.EventArgs.Server;

using static RGM.Variables.Variable;

namespace RGM.Modes
{
    [Mode(ModeCategory.Public, ModeInfo.Set, ModeType.GGClub)]
    internal class GGClub : Mode
    {
        public override string Name => "GG 클럽";
        public override string Description => "빠르게 황금색 플랫폼을 사수하세요!";

        public override string Detail =>
            """
            <b><color=#F00000>페</color><color=#F54900>이</color><color=#FA9300>즈</color></b>가 총 10개로 이루어져 있습니다!

            순발력을 마음껏 뽐내 보세요!
            """;

        public override string Color => "C8FE2E";
        public override string Map => "GGClub";

        public static GGClub Instance;

        private readonly List<Player> _pl = [];
        private List<Transform> _clubLights = [];
        private List<Transform> _pads = [];
        private readonly List<Transform> _goldPads = [];

        private bool _isSongStopped;
        private int _phase = 1;

        private CoroutineHandle _onModeStarted;
        private CoroutineHandle _dj;
        private CoroutineHandle _gingerbreadHint;
        private CoroutineHandle _showPhase;

        private AudioClipPlayback _audio;

        public override void OnEnabled()
        {
            Round.IsLocked = true;
            Respawn.PauseWaves();
            Server.FriendlyFire = true;

            Exiled.Events.Handlers.Player.Spawned += OnSpawned;
            Exiled.Events.Handlers.Player.Died += OnDied;
            Exiled.Events.Handlers.Player.Hurting += OnHurting;
            Exiled.Events.Handlers.Player.SpawnedRagdoll += OnSpawnedRagdoll;

            Exiled.Events.Handlers.Server.RoundEnded += OnRoundEnded;

            _onModeStarted = Timing.RunCoroutine(OnModeStarted());
            _dj = Timing.RunCoroutine(Dj());
            _gingerbreadHint = Timing.RunCoroutine(GingerbreadHint());
            _showPhase = Timing.RunCoroutine(ShowPhase());

            _audio = Tools.PlayGlobalAudio("tothemoon", 1, true);
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Player.Spawned -= OnSpawned;
            Exiled.Events.Handlers.Player.Died -= OnDied;
            Exiled.Events.Handlers.Player.Hurting -= OnHurting;
            Exiled.Events.Handlers.Player.SpawnedRagdoll -= OnSpawnedRagdoll;

            Exiled.Events.Handlers.Server.RoundEnded -= OnRoundEnded;

            Timing.KillCoroutines(_onModeStarted);
            Timing.KillCoroutines(_dj);
            Timing.KillCoroutines(_gingerbreadHint);
            Timing.KillCoroutines(_showPhase);

            _audio.IsPaused = true;
        }

        private IEnumerator<float> OnModeStarted()
        {
            PlayerManager.List.CopyTo(_pl);

            yield return Timing.WaitForOneFrame;

            foreach (var player in PlayerManager.List.Where(x => !x.IsNPC).ToList())
            {
                player.Role.Set(RoleTypeId.Scp3114);
                player.Position = new Vector3(77.66434f, 315.4858f, -42.20193f);

                Server.ExecuteCommand($"/speak {player.Id} 1");
                IntercomPlayers.Add(player);
            }

            for (var i = 1; i < 11; i++)
            {
                PlayerManager.List.ToList().ForEach(x => x.AddHint("GG클럽 게임 시작 알림", $"게임이 {11 - i}초 후에 시작됩니다.", 1.2f));

                yield return Timing.WaitForSeconds(1f);
            }

            _clubLights = Tools.GetObjectList("ClubLight");
            _pads = Tools.GetObjectList("Pad");

            while (_phase < 11)
            {
                yield return Timing.WaitForSeconds(11 - _phase);

                _isSongStopped = true;
                _audio.IsPaused = true;

                for (var i = 1; i < 12 - _phase; i++)
                {
                    var goldPad = _pads.GetRandomValue();

                    if (!_goldPads.Contains(goldPad))
                        _goldPads.Add(goldPad);
                }

                yield return Timing.WaitForSeconds(3 - _phase * 0.2f);

                foreach (var player in PlayerManager.List.Where(_pl.Contains))
                    if (Physics.Raycast(player.Position, Vector3.down, out var hit, 3f, (LayerMask)1))
                    {
                        try
                        {
                            if (hit.transform.GetComponent<AdminToys.PrimitiveObjectToy>().NetworkMaterialColor ==
                                UnityEngine.Color.yellow) continue;
                            if (player.IsAlive && !player.IsNPC)
                                player.Kill("황금색 발판을 밟지 못한 자여.");
                        }
                        catch (Exception)
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

                foreach (var pad in _pads)
                    if (pad.GetComponent<AdminToys.PrimitiveObjectToy>().NetworkMaterialColor ==
                        UnityEngine.Color.white)
                        pad.GetComponent<AdminToys.PrimitiveObjectToy>().NetworkMaterialColor = UnityEngine.Color.red;

                yield return Timing.WaitForSeconds(1.25f);

                _isSongStopped = false;
                _audio.IsPaused = false;
                _goldPads.Clear();

                _phase++;
            }

            Round.IsLocked = false;

            if (_pl.Count > 1)
                PlayerManager.List.ToList().ForEach(x => x.AddBroadcast(3, "게임이 종료되었습니다. 그나저나 어떻게 사셨"));
        }

        private IEnumerator<float> GingerbreadHint()
        {
            while (true)
            {
                foreach (var player in PlayerManager.List)
                    player.AddHint("3114 힌트", $"<b>[TIP]</b> gingerbread를 입력하면 춤출 수 있습니다.", 1.2f);

                yield return Timing.WaitForSeconds(1f);
            }
        }

        private IEnumerator<float> Dj()
        {
            yield return Timing.WaitForSeconds(1f);

            while (true)
            {
                if (!_isSongStopped)
                {
                    foreach (var pad in _pads)
                        pad.GetComponent<AdminToys.PrimitiveObjectToy>().NetworkMaterialColor =
                            new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);

                    foreach (var clubLight in _clubLights)
                        clubLight.GetComponent<AdminToys.LightSourceToy>().NetworkLightColor =
                            new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
                }
                else
                {
                    foreach (var pad in _pads)
                        pad.GetComponent<AdminToys.PrimitiveObjectToy>().NetworkMaterialColor = UnityEngine.Color.white;

                    foreach (var goldPad in _goldPads)
                        goldPad.GetComponent<AdminToys.PrimitiveObjectToy>().NetworkMaterialColor =
                            UnityEngine.Color.yellow;

                    foreach (var clubLight in _clubLights)
                        clubLight.GetComponent<AdminToys.LightSourceToy>().NetworkLightColor = UnityEngine.Color.white;
                }

                yield return Timing.WaitForSeconds(1.1f - _phase * 0.1f);
            }
        }

        private IEnumerator<float> ShowPhase()
        {
            while (_phase < 11)
            {
                MultiBroadcast.API.MultiBroadcast.AddMapBroadcast(1,
                    $"<b><color=#FFF700>P</color><color=#FFC516>h</color><color=#FF942C>a</color><color=#FF6242>s</color><color=#FF3158>e</color></b> {_phase}");

                yield return Timing.WaitForSeconds(1f);
            }
        }

        private void OnSpawned(SpawnedEventArgs ev)
        {
            Server.ExecuteCommand($"/speak {ev.Player.Id} 1");
            IntercomPlayers.Add(ev.Player);
        }

        private void OnDied(DiedEventArgs ev)
        {
            if (_pl.Contains(ev.Player))
            {
                _pl.Remove(ev.Player);

                if (_pl.Count < 3)
                    Round.IsLocked = false;
            }
        }

        private void OnHurting(HurtingEventArgs ev)
        {
            if (ev.Player.IsNPC)
                ev.IsAllowed = false;
        }

        private void OnSpawnedRagdoll(SpawnedRagdollEventArgs ev)
        {
            ev.Ragdoll.Destroy();
        }

        private void OnRoundEnded(RoundEndedEventArgs ev)
        {
            List<Player> players = [.. PlayerManager.List.Where(x => x.IsAlive && !x.IsNPC)];

            switch (players.Count)
            {
                case 1:
                    Timing.RunCoroutine(Tools.SetWinner([.. players], 5));
                    break;
                case > 1:
                    Timing.RunCoroutine(Tools.SetWinner([.. players], 1));
                    break;
            }
        }
    }
}
