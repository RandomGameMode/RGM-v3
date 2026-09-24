using Exiled.API.Features;
using RGM.API.Features;
using System;
using System.Collections.Generic;

namespace RGM.Modes;

/// <summary>
/// 전용무기 XP. 1레벨 기준 20, 이후 ceil(1.05x + 15). 최대 90.
/// </summary>
public static class ExclusiveWeaponGrowth
{
    private static readonly HashSet<Player> PendingApply = [];

    private static int GetRequiredExp(int currentLevel)
    {
        currentLevel = ExclusiveWeaponStats.Clamp(currentLevel, 1, ExclusiveWeaponInfo.MaxLevel);
        if (currentLevel >= ExclusiveWeaponInfo.MaxLevel)
            return 0;

        int exp = ExclusiveWeaponInfo.BaseExp;
        for (int level = 1; level < currentLevel; level++)
            exp = NextExp(exp);

        return exp;
    }

    private static int NextExp(int previousRequired)
    {
        return (int)Math.Ceiling(ExclusiveWeaponInfo.LevelExpMultiplier * previousRequired + ExclusiveWeaponInfo.LevelExpAdd);
    }

    public static bool CanGrow(Player player)
    {
        if (player == null)
            return false;

        if (!EchoInfo.PlayerLoadouts.TryGetValue(player, out var loadout) || !loadout.EquippedWeapon.HasValue)
            return false;

        var progress = ExclusiveWeaponInfo.GetOrCreateProgress(player);
        return progress.GetLevel(loadout.EquippedWeapon.Value) < ExclusiveWeaponInfo.MaxLevel;
    }

    public static void GrantExp(Player player, float amount, string reason = null)
    {
        if (player == null || amount <= 0)
            return;

        if (!EchoInfo.PlayerLoadouts.TryGetValue(player, out var loadout) || !loadout.EquippedWeapon.HasValue)
            return;

        var type = loadout.EquippedWeapon.Value;
        var progress = ExclusiveWeaponInfo.GetOrCreateProgress(player);
        int oldLevel = progress.GetLevel(type);

        if (!AddExp(progress, type, amount, out int newLevel) || newLevel <= oldLevel)
            return;

        var data = type.GetData();
        string name = data?.Name ?? type.ToString();
        player.AddBroadcast(3, $"<size=25><color=#ffcc66>{name}</color> → Lv.<b>{newLevel}</b></size>");

        ScheduleApply(player);
    }

    private static bool AddExp(ExclusiveWeaponProgress progress, ExclusiveWeaponType type, float amount, out int newLevel)
    {
        newLevel = progress.GetLevel(type);
        if (amount <= 0 || newLevel >= ExclusiveWeaponInfo.MaxLevel)
            return false;

        if (!progress.Experience.ContainsKey(type))
            progress.Experience[type] = 0;

        progress.Experience[type] += amount;
        bool leveled = false;

        while (newLevel < ExclusiveWeaponInfo.MaxLevel)
        {
            int required = GetRequiredExp(newLevel);
            if (required <= 0 || progress.Experience[type] < required)
                break;

            progress.Experience[type] -= required;
            newLevel++;
            progress.Levels[type] = newLevel;
            leveled = true;
        }

        if (newLevel >= ExclusiveWeaponInfo.MaxLevel)
            progress.Experience[type] = 0;

        return leveled;
    }

    public static string FormatProgress(Player player, ExclusiveWeaponType type)
    {
        var progress = ExclusiveWeaponInfo.GetOrCreateProgress(player);
        int level = progress.GetLevel(type);
        if (level >= ExclusiveWeaponInfo.MaxLevel)
            return "MAX";

        float current = progress.GetExperience(type);
        int required = GetRequiredExp(level);
        return $"{current:0.##}/{required}";
    }

    private static void ScheduleApply(Player player)
    {
        if (player == null || !ExclusiveWeaponInfo.PlayerWeapons.ContainsKey(player))
            return;

        if (!PendingApply.Add(player))
            return;

        MEC.Timing.CallDelayed(MEC.Timing.WaitForOneFrame, () =>
        {
            PendingApply.Remove(player);

            if (player != null && player.IsAlive && EchoInfo.PlayerLoadouts.ContainsKey(player))
                EchoBattleCore.RefreshGrowthStats(player);
        });
    }

    public static void ClearPending(Player player)
    {
        if (player != null)
            PendingApply.Remove(player);
    }
}
