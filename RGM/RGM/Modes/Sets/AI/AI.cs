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
using PlayerRoles.FirstPersonControl;
using Exiled.Events.EventArgs.Player;

namespace RGM.Modes
{
    [Mode(ModeCategory.Private, ModeInfo.Set, ModeType.AI)]
    class AI : Mode
    {
        public override string Name => "섬뜩한 힘";
        public override string Description => "SCP들이 전부 A.I.로 대체되고 데미지를 5%만 받습니다. (최대 4개체)";
        public override string Detail =>
"""
<b>[A.I. 리스트]</b>
<color=red>SCP-173</color>
<color=red>SCP-049</color>
<color=red>SCP-096</color>
<color=red>SCP-106</color>
""";
        public override string Color => "7401DF";

        public static AI Instance;

        public override void OnEnabled()
        {
            Exiled.Events.Handlers.Player.Hurting += OnHurting;

            Timing.RunCoroutine(OnModeStarted());
            Timing.RunCoroutine(BlockFalldown());
        }

        public IEnumerator<float> OnModeStarted()
        {
            foreach (var p in Player.List.Where(x => x.IsScp))
                p.Role.Set(RoleTypeId.ClassD);

            foreach (var aiscprole in Datas.AIRoles)
                Server.ExecuteCommand($"/spawnai {aiscprole.ToString()}");

            yield break;
        }

        public IEnumerator<float> BlockFalldown()
        {
            while (true)
            {
                foreach (var ai in Player.List.Where(x => x.IsNPC))
                {
                    if (ai.Position.y < -2000)
                        Server.ExecuteCommand($"/tpx {ai.Id} {Tools.GetRandomValue(Player.List.Where(x => x.IsNPC && x != ai).ToList()).Id}");
                }

                yield return Timing.WaitForSeconds(1f);
            }
        }

        public void OnHurting(HurtingEventArgs ev)
        {
            if (ev.Player.IsScp)
                ev.DamageHandler.Damage /= 20;
        }
    }
}
