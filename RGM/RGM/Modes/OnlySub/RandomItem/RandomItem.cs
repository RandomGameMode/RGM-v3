using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Features;
using MEC;
using RGM.API.Features;
using PlayerRoles;
using Exiled.API.Extensions;
using InventorySystem.Items;

namespace RGM.Modes
{
    [Mode(ModeCategory.OnlySub, ModeInfo.Plus, ModeType.RandomItem)]
    public class RandomItem : Mode
    {
        private static bool _isEnabled;
        
        public override string Name => "랜덤박스";
        public override string Description => "20초마다 랜덤한 아이템을 얻을 수 있습니다!";

        public override string Detail =>
            """
            랜덤 아이템이 지급됩니다.

            이후, 20초마다 무작위 아이템들을 하나 더 받습니다.
            """;

        public override string Color => "BFFF00";

        public static RandomItem Instance;

        private CoroutineHandle _onModeStarted;

        public override void OnEnabled()
        {
            _isEnabled = true;
            Exiled.Events.Handlers.Player.Spawned += OnSpawned;

            _onModeStarted = Timing.RunCoroutine(OnModeStarted());
        }

        public override void OnDisabled()
        {
            _isEnabled = false;
            Exiled.Events.Handlers.Player.Spawned -= OnSpawned;

            Timing.KillCoroutines(_onModeStarted);
        }

        private IEnumerator<float> OnModeStarted()
        {
            foreach (var player in PlayerManager.List)
            {
                Timing.RunCoroutine(Spawned(player));
            }

            while (_isEnabled)
            {
                yield return Timing.WaitForSeconds(20f);

                foreach (var player in PlayerManager.List.Where(x => x.IsAlive && x.Role.Type != RoleTypeId.Scp079))
                    try
                    {
                        List<ItemType> itemList = Tools.EnumToList<ItemType>();
                        ItemType item = itemList.GetRandomValue();
                        player.AddItem(item);

                        player.AddHint("랜덤박스", $"<color=#F3F781>{item.GetName()}</color>(을)를 지급받았습니다.",
                            5);
                    }
                    catch (KeyNotFoundException e)
                    { Log.Warn($"[RGM] RandomItem card fetch failure: {e.Message}"); }
                    catch (Exception ex) { Log.Error($"[RGM] RandomItem Mode Error: {ex}"); }
            }
        }

        private void OnSpawned(Exiled.Events.EventArgs.Player.SpawnedEventArgs ev)
        {
            if (ev.Player.IsNonePlayer())
                return;

            if (_isEnabled)
                Timing.RunCoroutine(Spawned(ev.Player));
        }

        private IEnumerator<float> Spawned(Player player)
        {
            List<ItemType> itemList = [.. Tools.EnumToList<ItemType>().Where(x => !x.IsAmmo())];
        
            yield return Timing.WaitForOneFrame;
        
            if (!player.IsAlive || player.Role.Type == RoleTypeId.Scp079)
                yield break;
        
            yield return Timing.WaitForSeconds(0.1f);
            player.ClearInventory();

            // 탄약 아이템은 인벤토리 슬롯을 점유하므로, 예비 탄약으로 직접 지급합니다.
            // 아래 랜덤 아이템 6개가 슬롯 부족으로 누락되지 않도록 합니다.
            player.AddAmmo(AmmoType.Nato9, 90);
            player.AddAmmo(AmmoType.Nato556, 60);
            player.AddAmmo(AmmoType.Nato762, 60);
            player.AddAmmo(AmmoType.Ammo12Gauge, 16);
            player.AddAmmo(AmmoType.Ammo44Cal, 10);

            for (int i = 1; i < 7; i++)
            {
                var item = itemList.GetRandomValue();
                
                player.AddItem(item);

                yield return Timing.WaitForSeconds(1);
            }
        }
    }
}