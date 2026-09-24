using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using HarmonyLib;
using PlayerRoles.PlayableScps.Scp939;

namespace RGM.Modes.Abilities.Unique.Scp939.Rare;

/*[Ability("연마", "SCP-939의 공격에 인원 비례 데미지 감소 패널티가 제거됩니다.",
    AbilityCategory.Rare, AbilityType.RARE_SCP939_SHARPNESS, RoleAbility.Scp939)]*/

public class Sharpness : Ability
{
    private const float ClawBaseDamage = 40f;

    private static readonly HashSet<ReferenceHub> ActiveOwners = new();
    private Harmony _harmony;

    public override void OnEnabled()
    {
        ActiveOwners.Add(Owner.ReferenceHub);
        _harmony = new Harmony("RGM.ABattle.Scp939.Sharpness");
        EnsurePatch();
    }

    public override void OnDisabled()
    {
        ActiveOwners.Remove(Owner.ReferenceHub);
        _harmony.UnpatchAll();
        _harmony = null;
    }

    private void EnsurePatch()
    {
        if (_harmony.GetPatchedMethods().Any()) return;
        
        try
        {
            _harmony.Patch(
                AccessTools.Method(typeof(Scp939ClawAbility), nameof(Scp939ClawAbility.DamagePlayers)),
                prefix: new HarmonyMethod(typeof(Sharpness), nameof(DamagePlayersPrefix)));
        }
        catch (Exception e)
        {
            Log.Error($"[Sharpness] Failed to patch SCP-939 claw damage: {e}");
        }
    }

    private static bool DamagePlayersPrefix(Scp939ClawAbility __instance)
    {
        if (!ActiveOwners.Contains(__instance.Owner))
            return true;

        foreach (ReferenceHub target in __instance.DetectedPlayers)
            __instance.DamagePlayer(target, ClawBaseDamage);

        return false;
    }
}