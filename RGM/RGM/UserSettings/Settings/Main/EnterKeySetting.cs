using SecretAPI.Features.UserSettings;
using UnityEngine;

namespace RGM.UserSettings;

public static partial class MainSettingManager
{
    private sealed partial class EnterKeySetting() : CustomKeybindSetting(12059, "확인 키ㅣEnter key", KeyCode.KeypadEnter) 
    {
        public override CustomHeader Header => Setting;
        protected override CustomSetting CreateDuplicate() => new EnterKeySetting();
        protected override void HandleSettingUpdate()
        {
        }
    }
}