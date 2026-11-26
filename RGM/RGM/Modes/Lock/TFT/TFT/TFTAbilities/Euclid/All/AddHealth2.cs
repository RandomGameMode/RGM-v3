namespace RGM.Modes.Lock.TFT.Euclid.All;

[TFTAbility("운동 · 숙련", "최대 체력 + 체력 -> +100❤️ (SCP x6)", 
    TFTAbilityLevel.Euclid, TFTAbilityCategory.All, TFTAbilityPoint.Once, TFTAbilityType.AddHealth2, "❤️")]
public class AddHealth2 : TFTAbility
{
    const float InitialHealth = 100f;
    public override void OnEnabled()
    {
        var health = Owner.IsScp ? InitialHealth * 6 : InitialHealth;
        Owner.MaxHealth += health;
        Owner.Health += health;
    }

    public override void OnDisabled()
    {
    }
}
