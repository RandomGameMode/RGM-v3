using System.Collections.Generic;
using System.Linq;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using MEC;
using Mirror;

using RGM.API.Features;
using UnityEngine;

namespace RGM.Modes
{
    [Mode(ModeCategory.Public, ModeInfo.Set, ModeType.Cell)]
    class Cell : Mode
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
            PlayerManager.List.ToList().CopyTo(_pl);

            foreach (var player in PlayerManager.List)
            {
                player.Role.Set(PlayerRoles.RoleTypeId.Tutorial);
                player.Position = GameObject.Find("[SP] Base").transform.position;
            }

            while (true)
            {
                var badLucky = PlayerManager.List.Where(x => x.IsAlive).ToList().GetRandomValue();

                Throwable scp018 = (Throwable)badLucky.AddItem(ItemType.SCP018);
                Scp018 scp = (Scp018)scp018;
                scp.ChangeItemOwner(badLucky, null);
                badLucky.ThrowItem(scp018);
                badLucky.RemoveItem(scp018);

                yield return Timing.WaitForSeconds(60);
            }
        }

        private void OnDied(Exiled.Events.EventArgs.Player.DiedEventArgs ev)
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
