using SecretAPI.Features.UserSettings;
using System.Collections.Generic;
using MEC;

namespace RGM.UserSettings
{
    public static partial class SettingManager
    {
        private static CoroutineHandle _reloader;

        public static partial void Init();
        
        public static partial void Init()
        {
            if (!Timing.IsRunning(_reloader))
                _reloader = Timing.RunCoroutine(Reloader());

            MainSettingManager.Init();
            Timing.CallDelayed(Timing.WaitForOneFrame * 5,() => CustomSetting.Register(SettingVariables.Settings[0]));
        }

        private static IEnumerator<float> Reloader()
        {
            while (true)
            {
                yield return Timing.WaitForSeconds(60f);
                
                CustomSetting.ResyncServer();
            }
        }
    }
}
