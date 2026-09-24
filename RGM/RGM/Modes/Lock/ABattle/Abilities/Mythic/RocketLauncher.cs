using System;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using MEC;
using RGM.API.DataBases;
using RGM.API.Features;
using System.Collections.Generic;
using PlayerRoles;
using Random = UnityEngine.Random;

namespace RGM.Modes.Abilities.Mythic;

[Ability("로켓 런처", """
                  공격 시, 10% 확률로 상대방을 하늘로 승천시킬 수 있습니다!
                  <color=red>SCP</color>는 45% 확률로 적용되며, 특정 직업군은 무조건 승천시킵니다!
                  해당 공격은 『사망』 효과가 적용됩니다.
                  """,
    AbilityCategory.Mythic, AbilityType.MYTHIC_ROCKETLAUNCHER)]
public class RocketLauncher : Ability
{
    private readonly List<Player> _isInRocket = [];

    public override void OnEnabled()
    {
        Exiled.Events.Handlers.Player.Hurting += OnHurting;
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Player.Hurting -= OnHurting;
    }

    private void OnHurting(HurtingEventArgs ev)
    {
        if (ev.Attacker == null || 
            ev.Attacker != Owner || 
            !HitboxIdentity.IsEnemy(ev.Attacker.ReferenceHub, ev.Player.ReferenceHub))
            return;
        
        if (_isInRocket.Contains(ev.Player)) return;
        
        _isInRocket.Add(ev.Player);
        
        if (Convert.ToByte(Random.Range(1, 101)) <= GetPercent())
        {
            Tools.MessageTranslated("", $"{ev.Player.DisplayNickname}(<color={ev.Player.Role.Color.ToHex()}>{( Trans.Role[ev.Player.Role.Type])}</color>)(이)가 하늘로 승천했습니다.");
            Timing.RunCoroutine(Tools.DoRocket(ev.Attacker, ev.Player, 1f, isInstantKill:true));
        }
        
        Timing.CallDelayed(1, () =>
        {
            _isInRocket.Remove(ev.Player);
        });

        return;

        byte GetPercent()
        {
            if (ev.Attacker.IsScpRole())
                return 45;

            return Convert.ToByte(ev.Attacker.Role.Type == RoleTypeId.Tutorial ? 173 : 10);   
        }
    }
}