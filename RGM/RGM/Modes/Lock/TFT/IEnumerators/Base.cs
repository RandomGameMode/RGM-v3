using Exiled.API.Features;
using Exiled.API.Features.Roles;
using HintServiceMeow.Core.Enum;
using HintServiceMeow.Core.Extension;
using MEC;
using System.Collections.Generic;
using static DAONTFT.Core.Variables.Base;
using Hint = HintServiceMeow.Core.Models.Hints.Hint;
using RGM.API.Features;

namespace DAONTFT.Core.IEnumerators
{
    public static class Enumerator
    {
        public static IEnumerator<float> UpgradeDisplay(Player Owner)
        {
            Hint hint = new Hint
            {
                Text = $"",
            };

            while (true)
            {
                // 라운드 종료 후에는 SetWinner 결과 화면이 가려지지 않도록 힌트 표시를 중단한다.
                if (Round.IsEnded)
                {
                    yield return Timing.WaitForOneFrame;
                    continue;
                }

                if (Owner.IsAlive && PlayerShowTFTs.ContainsKey(Owner) && PlayerShowTFTs[Owner])
                {
                    List<string> queue = new();

                    if (PlayerTFTAbilities.TryGetValue(Owner, out var abilities))
                    {
                        foreach (var ability in abilities)
                        {
                            if (ability?.Data == null)
                                continue;

                            queue.Add($"{ability.Data.GetFormattedName()}ㅣ{ability.Data.Description}");
                        }
                    }
                    else
                    {
                        queue.Add("증강 정보 없음");
                    }

                    hint = new Hint
                    {
                        Text = $"<size=15>{string.Join("\n", queue)}</size>",
                        Id = "증강 리스트",
                        XCoordinate = -1000,
                        YCoordinate = 80,
                        Alignment = HintAlignment.Right
                    };

                    Owner.AddCustomHint(hint);

                    yield return Timing.WaitForOneFrame;

                    Owner.RemoveHint(hint);
                }
                else if (Owner.Role is SpectatorRole spectator && spectator.SpectatedPlayer != null)
                {
                    List<string> queue = new();

                    if (PlayerTFTAbilities.TryGetValue(spectator.SpectatedPlayer, out var abilities))
                    {
                        foreach (var ability in abilities)
                        {
                            if (ability?.Data == null)
                                continue;

                            queue.Add($"{ability.Data.GetFormattedName()}ㅣ{ability.Data.Description}");
                        }
                    }
                    else
                    {
                        queue.Add("증강 정보 없음");
                    }

                    hint = new Hint
                    {
                        Text = $"<size=15>{string.Join("\n", queue)}</size>",
                        Id = "증강 리스트",
                        XCoordinate = -1000,
                        YCoordinate = 80,
                        Alignment = HintAlignment.Right
                    };

                    Owner.AddCustomHint(hint);

                    yield return Timing.WaitForOneFrame;

                    Owner.RemoveHint(hint);
                }
                else
                {
                    yield return Timing.WaitForOneFrame;
                }
            }
        }
    }
}
