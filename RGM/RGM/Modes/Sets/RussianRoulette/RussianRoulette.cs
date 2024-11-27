using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CustomPlayerEffects;
using CustomRendering;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Roles;
using MapEditorReborn.API.Features.Objects;
using MEC;
using Mirror;
using MultiBroadcast.API;
using PlayerRoles;
using UnityEngine;
using Exiled.API.Enums;
using RGM.API.Features;
using MapEditorReborn.API.Features;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Exiled.API.Features.Items;

using static RGM.Variables.ServerManagers;

namespace RGM.Modes
{
    [Mode(ModeCategory.Public, ModeInfo.Set, ModeType.RussianRoulette)]
    class RussianRoulette : Mode
    {
        public override string Name => "러시안 룰렛";
        public override string Description => "정치질이 난무하는 운과 심리전의 싸움입니다.";
        public override string Detail =>
"""
최대 7개의 그룹으로 나뉘어 각 5명씩 예선전을 치릅니다.

각 그룹의 우승자는 최대 7명으로 결승전을 치르게 됩니다.

<size=25>
* 자신을 쏘는 경우(허공에 발사하거나 10초를 버틴 경우) 격발 기회를 한번 더 얻습니다.
* 다른 플레이어를 사망에 이르게 할 경우 격발 기회를 한번 더 얻습니다.
</size>
""";
        public override string Color => "F5ECCE";

        public static RussianRoulette Instance;

        public int RequiredFinals;
        public List<Player> pl = new List<Player>();
        public List<Player> Finals = new List<Player>();

        public Dictionary<Vector3, List<Player>> TablePositions = new Dictionary<Vector3, List<Player>>();
        public Dictionary<Player, Player> ShotChecks = new Dictionary<Player, Player>();

        public override void OnEnabled()
        {
            Round.IsLocked = true;
            foreach (var spawn in Respawning.WaveManager.Waves) spawn.Destroy();

            Exiled.Events.Handlers.Player.Shot += OnShot;
            Exiled.Events.Handlers.Player.SearchingPickup += OnSearchingPickup;
            Exiled.Events.Handlers.Player.DroppingItem += OnDroppingItem;
            Exiled.Events.Handlers.Player.Kicking += OnKicking;

            Timing.RunCoroutine(OnModeStarted());
        }

        public IEnumerator<float> OnModeStarted()
        {
            MapUtils.LoadMap("ru");

            foreach (var ply in Player.List)
            {
                if (GodModePlayers.Contains(ply))
                    GodModePlayers.Remove(ply);

                ply.Kill("예선전을 치를 준비가 되셨나요?");
            }

            for (int i = 1; i < 11; i++)
            {
                foreach (var player in Player.List)
                    player.AddBroadcast(1, $"{11 - i}초 뒤 <b><color=#F2F5A9>예선전</color></b>이 시작됩니다.");

                yield return Timing.WaitForSeconds(1f);
            }

            Player.List.ToList().CopyTo(pl);
            pl.ShuffleList();

            foreach (Transform table in Tools.GetObjectList("RussianRouletteTable"))
                TablePositions.Add(table.position, new List<Player>());

            RequiredFinals = TablePositions.Count;

            foreach (var table in TablePositions)
            {
                if (pl.Count < 1)
                    break;

                while (table.Value.Count < 6)
                {
                    if (pl.Count < 1)
                        break;

                    table.Value.Add(pl[0]);
                    pl.Remove(pl[0]);
                }

                if (table.Value.Count > 0)
                {
                    List<Player> players = table.Value;
                    List<Vector3> seats = Tools.GetCirclePoints(table.Key, 2, table.Value.Count);

                    foreach (var player in players)
                    {
                        player.Role.Set(RoleTypeId.ClassD);
                        player.Position = seats[players.IndexOf(player)];
                        player.EnableEffect(EffectType.Ensnared);
                    }

                    Timing.RunCoroutine(Process("예선전", table.Key));
                }
            }

            while (Player.List.Where(x => x.IsAlive).ToList().Count > 0)
                yield return Timing.WaitForSeconds(1f);

            for (int i = 1; i < 11; i++)
            {
                foreach (var player in Player.List)
                    player.AddBroadcast(1, $"{11 - i}초 뒤 <b><color=#F7D358>결승전</color></b>이 시작됩니다.");

                yield return Timing.WaitForSeconds(1f);
            }

            List<Vector3> seats1 = Tools.GetCirclePoints(TablePositions.Keys.ToList()[0], 2, Finals.Where(x => x != null).ToList().Count);

            foreach (var player in Finals.Where(x => x != null))
            {
                try
                {
                    player.Role.Set(RoleTypeId.ClassD);
                    player.Position = seats1[Finals.Where(x => x != null).ToList().IndexOf(player)];
                    player.EnableEffect(EffectType.Ensnared);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }

            Timing.RunCoroutine(Process("결승전", Vector3.zero));

            yield break;
        }

        public IEnumerator<float> Process(string roundName, Vector3 key)
        {
            List<Player> Players = roundName == "예선전" ? TablePositions[key] : Finals;

            while (Players.Count > 1)
            {
                int trueIndex = UnityEngine.Random.Range(0, 6);

                List<bool> Bullets = Enumerable.Repeat(false, 6).ToList();
                Bullets[trueIndex] = true;

                int Count = 0;
                Player Gunner = Players[0];
                Player Target = null;

                foreach (var bullet in Bullets)
                {
                    Firearm Revolver = (Firearm)Gunner.AddItem(ItemType.GunRevolver);
                    Revolver.Ammo = 1;

                    Gunner.CurrentItem = Revolver;
                    Gunner.ShowHint($"<size=25>당신의 차례입니다.\n쏠 유저를 신중하게 고르십시오.</size>");

                    for (int i = 1; i < 11; i++)
                    {
                        foreach (var player in Players) player.AddBroadcast(1, $"{11 - i}");

                        if (ShotChecks.ContainsKey(Gunner))
                        {
                            Target = ShotChecks[Gunner];
                            ShotChecks.Remove(Gunner);
                            break;
                        }

                        yield return Timing.WaitForSeconds(1f);
                    }


                    Count++;
                    Gunner.RemoveItem(Revolver);

                    bool IsSelfShot = false;
                    bool IsRoundEnd = false;

                    bool ShotEvent(Player Attacker, Player Player)
                    {
                        if (bullet)
                        {
                            if (Player != null)
                                Player.Kill("리볼버의 탄약이 당신을 꿰뚫었습니다.");

                            if (Players.Contains(Player))
                                Players.Remove(Player);

                            foreach (var player in Players) player.AddBroadcast(1, $"누군가가 사망하였습니다. ({6 - Count}/6)");

                            return true;
                        }
                        else
                        {
                            foreach (var player in Players) player.AddBroadcast(1, $"아무 일도 일어나지 않았습니다. ({6 - Count}/6)");

                            if (Player == null)
                            {
                                foreach (var player in Players) player.AddBroadcast(1, $"<size=25>자신을 쏘고 살아남았으므로 격발 기회를 한번 더 얻습니다.</size>");
                                IsSelfShot = true;
                            }

                            return false;
                        }
                    }

                    if (Target != null)
                        IsRoundEnd = ShotEvent(Gunner, Target);

                    else
                        IsRoundEnd = ShotEvent(Gunner, Gunner);

                    if (IsRoundEnd)
                        break;

                    if (!IsSelfShot)
                    {
                        int currentIndex = Players.IndexOf(Gunner);
                        int nextIndex = (currentIndex + 1) % Players.Count;

                        Gunner = Players[nextIndex];
                    }
                }
            }

            if (roundName == "결승전")
            {
                Round.IsLocked = false;

                foreach (var player in Player.List)
                    player.AddBroadcast(20, $"<size=25>🎉 축하합니다, <b><color=yellow>{Finals[0].DisplayNickname}</color></b>(이)가 <b><color=#{ModeType.RussianRoulette.GetModeData().Color}>러시안 룰렛</color></b>에서 우승하였습니다! 🎉</size>");
                
                Finals[0].DisableEffect(EffectType.Ensnared);
            }
            else
            {
                Players[0].Kill("결승전에 진출했습니다! 다른 그룹의 게임이 종료될 때까지 기다려주세요.");
                Finals.Add(Players[0]);
            }

            yield break;
        }

        public void OnShot(Exiled.Events.EventArgs.Player.ShotEventArgs ev)
        {
            ShotChecks.Add(ev.Player, ev.Target);
        }

        public void OnSearchingPickup(Exiled.Events.EventArgs.Player.SearchingPickupEventArgs ev)
        {
            ev.IsAllowed = false;
        }

        public void OnDroppingItem(Exiled.Events.EventArgs.Player.DroppingItemEventArgs ev)
        {
            ev.IsAllowed = false;
        }

        public void OnKicking(Exiled.Events.EventArgs.Player.KickingEventArgs ev)
        {
            if (ev.Reason.ToLower().Contains("afk"))
                ev.IsAllowed = false;
        }
    }
}
