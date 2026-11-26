using Exiled.API.Extensions;
using MEC;
using System.Collections.Generic;

namespace RGM.Modes.Lock.TFT.Keter.Human;

[TFTAbility("무기고+", "30초마다 고가치 아이템을 얻습니다.", 
    TFTAbilityLevel.Keter, TFTAbilityCategory.Human, TFTAbilityPoint.Once, TFTAbilityType.Armory2, "📦")]
public class Armory2 : TFTAbility
{
    private readonly List<ItemType> _highvalueitems =
    [
        ItemType.ParticleDisruptor,
        ItemType.Jailbird,
        ItemType.MicroHID,
        ItemType.SCP1509,
        ItemType.SCP1853,
        ItemType.AntiSCP207,
        ItemType.SCP268,
        ItemType.SCP500,
        ItemType.KeycardO5,
        ItemType.SCP1344,
        ItemType.GunLogicer,
        ItemType.GunFRMG0,
        ItemType.ArmorHeavy
    ];   
    
    private CoroutineHandle _randomitem;
    
    public override void OnEnabled()
    {
        _randomitem = Timing.RunCoroutine(RandomItem());
    }

    public override void OnDisabled()
    {
        Timing.KillCoroutines(_randomitem);
    }
    
    private IEnumerator<float> RandomItem()
    {
        while (Owner.IsAlive)
        {
            Owner.AddItem(_highvalueitems.GetRandomValue());
            for (int i = 30; i > 0; i--)
            {
                Data.Description = $"다음 아이템 획득까지: {i}초";
                yield return Timing.WaitForSeconds(1f);
            }
        }
    }
}
