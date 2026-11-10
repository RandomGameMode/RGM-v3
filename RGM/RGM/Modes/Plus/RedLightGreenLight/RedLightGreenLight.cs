using System.Collections.Generic;
using System.Linq;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Roles;
using MEC;
using PlayerRoles;
using RGM.API.Features;
using RGM.Patches;
using UnityEngine;

using static RGM.Variables.Variable;

namespace RGM.Modes
{
    [Mode(ModeCategory.Public, ModeInfo.Plus, ModeType.RedLightGreenLight)]
    internal class RedLightGreenLight : Mode
    {
        public override string Name => "빨간 불, 초록 불";
        public override string Description => "빨간 불에는 움직이지 마세요.";

        public override string Detail =>
            """
            순발력이 좋아야 살아남습니다.

            엘레베이터와 같은 움직이는 물체를 주의하세요!

            아, 그리고 설마 죽을까봐 움직이지 않거나 조금씩만 이동하는 쫄보는 없겠죠?

            * 게임 시작 8분 뒤 <color=red>자동핵</color>이 작동됩니다.
            """;

        public override string Color => "F7D358";

        public static RedLightGreenLight Instance;

        private string _light = "Green";
        private readonly Dictionary<Player, Vector3> _playerPosition = new();
        private readonly Dictionary<Player, Quaternion> _playerRotation = new();

        private CoroutineHandle _onModeStarted;
        private CoroutineHandle _recordPlayerInfo;
        private CoroutineHandle _checkRedLight;
        private readonly AutoWarhead _autoWarhead = new(8, 1);

        private static Quaternion Rot(Player player)
        {
            if (player.Role is Scp079Role scp079Role)
                return scp079Role.Camera.Rotation;
            return player.CameraTransform.rotation;
        }

        public override void OnEnabled()
        {
            _onModeStarted = Timing.RunCoroutine(OnModeStarted());
            _recordPlayerInfo = Timing.RunCoroutine(RecordPlayerInfo());
            _checkRedLight = Timing.RunCoroutine(CheckRedLight());
            _autoWarhead.RunCoroutine();
        }

        public override void OnDisabled()
        {
            Timing.KillCoroutines(_onModeStarted);
            Timing.KillCoroutines(_recordPlayerInfo);
            Timing.KillCoroutines(_checkRedLight);
            _autoWarhead.KillCoroutine();
        }

        private IEnumerator<float> OnModeStarted()
        {
            PlayerManager.List.ToList().ForEach(x => x.AddHint("불", $"<color=green>초록 불</color>! 움직여도 됩니다.", 250));

            while (true)
            {
                yield return Timing.WaitForSeconds(Random.Range(1, 46));

                _light = "Red";
                PlayerManager.List.ToList().ForEach(x => x.AddHint("불", $"<color=red>빨간 불</color>! 움직이지 마세요!", 250));

                yield return Timing.WaitForSeconds(Random.Range(3, 11));

                _light = "Green";
                PlayerManager.List.ToList().ForEach(x => x.AddHint("불", $"<color=green>초록 불</color>! 움직여도 됩니다.", 250));
            }
        }

        private IEnumerator<float> RecordPlayerInfo()
        {
            while (true)
            {
                foreach (var player in PlayerManager.List)
                {
                    if (_playerPosition.ContainsKey(player))
                    {
                        _playerPosition[player] = player.Position;
                        _playerRotation[player] = Rot(player);
                    }
                    else
                    {
                        _playerPosition.Add(player, player.Position);
                        _playerRotation.Add(player, Rot(player));
                    }

                    if (player.Role.Type == RoleTypeId.Scp079 &&
                        PlayerManager.List.Count(x => x.IsScpRole()) < 2)
                    {
                        player.Role.Set(RoleTypeId.Tutorial);
                        player.Position = PlayerManager.List.Where(x => x.IsHuman).ToList().GetRandomValue().Position;
                        Timing.RunCoroutine(Tools.DoRocket(player, player));
                    }
                }

                yield return Timing.WaitForSeconds(0.5f);
            }
        }

        private IEnumerator<float> CheckRedLight()
        {
            while (true)
            {
                while (_light == "Green")
                    yield return Timing.WaitForSeconds(0.1f);

                yield return Timing.WaitForSeconds(0.53f); // 530ms

                while (_light == "Red")
                {
                    foreach (var player in PlayerManager.List)
                        if (_playerPosition.ContainsKey(player) && player.IsAlive)
                            if (_playerPosition[player] != player.Position || _playerRotation[player] != Rot(player))
                            {
                                if (GodModePlayers.Contains(player))
                                    GodModePlayers.Remove(player);

                                if (player.Role.Type == RoleTypeId.Scp079)
                                {
                                    player.Role.Set(RoleTypeId.Tutorial);
                                    player.Position = PlayerManager.List.Where(x => x.IsHuman).ToList().GetRandomValue()
                                        .Position;
                                    Timing.RunCoroutine(Tools.DoRocket(player, player, 1f));
                                }
                                else
                                {
                                    Timing.RunCoroutine(Tools.DoRocket(player, player, 0.01f));
                                }

                                Server.ExecuteCommand($"/clearcassie");
                                Tools.MessageTranslated("",
                                    $"안타깝게도 <b><i>{player.DisplayNickname}</i></b>(이)가 <b><color=red>빨간 불</color></b>에 건넜습니다.");
                            }

                    yield return Timing.WaitForSeconds(0.1f);
                }
            }
        }
    }
}
