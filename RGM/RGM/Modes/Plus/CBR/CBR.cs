using System.Collections.Generic;
using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Features.Doors;
using Exiled.Events.EventArgs.Player;
using MEC;
using PlayerRoles;
using RGM.API.Features;
using UnityEngine;
using Player = Exiled.API.Features.Player;
using Round = Exiled.API.Features.Round;
using Server = Exiled.API.Features.Server;

namespace RGM.Modes;


[Mode(ModeCategory.Private, ModeInfo.Plus, ModeType.CBR)]
public class CBR : Mode
{
    private readonly float _damage = Random.Range(9f, 12.5f);
    
    public override void OnEnabled()
    {
        Round.IsLocked = true;
        Server.FriendlyFire = true;
        Start();
        
        Exiled.Events.Handlers.Player.Died += OnDied;
        Exiled.Events.Handlers.Player.Hurting += OnHurting;
        foreach (var door in Door.List.Where(x => x.IsCheckpoint)) door.Lock(DoorLockType.NoPower);
        
        if (PlayerManager.List.Count >= 2) return;
        Round.IsLocked = false;
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Player.Died -= OnDied;
        Exiled.Events.Handlers.Player.Hurting -= OnHurting;
    }

    private static void OnDied(DiedEventArgs _)
    {
        if (PlayerManager.List.Count >= 2) return;

        Timing.RunCoroutine(Tools.SetWinner([.. PlayerManager.List.Where(x => x.IsAlive && !x.IsNPC)], 15));
        Timing.CallDelayed(Timing.WaitForOneFrame * 15, () => { Round.IsLocked = false; });
    }

    private void OnHurting(HurtingEventArgs ev) => ev.DamageHandler.Damage = _damage;

    private static void Start()
    {
        foreach (var player in PlayerManager.List.Where(x => x.Role !=  RoleTypeId.ClassD)) Timing.RunCoroutine(AddItem(player));
        Timing.CallDelayed(Timing.WaitForOneFrame * 15, () => Server.ExecuteCommand("/decontamination f"));
    }

    private static IEnumerator<float> AddItem(Player pl)
    {
        pl.RoleManager.ServerSetRole(RoleTypeId.ClassD, RoleChangeReason.None);
        while (pl.Items.Count < 8) pl.AddItem(ItemType.Medkit);
        yield break;
    }
    
    public override string Name => "화생방";
    public override string Description => "혼돈의 군대식 가스실에서 생존해보세요!";
    public override string Detail => "구급상자 8개가 주어집니다. 어떻게든 생존하세요. 가스의 세기는 다를 것입니다.";
    public override string Color  => "FFF347";

    public override string Author { get; set; } = "babycat_offical";

    public override string Suggester { get; set; } = "GoldenPig1205";
}