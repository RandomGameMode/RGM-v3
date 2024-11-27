using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CustomRendering;
using Exiled.API.Enums;
using Exiled.API.Features;
using MEC;
using Mirror;
using MultiBroadcast;
using PlayerRoles;
using Respawning;
using Respawning.Waves;
using RGM.API.Features;
using UnityEngine;

namespace RGM.Modes
{
    [Mode(ModeCategory.Public, ModeInfo.Set, ModeType.HideAndSeek)]
    class HideAndSeek : Mode
    {
        public override string Name => "숨바꼭질";
        public override string Description => "꼭꼭 숨으세요! 사냥개가 당신을 찾을 것입니다. 제한 시간동안 버티세요!";
        public override string Detail =>
"""
고도의 심리전 싸움입니다.

최후의 승자는 과연 누가 될 것인가..
""";
        public override string Color => "F5A9E1";

        public static HideAndSeek Instance;

        List<Player> Finders = new List<Player>();

        public override void OnEnabled()
        {
            Round.IsLocked = true;
            foreach (var spawn in Respawning.WaveManager.Waves) spawn.Destroy();

            Timing.RunCoroutine(OnModeStarted());
        }

        public IEnumerator<float> OnModeStarted()
        {
            Server.ExecuteCommand($"/mp load HideAndSeek");

            for (float i = 1; i < Player.List.Count / 10 + 2; i++)
                Finders.Add(Tools.GetRandomValue(Player.List.Where(x => !Finders.Contains(x)).ToList()));

            Player.List.ToList().ForEach(x => x.IsGodModeEnabled = true);

            foreach (var player in Player.List.Where(x => !Finders.Contains(x)))
            {
                player.Role.Set(RoleTypeId.ClassD);
                player.Position = GameObject.Find("StartPoint").transform.position;
            }

            for (int i = 1; i < 10; i++)
            {
                foreach (var player in Player.List)
                {
                    player.ClearBroadcasts();
                    player.Broadcast(2, $"<size=25><b><color=red>{10 - i}초 뒤 술래가 출몰합니다.</color></b></size>");
                }

                yield return Timing.WaitForSeconds(1f);
            }

            int Remaining = 75;

            foreach (var Finder in Finders)
            {
                Finder.Role.Set(RoleTypeId.Scp939);
                Finder.Position = GameObject.Find("StartPoint").transform.position;
            }

            yield return Timing.WaitForSeconds(1f);

            Player.List.ToList().ForEach(x => x.IsGodModeEnabled = false);
            Round.IsLocked = false;

            for (int i = 1; i < Remaining; i++)
            {
                foreach (var player in Player.List)
                {
                    player.ClearBroadcasts();
                    player.Broadcast(2, $"<size=25><b><color=#2EFEF7>{Remaining - i}초 뒤 술래가 패배합니다.</color></b></size>");
                }

                yield return Timing.WaitForSeconds(1f);
            }

            Finders.ForEach(x => x.Kill($"제한 시간 안에 생존자를 전부 죽이지 못했습니다."));
        }
    }
}
