namespace RGM.Modes.Lock.TFT.Euclid.Human;

[TFTAbility("스피드왜건", "콜라를 2개 획득합니다.", TFTAbilityLevel.Euclid, TFTAbilityCategory.Human, TFTAbilityPoint.Once, TFTAbilityType.Scp207, "🍹")]
public class Scp207 : TFTAbility
{
    public override void OnEnabled()
    {
        Owner.AddItem(ItemType.SCP1853, 2);
    }
}
