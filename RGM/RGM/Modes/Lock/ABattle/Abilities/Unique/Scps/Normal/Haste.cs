using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Discord;
using Exiled.API.Features;
using Exiled.API.Features.Roles;
using HarmonyLib;
using MEC;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp049;
using PlayerRoles.PlayableScps.Scp079.Pinging;
using RGM.API.Features;
using RGM.Modes.Patches;
using UnityEngine;
using Scp049Role = Exiled.API.Features.Roles.Scp049Role;

namespace RGM.Modes.Abilities.Unique.Scps.Normal;

/*[Ability("가속", "SCP 진영의 스킬 쿨타임이 10%p 감소합니다. (최대 60%p까지 적용)",
    AbilityCategory.Normal, AbilityType.NORMAL_SCPS_HASTE, RoleAbility.Scps)]*/
public class Haste : Ability
{
    private const float ReductionPerAbility = 0.1f;
    private const float MaximumReduction = 0.6f;
    private const float PingCooldown = 3f;
    private const string ModeID = "com.RGM.Modes.ABattle.Haste";

    private static readonly FieldInfo PingRateLimiterField = typeof(Scp079PingAbility).GetField(
        "_rateLimiter",
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

    private CoroutineHandle _onStarted;

    public static byte Count { get; private set; }
    private static Harmony _harmony;

    public override void OnEnabled()
    {
        if (Count < 15)
            Count++;
        
        _harmony ??= new Harmony(ModeID);
        Exiled.Events.Handlers.Scp079.Pinging += OnPinging;

        _onStarted = Timing.RunCoroutine(OnStarted());
    }

    public override void OnDisabled()
    {
        _harmony.UnpatchAll(ModeID);
        _harmony = null;
        Exiled.Events.Handlers.Scp079.Pinging -= OnPinging;

        Timing.KillCoroutines(_onStarted);
    }

    private IEnumerator<float> OnStarted()
    {
        while (true)
        {
            if (!IsPrimaryInstance())
            {
                yield return Timing.WaitForOneFrame;
                continue;
            }

            float reduction = GetCooldownReduction();
            float additionalRecovery = Timing.DeltaTime * reduction / (1f - reduction);

            switch (Owner.Role)
            {
                case Scp049Role scp049:
                    scp049.CallCooldown = ReduceCooldown(scp049.CallCooldown, additionalRecovery);
                    scp049.GoodSenseCooldown = ReduceCooldown(scp049.GoodSenseCooldown, additionalRecovery);
                    break;
                case Scp106Role scp106:
                    scp106.RemainingSinkholeCooldown =
                        ReduceCooldown(scp106.RemainingSinkholeCooldown, additionalRecovery);
                    break;
                case Scp173Role scp173:
                    scp173.RemainingBreakneckCooldown =
                        ReduceCooldown(scp173.RemainingBreakneckCooldown, additionalRecovery);
                    scp173.RemainingTantrumCooldown =
                        ReduceCooldown(scp173.RemainingTantrumCooldown, additionalRecovery);
                    break;
                case Scp096Role scp096:
                    scp096.EnrageCooldown = ReduceCooldown(scp096.EnrageCooldown, additionalRecovery);
                    scp096.ChargeCooldown = ReduceCooldown(scp096.ChargeCooldown, additionalRecovery);
                    break;
                case Scp939Role scp939:
                    scp939.MimicryCooldown = ReduceCooldown(scp939.MimicryCooldown, additionalRecovery);
                    scp939.AmnesticCloudCooldown =
                        ReduceCooldown(scp939.AmnesticCloudCooldown, additionalRecovery);
                    break;
                case Scp079Role scp079:
                    scp079.BlackoutZoneCooldown =
                        ReduceCooldown(scp079.BlackoutZoneCooldown, additionalRecovery);
                    scp079.RoomLockdownCooldown =
                        ReduceCooldown(scp079.RoomLockdownCooldown, additionalRecovery);
                    break;
            }

            yield return Timing.WaitForOneFrame;
        }
    }

    private void OnPinging(Exiled.Events.EventArgs.Scp079.PingingEventArgs ev)
    {
        if (ev.Player != Owner || !IsPrimaryInstance())
            return;

        Timing.CallDelayed(Timing.WaitForOneFrame, () =>
        {
            if (Owner.Role is not Scp079Role scp079 || scp079.PingAbility == null || PingRateLimiterField == null)
                return;

            PingRateLimiterField.SetValue(
                scp079.PingAbility,
                new RateLimiter(PingCooldown * GetCooldownMultiplier()));
        });
    }

    private bool IsPrimaryInstance()
    {
        return ReferenceEquals(Owner.GetAbility(AbilityType.NORMAL_SCPS_HASTE), this);
    }

    private float GetCooldownReduction()
    {
        int abilityCount = Owner.GetAbilities().Count(ability =>
            ability.Data.AbilityType == AbilityType.NORMAL_SCPS_HASTE);
        return Math.Min(abilityCount * ReductionPerAbility, MaximumReduction);
    }

    private float GetCooldownMultiplier()
    {
        return 1f - GetCooldownReduction();
    }

    private static float ReduceCooldown(float cooldown, float amount)
    {
        return cooldown > 0f ? Math.Max(0f, cooldown - amount) : cooldown;
    }
}

public class Scp049Patches
{
    private const float Scp049ResurrectDuration = 7;

    public static void Scp049Postfix(ref float __result)
    {
        try
        {
            __result = Mathf.Max(0.2f, Scp049ResurrectDuration - Haste.Count * .8f);
        }
        catch (Exception e)
        {
            Log.Error($"[KoreanSpeed/Scp049Postfix] Exception: {e}");
        }
    }
}