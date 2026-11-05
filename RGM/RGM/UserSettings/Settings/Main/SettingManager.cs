using System;
using Exiled.API.Features;
using RGM.Variables;
using SecretAPI.Features.UserSettings;

namespace RGM.UserSettings;

public static partial class MainSettingManager
{
    [Obsolete("기능 정지로 인해 사용이 중지되었습니다.")]
    private sealed partial class TranslationSetting() : CustomDropdownSetting(12054, "번역ㅣTranslation",
        ["Korean (ko)", "English (en)"],
        defaultOptionIndex: Main.Instance.Config.EN ? 1 : 0,
        hint: "언어의 장벽을 부수려면 이 설정을 사용하세요.\n\nUse this setting to break the language barrier.") 
    {
        public override CustomHeader Header => Setting;
        protected override CustomSetting CreateDuplicate() => new TranslationSetting();

        protected override void HandleSettingUpdate()
        {
            if (KnownOwner == null)
                return;

            Player player = Player.Get(KnownOwner.ReferenceHub);

            Variable.TranslatorPlayers[player] = SelectedOption.Split('(')[1].Replace(")", "");
        }
    }
}