using Exiled.API.Features;
using RGM.Variables;
using SecretAPI.Features.UserSettings;

namespace RGM.UserSettings;

public static partial class MainSettingManager
{
    private sealed partial class MuteBGMSetting() : CustomTwoButtonSetting(12053, "BGM 음소거ㅣBGM mute", "ON", "OFF",
        defaultIsB: true,
        hint:
        "음악이 유튜브 저작권에 걸릴 것 같다고요? 이 기능을 사용하세요.\n\nAre you worried BGM might be copyrighted by YouTube? Use this feature.") 
    {
        public override CustomHeader Header => Setting;
        protected override CustomSetting CreateDuplicate() => new MuteBGMSetting();

        protected override void HandleSettingUpdate()
        {
            if (KnownOwner == null)
                return;

            Player player = Player.Get(KnownOwner.ReferenceHub);

            if (IsOptionA)
            {
                if (!Variable.MuteBGMPlayers.Contains(player))
                    Variable.MuteBGMPlayers.Add(player);
            }
            else if (Variable.MuteBGMPlayers.Contains(player))
            {
                Variable.MuteBGMPlayers.Remove(player);
            }
        }
    }
}