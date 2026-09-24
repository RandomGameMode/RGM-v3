using Exiled.API.Features;
using MEC;
using System.Collections.Generic;
using System.Linq;
using static RGM.Variables.Variable;

namespace RGM.API.Features
{
    /**
     * <summary>힌트와 관련된 작업을 처리합니다</summary>
     */
    public static class HintManager
    {
        /// <summary>
        /// 해당 플레이어 객체의 힌트 정보를 기록합니다.
        /// </summary>
        private static readonly Dictionary<Player, Dictionary<string, (string, float)>> PlayerHints = new(); // Custom ID, (힌트, 남은 시간)
        
        /**
         * <summary>살아있는 플레이어에게 힌트를 띄웁니다</summary>
         * <returns>MEC 코루틴</returns>
         */
        public static IEnumerator<float> OnStarted()
        {
            while (!Round.IsEnded)
            {
                foreach (var player in Player.List.Where(x => x.IsAlive && PlayerHints.ContainsKey(x) && PlayerHints[x].Count > 0))
                {
                    string message = $"{string.Join("\n", PlayerHints[player].Values.Select(x => x.Item1))}";

                    message = message.Replace("<color=#855439>*</color>", "");

                    if (player.IsUsingTranslator())
                    {
                        TranslationManager.TranslatePreserveNewlines(message, TranslatorPlayers[player], 
                            translated => 
                            {
                                player.ShowHint(translated, 0.2f); 
                            });
                    }
                    else
                        player.ShowHint(message, 0.2f);
                }
                
                yield return Timing.WaitForSeconds(0.1f);
            }
        }
        
        /**
         * <summary>힌트가 띄워진 모든 플레이어에게서 힌트를 제거합니다</summary>
         */
        public static IEnumerator<float> RemoveHint()
        {
            while (true)
            {
                foreach (var player in Player.List.Where(x => x.IsAlive && PlayerHints.ContainsKey(x)))
                {
                    foreach (var hint in PlayerHints[player].ToList())
                    {
                        if (hint.Value.Item2 <= 0)
                        {
                            PlayerHints[player].Remove(hint.Key);
                        }
                        else
                        {
                            PlayerHints[player][hint.Key] = (hint.Value.Item1, hint.Value.Item2 - 0.1f);
                        }
                    }
                }

                yield return Timing.WaitForSeconds(0.1f);
            }
        }
        
        /**
         * <summary>Player의 확장 메서드로서 힌트를 띄우는 메서드를 추가합니다</summary>
         * <param name="player"> 플레이어 인스턴스</param>
         * <param name="customId">힌트 커스텀 ID</param>
         * <param name="hint">힌트 내용</param>
         * <param name="duration">힌트 길이</param>
         */
        public static void AddHint(this Player player, string customId, string hint, float duration = 3)
        {
            duration = (int)duration;

            if (!PlayerHints.ContainsKey(player))
                PlayerHints[player] = new Dictionary<string, (string, float)> { };

            if (PlayerHints[player].ContainsKey(customId))
            {
                PlayerHints[player].Remove(customId);
            }

            PlayerHints[player].Add(customId, (hint, duration));
        }
    }
}