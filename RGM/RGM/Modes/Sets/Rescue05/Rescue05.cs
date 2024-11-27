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

using static RGM.Variables.ServerManagers;
using RGM.API.DataBases;
using Respawning;
using Respawning.Waves;

namespace RGM.Modes
{
    [Mode(ModeCategory.Public, ModeInfo.Set, ModeType.Rescue05)]
    class Rescue05 : Mode
    {
        public override string Name => "05 평의회 구출 작전";
        public override string Description => "05 평의회를 구출하려는 자들과 사살하려는 자들의 싸움입니다.";
        public override string Detail =>
"""
<color=#000000><b>05 평의회</b></color>가 탈출에 성공할 경우,
<color=#2E9AFE>MTF</color> 진영은 <b>강화제 제작 방법</b>을 입수하게 됩니다.

<color=#000000><b>05 평의회</b></color>가 사망할 경우,
<color=#088A08>혼돈의 반란</color> 진영만 시설에 지원하게 됩니다.

<i>* 게임 시작 10분 뒤 <color=red>자동핵</color>이 작동됩니다.</i>
""";
        public override string Color => "0040FF";

        public static Rescue05 Instance;

        public Player Level05;
        public Player Assassin;
        public bool IsMTFEnabled = false;
        public bool IsCHIEnabled = false;

        public override void OnEnabled()
        {
            Exiled.Events.Handlers.Player.Escaping += OnEscaping;
            Exiled.Events.Handlers.Player.Spawned += OnSpawned;
            Exiled.Events.Handlers.Player.Dying += OnDying;
            Exiled.Events.Handlers.Server.RespawningTeam += OnRespawningTeam;

            Timing.RunCoroutine(OnModeStarted());
            Timing.RunCoroutine(AutoWarhead());
        }

        public IEnumerator<float> OnModeStarted()
        {
            Level05 = Tools.GetRandomValue(Player.List.ToList());
            Assassin = Tools.GetRandomValue(Player.List.Where(x => x != Level05).ToList());

            List<ItemType> Level05Items = new List<ItemType>() 
            {
                ItemType.KeycardScientist,
                ItemType.GrenadeFlash,
                ItemType.Flashlight,
                ItemType.Coin,
                ItemType.Medkit,
                ItemType.Painkillers,
                ItemType.SCP500,
                ItemType.Radio
            };

            Level05.Role.Set(RoleTypeId.Scientist);
            Level05.ClearInventory();
            foreach (ItemType item in Level05Items) Level05.AddItem(item);
            Level05.AddBroadcast(10, $"<size=25>당신은 <color=#000000><b>05 평의회</b></color>, <color=#A4A4A4>시설 경비</color>들의 도움을 받아 시설에서 탈출하십시오.</size>");

            List<ItemType> AssanssinItems = new List<ItemType>()
            {
                ItemType.GunCOM15,
                ItemType.Ammo9x19
            };

            Assassin.Role.Set(RoleTypeId.ClassD);
            foreach (ItemType item in AssanssinItems) Assassin.AddItem(item);
            Assassin.AddBroadcast(10, $"<size=25>당신은 <color=#FE9A2E><b>암살자</b></color>, <color=#04B404>혼돈의 반란</color>들과 함께 <color=#000000><b>05 평의회</b></color>를 사살하십시오.</size>");

            for (int i = 0; i < Player.List.Count / 1.5f; i++)
            {
                Player player = Tools.GetRandomValue(Player.List.Where(x => x != Level05 && x != Assassin).ToList());
                player.Role.Set(RoleTypeId.ChaosRifleman);

                player.AddBroadcast(10, $"<size=25>당신은 <color=#04B404><b>혼돈의 반란</b></color>, <color=#FE9A2E><b>암살자</b></color>에 협조하여 <color=#000000><b>05 평의회</b></color>를 사살하십시오.</size>");
            }

            foreach (var player in Player.List.Where(x => x != Level05 && x != Assassin && !x.IsCHI))
            {
                player.Role.Set(RoleTypeId.FacilityGuard);

                player.AddBroadcast(10, $"<size=25>당신은 <color=#A4A4A4>시설 경비</color>, <color=#000000><b>05 평의회</b></color>를 안전하게 탈출시키십시오.</size>");
            }

            yield break;
        }

        public IEnumerator<float> AutoWarhead()
        {
            yield return Timing.WaitForSeconds(9 * 60);

            Server.ExecuteCommand("/cassie_sl 1분 뒤 <color=red>자동핵</color>이 작동됩니다.");

            yield return Timing.WaitForSeconds(1 * 60);

            AutoNuke = true;
            Warhead.Start();
        }

        public void OnEscaping(Exiled.Events.EventArgs.Player.EscapingEventArgs ev)
        {
            if (ev.Player == Level05 && ev.Player.Role.Type == RoleTypeId.Scientist)
            {
                foreach (var player in Player.List)
                    player.AddBroadcast(10, $"<size=30><b><color=#000000>05 평의회</color>({Level05.DisplayNickname})</b>가 탈출하여 <u><i>강화제 제작 방법</i>을 재단에 넘기는 데 성공하였습니다.</u></size>\n<size=25><b>이후로 시설에 지원한 <color=#0040FF>MTF</color>들이 강화됩니다.</b></size>");

                IsMTFEnabled = true;
            }
        }

        public void OnSpawned(Exiled.Events.EventArgs.Player.SpawnedEventArgs ev)
        {
            if (IsMTFEnabled)
            {
                if (ev.Player.IsNTF)
                {
                    ev.Player.MaxHealth *= 2;
                    ev.Player.Health = ev.Player.MaxHealth;
                    ev.Player.Scale = new Vector3(1.2f, 1.1f, 1.2f);
                    ev.Player.EnableEffect(EffectType.Scp1853, 5);
                }
            }
        }

        public void OnDying(Exiled.Events.EventArgs.Player.DyingEventArgs ev)
        {
            if (ev.Player == Level05 && ev.Player.Role.Type == RoleTypeId.Scientist)
            {
                foreach (var player in Player.List)
                    player.AddBroadcast(10, $"<size=30><b><color=#000000>05 평의회</color>({Level05.DisplayNickname})</b>가 <color=red>사살당하였습니다.</color></size>\n<size=25><b>이후로 <color=#04B404>혼돈의 반란</color>만 시설에 지원하게 됩니다.</b></size>");

                IsCHIEnabled = true;
            }
        }

        public void OnRespawningTeam(Exiled.Events.EventArgs.Server.RespawningTeamEventArgs ev)
        {
            if (IsCHIEnabled)
                WaveManager.Spawn(new ChaosSpawnWave());
        }
    }
}
