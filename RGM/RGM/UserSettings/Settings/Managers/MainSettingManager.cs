using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using SecretAPI.Features.UserSettings;

namespace RGM.UserSettings;

public static partial class MainSettingManager
{
    private static CustomHeader Setting { get; } = new("<b>랜덤게임모드</b>");
    private static CustomKeybindSetting ScpCanEquipRandomItem { get; set; }
    private static CustomTwoButtonSetting MuteBGM { get; set; }
    private static CustomDropdownSetting Translation { get; set; }
    private static CustomKeybindSetting UpKey { get; set; }
    private static CustomKeybindSetting DownKey { get; set; }
    private static CustomKeybindSetting LeftKey { get; set; }
    private static CustomKeybindSetting RightKey { get; set; }
    private static CustomKeybindSetting EnterKey { get; set; }
    private static CustomKeybindSetting DetailInfoKey { get; set; }  
    
    public static void Init()
    {
        if (ScpCanEquipRandomItem != null)
            return;

        ScpCanEquipRandomItem = new ScpCanEquipRandomItemSetting();
        MuteBGM = new MuteBGMSetting();
        Translation = new TranslationSetting();
        UpKey = new UpKeySetting();
        DownKey = new DownKeySetting();
        LeftKey = new LeftKeySetting();
        RightKey = new RightKeySetting();
        EnterKey = new EnterKeySetting();
        DetailInfoKey = new DetailInfoKeySetting();
        
        SettingVariables.Settings.Add(0, 
        [
            ScpCanEquipRandomItem,
            MuteBGM,
            Translation,
            UpKey,
            DownKey,
            LeftKey,
            RightKey,
            EnterKey,
            DetailInfoKey
        ]);
    }


    private sealed partial class MuteBGMSetting;

    private sealed partial class MuteBGMSetting;

    private sealed partial class ScpCanEquipRandomItemSetting;
        
    private sealed partial class TranslationSetting;

    private sealed partial class UpKeySetting;

    private sealed partial class DownKeySetting;

    private sealed partial class LeftKeySetting;

    private sealed partial class RightKeySetting;

    private sealed partial class EnterKeySetting : CustomKeybindSetting;

    private sealed partial class DetailInfoKeySetting;
}