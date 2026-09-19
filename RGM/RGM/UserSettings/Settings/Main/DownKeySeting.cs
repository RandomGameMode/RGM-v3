using SecretAPI.Features.UserSettings;
using UnityEngine;

namespace RGM.UserSettings;

public static partial class MainSettingManager
{
    private sealed partial class DownKeySetting() : CustomKeybindSetting(12056, "아래 이동키ㅣDown movement key",
        KeyCode.DownArrow)
    {
        public override CustomHeader Header => Setting;

        protected override CustomSetting CreateDuplicate() => new DownKeySetting();

        protected override void HandleSettingUpdate()
        {
        }
    }
}