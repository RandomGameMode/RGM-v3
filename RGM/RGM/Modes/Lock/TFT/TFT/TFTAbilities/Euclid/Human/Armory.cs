namespace RGM.Modes.Lock.TFT.Euclid.Human;

[TFTAbility("무기 수집가", "SCP-127, SCP-1509를 획득합니다.", TFTAbilityLevel.Euclid, TFTAbilityCategory.Human, TFTAbilityPoint.Once, TFTAbilityType.Armory1, "📦")]
public class Armory1 : TFTAbility
{
    public override void OnEnabled()
    {
        Owner.AddItem(ItemType.GunSCP127);
        Owner.AddItem(ItemType.SCP1509);
    }
}
