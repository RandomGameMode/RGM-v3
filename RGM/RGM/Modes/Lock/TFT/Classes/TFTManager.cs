using DAONTFT.Core.TFT;
using Exiled.API.Features;
using HintServiceMeow.Core.Enum;
using RGM.API.Features;
using System.Collections.Generic;
using System.Linq;
using static RGM.Modes.Lock.TFT.Variables.Base;
using Hint = HintServiceMeow.Core.Models.Hints.Hint;

namespace DAONTFT.Core.Classes
{
    public class Upgrade
    {
        public float X { get; set; }
        public string Emoji { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public TFTAbilityType Type { get; set; }
        public TFTAbilityLevel Level { get; set; }
    }

    public static class TFTManager
    {
        public static List<Hint> GetUpgradeDisplay(Player player, List<Upgrade> upgrades, int x = 0, TFTAbilityType? selectedType = null)
        {
            List<Hint> hints = new();

            Hint get(string text, float x, float y)
            {
                Hint o = new Hint
                {
                    Text = text,
                    Id = "증강",
                    XCoordinate = x,
                    YCoordinate = y,
                    Alignment = HintAlignment.Center
                };

                return o;
            }

            Hint desc1 = get("<size=50>증강 선택</size>", 0, 150 + x);
            Hint desc2 = get("<size=20><i><color=#ffffffcc>좌/우 키로 선택을 이동하고 Enter 키로 확정하십시오.\n<size=15><color=#bcbcbccc><i>[ESC] -> [Settings] -> [Server-specific]</i></color></size></color></i></size>", 0, 900 + x);

            hints.AddRange(new[] { desc1, desc2 });

            foreach (var upgrade in upgrades)
            {
                bool isSelected = selectedType.HasValue && selectedType.Value == upgrade.Type;
                string boxColor = isSelected ? "#FFFFFF" : upgrade.Level.GetColor();

                Hint o = get($"<color={upgrade.Level.GetColor()}><size=170>○</size></color>", upgrade.X, 350 + x);
                Hint emoji = get($"<color={upgrade.Level.GetColor()}><size=100>{upgrade.Emoji}</size></color>", upgrade.X, 350 + x);
                Hint title = get($"<size=32><b>{Tools.InsertBreaks(upgrade.Title, 12)}</b></size>", upgrade.X, 500 + x);
                Hint description = get($"<size=20>{Tools.InsertBreaks(upgrade.Description, 20)}</size>", upgrade.X, 610 + x);
                Hint chance = get($"<size=15>리롤 가능한 횟수: {Selections[player].First(x => x.Key == upgrade.Type).Value}</size>", upgrade.X, 750 + x);
                Hint box = get($"""
<size=180><color={boxColor}>
┌─┐   


└─┘   
</color></size>
""", upgrade.X, 430 + x);
                hints.AddRange(new[] { o, emoji, title, description, chance, box });
            }

            return hints;
        }
    }
}
