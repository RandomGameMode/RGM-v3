using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using MEC;
using RGM.API.Features;
using System.Collections.Generic;
using UnityEngine;

namespace RGM.Modes
{
    [Mode(ModeCategory.Private, ModeInfo.Plus, ModeType.GulliversTravels)] //Onlysub
    public class GulliversTravels : Mode
    {
        public override string Name => "걸리버 여행기";
        public override string Description => "모두가 소인화가 되었습니다!";
        public override string Detail =>
"""
크기가 90% 작아지고 점프력이 70% 증가합니다.
""";
        public override string Color => "473417";
        private const float Scale = 0.9f;
        CoroutineHandle _onModeStarted;

        public override void OnEnabled()
        {
            Exiled.Events.Handlers.Player.Spawned += OnSpawned;
            _onModeStarted = Timing.RunCoroutine(OnModeStarted());
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Player.Spawned -= OnSpawned;
            Timing.KillCoroutines(_onModeStarted);
        }

        public IEnumerator<float> OnModeStarted()
        {
            yield return Timing.WaitForSeconds(2f);

            foreach (var player in PlayerManager.List) 
            {
                Spawned(player);
            }

            yield break;
        }
        public void OnSpawned(SpawnedEventArgs ev)
        {
            Spawned(ev.Player);
        }
        public void Spawned(Player player) 
        {
            if (player == null || !player.IsAlive) return;
            player.Scale = new Vector3(player.Scale.x - Scale, player.Scale.y - Scale, player.Scale.z - Scale);
            player.EnableEffect(EffectType.Lightweight, 70);
        }
        
    }
}
