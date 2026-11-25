namespace DAONTFT.Core.TFT.Keter.All;

[TFTAbility("운동 · 통달", "HP +200️p, +50% 증가 (SCP 진영 6배수, 25%로 적용)", TFTAbilityLevel.Keter, TFTAbilityCategory.All, TFTAbilityPoint.Once, TFTAbilityType.AddHealth3, "❤️")]
public class AddHealth3 : TFTAbility
{
    private const float InitialHealth = 200f;
    public override void OnEnabled()
    {
        var health = Owner.IsScp ? InitialHealth * 6 : InitialHealth;
        var bonus = Owner.IsScp ? 1.25f : 1.5f;
        
        Owner.MaxHealth = (Owner.MaxHealth + health) * bonus;
        Owner.Health = (Owner.Health + health) * bonus;
    }
}
