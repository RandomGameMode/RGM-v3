using AFK;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Doors;
using Exiled.API.Features.Items;
using Exiled.Events.EventArgs.Player;
using MEC;
using Mirror;

using PlayerRoles;
using RGM.API.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Extensions;
using UnityEngine;
using static RGM.Variables.Variable;

namespace RGM.Modes
{
    [Mode(ModeCategory.Public, ModeInfo.Set, ModeType.HotPotato)]
    public class HotPotato : Mode
    {
        public override string Name => "폭탄 돌리기";
        public override string Description => "폭탄이 터지기 전에 다른 유저에게 넘기세요!";

        public override string Detail =>
            """
            유저에 비례하여 <b>폭탄 플레이어의 수가 조정</b>됩니다. (6명 당 1마리 + 1마리)

            어떤 수단을 사용하더라도 최후까지 살아남으세요!
            """;

        public override string Color => "FA58D0";
        public override string Map => "hp";

        public static HotPotato Instance;

        private readonly List<Player> _pl = [];
        private readonly List<Player> _bomberMans = [];

        private CoroutineHandle _onModeStarted;

        private AudioClipPlayback _audio;

        public override void OnEnabled()
        {
            Server.FriendlyFire = true;
            Round.IsLocked = true;
            Respawn.PauseWaves();
            AFKManager._kickTime = 120500;

            Exiled.Events.Handlers.Player.InteractingDoor += OnInteractingDoor;
            Exiled.Events.Handlers.Player.Spawned += OnSpawned;
            Exiled.Events.Handlers.Player.Hurting += OnHurting;
            Exiled.Events.Handlers.Player.Died += OnDied;

            _onModeStarted = Timing.RunCoroutine(OnModeStarted());

            _audio = Tools.PlayGlobalAudio("Skeleton", 1, true);
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Player.InteractingDoor -= OnInteractingDoor;
            Exiled.Events.Handlers.Player.Spawned -= OnSpawned;
            Exiled.Events.Handlers.Player.Hurting -= OnHurting;
            Exiled.Events.Handlers.Player.Died -= OnDied;

            Timing.KillCoroutines(_onModeStarted);

            _audio.IsPaused = true;
        }

        private IEnumerator<float> OnModeStarted()
        {
            PlayerManager.List.Where(x => !x.IsNPC).CopyTo(_pl);

            foreach (var door in Door.List) door.Lock(DoorLockType.Warhead);

            foreach (var player in PlayerManager.List.Where(x => !x.IsNPC))
            {
                player.Role.Set(RoleTypeId.ClassD);
                player.Position = GameObject.Find("[SP] Base").transform.position;
            }

            while (true)
            {
                _pl.ShuffleList();

                for (var i = 1; i < _pl.Count / 6 + 2; i++)
                    try
                    {
                        var bomberMan = _pl.Where(x => _pl.Contains(x) && !_bomberMans.Contains(x)).ToList()
                            .GetRandomValue();

                        if (bomberMan != null && bomberMan.IsConnected)
                        {
                            bomberMan.Role.Set(RoleTypeId.Scp049, SpawnReason.ForceClass, RoleSpawnFlags.None);
                            _bomberMans.Add(bomberMan);
                        }
                        else
                        {
                            _pl.Remove(bomberMan);
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }

                for (var i = 10; i > 0; i--)
                {
                    PlayerManager.List.ToList().ForEach(x => x.AddHint("폭탄돌리기 폭탄 알림",
                        $"현재 폭탄 : {string.Join(" & ", _bomberMans.Select(b => b.DisplayNickname))}\n{i}초 후 폭탄이 터집니다.",
                        1.1f));

                    yield return Timing.WaitForSeconds(1);
                }

                foreach (var bomber in _bomberMans)
                    try
                    {
                        if (!_pl.Contains(bomber)) continue;
                        _pl.Remove(bomber);

                        var g = (ExplosiveGrenade)Item.Create(ItemType.GrenadeHE, Server.Host);
                        g.FuseTime = 0.1f;
                        g.MaxRadius = 0;
                        g.SpawnActive(bomber.Position, Server.Host);

                        bomber.Kill("폭탄을 넘기지 못해서 죽어버렸다네~");
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }

                _bomberMans.Clear();

                if (_pl.Count < 2)
                {
                    Round.IsLocked = false;

                    _pl[0].Role.Set(RoleTypeId.ClassD, SpawnReason.ForceClass, RoleSpawnFlags.None);
                    PlayerManager.List.ToList().ForEach(x => x.AddBroadcast(20, $"승리자 : {_pl[0].DisplayNickname}"));
                    Timing.RunCoroutine(Tools.SetWinner([_pl[0]], 5));
                    break;
                }

                PlayerManager.List.ToList().ForEach(x => x.AddHint("폭탄돌리기 펑", "펑!", 2));

                yield return Timing.WaitForSeconds(2f);
            }
        }

        private void OnInteractingDoor(InteractingDoorEventArgs ev)
        {
            ev.IsAllowed = false;
        }

        private void OnSpawned(SpawnedEventArgs ev)
        {
            Server.ExecuteCommand($"/speak {ev.Player.Id} 1");
            IntercomPlayers.Add(ev.Player);

            ev.Player.GetEffect(EffectType.MovementBoost).Intensity = 50;
        }

        private void OnHurting(HurtingEventArgs ev)
        {
            if (ev.DamageHandler.Type is DamageType.Scp049 or DamageType.CardiacArrest)
                ev.IsAllowed = false;

            if (!_bomberMans.Contains(ev.Attacker) || _bomberMans.Contains(ev.Player) || ev.Player.IsNPC) return;
            _bomberMans.Remove(ev.Attacker);
            _bomberMans.Add(ev.Player);

            Server.ExecuteCommand($"/fc {ev.Attacker.Id} ClassD 0");
            Server.ExecuteCommand($"/fc {ev.Player.Id} Scp049 0");
        }

        private void OnDied(DiedEventArgs ev)
        {
            if (!_pl.Contains(ev.Player)) return;
            _pl.Remove(ev.Player);

            if (_pl.Count >= 2) return;
            Round.IsLocked = false;

            PlayerManager.List.ToList().ForEach(x => x.AddBroadcast(20, $"승리자 : {_pl[0].DisplayNickname}"));
            Timing.RunCoroutine(Tools.SetWinner([_pl[0]], 5));
        }
    }
}
