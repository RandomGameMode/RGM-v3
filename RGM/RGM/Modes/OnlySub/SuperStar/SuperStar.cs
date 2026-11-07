using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using MEC;
using RGM.API.Features;
using static RGM.Variables.Variable;

namespace RGM.Modes
{
    [Mode(ModeCategory.OnlySub, ModeInfo.Plus, ModeType.SuperStar)]
    internal class SuperStar : Mode
    {
        public override string Name => "슈퍼 스타";
        public override string Description => "모두의 마이크가 공유됩니다.";

        public override string Detail =>
            "말 그대로입니다. 마이크가 공유됩니다.";

        public override string Color => "FE2EF7";

        public static SuperStar Instance;

        private CoroutineHandle _onModeStarted;

        public override void OnEnabled()
        {
            _onModeStarted = Timing.RunCoroutine(OnModeStarted());
        }

        public override void OnDisabled()
        {
            Timing.KillCoroutines(_onModeStarted);
            IntercomPlayers.Clear();
        }

        private IEnumerator<float> OnModeStarted()
        {
            byte count = 0;
            while (true)
            {
                if (!PlayerManager.List.Exists(x =>
                        x != null &&
                        !x.IsNPC &&
                        !x.IsHost)) yield return Timing.WaitForSeconds(1f);

                if (!IntercomPlayers.Exists(player => PlayerManager.List.Contains(player)))
                {
                    Server.ExecuteCommand($"/speak {string.Join(".", PlayerManager.List.Select(x => x.Id))}. 1");
                    foreach (var player in PlayerManager.List.Where(player => !IntercomPlayers.Contains(player)))
                        IntercomPlayers.Add(player);
                }

                if (count >= 50)
                {
                    IntercomPlayers.Clear();
                    count = 0;

                    yield return Timing.WaitForSeconds(.2f);
                }

                count += 1;
                yield return Timing.WaitForSeconds(1f);
            }
        }
    }
}
