using System.Collections.Generic;
using System.Linq;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using MEC;

using PlayerRoles;
using RGM.API.Features;
using static RGM.Variables.Variable;

namespace RGM.Modes
{
    [Mode(ModeCategory.Public, ModeInfo.Set, ModeType.LastOne)]
    class LastOne : Mode
    {
        public override string Name => "라스트 원";
        public override string Description => "혼자서 끝까지 살아남으세요.";
        public override string Detail =>
"""
랜덤한 맵에서, 최후까지 살아남아 승리를 쟁취하세요.

제한 시간은 2분이며, 2분이 지나면 버스터콜이 발생합니다.

<b>[Map Credit]</b>
@vasileii, @sleeplessbutter
""";
        public override string Color => "F8E0E6";
        public override string Map => Maps.GetRandomValue();

        public static LastOne Instance;

        private List<ItemType> _startupItems = [];

        private CoroutineHandle _onModeStarted;

        public override void OnEnabled()
        {
            Server.FriendlyFire = true;
            Round.IsLocked = true;
            Respawn.PauseWaves();

            Exiled.Events.Handlers.Player.Died += OnDied;
            Exiled.Events.Handlers.Player.DroppingItem += OnDroppingItem;
            Exiled.Events.Handlers.Player.DroppingAmmo += OnDroppingAmmo;
            Exiled.Events.Handlers.Player.Shot += OnShot;

            _onModeStarted = Timing.RunCoroutine(OnModeStarted());
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Player.Died -= OnDied;
            Exiled.Events.Handlers.Player.DroppingItem -= OnDroppingItem;
            Exiled.Events.Handlers.Player.DroppingAmmo -= OnDroppingAmmo;
            Exiled.Events.Handlers.Player.Shot -= OnShot;

            Timing.KillCoroutines(_onModeStarted);
        }

        private IEnumerator<float> OnModeStarted()
        {
            _startupItems = Items();

            foreach (var player in PlayerManager.List)
            {
                player.Role.Set(RoleTypeId.Tutorial);
                player.Position = Tools.GetObjectList("Spot Random").GetRandomValue().position;
                foreach (var item in _startupItems)
                    player.AddItem(item);
            }

            yield return Timing.WaitForSeconds(120f);

            Player BusterCall = PlayerManager.List.Where(x => x.IsAlive).ToList().GetRandomValue();

            foreach (var player in PlayerManager.List)
            {
                player.Position = BusterCall.Position;
                player.AddBroadcast(20, "<b><size=30>[<color=yellow>버스터콜</color>]</size></b>\n<size=20>모두가 한자리에 모입니다.</size>");
            }
        }

        private List<ItemType> Items()
        {
            List<ItemType> guns =
            [
                ItemType.GunA7, 
                ItemType.GunE11SR, 
                ItemType.GunShotgun, 
                ItemType.GunCom45,
                ItemType.GunRevolver,
                ItemType.GunCOM18, 
                ItemType.GunCrossvec, 
                ItemType.GunLogicer, 
                ItemType.GunFRMG0, 
                ItemType.GunAK
            ];
            
            List<ItemType> cdItems = 
            [
                ItemType.Medkit, 
                ItemType.Painkillers, 
                ItemType.Radio, 
                ItemType.GrenadeFlash
            ];
            
            List<ItemType> items = [guns.GetRandomValue()];

            foreach (var item in cdItems)
            {
                if (UnityEngine.Random.Range(1, 3) == 1)
                    items.Add(item);
            }

            return items;
        }

        private void OnDied(DiedEventArgs ev)
        {
            List<Player> pl = [.. PlayerManager.List.Where(x => x.IsAlive)];

            if (pl.Count >= 2) return;
            Round.IsLocked = false;

            Timing.RunCoroutine(Tools.SetWinner([pl[0]], 5));
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
