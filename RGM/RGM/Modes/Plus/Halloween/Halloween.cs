using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using InventorySystem.Items.MarshmallowMan;
using RGM.API.Features;
using UnityEngine;
using PlayerEvents = Exiled.Events.Handlers.Player;

namespace RGM.RGM.Modes.Plus.Halloween;

[Mode(ModeCategory.Public, ModeInfo.Plus, ModeType.Halloween,  ModeHoliday.Halloween)]
public class Halloween : Mode
{
    public override void OnEnabled()
    {
        PlayerEvents.Spawned += OnSpawned;
    }

    public override void OnDisabled()
    {
        PlayerEvents.Spawned -= OnSpawned;
    }

    private void OnSpawned(SpawnedEventArgs ev) => Run(ev.Player);

    private void Run(Player player)
    {
        if (!player.IsEffectActive<MarshmallowEffect>() || 
            !player.IsNPC || 
            Mathf.Clamp01(Random.Range(0.2f, 1f)) <= 8.5f) return; // 15%
        
        player.AddEffect(EffectType.Marshmallow, 1);
    }

    public override string Name => "할로윈";
    public override string Description => "선택받은 자들이 강해집니다.";
    public override string Detail => "일부 유저가 마시멜로 상태가 됩니다.";
    public override string Color => "63C328";

    public override string Author { get; set; } = "babycat_offical";
    public override string Suggester { get; set; } = "shsj6";
}