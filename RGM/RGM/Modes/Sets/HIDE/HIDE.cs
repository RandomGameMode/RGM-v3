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
using MultiBroadcast;
using PlayerRoles;
using UnityEngine;
using Exiled.API.Enums;
using RGM.API.Features;

namespace RGM.Modes
{
    [Mode(ModeCategory.Public, ModeInfo.Set, ModeType.HIDE)]
    class HIDE : Mode
    {
        public override string Name => "HIDE";
        public override string Description => "숨 죽이는 그를 사살하십시오.";
        public override string Detail =>
"""
<color=red>SCP-3114</color>는 피격당하거나 공격하면 투명이 해제됩니다.
또한, 평상시에도 반투명 상태로 존재합니다. 이 점을 잘 이용해보세요!
""";
        public override string Color => "0489B1";

        public static HIDE Instance;

        public List<Player> pl = new List<Player>();
        public Player monster = null;


        public override void OnEnabled()
        {
            foreach (var spawn in Respawning.WaveManager.Waves) spawn.Destroy();
            Server.ExecuteCommand($"/el l all");
            Server.ExecuteCommand($"/close **");
            Server.ExecuteCommand($"/lock **");

            Exiled.Events.Handlers.Player.Hurting += OnHurting;
            Exiled.Events.Handlers.Player.Hurt += OnHurt;
            Exiled.Events.Handlers.Player.Jumping += OnJumping;

            Timing.RunCoroutine(OnModeStarted());
            Timing.RunCoroutine(Timer());
            Timing.RunCoroutine(Invisible());
        }

        public IEnumerator<float> OnModeStarted()
        {
            Server.ExecuteCommand($"/mp load container");

            Player.List.ToList().CopyTo(pl);
            monster = Tools.GetRandomValue(Player.List.ToList());

            try
            {
                Timing.CallDelayed(1f, () =>
                {
                    monster.Role.Set(RoleTypeId.Scp3114);
                    monster.Group = new UserGroup { BadgeText = "MONSTER", BadgeColor = "red" };
                    monster.Position = new Vector3(-15.84375f, 1001.957f, 49.89063f);
                    Server.ExecuteCommand($"/open ESCAPE_PRIMARY");

                    float health = 100 * Player.List.Count + 20 * Player.List.Count;
                    monster.MaxHealth = health;
                    monster.Health = health;
                    monster.IsUsingStamina = false;
                    monster.EnableEffect(EffectType.MovementBoost, 50);

                    foreach (var player in Player.List)
                    {
                        if (player != monster)
                        {
                            player.Role.Set(RoleTypeId.NtfPrivate);
                            player.Position = new Vector3(18.08594f, 1001.957f, 15.34766f);
                            for (int i = 1; i < 10; i++)
                                player.AddItem(ItemType.Ammo9x19);
                        }
                    }


                });
            }
            catch (Exception e)
            {
                ServerConsole.AddLog(e.ToString());
            }

            while (true)
            {
                foreach (var obj in MapEditorReborn.API.API.SpawnedObjects)
                {
                    if (obj.name == "CustomSchematic-MonsterCapsule")
                        obj.Position = monster.Position;
                }
                yield return Timing.WaitForSeconds(0.01f);
            }
        }

        public IEnumerator<float> Timer()
        {
            for (int i = 1; i < 180; i++)
            {
                Player.List.ToList().ForEach(x => x.Broadcast(2, $"<size=25><color=#2ECCFA>NTF 승리</color>까지</color> {180 - i}초</size>", shouldClearPrevious: true));
                yield return Timing.WaitForSeconds(1f);
            }

            foreach (var player in Player.List)
            {
                if (player.IsScp)
                    player.Kill("제한시간이 초과하였습니다.");
            }
        }

        public IEnumerator<float> Invisible()
        {
            while (true)
            {
                foreach (var player in Player.List)
                {
                    if (player == monster)
                        player.EnableEffect(EffectType.Invisible);
                }

                yield return Timing.WaitForSeconds(2f);
            }
        }

        public void OnHurting(Exiled.Events.EventArgs.Player.HurtingEventArgs ev)
        {
            if (ev.Attacker.IsScp && ev.DamageHandler.Type != DamageType.Strangled)
                ev.DamageHandler.Damage += 35;
        }

        public void OnHurt(Exiled.Events.EventArgs.Player.HurtEventArgs ev)
        {
            if (ev.Attacker != null && ev.Attacker == monster)
                ev.Attacker.DisableEffect(EffectType.Invisible);

            if (ev.Player == monster)
                ev.Player.DisableEffect(EffectType.Invisible);
        }

        public IEnumerator<float> OnJumping(Exiled.Events.EventArgs.Player.JumpingEventArgs ev)
        {
            if (ev.Player == monster)
            {
                for (int i = 1; i < 11; i++)
                {
                    if (Physics.Raycast(monster.Position, Vector3.up, out RaycastHit hit, 1, (LayerMask)1))
                        break;

                    ev.Player.Position += new Vector3(0, 0.3f, 0);
                    yield return Timing.WaitForSeconds(0.01f);
                }
            }
        }
    }
}
