namespace RGM.Modes.Lock.TFT.Euclid.Human;

[TFTAbility("침입", "SCP-268을 획득합니다.", TFTAbilityLevel.Euclid, TFTAbilityCategory.Human, TFTAbilityPoint.Once, TFTAbilityType.Breach, "🍹")]

public class Breach : TFTAbility
{
    public override void OnEnabled()
    {
        Owner.AddItem(ItemType.SCP268);
    }
}