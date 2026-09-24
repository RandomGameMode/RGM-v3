using Exiled.API.Features;
using HarmonyLib;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.Modules;
using InventorySystem.Items.Firearms.Modules.Scp127;
using PlayerStatsSystem;
using System;

namespace RGM.Patches;

/// <summary>
/// 무기 전역 패치:
/// 1) 리볼버 거리 감쇠 제거
/// 2) SCP-127 거리 감쇠를 성장 단계에 따라 완화 (1단계 유지 / 2단계 30m 증가 / 3단계 무제한)
///    감쇠 거리는 사격 시 레이캐스트 최대 거리(감쇠 거리 + 최대 피해 거리)도 함께 결정하므로,
///    값을 늘리면 사거리 제한까지 같이 풀립니다.
/// </summary>
public static class WeaponPatch
{
    /// <summary>거리 감쇠를 사실상 제거하는 값입니다.</summary>
    private const float UnlimitedFalloffDistance = 999f;

    private const float Scp127Tier2FalloffBonus = 30f;

    private const float Fsp9BaseDamageReduction = 6f;

    private const float FrMg0BaseDamageBonus = 2f;

    /// <summary>기본 헤드샷 배율에 합산할 MP7 보너스입니다. (800%p = 8.0배)</summary>
    private const float Fsp9HeadshotMultiplierBonus = 8.0f;

    public static void Apply(Harmony harmony)
    {
        try
        {
            // 총기 인스턴스마다 값을 덮어쓰면 지급·습득·SCP-914 등 모든 획득 경로를 따라다녀야 하므로,
            // 조회 지점 한 곳만 보정합니다.
            harmony.Patch(
                AccessTools.PropertyGetter(typeof(HitscanHitregModuleBase),
                    nameof(HitscanHitregModuleBase.DamageFalloffDistance)),
                postfix: new HarmonyMethod(typeof(WeaponPatch), nameof(DamageFalloffDistanceGetterPostfix)));

            harmony.Patch(
                AccessTools.PropertyGetter(typeof(HitscanHitregModuleBase),
                    nameof(HitscanHitregModuleBase.BaseDamage)),
                postfix: new HarmonyMethod(typeof(WeaponPatch), nameof(BaseDamageGetterPostfix)));

            harmony.Patch(
                AccessTools.Method(typeof(Scp127Hitscan), nameof(Scp127Hitscan.TryGetCurPair)),
                prefix: new HarmonyMethod(typeof(WeaponPatch), nameof(Scp127TryGetCurPairPrefix)));

            harmony.Patch(
                AccessTools.Method(typeof(FirearmDamageHandler), nameof(FirearmDamageHandler.ProcessDamage)),
                prefix: new HarmonyMethod(typeof(WeaponPatch), nameof(FirearmDamageHandlerProcessDamagePrefix)));

            Log.Info("[WeaponPatch] Applied.");
        }
        catch (Exception e)
        {
            Log.Error($"[WeaponPatch] Failed to apply: {e}");
        }
    }

    public static void DamageFalloffDistanceGetterPostfix(HitscanHitregModuleBase __instance, ref float __result)
    {
        try
        {
            Firearm firearm = __instance.Firearm;

            if (firearm == null)
                return;

            switch (firearm.ItemTypeId)
            {
                case ItemType.GunRevolver:
                    __result = UnlimitedFalloffDistance;
                    break;

                case ItemType.GunSCP127:
                    __result = GetScp127FalloffDistance(firearm, __result);
                    break;
            }
        }
        catch (Exception e)
        {
            Log.Error($"[WeaponPatch] DamageFalloffDistanceGetterPostfix Exception: {e}");
        }
    }

    public static void BaseDamageGetterPostfix(HitscanHitregModuleBase __instance, ref float __result)
    {
        try
        {
            switch (__instance.Firearm?.ItemTypeId)
            {
                case ItemType.GunFSP9:
                    __result -= Fsp9BaseDamageReduction;
                    break;

                case ItemType.GunFRMG0:
                    __result += FrMg0BaseDamageBonus;
                    break;
            }
        }
        catch (Exception e)
        {
            Log.Error($"[WeaponPatch] BaseDamageGetterPostfix Exception: {e}");
        }
    }

    public static bool Scp127TryGetCurPairPrefix(Scp127Hitscan __instance, ref Scp127Hitscan.StatsTierPair ret)
    {
        // 플레이어 퇴장 시 인벤토리 아이템은 Owner가 해제된 뒤 픽업 정보가 생성될 수 있습니다.
        // 원본 GetTierForItem은 이 경우 Owner.netId를 읽으므로, 기본 총기 수치를 사용하게 합니다.
        if (__instance.Firearm?.Owner != null)
            return true;

        ret = default;
        return false;
    }

    public static void FirearmDamageHandlerProcessDamagePrefix(FirearmDamageHandler __instance)
    {
        try
        {
            if (__instance.WeaponType != ItemType.GunFSP9 ||
                __instance.Hitbox != HitboxType.Headshot ||
                !FirearmDamageHandler.HitboxDamageMultipliers.TryGetValue(HitboxType.Headshot,
                    out float baseMultiplier) ||
                baseMultiplier <= 0f)
                return;

            // ProcessDamage가 뒤이어 기본 헤드샷 배율을 적용하므로, 선보정하여 최종 배율에 8.0를 합산합니다.
            __instance.Damage *= (baseMultiplier + Fsp9HeadshotMultiplierBonus) / baseMultiplier;
        }
        catch (Exception e)
        {
            Log.Error($"[WeaponPatch] FirearmDamageHandlerProcessDamagePrefix Exception: {e}");
        }
    }

    private static float GetScp127FalloffDistance(Firearm firearm, float baseFalloffDistance)
    {
        // 성장 단계는 총기 소유자별로 기록되므로, 주인이 없는 동안에는 기본값을 사용합니다.
        if (firearm.Owner == null)
            return baseFalloffDistance;

        return Scp127TierManagerModule.GetTierForItem(firearm) switch
        {
            Scp127Tier.Tier2 => baseFalloffDistance + Scp127Tier2FalloffBonus,
            Scp127Tier.Tier3 => UnlimitedFalloffDistance,
            _ => baseFalloffDistance,
        };
    }
}