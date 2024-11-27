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

namespace RGM.Modes
{
    [Mode(ModeCategory.Public, ModeInfo.Set, ModeType.DeadLine)]
    public class DeadLine : Mode
    {
        public override string Name => "데드 라인";
        public override string Description => "빨간색을 밟지 마세요!";
        public override string Detail =>
"""
<color=red>빨간색</color>을 밟으면 죽습니다.

<i>발 아래를 조심하세요!</i>
""";
        public override string Color => "FA8258";

        public static DeadLine Instance;

        public List<Player> pl = new List<Player>();

        Player dj;

        public override void OnEnabled()
        {
            Round.IsLocked = true;
            foreach (var spawn in Respawning.WaveManager.Waves) spawn.Destroy();

            Exiled.Events.Handlers.Player.Spawned += OnSpawned;
            Exiled.Events.Handlers.Player.Died += OnDied;

            Timing.RunCoroutine(OnModeStarted());
        }

        public IEnumerator<float> OnModeStarted()
        {
            Server.ExecuteCommand($"/mp load dl");

            Player.List.CopyTo(pl);

            dj = Tools.SpawnDJ("dj", RoleTypeId.Tutorial, new Vector3(79.23709f, 1022.955f, -41.04944f), "dj");

            GGUtils.Gtool.PlaySound("dj", "LineLite", VoiceChat.VoiceChatChannel.Intercom, 25, true);

            Timing.RunCoroutine(DJHeadBanging());

            foreach (var player in Player.List.Where(x => !x.IsNPC))
            {
                player.Role.Set(RoleTypeId.Scientist);
                player.Position = new Vector3(80.1824f, 1011.915f, -49.73869f);
                player.ClearInventory();
            }

            while (true)
            {
                foreach (var player in Player.List.Where(x => !x.IsNPC && x.IsAlive))
                {
                    if (Physics.Raycast(player.Position, Vector3.down, out RaycastHit hit, 1, (LayerMask)1))
                    {
                        if (hit.transform.name.Contains("Dead"))
                        {
                            if (pl.Contains(player))
                            {
                                pl.Remove(player);

                                if (pl.Count < 2)
                                    Round.IsLocked = false;

                                player.Kill("선을 넘어버렸다네~");
                            }
                        }
                    }
                }

                yield return Timing.WaitForOneFrame;
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
