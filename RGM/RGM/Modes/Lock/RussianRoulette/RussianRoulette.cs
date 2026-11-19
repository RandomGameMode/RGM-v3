using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using MEC;
using Mirror;
using Exiled.Events.EventArgs.Player;
using PlayerRoles;
using UnityEngine;
using Exiled.API.Enums;
using RGM.API.Features;
using Exiled.API.Features.Items;
using Random = UnityEngine.Random;

using static RGM.Variables.Variable;

namespace RGM.Modes
{
    [Mode(ModeCategory.Public, ModeInfo.Lock, ModeType.RussianRoulette)]
    internal class RussianRoulette : Mode
    {
        public override string Name => "러시안 룰렛";
        public override string Description => "정치질이 난무하는 운과 심리전의 싸움입니다.";

        public override string Detail =>
            """
            그룹 당 6명씩 배정되며, 각 그룹 마다 예선전을 치릅니다.

            예선전에서 통과한 사람은 결선으로 이동하며, 결선에서 최종 승자를 가립니다.

            <size=25>* 자신을 쏘는 경우(허공에 발사하거나 10초를 버틴 경우) 격발 기회를 한번 더 얻습니다.</size>
            <size=25>* 리볼버는 6발을 기준으로 합니다.</size>
            """;

        public override string Color => "F5ECCE";
        public override string Map => "ru";

        public static RussianRoulette Instance;

        private readonly List<Player> _pl = [];
        private readonly List<Player> _finals = [];

        private readonly Dictionary<Vector3, List<Player>> _tablePositions = new();
        private readonly Dictionary<Player, Player> _shotChecks = new();

        private CoroutineHandle _onModeStarted;

        public override void OnEnabled()
        {
            Round.IsLocked = true;
            Respawn.PauseWaves();

            Exiled.Events.Handlers.Player.Shot += OnShot;
            Exiled.Events.Handlers.Player.SearchingPickup += OnSearchingPickup;
            Exiled.Events.Handlers.Player.DroppingItem += OnDroppingItem;
            Exiled.Events.Handlers.Player.Kicking += OnKicking;

            _onModeStarted = Timing.RunCoroutine(OnModeStarted());
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Player.Shot -= OnShot;
            Exiled.Events.Handlers.Player.SearchingPickup -= OnSearchingPickup;
            Exiled.Events.Handlers.Player.DroppingItem -= OnDroppingItem;
            Exiled.Events.Handlers.Player.Kicking -= OnKicking;

            Timing.KillCoroutines(_onModeStarted);
        }

        private IEnumerator<float> OnModeStarted()
        {
            foreach (var ply in PlayerManager.List)
            {
                if (GodModePlayers.Contains(ply))
                    GodModePlayers.Remove(ply);

                ply.Kill("예선전을 치를 준비가 되셨나요?");
            }

            for (var i = 10; i > 0; i--)
            {
                MultiBroadcast.API.MultiBroadcast.AddMapBroadcast(1,
                    $"{i}초 뒤 <b><color=#F2F5A9>예선전</color></b>이 시작됩니다.");

                yield return Timing.WaitForSeconds(1);
            }

            PlayerManager.List.ToList().CopyTo(_pl);
            _pl.ShuffleList();

            foreach (var table in Tools.GetObjectList("RussianRouletteTable"))
                _tablePositions.Add(table.position, []);

            foreach (var table in _tablePositions)
            {
                if (_pl.Count < 1)
                    break;

                while (table.Value.Count < 6)
                {
                    if (_pl.Count < 1)
                        break;

                    table.Value.Add(_pl[0]);
                    _pl.RemoveAt(0);
                }

                if (table.Value.Count <= 0) continue;
                var players = table.Value;
                var seats = Tools.GetCirclePoints(table.Key, 2, table.Value.Count);

                foreach (var player in players)
                {
                    player.Role.Set(RoleTypeId.ClassD);
                    player.Position = seats[players.IndexOf(player)];
                    player.EnableEffect(EffectType.Ensnared);
                }

                Timing.RunCoroutine(Process("예선전", table.Key));
            }

            while (PlayerManager.List.Where(x => x.IsAlive).ToList().Count > 0)
                yield return Timing.WaitForSeconds(1f);

            for (var i = 10; i > 0; i--)
            {
                MultiBroadcast.API.MultiBroadcast.AddMapBroadcast(1,
                    $"{i}초 뒤 <b><color=#F7D358>결승전</color></b>이 시작됩니다.");

                yield return Timing.WaitForSeconds(1);
            }

            var seats1 = Tools.GetCirclePoints(_tablePositions.Keys.ToList()[0], 2,
                _finals.Where(x => x != null).ToList().Count);

            foreach (var player in _finals.Where(x => x != null))
                try
                {
                    player.Role.Set(RoleTypeId.ClassD);
                    player.Position = seats1[_finals.Where(x => x != null).ToList().IndexOf(player)];
                    player.EnableEffect(EffectType.Ensnared);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }

            foreach (var player in _finals.ToList())
                if (!player.IsConnected)
                    _finals.Remove(player);

            Timing.RunCoroutine(Process("결승전", Vector3.zero));
        }

        private IEnumerator<float> Process(string roundName, Vector3 key)
        {
            var players = roundName == "예선전" ? _tablePositions[key] : _finals;

            var currentPlayerIndex = 0;

            while (players.Count > 1)
            {
                var trueIndex = Random.Range(0, 6);

                var bullets = Enumerable.Repeat(false, 6).ToList();
                bullets[trueIndex] = true;

                var count = 0;
                Player target = null;

                foreach (var bullet in bullets)
                {
                    var revolver = (Firearm)players[currentPlayerIndex].AddItem(ItemType.GunRevolver);
                    revolver.BarrelAmmo = 1;

                    players[currentPlayerIndex].CurrentItem = revolver;
                    players[currentPlayerIndex].AddHint("러시안 룰렛", $"""
                                                                   <size=25>당신의 차례입니다.
                                                                   다른 유저를 사살하거나, 자신을 공격함으로써 공격 기회를 한번 더 얻을 수 있습니다.</size>
                                                                   """);

                    for (var i = 10; i > 0; i--)
                    {
                        foreach (var player in players) player.AddBroadcast(1, $"{i}");

                        if (_shotChecks.ContainsKey(players[currentPlayerIndex]))
                        {
                            target = _shotChecks[players[currentPlayerIndex]];
                            _shotChecks.Remove(players[currentPlayerIndex]);
                            break;
                        }

                        yield return Timing.WaitForSeconds(1f);
                    }

                    count++;
                    players[currentPlayerIndex].RemoveItem(revolver);

                    var isSelfShot = false;

                    var isRoundEnd = ShotEvent(players[currentPlayerIndex], target);

                    if (isRoundEnd)
                        break;

                    if (!isSelfShot) currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;
                    continue;

                    void SendAlert(string message)
                    {
                        if (roundName == "결승전")
                            MultiBroadcast.API.MultiBroadcast.AddMapBroadcast(1, message);
                        else
                            foreach (var player in players)
                                player.AddBroadcast(1, message);
                    }

                    bool ShotEvent(Player attacker, Player player)
                    {
                        if (bullet)
                        {
                            player = player == null || attacker == player ? attacker : player;

                            player.Kill("리볼버의 탄약이 당신을 꿰뚫었습니다.");

                            if (players.Contains(player))
                                players.Remove(player);

                            SendAlert($"누군가가 사망하였습니다. ({6 - count}/6)");

                            return true;
                        }

                        SendAlert($"아무 일도 일어나지 않았습니다. ({6 - count}/6)");

                        if (player != null && attacker != player) return false;
                        SendAlert("<size=25>자신을 쏘고 살아남았으므로 격발 기회를 한번 더 얻습니다.</size>");
                        isSelfShot = true;

                        return false;
                    }
                }

                if (currentPlayerIndex >= players.Count) currentPlayerIndex = 0;
            }

            if (roundName == "결승전")
            {
                Round.IsLocked = false;

                MultiBroadcast.API.MultiBroadcast.AddMapBroadcast(20,
                    $"<size=25>🎉 축하합니다, <b><color=yellow>{_finals[0].DisplayNickname}</color></b>(이)가 <b><color=#{ModeType.RussianRoulette.GetModeData().Color}>러시안 룰렛</color></b>에서 우승하였습니다! 🎉</size>");

                _finals[0].DisableEffect(EffectType.Ensnared);
                Timing.RunCoroutine(Tools.SetWinner([_finals[0]], 5));
            }
            else
            {
                players[0].Kill("결승전에 진출했습니다! 다른 그룹의 게임이 종료될 때까지 기다려주세요.");
                _finals.Add(players[0]);
            }
        }

        private void OnShot(ShotEventArgs ev)
        {
            _shotChecks.Add(ev.Player, ev.Target);
        }

        private void OnSearchingPickup(SearchingPickupEventArgs ev)
        {
            ev.IsAllowed = false;
        }

        private void OnDroppingItem(DroppingItemEventArgs ev)
        {
            ev.IsAllowed = false;
        }

        private void OnKicking(KickingEventArgs ev)
        {
            if (ev.Reason.ToLower().Contains("afk"))
                ev.IsAllowed = false;
        }
    }
}
