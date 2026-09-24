using RGM.API.Features;

namespace RGM.Modes.Abilities.Normal;

[Ability("운동", "30p 만큼 최대 체력을 추가합니다. (SCP는 5배의 보너스를 받습니다.)", AbilityCategory.Normal, AbilityType.NORMAL_WORKOUT)]
public class Workout : Ability
{
    private const float Health = 30;
    private float _additionHealth;

    public override void OnEnabled()
    {
        _additionHealth = Owner.IsScpRole() ? Health * 5 : Health;
        Owner.MaxHealth += _additionHealth;
        Owner.Health += _additionHealth;
    }
}