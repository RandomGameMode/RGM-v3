using MEC;
using PlayerRoles;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Exiled.API.Features;

using static RGM.Variables.Variable;

using RGM.API.Features;

namespace RGM.IEnumerators
{
    public static class LobbyIEnumerator
    {
        public static IEnumerator<float> GameStartButton()
        {
            int RemainingPress = 10;
            bool ButtonPressed = false;
            Transform redObject = null;

            while (!ButtonPressed)
            {
                if (!FreezeGameStart)
                {
                    bool pressing = false;

                    foreach (var player in PlayerManager.List)
                    {
                        if (player == null || !player.IsConnected)
                            continue;

                        if (Physics.Raycast(player.Position, Vector3.down, out RaycastHit hit, 1f, (LayerMask)1))
                        {
                            if (hit.transform.name == "GameStartRed")
                            {
                                if (PlayerManager.List.Count() > 1 && RemainingPress <= 0)
                                {
                                    ButtonPressed = true;
                                }

                                redObject = hit.transform;
                                pressing = true;

                                RemainingPress -= 1;

                                redObject.position = new Vector3(redObject.position.x, redObject.position.y - 0.015f, redObject.transform.position.z);
                            }
                        }
                    }

                    if (!pressing)
                    {
                        if (RemainingPress < 10)
                        {
                            RemainingPress += 1;

                            redObject.position = new Vector3(redObject.transform.position.x, redObject.transform.position.y + 0.015f, redObject.transform.position.z);
                        }
                    }
                }

                yield return Timing.WaitForSeconds(0.1f);
            }

            if (SelectMode == "RandomSelect")
                Tools.PickModes();

            foreach (var player in Player.List.Where(x => !x.IsDnd()))
            {
                player.ClearInventory();
                player.Role.Set(RoleTypeId.Spectator);
            }

            Round.Start();

            yield break;
        }

        public static IEnumerator<float> ModeResetButton()
        {
            float RemainingPress = 20;
            bool ButtonPressed = false;
            Transform redObject = null;

            while (!ButtonPressed)
            {
                bool pressing = false;
                RaycastHit hit;
                int stack = 0;

                foreach (var player in PlayerManager.List)
                {
                    if (player == null || !player.IsConnected)
                        continue;

                    if (Physics.Raycast(player.Position, Vector3.down, out hit, 1f, (LayerMask)1))
                    {
                        if (hit.transform.name == "ModeResetRed")
                        {
                            stack += 1;
                            redObject = hit.transform;
                            pressing = true;
                        }
                    }
                }

                if (PlayerManager.List.Count() > 1)
                {
                    float maxPlayers = 35;
                    float pressMultiplier = Math.Max(1, maxPlayers / Server.PlayerCount);
                    float pressAmount = 0.012f * pressMultiplier;

                    if (RemainingPress <= 0)
                        ButtonPressed = true;

                    if (pressing)
                    {
                        RemainingPress -= pressAmount * stack;

                        redObject.position = new Vector3(redObject.position.x, redObject.position.y - (0.00012f * stack * pressMultiplier), redObject.transform.position.z);
                    }
                }

                yield return Timing.WaitForSeconds(0.1f);
            }

            Tools.PickModes();
            Tools.MessageTranslated("", $"<mark=#ffff00aa><color=#000000><color=#ffffff>모드 투표 리스트</color>가 초기화되었습니다.</color></mark>");

            FreezeGameStart = true;

            yield return Timing.WaitForSeconds(10f);

            FreezeGameStart = false;

            yield break;
        }
    }
}
