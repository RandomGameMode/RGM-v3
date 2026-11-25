namespace DAONTFT.Core.TFT.Euclid.Human;

[TFTAbility("초재생", "안티 콜라를 2개 획득합니다.", TFTAbilityLevel.Euclid, TFTAbilityCategory.Human, TFTAbilityPoint.Once, TFTAbilityType.AntiScp207, "🍹")]
public class AntiScp207 : TFTAbility
{
    public override void OnEnabled()
    {
        Owner.AddItem(ItemType.AntiSCP207, 2);
    }
}