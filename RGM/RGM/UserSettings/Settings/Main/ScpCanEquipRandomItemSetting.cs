using System.Linq;
using Exiled.API.Features;
using RGM.API.Features;
using SecretAPI.Extensions;
using SecretAPI.Features.UserSettings;
using UnityEngine;

namespace RGM.UserSettings;

public static partial class MainSettingManager
{
    private sealed partial class ScpCanEquipRandomItemSetting() : CustomKeybindSetting(12050,
        "SCP의 아이템 장착ㅣEquipping SCP items", KeyCode.H, allowSpectatorTrigger: false,
        hint: "SCP가 보유한 아이템 중 무작위로 하나를 장착합니다.\n\nEquip a random item from the SCP's inventory.") 
    {
        public override CustomHeader Header => Setting;
        protected override CustomSetting CreateDuplicate() => new ScpCanEquipRandomItemSetting();

        protected override void HandleSettingUpdate()
        {
            if (!IsPressed || KnownOwner == null)
                return;

            Player player = Player.Get(KnownOwner.ReferenceHub);
            if (!player.IsScpRole())
                return;

            var candidates = player.Items
                .Where(x => player.CurrentItem != x)
                .ToList();

            candidates.Add(null);

            if (candidates.Count == 0)
                return;

            player.CurrentItem = candidates.GetRandomValue();
        }
    }
}