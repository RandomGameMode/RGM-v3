using Exiled.API.Features.Items;

namespace RGM.Modes.Abilities.Rare;

[Ability("트리플악셀", "COM-45와 여분의 탄약을 지급받습니다.",
    AbilityCategory.Rare, AbilityType.RARE_TRIPLEAXEL)]
public class TripleAxel : Ability
{
    public override void OnEnabled()
    {
        Owner.AddItem(ItemType.GunCom45);
        Owner.AddItem(ItemType.Ammo9x19, 8);
    }
}
