using System;
using System.Linq;
using AdminToys;
using Exiled.API.Features;
using MEC;
using ProjectMER.Features;
using ProjectMER.Features.Objects;
using UnityEngine;

namespace RGM.Modes.Abilities.Rare;

[Ability("운디네", "청량한 바다의 기운! 불, 흙, 바람의 정령을 모으면..?", 
    AbilityCategory.Rare, AbilityType.RARE_UNDINE)]
public class Undine : Ability
{
    public override void OnEnabled()
    {
        Light(Owner, Color.blue);
    }
    
    private static void Light(Player player, Color color)
    {
        try
        {
            SchematicObject schematic = ObjectSpawner.SpawnSchematic("Light", Vector3.zero);
            LightSourceToy light = schematic.GetComponentsInChildren<LightSourceToy>().First();

            schematic.transform.parent = player.Transform;
            schematic.transform.localPosition = Vector3.zero;

            light.NetworkLightColor = color;
            light.NetworkLightRange = 50;
            light.NetworkLightIntensity = 8;

            Timing.CallDelayed(5, schematic.Destroy);
        }
        catch (NullReferenceException)
        {
            Log.Warn("Failure to fetch object 'light'.");
        }
    }
}
