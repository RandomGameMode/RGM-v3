using Exiled.API.Features;
using RGM.Modes.Lock.TFT.Variables;
using UserSettings.ServerSpecific;
using static RGM.Variables.Variable;

namespace DAONTFT.Core.TFT
{
    public static class TFTInputSetting
    {
        public static void OnSSInput(ReferenceHub sender, ServerSpecificSettingBase setting)
        {
            if (setting is not SSKeybindSetting keybind || !keybind.SyncIsPressed)
                return;

            Player player = Player.Get(sender);

            if (CurrentMode != RGM.ModeType.TFT)
                return;

            if (!Base.IsSelecting.TryGetValue(player, out bool isSelecting) || !isSelecting)
                return;

            if (setting.SettingId == 12057)
            {
                TFTBattle.MoveSelectionCursor(player, -1);
                return;
            }

            if (setting.SettingId == 12058)
            {
                TFTBattle.MoveSelectionCursor(player, 1);
                return;
            }

            if (setting.SettingId == 12059)
            {
                TFTBattle.ConfirmSelectionByCursor(player, out _);
                return;
            }
        }
    }
}
