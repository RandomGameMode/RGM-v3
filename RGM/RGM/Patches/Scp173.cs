using CustomPlayerEffects;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Scp173;
using HarmonyLib;
using PlayerStatsSystem;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RGM.Patches;

/// <summary>
/// SCP-173 전역 패치:
/// 1) 텔레포트 쿨타임이 MovementBoost 수치에 따라 선형 감소 (3초 → 최소 1초, 상한 수치 255)
///    다른 모드가 이미 쿨타임을 조절 중이면 그 모드의 값을 그대로 둡니다.
/// 2) 기본 공격(스냅)만 공격력 조정 효과에서 제외
///    기본 공격은 Damage = -1 센티넬로 즉사를 표현하기 때문에, 배율이 곱해지면 센티넬이 깨져
///    아무 피해도 들어가지 않습니다. 총기 등 양수 피해는 그대로 조정 효과를 받습니다.
/// </summary>
public static class Scp173Patch
{
    private const float BaseBlinkCooldown = 3f;
    private const float MinimumBlinkCooldown = 1f;
    private const float MaxMovementBoostIntensity = 200f;

    private static readonly Dictionary<Player, float> AppliedBlinkCooldowns = new();

    public static void Apply(Harmony harmony)
    {
        try
        {
            // 다른 모드가 지정한 쿨타임을 덮어쓰지 않으려면 모든 구독자보다 뒤에서 판단해야 하므로
            // 이벤트 구독 대신 디스패처 후처리에서 동작합니다.
            harmony.Patch(
                AccessTools.Method(typeof(Exiled.Events.Handlers.Scp173),
                    nameof(Exiled.Events.Handlers.Scp173.OnBlinking)),
                postfix: new HarmonyMethod(typeof(Scp173Patch), nameof(OnBlinkingPostfix)));

            harmony.Patch(
                AccessTools.Method(typeof(StandardDamageHandler), nameof(StandardDamageHandler.ApplyDamage)),
                prefix: new HarmonyMethod(typeof(Scp173Patch), nameof(ApplyDamagePrefix)));

            Exiled.Events.Handlers.Server.RestartingRound += OnRestartingRound;

            Log.Info("[Scp173Patch] Applied.");
        }
        catch (Exception e)
        {
            Log.Error($"[Scp173Patch] Failed to apply: {e}");
        }
    }

    public static void OnBlinkingPostfix(BlinkingEventArgs ev)
    {
        try
        {
            if (ev is not { IsAllowed: true } || ev.Player == null || ev.Scp173?.BlinkTimer == null)
                return;

            // 다른 모드가 이번 순간이동의 쿨타임을 이벤트에서 지정한 경우
            if (!Mathf.Approximately(ev.BlinkCooldown, BaseBlinkCooldown))
                return;

            // 다른 모드가 쿨타임을 직접 조절 중인 경우 (직전에 이 패치가 넣은 값이 아니면 개입하지 않음)
            float currentCooldown = ev.Scp173.BlinkTimer._totalCooldown;
            if (!Mathf.Approximately(currentCooldown, BaseBlinkCooldown) &&
                (!AppliedBlinkCooldowns.TryGetValue(ev.Player, out float appliedCooldown) ||
                 !Mathf.Approximately(currentCooldown, appliedCooldown)))
                return;

            if (!ev.Player.TryGetEffect(EffectType.MovementBoost, out StatusEffectBase movementBoost) ||
                !movementBoost.IsEnabled)
            {
                AppliedBlinkCooldowns.Remove(ev.Player);
                return;
            }

            float ratio = Mathf.Clamp01(movementBoost.Intensity / MaxMovementBoostIntensity);
            float cooldown = Mathf.Lerp(BaseBlinkCooldown, MinimumBlinkCooldown, ratio);

            ev.BlinkCooldown = cooldown;
            AppliedBlinkCooldowns[ev.Player] = cooldown;
        }
        catch (Exception e)
        {
            Log.Error($"[Scp173Patch] OnBlinkingPostfix Exception: {e}");
        }
    }

    public static void ApplyDamagePrefix(StandardDamageHandler __instance)
    {
        if (__instance is not ScpDamageHandler scpDamageHandler ||
            scpDamageHandler._translationId != DeathTranslations.Scp173.Id)
            return;

        if (scpDamageHandler.Damage < 0f)
            scpDamageHandler.Damage = StandardDamageHandler.KillValue;
    }

    private static void OnRestartingRound()
    {
        AppliedBlinkCooldowns.Clear();
    }
}
