using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Extensions;
using Exiled.Events.EventArgs.Player;
using MEC;
using PlayerRoles;
using RGM.API.Features;
using System.Collections.Generic;

namespace RGM.Modes;

/// <summary>
/// 전용무기 공진(1→5) 1회성 퀘스트.
/// 1) 적 8명 처치 (SCP: 16)
/// 2) 생존 540초 누적 (SCP: +180 → 720)
/// 3) 가한 데미지 10000 (SCP: 받은 데미지 7000)
/// 4) 치료량 1200 (SCP: HS 회복 6000)
/// 퀘스트는 순서와 관계없이 각각 1회 완료할 수 있으며, 완료할 때마다 공진이 증가합니다.
/// </summary>
public static class ExclusiveWeaponQuest
{
    private const int KillTargetHuman = 7;
    private const int KillTargetScp = 14;
    private const float SurviveTargetHuman = 540f;
    private const float SurviveTargetScp = 720f;
    private const float DealDamageTarget = 12000f;
    private const float TakeDamageTargetScp = 7500f;
    private const float HealTargetHuman = 1500f;
    private const float HsRecoverTargetScp = 4500f;

    private static readonly Dictionary<Player, CoroutineHandle> TrackHandles = new();
    private static readonly Dictionary<Player, float> PrevHs = new();

    public static void Register()
    {
        Exiled.Events.Handlers.Player.Hurting += OnHurting;
        Exiled.Events.Handlers.Player.Died += OnDied;
        Exiled.Events.Handlers.Player.Healing += OnHealing;
    }

    public static void Unregister()
    {
        Exiled.Events.Handlers.Player.Hurting -= OnHurting;
        Exiled.Events.Handlers.Player.Died -= OnDied;
        Exiled.Events.Handlers.Player.Healing -= OnHealing;

        foreach (var handle in TrackHandles.Values)
            Timing.KillCoroutines(handle);

        TrackHandles.Clear();
        PrevHs.Clear();
    }

    public static void EnsureTracking(Player player)
    {
        if (player == null || !player.IsAlive)
            return;

        if (!TryGetEquipped(player, out _, out _))
        {
            StopTracking(player);
            return;
        }

        if (TrackHandles.TryGetValue(player, out var handle) && handle.IsRunning)
            return;

        PrevHs[player] = player.HumeShield;
        TrackHandles[player] = Timing.RunCoroutine(TrackRoutine(player), $"ExcWeaponQuest_{player.UserId}");
    }

    public static void StopTracking(Player player)
    {
        if (player == null)
            return;

        Timing.KillCoroutines($"ExcWeaponQuest_{player.UserId}");
        TrackHandles.Remove(player);
        PrevHs.Remove(player);
    }

    public static void ClearPlayer(Player player)
    {
        StopTracking(player);
    }

    private static IEnumerator<float> TrackRoutine(Player player)
    {
        while (player != null && player.IsAlive)
        {
            if (!TryGetEquipped(player, out var type, out var progress))
            {
                TrackHandles.Remove(player);
                yield break;
            }

            // 공진 Max면 추적만 유지할 필요 없음
            if (progress.GetResonance(type) >= ExclusiveWeaponInfo.MaxResonance)
            {
                TrackHandles.Remove(player);
                yield break;
            }

            var quest = progress.GetOrCreateQuest(type);
            quest.SurviveSeconds += 1f;

            if (player.IsScp)
            {
                float hs = player.HumeShield;
                if (PrevHs.TryGetValue(player, out float prev) && hs > prev)
                    quest.HsRecovered += hs - prev;
                PrevHs[player] = hs;
            }

            TryAdvanceResonance(player, type, progress);
            yield return Timing.WaitForSeconds(1f);
        }

        // 사망 시 생존 누적은 유지 (SurviveSeconds는 건드리지 않음)
        if (player != null)
            TrackHandles.Remove(player);
    }

    private static void OnHurting(HurtingEventArgs ev)
    {
        float damage = ev.DamageHandler?.Damage ?? 0f;
        if (damage <= 0f)
            return;

        bool isDirectEnemyAttack = ev.Attacker != null
            && ev.Attacker != ev.Player
            && HitboxIdentity.IsEnemy(ev.Attacker.ReferenceHub, ev.Player.ReferenceHub)
            && ev.DamageHandler?.Type != DamageType.Tesla;

        if (isDirectEnemyAttack && TryGetEquipped(ev.Attacker, out var atkType, out var atkProgress))
        {
            var quest = atkProgress.GetOrCreateQuest(atkType);
            quest.DamageDealt += damage;
            TryAdvanceResonance(ev.Attacker, atkType, atkProgress);
        }

        if (isDirectEnemyAttack && TryGetEquipped(ev.Player, out var defType, out var defProgress))
        {
            var quest = defProgress.GetOrCreateQuest(defType);
            quest.DamageTaken += damage;
            TryAdvanceResonance(ev.Player, defType, defProgress);
        }
    }

    private static void OnDied(DiedEventArgs ev)
    {
        if (ev.Attacker == null
            || ev.Player == null
            || ev.Attacker == ev.Player
            || !TryGetEquipped(ev.Attacker, out var type, out var progress)
            || !IsEnemyRole(ev.Attacker.Role.Type, ev.TargetOldRole))
            return;

        var quest = progress.GetOrCreateQuest(type);
        quest.Kills++;
        TryAdvanceResonance(ev.Attacker, type, progress);
    }

    private static void OnHealing(HealingEventArgs ev)
    {
        if (ev.Player == null || ev.Amount <= 0f)
            return;

        if (!TryGetEquipped(ev.Player, out var type, out var progress))
            return;

        if (ev.Player.IsScp)
            return;

        var quest = progress.GetOrCreateQuest(type);
        quest.HealingDone += ev.Amount;
        TryAdvanceResonance(ev.Player, type, progress);
    }

    /// <summary>
    /// 완료된 미청구 퀘스트를 순서와 관계없이 각각 청구하며 승급합니다.
    /// Claimed 플래그로 같은 퀘스트가 여러 이벤트에서 중복 청구되는 것을 방지합니다.
    /// </summary>
    private static void TryAdvanceResonance(Player player, ExclusiveWeaponType type, ExclusiveWeaponProgress progress)
    {
        if (player == null || progress == null)
            return;

        var quest = progress.GetOrCreateQuest(type);
        bool advanced = false;

        for (int questIndex = 0; questIndex < quest.Claimed.Length; questIndex++)
        {
            int resonance = progress.GetResonance(type);
            if (resonance >= ExclusiveWeaponInfo.MaxResonance)
                break;

            if (quest.Claimed[questIndex])
                continue;

            if (!IsQuestComplete(player, questIndex, quest))
                continue;

            quest.Claimed[questIndex] = true;
            int newResonance = ExclusiveWeaponStats.Clamp(resonance + 1, 1, ExclusiveWeaponInfo.MaxResonance);
            progress.Resonance[type] = newResonance;
            advanced = true;

            var data = type.GetData();
            string name = data?.Name ?? type.ToString();
            player.AddBroadcast(4,
                $"<size=24><color=#ffcc66>{name}</color> 공진 <b>{resonance}</b> → <b>{newResonance}</b></size>\n" +
                $"<size=18>{GetQuestDescription(player, questIndex)}</size>");
        }

        if (advanced && ExclusiveWeaponInfo.PlayerWeapons.ContainsKey(player))
        {
            MEC.Timing.CallDelayed(MEC.Timing.WaitForOneFrame, () =>
            {
                if (player != null && player.IsAlive)
                    EchoBattleCore.ApplyLoadout(player);
            });
        }
    }

    private static bool IsQuestComplete(Player player, int questIndex, ResonanceQuestState quest)
    {
        bool scp = player.IsScpRole();

        return questIndex switch
        {
            0 => quest.Kills >= (scp ? KillTargetScp : KillTargetHuman),
            1 => quest.SurviveSeconds >= (scp ? SurviveTargetScp : SurviveTargetHuman),
            2 => scp
                ? quest.DamageTaken >= TakeDamageTargetScp
                : quest.DamageDealt >= DealDamageTarget,
            3 => scp
                ? quest.HsRecovered >= HsRecoverTargetScp
                : quest.HealingDone >= HealTargetHuman,
            _ => false
        };
    }

    private static string GetQuestDescription(Player player, int questIndex)
    {
        bool scp = player != null && player.IsScpRole();
        return questIndex switch
        {
            0 => scp ? "적 14명 처치" : "적 7명 처치",
            1 => scp ? "생존 720초 누적" : "생존 540초 누적",
            2 => scp ? "받은 데미지 7500" : "가한 데미지 12500",
            3 => scp ? "HS 회복 4500" : "치료량 1500",
            _ => "?"
        };
    }

    public static string FormatCurrentQuest(Player player, ExclusiveWeaponType type)
    {
        var progress = ExclusiveWeaponInfo.GetOrCreateProgress(player);
        int resonance = progress.GetResonance(type);
        if (resonance >= ExclusiveWeaponInfo.MaxResonance)
            return "공진 MAX";

        var quest = progress.GetOrCreateQuest(type);
        var lines = new List<string>();

        for (int questIndex = 0; questIndex < quest.Claimed.Length; questIndex++)
        {
            if (quest.Claimed[questIndex])
                continue;

            string label = GetQuestDescription(player, questIndex);
            string value = GetQuestProgress(player, questIndex, quest);
            lines.Add($"{label} ({value})");
        }

        return string.Join("\n", lines);
    }

    private static string GetQuestProgress(Player player, int questIndex, ResonanceQuestState quest)
    {
        bool scp = player.IsScpRole();
        return questIndex switch
        {
            0 => $"{quest.Kills}/{(scp ? KillTargetScp : KillTargetHuman)}",
            1 => $"{quest.SurviveSeconds:0}/{(scp ? SurviveTargetScp : SurviveTargetHuman):0}",
            2 => scp
                ? $"{quest.DamageTaken:0}/{TakeDamageTargetScp:0}"
                : $"{quest.DamageDealt:0}/{DealDamageTarget:0}",
            3 => scp
                ? $"{quest.HsRecovered:0}/{HsRecoverTargetScp:0}"
                : $"{quest.HealingDone:0}/{HealTargetHuman:0}",
            _ => "?"
        };
    }

    private static bool TryGetEquipped(Player player, out ExclusiveWeaponType type, out ExclusiveWeaponProgress progress)
    {
        type = ExclusiveWeaponType.None;
        progress = null;

        if (player == null)
            return false;

        if (!EchoInfo.PlayerLoadouts.TryGetValue(player, out var loadout) || !loadout.EquippedWeapon.HasValue)
            return false;

        type = loadout.EquippedWeapon.Value;
        progress = ExclusiveWeaponInfo.GetOrCreateProgress(player);
        return true;
    }

    private static bool IsEnemyRole(RoleTypeId attackerRole, RoleTypeId targetRole)
    {
        return HitboxIdentity.IsEnemy(RoleExtensions.GetTeam(attackerRole), RoleExtensions.GetTeam(targetRole));
    }
}
