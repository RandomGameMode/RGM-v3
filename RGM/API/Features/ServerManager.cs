using Exiled.API.Features;
using InventorySystem.Configs;
using InventorySystem.Items.Usables.Scp330;
using MEC;
using static RGM.IEnumerators.ServerIEnumerator;
using static RGM.Variables.Variable;

namespace RGM.API.Features
{
    public static class ServerManager
    {
        public static void Setup()
        {
            Server.ExecuteCommand("rnr");

            InventoryLimits.StandardCategoryLimits[ItemCategory.SpecialWeapon] = 8;
            InventoryLimits.StandardCategoryLimits[ItemCategory.SCPItem] = 8;
            InventoryLimits.Config.RefreshCategoryLimits();

            foreach (var data in CandyDataDict)
                Scp330Candies.DictionarizedCandies.Add(data.Key, data.Value);

            GlobalPlayer = AudioPlayer.CreateOrGet($"Global AudioPlayer",
                condition: (ReferenceHub hub) => !MuteBGMPlayers.Contains(Player.Get(hub)), onIntialCreation: (p) =>
            {
                Speaker speaker = p.AddSpeaker("Main", isSpatial: false, maxDistance: 5000);
            });

            Timing.RunCoroutine(InputCooldown());
            Timing.RunCoroutine(SyncSpectatedHint());
            Timing.RunCoroutine(RenewalPlayersInfo());
            Timing.RunCoroutine(HintManager.OnStarted());
            Timing.RunCoroutine(HintManager.RemoveHint());
            Timing.RunCoroutine(ChatManager.RunChat());
        }
    }
}
