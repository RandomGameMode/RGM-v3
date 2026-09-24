using Exiled.API.Features;
using UserSettings.ServerSpecific;
using static RGM.Variables.Variable;

namespace RGM.Modes.Lock.ABattle
{
    public static class ABattleSetting
    {
        public static void OnSSInput(ReferenceHub sender, ServerSpecificSettingBase setting)
        {
            if (setting is not SSKeybindSetting { SyncIsPressed: true })
                return;

            Player player = Player.Get(sender);

            if (CurrentMode == ModeType.ABattle && setting.SettingId == 12060 && Modes.ABattle.Instance != null)
            {
                Modes.ABattle.ExtraModeNotion(player, false);
                return;
            }

            if (CurrentMode == ModeType.ABattle && Modes.ABattle.Instance != null && Modes.ABattle.Instance.IsSelecting.TryGetValue(player, out bool isSelecting) && isSelecting)
            {
                switch (setting.SettingId)
                {
                    case 12055:
                        Modes.ABattle.Instance.MoveSelectionCursor(player, -1);
                        return;
                    case 12056:
                        Modes.ABattle.Instance.MoveSelectionCursor(player, 1);
                        return;
                    case 12059:
                        Modes.ABattle.Instance.ConfirmSelectionByCursor(player, out _);
                        return;
                }
            }
        }
    }
}
