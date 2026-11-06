using System;
using System.Collections.Generic;
using MEC;
using Exiled.API.Features;
using UnityEngine;
using InventorySystem.Items.Keycards.Snake;
using RGM.API.Features;

namespace RGM.Modes.SnakeSystem
{
    public static class SnakeGameMonitor
    {
        private static CoroutineHandle _monitorCoroutine;
        private static Dictionary<Player, PlayerSnakeData> _playerSnakeData = new Dictionary<Player, PlayerSnakeData>();
        // A SnakeDisplay represents one game session. It must never be used to
        // update the score of every player standing near it.
        private static Dictionary<SnakeDisplay, Player> _displayOwners = new Dictionary<SnakeDisplay, Player>();

        private class PlayerSnakeData
        {
            public int LastScore { get; set; }
            public SnakeDisplay LastDisplay { get; set; }
            public DateTime LastUpdate { get; set; }
            public bool IsPlayingSnake { get; set; }
        }

        public static void StartMonitoring()
        {
            if (_monitorCoroutine.IsRunning)
                Timing.KillCoroutines(_monitorCoroutine);

            _monitorCoroutine = Timing.RunCoroutine(MonitorSnakeGames());
        }

        public static void StopMonitoring()
        {
            if (_monitorCoroutine.IsRunning)
                Timing.KillCoroutines(_monitorCoroutine);

            _playerSnakeData.Clear();
            _displayOwners.Clear();
        }

        private static IEnumerator<float> MonitorSnakeGames()
        {
            while (true)
            {
                try
                {
                    CheckAllSnakeDisplays();
                    CleanupOldData();
                }
                catch (Exception ex)
                {
                    Log.Error($"Snake monitor error: {ex}");
                }

                yield return Timing.WaitForSeconds(Config.MonitoringInterval);
            }
        }

        private static void CheckAllSnakeDisplays()
        {
            SnakeDisplay[] snakeDisplays;

            try
            {
                snakeDisplays = UnityEngine.Object.FindObjectsByType<SnakeDisplay>(FindObjectsSortMode.None);
            }
            catch (System.Exception)
            {
#pragma warning disable CS0618 // 형식 또는 멤버는 사용되지 않습니다.
                snakeDisplays = UnityEngine.Object.FindObjectsOfType<SnakeDisplay>();
#pragma warning restore CS0618 // 형식 또는 멤버는 사용되지 않습니다.
            }

            // Önce tüm oyuncuları kontrol et
            foreach (var player in PlayerManager.List)
            {
                if (player == null || !player.IsAlive) continue;

                bool isHoldingSnakeCard = IsPlayerHoldingSnakeCard(player);

                if (isHoldingSnakeCard)
                {
                    var activeDisplay = FindActiveDisplayForPlayer(player, snakeDisplays);
                    if (activeDisplay != null && IsDisplayOwnedByPlayer(activeDisplay, player))
                    {
                        ProcessPlayerSnakeDisplay(player, activeDisplay);
                    }
                    else
                    {
                    }
                }
                else
                {
                    if (_playerSnakeData.ContainsKey(player))
                    {
                        _playerSnakeData[player].IsPlayingSnake = false;
                    }

                    ReleaseDisplaysOwnedBy(player);
                }
            }
        }

        private static bool IsPlayerHoldingSnakeCard(Player player)
        {
            try
            {
                var currentItem = player.CurrentItem;
                if (currentItem == null)
                {
                    return false;
                }

                // Snake is played only with the Chaos Insurgency keycard.
                // Treating every keycard as a Snake card made nearby players
                // observe and receive the same display's score.
                return currentItem.Type == ItemType.KeycardChaosInsurgency;
            }
            catch (Exception ex)
            {
                Log.Error($"Error checking snake card for <b><i>{player.Nickname}</i></b>: {ex}");
                return false;
            }
        }

        private static SnakeDisplay FindActiveDisplayForPlayer(Player player, SnakeDisplay[] displays)
        {
            SnakeDisplay closestDisplay = null;
            float closestDistance = float.MaxValue;

            foreach (var display in displays)
            {
                try
                {
                    if (display == null) continue;

                    float distance = Vector3.Distance(player.Position, display.transform.position);

                    if (distance < Config.PlayerDetectionRange && distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestDisplay = display;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Error checking display distance: {ex}");
                }
            }

            if (closestDisplay != null)
            {
            }

            return closestDisplay;
        }

        private static bool IsDisplayOwnedByPlayer(SnakeDisplay display, Player player)
        {
            if (_displayOwners.TryGetValue(display, out var owner))
            {
                if (owner != null && owner.IsConnected && owner.IsAlive && IsPlayerHoldingSnakeCard(owner))
                    return owner == player;

                _displayOwners.Remove(display);
            }

            _displayOwners[display] = player;
            return true;
        }

        private static void ReleaseDisplaysOwnedBy(Player player)
        {
            var displaysToRelease = new List<SnakeDisplay>();

            foreach (var entry in _displayOwners)
            {
                if (entry.Value == player)
                    displaysToRelease.Add(entry.Key);
            }

            foreach (var display in displaysToRelease)
                _displayOwners.Remove(display);
        }

        private static void ProcessPlayerSnakeDisplay(Player player, SnakeDisplay display)
        {
            try
            {
                if (display.ScoreText == null)
                {
                    return;
                }

                var scoreText = display.ScoreText.text;

                // Farklı skor formatlarını dene
                int currentScore = 0;
                if (!int.TryParse(scoreText, out currentScore))
                {
                    // "Score: 123" formatı
                    var cleanText = scoreText.Replace("Score: ", "").Replace("SKOR: ", "").Replace("Puan: ", "").Trim();
                    if (!int.TryParse(cleanText, out currentScore))
                    {
                        return;
                    }
                }

                if (!_playerSnakeData.ContainsKey(player))
                {
                    _playerSnakeData[player] = new PlayerSnakeData
                    {
                        LastScore = currentScore,
                        LastDisplay = display,
                        LastUpdate = DateTime.Now,
                        IsPlayingSnake = true
                    };

                    return;
                }

                var playerData = _playerSnakeData[player];

                if (playerData.LastDisplay != display)
                {
                    playerData.LastDisplay = display;
                    playerData.LastScore = currentScore;
                    playerData.LastUpdate = DateTime.Now;
                    playerData.IsPlayingSnake = true;
                    return;
                }

                // Skor değişikliği kontrolü
                if (currentScore != playerData.LastScore)
                {
                    if (currentScore == 0 && playerData.LastScore > 0)
                    {
                        if (playerData.IsPlayingSnake && (DateTime.Now - playerData.LastUpdate).TotalSeconds < 30)
                        {
                            SnakeEventManager.NotifyScoreChange(player, playerData.LastScore, true);
                        }
                        else
                        {
                            Log.Warn($"Player <b><i>{player.Nickname}</i></b> - Not awarding points (not playing or too old)");
                        }

                        playerData.IsPlayingSnake = false;
                    }
                    else if (currentScore > playerData.LastScore)
                    {
                        playerData.IsPlayingSnake = true;
                        SnakeEventManager.NotifyScoreChange(player, currentScore, false);
                    }

                    playerData.LastScore = currentScore;
                }

                playerData.LastUpdate = DateTime.Now;
            }
            catch (Exception ex)
            {
                Log.Error($"Error processing snake display for <b><i>{player.Nickname}</i></b>: {ex}");
            }
        }

        private static void CleanupOldData()
        {
            var playersToRemove = new List<Player>();

            foreach (var kvp in _playerSnakeData)
            {
                var player = kvp.Key;
                var data = kvp.Value;

                if (player == null || !player.IsConnected ||
                    (DateTime.Now - data.LastUpdate).TotalSeconds > Config.MaxPlayerDataAge)
                {
                    playersToRemove.Add(player);
                }
            }

            foreach (var player in playersToRemove)
            {
                _playerSnakeData.Remove(player);
                ReleaseDisplaysOwnedBy(player);
            }
        }

        public static void OnPlayerLeft(Player player)
        {
            if (_playerSnakeData.ContainsKey(player))
            {
                _playerSnakeData.Remove(player);
            }

            ReleaseDisplaysOwnedBy(player);
        }
    }
}