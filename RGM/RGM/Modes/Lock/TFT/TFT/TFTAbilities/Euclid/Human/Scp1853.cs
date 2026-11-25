namespace DAONTFT.Core.TFT.Euclid.Human;

[TFTAbility("무기 전문가", "SCP-1853 을 2개 획득합니다.", TFTAbilityLevel.Euclid, TFTAbilityCategory.Human, TFTAbilityPoint.Once, TFTAbilityType.Scp1853, "🔫")]
public class Scp1853 : TFTAbility
{
    public override void OnEnabled()
    {
            Owner.AddItem(ItemType.SCP1853);
    }
}
