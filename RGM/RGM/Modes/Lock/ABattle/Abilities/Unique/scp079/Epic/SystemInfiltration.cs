using MEC;
using RGM.API.Features;
using UnityEngine;

namespace RGM.Modes.Abilities.Unique.Scp079.Epic;


[Ability("시스템 침투", "35% 확률로 [워크스테이션 업그레이드]추가 모드를 추가합니다.",
    AbilityCategory.Epic, AbilityType.EPIC_SCP079_SystemInfiltration, RoleAbility.Scp079)]
public class SystemInfiltration : Ability
{
    private readonly ABattle _instance = ABattle.Instance;
    
    public override void OnEnabled()
    {
        TryAddExtraMode();
    }
    
    public void AllPlayerBroadcast(string message)
    {
        foreach (var p in PlayerManager.List)
        {
                p.AddBroadcast(10, message);
                p.SendConsoleMessage("\n" + message, "white");
        }
    }

    private void TryAddExtraMode()
    {
        Owner.AddHint("침투","시스템 침투 시도 중...");
        Timing.CallDelayed(3.5f, () =>
        {
            if (Owner.IsAlive)
            {
                if (Random.Range(1, 101) <= 35)
                { 
                    string extraMode = _instance.PickExtraMode();
                    if (extraMode != null)
                    {
                        Owner.AddHint("침투", $"시스템 침투에 성공하여 <b>{extraMode}</b> 모드가 추가되었습니다!");
                        Owner.AddAbility(AbilityType.DUMMY_INFILTRATIONSUCCESS);

                        foreach (var player in PlayerManager.List)
                        {

                            extraMode = $"\n<size=25><b><color=#fecdcd>{extraMode}</color></b></size>\n<size=20>{ABattle.ExtraModes[extraMode]}</size>";
                            
                            player.AddBroadcast(10, extraMode);
                            player.SendConsoleMessage("\n" + extraMode, "white");
                        }
                    }
                    else
                    {
                        Owner.AddHint("침투", "시스템 침투에 성공했으나, 더 이상 추가할 모드가 없습니다.");
                        Owner.AddAbility(AbilityType.DUMMY_INFILTRATIONSUCCESS);
                    }
                }
                else
                {
                    Owner.AddHint("침투", "시스템 침투에 실패하였습니다.");
                    Owner.AddAbility(AbilityType.DUMMY_INFILTRATIONFAIL);
                }

            }

        });
    }
}