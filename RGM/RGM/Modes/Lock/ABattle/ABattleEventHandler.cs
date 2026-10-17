using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using InventorySystem.Items.Firearms.Attachments;
using MEC;
using UnityEngine;
using Exiled.Events.EventArgs.Scp079;
using Exiled.API.Extensions;
using Exiled.Events.EventArgs.Scp1507;
using static RGM.Variables.Variable;
using Exiled.API.Enums;
using Exiled.Events.EventArgs.Server;
using PlayerRoles;
using RGM.API.Features;
using RGM.Features;
using Random = UnityEngine.Random;

namespace RGM.Modes;

public class ABattleEventHandler(ABattle aBattle)
{
    public static ABattleEventHandler Instance;
    private readonly Dictionary<Player, List<AbilityType>> _pendingAbilityRestores = [];
    private readonly Dictionary<Player, HashSet<WorkstationController>> _dementiaFailedWorkstations = [];

    internal void RegisterEvents()
    {
        Exiled.Events.Handlers.Server.RoundEnded += OnRoundEnded;
        Exiled.Events.Handlers.Player.Verified += OnVerified;
        Exiled.Events.Handlers.Player.Left += OnLeft;
        Exiled.Events.Handlers.Player.Spawned += OnSpawned;
        Exiled.Events.Handlers.Player.Jumping += OnJumping;
        Exiled.Events.Handlers.Player.ChangingRole += OnChangingRole;
        Exiled.Events.Handlers.Player.Died += OnDied;

        Exiled.Events.Handlers.Scp079.Pinging += OnPinging;
        Exiled.Events.Handlers.Scp1507.SpawningFlamingos += OnSpawningFlamingos;
    }

    internal void UnregisterEvents()
    {
        Exiled.Events.Handlers.Server.RoundEnded -= OnRoundEnded;
        Exiled.Events.Handlers.Player.Verified -= OnVerified;
        Exiled.Events.Handlers.Player.Left -= OnLeft;
        Exiled.Events.Handlers.Player.Spawned -= OnSpawned;
        Exiled.Events.Handlers.Player.Jumping -= OnJumping;
        Exiled.Events.Handlers.Player.ChangingRole -= OnChangingRole;
        Exiled.Events.Handlers.Player.Died -= OnDied;

        Exiled.Events.Handlers.Scp079.Pinging -= OnPinging;
        Exiled.Events.Handlers.Scp1507.SpawningFlamingos -= OnSpawningFlamingos;
    }

    private void OnVerified(VerifiedEventArgs ev)
    {
        Verified(ev.Player);
    }

    private void Verified(Player player)
    {
        aBattle.EnsurePlayer(player);
        ABattle.ExtraModeNotion(player);
    }

    private void OnLeft(LeftEventArgs ev)
    {
        _pendingAbilityRestores.Remove(ev.Player);
        _dementiaFailedWorkstations.Remove(ev.Player);
        aBattle.CleanupPlayer(ev.Player);
    }

    private void OnSpawned(SpawnedEventArgs ev)
    {
        if (!ev.Player.IsScpRole()) ev.Player.ApplyGodMode(50f);

        if (_pendingAbilityRestores.TryGetValue(ev.Player, out var abilities))
        {
            _pendingAbilityRestores.Remove(ev.Player);
            Timing.RunCoroutine(ABattle.RestoreAbilities(ev.Player, abilities));
        }

        Timing.RunCoroutine(Spawned(ev.Player));
    }

    private IEnumerator<float> Spawned(Player player)
    {
        aBattle.EnsurePlayer(player);

        yield return Timing.WaitForSeconds(1);

        if (player.IsAlive)
            ABattle.ApplyPrelude(player);
    }

    private void OnJumping(JumpingEventArgs ev)
    {
        aBattle.EnsurePlayer(ev.Player);

        if (!Physics.Raycast(ev.Player.Position, Vector3.down, out var hit, 5, (LayerMask)1)) return;
        if (hit.transform == null) return;

        var controller = hit.transform.GetComponentInParent<WorkstationController>();

        if (controller == null) return;
        if (IsDementiaReuseBlocked(ev.Player, controller))
            return;

        if (!ABattle.CurrentExtraModes.Contains("대출") && aBattle.PlayerWorkstations[ev.Player].Contains(controller))
            return;

        if (ABattle.CurrentExtraModes.Contains("대출") && aBattle.PlayerWorkstations[ev.Player].Contains(controller) &&
            Convert.ToByte(Random.Range(1, 101)) <= 10)
        {
            /*if (GodModePlayers.Contains(ev.Player))
                GodModePlayers.Remove(ev.Player);

            ev.Player.RemoveAllAbilities();
            ev.Player.Kill("욕심을 부리다가 아사했습니다.");*/
            ApplyInstantKill.Apply(ev.Player, ev.Player);
            return;
        }

        if (aBattle.Selections.ContainsKey(ev.Player))
            aBattle.Selections[ev.Player].Clear();

        if (ABattle.CurrentExtraModes.Contains("대출") && aBattle.PlayerWorkstations[ev.Player].Contains(controller))
            aBattle.StartSelect(ev.Player);

        if (!aBattle.PlayerWorkstations.TryGetValue(ev.Player, out var workstations))
        {
            aBattle.PlayerWorkstations.Add(ev.Player, [controller]);

            aBattle.StartSelect(ev.Player);
        }
        else
        {
            if (workstations.Contains(controller)) return;
            workstations.Add(controller);

            aBattle.StartSelect(ev.Player);
        }
    }

    private IEnumerator<float> OnChangingRole(ChangingRoleEventArgs ev)
    {
        if (!ev.NewRole.IsDead())
            aBattle.LastDeathRoles.Remove(ev.Player);

        bool shouldRestore = ev.Reason == SpawnReason.Escaped ||
                             (ev.NewRole == RoleTypeId.Tutorial && ev.Player.IsCuffed);

        if (shouldRestore)
            QueueAbilityRestore(ev.Player);
        else if (ev.Player.IsDead || ev.NewRole.IsDead() || !ev.Player.GetAbilities().Any())
            Timing.CallDelayed(Timing.WaitForOneFrame, () => aBattle.Reset(ev.Player));

        yield break;
    }

    private void OnDied(DiedEventArgs ev)
    {
        aBattle.LastDeathRoles[ev.Player] = ev.TargetOldRole;

        Timing.CallDelayed(Timing.WaitForOneFrame, () => { aBattle.Reset(ev.Player); });
    }

    private void OnPinging(PingingEventArgs ev)
    {
        aBattle.EnsurePlayer(ev.Player);

        Vector3 pos = ev.Position;

        if (!Physics.Raycast(new Vector3(pos.x, pos.y + 1, pos.z), Vector3.down, out var hit, 5, (LayerMask)1)) return;
        if (hit.transform == null) return;

        var controller = hit.transform.GetComponentInParent<WorkstationController>();

        if (controller == null) return;
        if (IsDementiaReuseBlocked(ev.Player, controller))
            return;

        if (!ABattle.CurrentExtraModes.Contains("대출") && aBattle.PlayerWorkstations[ev.Player].Contains(controller))
            return;

        if (ABattle.CurrentExtraModes.Contains("대출"))
        {
            if (aBattle.PlayerWorkstations[ev.Player].Contains(controller) &&
                Convert.ToByte(Random.Range(1, 101)) <= 10)
            {
                /*if (GodModePlayers.Contains(ev.Player))
                GodModePlayers.Remove(ev.Player);

                ev.Player.RemoveAllAbilities();
                ev.Player.Kill("욕심을 부리다가 아사했습니다.");*/
                ApplyInstantKill.Apply(ev.Player, ev.Player);
                return;
            }
        }

        if (aBattle.Selections.ContainsKey(ev.Player))
            aBattle.Selections[ev.Player].Clear();

        if (ABattle.CurrentExtraModes.Contains("대출"))
            aBattle.StartSelect(ev.Player);

        if (!aBattle.PlayerWorkstations.TryGetValue(ev.Player, out var workstations))
        {
            aBattle.PlayerWorkstations.Add(ev.Player, [controller]);

            aBattle.StartSelect(ev.Player);
        }
        else
        {
            if (workstations.Contains(controller)) return;
            workstations.Add(controller);

            aBattle.StartSelect(ev.Player);
        }
    }

    private bool IsDementiaReuseBlocked(Player player, WorkstationController controller)
    {
        if (!ABattle.CurrentExtraModes.Contains("치매"))
            return false;

        if (!aBattle.PlayerWorkstations.TryGetValue(player, out var workstations) ||
            !workstations.Contains(controller))
        {
            // LuckyVicky 등으로 기록이 초기화된 경우에는 다시 사용할 수 있다.
            if (_dementiaFailedWorkstations.TryGetValue(player, out var failedWorkstations))
                failedWorkstations.Remove(controller);

            return false;
        }

        if (!_dementiaFailedWorkstations.TryGetValue(player, out var failed))
        {
            failed = [];
            _dementiaFailedWorkstations.Add(player, failed);
        }

        // 이미 실패한 워크스테이션은 기록 초기화 능력이 있기 전까지 재시도할 수 없다.
        if (failed.Contains(controller))
            return true;

        if (Convert.ToByte(Random.Range(1, 101)) <= 32)
        {
            // 전체 기록이 아닌, 이번에 사용한 워크스테이션의 기록만 제거한다.
            player.AddHint("워크 치매", "<b><color=#D23265>내가 이 워크스테이션을 먹었던가...?</color></b>", 2);
            workstations.Remove(controller);
            return false;
        }

        failed.Add(controller);
        return true;
    }

    private void OnSpawningFlamingos(SpawningFlamingosEventArgs ev)
    {
        foreach (var player in ev.SpawnablePlayers)
            QueueAbilityRestore(player);
    }

    private void QueueAbilityRestore(Player player)
    {
        if (_pendingAbilityRestores.ContainsKey(player) ||
            !aBattle.PlayerAbilities.TryGetValue(player, out var playerAbilities))
            return;

        List<AbilityType> abilities =
        [
            .. playerAbilities
                .Where(x => x.Data.Category != AbilityCategory.Ancient)
                .Select(x => x.Data.AbilityType)
        ];

        if (abilities.Count == 0)
            return;

        _pendingAbilityRestores.Add(player, abilities);
        aBattle.Reset(player);
    }

    private static void OnRoundEnded(RoundEndedEventArgs ev)
    {
        List<Player> players = [.. PlayerManager.List.Where(x => x.IsAlive && !x.IsNPC)];

        switch (players.Count)
        {
            case 1:
                Timing.RunCoroutine(Tools.SetWinner([.. players], PlayerManager.List.Count / 2));
                break;
            case > 1:
                Timing.RunCoroutine(Tools.SetWinner([.. players], 1));
                break;
        }
    }
}