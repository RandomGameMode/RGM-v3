using System;
using System.Collections.Generic;
using System.Linq;
using RGM.Modes.Lock.TFT.Classes;
using RGM.Modes.Lock.TFT;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using HintServiceMeow.Core.Extension;
using MEC;
using PlayerRoles;
using RGM.API.Features;
using static RGM.Modes.Lock.TFT.Variables.Base;
using static RGM.Variables.Variable;
using Hint = HintServiceMeow.Core.Models.Hints.Hint;
using Random = UnityEngine.Random;

namespace RGM.Modes.Lock.TFT;

public static class TFTBattle
{
    private static readonly Dictionary<Player, int> SelectionCursor = new();

    // 플레이어의 AudioPlayer가 없거나 라운드 재시작 등으로 파괴된 경우(Unity fake-null)
    // 안전하게 재생을 건너뛴다. (확장 메서드 TryPlay 내부에서 발생하던 NullReferenceException 방지)
    private static void PlayAudio(Player player, string clipName, float volume = 1f)
    {
        if (player != null && PlayersAudio.TryGetValue(player, out var audioPlayer) && audioPlayer != null)
            audioPlayer.TryPlay(clipName, volume);
    }

    private static void ClearSelectionPresentation(Player player, bool playCloseAudio = false)
    {
        if (PlayerHints.TryGetValue(player, out var hints))
            foreach (var hint in hints.Where(x => x.Id == "증강").ToList())
            {
                player.RemoveHint(hint);
                hints.Remove(hint);
            }

        if (PlayersAudio.TryGetValue(player, out var audio) && audio != null)
            foreach (var clip in audio.ClipsById.Values)
                if (clip.Clip.Contains("증강"))
                    clip.IsPaused = true;

        player.DisableEffect(EffectType.Blinded);
        player.DisableEffect(EffectType.SoundtrackMute);

        if (playCloseAudio)
            PlayAudio(player, "증강 선택창 닫힘", 3);
    }

    private static readonly Dictionary<string, string> RatingColor = new()
    {
        { "일반", "#A4A4A4" },
        { "희귀", "#2ECCFA" },
        { "영웅", "#FF00FF" },
        { "전설", "#ffd700" },
        { "신화", "#DF0101" },
        { "전용", "#F7819F" },
        { "시너지", "#DEEFED" }
    };

    private static string ColorFormat(string text)
    {
        return text.Replace("[시너지]", $"<color={RatingColor["시너지"]}>[시너지]</color>")
            .Replace("[전용]", $"<color={RatingColor["전용"]}>[전용]</color>")
            .Replace("[신화]", $"<color={RatingColor["신화"]}>[신화]</color>")
            .Replace("[전설]", $"<color={RatingColor["전설"]}>[전설]</color>")
            .Replace("[영웅]", $"<color={RatingColor["영웅"]}>[영웅]</color>")
            .Replace("[희귀]", $"<color={RatingColor["희귀"]}>[희귀]</color>")
            .Replace("[일반]", $"<color={RatingColor["일반"]}>[일반]</color>");
    }

    public static TFTAbilityLevel GetRandomAbilityLevel()
    {
        var random = Random.Range(1, 101);

        switch (random)
        {
            case <= 15:
                return TFTAbilityLevel.Keter;
            case <= 50:
                return TFTAbilityLevel.Euclid;
            default:
                return TFTAbilityLevel.Safe;
        }
    }

    // 라운드 종료 시 모든 플레이어의 증강 선택창 힌트/효과를 정리한다.
    public static void ClearAllHints()
    {
        foreach (var player in Selections.Keys.ToList())
        {
            try
            {
                Selections.Remove(player);
                SelectionCursor.Remove(player);
                IsSelecting[player] = false;
                ClearSelectionPresentation(player);
            }
            catch (Exception e)
            {
                Log.Error($"[TFT] 힌트 정리 중 예외\n{e}");
            }
        }

        foreach (var pair in PlayerHints.ToList())
        {
            try
            {
                foreach (var hint in pair.Value.ToList())
                    pair.Key.RemoveHint(hint);

                pair.Value.Clear();
            }
            catch (Exception e)
            {
                Log.Error($"[TFT] 힌트 정리 중 예외\n{e}");
            }
        }
    }

    public static bool StartUpgrade(List<Player> players = null, TFTAbilityLevel? levelOverride = null)
    {
        if (Round.IsEnded)
            return false;

        var level = levelOverride ?? GetRandomAbilityLevel();
        var started = false;

        foreach (var player in players ?? PlayerManager.List.Where(x =>
                     x.IsAlive && x.GetAbilities().Count() < (Encounter == RoleTypeId.Scp0492 ? 4 : 3)))
        {
            var playerLevel = level;

            switch (Encounter)
            {
                case RoleTypeId.NtfCaptain:
                    playerLevel = TFTAbilityLevel.Keter;
                    break;
                case RoleTypeId.NtfSergeant:
                    playerLevel = TFTAbilityLevel.Euclid;
                    break;
                case RoleTypeId.NtfSpecialist when player.GetAbilities().Count == 0:
                case RoleTypeId.NtfPrivate when player.GetAbilities().Count == 2:
                    playerLevel = TFTAbilityLevel.Keter;
                    break;
            }

            var tftAbilityDatas = player.GetValidAbilities(playerLevel);
            Dictionary<TFTAbilityType, int> abilities = new();

            foreach (var tftAbility in tftAbilityDatas) abilities.Add(tftAbility.Key.TFTAbilityType, tftAbility.Value);

            if (abilities.Count == 0)
                continue;

            player.StartSelect(abilities, Math.Min(2, abilities.Count));
            started = true;
        }

        return started;
    }

    // 플레이어에게 특정 능력을 부여
    public static void AddTftAbility(this Player player, TFTAbilityType type)
    {
        Log.Info("AddTFTAbility called with " + player.DisplayNickname + " and " + type);

        if (!TFTAbilities.ContainsKey(type))
        {
            Log.Error($"TFTAbility {type} not found.");

            return;
        }

        if (!PlayerTFTAbilities.ContainsKey(player))
        {
            Log.Info("No key");
            PlayerTFTAbilities.Add(player, []);
        }

        var tftAbilityData = TFTAbilities[type];
        TFTAbility tftAbility;

        try
        {
            tftAbility = Activator.CreateInstance(TFTAbilities[type].Type) as TFTAbility;
        }
        catch (Exception e)
        {
            Log.Error($"An error occurred while trying to create an instance of {tftAbilityData.Name}: {e}");
            return;
        }

        if (tftAbility == null)
        {
            Log.Error(
                $"An error occurred while trying to create an instance of {tftAbilityData.Name}. The instance is null.");
            return;
        }

        tftAbility.Data = tftAbilityData;
        tftAbility.Owner = player;
        tftAbility.OnEnabled();

        PlayerTFTAbilities[player].Add(tftAbility);

        var styleName = ColorFormat(tftAbilityData.GetFormattedName());

        var message = $"<size=20>{styleName}</size>\n<size=15>{tftAbilityData.Description}</size>";
        //player.AddBroadcast(10, Message);
        player.SendConsoleMessage($"\n{message}", "white");
    }

    /*// 플레이어로부터 특정 능력 제거
    public static void RemoveTFTAbility(this Player player, TFTAbilityType type)
    {
        if (!PlayerTFTAbilities.TryGetValue(player, out var playerTFTAbility))
            return;

        var TFTAbility = playerTFTAbility.FirstOrDefault(x => x.Data.TFTAbilityType == type);

        if (TFTAbility == null)
            return;

        TFTAbility.OnDisabled();
        PlayerTFTAbilities[player].Remove(TFTAbility);
    }

    public static void RemoveTFTAbility(this Player player, TFTAbility TFTAbility)
    {
        if (TFTAbility == null)
            return;

        if (!PlayerTFTAbilities.TryGetValue(player, out var playerTFTAbility))
            return;

        if (!playerTFTAbility.Contains(TFTAbility))
            return;

        TFTAbility.OnDisabled();
        PlayerTFTAbilities[player].Remove(TFTAbility);
    }*/

    // 플레이어로부터 모든 능력 제거
    private static void RemoveAllAbilities(this Player player)
    {
        if (!PlayerTFTAbilities.TryGetValue(player, out var playerTftAbility))
            return;

        foreach (var tftAbility in playerTftAbility)
            tftAbility.OnDisabled();

        PlayerTFTAbilities.Remove(player);
    }

    // 플레이어의 모든 능력 가져오기
    public static List<TFTAbility> GetAbilities(this Player player)
    {
        return PlayerTFTAbilities.TryGetValue(player, out var playerTftAbility)
            ? playerTftAbility
            : new List<TFTAbility>();
    }

    public static TFTAbilityType FindTftAbility(string name)
    {
        return TFTAbilities.FirstOrDefault(x => x.Value.Name == name).Key;
    }

    // 플레이어의 특정 능력 가져오기
    private static TFTAbility GetTftAbility(this Player player, TFTAbilityType type)
    {
        return player.GetAbilities().FirstOrDefault(x => x.Data.TFTAbilityType == type);
    }

    // 플레이어가 특정 능력을 가지고 있는지 확인
    public static bool HasTftAbility(this Player player, TFTAbilityType type)
    {
        return player.GetTftAbility(type) != null;
    }

    /*public static List<TFTAbilityType> GetRandomAbilities(TFTAbilityLevel level, int count)
    {
        var abilities = TFTAbilities.Where(x => x.Value.Level == level).ToList();

        abilities.ShuffleList();

        var result = new List<TFTAbilityType>();
        for (var i = 0; i < count; i++)
        {
            var picked = abilities.GetRandomValue().Key;
            result.Add(picked);
        }

        return result;
    }*/

    private static void StartSelect(this Player player, Dictionary<TFTAbilityType, int> abilities, int count = 2)
    {
        if (!Selections.ContainsKey(player))
            Selections.Add(player, new Dictionary<TFTAbilityType, int>());

        if (abilities.Count == 0)
        {
            IsSelecting[player] = false;
            return;
        }

        IsSelecting[player] = true;

        Dictionary<TFTAbilityType, int> queue = new();
        var selectionCount = Math.Min(count, abilities.Count);
        var candidates = abilities.Keys.ToList();
        candidates.ShuffleList();

        foreach (var tftAbility in candidates.Take(selectionCount))
            queue.Add(tftAbility, Encounter == RoleTypeId.Tutorial ? 4 : 2);

        Selections[player] = queue;
        SelectionCursor[player] = 0;

        // 다음 타자, 코루틴!!!
        Timing.RunCoroutine(player.SelectionCoroutine());
    }

    private static IEnumerator<float> SelectionCoroutine(this Player player)
    {
        var level = TFTAbilityLevel.Safe;

        PlayAudio(player, "증강 선택창", 3);

        player.EnableEffect(EffectType.Blinded, 30);
        player.EnableEffect(EffectType.SoundtrackMute);

        List<TFTAbilityType> getAbilities()
        {
            return Selections.TryGetValue(player, out var selection)
                ? [.. selection.Keys]
                : [];
        }

        List<Upgrade> upgrades = [];

        foreach (var tftAbility in getAbilities())
        {
            TftAbilityAdd(tftAbility);

            var data = tftAbility.GetData();
            if (data != null)
                level = data.Level;
        }

        PlayAudio(player, $"{level.ToString()} 증강", 0.7f);

        var hintDisplay = Timing.RunCoroutine(HintDisplay());

        for (var i = 0; i < 120; i++) // 증강 선택창 유지 시간 (30초)
        {
            if (player.IsDead || !Selections.ContainsKey(player) || Round.IsEnded)
            {
                Selections.Remove(player);

                SelectionCursor.Remove(player);
                IsSelecting[player] = false;

                Timing.KillCoroutines(hintDisplay);
                ClearSelectionPresentation(player);

                yield break;
            }

            yield return Timing.WaitForSeconds(0.25f);
        }

        IsSelecting[player] = false;
        Timing.KillCoroutines(hintDisplay);
        RemoveHints();
        player.DisableEffect(EffectType.Blinded);
        player.DisableEffect(EffectType.SoundtrackMute);
        PlayAudio(player, "증강 선택창 닫힘", 3);

        if (!Selections.ContainsKey(player))
            yield break;

        var random = Random.Range(0, getAbilities().Count);

        player.AddTftAbility(getAbilities().ElementAt(random));

        Selections.Remove(player);
        SelectionCursor.Remove(player);
        yield break;

        IEnumerator<float> HintDisplay()
        {
            while (true)
            {
                ResetupgradeDisplay();

                yield return Timing.WaitForSeconds(0.1f);
            }

            void ResetupgradeDisplay()
            {
                try
                {
                    upgrades.Clear();

                    foreach (var tftAbility in getAbilities()) TftAbilityAdd(tftAbility);

                    RemoveHints();

                    var abilities = getAbilities();
                    TFTAbilityType? selected = null;

                    if (abilities.Count > 0)
                    {
                        if (!SelectionCursor.ContainsKey(player))
                            SelectionCursor[player] = 0;

                        var cursor = Math.Max(0, Math.Min(SelectionCursor[player], abilities.Count - 1));
                        SelectionCursor[player] = cursor;
                        selected = abilities[cursor];
                    }

                    if (!PlayerHints.ContainsKey(player))
                        PlayerHints[player] = new List<Hint>();

                    var hints = TFTManager.GetUpgradeDisplay(player, upgrades, 0, selected);

                    foreach (var hint in hints)
                    {
                        PlayerHints[player].Add(hint);
                        player.AddCustomHint(hint);
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"[TFT] 증강 선택창 표시 중 예외 / {player.Nickname ?? "null"}\n{e}");
                }
            }
        }

        void RemoveHints()
        {
            if (!PlayerHints.TryGetValue(player, out var playerHints))
                return;

            foreach (var hint in playerHints.Where(x => x.Id == "증강").ToList()) player.RemoveHint(hint);
        }

        void TftAbilityAdd(TFTAbilityType tftAbility)
        {
            var data = tftAbility.GetData();
            if (data == null)
                return;

            upgrades.Add(new Upgrade
            {
                X = -400 + getAbilities().IndexOf(tftAbility) * 800, //-1200 + getAbilities().IndexOf(TFTAbility) * 800,
                Emoji = data.Emoji,
                Title = data.Name,
                Description = data.Description,
                Level = data.Level,
                Type = data.TFTAbilityType
            });
        }
    }

    public static bool Select(this Player player, int index, out string response)
    {
        if (!Selections.ContainsKey(player))
        {
            response = "선택할 수 있는 능력이 없습니다.";
            return false;
        }

        Log.Info("Select called with " + player.Nickname + " and " + index);

        Log.Info("All abilities are not the same");

        var tftAbility = Selections[player].Keys.ElementAt(index - 1);

        player.AddTftAbility(tftAbility);

        Selections.Remove(player);
        SelectionCursor.Remove(player);

        ClearSelectionPresentation(player, true);

        response = $"{index}번 능력 선택 완료!";
        return true;
    }

    public static bool Reroll(this Player player, int index, out string response)
    {
        if (!Selections.ContainsKey(player))
        {
            response = "선택할 수 있는 능력이 없습니다.";
            return false;
        }

        Log.Info("Select called with " + player.Nickname + " and " + index);

        Log.Info("All abilities are not the same");

        var tftAbility = Selections[player].Keys.ElementAt(index - 1);

        if (Selections[player][tftAbility] > 0)
        {
            Selections[player][tftAbility]--;

            var chance = Selections[player][tftAbility];

            var tftAbilityDatas = player.GetValidAbilities(tftAbility.GetData().Level);

            Selections[player].Remove(tftAbility);
            Selections[player].Add(tftAbilityDatas.GetRandomValue().Key.TFTAbilityType, chance);

            PlayAudio(player, "증강 선택창 리롤", 2.5f);
            response = $"{index}번 능력 리롤 완료!";
            return true;
        }
        else
        {
            response = "이 선택지에서 더 이상 리롤할 수 없습니다.";
            return false;
        }
    }

    public static void MoveSelectionCursor(this Player player, int delta)
    {
        if (!Selections.TryGetValue(player, out var selections) || selections.Count == 0)
            return;

        if (!SelectionCursor.ContainsKey(player))
            SelectionCursor[player] = 0;

        var cursor = SelectionCursor[player];
        cursor = (cursor + delta) % selections.Count;

        if (cursor < 0)
            cursor += selections.Count;

        SelectionCursor[player] = cursor;

        PlayAudio(player, "Select");
    }

    public static bool ConfirmSelectionByCursor(this Player player, out string response)
    {
        if (!Selections.TryGetValue(player, out var selections) || selections.Count == 0)
        {
            response = "선택할 수 있는 능력이 없습니다.";
            return false;
        }

        if (!SelectionCursor.ContainsKey(player))
            SelectionCursor[player] = 0;

        var cursor = Math.Max(0, Math.Min(SelectionCursor[player], selections.Count - 1));
        return player.Select(cursor + 1, out response);
    }

    public static void Reset(this Player player)
    {
        player.RemoveAllAbilities();

        Selections.Remove(player);

        SelectionCursor.Remove(player);
        IsSelecting[player] = false;
        IsLifeUsed[player] = false;

        ClearSelectionPresentation(player);
    }
}