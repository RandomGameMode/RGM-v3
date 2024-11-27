using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CustomRendering;
using Exiled.API.Features;
using MEC;
using Mirror;
using MultiBroadcast;
using RGM.API.Features;
using UnityEngine;

namespace RGM.Modes
{
    [Mode(ModeCategory.Private, ModeInfo.Set, ModeType.MiniGames)]
    class MiniGames : Mode
    {
        public override string Name => "미니게임";
        public override string Description => "간단한 게임들을 즐겨보세요! 총 3개의 라운드로 구성되어 있습니다.";
        public override string Detail =>
"""
등장하는 미니게임들의 목록입니다.

airstrike
dm
escape
battle
versus
cs
glass
deathrun
line
dodge
fall
football
gungame
knives
puzzle
race
light
spleef
tag
tdm
lava
zombie
zombie2
""";
        public override string Color => "A4A4A4";

        public static MiniGames Instance;

        public int RoundCount = 0;
        public List<string> Games = new List<string>()
        {
            "airstrike",
            "dm",
            "escape",
            "battle",
            "versus",
            "cs",
            "glass",
            "deathrun",
            "line",
            "dodge",
            "fall",
            "football",
            "gungame",
            "knives",
            "puzzle",
            "race",
            "light",
            "spleef",
            "tag",
            "tdm",
            "lava",
            "zombie",
            "zombie2"
        };

        public override void OnEnabled()
        {
            Round.IsLocked = true;
            Map.IsDecontaminationEnabled = false;

            Timing.RunCoroutine(OnModeStarted());

            Server.ExecuteCommand($"/mp load ru");
        }

        public IEnumerator<float> OnModeStarted()
        {
            foreach (var player in Player.List)
            {
                player.Role.Set(PlayerRoles.RoleTypeId.ClassD);
                player.Position = new Vector3(-3.09375f, 1003.158f, 33.52344f);
            }

            yield return Timing.WaitForSeconds(10f);

            while (RoundCount < 3)
            {
                bool end = true;

                Server.ExecuteCommand($"/ev run {Tools.GetRandomValue(Games)}");

                while (end)
                {
                    foreach (var player in Player.List)
                    {
                        if (Physics.Raycast(player.Position, Vector3.down, out RaycastHit hit, 10, (LayerMask)1))
                        {
                            if (hit.transform.name == "classname=brush.003")
                            {
                                end = false;
                                break;
                            }
                        }
                    }

                    yield return Timing.WaitForSeconds(1f);
                }

                RoundCount += 1;

                foreach (var player in Player.List)
                    player.Position = new Vector3(-3.09375f, 1003.158f, 33.52344f);

                if (RoundCount != 3)
                {
                    for (int i = 1; i < 10; i++)
                    {
                        foreach (var player in Player.List)
                        {
                            player.ClearBroadcasts();
                            player.Broadcast(2, $"<b><color=red>{10 - i}</color>초 후 <i><color=yellow>{RoundCount + 1}번째 라운드</color></i>가 시작됩니다.</b>");
                        }
                        yield return Timing.WaitForSeconds(1f);
                    }
                }
            }

            Round.IsLocked = false;
        }
    }
}