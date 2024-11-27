using Exiled.API.Features.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Exiled.API.Features;
using MEC;
using UnityEngine;
using Exiled.API.Features.Roles;
using Exiled.API.Enums;
using PlayerRoles;
using MultiBroadcast.API;
using RGM.API.Features;
using Mirror;
using Respawning;

namespace RGM.Modes
{
    [Mode(ModeCategory.Public, ModeInfo.Set, ModeType.HotPotato)]
    public class HotPotato : Mode
    {
        public override string Name => "폭탄 돌리기";
        public override string Description => "폭탄이 터지기 전에 다른 유저에게 넘기세요!";
        public override string Detail =>
"""
유저에 비례하여 <b>폭탄 플레이어의 수가 조정</b>됩니다. (10명 당 1마리 + 1마리)

어떤 수단을 사용하더라도 최후까지 살아남으세요!
""";
        public override string Color => "FA58D0";

        public static HotPotato Instance;

        public List<Player> pl = new List<Player>();
        public List<Player> BomberMans = new List<Player>();

        Player dj;

        public override void OnEnabled()
        {
            Server.FriendlyFire = true;
            Round.IsLocked = true;
            foreach (var spawn in Respawning.WaveManager.Waves) spawn.Destroy();

            Exiled.Events.Handlers.Player.Spawned += OnSpawned;
            Exiled.Events.Handlers.Player.Hurting += OnHurting;
            Exiled.Events.Handlers.Player.Died += OnDied;

            Timing.RunCoroutine(OnModeStarted());
        }

        public IEnumerator<float> OnModeStarted()
        {
            Server.ExecuteCommand($"/mp load hp");

            Player.List.Where(x => !x.IsNPC).CopyTo(pl);

            dj = Tools.SpawnDJ("dj", RoleTypeId.Tutorial, new Vector3(82.51834f, 1014.692f, -50.10588f), "dj");

            GGUtils.Gtool.PlaySound("dj", "Skeleton", VoiceChat.VoiceChatChannel.Intercom, 25, true);

            Timing.RunCoroutine(DJHeadBanging());

            foreach (var player in Player.List.Where(x => !x.IsNPC))
            {
                player.Role.Set(RoleTypeId.ClassD);
                player.Position = new Vector3(83.91287f, 1014.692f, -37.13322f);
            }

            while (true)
            {
                pl.ShuffleList();

                for (float i = 1; i < pl.Count / 10 + 2; i++)
                {
                    Player BomberMan = Tools.GetRandomValue(pl.Where(x => pl.Contains(x) && !BomberMans.Contains(x)).ToList());

                    if (BomberMan != null)
                    {
                        BomberMan.Role.Set(RoleTypeId.Scp049, SpawnReason.ForceClass, RoleSpawnFlags.None);
                        BomberMans.Add(BomberMan);
                    }
                    else
                        pl.Remove(BomberMan);
                }

                for (int i = 1; i < 11; i++)
                {
                    Player.List.ToList().ForEach(x => x.ShowHint($"현재 폭탄 : {string.Join(" & ", BomberMans.Select(b => b.Nickname))}\n{11 - i}초 후 폭탄이 터집니다.", 1.2f));

                    yield return Timing.WaitForSeconds(1f);
                }

                foreach (var bomber in BomberMans)
                {
                    try
                    {
                        if (pl.Contains(bomber))
                        {
                            pl.Remove(bomber);

                            var g = (ExplosiveGrenade)Item.Create(ItemType.GrenadeHE, Server.Host);
                            g.FuseTime = 0.1f;
                            g.SpawnActive(bomber.Position, Server.Host);

                            bomber.Kill("폭탄을 넘기지 못해서 죽어버렸다네~");
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                }

                BomberMans.Clear();

                if (pl.Count < 2)
                {
                    Round.IsLocked = false;

                    pl[0].Role.Set(RoleTypeId.ClassD, SpawnReason.ForceClass, RoleSpawnFlags.None);
                    Player.List.ToList().ForEach(x => x.AddBroadcast(20, $"승리자 : {pl[0].Nickname}"));
                    break;
                }

                Player.List.ToList().ForEach(x => x.ShowHint($"펑!", 2));

                yield return Timing.WaitForSeconds(2f);
            }
        }

        public IEnumerator<float> DJHeadBanging()
        {
            yield return Timing.WaitForSeconds(1f);

            bool HeadUp = true;

            while (true)
            {
                if (HeadUp)
                {
                    GGUtils.Gtool.Rotate(dj.ReferenceHub, new Vector3(0, -1f, 0));

                    HeadUp = false;

                    yield return Timing.WaitForSeconds(0.2f);
                }
                else
                {
                    GGUtils.Gtool.Rotate(dj.ReferenceHub, new Vector3(0, 1f, 0));

                    HeadUp = true;

                    yield return Timing.WaitForSeconds(0.15f);
                }
            }
        }

        public void OnSpawned(Exiled.Events.EventArgs.Player.SpawnedEventArgs ev)
        {
            Server.ExecuteCommand($"/speak {ev.Player.Id} 1");

            ev.Player.GetEffect(EffectType.MovementBoost).Intensity = 50;
        }

        public void OnHurting(Exiled.Events.EventArgs.Player.HurtingEventArgs ev)
        {
            ev.IsAllowed = false;

            if (BomberMans.Contains(ev.Attacker) && !BomberMans.Contains(ev.Player) && !ev.Player.IsNPC)
            {
                BomberMans.Remove(ev.Attacker);
                BomberMans.Add(ev.Player);

                Server.ExecuteCommand($"/fc {ev.Attacker.Id} ClassD 0");
                Server.ExecuteCommand($"/fc {ev.Player.Id} Scp049 0");
            }
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
