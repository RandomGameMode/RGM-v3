using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using HarmonyLib;
using MEC;
using Mirror;
using UnityEngine;
using Exiled.API.Enums;
using PlayerRoles;
using RGM.API.Features;

namespace RGM.Modes
{
    [Mode(ModeCategory.Private, ModeInfo.Set, ModeType.Spleef)]
    class Spleef : Mode
    {
        public override string Name => "스플리프";
        public override string Description => "떨어지지 않으려면 계속 움직이세요!";
        public override string Detail =>
"""
총 10개의 층으로 이루어져 있는 것 같습니다.

최대한 오래 살아남기 위한 전략을 고안해 보세요!
""";
        public override string Color => "BEF781";
        public override string Map => "Spleef1205";

        public static Spleef Instance;

        List<Player> pl = [];
        List<AdminToys.PrimitiveObjectToy> Transforms = [];
        Dictionary<Player, float> OnGround = new();

        CoroutineHandle _onModeStarted;

        AudioClipPlayback audio;

        public override void OnEnabled()
        {
            Round.IsLocked = true;
            Respawn.PauseWaves();
            Server.FriendlyFire = true;

            Exiled.Events.Handlers.Player.Dying += OnDying;
            Exiled.Events.Handlers.Player.Hurting += OnHurting;

            _onModeStarted = Timing.RunCoroutine(OnModeStarted());

            audio = Tools.PlayGlobalAudio("Spleef", volume: 0.5f, loop: true);
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Player.Dying -= OnDying;
            Exiled.Events.Handlers.Player.Hurting -= OnHurting;

            Timing.KillCoroutines(_onModeStarted);

            audio.IsPaused = true;
        }

        public IEnumerator<float> OnModeStarted()
        {
            PlayerManager.List.ToList().CopyTo(pl);

            foreach (var player in PlayerManager.List)
            {
                player.Role.Set(RoleTypeId.ClassD);
                player.Position = new Vector3(36.99778f, 353.3881f, -88.50272f);
            }

            for (int i=1; i<11; i++)
            {
                PlayerManager.List.ToList().ForEach(x => x.AddHint("스플리프 안내", $"<b>{11 - i}초 뒤 게임이 시작됩니다.</b>", 1.2f));

                yield return Timing.WaitForSeconds(1f);
            }

            IEnumerator<float> RemovingPlatform(AdminToys.PrimitiveObjectToy Platform)
            {
                Platform.NetworkMaterialColor = UnityEngine.Color.green;

                yield return Timing.WaitForSeconds(0.3f);

                Platform.NetworkMaterialColor = UnityEngine.Color.yellow;

                yield return Timing.WaitForSeconds(0.3f);

                Platform.NetworkMaterialColor = UnityEngine.Color.red;

                yield return Timing.WaitForSeconds(0.3f);

                NetworkServer.Destroy(Platform.gameObject);
            }

            while (true)
            {
                foreach (var player in PlayerManager.List.Where(x => x.IsAlive).ToList())
                {
                    if (Physics.Raycast(player.Position, Vector3.down, out RaycastHit hit, 3f, (LayerMask)1))
                    {
                        OnGround[player] = 2.1f;

                        if (hit.transform.name == "Platform") 
                        {
                            AdminToys.PrimitiveObjectToy Platform = hit.transform.gameObject.GetComponent<AdminToys.PrimitiveObjectToy>();

                            if (!Transforms.Contains(Platform))
                            {
                                Transforms.Add(Platform);

                                Timing.RunCoroutine(RemovingPlatform(Platform));
                            }
                        }

                        else if (hit.collider.name == "Lava")
                            player.Kill("용암을 좋아한 나머지 뛰어들어갔습니다.");
                    }
                    else
                    {
                        if (player.IsAlive && OnGround.ContainsKey(player))
                        {
                            OnGround[player] -= 0.1f;

                            if (OnGround[player] <= 0)
                                player.Kill("꼼수를 쓰면 안되죠!");
                        }
                    }
                }

                yield return Timing.WaitForSeconds(0.1f);
            }
        }

        public void OnDying(Exiled.Events.EventArgs.Player.DyingEventArgs ev)
        {
            if (pl.Contains(ev.Player))
            {
                pl.Remove(ev.Player);

                if (pl.Count() < 2)
                {
                    Round.IsLocked = false;

                    PlayerManager.List.ToList().ForEach(x => x.AddBroadcast(20, $"승리자 : {pl[0].DisplayNickname}"));
                    Timing.RunCoroutine(Tools.SetWinner(new List<Player>() { pl[0] }, 5));
                }
            }
        }

        public void OnHurting(Exiled.Events.EventArgs.Player.HurtingEventArgs ev)
        {
            if (ev.DamageHandler.Type == DamageType.Falldown)
            {
                ev.IsAllowed = false;
            }
        }
    }
}
