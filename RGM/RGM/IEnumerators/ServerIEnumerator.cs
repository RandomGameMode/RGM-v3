using AdminToys;
using Christmas.Scp2536.Gifts;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Roles;
using Exiled.API.Features.Toys;
using MapGeneration.Holidays;
using MEC;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.PlayableScps.Scp1507;
using ProjectMER.Features;
using ProjectMER.Features.Objects;
using RGM.API.Features;
using RGM.API.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;
using static RGM.Variables.Variable;

namespace RGM.IEnumerators
{
    public static class ServerIEnumerator
    {
        public static IEnumerator<float> SyncSpectatedHint()
        {
            while (!Round.IsEnded)
            {
                foreach (var player in Player.List)
                {
                    switch (player.Role)
                    {
                        case OverwatchRole overwatch:
                        {
                            if (overwatch.SpectatedPlayer != null && overwatch.SpectatedPlayer.CurrentHint != null)
                            {
                                string content = overwatch.SpectatedPlayer.CurrentHint.Content;

                                if (player.IsUsingTranslator())
                                {
                                    TranslationManager.TranslatePreserveNewlines(content, TranslatorPlayers[player],
                                        translated =>
                                        {
                                            player.ShowHint(translated, 1.2f);
                                        }
                                    );
                                }
                                else
                                    player.ShowHint(content, 1.2f);
                            }

                            break;
                        }
                        case SpectatorRole spectator:
                        {
                            if (spectator.SpectatedPlayer != null && spectator.SpectatedPlayer.CurrentHint != null)
                            {
                                string content = spectator.SpectatedPlayer.CurrentHint.Content;

                                if (player.IsUsingTranslator())
                                {
                                    TranslationManager.TranslatePreserveNewlines(content, TranslatorPlayers[player],
                                        translated =>
                                        {
                                            player.ShowHint(translated, 1.2f);
                                        }
                                    );
                                }
                                else
                                    player.ShowHint(content, 1.2f);
                            }

                            break;
                        }
                    }
                }

                yield return Timing.WaitForSeconds(1);
            }
        }

        public static IEnumerator<float> ThrowawayBroadcast()
        {
            while (true)
            {
                yield return Timing.WaitForSeconds(Random.Range(4, 11) * 60f);

                if (HolidayUtils.IsHolidayActive(HolidayType.Christmas))
                {
                    TapeGift._canSpawn = true;
                    Scp1507Spawner.CurState = Scp1507Spawner.State.Idle;

                    Map.Broadcast(10, $"<b><size=25>플라밍고를 소환하는 테이프가 재사용 가능해집니다.</size></b>");
                }

                foreach (var player in Player.List)
                {
                    player.AddBroadcast(20, $"<size=20><b><color=#7289da>Discord</color>에 가입하여 <color=#C8FE2E>실시간 업데이트 현황</color>을 확인하고, 서버에 대한 <color=#F781D8>아이디어</color>를 나누고, <color=#FF4000>상점</color>을 이용하세요!</b></size>");
                    player.AddBroadcast(20, $"<size=20>만일 서버가 터진다면 그것은 개발자가 이상한 거 만들다가 가끔씩 터트리는 것이오니, 계속 플레이하시려면 고쳐질때까지 계속 <b>재접속</b>하시는 것을 권장드립니다.</size>");
                }
            }
        }

        public static IEnumerator<float> IsFallDown()
        {
            while (true)
            {
                foreach (var player in Player.List.Where(x => x.IsAlive))
                {
                    if (!OnGround.ContainsKey(player.UserId) || player.IsNoclipPermitted ||
                        player.Role.Type == RoleTypeId.Scp079) continue;
                    if (player.ReferenceHub.IsGrounded())
                        OnGround[player.UserId] = 6;

                    else
                    {
                        OnGround[player.UserId] -= 0.1f;

                        if (!(OnGround[player.UserId] <= 0)) continue;
                        if (Round.ElapsedTime.TotalSeconds < 10)
                        {
                            player.IsGodModeEnabled = true;

                            player.Position = PlayerManager.List.Where(x => x != player).GetRandomValue().Position;

                            Timing.CallDelayed(1, () =>
                            {
                                player.IsGodModeEnabled = false;
                            });
                        }
                        else
                        {
                            player.Kill("공허에 빨려들어갔습니다. (5초 이상 낙하)");
                        }

                        OnGround[player.UserId] = 6;
                    }
                }

                yield return Timing.WaitForSeconds(0.1f);
            }
        }

        public static IEnumerator<float> InputCooldown()
        {
            while (true)
            {
                ChatCooldown.Clear();
                EmotionCooldown.Clear();

                yield return Timing.WaitForSeconds(2f);
            }
        }

        public static IEnumerator<float> Ball()
        {
            while (!Round.IsStarted)
            {
                foreach (Player player in PlayerManager.List.Where(x => x.IsAlive))
                {
                    foreach (Transform ball in Balls)
                    {
                        GameObject _ball = ball.gameObject;

                        if (Vector3.Distance(_ball.transform.position, player.Position) < 2)
                        {
                            _ball.gameObject.TryGetComponent<Rigidbody>(out Rigidbody rig);
                            rig.AddForce(player.GameObject.transform.forward + new Vector3(0, 0.01f, 0), ForceMode.Impulse);
                        }

                        /*
                        else if (Vector3.Distance(_ball.transform.position, player.Position) > 45)
                            _ball.transform.position = new Vector3(player.Position.x, player.Position.y + 2, player.Position.z);
                        */
                    }
                }

                yield return Timing.WaitForOneFrame;
            }
        }

        public static IEnumerator<float> RenewalPlayersInfo()
        {
            while (true)
            {
                foreach (var player in PlayerManager.List.Where(x => PlayersInfo.ContainsKey(x.UserId) && x.IsAlive))
                {
                    PlayersInfo[player.UserId] = new PlayerInfo
                    {
                        RoleType = player.Role.Type,
                        MaxHealth = player.MaxHealth,
                        Health = player.Health,
                        ActiveEffects = player.ActiveEffects.ToList(),
                        Items = player.Items.ToList(),
                        CurrentItem = player.CurrentItem,
                        Position = new Vector3(player.Position.x, player.Position.y, player.Position.z)
                    };
                }

                yield return Timing.WaitForSeconds(1f);
            }
        }

        public static IEnumerator<float> HumanLoop()
        {
            while (!Round.IsEnded)
            {
                foreach (var player in PlayerManager.List.Where(x => x.IsHuman && !JumpScareCooldown.Contains(x)))
                {
                    if (!player.TryGetLookPlayer(25, out Player target, out _)) continue;
                    if (!target.IsScpRole()) continue;
                    JumpScareCooldown.Add(player);

                    Timing.CallDelayed(60, () =>
                    {
                        JumpScareCooldown.Remove(player);
                    });

                    PlayersAudio[player].TryPlay($"facingScp-{Random.Range(1, 7)}", volume: 2);
                                    
                    Timing.CallDelayed(3, () =>
                    {
                        PlayersAudio[player].TryPlay("chase", volume: 2);
                    });
                }

                yield return Timing.WaitForOneFrame;
            }
        }

        public static IEnumerator<float> Scp079Broadcast()
        {
            yield return Timing.WaitUntilTrue(() => Round.IsStarted);

            while (!Round.IsEnded)
            {
                if (Convert.ToInt16(Random.Range(1, 1001)) == 1)
                    Tools.PlayGlobalAudio($"scp079-{Random.Range(1, 3)}", volume: 1.5f);

                int citizenCount = PlayerManager.List.Count(x => x.Role.Type is RoleTypeId.ClassD or RoleTypeId.Scientist);

                switch (citizenCount)
                {
                    case 1 when !IsWarningAlone:
                        IsWarningAlone = true;

                        Tools.PlayGlobalAudio("scp079-4", volume: 1.2f);
                        break;
                    case 0 when !IsClearCitizen:
                        IsClearCitizen = true;

                        Tools.PlayGlobalAudio("scp079-3", volume: 1.2f);
                        break;
                }

                yield return Timing.WaitForSeconds(1);
            }
        }
        
        public static IEnumerator<float> Detonation()
        {
            while (!Round.IsEnded)
            {
                if (Warhead.IsDetonated)
                {
                    foreach (var player in PlayerManager.List)
                    {
                        if (player.CurrentRoom.Type == RoomType.Surface) continue;
                        if (GodModePlayers.Contains(player))
                            GodModePlayers.Remove(player);

                        player.Kill("제한된 구역입니다.");
                    }
                }

                yield return Timing.WaitForSeconds(1);
            }
        }

        public static IEnumerator<float> MovingShootingTarget()
        {
            Target1 = ShootingTargetToy.Get(PrefabHelper.Spawn(PrefabType.SportTarget).GetComponent<ShootingTarget>());
            Target2 = ShootingTargetToy.Get(PrefabHelper.Spawn(PrefabType.BinaryTarget).GetComponent<ShootingTarget>());

            Target1.Scale = new Vector3(0.5f, 0.5f, 0.5f);
            Target2.Scale = new Vector3(0.5f, 0.5f, 0.5f);

            void apply(ShootingTargetToy target, (float, float, float, float, float, float) vector3)
            {
                Player player = Player.List.OrderBy(x => Vector3.Distance(x.Position, target.Position)).FirstOrDefault();

                target.Position = new Vector3(Random.Range(vector3.Item1, vector3.Item2), Random.Range(vector3.Item3, vector3.Item4), Random.Range(vector3.Item5, vector3.Item6));
                target.Rotation = target.Rotation = Quaternion.LookRotation(player.Position - target.Position) * Quaternion.Euler(0, 90, 0);
            }

            while (true)
            {
                while (Server.PlayerCount == 0) yield return Timing.WaitForOneFrame;

                apply(Target1, (52.01f, 64.06f, 181.50f, 181.10f, -58.50f, -44.83f));
                apply(Target2, (50.37f, 44.47f, 181.50f, 181.61f, -58.74f, -48.45f));

                while (!ShootingTargetSignal) yield return Timing.WaitForOneFrame;

                ShootingTargetSignal = false;
            }
        }

        // 이거 어디서 사용되는지 체크 필요
        public static IEnumerator<float> TimezoneCheck()
        {
            DateTime lastProcessedDate = DateTime.MinValue.Date;
            bool isProcessing = false;
            bool pendingReset = false;

            object usersLock = UsersManager.UsersCache;

            TimeZoneInfo kst;
            try
            {
                kst = TimeZoneInfo.FindSystemTimeZoneById("Asia/Seoul");
            }
            catch
            {
                kst = TimeZoneInfo.Local;
            }

            try
            {
                var initNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, kst);
                lastProcessedDate = initNow.Date;
            }
            catch { }

            while (true)
            {
                try
                {
                    var nowKst = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, kst);
                    if (nowKst.Date != lastProcessedDate && !isProcessing)
                    {
                        pendingReset = true;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"[TimezoneCheck] time check error: {ex}");
                }

                if (pendingReset && !isProcessing)
                {
                    isProcessing = true;
                    pendingReset = false;

                    yield return Timing.WaitForSeconds(UnityEngine.Random.Range(0f, 30f));

                    try
                    {
                        var checkKst = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, kst);
                        if (checkKst.Date != lastProcessedDate)
                        {
                            int processed = 0, skipped = 0, resized = 0, notList = 0;

                            List<string> keysSnapshot;
                            lock (usersLock)
                            {
                                keysSnapshot = UsersManager.UsersCache.Keys.ToList();
                            }

                            lock (usersLock)
                            {
                                foreach (var steamId in keysSnapshot)
                                {
                                    if (!UsersManager.UsersCache.TryGetValue(steamId, out var data) || data == null)
                                    {
                                        skipped++;
                                        continue;
                                    }

                                    if (data is List<string> list)
                                    {
                                        if (list.Count < 30)
                                        {
                                            while (list.Count < 30) list.Add("0");
                                            resized++;
                                        }

                                        if (list[29] == "0")
                                            list[28] = "0";

                                        list[29] = "0";
                                        processed++;
                                    }
                                    else
                                    {
                                        notList++;
                                    }
                                }

                                UsersManager.SaveUsers();
                            }

                            lastProcessedDate = checkKst.Date;

                            Log.Info(
                                $"[TimezoneCheck] Daily reset done ({lastProcessedDate:yyyy-MM-dd}) " +
                                $"processed={processed}, skipped={skipped}, resized={resized}, notList={notList}, totalKeys={keysSnapshot.Count}"
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"[TimezoneCheck] reset error: {ex}");
                    }

                    isProcessing = false;
                }

                yield return Timing.WaitForSeconds(60f);
            }
        }

        public static IEnumerator<float> ScpGlow(Player owner)
        {
            SchematicObject schematic = ObjectSpawner.SpawnSchematic("Light", Vector3.zero);
            LightSourceToy light = schematic.GetComponentsInChildren<LightSourceToy>().First();

            bool flag = false;

            while (owner.IsScp)
            {
                switch (flag)
                {
                    case false when owner.CurrentItem != null && owner.Role is not Scp3114Role
                    {
                        DisguiseStatus: PlayerRoles.PlayableScps.Scp3114.Scp3114Identity.DisguiseStatus.Active
                    }:
                    {
                        flag = true;

                        schematic.transform.parent = owner.Transform;
                        schematic.transform.localPosition = Vector3.zero;

                        if (ColorUtility.TryParseHtmlString("#ffff00", out Color color))
                            light.NetworkLightColor = color;
                        light.NetworkLightRange = 32;
                        light.NetworkLightIntensity = 8;
                        break;
                    }
                    case true when owner.CurrentItem == null:
                        flag = false;

                        schematic.transform.parent = null;
                        schematic.transform.position = Vector3.zero;
                        break;
                }

                yield return Timing.WaitForSeconds(1);
            }

            schematic.Destroy();
        }
    }
}
