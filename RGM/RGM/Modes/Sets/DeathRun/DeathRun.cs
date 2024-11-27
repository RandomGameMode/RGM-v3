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

namespace RGM.Modes
{
    [Mode(ModeCategory.Private, ModeInfo.Set, ModeType.DeathRun)]
    public class DeathRun : Mode
    {
        public override string Name => "데스런";
        public override string Description => "과학자는 죄수들의 접근을 막아야 합니다. 널리 퍼져 있는 함정들을 조심하십시오!";
        public override string Detail =>
"""
<color=yellow>과학자</color>의 경우, <color=red>빨간 버튼</color>을 눌러 함정을 발동시킬 수 있습니다.
단, 함정은 1번만 작동시킬 수 있으므로, 한번에 최대한 많은 <color=orange>D계급</color>들을 죽이세요.
모든 <color=orange>D계급</color>을 죽이거나, 180초를 버티면 <color=yellow>과학자</color>의 승리입니다.

<color=orange>D계급</color>의 경우, 목적지까지 빠르게 도달해야 합니다.
목적지에 도달한 경우 <color=yellow>과학자</color>를 사살하여 승리할 수 있게 됩니다. 총을 얻게 되는 거죠!
""";
        public override string Color => "FF4000";

        public static DeathRun Instance;

        Player Tagger = null;

        public override void OnEnabled()
        {
            foreach (var spawn in Respawning.WaveManager.Waves) spawn.Destroy();

            Timing.RunCoroutine(OnModeStarted());
        }

        public IEnumerator<float> OnModeStarted()
        {
            Server.ExecuteCommand("/mp load DeathRun");

            Tagger = Tools.GetRandomValue(Player.List.ToList());

            Tagger.Role.Set(RoleTypeId.Scientist);
            Tagger.Position = new Vector3(44.57422f, 999.7067f, 78.52734f);
            Tagger.ClearInventory();
            Tagger.ShowHint("곧 <color=orange>D계급</color>이 데스런에 입장합니다.\n그들을 막기 위해 <color=red>빨간 버튼</color>을 눌러 일회용 함정을 발동시킬 수 있습니다.\n\n<size=40><b>절대로 그들이 목적지에 도착하지 않도록 하세요!</b></size>", 10);

            foreach (var player in Player.List.Where(x => x != Tagger))
            {
                player.Role.Set(RoleTypeId.ClassD);
                player.Position = new Vector3(48.86719f, 1002.6483f, 86.34375f);
                Timing.CallDelayed(1f, () => player.EnableEffect(EffectType.Ensnared));
            }

            for (int i=1; i<11; i++)
            {
                foreach (var player in Player.List)
                {
                    player.ClearPlayerBroadcasts();
                    player.AddBroadcast(2, $"<size=30><b><color=red>{11 - i}</color>초 후 게임이 시작됩니다. 준비하세요!</b></size>");
                }
                yield return Timing.WaitForSeconds(1f);
            }

            Timing.RunCoroutine(Timer());

            foreach (var player in Player.List.Where(x => x != Tagger))
                player.DisableEffect(EffectType.Ensnared);
        }

        public IEnumerator<float> Timer()
        {
            for (int i=1; i<201; i++)
            {
                foreach (var player in Player.List)
                {
                    player.ClearPlayerBroadcasts();
                    player.AddBroadcast(2, $"<size=25><b><color=yellow>과학자</color>가 총기를 입수하기까지 <color=red>{201 - i}</color>초 남았습니다.</b></size>");
                }
                yield return Timing.WaitForSeconds(1f);
            }

            foreach (var player in Player.List.Where(x => x.IsAlive))
            {
                if (player != Tagger)
                    player.Role.Set(RoleTypeId.Tutorial, SpawnReason.ForceClass, RoleSpawnFlags.None);
            }

            Tagger.AddItem(ItemType.GunE11SR);
            for (int i=1; i<11; i++)
                Tagger.AddItem(ItemType.Ammo556x45);
        }
    }
}
