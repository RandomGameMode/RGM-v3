using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Doors;
using Exiled.Events.EventArgs.Player;
using MEC;

using PlayerRoles;
using RGM.API.Features;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Extensions;

namespace RGM.Modes
{
    [Mode(ModeCategory.Public, ModeInfo.Set, ModeType.ClassSociety)]
    public class ClassSociety : Mode
    {
        public override string Name => "계급 사회";
        public override string Description => "자신보다 낮은 숫자를 사살할 수 있습니다. (예외: 1 > 10)";
        public override string Detail =>
"""
모든 D계급은 1부터 10까지 랜덤한 숫자를 부여받습니다.
대신 1은 10을 처치할 수 있습니다. (10은 1 처치 불가)
처치하면 신분이 한 단계 상승합니다.
같은 격의 신분은 서로를 사살할 수 있습니다.
2명만 남았을 경우 둘 다 10이 됩니다.
""";
        public override string Color => "50eb7f";

        public static ClassSociety Instance;

        private CoroutineHandle _onModeStarted;
        private CoroutineHandle _display;

        public override void OnEnabled()
        {
            Server.FriendlyFire = true;
            Round.IsLocked = true;
            Respawn.PauseWaves();

            Exiled.Events.Handlers.Player.Hurting += OnHurting;
            Exiled.Events.Handlers.Player.Died += OnDied;
            Exiled.Events.Handlers.Player.DroppingItem += OnDroppingItem;
            Exiled.Events.Handlers.Player.DroppingAmmo += OnDroppingAmmo;
            Exiled.Events.Handlers.Player.Shot += OnShot;

            _onModeStarted = Timing.RunCoroutine(OnModeStarted());
            _display = Timing.RunCoroutine(Display());
        }

        public override void OnDisabled()
        {
            Server.FriendlyFire = false;
            Round.IsLocked = false;
            Respawn.ResumeWaves();

            Exiled.Events.Handlers.Player.Hurting -= OnHurting;
            Exiled.Events.Handlers.Player.Died -= OnDied;
            Exiled.Events.Handlers.Player.DroppingItem -= OnDroppingItem;
            Exiled.Events.Handlers.Player.DroppingAmmo -= OnDroppingAmmo;
            Exiled.Events.Handlers.Player.Shot -= OnShot;

            Timing.KillCoroutines(_onModeStarted);
            Timing.KillCoroutines(_display);
        }

        private IEnumerator<float> OnModeStarted()
        {
            Exiled.API.Features.Map.CleanAllItems();

            foreach (var door in Door.List.Where(x => x.IsCheckpoint))
            {
                door.Lock(DoorLockType.SpecialDoorFeature);
            }

            foreach (var player in PlayerManager.List)
            {
                player.Role.Set(RoleTypeId.ClassD);
                player.AddItem(ItemType.GunCOM15);
                player.RankName = $"{UnityEngine.Random.Range(1, 11)}";
            }

            yield return Timing.WaitForSeconds(60f);

            foreach (var door in Door.List)
            {
                door.IsOpen = false;
                door.Lock(DoorLockType.SpecialDoorFeature);
            }

            yield return Timing.WaitForSeconds(1);

            Player BusterCall = PlayerManager.List.Where(x => x.IsAlive).ToList().GetRandomValue();

            foreach (var player in PlayerManager.List)
            {
                player.Position = BusterCall.Position;
                player.AddBroadcast(20, "<b><size=30>[<color=yellow>버스터콜</color>]</size></b>\n<size=20>모두가 한자리에 모입니다.</size>");
            }
        }

        private IEnumerator<float> Display()
        {
            while (!Round.IsEnded)
            {
                foreach (var player in PlayerManager.List)
                {
                    player.AddHint($"계급 사회", $"<size=20>당신의 계급: {player.RankName}</size>", 1);
                }

                yield return Timing.WaitForSeconds(1);
            }
        }

        private void OnHurting(HurtingEventArgs ev)
        {
            if (ev.Attacker == null) return;
            int attackerLevel = int.Parse(ev.Attacker.RankName);
            int playerLevel = int.Parse(ev.Player.RankName);

            switch (attackerLevel)
            {
                case 10 when playerLevel == 1:
                    ev.IsAllowed = false;
                    break;
                case 1 when playerLevel == 10:
                    break;
                default:
                {
                    if (attackerLevel >= playerLevel)
                    {

                    }
                    else
                    {
                        ev.IsAllowed = false;
                    }

                    break;
                }
            }
        }

        private void OnDied(DiedEventArgs ev) 
        {
            if (ev.Attacker != null)
            {
                int attackerLevel = int.Parse(ev.Attacker.RankName);

                ev.Attacker.RankName = attackerLevel == 10 ? "1" : $"{attackerLevel + 1}";

                ev.Player.RankName = "탈락";
                ev.Player.RankColor = "red";
            }

            var players = PlayerManager.List.Where(x => x.IsAlive && !x.IsNPC).ToList();

            switch (players.Count)
            {
                case 2:
                {
                    foreach (var player in players)
                    {
                        player.RankName = "10";
                        player.ClearInventory();
                        player.AddItem(ItemType.GunLogicer);
                    }

                    break;
                }
                case < 2:
                    Round.IsLocked = false;

                    Timing.RunCoroutine(Tools.SetWinner(players.ToList(), 5));
                    break;
            }
        }

        private void OnDroppingItem(DroppingItemEventArgs ev)
        {
            ev.IsAllowed = false;
        }

        private void OnDroppingAmmo(DroppingAmmoEventArgs ev)
        {
            ev.IsAllowed = false;
        }

        private void OnShot(ShotEventArgs ev)
        {
            ev.Player.AddAmmo(ev.Firearm.AmmoType, 1);
        }
    }
}
