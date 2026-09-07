using DiscordInteraction.Discord;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using MEC;
using RGM.API.Features;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RGM.Modes
{
    [Mode(ModeCategory.OnlySub, ModeInfo.Plus, ModeType.RandomEffect)]
    class RandomEffect : Mode
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

        /*public static readonly Dictionary<string, string> ExtraModes = new()
        {
            { "맛있는 스튜", "긍정적 효과만 적용됩니다."},
            { "맛없는 스튜", "부정적 효과만 적용됩니다."},
            { "이상한 스튜", "효과가 2개 적용됩니다." },
            { "기묘한 스튜", "효과가 3개 적용됩니다."},
            { "섞인 스튜", "1분마다 효과가 추가됩니다."},
            { "평범한 스튜", "추가된 것 없이 효과 1개만 적용됩니다."},
            { "식은 스튜", "효과가 1만큼만 적용됩니다."},
            { "넘치는 스튜", "효과가 255만큼 적용됩니다."}
        };

        public string PickExtraMode(List<string> exceptModes = null, bool allowBasic = true)
        {
            exceptModes ??= new List<string>();

            var candidates = ExtraModes.Keys
                .Where(x => x != "평범한 스튜" && !exceptModes.Contains(x) && !CurrentExtraModes.Contains(x))
                .ToList();

            string extraMode;

            if (allowBasic && Random.Range(1, 7) == 1)
            {
                extraMode = "평범한 스튜";
            }
            else if (candidates.Count == 0)
            {
                if (!allowBasic)
                    return null;

                extraMode = "평범한 스튜";
            }
            else
            {
                extraMode = candidates.GetRandomValue();
            }

            bool newlyAdded = false;

            if (extraMode == "평범한 스튜")
            {
                if (CurrentExtraModes.Count == 0)
                {
                    CurrentExtraModes.Add("평범한 스튜");
                    newlyAdded = true;
                }
            }
            else if (!CurrentExtraModes.Contains(extraMode))
            {
                CurrentExtraModes.Remove("평범한 스튜");
                CurrentExtraModes.Add(extraMode);
                newlyAdded = true;
            }

            Webhook.Send($"추가 모드: {extraMode}");
            Log.Info($"추가 모드: {extraMode}");

            if (!newlyAdded)
                return extraMode;

            switch (extraMode)
            {
            }

            return extraMode;
        }

        public List<string> CurrentExtraModes = new();*/

        List<EffectType> ignoredEffect = new List<EffectType>
        {
            EffectType.PocketCorroding,
            EffectType.PitDeath,
            EffectType.CardiacArrest,
            EffectType.Poisoned,
            EffectType.SpawnProtected,
            EffectType.Ensnared,
            EffectType.Flashed,
            EffectType.SeveredHands
        };


        //뭔 효관지 모르는거: ?(뒤에 ??도 포함), 실제로 기능하는 효관지 모르는거: ?? 
        //그리고 특수한 경우(할로윈)에 적용되는 효과가 아닌 거 중에서 모르는 거는 중립에 떄려 박음
        /*List<EffectType> GoodEffect = new List<EffectType>
        {
            
        };

        List<EffectType> BadEffect = new List<EffectType>
        {
           EffectType.AmnesiaItems,
           EffectType.AmnesiaVision,
           EffectType.
        };

        List<EffectType> HalloweenEffect = new List<EffectType>
        {
            EffectType.Marshmallow,
            EffectType.Metal,
            EffectType.OrangeCandy,
            EffectType.OrangeWitness,
            EffectType.Prismatic, //?
            EffectType.SlowMetabolism, //?
            EffectType.Spicy,
            EffectType.SugarCrave,
            EffectType.SugarRush,
            EffectType.SugarHigh, //SugarHigh랑 SugarCrave 중에 보라 사탕이랑 초록사탕인 것 같은데 모르겠음
            EffectType.TemporaryBypass, //?
            EffectType.TraumatizedByEvil, //?
            EffectType.WhiteCandy
        };

        List<EffectType> ChristmasEffect = new List<EffectType>
        {
            EffectType.BecomingFlamingo,  //??
            EffectType.Scp559,
            EffectType.Scp956Target, //??
            EffectType.Snowed,
            EffectType.SugarCrave
        };

        List<EffectType> AprilFoolsEffect = new List<EffectType>
        {
            EffectType.BecomingFlamingo,  //??
            EffectType.Scp559,
            EffectType.Scp956Target, //??
            EffectType.Snowed
        };*/

        CoroutineHandle _onModeStarted;

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

        public IEnumerator<float> OnModeStarted()
        {
            foreach (var player in PlayerManager.List)
            {
                Spawned(player);
            }

            yield break;
        }

        public void OnSpawned(SpawnedEventArgs ev)
        {
            if (ev.Player.IsAlive)
                Spawned(ev.Player);
        }

        public void Spawned(Player player)
        {
            List<EffectType> effects = Tools.EnumToList<EffectType>().Where(x => !ignoredEffect.Contains(x)).ToList();

            EffectType Effect = effects.GetRandomValue();
            byte Intensity = (byte)UnityEngine.Random.Range(1, UnityEngine.Random.Range(12, UnityEngine.Random.Range(48, UnityEngine.Random.Range(64, UnityEngine.Random.Range(100, 255)))));

            player.EnableEffect(Effect, Intensity);
            player.AddHint("랜덤효과 안내", $"<color=#D0FA58>{Effect}</color> 효과가 {Intensity}만큼 적용되는 중입니다.", 99999);
        }

        /*public bool AddEffect(Player player, EffectType effect, byte intensity)
        {
            return false;
        }

        public IEnumerator<float> MixedStew() 
        {
            yield break;
        }*/
    }
}
