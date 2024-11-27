using Exiled.API.Features.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Exiled.API.Features;
using MEC;
using RGM.API.Features;
using PlayerRoles;
using Exiled.API.Enums;

namespace RGM.Modes
{
    [Mode(ModeCategory.Public, ModeInfo.Plus, ModeType.Outlaw)]
    public class Outlaw : Mode
    {
        public override string Name => "무법자";
        public override string Description => "모두가 총기 하나를 가지고 시작합니다.";
        public override string Detail =>
"""
남에게 지속적으로 데미지를 입힐 수 있는,
투사체가 아닌 아이템 중에서 랜덤으로 지급받습니다.
""";
        public override string Color => "9F81F7";

        public static Outlaw Instance;

        public override void OnEnabled()
        {
            Exiled.Events.Handlers.Player.Spawned += OnSpawned;

            Timing.RunCoroutine(OnModeStarted());
        }

        public IEnumerator<float> OnModeStarted()
        {
            yield return Timing.WaitForSeconds(1f);

            foreach (var player in Player.List)
            {
                Spawned(player);
            }

            yield break;
        }

        public void OnSpawned(Exiled.Events.EventArgs.Player.SpawnedEventArgs ev)
        {
            Spawned(ev.Player);
        }

        public void Spawned(Player player)
        {
            if (player.Role.Type != RoleTypeId.Scp079)
            {
                List<ItemType> FirearmList = new List<ItemType>()
                {
                    ItemType.GunCOM15,
                    ItemType.GunCOM18,
                    ItemType.GunCom45,
                    ItemType.GunFSP9,
                    ItemType.GunE11SR,
                    ItemType.GunFRMG0,
                    ItemType.GunAK,
                    ItemType.GunShotgun,
                    ItemType.GunRevolver,
                    ItemType.GunLogicer,
                    ItemType.GunA7,
                    ItemType.Jailbird,
                    ItemType.ParticleDisruptor,
                    ItemType.MicroHID
                };

                Item CurrentItem = player.AddItem(Tools.GetRandomValue(FirearmList));

                if (CurrentItem is Firearm firearm)
                {
                    if (firearm.AmmoType != AmmoType.None)
                    {
                        for (int i = 0; i < 3; i++)
                            player.AddAmmo(firearm.AmmoType, (ushort)firearm.MaxAmmo);
                    }
                }
            }
        }
    }
}
