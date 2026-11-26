using Exiled.Events.EventArgs.Player;

namespace RGM.Modes.Lock.TFT.Euclid.Human;

[TFTAbility("반창고", "체력이 절반 이하로 줄어들었을 경우 180만큼 즉시 회복합니다. (최대 체력 무시)", TFTAbilityLevel.Euclid, TFTAbilityCategory.Human, TFTAbilityPoint.Continuous, TFTAbilityType.AdhesivePlaster, "😙")]
public class AdhesivePlaster : TFTAbility
{
    public override void OnEnabled()
    {
        Exiled.Events.Handlers.Player.Hurting += OnHurting;
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Player.Hurting -= OnHurting;
    }

    public void OnHurting(HurtingEventArgs ev)
    {
        if (ev.Player != Owner)
            return;

        if (ev.Player.Health <= ev.Player.MaxHealth / 2)
        {
            ev.Player.Health += 180;

            Data.Description = "체력이 절반 이하로 줄어들었을 경우 180만큼 즉시 회복합니다. (사용 완료)";

            OnDisabled();
        }
    }
}
