using System.Collections.Generic;
using System.Linq;
using MEC;
using Exiled.API.Features.Roles;
using Exiled.API.Enums;
using RGM.API.Features;
using Exiled.API.Extensions;
using Exiled.Events.EventArgs.Player;
using RGM.Patches;
using RGM.Variables;

namespace RGM.Modes
{
    [Mode(ModeCategory.OnlySub, ModeInfo.Plus, ModeType.Curse)]
    public class Curse : Mode
    {
        public override string Name => "저주";
        public override string Description => "관전자의 수에 비례해 능력치가 하락합니다.";

        public override string Detail =>
            """
            플레이어는 자신을 바라보는 <b>관전자</b>의 수를 확인할 수 있습니다.
            <b>관전자</b>의 수에 비례해 다음 능력치가 하락합니다.
            • 이동 속도
            • 점프력 하락
            • 받는 데미지 증가
            • (SCP-079) 전력 하락

            <b>관전자</b>의 수가 5명씩 넘어갈 때마다 특수한 효과가 적용됩니다.
            3명 이상 - SCP-939 안개 효과
            5명 이상 - 블라인드 효과
            8명 이상 - 아이템이 자꾸 버려짐
            12명 이상 - <b><color=#0BBDF9>S</color><color=#12B2F9>C</color><color=#19A7FA>P</color><color=#209CFA>-</color><color=#2792FB>2</color><color=#2E87FB>4</color><color=#357CFC>4</color> <color=#4367FD>효</color><color=#4A5CFE>과</color></b>
            16명 이상 - <color=#CCF2FF>섬</color><color=#D6DBF7>광</color> <color=#EAADE7>효</color><color=#F496DF>과</color>
            20명 이상 - <b><color=#FF1F1F>손</color> <color=#EB335F>절</color><color=#E13D7F>단</color> <color=#CD51BF>효</color><color=#C35BDF>과</color></b>

            * 게임 시작 10분 뒤 <color=red>자동핵</color>이 작동됩니다.
            """;

        public override string Color => "000000";
        public override string Suggester => "광대";

        public static Curse Instance;

        private CoroutineHandle _onModeStarted;
        private readonly AutoWarhead _autoWarhead = new(10, 1);

        public override void OnEnabled()
        {
            Variable.IsNonePlayerAllowed = false;

            Exiled.Events.Handlers.Player.Hurt += OnHurt;

            _onModeStarted = Timing.RunCoroutine(OnModeStarted());
            _autoWarhead.RunCoroutine();
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Player.Hurt -= OnHurt;

            Timing.KillCoroutines(_onModeStarted);
            _autoWarhead.KillCoroutine();
        }

        private IEnumerator<float> OnModeStarted()
        {
            while (true)
            {
                foreach (var player in PlayerManager.List.Where(x => x.IsAlive))
                {
                    var s = player.CurrentSpectatingPlayers.Count();
                    var t = "관전자들이 조용합니다. 누구에게도 원성을 사지 않으신 것 같군요!";

                    player.GetEffect(EffectType.Slowness).Intensity = (byte)(1.5 * s);
                    player.GetEffect(EffectType.HeavyFooted).Intensity = (byte)(1.5 * s);

                    if (player.Role is Scp079Role scp079)
                        scp079.Energy -= 0.15f * s;

                    if (s >= 3)
                    {
                        t = "소수의 관전자들이 당신을 알아봅니다.";
                        player.EnableEffect(EffectType.AmnesiaVision);
                    }
                    else
                    {
                        player.DisableEffect(EffectType.AmnesiaVision);
                    }

                    if (s >= 5)
                    {
                        t = "더 많은 관전자들이 당신을 싸늘하게 지켜봅니다.";
                        player.EnableEffect(EffectType.Blinded);
                    }
                    else
                    {
                        player.DisableEffect(EffectType.Blinded);
                    }

                    if (s >= 8)
                    {
                        t = "많은 관전자들이 당신을 원망하고 있습니다.";
                        if (UnityEngine.Random.Range(1, 11) == 1)
                        {
                            var item = player.Items.GetRandomValue(x => !x.IsAmmo);
                            player.CurrentItem = item;

                            Timing.CallDelayed(1, () =>
                            {
                                if (player.CurrentItem == item)
                                    player.DropItem(item);
                            });
                        }
                    }

                    if (s >= 12)
                    {
                        t = "다수의 관전자들이 당신을 향해 분노하고 있습니다.";
                        player.EnableEffect(EffectType.Hypothermia, 100);
                    }
                    else
                    {
                        player.DisableEffect(EffectType.Blinded);
                    }

                    if (s >= 16)
                    {
                        t = "대다수의 관전자들이 당신의 죽음을 바랍니다.";
                        player.EnableEffect(EffectType.Flashed, 1, 0.3f);
                    }
                    else
                    {
                        player.DisableEffect(EffectType.Flashed);
                    }

                    if (s >= 20)
                    {
                        t = "<b><color=red>모든 관전자들이 당신을 저주합니다.</color></b>";
                        player.EnableEffect(EffectType.SeveredHands);
                        player.EnableEffect(EffectType.SeveredEyes);
                    }
                    else
                    {
                        player.DisableEffect(EffectType.SeveredHands);
                        player.DisableEffect(EffectType.SeveredEyes);
                    }

                    player.AddHint("저주",
                        $"""
                         <size=20>현재 {s}명이 당신을 관전하고 있습니다.</size>
                         <size=25>{t}</size>
                         """, 1.2f);
                }

                yield return Timing.WaitForSeconds(1f);
            }
        }

        private void OnHurt(HurtEventArgs ev)
        {
            var spectatorCount = ev.Player.CurrentSpectatingPlayers.Count();
            var increaseRate = 1f + 0.01f * spectatorCount;

            ev.DamageHandler.Damage *= increaseRate;
        }
    }
}
