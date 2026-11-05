using System.Collections.Generic;
using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using MEC;
using Mirror;
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
        public override string Map => "bp";

        public static BombParty Instance;

        private readonly List<Player> _pl = [];

        private CoroutineHandle _onModeStarted;

        public override void OnEnabled()
        {
            Round.IsLocked = true;
            Respawn.PauseWaves();
            Server.FriendlyFire = true;

            Exiled.Events.Handlers.Player.Died += OnDied;

            Timing.RunCoroutine(OnModeStarted());
        }

        public override void OnDisabled()
        {
            Round.IsLocked = false;
            Respawn.ResumeWaves();
            Server.FriendlyFire = false;

            Exiled.Events.Handlers.Player.Died -= OnDied;

            Timing.KillCoroutines(_onModeStarted);
        }

        private IEnumerator<float> OnModeStarted()
        {
            PlayerManager.List.ToList().CopyTo(_pl);

            foreach (var player in PlayerManager.List)
            {
                player.Role.Set(PlayerRoles.RoleTypeId.ClassD);
                player.Position = new Vector3(-0.3515625f, 300.9572f, -9.261719f);
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
                    if (Random.Range(1, 4) == 1)
                    {
                        var f1 = (FlashGrenade)Item.Create(ItemType.GrenadeFlash);
                        f1.FuseTime = 2f;
                        f1.SpawnActive(GetRandomPosition());
                    }
                }

                if (t > 60)
                {
                    if (Random.Range(1, 4) == 1)
                    {
                        var g1 = (ExplosiveGrenade)Item.Create(ItemType.GrenadeHE, Server.Host);
                        g1.FuseTime = 5f;
                        g1.SpawnActive(GetRandomPosition(), Server.Host);
                    }
                }

                if (t > 90)
                {
                    if (Random.Range(1, 4) == 1)
                    {
                        var scp244 =
                            (Scp244)Item.Create(
                                new List<ItemType>() { ItemType.SCP244a, ItemType.SCP244b }.GetRandomValue(),
                                Server.Host);
                        scp244.CreatePickup(GetRandomPosition(), new Quaternion(45, 0, 0, 0));
                    }
                }

                if (t > 120)
                {
                    if (Random.Range(1, 4) == 1)
                    {
                        var scp018 = (Scp018)Item.Create(ItemType.SCP018, Server.Host);
                        scp018.SpawnActive(GetRandomPosition(), Server.Host);
                    }
                }

                if (t > 150)
                {
                    var g2 = (ExplosiveGrenade)Item.Create(ItemType.GrenadeHE, Server.Host);
                    g2.FuseTime = 3f;
                    do
                    {
                        g2.SpawnActive(GetRandomPosition(), Server.Host);
                    } while (Random.Range(1, 101) <= 35);

                    var f2 = (FlashGrenade)Item.Create(ItemType.GrenadeFlash);
                    f2.FuseTime = 2f;
                    do
                    {
                        f2.SpawnActive(GetRandomPosition(), Server.Host);
                    } while (Random.Range(1, 101) <= 25);

                    var scp244 =
                        (Scp244)Item.Create(
                            new List<ItemType> { ItemType.SCP244a, ItemType.SCP244b }.GetRandomValue(), Server.Host);
                    do
                    {
                        scp244.CreatePickup(GetRandomPosition(), new Quaternion(45, 0, 0, 0));
                    } while (Random.Range(1, 101) <= 25);

                    var scp018 = (Scp018)Item.Create(ItemType.SCP018, Server.Host);
                    do
                    {
                        scp018.SpawnActive(GetRandomPosition(), Server.Host);
                    } while (Random.Range(1, 101) <= 35);
                }

                PlayerManager.List.ToList().ForEach(x => x.DisableEffect(EffectType.Flashed));
                yield return Timing.WaitForSeconds(3f);
            }
        }

        private Vector3 GetRandomPosition()
        {
            return new Vector3(Random.Range(-9.941405f, 10.92998f), 303.9572f, Random.Range(-15.76172f, 2.550781f));
        }

        private void OnDied(Exiled.Events.EventArgs.Player.DiedEventArgs ev)
        {
            if (!_pl.Contains(ev.Player)) return;
            _pl.Remove(ev.Player);

            if (_pl.Count >= 2) return;
            Round.IsLocked = false;

            PlayerManager.List.ToList().ForEach(x => x.AddBroadcast(20, $"승리자 : {_pl[0].DisplayNickname}"));
            Timing.RunCoroutine(Tools.SetWinner([_pl[0]], 5));
        }
    }
}