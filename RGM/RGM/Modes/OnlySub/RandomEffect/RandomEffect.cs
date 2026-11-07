using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using MEC;
using RGM.API.Features;
using System.Collections.Generic;
using System.Linq;

namespace RGM.Modes
{
    [Mode(ModeCategory.OnlySub, ModeInfo.Plus, ModeType.RandomEffect)]
    internal class RandomEffect : Mode
    {
        public override string Name => "랜덤효과";
        public override string Description => "영구적인 랜덤한 효과를 얻습니다.";

        public override string Detail =>
            """
            효과의 종류와 세기는 랜덤입니다.
            """;

        public override string Color => "BFFF00";
        public override string Suggester => "몬키키";

        public static RandomEffect Instance;

        private readonly List<EffectType> _ignoredEffect =
        [
            EffectType.PocketCorroding,
            EffectType.PitDeath,
            EffectType.CardiacArrest,
            EffectType.Poisoned,
            EffectType.SpawnProtected,
            EffectType.Ensnared,
            EffectType.Flashed,
            EffectType.SeveredHands
        ];


        private List<EffectType> GoodEffect = [];

        private List<EffectType> BadEffect =
        [
            EffectType.AmnesiaItems,
            EffectType.AmnesiaVision
        ];

        private List<EffectType> HalloweenEffect =
        [
            EffectType.Marshmallow,
            EffectType.Metal,
            EffectType.OrangeCandy,
            EffectType.OrangeWitness,
            EffectType.Prismatic, //?
            EffectType.SlowMetabolism, //?
            EffectType.Spicy,
            EffectType.SugarCrave,
            EffectType.SugarRush,
            EffectType.SugarHigh, //SugarHigh랑 SugarCrave 중에 보라 사탕이랑 초록사탕인 것 같은데 모르겠음 <- Ans. 초록사탕임.
            EffectType.TemporaryBypass, //?
            EffectType.TraumatizedByEvil, //?
            EffectType.WhiteCandy
        ];

        private List<EffectType> ChristmasEffect =
        [
            EffectType.BecomingFlamingo, //??
            EffectType.Scp559,
            EffectType.Scp956Target, //??
            EffectType.Snowed,
            EffectType.SugarCrave
        ];

        private List<EffectType> AprilFoolsEffect =
        [
            EffectType.BecomingFlamingo, //??
            EffectType.Scp559,
            EffectType.Scp956Target, //??
            EffectType.Snowed
        ];

        private CoroutineHandle _onModeStarted;

        public override void OnEnabled()
        {
            Exiled.Events.Handlers.Player.Spawned += OnSpawned;

            _onModeStarted = Timing.RunCoroutine(OnModeStarted());
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Player.Spawned -= OnSpawned;

            Timing.KillCoroutines(_onModeStarted);
        }

        private IEnumerator<float> OnModeStarted()
        {
            foreach (var player in PlayerManager.List) Spawned(player);

            yield break;
        }

        private void OnSpawned(SpawnedEventArgs ev)
        {
            if (ev.Player.IsAlive)
                Spawned(ev.Player);
        }

        private void Spawned(Player player)
        {
            var effects = Tools.EnumToList<EffectType>().Where(x => !_ignoredEffect.Contains(x)).ToList();

            var effect = effects.GetRandomValue();
            var intensity = (byte)UnityEngine.Random.Range(1,
                UnityEngine.Random.Range(12,
                    UnityEngine.Random.Range(48, UnityEngine.Random.Range(64, UnityEngine.Random.Range(100, 255)))));

            player.EnableEffect(effect, intensity);
            player.AddHint("랜덤효과 안내", $"<color=#D0FA58>{effect}</color> 효과가 {intensity}만큼 적용되는 중입니다.", 99999);
        }

        /*public bool AddEffect(Player player, EffectType effect, byte intensity)
        {
            return false;
        }*/
    }
}
