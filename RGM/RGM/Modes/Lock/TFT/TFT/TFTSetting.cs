using Exiled.API.Features;
using Exiled.API.Features.Core.UserSettings;
using UserSettings.ServerSpecific;
using static RGM.Modes.Lock.TFT.Variables.Base;

namespace RGM.Modes.Lock.TFT
{
    public static class TFTSetting
    {
        public static HeaderSetting RankHeader { get; private set; } = new HeaderSetting(190023, "전략적 팀 전투");

        public static void Init()
        {
        }

        public static void OnSSInput(ReferenceHub sender, ServerSpecificSettingBase setting)
        {
            Player player = Player.Get(sender);

            TFTInputSetting.OnSSInput(sender, setting);

            if (setting is SSKeybindSetting keybind && keybind.SyncIsPressed)
            {
                if (keybind.SettingId == 12060)
                {
                    if (!PlayerShowTFTs.ContainsKey(player))
                        PlayerShowTFTs.Add(player, false);

                    PlayerShowTFTs[player] = !PlayerShowTFTs[player];
                }
            }
        }
    }
}
