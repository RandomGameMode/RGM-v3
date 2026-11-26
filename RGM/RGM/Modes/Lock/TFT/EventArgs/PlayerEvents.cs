using RGM.Modes.Lock.TFT.IEnumerators;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using MEC;
using System.Collections.Generic;
using static RGM.Modes.Lock.TFT.Variables.Base;

namespace RGM.Modes.Lock.TFT.EventArgs
{
    public static class PlayerEvents
    {
        public static void OnVerified(VerifiedEventArgs ev)
        {
            Verified(ev.Player);
        }

        public static void Verified(Player player)
        {
            if (!PlayerTFTAbilities.ContainsKey(player))
            {
                PlayerTFTAbilities.Add(player, new List<TFTAbility>());
                IsSelecting.Add(player, false);
                IsLifeUsed.Add(player, false);
            }

            if (!PlayerHints.ContainsKey(player))
            {
                PlayerHints.Add(player, new());
            }

            Timing.RunCoroutine(Enumerator.UpgradeDisplay(player));
        }
    }
}
