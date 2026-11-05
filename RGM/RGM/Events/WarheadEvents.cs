using System;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Doors;
using Exiled.Events.EventArgs.Warhead;
using MEC;
using PlayerRoles;
using RGM.API.Features;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static RGM.Variables.Variable;

namespace RGM.Events
{
    public static class WarheadEvents
    {
        public static IEnumerator<float> OnDetonating(DetonatingEventArgs ev)
        {
            foreach (var door in Door.List)
            {
                if (door is BreakableDoor breakableDoor)
                    breakableDoor.IsDestroyed = true;
            }

            foreach (var player in PlayerManager.List.Where(ShouldDieFromWarhead))
            {
                if (GodModePlayers.Contains(player))
                    GodModePlayers.Remove(player);

                // Custom(string) Kill은 DamageType.Warhead가 아니어서 무적 예외가 적용되지 않음
                player.Kill(DamageType.Warhead);
            }

            Timing.CallDelayed(2 * 60, () => 
                GlobalPlayer.TryPlay("SCP - Breach"));

            yield return Timing.WaitForSeconds(300);

            Tools.MessageTranslated("", "시간이 너무 오래 걸립니다! 모두의 체력이 점차 줄어듭니다!");
            PlayerManager.List.ToList().ForEach(x => x.EnableEffect(EffectType.PocketCorroding));
            
            var x = 1f;

            while (!Round.IsEnded)
            {
                var fx = (float)Math.Tanh(x / 60);
                
                foreach (var player in PlayerManager.List)
                {
                    player.Health -= player.MaxHealth * fx;
                    
                    if (player.Health <= 0 && player.IsAlive)
                        player.Kill("게임을 질질 끌어서 죽었습니다.");
                }
                x += 0.5f;
                yield return Timing.WaitForSeconds(0.5f);
            }
        }

        private static bool ShouldDieFromWarhead(Player player)
        {
            if (!player.IsAlive)
                return false;

            // SCP-079 Zone은 현재 카메라 기준이라 Surface면 기존 필터에서 빠짐
            if (player.Role.Type == RoleTypeId.Scp079)
                return true;

            if (player.Zone != ZoneType.Surface)
                return true;

            return Physics.RaycastAll(player.Position, Vector3.down, 5, (LayerMask)1).Any(hit =>
                hit.transform.parent != null &&
                hit.transform.parent.name == "ElevatorChamber Gates(Clone)");
        }
    }
}
