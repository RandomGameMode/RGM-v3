using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using MEC;
using RGM.API.Features;
using System.Collections.Generic;
using UnityEngine;
using static RGM.Variables.Variable;

namespace RGM.Modes
{
    [Mode(ModeCategory.OnlySub, ModeInfo.Plus, ModeType.SitDown)]
    public class SitDown : Mode
    {
        public override string Name => "뜀박질";
        public override string Description => "일어났다가 앉았다가";

        public override string Detail =>
            """
            인간은 ALT키를 통해 일어났다가 앉을 수 있습니다.
            """;

        public override string Color => "d7dfbd";

        public static SitDown Instance;

        private static readonly Dictionary<Player, PlayerStatus> PlayerStatuses = new();

        private CoroutineHandle _onModeStarted;

        private class PlayerStatus
        {
            public bool IsSitDown { get; set; }
            public bool IsChangingSitDownState { get; set; }
        }

        public override void OnEnabled()
        {
            Exiled.Events.Handlers.Player.TogglingNoClip += OnTogglingNoClip;

            _onModeStarted = Timing.RunCoroutine(OnModeStarted());
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Player.TogglingNoClip -= OnTogglingNoClip;

            Timing.KillCoroutines(_onModeStarted);
        }

        public IEnumerator<float> OnModeStarted()
        {
            foreach (var player in PlayerManager.List) PlayerStatuses.Add(player, new PlayerStatus());

            yield break;
        }

        public void OnVerified(VerifiedEventArgs ev)
        {
            if (!PlayerStatuses.ContainsKey(ev.Player)) PlayerStatuses.Add(ev.Player, new PlayerStatus());
        }

        public static IEnumerator<float> OnTogglingNoClip(TogglingNoClipEventArgs ev)
        {
            if (PlayerStatuses[ev.Player].IsChangingSitDownState || ev.Player.IsJumping ||
                ev.Player.IsNoclipPermitted || !ev.Player.IsHuman) yield break;
            PlayerStatuses[ev.Player].IsChangingSitDownState = true;

            var audioPlayer = AudioPlayer.CreateOrGet($"Player {ev.Player.Nickname}",
                condition: hub => !MuteBGMPlayers.Contains(Player.Get(hub)),
                onIntialCreation: p =>
                {
                    var speaker = p.AddSpeaker("Main", maxDistance: 3);

                    p.transform.parent = ev.Player.GameObject.transform;

                    speaker.transform.parent = ev.Player.GameObject.transform;

                    speaker.transform.localPosition = Vector3.zero;
                });

            if (PlayerStatuses[ev.Player].IsSitDown)
            {
                audioPlayer.TryPlay($"standing");

                while (ev.Player.Scale.y >= 0.65f)
                {
                    ev.Player.Scale = new Vector3(1, ev.Player.Scale.y - 0.01f, 1);

                    yield return Timing.WaitForOneFrame;
                }

                ev.Player.EnableEffect(EffectType.Slowness, 50);
                ev.Player.EnableEffect(EffectType.SilentWalk, 10);

                PlayerStatuses[ev.Player].IsSitDown = false;
            }
            else
            {
                if (!Physics.Raycast(ev.Player.Position, Vector3.up, out var hit, 1, (LayerMask)1))
                {
                    audioPlayer.TryPlay($"sitting");

                    while (ev.Player.Scale.y <= 1)
                    {
                        ev.Player.Scale = new Vector3(1, ev.Player.Scale.y + 0.01f, 1);

                        yield return Timing.WaitForOneFrame;
                    }

                    ev.Player.DisableEffect(EffectType.Slowness);
                    ev.Player.DisableEffect(EffectType.SilentWalk);

                    PlayerStatuses[ev.Player].IsSitDown = true;
                }
            }

            PlayerStatuses[ev.Player].IsChangingSitDownState = false;
        }
    }
}
