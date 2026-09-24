using System;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Warhead;
using MEC;
using PlayerRoles;
using RGM.API.DataBases;
using Random = UnityEngine.Random;

namespace RGM.Modes.Abilities.Epic;

[Ability("매드 사이언티스트", 
    "사망 시 10초 후 부활하며 랜덤한 능력 10개를 부여합니다.(별도 등급 확률 적용)", 
    AbilityCategory.Epic, AbilityType.EPIC_MADSCIENTIST)]
public class MadScientist : Ability
{
    public override void OnEnabled()
    {
        Exiled.Events.Handlers.Player.Died += OnDied;
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Player.Died -= OnDied;
    }

    private void OnDied(DiedEventArgs ev)
    {
        Timing.CallDelayed(Timing.WaitForOneFrame, () =>
        {
            if (ev.Player != Owner ||
                !ev.Player.IsDead ||
                Datas.BlockDamageTypes.Contains(ev.DamageHandler.Type) || 
                Warhead.IsDetonated)
                return;

            Timing.CallDelayed(10, () =>
            {
                if (ev.Player.IsDead) Owner.Role.Set(ev.TargetOldRole, RoleSpawnFlags.None);

                Timing.CallDelayed(Timing.WaitForOneFrame, () =>
                {
                    for (int i = 0; i < 10; i++)
                    {
                        try
                        {
                            var rand = Convert.ToInt16(Random.Range(1, 501));
                            switch (rand)
                            {
                                case 1: // 0.20%
                                    Owner.AddAbility(ABattle.Instance.GetRandomAbilities(Owner, AbilityCategory.Mythic, 1)[0]);
                                    break;
                    
                                case <= 9: // 1.80%
                                    Owner.AddAbility(ABattle.Instance.GetRandomAbilities(Owner, AbilityCategory.Legend, 1)[0]);
                                    break;
                    
                                case <= 75: // 15.0%
                                    Owner.AddAbility(ABattle.Instance.GetRandomAbilities(Owner, AbilityCategory.Epic, 1)[0]);
                                    break;
                    
                                case <= 175: // 35.0%
                                    Owner.AddAbility(ABattle.Instance.GetRandomAbilities(Owner, AbilityCategory.Rare, 1)[0]);
                                    break;
                    
                                default: // 48.0%
                                    Owner.AddAbility(ABattle.Instance.GetRandomAbilities(Owner, AbilityCategory.Normal, 1)[0]);
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"Failed to add ability to Mad Scientist: {ex}");
                        }
                    }
                });
            });
        });
    }
}