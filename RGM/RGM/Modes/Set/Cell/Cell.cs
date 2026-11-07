using System.Collections.Generic;
using System.Linq;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.Events.EventArgs.Player;
using MEC;
using Mirror;
using PlayerRoles;
using RGM.API.Features;
using UnityEngine;

namespace RGM.Modes
{
    [Mode(ModeCategory.Public, ModeInfo.Set, ModeType.Cell)]
    internal class Cell : Mode
    {
        public override string Name => "고문";
        public override string Description => "고문을 최대한 오래 버티는 플레이어가 승리합니다.";

        public override string Detail =>
            """
            좁은 방에서 <color=#FE2E2E>SCP-018</color>이 던져집니다!
            60초마다 새로운 SCP-018이 스폰됩니다.

            최대한 잘 피해 보세요!
            """;

        public override string Color => "D7DF01";
        public override string Map => "cell";

        public static Cell Instance;

        private readonly List<Player> _pl = [];

        private CoroutineHandle _onModeStarted;

        public override void OnEnabled()
        {
            Round.IsLocked = true;
            Server.FriendlyFire = true;

            Exiled.Events.Handlers.Player.Died += OnDied;

            _onModeStarted = Timing.RunCoroutine(OnModeStarted());
        }

        public override void OnDisabled()
        {
            Round.IsLocked = false;
            Server.FriendlyFire = false;

            Exiled.Events.Handlers.Player.Died -= OnDied;

            Timing.KillCoroutines(_onModeStarted);
        }

        private IEnumerator<float> OnModeStarted()
        {
            yield return Timing.WaitForOneFrame;

            PlayerManager.List.ToList().CopyTo(_pl);

            foreach (var player in PlayerManager.List)
            {
                player.Role.Set(RoleTypeId.Tutorial);
                player.Position = GameObject.Find("[SP] Base").transform.position;
            }

            Timing.RunCoroutine(Timer());
            Timing.RunCoroutine(AddScp018());
        }

        private static IEnumerator<float> Timer()
        {
            for (var time = 0;  ; time++)
            {
                if (Round.IsEnded) yield break;
                
                PlayerManager.List.ToList()
                    .ForEach(x => x.AddBroadcast(1, $"<size=32>{time}</size>"));
                
                yield return Timing.WaitForSeconds(1);
            }
        }

        private static IEnumerator<float> AddScp018()
        {
            var thrower = PlayerManager.List.Where(x => x.IsAlive).ToList().GetRandomValue();

            var scp018 = (Throwable)thrower.AddItem(ItemType.SCP018);
            var scp = (Scp018)scp018;
            scp.ChangeItemOwner(thrower, null);
            thrower.ThrowItem(scp018);
            thrower.RemoveItem(scp018);

            PlayerManager.List.ToList()
                .ForEach(x => x.AddBroadcast(5, $"<size=40><color=red>새로운 SCP-018이 투하되었습니다.</color></size>"));
            
            yield return Timing.WaitForSeconds(Random.Range(30, 61));
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
