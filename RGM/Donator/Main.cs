using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using PlayerRoles;
using RGM.API.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerStatsSystem;
using MapEditorReborn.API.Features;
using MapEditorReborn.API.Features.Objects;
using MEC;
using UnityEngine;
using Exiled.API.Features.Toys;
using MapEditorReborn.Commands.ModifyingCommands.Rotation;
using RGM.API.Interfaces;

using static RGM.Variables.ServerManagers;
using InventorySystem.Items.Firearms.ShotEvents;
using InventorySystem.Items.Firearms;

namespace RGM.Donator
{
    public class Main
    {
        public static Main Instance;

        public IEnumerator<float> KillEffect(List<string> PlayerData, Player Attacker, Player Player)
        {
            Quaternion Rotation = new Quaternion(0, Attacker.CameraTransform.rotation.y + 180, 0, 0);

            if (PlayerData[4] == "영혼 가출")
            {
                DamageHandlerBase DisruptorDamage = new DisruptorDamageHandler(new DisruptorShotEvent(new Firearm(), InventorySystem.Items.Firearms.Modules.DisruptorActionModule.FiringState.None), Vector3.up, -1);

                Ragdoll.CreateAndSpawn(Player.Role.Type, PlayerData[4], DisruptorDamage, Player.Position, Rotation);
            }

            if (PlayerData[4] == "솔라 테라")
            {
                SchematicObject SolarTerra = ObjectSpawner.SpawnSchematic("SolarTerra", Player.Position, Rotation, isStatic: false);

                Timing.CallDelayed(1.5f, SolarTerra.Destroy);
            }

            if (PlayerData[4] == "Kerfus")
            {
                SchematicObject Kerfus = ObjectSpawner.SpawnSchematic("Kerfusa", Player.Position + new Vector3(0, 19, 0), Rotation, isStatic: false);

                for (int i = 1; i < 11; i++)
                {
                    Kerfus.Position += new Vector3(0, -2f, 0);

                    yield return Timing.WaitForSeconds(0.05f);
                }

                yield return Timing.WaitForSeconds(1.5f);

                for (int i = 1; i < 11; i++)
                {
                    Kerfus.Position += new Vector3(0, 2f, 0);

                    yield return Timing.WaitForSeconds(0.05f);
                }

                Kerfus.Destroy();
            }

            if (PlayerData[4] == "은제 말뚝")
            {
                SchematicObject SilverStake = ObjectSpawner.SpawnSchematic("SilverStake", Player.Position, Rotation, isStatic: false);

                Timing.CallDelayed(1.5f, SilverStake.Destroy);
            }

            if (PlayerData[4] == "KO 사인")
            {
                SchematicObject KO = ObjectSpawner.SpawnSchematic("KO", Player.Position, Rotation, isStatic: false);

                Timing.CallDelayed(1.5f, KO.Destroy);
            }
        }

        public void OnEnabled()
        {
            Exiled.Events.Handlers.Player.Dying += OnDying;

            Timing.RunCoroutine(CustomermizingRotation());
        }

        public void OnDying(Exiled.Events.EventArgs.Player.DyingEventArgs ev)
        {
            if (ev.Attacker != null && UsersManager.UsersCache.ContainsKey(ev.Attacker.UserId))
            {
                List<string> Attacker = UsersManager.UsersCache[ev.Attacker.UserId];
                List<string> Player = UsersManager.UsersCache[ev.Attacker.UserId];

                if (Attacker[4] != "0")
                    KillEffect(Attacker, ev.Attacker, ev.Player);
            }
        }

        public IEnumerator<float> CustomermizingRotation()
        {
            while (true)
            {
                try
                {
                    foreach (var player in Player.List.Where(x => !x.IsNPC))
                    {
                        if (UsersManager.UsersCache.ContainsKey(player.UserId))
                        {
                            Dictionary<string, PlayerReport> pr = PlayersReport;

                            List<string> userValues = UsersManager.UsersCache[player.UserId];

                            string Formatter(string str)
                            {
                                return str
                                    .Replace("\\n", "\n")
                                    .Replace("{name}", player.Nickname)
                                    .Replace("{kill}", $"{pr[player.UserId].Kill}")
                                    .Replace("{death}", $"{pr[player.UserId].Death}")
                                    .Replace("{revive}", $"{pr[player.UserId].Revive}")
                                    .Replace("{kill_scp}", $"{pr[player.UserId].KillScp}")
                                    .Replace("{kill_human}", $"{pr[player.UserId].KillHuman}")
                                    .Replace("{max_health}", $"{player.MaxHealth}")
                                    .Replace("{health}", $"{player.Health}")
                                    .Replace("{items_count}", $"{player.Items.Count}")
                                    ;
                            }

                            if (userValues[5] != "0")
                                player.DisplayNickname = Formatter(userValues[5]);

                            else
                                player.DisplayNickname = "";

                            if (userValues[6] != "0")
                                player.CustomInfo = Formatter(userValues[6]);

                            else
                                player.CustomInfo = "";
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }

                yield return Timing.WaitForSeconds(1f);
            }
        }
    }
}
