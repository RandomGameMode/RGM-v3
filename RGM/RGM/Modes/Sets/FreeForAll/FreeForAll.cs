using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using CustomRendering;
using Exiled.API.Features;
using Exiled.API.Features.Doors;
using HarmonyLib;
using MEC;
using Mirror;
using MultiBroadcast;
using MultiBroadcast.API;
using RGM.API.Features;
using UnityEngine;

namespace RGM.Modes
{
    [Mode(ModeCategory.Public, ModeInfo.Set, ModeType.FreeForAll)]
    class FreeForAll : Mode
    {
        public override string Name => "개인전";
        public override string Description => "최후의 1인이 되세요!";
        public override string Detail =>
"""
랜덤한 문으로 순간이동한 후 랜덤하게 지급되는 아이템으로 싸움을 시작합니다.

<i>모든 문은 잠겨 있습니다.</i>
""";
        public override string Color => "FA58F4";

        public static FreeForAll Instance;

        public List<Player> pl = new List<Player>();
        public List<ItemType> StartupItems = new List<ItemType>();
        public Door door;

        public override void OnEnabled()
        {
            Server.FriendlyFire = true;
            Round.IsLocked = true;
            foreach (var spawn in Respawning.WaveManager.Waves) spawn.Destroy();
            Door.List.ToList().ForEach(x => x.Lock(1205, Exiled.API.Enums.DoorLockType.Lockdown079));

            Exiled.Events.Handlers.Player.Dying += OnDying;
            Exiled.Events.Handlers.Player.Spawned += OnSpawned;

            Timing.RunCoroutine(OnModeStarted());
            Timing.RunCoroutine(CleanAll());
        }

        public List<ItemType> Items()
        {
            List<ItemType> Guns = new List<ItemType>() { ItemType.GunA7, ItemType.GunE11SR, ItemType.GunShotgun, ItemType.GunCom45, ItemType.GunFSP9, ItemType.GunRevolver,
                ItemType.GunCOM18, ItemType.GunCrossvec, ItemType.GunLogicer, ItemType.GunFRMG0, ItemType.GunAK, ItemType.Jailbird, ItemType.ParticleDisruptor };
            List<ItemType> Ammos = new List<ItemType>() { ItemType.Ammo12gauge, ItemType.Ammo44cal, ItemType.Ammo556x45, ItemType.Ammo762x39, ItemType.Ammo9x19 };
            List<ItemType> CDItems = new List<ItemType>() { ItemType.Medkit, ItemType.Painkillers, ItemType.Radio, ItemType.GrenadeFlash };
            List<ItemType> Items = new List<ItemType>();

            Items.Add(Tools.GetRandomValue(Guns));

            foreach (var ammo in Ammos)
            {
                for (int i = 0; i < 20; i++)
                    Items.Add(ammo);
            }

            foreach (var item in CDItems)
            {
                if (UnityEngine.Random.Range(1, 3) == 1)
                    Items.Add(item);
            }

            return Items;
        }

        public IEnumerator<float> OnModeStarted()
        {
            door = Tools.GetRandomValue(Door.List.ToList());
            StartupItems = Items();

            Player.List.ToList().CopyTo(pl);
            Player.List.ToList().ForEach(Spawned);

            yield return Timing.WaitForSeconds(180f);

            Player BusterCall = Tools.GetRandomValue(Player.List.Where(x => x.IsAlive).ToList());

            foreach (var player in Player.List)
            {
                player.Position = BusterCall.Position;
                player.Broadcast(5, "<b><size=30>[<color=yellow>버스터콜</color>]</size></b>\n<size=20>모두가 한자리에 모입니다.</size>");
            }
        }

        public IEnumerator<float> CleanAll()
        {
            while (true)
            {
                Map.CleanAllItems();
                Map.CleanAllRagdolls();

                yield return Timing.WaitForSeconds(1f);
            }
        }

        public void OnDying(Exiled.Events.EventArgs.Player.DyingEventArgs ev)
        {
            if (pl.Contains(ev.Player))
            {
                pl.Remove(ev.Player);

                if (pl.Count < 2)
                {
                    Round.IsLocked = false;

                    Player.List.ToList().ForEach(x => x.AddBroadcast(20, $"승리자 : {pl[0].Nickname}"));
                }
            }
        }

        public void OnSpawned(Exiled.Events.EventArgs.Player.SpawnedEventArgs ev)
        {
            Spawned(ev.Player);
        }

        public void Spawned(Player player)
        {
            Player.List.ToList().ForEach(x => x.DisableEffect(Exiled.API.Enums.EffectType.FogControl));
            Timing.CallDelayed(0.1f, () => Player.List.ToList().ForEach(x => x.EnableEffect(Exiled.API.Enums.EffectType.FogControl)));

            if (player.Role.Type != PlayerRoles.RoleTypeId.NtfSpecialist && pl.Contains(player))
            {
                player.Role.Set(PlayerRoles.RoleTypeId.NtfSpecialist);
                player.Position = new Vector3(door.Position.x, door.Position.y + 2, door.Position.z);

                player.ClearInventory();

                foreach (var item in StartupItems)
                    player.AddItem(item);
            }
        }
    }
}
