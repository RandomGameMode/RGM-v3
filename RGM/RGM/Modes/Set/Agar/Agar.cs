using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Doors;
using Exiled.API.Features.Items;
using Exiled.Events.EventArgs.Player;
using MEC;

using PlayerRoles;
using RGM.API.Features;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static RGM.Variables.Variable;
using Exiled.API.Enums;
using Exiled.Events.EventArgs.Server;
using Decals;

namespace RGM.Modes
{
    [Mode(ModeCategory.Public, ModeInfo.Set, ModeType.Agar)]
    internal class Agar : Mode
    {
        public override string Name => "세력 키우기";
        public override string Description => "SCP-1509를 사용하여 아군을 늘리고 적군을 몰살하세요!";
        public override string Detail => "세력을 키우자~";
        public override string Color => "f1a783";

        public static Agar Instance;

        private readonly List<Player> _teamA = [];
        private readonly List<Player> _teamB = [];

        private CoroutineHandle _onModeStarted;

        public override void OnEnabled()
        {
            _teamA.Clear();
            _teamB.Clear();

            Respawn.PauseWaves();
            Door.List.ToList().ForEach(x => x.Lock(1205, DoorLockType.Lockdown079));

            Exiled.Events.Handlers.Server.RoundEnded += OnRoundEnded;

            Exiled.Events.Handlers.Player.Died += OnDied;
            Exiled.Events.Handlers.Player.DroppingItem += OnDroppingItem;
            Exiled.Events.Handlers.Player.DroppingAmmo += OnDroppingAmmo;
            Exiled.Events.Handlers.Player.Shot += OnShot;

            _onModeStarted = Timing.RunCoroutine(OnModeStarted());
        }

        public override void OnDisabled()
        {
            Respawn.ResumeWaves();

            Exiled.Events.Handlers.Server.RoundEnded -= OnRoundEnded;

            Exiled.Events.Handlers.Player.Died -= OnDied;
            Exiled.Events.Handlers.Player.DroppingItem -= OnDroppingItem;
            Exiled.Events.Handlers.Player.DroppingAmmo -= OnDroppingAmmo;
            Exiled.Events.Handlers.Player.Shot -= OnShot;

            Timing.KillCoroutines(_onModeStarted);
        }

        private IEnumerator<float> OnModeStarted()
        {
            Tools.LoadMap("Battle_AGAR");
            yield return Timing.WaitForSeconds(1f);

            var players = PlayerManager.List.ToList();
            players.ShuffleList();

            var halfCount = players.Count / 2;

            _teamA.AddRange(players.Take(halfCount));
            _teamB.AddRange(players.Skip(halfCount));

            foreach (var player in _teamA)
            {
                player.Role.Set(RoleTypeId.ClassD);
                player.Position = Tools.GetObjectList("Spot A").GetRandomValue().position;
                player.AddItem(ItemType.SCP1509);
            }

            foreach (var player in _teamB)
            {
                player.Role.Set(RoleTypeId.Scientist);
                player.ClearInventory();
                player.Position = Tools.GetObjectList("Spot B").GetRandomValue().position;
                player.AddItem(ItemType.SCP1509);
            }

            while (!Round.IsEnded)
            {
                foreach (var player in PlayerManager.List)
                    player.AddBroadcast(1,
                        $"<size=30><b><color={RoleTypeId.ClassD.GetColor().ToHex()}>{PlayerManager.List.Count(x => x.Role.Type.GetSide() == Side.ChaosInsurgency)}</color> vs <color={RoleTypeId.Scientist.GetColor().ToHex()}>{PlayerManager.List.Count(x => x.Role.Type.GetSide() == Side.Mtf)}</color></b></size>");

                yield return Timing.WaitForSeconds(1f);
            }
        }

        private void OnRoundEnded(RoundEndedEventArgs ev)
        {
            var winner = PlayerManager.List.First(x => x.UserId == PlayersReport
                .OrderByDescending(kv => kv.Value.Damage)
                .Take(1)
                .ToList().First().Key);

            Timing.RunCoroutine(Tools.SetWinner([winner], 5));
        }

        private void OnDied(DiedEventArgs ev)
        {
            if (ev.Attacker != null)
            {
                var reviver = PlayerManager.List.GetRandomValue(x => x.Role.Type == RoleTypeId.Spectator);
                reviver.Role.Set(
                    Tools.EnumToList<RoleTypeId>().GetRandomValue(x => x.GetSide() == ev.Attacker.Role.Type.GetSide()),
                    RoleSpawnFlags.None);
                if (reviver.Role.Team == Team.Dead)
                    reviver.Role.Set(Random.Range(1, 3) == 1 ? RoleTypeId.ClassD : RoleTypeId.Scientist,
                        RoleSpawnFlags.None);
                var item = reviver.AddItem(ItemType.SCP1509);
                reviver.CurrentItem = item;
            }

            Exiled.API.Features.Map.CleanAllItems();
            Exiled.API.Features.Map.CleanAllRagdolls();
            foreach (var decal in new List<DecalPoolType>
                     {
                         DecalPoolType.Bullet,
                         DecalPoolType.GlassCrack,
                         DecalPoolType.Buckshot,
                         DecalPoolType.Blood
                     })
                Exiled.API.Features.Map.Clean(decal);
        }

        private void OnDroppingItem(DroppingItemEventArgs ev)
        {
            ev.IsAllowed = false;
        }

        private void OnDroppingAmmo(DroppingAmmoEventArgs ev)
        {
            ev.IsAllowed = false;
        }

        private void OnShot(ShotEventArgs ev)
        {
            ev.Player.AddAmmo(ev.Firearm.AmmoType, 1);
        }
    }
}
