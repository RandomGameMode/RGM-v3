using Exiled.API.Features.Items;
using System.Collections.Generic;
using System.Linq;
using MEC;
using Exiled.API.Features.Roles;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using RGM.API.Features;

using PlayerRoles;
using RGM.Patches;
using RGM.Variables;

namespace RGM.Modes
{
    [Mode(ModeCategory.OnlySub, ModeInfo.Plus, ModeType.Blessing)]
    public class Blessing : Mode
    {
        public override string Name => "축복";
        public override string Description => "관전자의 수에 비례해 능력치가 상승합니다.";

        public override string Detail =>
            """
            플레이어는 자신을 바라보는 <b>관전자</b>의 수를 확인할 수 있습니다.
            <b>관전자</b>의 수에 비례해 다음 능력치가 상승합니다.
            • 이동 속도
            • 점프력 증가
            • 피해량 증가
            • 받는 피해량 감소
            • 초당 체력 회복
            • (SCP-079) 전력 회복

            <b>관전자</b>의 수가 5명씩 넘어갈 때마다 특수한 효과가 적용됩니다.
            3명 이상 - 스테미나 무제한
            5명 이상 - 바이패스 활성화
            8명 이상 - 유령화 효과
            12명 이상 - <color=#57F104>아</color><color=#5DEE03>이</color><color=#63EB03>템</color><color=#6AE803>이</color> <color=#76E202>지</color><color=#7DDF02>급</color><color=#83DD01>될</color> <color=#90D701>수</color> <color=#9DD100>있</color><color=#A3CE00>음</color>
            16명 이상 - <b><color=#A400F0>투</color><color=#B600EE>명</color> <color=#DA00EC>효</color><color=#EC00EB>과</color></b>

            * 게임 시작 10분 뒤 <color=red>자동핵</color>이 작동됩니다.
            """;

        public override string Color => "F6D8CE";

        public static Blessing Instance;

        private CoroutineHandle _onModeStarted;
        private readonly AutoWarhead _autoWarhead = new(10, 1);

        public override void OnEnabled()
        {
            Variable.IsNonePlayerAllowed = false;

            Exiled.Events.Handlers.Player.Hurting += OnHurting;

            _onModeStarted = Timing.RunCoroutine(OnModeStarted());
            _autoWarhead.RunCoroutine();
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Player.Hurting -= OnHurting;

            Timing.KillCoroutines(_onModeStarted);
            _autoWarhead.KillCoroutine();
        }

        private IEnumerator<float> OnModeStarted()
        {
            while (true)
            {
                foreach (var player in PlayerManager.List)
                    if (player.IsAlive)
                    {
                        var s = player.CurrentSpectatingPlayers.Count();
                        var t = "관전자들이 조용합니다. 박진감 넘치는 플레이를 보여줘야 할 것 같은데요..";

                        player.GetEffect(EffectType.MovementBoost).Intensity = (byte)(2.5 * s);
                        player.GetEffect(EffectType.DamageReduction).Intensity = (byte)(2.5 * s);
                        player.GetEffect(EffectType.Lightweight).Intensity = (byte)(2.5 * s);
                        player.Heal(0.35f * s);

                        if (player.Role is Scp079Role scp079)
                            scp079.Energy += 0.35f * s;

                        if (s >= 3)
                        {
                            t = "소수의 관전자들이 당신을 지켜봅니다.";
                            player.IsUsingStamina = false;
                        }
                        else
                        {
                            player.IsUsingStamina = true;
                        }

                        if (s >= 5)
                        {
                            t = "더 많은 관전자들이 당신을 응원합니다.";
                            player.IsBypassModeEnabled = true;
                        }
                        else
                        {
                            player.IsBypassModeEnabled = false;
                        }

                        if (s >= 8)
                        {
                            t = "많은 관전자들이 당신을 지지합니다.";
                            player.EnableEffect(EffectType.Ghostly, 1, 1.2f);
                        }

                        if (s >= 12)
                        {
                            t = "다수의 관전자들이 당신을 후원합니다.";
                            if (UnityEngine.Random.Range(1, 51) == 1)
                            {
                                player.AddItem(Tools.EnumToList<ItemType>().GetRandomValue());
                            }
                        }

                        if (s >= 16)
                        {
                            t = "<b><color=#bbebe7>모든 관전자들이 당신의 앞길을 축복합니다.</color></b>";
                            player.EnableEffect(EffectType.Invisible, 1, 1.2f);
                        }

                        player.AddHint("축복",
                            $"""
                             <size=20>현재 {s}명이 당신을 관전하고 있습니다.</size>
                             <size=25>{t}</size>
                             """, 1.2f);
                    }

                yield return Timing.WaitForSeconds(1f);
            }
        }

        private void OnHurting(Exiled.Events.EventArgs.Player.HurtingEventArgs ev)
        {
            if (ev.Attacker.Role.Type == RoleTypeId.Scp173)
                return;

            if (ev.Attacker == null) return;
            var s = ev.Attacker.CurrentSpectatingPlayers.Count();
            ev.DamageHandler.Damage *= 1f + 0.2f * s;
        }
    }
}
