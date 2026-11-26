using Exiled.API.Features;
using HintServiceMeow.Core.Enum;
using HintServiceMeow.Core.Extension;
using MEC;
using RGM.API.Features;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace RGM.Modes.Lock.TFT.Euclid.Scp;

[TFTAbility("본능", "가장 가까운 인간의 위치를 확인합니다.", TFTAbilityLevel.Euclid, TFTAbilityCategory.Scp, TFTAbilityPoint.Continuous,
    TFTAbilityType.FindLocation, "📥")]
public class FindLocation : TFTAbility
{
    private CoroutineHandle _findLocation;

    public override void OnEnabled()
    {
        _findLocation = Timing.RunCoroutine(findLocation());
    }

    public override void OnDisabled()
    {
        Timing.KillCoroutines(_findLocation);
    }

    private IEnumerator<float> findLocation()
    {
        var id = "FInd Location";
        var hint = new HintServiceMeow.Core.Models.Hints.Hint
        {
            Text = $""
        };

        while (Owner.IsScp && !Round.IsEnded)
            if (Owner.TryGetNearestPlayer(out var player, out var radius, Player.List.Where(x => x.IsScp).ToList()))
            {
                hint = new HintServiceMeow.Core.Models.Hints.Hint
                {
                    Text = $"<size=20>가장 가까운 인간: {(int)radius}m</size>",
                    Id = id,
                    XCoordinate = 0,
                    YCoordinate = 900,
                    Alignment = HintAlignment.Right
                };

                Owner.AddCustomHint(hint);

                yield return Timing.WaitForSeconds(1f);

                Owner.RemoveHint(hint);
            }
            else
            {
                yield return Timing.WaitForSeconds(1);
            }
    }
}
