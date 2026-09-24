using Exiled.API.Features;
using MEC;
using RGM.API.Features;
using RGM.Modes;
using System.Collections.Generic;
using static RGM.Variables.Variable;

namespace RGM.Patches;

public sealed class AutoWarhead(int delayMinutes = 20, int warningMinutes = 0)
{
    private CoroutineHandle _coroutine;
    private bool _isRunning;

    public int DelayMinutes { get; } = delayMinutes;

    public int WarningMinutes { get; } = warningMinutes;

    private float DelaySeconds => DelayMinutes * 60f;

    public CoroutineHandle RunCoroutine()
    {
        KillCoroutine();

        _coroutine = Timing.RunCoroutine(Run());
        _isRunning = true;
        return _coroutine;
    }

    public void KillCoroutine()
    {
        if (!_isRunning)
            return;

        Timing.KillCoroutines(_coroutine);
        _isRunning = false;
    }

    private IEnumerator<float> Run()
    {
        float delaySeconds = DelaySeconds;
        if (CurrentMode == ModeType.EchoBattle)
            delaySeconds += EchoBattle.RoundStartDelaySeconds;

        if (WarningMinutes > 0 && WarningMinutes < DelayMinutes)
        {
            yield return Timing.WaitForSeconds(delaySeconds - WarningMinutes * 60f);

            if (Warhead.IsDetonated)
                yield break;

            Tools.MessageTranslated("", $"{WarningMinutes}분 뒤 <color=red>자동핵</color>이 작동됩니다.");
            yield return Timing.WaitForSeconds(WarningMinutes * 60f);
        }
        else
        {
            yield return Timing.WaitForSeconds(delaySeconds);
        }

        if (!Warhead.IsDetonated && CurrentMode.GetModeData().Type != ModeType.Develop)
        {
            DeadmanSwitch.StartWarhead();
            Tools.MessageTranslated("", "<color=red>예정된 시설 자폭 프로세스가 시작되었습니다.</color> <b>대피하십시오.</b>");
        }

        _isRunning = false;
    }
}