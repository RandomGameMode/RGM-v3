using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CustomRendering;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using InventorySystem.Items.Usables.Scp244;
using MEC;
using Mirror;
using MultiBroadcast.API;
using Respawning;
using RGM.API.Features;
using UnityEngine;

namespace RGM.Modes
{
    [Mode(ModeCategory.Public, ModeInfo.Set, ModeType.BombParty)]
    class BombParty : Mode
    {
        public override string Name => "폭탄 파티";
        public override string Description => "버티면 버틸수록 난이도가 올라갑니다.";
        public override string Detail =>
"""
투사체 또는 SCP 아이템이 랜덤한 곳에 떨어집니다.

3초마다 <color=#F7BE81>이벤트</color>가 일어납니다.

라운드 경과
0초 후ㅣ<b>고폭 수류탄</b>이 떨어집니다.
30초 후ㅣ<b>섬광탄</b>이 1/3 확률로 떨어집니다.
60초 후ㅣ<b>고폭 수류탄</b>이 1/3 확률로 떨어집니다.
90초 후ㅣ<b>SCP-244</b>가 1/3 확률로 떨어집니다.
120초 후ㅣ<b>SCP-018</b>이 1/3 확률로 떨어집니다.

3분 이상 버틴다면 스스로를 칭찬해주세요.
""";
        public override string Color => "FAAC58";

        public static BombParty Instance;

        public List<Player> pl = new List<Player>();

        public override void OnEnabled()
        {
            Round.IsLocked = true;
            foreach (var spawn in Respawning.WaveManager.Waves) spawn.Destroy();

            Exiled.Events.Handlers.Player.Died += OnDied;

            Timing.RunCoroutine(OnModeStarted());
        }

        public IEnumerator<float> OnModeStarted()
        {
            Server.ExecuteCommand($"/mp load bp");

            Player.List.ToList().CopyTo(pl);

            foreach (var player in Player.List)
            {
                player.Role.Set(PlayerRoles.RoleTypeId.ClassD);
                player.Position = new Vector3(-0.09375f, 1000.957f, -7.28125f);
            }

            int t = 0;

            while (true)
            {
                t += 3;

                var g = (ExplosiveGrenade)Item.Create(ItemType.GrenadeHE, Server.Host);
                g.FuseTime = 3f;
                g.SpawnActive(GetRandomPosition(), Server.Host);

                if (t > 30)
                {
                    if (UnityEngine.Random.Range(1, 4) == 1)
                    {
                        var g1 = (FlashGrenade)Item.Create(ItemType.GrenadeFlash);
                        g1.FuseTime = 2f;
                        g1.SpawnActive(GetRandomPosition());
                    }
                }
                if (t > 60)
                {
                    if (UnityEngine.Random.Range(1, 4) == 1)
                    {
                        var g1 = (ExplosiveGrenade)Item.Create(ItemType.GrenadeHE, Server.Host);
                        g1.FuseTime = 5f;
                        g1.SpawnActive(GetRandomPosition(), Server.Host);
                    }
                }
                if (t > 90)
                {
                    if (UnityEngine.Random.Range(1, 4) == 1)
                    {
                        var scp244 = (Scp244)Item.Create(Tools.GetRandomValue(new List<ItemType>() { ItemType.SCP244a, ItemType.SCP244b }), Server.Host);
                        scp244.CreatePickup(GetRandomPosition(), new Quaternion(45, 0, 0, 0));
                    }
                }
                if (t > 120)
                {
                    if (UnityEngine.Random.Range(1, 4) == 1)
                    {
                        var scp018 = (Scp018)Item.Create(ItemType.SCP018, Server.Host);
                        scp018.SpawnActive(GetRandomPosition(), Server.Host);
                    }
                }

                Player.List.ToList().ForEach(x => x.DisableAllEffects());
                yield return Timing.WaitForSeconds(3f);
            }
        }

        public Vector3 GetRandomPosition()
        {
            return new Vector3(UnityEngine.Random.Range(-9.941405f, 10.92998f), 1004.188f, UnityEngine.Random.Range(-15.76172f, 2.550781f));
        }

        public void OnDied(Exiled.Events.EventArgs.Player.DiedEventArgs ev)
        {
            if (pl.Contains(ev.Player))
            {
                pl.Remove(ev.Player);

                if (pl.Count < 2)
                {
                    Round.IsLocked = false;

                    Player.List.ToList().ForEach(x => x.AddBroadcast(20, $"승리자 : {pl[0].Nickname}"));
                }
            }
        }
    }
}
