using System.Collections.Generic;
using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.Events.EventArgs.Player;
using MEC;
using PlayerRoles;
using RGM.API.Features;

namespace RGM.Modes.Abilities.Legend;

[Ability("뱀의 손 무전기", "무전기를 든 상태로 우클릭하면 뱀의 손 지원을 부르며, 자신도 뱀의 손 소속이 됩니다. 이후 럭키비키를 획득합니다.",
    AbilityCategory.Legend, AbilityType.LEGEND_SNAKEHANDRADIO)]
public class SnakeHandRadio : Ability
{
    private ushort _callSnakeHandsSerial;

    public override void OnEnabled()
    {
        Item SH = Owner.AddItem(ItemType.Radio);
        _callSnakeHandsSerial = SH.Serial;

        Exiled.Events.Handlers.Player.ChangedItem += OnChangedItem;
        Exiled.Events.Handlers.Player.TogglingRadio += OnTogglingRadio;
    }
    private void OnChangedItem(ChangedItemEventArgs ev)
    {
        if (ev.Item?.Serial != _callSnakeHandsSerial)
            return;
        
        ev.Player.AddHint("뱀의 손 무전기", $"<b><color={ABattle.RatingColor["전설"]}>뱀의 손 무전기</color></b> 능력이 있는 <b>무전기</b>입니다!");
    }

    private void OnTogglingRadio(TogglingRadioEventArgs ev)
    {
        if (_callSnakeHandsSerial != ev.Item.Serial) return;
        
        List<Player> deadPlayers = PlayerManager.List.Where(x => x.IsDead).ToList();
        Tools.CallSnakeHand(ev.Player, deadPlayers);

        Timing.CallDelayed(1, () =>
        {
            ev.Player.Position = deadPlayers.FirstOrDefault()!.Position;
        });
        
        ev.Item.Destroy();
        ev.Player.AddAbility(AbilityType.DUMMY_USEDSNAKEHANDRADIO);
        ev.Player.RemoveAbility(this);

        if (ev.Player.Role.Type != RoleTypeId.Tutorial)
            ev.Player.Role.Set(RoleTypeId.Tutorial, SpawnReason.ForceClass, RoleSpawnFlags.None);

        ev.Player.AddAbility(AbilityType.EPIC_LUCKYVIKEY);
    }
}
