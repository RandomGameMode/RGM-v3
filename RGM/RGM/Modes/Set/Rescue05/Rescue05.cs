using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using MEC;

using PlayerRoles;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.Events.EventArgs.Player;
using RGM.API.Features;
using RGM.Patches;
using UnityEngine;

namespace RGM.Modes
{
    [Mode(ModeCategory.Public, ModeInfo.Set, ModeType.Rescue05)]
    internal class Rescue05 : Mode
    {
        public override string Name => "05 평의회 구출 작전";
        public override string Description => "05 평의회를 구출하려는 자들과 사살하려는 자들의 싸움입니다.";

        public override string Detail =>
            """
            <color=#000000><b>05 평의회</b></color>가 탈출에 성공할 경우,
            <color=#2E9AFE>MTF</color> 진영이 승리합니다.

            <color=#000000><b>05 평의회</b></color>가 사살될 경우,
            <color=#088A08>혼돈의 반란</color> 진영이 승리합니다.

            * 게임 시작 8분 뒤 <color=red>자동핵</color>이 작동됩니다.
            """;

        public override string Color => "0040FF";

        public static Rescue05 Instance;

        private Player _level05;
        private Player _assassin;

        private CoroutineHandle _onModeStarted;
        private readonly AutoWarhead _autoWarhead = new(8, 1);

        public override void OnEnabled()
        {
            Exiled.Events.Handlers.Player.Escaping += OnEscaping;
            Exiled.Events.Handlers.Player.Dying += OnDying;
            Exiled.Events.Handlers.Player.Handcuffing += OnHandcuffing;

            _onModeStarted = Timing.RunCoroutine(OnModeStarted());
            _autoWarhead.RunCoroutine();
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Player.Escaping -= OnEscaping;
            Exiled.Events.Handlers.Player.Dying -= OnDying;
            Exiled.Events.Handlers.Player.Handcuffing -= OnHandcuffing;

            Timing.KillCoroutines(_onModeStarted);
            _autoWarhead.KillCoroutine();
        }

        private IEnumerator<float> OnModeStarted()
        {
            if (Random.Range(1, 101) <= 10) //10% 확률로 워크스테이션 업그레이드 시작
                Tools.TryInstallMode(ModeType.ABattle);
            _level05 = PlayerManager.List.ToList().GetRandomValue();
            _assassin = PlayerManager.List.Where(x => x != _level05).ToList().GetRandomValue();

            List<ItemType> level05Items =
            [
                ItemType.KeycardO5,
                ItemType.GrenadeFlash,
                ItemType.Flashlight,
                ItemType.GunCOM15,
                ItemType.Medkit,
                ItemType.Painkillers,
                ItemType.SCP500,
                ItemType.Radio
            ];

            _level05.Role.Set(RoleTypeId.Scientist);
            _level05.ClearInventory();
            foreach (var item in level05Items) _level05.AddItem(item);
            _level05.AddBroadcast(10,
                "<size=25>당신은 <color=#000000><b>05 평의회</b></color>, <color=#A4A4A4>시설 경비</color>들의 도움을 받아 시설에서 탈출하십시오.</size>");

            var assanssinItems = new List<ItemType>
            {
                ItemType.GunCOM15,
                ItemType.Ammo9x19
            };

            _assassin.Role.Set(RoleTypeId.ClassD);
            foreach (var item in assanssinItems) _assassin.AddItem(item);
            _assassin.AddBroadcast(10,
                "<size=25>당신은 <color=#FE9A2E><b>암살자</b></color>, <color=#04B404>혼돈의 반란</color>들과 함께 <color=#000000><b>05 평의회</b></color>를 사살하십시오.</size>");

            for (var i = 0; i < PlayerManager.List.Count / 1.5f; i++)
            {
                var player = PlayerManager.List.Where(x => x != _level05 && x != _assassin).ToList().GetRandomValue();
                player.Role.Set(RoleTypeId.ChaosRifleman);

                player.AddBroadcast(10,
                    "<size=25>당신은 <color=#04B404><b>혼돈의 반란</b></color>, <color=#FE9A2E><b>암살자</b></color>에 협조하여 <color=#000000><b>05 평의회</b></color>를 사살하십시오.</size>");
            }

            foreach (var player in PlayerManager.List.Where(x => x != _level05 && x != _assassin && !x.IsCHI))
            {
                player.Role.Set(RoleTypeId.FacilityGuard);
                player.Role.Set(RoleTypeId.NtfPrivate, RoleSpawnFlags.AssignInventory);

                player.AddBroadcast(10,
                    "<size=25>당신은 <color=#A4A4A4>시설 경비</color>, <color=#000000><b>05 평의회</b></color>를 안전하게 탈출시키십시오.</size>");
            }

            yield break;
        }

        private void OnEscaping(EscapingEventArgs ev)
        {
            if (ev.Player != _level05 || ev.Player.Role.Type != RoleTypeId.Scientist) return;
            Round.IsLocked = false;
            Timing.RunCoroutine(Tools.SetWinner(
                [.. PlayerManager.List.Where(x => x == _level05 || x.IsNTF)], 2));

            foreach (var player in Player.List)
            {
                player.AddBroadcast(20,
                    $"<size=30><b><color=#000000>05 평의회</color>({_level05.DisplayNickname})</b>가 탈출하여 <u>강화제 제작 방법을 재단에 넘기는 데 성공하였습니다.</u></size>");

                if (player.IsCHI || player.Role.Type == RoleTypeId.ClassD)
                    player.Role.Set(RoleTypeId.Tutorial, RoleSpawnFlags.None);
            }
        }

        private void OnDying(DyingEventArgs ev)
        {
            if (ev.Player != _level05 || ev.Player.Role.Type != RoleTypeId.Scientist) return;
            Round.IsLocked = false;
            Timing.RunCoroutine(Tools.SetWinner(
                [.. PlayerManager.List.Where(x => x == _assassin || x.IsCHI)], 2));

            foreach (var player in PlayerManager.List)
            {
                player.AddBroadcast(20,
                    $"<size=30><b><color=#000000>05 평의회</color>({_level05.DisplayNickname})</b>가 <color=red>사살당하였습니다.</color></size>");

                if (player.IsNTF)
                    player.Role.Set(RoleTypeId.Tutorial, RoleSpawnFlags.None);
            }
        }

        private void OnHandcuffing(HandcuffingEventArgs ev)
        {
            if (ev.Target == _level05 && ev.Target.Role.Type == RoleTypeId.Scientist)
                ev.IsAllowed = false;
        }
    }
}
