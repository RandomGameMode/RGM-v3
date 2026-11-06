using System;
using System.Collections.Generic;
using Exiled.API.Features;
using UnityEngine;
using MEC;
using HarmonyLib;
using PlayerRoles;
using RGM.API.Features;
using RGM.API.DataBases;
using Exiled.API.Enums;
using RGM.Patches;

namespace RGM.Modes
{
    [Mode(ModeCategory.Public, ModeInfo.Plus, ModeType.FriendlyFire)]
    class FriendlyFire : Mode
    {
        public override string Name => "어제의 동지는 오늘의 적";
        public override string Description => "팀킬이 가능합니다, 이젠 아무도 믿지 마세요.";
        public override string Detail =>
"""
<color=#81F781>인간</color>이던 <color=red>SCP</color>던 <b><color=#FFBF00>팀킬</color>이 허용</b>됩니다.
팀킬이 허용되므로, 티밍이 자유로워집니다. (이 모드는 팀킬, 티밍, 약한 저격, 약한 친목으로 제재되지 않습니다.)

인간 진영의 경우 [ALT]키를 눌러 다른 플레이어에게 주먹을 날릴 수 있습니다. (데미지: 25)
SCP 진영의 경우 기본 공격으로 다른 플레이어를 공격할 수 있게 되지만 몇몇 SCP는 특수한 규칙을 따릅니다.
- <color=red>SCP-173</color> 순간이동: SCP에게 순간이동할 때, 해당 SCP를 쳐다보고 있어야 목을 꺾을 수 있습니다.
- <color=red>SCP-173</color> 공격: 쳐다보고 있지 않을 때, [ALT]키를 눌러 죽일 수 있습니다.
- <color=red>SCP-939</color> 런지: SCP에게 런지를 시도할 때, 해당 SCP를 쳐다보고 있어야 공격이 가능합니다.
- <color=red>SCP-106</color> 공격: [ALT]키를 눌러 공격을 시도할 수 있습니다.
- <color=red>SCP-3114</color> 목 조르기: 불가

<b><color=#AEFF00>개</color><color=#B7E200>발</color><color=#C0C600>자</color> <color=#D28D00>추</color><color=#DB7100>천</color> <color=#ED3800>모</color><color=#F61C00>드</color></b> 🥵🥵
<size=15>내면 속 악마의 속삭임에 몸을 맡기세요</size>

* 게임 시작 10분 뒤 <color=red>자동핵</color>이 작동됩니다.
""";
        public override string Color => "F78181";

        public static FriendlyFire Instance;

        private const float Meleedamage = 25f;
        private readonly List<Player> _humanMeleeCooldown = [];
        private int _scp106AttackTeamCoolDown;
        private readonly List<Player> _scp106Stacks = [];

        private CoroutineHandle _onModeStarted;
        private readonly AutoWarhead _autoWarhead = new(10, 1);

        private Harmony harmony;

        public override void OnEnabled()
        {
            Exiled.Events.Handlers.Player.Spawned += OnSpawned;
            Exiled.Events.Handlers.Player.TogglingNoClip += OnTogglingNoClip;

            Exiled.Events.Handlers.Scp173.Blinking += OnBlinking;

            Exiled.Events.Handlers.Scp939.Lunging += OnLunging;

            _onModeStarted = Timing.RunCoroutine(OnModeStarted());
            _autoWarhead.RunCoroutine();

            harmony = new Harmony($"FriendlyFire - {DateTime.Now.Ticks}");
            harmony.Patch(AccessTools.Method(typeof(HitboxIdentity), nameof(HitboxIdentity.IsEnemy), [typeof(Team), typeof(Team)]), 
                postfix: new HarmonyMethod(AccessTools.Method(typeof(HitboxPatchPostfix), nameof(HitboxPatchPostfix.Postfix))));
        }
        
        public void OnEnabledForNoNuke()
        {
            Exiled.Events.Handlers.Player.Spawned += OnSpawned;
            Exiled.Events.Handlers.Player.TogglingNoClip += OnTogglingNoClip;

            Exiled.Events.Handlers.Scp173.Blinking += OnBlinking;

            Exiled.Events.Handlers.Scp939.Lunging += OnLunging;

            _onModeStarted = Timing.RunCoroutine(OnModeStarted());

            harmony = new Harmony($"FriendlyFire - {DateTime.Now.Ticks}");
            harmony.Patch(AccessTools.Method(typeof(HitboxIdentity), nameof(HitboxIdentity.IsEnemy), [typeof(Team), typeof(Team)]), 
                postfix: new HarmonyMethod(AccessTools.Method(typeof(HitboxPatchPostfix), nameof(HitboxPatchPostfix.Postfix))));
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Player.Spawned -= OnSpawned;
            Exiled.Events.Handlers.Player.TogglingNoClip -= OnTogglingNoClip;

            Exiled.Events.Handlers.Scp173.Blinking -= OnBlinking;

            Exiled.Events.Handlers.Scp939.Lunging -= OnLunging;

            Timing.KillCoroutines(_onModeStarted);
            _autoWarhead.KillCoroutine();

            harmony.UnpatchAll();
        }

        private IEnumerator<float> OnModeStarted()
        {
            foreach (var player in PlayerManager.List)
            {
                Spawned(player);
            }

            yield break;
        }

        private void OnSpawned(Exiled.Events.EventArgs.Player.SpawnedEventArgs ev)
        {
            Spawned(ev.Player);
        }

        private void Spawned(Player player)
        {
            if (new List<RoleTypeId> { RoleTypeId.Scp173, RoleTypeId.Scp106 }.Contains(player.Role.Type) || player.IsHuman)
                player.AddHint("어동오적", $"<size=20>[ALT]ㅣ같은 진영에게 피해를 입힐 수 있습니다.</size>", 10);

            else if (player.Role.Type == RoleTypeId.Scp939)
                player.AddHint("어동오적 939", $"<size=20>런지를 사용하는 도중 근접한 SCP를 쳐다보면 해당 개체에 피해를 입힐 수 있습니다.</size>", 10);
        }

        private IEnumerator<float> OnTogglingNoClip(Exiled.Events.EventArgs.Player.TogglingNoClipEventArgs ev)
        {
            if (ev.Player.IsHuman && !ev.Player.IsCuffed)
            {
                if (!ev.Player.TryGetLookPlayer(2f, out Player player, out RaycastHit? hit)) yield break;
                if (ev.Player == player || _humanMeleeCooldown.Contains(ev.Player)) yield break;

                float DamageCalcu(string pos)
                {
                    switch (pos)
                    {
                        case "Head":
                            return Meleedamage * 2f;

                        case "Chest":
                            return Meleedamage;

                        default:
                            return Meleedamage * 0.7f;
                    }
                }

                if (hit != null)
                {
                    float damage = DamageCalcu(hit.Value.transform.name);

                    ev.Player.ShowHitMarker(damage / Meleedamage);
                    player.Hit(ev.Player, damage);
                }

                ev.Player.Grab();

                _humanMeleeCooldown.Add(ev.Player);

                yield return Timing.WaitForSeconds(1);

                _humanMeleeCooldown.Remove(ev.Player);
            }
            else switch (ev.Player.Role.Type)
            {
                case RoleTypeId.Scp173:
                {
                    if (!ev.Player.TryGetLookPlayer(2f, out Player player, out RaycastHit? hit)) yield break;
                    if (ev.Player.Role is not Exiled.API.Features.Roles.Scp173Role { BlinkCooldown: 0f }) yield break;
                    if (ev.Player == player || !player.IsScpRole()) yield break;
                    Hitmarker.SendHitmarkerDirectly(ev.Player.ReferenceHub, 1.2f);
                    Server.ExecuteCommand($"/cassie <color=#ff0000>{(Trans.Role[ev.Player.Role.Type])}</color>이(가) <color=#ff0000>{(Trans.Role[player.Role.Type])}</color>의 뒷통수를 쳤습니다.");
                    player.Hit(ev.Player, player.MaxHealth);
                    break;
                }
                case RoleTypeId.Scp106:
                {
                    if (!ev.Player.TryGetLookPlayer(2f, out Player player, out RaycastHit? hit)) yield break;
                    if (ev.Player == player || !player.IsScpRole() || _scp106AttackTeamCoolDown > 0) yield break;
                    Hitmarker.SendHitmarkerDirectly(ev.Player.ReferenceHub, 0.9f);
                    player.Hurt(40, DamageType.Scp106);
                    _scp106AttackTeamCoolDown = 1;

                    if (_scp106Stacks.Contains(player))
                    {
                        player.EnableEffect(EffectType.PocketCorroding);

                        _scp106AttackTeamCoolDown = 0;
                    }
                    else
                    {
                        _scp106Stacks.Add(player);

                        yield return Timing.WaitForSeconds(1);

                        _scp106AttackTeamCoolDown = 0;

                        yield return Timing.WaitForSeconds(9);

                        _scp106Stacks.Remove(player);
                    }

                    break;
                }
            }
        }

        private IEnumerator<float> OnBlinking(Exiled.Events.EventArgs.Scp173.BlinkingEventArgs ev)
        {
            yield return Timing.WaitForSeconds(0.02f);

            if (!ev.Player.TryGetLookPlayer(2f, out Player player, out RaycastHit? hit)) yield break;
            if (ev.Player == player || !player.IsScpRole()) yield break;
            Hitmarker.SendHitmarkerDirectly(ev.Player.ReferenceHub, 1.5f);
            Server.ExecuteCommand($"/cassie <color=#ff0000>{Trans.Role[ev.Player.Role.Type]}</color>이(가)  <color=#ff0000>{Trans.Role[player.Role.Type]}</color>의 뒷통수를 쳤습니다.");
            player.Hit(ev.Player, player.MaxHealth);
        }

        private IEnumerator<float> OnLunging(Exiled.Events.EventArgs.Scp939.LungingEventArgs ev)
        {
            for (float i = 0; i < 0.7f; i += 0.01f)
            {
                if (ev.Player.TryGetLookPlayer(2f, out Player player, out RaycastHit? hit))
                {
                    if (ev.Player != player && player.IsScpRole())
                    {
                        Hitmarker.SendHitmarkerDirectly(ev.Player.ReferenceHub, 1.5f);
                        player.Hit(ev.Player, player.MaxHealth);

                        if (ev.Player.IsDead)
                            Server.ExecuteCommand($"/cassie <color=#ff0000>{Trans.Role[ev.Player.Role.Type]}</color>이(가)  <color=#ff0000>{Trans.Role[player.Role.Type]}</color>의 뒷통수를 쳤습니다.");
                    }
                }

                yield return Timing.WaitForSeconds(0.01f);
            }
        }

        private class HitboxPatchPostfix
        {
            public static void Postfix(ref bool __result)
            {
                __result = true;
            }
        }
    }
}
