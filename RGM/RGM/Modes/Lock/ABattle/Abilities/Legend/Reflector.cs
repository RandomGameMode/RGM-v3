using System;
using Random = UnityEngine.Random;

namespace RGM.Modes.Abilities.Legend;

[Ability("반사경", "능력을 획득하면 50% 확률로 동일한 능력을 하나 더 얻습니다. (최대 3회 연쇄 가능.)",
    AbilityCategory.Legend, AbilityType.LEGEND_REFLECTOR, RoleAbility.None, true)]
public class Reflector : Ability
{
    private const byte MaxChainCount = 3;
    private const byte ReflectionChance = 50;

    public override void OnEnabled() => ABattle.Instance.AddingAbility += OnAddingAbility;

    public override void OnDisabled() => ABattle.Instance.AddingAbility -= OnAddingAbility;

    private void OnAddingAbility(AddingAbilityEventArgs ev)
    {
        if (!ev.IsAllowed || !ev.AllowReflector || ev.Player != Owner || ev.ReflectorChain >= MaxChainCount)
            return;

        if (ABattle.Instance.Abilities[ev.AbilityType].Category == AbilityCategory.Ancient ||
            ABattle.Instance.Abilities[ev.AbilityType].Category == AbilityCategory.Synergy ||
            Convert.ToByte(Random.Range(1, 101)) > ReflectionChance)
            return;

        ABattle.Instance.AddAbility(
            ev.Player,
            ev.AbilityType,
            reflectorChain: ev.ReflectorChain + 1,
            allowReflector: ev.AllowReflector,
            extraReflectorChain: ev.ExtraReflectorChain);
    }
}