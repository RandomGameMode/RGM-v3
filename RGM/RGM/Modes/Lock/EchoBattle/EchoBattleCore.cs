using Exiled.API.Features;
using Exiled.API.Features.Roles;
using HintServiceMeow.Core.Enum;
using HintServiceMeow.Core.Extension;
using MEC;
using RGM.API.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Hint = HintServiceMeow.Core.Models.Hints.Hint;

namespace RGM.Modes;

public static class EchoBattleCore
{
    // 상태표는 우측 상단, 알림은 화면 중앙에 분리해 서로 가리지 않도록 배치합니다.
    private const float HintX = 420f;
    private const float HintY = 330f;
    private const float NotificationX = 0f;
    private const float NotificationY = 770f;
    private const int MaxNotifications = 5;

    private sealed class Notification
    {
        public string Text;
        public DateTime ExpiresAt;
    }

    private static readonly Dictionary<Player, List<Notification>> Notifications = new();

    public static void ShowNotification(Player player, string text, float duration = 3f)
    {
        if (player == null || string.IsNullOrWhiteSpace(text))
            return;

        DateTime now = DateTime.UtcNow;
        if (!Notifications.TryGetValue(player, out var queue))
        {
            queue = [];
            Notifications[player] = queue;
        }

        queue.RemoveAll(x => x.ExpiresAt <= now);

        // 같은 알림이 연속으로 들어오면 줄을 늘리지 않고 표시 시간만 갱신합니다.
        var duplicate = queue.FirstOrDefault(x => x.Text == text);
        if (duplicate != null)
        {
            duplicate.ExpiresAt = now.AddSeconds(Math.Max(0.1f, duration));
            return;
        }

        queue.Add(new Notification
        {
            Text = text,
            ExpiresAt = now.AddSeconds(Math.Max(0.1f, duration))
        });

        while (queue.Count > MaxNotifications)
            queue.RemoveAt(0);
    }

    private static string BuildNotificationText(Player player)
    {
        if (player == null || !Notifications.TryGetValue(player, out var queue))
            return null;

        DateTime now = DateTime.UtcNow;
        queue.RemoveAll(x => x.ExpiresAt <= now);
        if (queue.Count == 0)
        {
            Notifications.Remove(player);
            return null;
        }

        return string.Join("\n", queue.Select(x => x.Text));
    }

    public static void ClearNotifications(Player player)
    {
        if (player != null)
            Notifications.Remove(player);
    }

    public static void RegisterEchoes()
    {
        EchoInfo.Echoes.Clear();

        foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
        {
            var attr = type.GetCustomAttribute<EchoAttribute>();
            if (attr == null || !typeof(Echo).IsAssignableFrom(type))
                continue;

            EchoInfo.Echoes[attr.Type] = new EchoData
            {
                Type = type,
                Name = attr.Name,
                Description = attr.Description,
                Emoji = attr.Emoji,
                EchoType = attr.Type,
                Cost = attr.Cost,
                MainStatType = attr.MainStatType
            };
        }

        Log.Info($"[EchoBattle] Registered {EchoInfo.Echoes.Count} echoes.");
    }

    public static void AddEcho(Player player, EchoType type, int level, bool isMainSlot, int slotIndex = 0)
    {
        if (!EchoInfo.Echoes.TryGetValue(type, out var data))
        {
            Log.Error($"[EchoBattle] Echo {type} not found.");
            return;
        }

        Echo echo;
        try
        {
            echo = Activator.CreateInstance(data.Type) as Echo;
        }
        catch (Exception e)
        {
            Log.Error($"[EchoBattle] Failed to create {data.Name}: {e}");
            return;
        }

        if (echo == null)
            return;

        echo.Data = data;
        echo.Owner = player;
        echo.Level = EchoStats.Clamp(level, 1, EchoInfo.MaxLevel);
        echo.IsMainSlot = isMainSlot;

        if (!EchoInfo.PlayerLoadouts.TryGetValue(player, out var loadout))
        {
            loadout = new EchoLoadout();
            EchoInfo.PlayerLoadouts[player] = loadout;
        }

        echo.SelectedMainStat = loadout.ResolveMainStat(slotIndex, data);

        // 기존 부가 옵션 유지 + 해금분만 추가 (레벨업 재적용 시 재롤/수치 하락 방지)
        echo.SubOptions = EchoStats.EnsureSubOptions(loadout, type, echo.Level);

        if (!EchoInfo.PlayerEchoes.ContainsKey(player))
            EchoInfo.PlayerEchoes[player] = new();

        EchoInfo.PlayerEchoes[player].Add(echo);
        echo.OnEnabled();
    }

    public static void RemoveAllEchoes(Player player)
    {
        EchoStats.ClearPassiveEffects(player);

        if (!EchoInfo.PlayerEchoes.TryGetValue(player, out var list))
        {
            EchoInfo.PlayerStats.Remove(player);
            return;
        }

        foreach (var echo in list)
            echo.OnActiveEffect();

        list.Clear();
        EchoInfo.PlayerStats.Remove(player);
    }

    /// <summary>
    /// 역할 변경/리셋 시 호출. 기본 MaxHealth/HS 캐시도 함께 비웁니다.
    /// </summary>
    public static void ClearPlayerRuntime(Player player)
    {
        RemoveAllEchoes(player);
        EchoInfo.PlayerBaseMaxHealth.Remove(player);
        EchoInfo.PlayerBaseMaxHs.Remove(player);
        EchoInfo.PlayerPassiveEffects.Remove(player);
    }

    public static void ApplyLoadout(Player player)
    {
        // RemoveAllEchoes가 스탯 AHP를 지우므로, 재적용 전 현재량을 보존
        float? preservedEchoAhp = EchoStats.TryPeekEchoAhpAmount(player, out float echoAhp)
            ? echoAhp
            : null;

        // 레벨업 재적용 시에도 역할 기본 MaxHealth는 유지해야 복리가 나지 않음
        RemoveAllEchoes(player);
        ExclusiveWeaponCore.RemoveWeapon(player);

        if (!EchoInfo.PlayerLoadouts.TryGetValue(player, out var loadout))
        {
            EchoQuest.StopSurviveTracking(player);
            ExclusiveWeaponQuest.StopTracking(player);
            return;
        }

        // 적용 직전 슬롯/스탯 잔존값 제거
        loadout.SanitizeMainSlot();
        loadout.SanitizeAllMainStats();

        if (loadout.MainSlot.HasValue)
            AddEcho(player, loadout.MainSlot.Value, loadout.GetLevel(loadout.MainSlot.Value), true, 0);

        for (int i = 0; i < loadout.SubSlots.Length; i++)
        {
            if (loadout.SubSlots[i].HasValue)
                AddEcho(player, loadout.SubSlots[i].Value, loadout.GetLevel(loadout.SubSlots[i].Value), false, i + 1);
        }

        ExclusiveWeaponCore.ApplyWeapon(player);

        bool hasEchoes = EchoInfo.PlayerEchoes.TryGetValue(player, out var echoes) && echoes.Count > 0;
        bool hasWeapon = ExclusiveWeaponInfo.PlayerWeapons.ContainsKey(player);

        if (!hasEchoes && !hasWeapon)
        {
            EchoQuest.StopSurviveTracking(player);
            ExclusiveWeaponQuest.StopTracking(player);
            return;
        }

        var snapshot = hasEchoes
            ? EchoStats.BuildSnapshot(player, echoes)
            : new EchoStatSnapshot();

        ExclusiveWeaponCore.MergeStats(player, snapshot);
        EchoInfo.PlayerStats[player] = snapshot;
        EchoStats.ApplyPassiveEffects(player, snapshot, preservedEchoAhp);

        // 레벨업 재적용 시 생존 타이머가 리셋되지 않도록, 미추적일 때만 시작
        EchoQuest.EnsureSurviveTracking(player);
        ExclusiveWeaponQuest.EnsureTracking(player);
    }

    /// <summary>
    /// 레벨업에 따른 스탯만 다시 계산합니다.
    /// 액티브 Echo/전용무기 인스턴스를 유지하여 사용 중인 지속 효과와 쿨타임이 초기화되지 않습니다.
    /// </summary>
    public static void RefreshGrowthStats(Player player)
    {
        if (player == null || !EchoInfo.PlayerLoadouts.TryGetValue(player, out var loadout))
            return;

        bool hasEchoes = EchoInfo.PlayerEchoes.TryGetValue(player, out var echoes);
        bool hasWeapon = ExclusiveWeaponInfo.PlayerWeapons.TryGetValue(player, out var weapon) && weapon != null;

        // 장착 변경, 역할 변경 등으로 런타임 인스턴스가 없거나 불일치하면 기존 전체 적용 경로를 사용합니다.
        if ((!hasEchoes && !hasWeapon)
            || (loadout.MainSlot.HasValue && !echoes.Any(x => x.IsMainSlot && x.Data?.EchoType == loadout.MainSlot.Value))
            || (loadout.EquippedWeapon.HasValue && (!hasWeapon || weapon.Data?.WeaponType != loadout.EquippedWeapon.Value)))
        {
            ApplyLoadout(player);
            return;
        }

        float? preservedEchoAhp = EchoStats.TryPeekEchoAhpAmount(player, out float echoAhp)
            ? echoAhp
            : null;

        foreach (var echo in echoes)
        {
            if (echo?.Data == null)
                continue;

            echo.Level = loadout.GetLevel(echo.Data.EchoType);
            echo.SubOptions = EchoStats.EnsureSubOptions(loadout, echo.Data.EchoType, echo.Level);
        }

        if (hasWeapon)
        {
            weapon.Level = ExclusiveWeaponInfo.GetOrCreateProgress(player).GetLevel(weapon.Data.WeaponType);
            weapon.Resonance = ExclusiveWeaponInfo.GetOrCreateProgress(player).GetResonance(weapon.Data.WeaponType);
        }

        var snapshot = hasEchoes ? EchoStats.BuildSnapshot(player, echoes) : new EchoStatSnapshot();
        ExclusiveWeaponCore.MergeStats(player, snapshot);
        EchoInfo.PlayerStats[player] = snapshot;
        EchoStats.ApplyPassiveEffects(player, snapshot, preservedEchoAhp);
    }

    public static void Reset(Player player)
    {
        EchoQuest.StopSurviveTracking(player);
        ExclusiveWeaponCore.Reset(player);
        ClearNotifications(player);
        ClearPlayerRuntime(player);
    }

    public static IEnumerator<float> HintDisplay(Player owner)
    {
        while (true)
        {
            Player target = owner;
            Hint statusHint = null;
            Hint notificationHint = null;

            try
            {
                if (owner.Role is SpectatorRole spectator && spectator.SpectatedPlayer != null)
                    target = spectator.SpectatedPlayer;

                bool showStatus = !Round.IsEnded &&
                    (!EchoInfo.PlayerShowHints.TryGetValue(owner, out bool enabled) || enabled);
                if (showStatus && target != null && target.IsAlive)
                {
                    string text = BuildHintText(target);
                    if (!string.IsNullOrEmpty(text))
                    {
                        statusHint = new Hint
                        {
                            Text = $"<size=14>{text}</size>",
                            Id = "EchoBattleHint",
                            XCoordinate = HintX,
                            YCoordinate = HintY,
                            Alignment = HintAlignment.Right
                        };

                        owner.AddCustomHint(statusHint);
                    }
                }

                string notificationText = BuildNotificationText(owner);
                if (!string.IsNullOrEmpty(notificationText))
                {
                    notificationHint = new Hint
                    {
                        Text = $"<size=22>{notificationText}</size>",
                        Id = "EchoBattleNotification",
                        XCoordinate = NotificationX,
                        YCoordinate = NotificationY,
                        Alignment = HintAlignment.Center
                    };

                    owner.AddCustomHint(notificationHint);
                }
            }
            catch (Exception e)
            {
                Log.Debug($"[EchoBattle] Hint error: {e.Message}");
            }

            yield return Timing.WaitForOneFrame;

            if (statusHint != null)
                owner.RemoveHint(statusHint);
            if (notificationHint != null)
                owner.RemoveHint(notificationHint);
        }
    }

    private static string BuildHintText(Player player)
    {
        var lines = new List<string>();

        string weaponHint = ExclusiveWeaponCore.BuildHintSection(player);
        if (!string.IsNullOrEmpty(weaponHint))
            lines.Add(weaponHint);

        if (!EchoInfo.PlayerEchoes.TryGetValue(player, out var echoes) || echoes.Count == 0)
        {
            if (lines.Count == 0)
            {
                lines.Add("<color=#88aaff>[ESC] → Settings → Server-specific</color>");
                lines.Add("전용무기 / Echo를 장착하세요.");
            }

            if (EchoInfo.PlayerStats.TryGetValue(player, out var weaponOnlyStats))
            {
                lines.Add("<color=#aaaaaa>── 강화 합산 ──</color>");
                foreach (var pair in weaponOnlyStats.GetAggregatedDisplay())
                    lines.Add($"{pair.Key}: <b>{pair.Value:0.#}</b>");
            }

            return string.Join("\n", lines);
        }

        EchoInfo.PlayerLoadouts.TryGetValue(player, out var loadout);

        var main = echoes.FirstOrDefault(x => x.IsMainSlot);
        if (main is EchoActiveAbility active)
        {
            string status;
            if (active.RemainingDuration > 0.1f)
                status = $"<color=#7CFC00>지속 {active.RemainingDuration:0}초</color>";
            else if (active.IsOnCooldown)
                status = $"<color=#ff6666>쿨 {active.RemainingCooldown:0}초</color>";
            else
                status = "<color=#7CFC00>사용 가능</color>";

            lines.Add($"<b>{main.Data.GetFormattedName()}</b> Lv.{main.Level}");
            if (loadout != null)
                lines.Add($"XP {EchoGrowth.FormatProgress(loadout, main.Data.EchoType)}");
            lines.Add($"[ALT] {status}");
        }
        else if (main != null)
        {
            lines.Add($"<b>{main.Data.GetFormattedName()}</b> Lv.{main.Level}");
            if (loadout != null)
                lines.Add($"XP {EchoGrowth.FormatProgress(loadout, main.Data.EchoType)}");
        }

        // 장착 Echo 레벨 요약
        if (loadout != null)
        {
            var equippedLines = new List<string>();
            foreach (var echo in echoes)
            {
                if (echo?.Data == null)
                    continue;
                equippedLines.Add($"{echo.Data.Emoji}{echo.Data.Name} Lv.{echo.Level} [{EchoStats.GetMainStatDisplayName(echo.SelectedMainStat)}] ({EchoGrowth.FormatProgress(loadout, echo.Data.EchoType)})");
            }

            if (equippedLines.Count > 0)
            {
                lines.Add("<color=#aaaaaa>── Echo ──</color>");
                lines.AddRange(equippedLines);
            }
        }

        if (EchoInfo.PlayerStats.TryGetValue(player, out var stats))
        {
            lines.Add("<color=#aaaaaa>── 강화 합산 ──</color>");
            foreach (var pair in stats.GetAggregatedDisplay())
                lines.Add($"{pair.Key}: <b>{pair.Value:0.#}</b>");
        }

        return string.Join("\n", lines);
    }
}
