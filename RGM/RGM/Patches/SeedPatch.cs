using GameCore;
using HarmonyLib;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Handlers;
using MapGeneration;
using Mirror;
using UnityEngine;

namespace RGM.Patches;

/// <summary>
/// After sending the seed to all connected clients, we'll send it again as 0.
/// Since network messages are ordered and reliable, the first correct seed will generate the map and the second one will just hide it from clients.
/// Should be fast enough that players can't see the seed.
/// </summary>

[HarmonyPatch(typeof(SeedSynchronizer), nameof(SeedSynchronizer.Awake))]
public static class SeedSynchronizerAwakePatch
{
    public static int ActualSeed { get; private set; }
    
    [HarmonyPrefix]
    public static bool Prefix(SeedSynchronizer __instance)
    {
        ActualSeed = -1;
        SeedSynchronizer._singleton = __instance;
        NetworkServer.OnConnectedEvent += __instance.OnNewPlayerConnected;
        var num = ConfigFile.ServerConfig.GetInt("map_seed", -1);
        if (num < 1)
        {
            SeedSynchronizer.Seed = Random.Range(1, int.MaxValue);
            SeedSynchronizer.DebugInfo("Server has successfully generated a random seed: " + SeedSynchronizer.Seed, MessageImportance.Normal);
        }
        else
        {
            SeedSynchronizer.Seed = Mathf.Clamp(num, 1, int.MaxValue);
            SeedSynchronizer.DebugInfo("Server has successfully loaded a seed from config: " + SeedSynchronizer.Seed, MessageImportance.Normal);
        }

        var ev = new MapGeneratingEventArgs(SeedSynchronizer.Seed);
        ServerEvents.OnMapGenerating(ev);
        if (!ev.IsAllowed || ev.Seed == -1)
        {
            SeedSynchronizer.DebugInfo("Map generation cancelled by a plugin.", MessageImportance.Normal);
            SeedSynchronizer.Seed = -1;
        }
        else
        {
            SeedSynchronizer.Seed = ev.Seed;
            ActualSeed = ev.Seed;
        }

        NetworkServer.SendToAll(new SeedSynchronizer.SeedMessage
        {
            Value = SeedSynchronizer.Seed
        });
        
        __instance.GenerateLevel(SeedSynchronizer.Seed != -1);
        
        // Send a fake seed so all clients don't get to see it post generation.
        NetworkServer.SendToAll(new SeedSynchronizer.SeedMessage
        {
            Value = 0
        });

        return false;
    }
}

/// <summary>
/// Same thing as the above, but for new connections.
/// </summary>
[HarmonyPatch(typeof(SeedSynchronizer), nameof(SeedSynchronizer.OnNewPlayerConnected))]
public static class SeedSynchronizerNewPlayerPatch
{
    [HarmonyPrefix]
    public static bool Prefix(SeedSynchronizer __instance, NetworkConnectionToClient connectionToClient)
    {
        if (connectionToClient == NetworkClient.connection)
        {
            return false;
        }
        
        connectionToClient.Send(new SeedSynchronizer.SeedMessage
        {
            Value = SeedSynchronizerAwakePatch.ActualSeed
        });
        
        connectionToClient.Send(new SeedSynchronizer.SeedMessage
        {
            Value = 0
        });

        return false;
    }
}