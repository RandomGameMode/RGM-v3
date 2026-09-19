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

    public static void AddSettings<T>(this T setting, ushort groupId) where T : CustomSetting 
        => ModifySettings(groupId, data => data.ToList().Add(setting));

    public static T CreateSettings<T>(T setting) where T : CustomSetting
    {
        try
        {
            var instance = Activator.CreateInstance<T>();

            if (SettingVariables.Settings.Values.Any(x => x.Any(y => y.Id == instance.Id)))
                throw new Exception($"This settings already exists: {instance.Id}");
            Log.Info($"Created Settings instance: {instance.Label}");

            return instance;
        }
        catch (Exception e)
        {
            Log.Error($"Failure to create settings instance: {e}");
            throw e.InnerException!;
        }
    }

    private static void ModifySettings(ushort settingId, Action<IEnumerable<CustomSetting>> code)
    {
        if (!SettingVariables.Settings.TryGetValue(settingId, out var setting)) return;
        code.Invoke(setting);
    }
}