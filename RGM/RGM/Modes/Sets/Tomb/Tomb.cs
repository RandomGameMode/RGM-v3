using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CustomRendering;
using Exiled.API.Features;
using MEC;
using Mirror;
using UnityEngine;
using Exiled.API.Features.Items;
using RGM.API.Features;
using MultiBroadcast.API;
using PlayerRoles;

namespace RGM.Modes
{
    [Mode(ModeCategory.Public, ModeInfo.Set, ModeType.Tomb)]
    class Tomb : Mode
    {
        public override string Name => "무덤";
        public override string Description => "살아남으려면 뭐라도 해야 합니다.";
        public override string Detail =>
"""
널리 펼쳐진 평지에는 <b>수많은 아이템</b>이 널려 있습니다.

<i>배틀그라운드와 흡사하죠.</b>
""";
        public override string Color => "000000";

        public static Tomb Instance;

        public List<Player> pl = new List<Player>();

        public Vector3 RandomPosition()
        {
            return new Vector3(UnityEngine.Random.Range(-27.92969f, 44.88281f), 1043f, UnityEngine.Random.Range(-75.78906f, -2.71875f));
        }

        public override void OnEnabled()
        {
            Server.FriendlyFire = true;
            Round.IsLocked = true;
            foreach (var spawn in Respawning.WaveManager.Waves) spawn.Destroy();

            Exiled.Events.Handlers.Player.Died += OnDied;

            Timing.RunCoroutine(OnModeStarted());
        }

        public IEnumerator<float> OnModeStarted()
        {
            Server.ExecuteCommand($"/mp load plane");

            Player.List.CopyTo(pl);

            List<ItemType> ItemTypes = Tools.EnumToList<ItemType>();

            for (int i = 1; i <= 1205; i++)
            {
                Item Item = Item.Create(Tools.GetRandomValue(ItemTypes.ToList()));

                Item.CreatePickup(RandomPosition());
            }

            foreach (var player in Player.List)
            {
                player.Role.Set(RoleTypeId.Tutorial);
                player.Position = RandomPosition();
            }

            yield return 0f;
        }

        public void OnDied(Exiled.Events.EventArgs.Player.DiedEventArgs ev)
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
    }
}
