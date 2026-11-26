namespace RGM.Modes.Lock.TFT.Safe.All;

[TFTAbility("운동 · 입문", "최대 체력 + 체력 -> +40❤️ (SCP x8)", TFTAbilityLevel.Safe, TFTAbilityCategory.All, TFTAbilityPoint.Once, TFTAbilityType.AddHealth1, "❤️")]
public class AddHealth1 : TFTAbility
{
    const float InitialHealth = 40f;
    public override void OnEnabled()
    {
        var health = Owner.IsScp ? InitialHealth * 8 : InitialHealth;
        Owner.MaxHealth += health;
        Owner.Health += health;
    }
}
