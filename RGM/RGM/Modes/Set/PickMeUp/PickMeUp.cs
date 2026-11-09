using Exiled.API.Features.Items;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using MEC;
using UnityEngine;
using Exiled.API.Enums;
using PlayerRoles;

using RGM.API.Features;

using static RGM.Variables.Variable;
using Exiled.API.Features.Doors;
using Exiled.Events.EventArgs.Player;
using Exiled.API.Extensions;
using Exiled.API.Features.Pickups;

namespace RGM.Modes
{
    [Mode(ModeCategory.Public, ModeInfo.Set, ModeType.PickMeUp)]
    public class PickMeUp : Mode
    {
        public override string Name => "픽미업!";
        public override string Description => "누구보다 빠르게 아이템을 사수하세요.";

        public override string Detail =>
            """
            매 라운드마다 유저는 한정된 아이템을 획득해야 살아남을 수 있습니다.

            최후의 1인이 남을 때까지 게임은 계속됩니다.
            """;

        public override string Color => "AEEFC5";
        public override string Map => "PickMeUp";

        public static PickMeUp Instance;

        private static readonly List<ItemType> ItemTypes =
        [
            ItemType.Painkillers,
            ItemType.Coin,
            ItemType.Lantern,
            ItemType.Radio,
            ItemType.KeycardMTFPrivate,
            ItemType.Adrenaline,
            ItemType.Flashlight
        ];

        private static readonly List<Player> PassedPlayers = [];

        private static Vector3 _spawnBase;
        private static Vector3 _spawnX1;
        private static Vector3 _spawnX2;
        private static Vector3 _spawnY1;
        private static Vector3 _spawnY2;
        private static Vector3 _spawnZ1;
        private static Vector3 _spawnZ2;

        private static Vector3 SpawnX()
        {
            return new Vector3(Random.Range(_spawnX1.x, _spawnX2.x), _spawnX1.y, Random.Range(_spawnX1.z, _spawnX2.z));
        }

        private static Vector3 SpawnY()
        {
            return new Vector3(Random.Range(_spawnY1.x, _spawnY2.x), _spawnY1.y, Random.Range(_spawnY1.z, _spawnY2.z));
        }

        private static Vector3 SpawnZ()
        {
            return new Vector3(Random.Range(_spawnZ1.x, _spawnZ2.x), _spawnZ1.y, Random.Range(_spawnZ1.z, _spawnZ2.z));
        }

        private CoroutineHandle _onModeStarted;

        private AudioClipPlayback _audio;

        public override void OnEnabled()
        {
            Server.FriendlyFire = true;
            Round.IsLocked = true;
            Respawn.PauseWaves();
            Door.Get(DoorType.Scp079Second).IsOpen = true;

            Timing.CallDelayed(1, () =>
            {
                _spawnBase = GameObject.Find("SpawnBasePickMeUp").transform.position;
                _spawnX1 = GameObject.Find("SpawnX1").transform.position;
                _spawnX2 = GameObject.Find("SpawnX2").transform.position;
                _spawnY1 = GameObject.Find("SpawnY1").transform.position;
                _spawnY2 = GameObject.Find("SpawnY2").transform.position;
                _spawnZ1 = GameObject.Find("SpawnZ1").transform.position;
                _spawnZ2 = GameObject.Find("SpawnZ2").transform.position;

                Exiled.Events.Handlers.Player.ItemAdded += OnItemAdded;
                Exiled.Events.Handlers.Player.DroppingItem += OnDroppingItem;

                _onModeStarted = Timing.RunCoroutine(OnModeStarted());
            });
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Player.ItemAdded -= OnItemAdded;
            Exiled.Events.Handlers.Player.DroppingItem -= OnDroppingItem;

            Timing.KillCoroutines(_onModeStarted);

            if (_audio != null)
                _audio.IsPaused = true;
        }

        private IEnumerator<float> OnModeStarted()
        {
            foreach (var player in PlayerManager.List) player.Role.Set(RoleTypeId.Tutorial);

            while (!Round.IsEnded)
            {
                Exiled.API.Features.Map.CleanAllItems();
                Exiled.API.Features.Map.CleanAllRagdolls();

                PassedPlayers.Clear();

                foreach (var player in PlayerManager.List)
                {
                    player.Position = _spawnBase;
                    player.ClearInventory();
                }

                var item = ItemTypes.GetRandomValue();

                Vector3 Pos()
                {
                    return new List<Vector3> { SpawnX(), SpawnY(), SpawnZ() }.GetRandomValue();
                }

                for (var i = 0; i < PlayerManager.List.Count(x => x.IsAlive) / 2; i++)
                    Pickup.CreateAndSpawn(item, Pos());

                _audio = GlobalPlayer.TryPlay($"PickMeUp", 2f);

                for (var i = 1; i < 11; i++)
                {
                    foreach (var player in PlayerManager.List)
                        player.AddBroadcast(1, $"<b><size=30>{11 - i}</size></b>");

                    yield return Timing.WaitForSeconds(1f);
                }

                Door.Get(DoorType.Scp079Armory).IsOpen = true;

                for (var i = 1; i < 7; i++)
                {
                    foreach (var player in PlayerManager.List)
                        player.AddBroadcast(1, $"<b><size=30>{7 - i}</size></b>");

                    yield return Timing.WaitForSeconds(1f);
                }

                Door.Get(DoorType.Scp079Armory).IsOpen = false;

                yield return Timing.WaitForSeconds(1);

                foreach (var player in PlayerManager.List.Where(x => x.IsAlive && !PassedPlayers.Contains(x)))
                {
                    var g = (ExplosiveGrenade)Item.Create(ItemType.GrenadeHE, Server.Host);
                    g.FuseTime = 0.1f;
                    g.MaxRadius = 0;
                    g.SpawnActive(player.Position, Server.Host);

                    player.Kill($"굼떠서 사망했습니다.");
                }

                if (PlayerManager.List.Count(x => x.IsAlive) <= 1)
                {
                    Round.IsLocked = false;
                    Timing.RunCoroutine(
                        Tools.SetWinner([PlayerManager.List.Where(x => x.IsAlive).ToList()[0]], 5));

                    yield break;
                }
            }
        }

        private void OnItemAdded(ItemAddedEventArgs ev)
        {
            if (!PassedPlayers.Contains(ev.Player))
                PassedPlayers.Add(ev.Player);
        }

        private void OnDroppingItem(DroppingItemEventArgs ev)
        {
            ev.IsAllowed = false;
        }
    }
}
