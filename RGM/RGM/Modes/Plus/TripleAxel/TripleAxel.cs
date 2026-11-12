using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using MEC;
using RGM.API.Features;
using Exiled.Events.EventArgs.Player;
using Exiled.API.Extensions;
using PlayerRoles;
using RGM.Patches;

namespace RGM.Modes;

[Mode(ModeCategory.Public, ModeInfo.Plus, ModeType.TripleAxel)]
public class TripleAxel : Mode
{
    public override string Name => "트리플악셀";
    public override string Description => "획득한 총기는 COM-45로 변환됩니다. 대신 데미지가 77%로 하향됩니다.";
    public override string Detail =>
        """
        총기를 습득하는 그 순간부터 COM-45로 변환됩니다.
        탄약을 습득하는 그 순간부터 9x19 탄약으로 변경됩니다.
        COM-45로 인한 데미지가 77%로 하향됩니다.

        해당 모드에서는 SCP-173이 등장하지 않으며, Flashlight 아이템이 기본 지급됩니다.

        * 게임 시작 10분 뒤 <color=red>자동핵</color>이 작동됩니다.
        """;
    public override string Color => "DF7401";

    public static TripleAxel Instance;

    private CoroutineHandle _onModeStarted;
    private readonly AutoWarhead _autoWarhead = new(10, 1);

    private static readonly List<RoleTypeId> ChangeRoles = Tools.EnumToList<RoleTypeId>()
        .Where(role => role.IsScp() && role is not RoleTypeId.Scp0492 and not RoleTypeId.Scp079)
        .ToList();
        
    private static readonly List<RoleTypeId> ScpRoles =
    [
        RoleTypeId.Scp049,
        RoleTypeId.Scp096,
        RoleTypeId.Scp106,
        RoleTypeId.Scp939,
        RoleTypeId.Scp3114
    ];
        
    public override void OnEnabled()
    {
        Exiled.Events.Handlers.Player.ItemAdded += OnItemAdded;
        Exiled.Events.Handlers.Player.Hurting += OnHurting;
        Exiled.Events.Handlers.Player.Spawned += OnSpawned;

        _onModeStarted = Timing.RunCoroutine(OnModeStarted());
        _autoWarhead.RunCoroutine();
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Player.ItemAdded -= OnItemAdded;
        Exiled.Events.Handlers.Player.Hurting -= OnHurting;
        Exiled.Events.Handlers.Player.Spawned -= OnSpawned;

        Timing.KillCoroutines(_onModeStarted);
        _autoWarhead.KillCoroutine();
    }
        
    private IEnumerator<float> OnModeStarted()
    {
        foreach (var player in PlayerManager.List)
        {
            foreach (var item in player.Items)
            {
                if (!item.IsFirearm || item.Type == ItemType.GunCom45) continue;
                player.RemoveItem(item);
                player.AddItem(ItemType.GunCom45);
            }

            Spawned(player);
        }

        yield break;
    }

    private void OnItemAdded(ItemAddedEventArgs ev)
    {
        if (!ev.Item.IsFirearm || ev.Item.Type == ItemType.GunCom45) return;
        ev.Player.RemoveItem(ev.Item);
        ev.Player.AddItem(ItemType.GunCom45);
    }

    private void OnHurting(HurtingEventArgs ev)
    {
        if (ev.Attacker?.CurrentItem?.Type == ItemType.GunCom45)
            ev.DamageHandler.Damage *= 0.77f;
    }

    private void OnSpawned(SpawnedEventArgs ev)
    {
        Spawned(ev.Player);
    }
        
    private void Spawned(Player player)
    {
        if (!player.IsAlive) return;

        RoleTypeId growthRole = ChangeRoles.GetRandomValue();
            
        if (player.Role.Type == RoleTypeId.Scp173)
            Timing.CallDelayed(Timing.WaitForOneFrame, () =>
            {
                SetRole(player, growthRole);
            });

        if (player.IsScpRole()) return;
        player.ClearAmmo();
        player.AddItem(ItemType.Flashlight);
        player.AddItem(ItemType.Ammo9x19, 30);
    }
        
    private static void SetRole(Player target, RoleTypeId growthRole)
    {
        if (target is not { IsAlive: true } || target.Role.Type != RoleTypeId.Scp0492)
            return;

        float previousHealth = target.Health;
        target.Role.Set(growthRole, RoleSpawnFlags.None);

        if (previousHealth > target.MaxHealth)
            target.Health = previousHealth;
    }
}