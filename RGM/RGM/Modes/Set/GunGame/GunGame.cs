using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using Exiled.API.Features.Doors;
using MEC;
using UnityEngine;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using PlayerRoles;
using RGM.API.Features;
using static RGM.Variables.Variable;

namespace RGM.Modes
{
    [Mode(ModeCategory.Public, ModeInfo.Set, ModeType.GunGame)]
    internal class GunGame : Mode
    {
        public override string Name => "Gun Game";
        public override string Description => "대인전에 자신 있으신가요? 실력을 증명하기 위해 우승을 차지하세요!";

        public override string Detail =>
            """
            제일버드
            SCP-1509
            입자 분열기
            FR-MG-0
            Logicer
            MTF-E11-SR
            AK
            Crossvec
            A7
            FSP-9
            COM-45
            .44 리볼버
            COM-18
            COM-15
            마이크로 H.I.D
            """;

        public override string Color => "088A08";

        public static GunGame Instance;

        private readonly Dictionary<Player, int> _stage = new();
        private bool _isEnd;

        private readonly List<ItemType> _gunsList =
        [
            ItemType.Jailbird,
            ItemType.SCP1509,
            ItemType.ParticleDisruptor,
            ItemType.GunFRMG0,
            ItemType.GunLogicer,
            ItemType.GunE11SR,
            ItemType.GunAK,
            ItemType.GunCrossvec,
            ItemType.GunA7,
            ItemType.GunFSP9,
            ItemType.GunCom45,
            ItemType.GunRevolver,
            ItemType.GunCOM18,
            ItemType.GunCOM15,
            ItemType.MicroHID,
            ItemType.Lantern
        ];

        private CoroutineHandle _onModeStarted;
        private CoroutineHandle _scoreBoard;

        public override void OnEnabled()
        {
            Server.FriendlyFire = true;
            Round.IsLocked = true;
            Respawn.PauseWaves();

            Exiled.Events.Handlers.Player.Dying += OnDying;
            Exiled.Events.Handlers.Player.Left += OnLeft;

            _onModeStarted = Timing.RunCoroutine(OnModeStarted());
            _scoreBoard = Timing.RunCoroutine(ScoreBoard());
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Player.Dying -= OnDying;
            Exiled.Events.Handlers.Player.Left -= OnLeft;

            Timing.KillCoroutines(_onModeStarted);
            Timing.KillCoroutines(_scoreBoard);
        }

        private IEnumerator<float> OnModeStarted()
        {
            foreach (var door in Door.List.Where(x => x.Zone == ZoneType.HeavyContainment))
                if (door.IsCheckpoint || door.IsElevator)
                    door.Lock(1205, DoorLockType.Lockdown079);

                else
                    door.IsOpen = true;

            foreach (var player in PlayerManager.List)
            {
                if (GodModePlayers.Contains(player))
                    GodModePlayers.Remove(player);

                player.Kill("전장에 뛰어들 준비가 되셨나요?");
            }

            while (!_isEnd)
            {
                foreach (var player in PlayerManager.List)
                    if (_stage.TryGetValue(player, out var count))
                    {
                        if (count >= _gunsList.Count - 1)
                            _isEnd = true;

                        if (player.IsDead)
                            PlayerSpawn(player);
                    }
                    else
                    {
                        _stage.Add(player, 0);
                    }

                yield return Timing.WaitForSeconds(1f);
            }

            var topPlayer = _stage.OrderByDescending(x => x.Value).FirstOrDefault().Key;
            Round.IsLocked = false;

            PlayerManager.List.ToList().ForEach(x => x.AddBroadcast(20,
                $"<b><color=yellow>{topPlayer.DisplayNickname}</color></b>(이)가 <color=#088A08>Gun Game</color>에서 우승했습니다!"));
            Timing.RunCoroutine(Tools.SetWinner([topPlayer], 5));
        }

        private IEnumerator<float> ScoreBoard()
        {
            while (!_isEnd)
            {
                Timing.RunCoroutine(Processing());

                yield return Timing.WaitForSeconds(1f);
            }

            yield break;

            IEnumerator<float> Processing()
            {
                Exiled.API.Features.Map.CleanAllItems();
                Exiled.API.Features.Map.CleanAllRagdolls();

                foreach (var player in PlayerManager.List)
                    if (_stage.ContainsKey(player))
                    {
                        var topPlayer = _stage.OrderByDescending(x => x.Value).FirstOrDefault().Key;
                        player.AddBroadcast(1,
                            $"<size=25><b><color=yellow>선두주자(1st) 플레이어</color> - {topPlayer.Nickname}({_stage[topPlayer]}점)</b></size>\n" +
                            $"<size=20>우승까지 <color=red>{_gunsList.Count - _stage[player] - 1}</color>킬({_stage[player]}점) 남았습니다.</size>");
                    }

                yield return 0f;
            }
        }

        private void PlayerSpawn(Player player)
        {
            var selectedDoor = Door.List.Where(x =>
                !x.IsElevator && !x.IsPartOfCheckpoint && x.Zone == ZoneType.HeavyContainment &&
                !new List<RoomType>
                        { RoomType.Hcz939, RoomType.Hcz079, RoomType.Hcz049, RoomType.Hcz106, RoomType.HczNuke }
                    .Contains(x.Room.Type)).ToList().GetRandomValue();

            player.Role.Set(RoleTypeId.ClassD);
            player.Health = player.MaxHealth;
            player.EnableEffect(EffectType.Flashed, 1, 0.1f);
            player.ClearInventory();
            player.AddItem(_gunsList[_stage[player]]);
            player.Position =
                new Vector3(selectedDoor.Position.x, selectedDoor.Position.y + 2, selectedDoor.Position.z);

            Timing.CallDelayed(1, () =>
            {
                List<ItemType> ammosList =
                [
                    ItemType.Ammo12gauge,
                    ItemType.Ammo762x39,
                    ItemType.Ammo556x45,
                    ItemType.Ammo9x19,
                    ItemType.Ammo44cal
                ];

                foreach (var ammo in ammosList)
                    player.AddItem(ammo, 3);
            });
        }

        private void OnDying(Exiled.Events.EventArgs.Player.DyingEventArgs ev)
        {
            if (!_stage.ContainsKey(ev.Player)) return;
            _stage[ev.Attacker]++;
            ev.Attacker.ClearInventory();
            ev.Attacker.AddItem(_gunsList[_stage[ev.Attacker]]);
            PlayerSpawn(ev.Player);
            ev.IsAllowed = false;
        }

        private static void OnLeft(Exiled.Events.EventArgs.Player.LeftEventArgs ev)
        {
            if (Server.PlayerCount < 2)
                Round.IsLocked = false;
        }
    }
}