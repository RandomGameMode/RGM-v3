using Exiled.Events.EventArgs.Player;
using RGM.Patches;

namespace RGM.Modes
{
    [Mode(ModeCategory.Public, ModeInfo.Plus, ModeType.JohnWick)]
    public class JohnWick : Mode
    {
        public override string Name => "존 윅";
        public override string Description => "권총류 무기의 데미지가 일정 배율로 상승합니다.";
        public override string Detail =>
"""
COM-15 -> 770% 증가
COM-18 -> 280% 증가
COM-45 -> 170% 증가
.44 리볼버 -> 80% 증가

* 게임 시작 14분 뒤 <color=red>자동핵</color>이 작동됩니다.
""";
        public override string Color => "2EFEF7";

        public static JohnWick Instance;
        
        private readonly AutoWarhead _autoWarhead = new(14, 1);

        public override void OnEnabled()
        {
            Exiled.Events.Handlers.Player.Hurting += OnHurting;
            _autoWarhead.RunCoroutine();
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Player.Hurting -= OnHurting;
            _autoWarhead.KillCoroutine();
        }

        private void OnHurting(HurtingEventArgs ev)
        {
            if (ev.Attacker == null) return;
            ev.Amount *= ev.Attacker.CurrentItem.Type switch
            {
                ItemType.GunCOM15 => 8.7f,
                ItemType.GunCOM18 => 3.8f,
                ItemType.GunCom45 => 2.7f,
                ItemType.GunRevolver => 1.8f,
                _ => 1f
            };
        }
    }
}
