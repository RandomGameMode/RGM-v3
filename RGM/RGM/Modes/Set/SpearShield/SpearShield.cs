using AFK;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using MEC;
using PlayerRoles;
using RGM.API.Features;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RGM.Modes;

[Mode(ModeCategory.Public, ModeInfo.Set, ModeType.SpearShield)]

public class SpearShield : Mode
{
    private readonly List<Player> _teamA = [];
    private readonly List<Player> _teamB = [];

    private CoroutineHandle _onModeStarted;
    private bool _battleStarted;
    private bool _matchEnded;

    public override string Name => "창과 방패";
    public override string Description => "최대한 뚫고, 막으세요!";
    public override string Color => "fb9517";
    public override string Map => "SpearShield";
    public override string Author => "DeniA";
    public override string Detail =>
        """
        NTF는 CHAOS를 최대한 빠르게 사살해야 합니다.
        CHAOS는 지급되는 회복 아이템을 사용하여 최대한 버텨야 합니다.

        제한 시간은 12초이며, 제한 시간 경과 시 NTF가 패배합니다.
        """;

    public override void OnEnabled()
    {
        Round.IsLocked = true;
        Respawn.PauseWaves();
        AFKManager._kickTime = 120500;

        _teamA.Clear();
        _teamB.Clear();
        _battleStarted = false;
        _matchEnded = false;

        Exiled.Events.Handlers.Player.Died += OnDied;
        Exiled.Events.Handlers.Player.Shooting += OnShooting;

        _onModeStarted = Timing.RunCoroutine(OnModeStarted());
    }

    public override void OnDisabled()
    {
        Round.IsLocked = false;
        Respawn.ResumeWaves();

        Exiled.Events.Handlers.Player.Died -= OnDied;
        Exiled.Events.Handlers.Player.Shooting -= OnShooting;

        Timing.KillCoroutines(_onModeStarted);
    }

    private IEnumerator<float> OnModeStarted()
    {
        var players = PlayerManager.List.Where(x => !x.IsNPC).ToList();
        if (players.Count < 2)
            yield break;

        players.ShuffleList();

        int halfCount = players.Count / 2;
        _teamA.AddRange(players.Take(halfCount));
        _teamB.AddRange(players.Skip(halfCount));

        GameObject baseSpawn = GameObject.Find("[SP] Base");
        if (baseSpawn == null)
        {
            Log.Error("[SpearShield] [SP] Base 스폰 지점을 찾지 못했습니다.");
            yield break;
        }

        var basePosition = baseSpawn.transform.position;

        foreach (var player in _teamA)
        {
            player.Role.Set(RoleTypeId.NtfCaptain, RoleSpawnFlags.None);
            yield return Timing.WaitForOneFrame;

            player.ClearInventory();
            player.Position = basePosition;
            player.AddItem(ItemType.GunE11SR);
            player.EnableEffect(EffectType.Scp1853);
            player.ApplyGodMode(3);
        }

        foreach (var player in _teamB)
        {
            player.Role.Set(RoleTypeId.ChaosRepressor, RoleSpawnFlags.None);
            yield return Timing.WaitForOneFrame;

            player.ClearInventory();
            player.Position = basePosition;
            player.AddItem(ItemType.ArmorHeavy);
            for (int i = 0; i < 7; i++)
                player.AddItem(ItemType.SCP500);

            player.ApplyGodMode(3);
        }

        foreach (var player in players)
            player.AddBroadcast(3, "<size=30><b>3초 후 전투가 시작됩니다.</b></size>");

        yield return Timing.WaitForSeconds(3f);

        if (_matchEnded || Round.IsEnded)
            yield break;

        _battleStarted = true;

        for (int second = 10; second > 0 && !_matchEnded; second--)
        {
            if (CheckWinner())
                yield break;

            foreach (var player in _teamA.Where(x => x.IsAlive))
                player.Hurt(player.MaxHealth * 0.1f, "시간 제한으로 체력이 감소합니다.");

            foreach (var player in players.Where(x => x.IsAlive))
                player.AddHint("창과 방패", $"<b>남은 시간: {second}초</b>", 1.05f);

            Exiled.API.Features.Map.CleanAllItems();
            yield return Timing.WaitForSeconds(1);
        }

        if (!_matchEnded)
        {
            if (!CheckWinner())
                SetWinner(_teamB, "CHAOS가 10초 동안 버텨냈습니다.");
        }
    }

    private void OnShooting(ShootingEventArgs ev)
    {
        if (!_teamA.Contains(ev.Player) || ev.Item?.Type != ItemType.GunE11SR)
            return;

        ev.Firearm.MagazineAmmo = 101;
    }

    private void OnDied(DiedEventArgs ev)
    {
        if (_battleStarted && !_matchEnded)
            CheckWinner();
    }

    private bool CheckWinner()
    {
        if (_matchEnded)
            return true;

        if (!_teamB.Any(x => x.IsAlive))
        {
            SetWinner(_teamA, "NTF가 CHAOS를 섬멸했습니다.");
            return true;
        }

        if (!_teamA.Any(x => x.IsAlive))
        {
            SetWinner(_teamB, "NTF가 전멸했습니다.");
            return true;
        }

        return false;
    }

    private void SetWinner(List<Player> winningTeam, string message)
    {
        if (_matchEnded)
            return;

        _matchEnded = true;
        Round.IsLocked = false;

        foreach (var player in PlayerManager.List)
            player.AddBroadcast(10, $"<size=30><b>{message}</b></size>");

        Timing.RunCoroutine(Tools.SetWinner(winningTeam, 1));
    }
}