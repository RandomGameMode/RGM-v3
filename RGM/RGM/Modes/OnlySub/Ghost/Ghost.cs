using Exiled.API.Features.Items;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using MEC;
using RGM.API.Features;
using Exiled.Events.EventArgs.Player;
using Exiled.API.Enums;
using Exiled.API.Features.Doors;
using Exiled.API.Extensions;
using UnityEngine;
using RGM.API.DataBases;

using static RGM.Variables.Variable;

namespace RGM.Modes
{
    [Mode(ModeCategory.OnlySub, ModeInfo.Plus, ModeType.Ghost)]
    public class Ghost : Mode
    {
        public override string Name => "고스트";
        public override string Description => "정체불명의 누군가가 시설의 제어권을 탈취했군요.";

        public override string Detail =>
            """
            1초마다 무언가 이벤트가 일어납니다.
            """;

        public override string Color => "7401DF";

        public static Ghost Instance;

        private CoroutineHandle _onModeStarted;

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

        private IEnumerator<float> OnModeStarted()
        {
            foreach (var player in PlayerManager.List) Spawned(player);

            while (true)
            {
                var a = Random.Range(1, Random.Range(2, Random.Range(3, Random.Range(6, Random.Range(8, 13)))));
                var door = Door.List.GetRandomValue();

                switch (a)
                {
                    case 1:
                        door.Room.Color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
                        break;

                    case 2:
                        if (!door.IsElevator)
                            door.IsOpen = Random.Range(1, 3) == 1;
                        break;

                    case 3:
                        if (door is BreakableDoor breakableDoor)
                            switch (Random.Range(1, 3))
                            {
                                case 1:
                                    breakableDoor.Break();
                                    break;
                                case 2:
                                    breakableDoor.Repair();
                                    break;
                            }

                        break;

                    case 4:
                        door.Lock(Random.Range(1, 121), DoorLockType.AdminCommand);
                        break;

                    case 5:
                        door.Room.TurnOffLights(Random.Range(1, 121));
                        break;

                    case 6:
                        Tools.MessageTranslated("", $".G{Random.Range(1, 101)}");
                        break;

                    case 7:
                        var teslaGate = TeslaGate.AllGates.GetRandomValue();

                        switch (Random.Range(1, 3))
                        {
                            case 1:
                                teslaGate.RpcPlayAnimation();
                                break;

                            case 2:
                                teslaGate.RpcInstantBurst();
                                break;
                        }

                        break;

                    case 8:
                        Warhead.Start();
                        break;

                    case 9:
                        Warhead.Stop();
                        break;

                    case 10:
                        var item = Item.Create(Tools.EnumToList<ItemType>().Where(x => !Datas.ExceptItems.Contains(x))
                            .ToList().GetRandomValue());
                        item.CreatePickup(door.Position, new Quaternion(a, a, a, a));
                        break;

                    case 11:
                        var audioPlayer = AudioPlayer.CreateOrGet($"{Random.Range(1, 10000001)}",
                            condition: (ReferenceHub hub) => !MuteBGMPlayers.Contains(Player.Get(hub)),
                            onIntialCreation: (p) =>
                            {
                                var speaker = p.AddSpeaker("Main", isSpatial: true, minDistance: 1, maxDistance: 10);

                                speaker.transform.position = door.Position;
                            });

                        audioPlayer.TryPlay(AudioClipStorage.AudioClips.Select(x => x.Key).ToList().GetRandomValue());
                        break;

                    case 12:
                        var player = PlayerManager.List.GetRandomValue();

                        player.EnableEffect(EffectType.Slowness, 150, Random.Range(1, 11));
                        break;
                }

                yield return Timing.WaitForOneFrame;
            }
        }

        private void OnSpawned(SpawnedEventArgs ev)
        {
            if (ev.Player.IsAlive)
                Spawned(ev.Player);
        }

        private void Spawned(Player player)
        {
            player.EnableEffect(EffectType.Fade, 100);
        }
    }
}
