using CommandSystem;
using DiscordInteraction.Discord;
using Exiled.API.Extensions;
using Exiled.API.Features;
using InventorySystem.Items.Firearms.Attachments;
using MapGeneration.Holidays;
using MEC;
using PlayerRoles;
using RemoteAdmin;
using RGM.API.Features;
using RGM.Modes.Commands;
using RGM.Modes.Lock.ABattle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using CustomPlayerEffects;
using Exiled.API.Features.Doors;
using Mirror;
using NetworkManagerUtils.Dummies;
using PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers;
using UnityEngine;
using UserSettings.ServerSpecific;
using static RGM.Variables.Variable;
using Random = UnityEngine.Random;

namespace RGM.Modes;

[Mode(ModeCategory.Public, ModeInfo.Lock, ModeType.ABattle)]
public class ABattle : Mode
{
    public override string Name => "워크스테이션 업그레이드";
    public override string Description => "워크스테이션에서 업그레이드하세요!";

    public override string Detail =>
        """
        <color=#F5DA81>인간 진영</color>일 경우, 워크스테이션에서 점프하면 능력을 1개 얻습니다.
        <color=red>SCP-079</color>일 경우, 레벨이 올라갈 때마다 능력을 1개 얻습니다.

        각 능력 등급들의 확률을 확인하려면 아래를 참고하십시오.
        • <color=#A4A4A4>일반</color> - 62.40%
        • <color=#2ECCFA>희귀</color> - 31.55%
        • <color=#BF40BF>영웅</color> - 5.85%
        • <color=#FFC000>전설</color> - 0.25%
        • <color=#FF2400>신화</color> - 0.05%
        • <color=#008000>고대</color> - 0.01%
        • <color=#DEEFED>시너지</color> - ???

        • <color=#F7819F>전용</color> 
        <color=#A4A4A4>일반</color> - 5%
        <color=#2ECCFA>희귀</color> - 10%
        <color=#BF40BF>영웅</color> - 15%
        <color=#FFC000>전설</color> - 25%
        <color=#FF2400>신화</color> - 35%
        <color=#008000>고대</color> - 45%
        (등급에 따라 확률 변동, 능력 선택 옵션 독립)

        66.6% 확률로 추가 모드가 활성화됩니다.
        워크스테이션이 시설에 더 추가됩니다.

        <size=25><b>모드 전용 명령어</b></size>
        <size=20>.(번호) - 1번부터 5번까지 있습니다. 능력을 선택할 때 사용됩니다.</size>
        <size=20>.추가모드 - 현재 워크스테이션 업그레이드 모드의 추가 모드를 확인합니다.</size>
        """;

    public override string Color => "AAFF00";
    public override string Map => "ABattle";

    public override string Author => "GoldenPig1205, DeniA, and RGM Contributors :D";

    public static ABattle Instance;
    private static readonly Mutex _chaosModeLock = new();

    // 동기화 객체
    private readonly object _selectionLock = new();
    private readonly object _cursorLock = new();

    public Dictionary<Player, List<WorkstationController>> PlayerWorkstations = new();
    public Dictionary<AbilityType, AbilityData> Abilities = new();
    public Dictionary<Player, List<Ability>> PlayerAbilities = new();
    public Dictionary<Player, List<AbilityType>> Selections = new();
    public Dictionary<Player, bool> IsSelecting = new();
    public Dictionary<Player, bool> IsLifeUsed = new();
    public Dictionary<Player, RoleTypeId> LastDeathRoles = new();

    private Dictionary<AbilityType, List<AbilityType>> SynergyAbilities = new();
    private Dictionary<Player, int> SelectionCursor = new();
    public event Action<AddingAbilityEventArgs> AddingAbility;
    private Mutex _chaosMutex = new();
    private ABattleEventHandler _eventHandler;

    public static readonly Dictionary<string, string> RatingColor = new()
    {
        { "일반", "#A4A4A4" },
        { "희귀", "#2ECCFA" },
        { "영웅", "#BF40BF" },
        { "전설", "#FFC000" },
        { "신화", "#FF2400" },
        { "고대", "#008000" },
        { "전용", "#F7819F" },
        { "시너지", "#DEEFED" }
    };

    public static readonly Dictionary<string, string> SelectFormat = new()
    {
        { "일반", "<b><color=#404040>일반</color></b>" },
        { "희귀", "<b><color=#47DAFF>희귀</color></b>" },
        { "영웅", "<b><color=#CB62CB>영웅</color></b>" },
        { "전설", "<b><color=#FFD700>전설</color></b>" },
        { "신화", "<b><color=#F52500>신화</color></b>" },
        { "고대", "<b><color=#008000>고대</color></b>" },
        { "전용", "<b><color=#F7819F>전용</color></b>" },
        { "알 수 없음", "<b><color=#333333>알수없음</b>" }
    };

    public static readonly Dictionary<string, string> ExtraModes = new()
    {
        { "기본", "워크스테이션 업그레이드를 즐기세요!" },
        //{"치매", "25% 확률로 획득했던 워크스테이션에서 능력을 다시 획득할 수 있습니다."},
        { "반사경", "능력 획득 시, 25% 확률로 반사경 효과가 적용됩니다." },
        { "수저", "능력 선택창에서 등장하는 능력의 수가 최대 5개까지 늘어날 수 있습니다." },
        { "골드 전주곡", $"스폰 즉시 <color={RatingColor["영웅"]}>영웅</color> 등급의 능력을 얻습니다. (일부 능력 제한)" },
        {
            "프리즘 전주곡",
            $"스폰 즉시 <color={RatingColor["영웅"]}>영웅</color>(15% 확률로 <color={RatingColor["전설"]}>전설</color>, 1% 확률로 <color={RatingColor["신화"]}>신화</color>) 등급의 능력을 얻습니다."
        },
        { "잔칫상", $"<color={RatingColor["희귀"]}>희귀</color> 이상 등급의 능력이 등장할 확률이 높아집니다." },
        //{"스펙업", "능력을 획득할 때마다 기본 최대 체력의 인간 9%, SCP 1.5%만큼 최대 체력이 증가합니다."},
        { "스펙업", "능력을 획득할 때마다 10(SCP 50)만큼 최대 체력이 증가합니다." },
        { "캐시 청소", "8분마다 모든 유저의 워크스테이션 획득 기록이 초기화됩니다." },
        { "대출", "워크스테이션 제한이 해제됩니다. 각 워크스테이션마다 처음 1회를 제외하고 추가로 얻으려고 시도하는 경우, 18% 확률로 아사합니다." },
        { "지원", "1~3분마다 모두에게 능력 선택창이 열립니다." },
        {
            "난장판", "유령이 시스템을 장악하여 난장판이 되었습니다. 이로 인해 관리자의 제약이 모두 풀립니다.\n" +
                   "아, 빼먹은 것이 있군요. <b><color=#FF5F1F>HYPER BURNING</color></b>이 활성화됩니다.\n"
        }
    };

    private static readonly List<ICommand> DotCommands =
    [
        new SelectFirst(),
        new SelectSecond(),
        new SelectThird(),
        new SelectFourth(),
        new SelectFifth(),
        new GetExtraMode(),
        new CASSIE(),
        new AbilityInformation()
    ];

    private static readonly List<ICommand> RemoteAdminCommands =
    [
        new AddAbility(),
        new AddExtraMode()
    ];

    private static string ColorFormat(string text)
    {
        return
            text.Replace("[시너지]", $"<color={RatingColor["시너지"]}>[시너지]</color>")
                .Replace("[고대]", $"<color={RatingColor["고대"]}>[고대]</color>")
                .Replace("[신화]", $"<color={RatingColor["신화"]}>[신화]</color>")
                .Replace("[전설]", $"<color={RatingColor["전설"]}>[전설]</color>")
                .Replace("[영웅]", $"<color={RatingColor["영웅"]}>[영웅]</color>")
                .Replace("[희귀]", $"<color={RatingColor["희귀"]}>[희귀]</color>")
                .Replace("[일반]", $"<color={RatingColor["일반"]}>[일반]</color>");
    }

    public string PickExtraMode(List<string> exceptModes = null, bool allowBasic = true)
    {
        exceptModes ??= new List<string>();

        var candidates = ExtraModes.Keys
            .Where(x => x != "기본" && !exceptModes.Contains(x) && !CurrentExtraModes.Contains(x))
            .ToList();

        string extraMode;

        if (allowBasic && Convert.ToByte(Random.Range(1, 7)) == 1)
        {
            extraMode = "기본";
        }
        else if (candidates.Count == 0)
        {
            if (!allowBasic)
                return null;

            extraMode = "기본";
        }
        else
        {
            extraMode = candidates.GetRandomValue();
        }

        bool newlyAdded = false;

        if (extraMode == "기본")
        {
            if (CurrentExtraModes.Count == 0)
            {
                CurrentExtraModes.Add("기본");
                newlyAdded = true;
            }
        }
        else if (!CurrentExtraModes.Contains(extraMode))
        {
            CurrentExtraModes.Remove("기본");
            CurrentExtraModes.Add(extraMode);
            newlyAdded = true;
        }

        Webhook.Send($"추가 모드: {extraMode}");
        Log.Info($"추가 모드: {extraMode}");

        if (!newlyAdded)
            return extraMode;

        switch (extraMode)
        {
            case "캐시 청소":
                Timing.RunCoroutine(Instance.ClearCache());
                break;
            case "지원":
                Timing.RunCoroutine(Instance.Backup());
                break;
            case "난장판":
            {
                for (int i = 0; i < 3; i++)
                    PickExtraMode(exceptModes: ["난장판"], allowBasic: false);
                Timing.CallDelayed(1f, ActivateExtraModeForChaos);
                break;
            }
        }

        return extraMode;
    }

    private void ActivateExtraModeForChaos()
    {
        // 1. C.A.S.S.I.E 방송 후 딜레이
        // 2. 실행 후 딜레이
        const float waitTime = 5f;
        const float chaosStopTime = 55f;

        // (확률) = 100 - 값 + 1
        const int mythicChance = 99;
        const int legendaryChance = 97;
        const int epicChance = 80;
        const int explodeChance = 98;
        const int repeatCount = 5;
        const int explodeCount = 5;

        var rand = new System.Random(Exiled.API.Features.Map.Seed);

        Timing.RunCoroutine(Chaos()); // 가랏 피카츄!!
        Timing.RunCoroutine(ClearMaker());
        FriendlyFire.Instance.OnEnabledForNoNuke();

        return; // 지역 함수

        IEnumerator<float> Chaos()
        {
            if (!_chaosModeLock.WaitOne(5)) yield break;

            yield return Timing.WaitForSeconds(0.1f);

            while (EnabledModeList.Exists(x => x.Data.Type == ModeType.ABattle))
            {
                Tools.MessageTranslated(".G6 .G6 .G6",
                    $"{waitTime}초 후 무언가가 일어납니다.");
                Timing.CallDelayed(waitTime, () => Timing.RunCoroutine(ChaosMaker())); // 가랏 몬스터볼!!

                yield return Timing.WaitForSeconds(chaosStopTime);
            }

            FriendlyFire.Instance.OnDisabled();
            _chaosModeLock.ReleaseMutex();
        }

        IEnumerator<float> ChaosMaker()
        {
            if (!_chaosMutex.WaitOne(1000)) yield break;

            // -----------------------------------------------------------------------------------------------

            List<Player> complete = [];
            Timing.CallDelayed(Timing.WaitForOneFrame, () =>
            {
                for (int i = 0; i < explodeCount; i++)
                {
                    if (!(NextRandom() >= explodeChance)) continue;

                    List<string> musicList =
                    [
                        "짬뽕-1",
                        "폭8-2",
                        "폭8-1"
                    ];

                    List<string> delayBomb =
                        ["짬뽕-1"];

                    var player = PlayerManager.List.Where(x => x.IsAlive && !x.IsNPC).GetRandomValue();
                    if (player.IsDead || player.IsHost || complete.Contains(player)) continue;
                    var text = musicList.GetRandomValue();
                    var role = player.Role;

                    complete.Add(player);

                    Timing.CallDelayed(10, () =>
                    {
                        if (player.IsDead) player.Role.Set(role, RoleSpawnFlags.None);

                        Timing.CallDelayed(Timing.WaitForOneFrame, () =>
                        {
                            for (int j = 0; j < 5; j++)
                            {
                                try
                                {
                                    player.AddAbility(Instance.GetRandomAbilities(player,
                                        GetCategory(player, allowAncient: false), 1)[0]);
                                }
                                catch (Exception ex)
                                {
                                    Log.Error($"Failed to add ability to Mad Scientist: {ex}");
                                }
                            }
                        });
                    });

                    if (!delayBomb.Contains(text))
                    {
                        MakeRadio(player, text);
                        player.ExplodeGrenade(ignore: true);
                        player.Kill("약한 폭8을 맛보았습니다.");
                        continue;
                    }

                    MakeRadio(player, text);
                    Timing.CallDelayed(2f, () =>
                    {
                        for (var a = 0; a < 3; a++)
                            player.ExplodeGrenade(instakill: false);
                        player.Kill("강력한 폭8을 맛보았습니다.");
                    });
                }
            });
            // -----------------------------------------------------------------------------------------------

            PlayerManager.List.Where(x => x.IsAlive && !x.IsNPC)
                .ToList()
                .ForEach(x
                    => x.AddAbility(GetRandomAbilities(x, AbilityCategory.Normal, 5).GetRandomValue()));

            for (var a = 0; a < repeatCount; a++)
            {
                yield return Timing.WaitForOneFrame;

                if (Door.List.GetRandomValue() is BreakableDoor door) door.IsDestroyed = true;

                if (NextRandom() >= epicChance)
                {
                    var player = PlayerManager.List.Where(x => x.IsAlive && !x.IsNPC).GetRandomValue();
                    AddAbility(player, GetRandomAbilities(player, AbilityCategory.Epic, 5).GetRandomValue());
                    Tools.MessageTranslated(".G2 .G2",
                        $"플레이어 <b><color={player.Role.Color.ToHex()}>{player.Nickname}</color></b>이(가) 25%의 확률로 <b><color={AbilityCategory.Epic.GetColor()}>영웅</color></b> 능력을 획득하였습니다!");

                    continue;
                }

                if (NextRandom() >= legendaryChance)
                {
                    var player = PlayerManager.List.Where(x => x.IsAlive && !x.IsNPC).GetRandomValue();
                    AddAbility(player, GetRandomAbilities(player, AbilityCategory.Legend, 5).GetRandomValue());
                    Tools.MessageTranslated(".G6 .G6 .G6 .G6 .G6",
                        $"플레이어 <b><color={player.Role.Color.ToHex()}>{player.Nickname}</color></b>이(가) 20%의 확률로 <b><color={AbilityCategory.Legend.GetColor()}>전설</color></b> 능력을 획득하였습니다!");

                    continue;
                }

                // 이름 충돌 방지로 설치, 반전을 하지 말 것
                if (NextRandom() >= mythicChance)
                {
                    var player = PlayerManager.List.Where(x => x.IsAlive && !x.IsNPC).GetRandomValue();
                    AddAbility(player, GetRandomAbilities(player, AbilityCategory.Mythic, 5).GetRandomValue());
                    Tools.MessageTranslated(".G6 .G6 .G6 .G6 .G6",
                        $"플레이어 <b><color={player.Role.Color.ToHex()}>{player.Nickname}</color></b>이(가) 15%의 확률로 <b><color={AbilityCategory.Mythic.GetColor()}>신화</color></b> 능력을 획득하였습니다!");
                }
            }

            _chaosMutex.ReleaseMutex();

            // -----------------------------------------------------------------------------------------------
            yield break; // 지역 함수

            float NextRandom() => rand.Next(1, 101);
        }

        IEnumerator<float> ClearMaker()
        {
            while (EnabledModeList.Exists(x => x.Data.Type == ModeType.ABattle))
            {
                yield return Timing.WaitForSeconds(120f);

                try
                {
                    PlayerManager.List
                        .Where(x => x.IsNPC && x.Nickname == "영사기")
                        .ToList()
                        .ForEach(clear => NetworkServer.Destroy(clear.GameObject));
                }
                catch (Exception e)
                {
                    Log.Error($"이런, 청소 기능에 버그가 발생하였네요 :(\n" +
                              $"사유:{e.GetType().Name}: {e.Message}\n" +
                              $"StackTrace: {e.StackTrace}");
                }
            }
        }

        void MakeRadio(Player player, string arg)
        {
            if (player == null) return;
            var dummy = DummyUtils.SpawnDummy("영사기");

            dummy.roleManager.ServerSetRole(RoleTypeId.Tutorial, RoleChangeReason.None);
            dummy.playerEffectsController.EnableEffect<SpawnProtected>();
            dummy.ServerSetEmotionPreset(EmotionPresetType.AwkwardSmile);
            dummy.transform.position = player.Position;
            Timing.RunCoroutine(Teleporter(player));

            var radio = AudioPlayer.CreateOrGet("AudioPlayer", condition: hub
                => !MuteBGMPlayers.Contains(Player.Get(hub)), onIntialCreation: p =>
            {
                Speaker speaker = p.AddSpeaker("Main", 1.5f, isSpatial: true, minDistance: 1f, maxDistance: 50f);
                p.transform.parent = dummy.gameObject.transform;
                speaker.transform.parent = dummy.gameObject.transform;
                speaker.transform.localPosition = Vector3.zero;
            });


            Timing.CallDelayed(radio.TryPlay(arg, 1.5f).Duration.Seconds + 1, ()
                => NetworkServer.Destroy(dummy.gameObject));

            return;

            IEnumerator<float> Teleporter(Player pl)
            {
                while (pl != null && !pl.IsDead)
                {
                    dummy.transform.position = pl.Position;
                    yield return Timing.WaitForOneFrame;
                }
            }
        }
    }

    public static readonly List<string> CurrentExtraModes = [];

    private CoroutineHandle _onModeStarted;
    private CoroutineHandle _hintCoroutine;

    // 플러그인에 있는 모든 능력 검색
    public override void OnEnabled()
    {
        Instance = this;

        PickExtraMode();

        _eventHandler = new ABattleEventHandler(this);
        _eventHandler.RegisterEvents();

        foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
        {
            var abilityAttribute = type.GetCustomAttribute<AbilityAttribute>();

            if (abilityAttribute == null)
                continue;

            if (!typeof(Ability).IsAssignableFrom(type))
                continue;

            switch (abilityAttribute.HolidayType)
            {
                case AbilityHolidayType.Christmas when !HolidayUtils.IsHolidayActive(HolidayType.Christmas):
                case AbilityHolidayType.Halloween when !HolidayUtils.IsHolidayActive(HolidayType.Halloween):
                    continue;
                case AbilityHolidayType.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (Abilities.TryGetValue(abilityAttribute.Type, out var ability))
            {
                Log.Error(
                    $"Duplicate AbilityType '{abilityAttribute.Type}' on {type.FullName}. Already registered by {ability.Type.FullName}.");
                continue;
            }

            Abilities.Add(abilityAttribute.Type, new AbilityData
            {
                Type = type,
                Name = abilityAttribute.Name,
                Description = abilityAttribute.Description,
                Category = abilityAttribute.Category,
                AbilityType = abilityAttribute.Type,
                HolidayType = abilityAttribute.HolidayType,
                Keep = abilityAttribute.Keep,
                _79Allowed = abilityAttribute._79Allowed,
                RoleAbility = abilityAttribute.RoleAbility
            });

            var requiresAbilityAttribute = type.GetCustomAttribute<RequiresAbilityAttribute>();

            if (requiresAbilityAttribute != null && requiresAbilityAttribute.Abilities.Length > 0)
            {
                SynergyAbilities.Add(abilityAttribute.Type, requiresAbilityAttribute.Abilities.ToList());

                Abilities[abilityAttribute.Type].Category = AbilityCategory.Synergy;
                Abilities[abilityAttribute.Type].Requires = requiresAbilityAttribute.Abilities.ToList();
            }
        }

        foreach (var dot in DotCommands)
            QueryProcessor.DotCommandHandler.RegisterCommand(dot);

        foreach (var ra in RemoteAdminCommands)
            CommandProcessor.RemoteAdminCommandHandler.RegisterCommand(ra);

        _onModeStarted = Timing.RunCoroutine(OnModeStarted());
        _hintCoroutine = Timing.RunCoroutine(HintCoroutine());

        ServerSpecificSettingsSync.ServerOnSettingValueReceived += ABattleSetting.OnSSInput;
    }

    public override void OnDisabled()
    {
        _eventHandler.UnregisterEvents();
        Timing.KillCoroutines(_onModeStarted);
        Timing.KillCoroutines(_hintCoroutine);

        CurrentExtraModes.Clear();

        foreach (var dot in DotCommands)
        {
            if (QueryProcessor.DotCommandHandler.TryGetCommand(dot.Command, out ICommand command))
                QueryProcessor.DotCommandHandler.UnregisterCommand(command);
        }

        foreach (var ra in RemoteAdminCommands)
        {
            if (CommandProcessor.RemoteAdminCommandHandler.TryGetCommand(ra.Command, out ICommand command))
                CommandProcessor.RemoteAdminCommandHandler.UnregisterCommand(command);
        }

        foreach (var player in PlayerAbilities.Keys.ToList())
            CleanupPlayer(player);

        PlayerWorkstations.Clear();
        PlayerAbilities.Clear();
        Selections.Clear();
        IsSelecting.Clear();
        IsLifeUsed.Clear();
        LastDeathRoles.Clear();
        SelectionCursor.Clear();

        ServerSpecificSettingsSync.ServerOnSettingValueReceived -= ABattleSetting.OnSSInput;
        AddingAbility = null;
    }

    private IEnumerator<float> OnModeStarted()
    {
        yield return Timing.WaitForOneFrame;

        Tools.LoadMap("AddCamera");
        if (CurrentExtraModes.Contains("난장판") || Convert.ToByte(Random.Range(1, 101)) <= 10)
        {
            Tools.LoadMap("AddWorkstation");
            foreach (var player in PlayerManager.List)
            {
                player.AddBroadcast(10, $"""
                                         <size=25><b><color=#FF5F1F>HYPER BURNING</color></b></size>
                                         <size=20><color=#DC143C>더욱 더 많은 워크스테이션!!!</color></size>
                                         """);
            }
        }

        foreach (var player in PlayerManager.List)
        {
            try
            {
                EnsurePlayer(player);
                ExtraModeNotion(player);
                ApplyPrelude(player);
            }
            catch (Exception e)
            {
                Log.Error(
                    $"An error occurred while trying to add <b><i>{player.Nickname}</i></b> to the dictionary: {e}");
            }
        }
    }

    private IEnumerator<float> HintCoroutine()
    {
        while (true)
        {
            foreach (var player in Player.List.Where(x => !x.IsNPC))
            {
                var CurrentHint = player.CurrentHint;
                var isStatusHint = CurrentHint != null &&
                                   (CurrentHint.Content.Contains("워크스테이션") || CurrentHint.Content.Contains("보유 업그레이드"));

                if (player.IsAlive)
                    player.AddHint("워크스테이션 힌트", FormatHint(player), 1.1f);
            }

            yield return Timing.WaitForOneFrame;
        }
    }

    private IEnumerator<float> ClearCache()
    {
        while (true)
        {
            foreach (var player in PlayerWorkstations.Keys)
            {
                PlayerWorkstations[player].Clear();

                if (player != null && player.IsConnected)
                    player.AddBroadcast(10,
                        $"<b><size=25>캐시 청소가 완료되었습니다. 이전에 방문한 워크스테이션에서 능력을 다시 얻을 수 있습니다.</size></b>");
            }

            yield return Timing.WaitForSeconds(60 * 8);
        }
    }

    private IEnumerator<float> Backup()
    {
        while (true)
        {
            foreach (var player in
                     PlayerManager.List.Where(x => !x.IsNPC && x.IsAlive && PlayerManager.List.Contains(x)))
            {
                player.AddBroadcast(10, "<size=24><b>10초 후 모든 생존 플레이어에게 능력 선택창이 열립니다.</b></size>");
            }

            yield return Timing.WaitForSeconds(10f);

            foreach (var player in
                     PlayerManager.List.Where(x => !x.IsNPC && x.IsAlive && PlayerManager.List.Contains(x)))
            {
                StartSelect(player);
            }

            yield return Timing.WaitForSeconds(Convert.ToByte(Random.Range(60, 181)));
        }
    }

    private string FormatHint(Player player)
    {
        if (!PlayerAbilities.TryGetValue(player, out var ability) || !ability.Any())
        {
            return player.Role.Type == RoleTypeId.Scp079
                ? "<align=left><b><size=22>워크스테이션 상단을 핑으로 찍으면 능력을 획득할 수 있습니다.</size></b></align>"
                : "<align=left><b><size=22>워크스테이션 위에서 점프하면 능력을 획득할 수 있습니다.</size></b></align>";
        }

        var abilitiesText = string.Join(", ",
            PlayerAbilities[player]
                .GroupBy(x => x.Data.AbilityType)
                .Select(g => g.Count() > 1
                    ? $"{g.First().Data.GetFormattedName()} x{g.Count()}"
                    : g.First().Data.GetFormattedName())
                .ToList());

        return $"<align=left><b><size=24>보유 업그레이드</size></b>\n<size=20>{abilitiesText}</size>\n</align>";
    }

    public static IEnumerator<float> RestoreAbilities(Player player, List<AbilityType> abilities)
    {
        yield return Timing.WaitForOneFrame;

        if (!player.IsAlive)
            yield break;

        foreach (var ability in abilities)
            player.AddAbility(ability);

        yield return Timing.WaitForOneFrame;

        player.AddBroadcast(10, $"<size=25><b>모든 능력을 제거한 후, 수복하였습니다.</b></size>");
    }

    public static void ExtraModeNotion(Player player, bool enableBroadcast = true)
    {
        if (player == null) return;

        foreach (var cem in CurrentExtraModes)
        {
            var extraMode = $"<size=25><b><color=#fecdcd>{cem}</color></b></size>\n<size=20>{ExtraModes[cem]}</size>";

            if (enableBroadcast)
                player.AddBroadcast(10, extraMode);

            player.SendConsoleMessage("\n" + extraMode, "white");
        }
    }

    public string GetAbilityInformation(AbilityType ability)
    {
        string message = $"{Abilities[ability].GetFormattedName()}: {Abilities[ability].Description}";
        return message;
    }

    // 플레이어에게 특정 능력을 부여
    // reflectorChain: LEGEND_REFLECTOR로 인한 연쇄 횟수 (최대 3회)
    // extraReflectorChain: 추가 모드 반사경으로 인한 연쇄 횟수 (최대 2회)
    // allowReflector: false면 반사경 연쇄를 건너뜀 (복제 등)
    public bool AddAbility(Player player, AbilityType type, int reflectorChain = 0, bool allowReflector = true,
        int extraReflectorChain = 0)
    {
        if (player == null) return false;

        if (!Abilities.ContainsKey(type))
        {
            Log.Error($"Ability {type} not found.");
            return false;
        }

        var addingAbilityEventArgs = new AddingAbilityEventArgs(
            player,
            type,
            reflectorChain,
            allowReflector,
            extraReflectorChain);
        AddingAbility?.Invoke(addingAbilityEventArgs);

        if (!addingAbilityEventArgs.IsAllowed)
            return false;

        if (type.ToString().Contains("LEGEND"))
        {
            string name;

            switch (type)
            {
                case AbilityType.LEGEND_LAVACHICKEN: name = "LavaChicken"; break;
                default: name = "누군가가 전설 능력을 획득하였습니다"; break;
            }

            if (GlobalPlayer.ClipsById.Count(x => x.Value.Clip == name) < 1)
                Tools.PlayGlobalAudio(name, 1.5f);
        }
        else if (type.ToString().Contains("MYTHIC"))
        {
            string name;

            switch (type)
            {
                case AbilityType.MYTHIC_KINGSCOLOR: name = "시산혈해의 파도가 보인다"; break;
                default: name = "누군가가 신화 능력을 영접하였습니다"; break;
            }

            if (GlobalPlayer.ClipsById.Count(x => x.Value.Clip == name) < 1)
                Tools.PlayGlobalAudio(name, 2.5f);
        }
        else if (type.ToString().Contains("ANCIENT"))
        {
            const string name = "누군가가 고대의 무한한 힘을 손에 얻었습니다";

            if (GlobalPlayer.ClipsById.Count(x => x.Value.Clip == name) < 1)
                Tools.PlayGlobalAudio(name, 2f);
        }

        if (allowReflector && Abilities[type].Category != AbilityCategory.Ancient &&
            Abilities[type].Category != AbilityCategory.Synergy)
        {
            // 추가 모드 반사경: 25% 확률로 동일 능력 추가 획득. 해당 모드의 연쇄는 최대 2회까지.
            if (CurrentExtraModes.Contains("반사경") && extraReflectorChain < 2 &&
                Convert.ToByte(Random.Range(1, 101)) <= 25)
            {
                AddAbility(player, type, reflectorChain, allowReflector, extraReflectorChain + 1);
            }
        }

        Log.Info("AddAbility called with " + player.Nickname + " and " + type);

        if (!PlayerAbilities.ContainsKey(player))
        {
            Log.Info("No key");
            PlayerAbilities.Add(player, []);
        }

        var abilityData = Abilities[type];
        Ability ability;

        try
        {
            ability = Activator.CreateInstance(Abilities[type].Type) as Ability;
        }
        catch (Exception e)
        {
            Log.Error($"An error occurred while trying to create an instance of {abilityData.Name}: {e}");
            return false;
        }

        if (ability == null)
        {
            Log.Error(
                $"An error occurred while trying to create an instance of {abilityData.Name}. The instance is null.");
            return false;
        }

        ability.Data = abilityData;
        ability.Owner = player;
        ability.OnEnabled();

        PlayerAbilities[player].Add(ability);
        EnableSynergyAbility(player);

        string styleName = ColorFormat(abilityData.GetFormattedName());

        string Message = $"<size=24>{styleName}</size>\n<size=20>{abilityData.Description}</size>";
        player.AddBroadcast(8, Message);
        player.SendConsoleMessage($"\n{Message}", "white");

        if (CurrentExtraModes.Contains("스펙업"))
        {
            /*float baseMaxHealth = player.ReferenceHub.roleManager
                .GetRoleBase(player.Role.Type) is IHealthbarRole role
                ? role.MaxHealth
                : player.MaxHealth;
            float healthIncrease = baseMaxHealth * (player.IsScpRole() ? 0.015f : 0.12f);*/
            // 왜 이 코드가 서버렉을 유발하는지 모르겠음

            var healthIncrease = player.IsScpRole() ? 50 : 10;

            player.MaxHealth += healthIncrease;
            player.Health += healthIncrease;
        }

        return true;
    }

    // 플레이어에게 시너지 능력 부여
    private void EnableSynergyAbility(Player player)
    {
        List<Ability> abilities = PlayerAbilities[player];

        foreach (var synergy in SynergyAbilities)
        {
            // 시너지 능력의 요구사항이 중복을 포함할 수 있도록, 각 요구 능력의 개수를 세서 비교
            bool hasAllRequired = true;
            foreach (var req in synergy.Value.GroupBy(x => x))
            {
                int requiredCount = req.Count();
                int playerCount = abilities.Count(a => a.Data.AbilityType == req.Key);
                if (playerCount >= requiredCount) continue;
                hasAllRequired = false;
                break;
            }

            if (!hasAllRequired)
                continue;

            if (!Abilities.TryGetValue(synergy.Key, out var synergyAbilityType))
                continue;

            if (abilities.Any(a => a.Data.AbilityType == synergy.Key))
                continue;

            player.AddAbility(synergyAbilityType.AbilityType);
        }
    }

    // 플레이어로부터 특정 능력 제거
    public void RemoveAbility(Player player, AbilityType type)
    {
        if (!PlayerAbilities.TryGetValue(player, out var playerAbility))
            return;

        var ability = playerAbility.FirstOrDefault(x => x.Data.AbilityType == type);

        if (ability == null)
            return;

        DisableAbilitySafely(ability);
        PlayerAbilities[player].Remove(ability);

        RemoveSynergyAbility(PlayerAbilities[player]);
    }

    public void RemoveAbility(Player player, Ability ability)
    {
        if (ability == null)
            return;

        if (!PlayerAbilities.TryGetValue(player, out var playerAbility))
            return;

        if (!playerAbility.Contains(ability))
            return;

        DisableAbilitySafely(ability);
        PlayerAbilities[player].Remove(ability);

        RemoveSynergyAbility(PlayerAbilities[player]);
    }

    // 플레이어로부터 시너지 능력 확인 후 제거
    private void RemoveSynergyAbility(List<Ability> abilities)
    {
        foreach (var synergy in SynergyAbilities)
        {
            if (!synergy.Value.All(req => abilities.Any(a => a.Data.AbilityType == req)) &&
                abilities.Any(a => a.Data.AbilityType == synergy.Key))
            {
                var ability = abilities.First(a => a.Data.AbilityType == synergy.Key);
                DisableAbilitySafely(ability);
                abilities.Remove(ability);
            }
        }
    }

    private static void DisableAbilitySafely(Ability ability)
    {
        if (ability == null)
            return;

        try
        {
            ability.OnDisabled();
        }
        catch (Exception e)
        {
            Log.Error($"Failed to disable ability {ability.Data?.AbilityType}: {e}");
        }
    }

    public void CleanupPlayer(Player player)
    {
        if (player == null)
            return;

        if (PlayerAbilities.TryGetValue(player, out var abilities))
        {
            foreach (var ability in abilities.ToList())
                DisableAbilitySafely(ability);

            PlayerAbilities.Remove(player);
        }

        PlayerWorkstations.Remove(player);
        Selections.Remove(player);
        IsSelecting.Remove(player);
        IsLifeUsed.Remove(player);
        LastDeathRoles.Remove(player);
        SelectionCursor.Remove(player);
    }

    // 플레이어로부터 모든 능력 제거
    public void RemoveAllAbilities(Player player)
    {
        if (!PlayerAbilities.TryGetValue(player, out var playerAbility))
            return;

        foreach (var ability in playerAbility.Where(x => x.Data.Category != AbilityCategory.Ancient).ToList())
            DisableAbilitySafely(ability);

        playerAbility.RemoveAll(x => x.Data.Category != AbilityCategory.Ancient);

        if (playerAbility.Count == 0)
            PlayerAbilities.Remove(player);
    }

    // 플레이어의 모든 능력 가져오기
    public List<Ability> GetAbilities(Player player)
    {
        return PlayerAbilities.TryGetValue(player, out var playerAbility) ? playerAbility : new List<Ability>();
    }

    public AbilityType FindAbility(string name)
    {
        return Abilities.FirstOrDefault(x => x.Value.Name == name).Key;
    }

    // 플레이어의 특정 능력 가져오기
    public Ability GetAbility(Player player, AbilityType type)
    {
        return GetAbilities(player).FirstOrDefault(x => x.Data.AbilityType == type);
    }

    // 플레이어가 특정 능력을 가지고 있는지 확인
    public bool HasAbility(Player player, AbilityType type)
    {
        return GetAbility(player, type) != null;
    }

    public List<AbilityType> GetRandomAbilities(Player player, AbilityCategory category, int count,
        IEnumerable<AbilityType> exceptTypes = null, RoleAbility roleAbility = RoleAbility.None,
        bool _79Allowed = false)
    {
        if (category == AbilityCategory.Ancient && player.HasAbility(AbilityType.SYNERGY_BLACKMARKET))
            return [];

        var abilities = Abilities
            .Where(x => x.Value.Category == category)
            .Where(x =>
            {
                var conditionAttr = x.Value.Type.GetCustomAttribute<ConditionAbilityAttribute>();
                return conditionAttr == null || conditionAttr.Abilities.All(player.HasAbility);
            })
            .Where(x => x.Value.RoleAbility == roleAbility ||
                        (roleAbility != RoleAbility.None && x.Value.RoleAbility.IsFactionRoleFor(player)))
            .ToList();


        if (player.Role == RoleTypeId.Scp079)
        {
            abilities = Abilities
                .Where(x => (x.Value._79Allowed
                             || x.Value.RoleAbility == RoleAbility.Scp079)
                            && x.Value.Category == category)
                .Where(x =>
                {
                    var conditionAttr = x.Value.Type.GetCustomAttribute<ConditionAbilityAttribute>();
                    return conditionAttr == null || conditionAttr.Abilities.All(player.HasAbility);
                })
                .ToList();
        }

        if (category == AbilityCategory.Dummy)
            abilities = Abilities.ToList();

        if (abilities.Count <= 0)
            return [];

        if (exceptTypes != null)
        {
            var excludedAbilityTypes = exceptTypes.ToHashSet();
            abilities = abilities.Where(x => !excludedAbilityTypes.Contains(x.Key)).ToList();
        }

        abilities.ShuffleList();

        var result = new List<AbilityType>();
        for (int i = 0; i < count; i++)
        {
            var picked = abilities.GetRandomValue().Key;
            result.Add(picked);
        }

        return result;
    }

    public void StartSelect(Player player, List<AbilityType> abilities = null, int count = 3)
    {
        if (CurrentExtraModes.Contains("수저"))
        {
            switch (Convert.ToByte(Random.Range(1, 4)))
            {
                case 3:
                    count = 5;
                    break;
                case 2:
                    count = 4;
                    break;
                case 1:
                    count = 3;
                    break;
            }
        }

        if (!Selections.ContainsKey(player))
            Selections.Add(player, []);

        lock (_selectionLock)
        {
            IsSelecting[player] = true;
        }

        var category = GetCategory(player);

        if (category == AbilityCategory.Dummy)
            return;

        int roleAbilityChance = GetRoleAbilityChance(category);

        abilities ??= GetRandomAbilities(player, category, count);
        var ignoredIndexes = new List<int>();

        if (abilities.Count == 0)
            return;

        if (player.HasAbility(AbilityType.RARE_TRANSITION))
        {
            player.RemoveAbility(AbilityType.RARE_TRANSITION);

            var transition = Convert.ToByte(Random.Range(1, 101)) <= 4 * player.AbilityCount(AbilityType.NORMAL_HEREDITY) +
                (CurrentExtraModes.Contains("잔칫상") ? 40 : 25);

            if (transition)
            {
                abilities = GetRandomAbilities(player, AbilityCategory.Epic, 5);
                category = AbilityCategory.Epic;
                player.AddAbility(AbilityType.DUMMY_RARETRANSITIONSUCCESS);
            }
            else
                player.AddAbility(AbilityType.DUMMY_RARETRANSITIONFAILURE);
        }

        if (player.HasAbility(AbilityType.EPIC_TRANSITION))
        {
            player.RemoveAbility(AbilityType.EPIC_TRANSITION);

            var transition = Convert.ToByte(Random.Range(1, 101)) <= 4 * player.AbilityCount(AbilityType.NORMAL_HEREDITY) + 
                (CurrentExtraModes.Contains("잔칫상") ? 40 : 25);

            if (transition)
            {
                abilities = GetRandomAbilities(player, AbilityCategory.Legend, 5);
                category = AbilityCategory.Legend;
                player.AddAbility(AbilityType.DUMMY_EPICTRANSITIONSUCCESS);
            }
            else
                player.AddAbility(AbilityType.DUMMY_EPICTRANSITIONFAILURE);
        }

        if (player.HasAbility(AbilityType.LEGEND_TRANSITION))
        {
            player.RemoveAbility(AbilityType.LEGEND_TRANSITION);

            var transition = Convert.ToByte(Random.Range(1, 101)) <= 4 * player.AbilityCount(AbilityType.NORMAL_HEREDITY) + 
                (CurrentExtraModes.Contains("잔칫상") ? 40 : 25);

            if (transition)
            {
                abilities = GetRandomAbilities(player, AbilityCategory.Mythic, 5);
                category = AbilityCategory.Mythic;
                player.AddAbility(AbilityType.DUMMY_LEGENDTRANSITIONSUCCESS);
            }
            else
                player.AddAbility(AbilityType.DUMMY_LEGENDTRANSITIONFAILURE);
        }

        lock (_selectionLock)
        {
            Selections[player] = abilities;
            SelectionCursor[player] = 0;
        }

        if (Convert.ToByte(Random.Range(1, 101)) <= roleAbilityChance) // 전용 능력
        {
            int index;

            do
                index = Convert.ToSByte(Random.Range(0, 3));
            while (ignoredIndexes.Contains(index));

            ignoredIndexes.Add(index);

            List<AbilityCategory> exceptCategory =
            [
                AbilityCategory.Synergy,
                AbilityCategory.Dummy,
                AbilityCategory.None
            ];

            if (player.HasAbility(AbilityType.SYNERGY_BLACKMARKET))
                exceptCategory.Add(AbilityCategory.Ancient);

            var ability =
                GetRandomAbilities(
                    player,
                    player.HasAbility(AbilityType.SYNERGY_BLACKMARKET)
                        ? Tools.EnumToList<AbilityCategory>()
                            .Where(a => !exceptCategory.Contains(a) && a >= category)
                            .GetRandomValue()
                        : category,
                    1,
                    roleAbility: player.HasAbility(AbilityType.SYNERGY_BLACKMARKET)
                        ? Tools.EnumToList<RoleAbility>().GetRandomValue()
                        : player.GetRoleAbility()
                ).FirstOrDefault();


            if (ability != AbilityType.NONE)
                abilities[index] = ability;
        }

        if (player.HasAbility(AbilityType.RARE_SCP079_DUPLICATION))
        {
            player.RemoveAbility(AbilityType.RARE_SCP079_DUPLICATION);
            player.AddAbility(AbilityType.DUMMY_DONEDUPLICATION);
            if (abilities.Count > 0)
            {
                var firstAbility = abilities[0];
                for (int i = 0; i < abilities.Count; i++)
                {
                    abilities[i] = firstAbility;
                }
            }
        }

        if (abilities.Distinct().Count() == 1 &&
            abilities.Count > 2 &&
            abilities.All(ability =>
                Abilities[ability].Category != AbilityCategory.Ancient)) // 능력 선택창에 등장한 능력이 최소 3개 이상이고, 중복인 경우
        {
            player.AddAbility(AbilityType.SYNERGY_DUPLICATEFATE);

            foreach (var ability in abilities)
            {
                player.AddAbility(ability);
            }
        }

        // 다음 타자, 코루틴!!!
        Timing.RunCoroutine(SelectionCoroutine(player));
    }

    private IEnumerator<float> SelectionCoroutine(Player player)
    {
        var abilities = Selections[player];

        for (var i = 0; i < 100; i++)
        {
            lock (_selectionLock)
            {
                if (player.IsDead || !Selections.ContainsKey(player))
                {
                    Selections.Remove(player);
                    SelectionCursor.Remove(player);
                    IsSelecting[player] = false;
                    player.AddHint("능력 선택", "", 0.2f);

                    yield break;
                }
            }

            var text = BuildSelectionText();
            player.AddHint("능력 선택",
                $"""
                 <align=left><size=40><b>능력 선택창ㅣ{SelectFormat[CheckAbilityGrade(text)]} ({(100 - i) / 5})</b></size>

                 <size=30>{text}</size>

                 <size=25><b>위/아래 키로 선택 후, Enter 키로 확정하세요.</b></size>
                 <size=20><color=#bcbcbc><i>[ESC] -> [Settings] -> [Server-specific]</i></color></size></align>





                 """,
                1f);

            yield return Timing.WaitForSeconds(0.2f);
        }

        AbilityType selectedAbility;

        lock (_selectionLock)
        {
            if (!Selections.TryGetValue(player, out var currentAbilities) || currentAbilities.Count == 0)
                yield break;

            selectedAbility = currentAbilities[Random.Range(0, currentAbilities.Count)];
            Selections.Remove(player);
            SelectionCursor.Remove(player);
            IsSelecting[player] = false;
        }

        player.AddHint("능력 선택", "", 0.2f);
        player.AddAbility(selectedAbility);

        yield break;

        bool HolidayFormat(AbilityType type, out string result)
        {
            result = "";

            switch (Abilities[type].HolidayType)
            {
                case AbilityHolidayType.Halloween:
                    result =
                        "<b><color=#FF9500>[</color><color=#FF9F09>H</color><color=#FFA912>A</color><color=#FFB31B>L</color><color=#FFBD24>L</color><color=#FFC72E>O</color><color=#FFDC37>W</color><color=#FFF240>E</color><color=#FFFF49>EE</color><color=#FFFF52>N</color><color=#FFFF5C>]</color></b>";
                    return true;
                case AbilityHolidayType.Christmas:
                    result =
                        "<b><color=#FC0000>[</color><color=#EA1300>C</color><color=#D82600>h</color><color=#C63900>r</color><color=#B44C00>i</color><color=#A25F00>s</color><color=#917200>t</color><color=#7F8500>m</color><color=#6D9800>a</color><color=#5BAB00>s</color><color=#49BE00>]</color></b>";
                    return true;
                default:
                    return false;
            }
        }

        string CheckAbilityGrade(string text)
        {
            if (text.Contains("일반")) return "일반";
            if (text.Contains("희귀")) return "희귀";
            if (text.Contains("영웅")) return "영웅";
            if (text.Contains("전설")) return "전설";
            if (text.Contains("신화")) return "신화";
            if (text.Contains("고대")) return "고대";

            return "알 수 없음";
        }

        string BuildSelectionText()
        {
            if (!SelectionCursor.ContainsKey(player))
                SelectionCursor[player] = 0;

            int cursor = SelectionCursor[player];
            if (abilities.Count > 0)
                cursor = Math.Max(0, Math.Min(cursor, abilities.Count - 1));

            return string.Join("\n", abilities.Select((x, i) =>
            {
                string prefix = i == cursor ? "▶ " : "   ";
                return
                    $"{prefix}[{i + 1}] {x.GetTranslation()}\n<size=20>{(HolidayFormat(x, out string result) ? $"{result} " : "")}{Abilities[x].Description}</size>\n";
            }));
        }
    }

    public static AbilityCategory GetCategory(Player player, bool allowAncient = true)
    {
        if (!player.IsAlive) return AbilityCategory.Dummy;

        var random = Convert.ToUInt16(Random.Range(1, 20001)); // 0.005 단위
        var hasBlackMarket = player.HasAbility(AbilityType.SYNERGY_BLACKMARKET);

        if (CurrentExtraModes.Contains("잔칫상"))
        {
            return random switch
            {
                <= 2 when allowAncient && !hasBlackMarket => AbilityCategory.Ancient, // 0.010
                <= 30 => AbilityCategory.Mythic, // 0.150
                <= 140 => AbilityCategory.Legend, // 0.700
                <= 1970 => AbilityCategory.Epic, // 9.850
                <= 7716 => AbilityCategory.Rare, // 38.580
                _ => AbilityCategory.Normal // 50.710
            };
        }

        return random switch
        {
            1 when allowAncient && !hasBlackMarket => AbilityCategory.Ancient, // 0.005
            <= 10 => AbilityCategory.Mythic, // 0.050
            <= 50 => AbilityCategory.Legend, // 0.250
            <= 1150 => AbilityCategory.Epic, // 5.750
            <= 6370 => AbilityCategory.Rare, // 31.850
            _ => AbilityCategory.Normal // 62.405
        };
    }

    private static byte GetRoleAbilityChance(AbilityCategory category)
    {
        return category switch
        {
            AbilityCategory.Ancient => 40,
            AbilityCategory.Mythic => 30,
            AbilityCategory.Legend => 22,
            AbilityCategory.Epic => 15,
            AbilityCategory.Rare => 10,
            AbilityCategory.Normal => 5,
            _ => 5
        };
    }

    public bool Select(Player player, int index, out string response)
    {
        AbilityType ability;

        lock (_selectionLock)
        {
            if (!Selections.TryGetValue(player, out var abilities) || abilities.Count == 0)
            {
                response = "선택할 수 있는 능력이 없습니다.";
                return false;
            }

            if (index < 1 || index > abilities.Count)
            {
                response = $"{index}번에 할당된 능력이 존재하지 않습니다.";
                return false;
            }

            Log.Info("Select called with " + player.Nickname + " and " + index);

            // 첫 입력이 선택권을 즉시 소비하도록 처리해 다음 프레임에 도착한 입력을 차단한다.
            ability = abilities[index - 1];
            Selections.Remove(player);
            SelectionCursor.Remove(player);
            IsSelecting[player] = false;
        }

        if (!AddAbility(player, ability))
        {
            response = "해당 능력은 획득할 수 없습니다.";
            return false;
        }

        player.AddHint("능력 선택", "", 0.1f);
        response = $"{index}번 능력 선택 완료!";
        return true;
    }

    public void MoveSelectionCursor(Player player, int delta)
    {
        if (!Selections.TryGetValue(player, out var abilities) || abilities.Count == 0)
            return;

        lock (_cursorLock) // 교차 동기화 방지
        {
            if (!SelectionCursor.ContainsKey(player))
                SelectionCursor[player] = 0;

            int cursor = SelectionCursor[player];
            cursor = (cursor + delta) % abilities.Count;

            if (cursor < 0)
                cursor += abilities.Count;

            SelectionCursor[player] = cursor;
        }

        PlayersAudio[player].TryPlay("Select");
    }

    public bool ConfirmSelectionByCursor(Player player, out string response)
    {
        lock (_cursorLock)
        {
            if (!Selections.TryGetValue(player, out var abilities) || abilities.Count == 0)
            {
                response = "선택할 수 있는 능력이 없습니다.";
                return false;
            }

            if (!SelectionCursor.ContainsKey(player))
                SelectionCursor[player] = 0;

            PlayersAudio[player].TryPlay("SelectConfirm", 1.5f);

            int cursor = Math.Max(0, Math.Min(SelectionCursor[player], abilities.Count - 1));
            return Select(player, cursor + 1, out response);
        }
    }

    public void EnsurePlayer(Player player)
    {
        if (player == null)
            return;

        if (!PlayerWorkstations.ContainsKey(player))
            PlayerWorkstations.Add(player, []);

        if (!PlayerAbilities.ContainsKey(player))
            PlayerAbilities.Add(player, []);

        if (!Selections.ContainsKey(player))
            Selections.Add(player, []);

        if (!IsSelecting.ContainsKey(player))
            IsSelecting.Add(player, false);

        if (!IsLifeUsed.ContainsKey(player))
            IsLifeUsed.Add(player, false);
    }

    public void Reset(Player player)
    {
        EnsurePlayer(player);

        player.RemoveAllAbilities();

        if (PlayerWorkstations.ContainsKey(player))
            PlayerWorkstations[player].Clear();

        lock (_selectionLock)
        {
            if (PlayerWorkstations.ContainsKey(player))
                IsSelecting[player] = false;
            SelectionCursor.Remove(player);
        }

        if (PlayerWorkstations.ContainsKey(player))
            IsLifeUsed[player] = false;
    }

    public static void ApplyPrelude(Player player)
    {
        if (CurrentExtraModes.Contains("골드 전주곡"))
        {
            if (player.IsNonePlayer()) return;

            player.AddAbility(Instance.GetRandomAbilities(player, AbilityCategory.Epic, 1,
            [
                AbilityType.EPIC_PRIEST, AbilityType.EPIC_RAMBO, AbilityType.EPIC_SUICIDEBOMBER,
                AbilityType.EPIC_TERRORISTREMAINS, AbilityType.EPIC_SCP127, AbilityType.EPIC_SCP1509,
                AbilityType.EPIC_CSAT, AbilityType.EPIC_RANDOMCHEST, AbilityType.EPIC_GRAVEROBBER
            ]).First());
        }
        else if (CurrentExtraModes.Contains("프리즘 전주곡"))
        {
            if (player.IsNonePlayer()) return;
            var prismrand = Convert.ToByte(Random.Range(1, 101));

            AbilityCategory GetRandom()
            {
                return prismrand <= 15
                    ? prismrand == 7 ? AbilityCategory.Mythic : AbilityCategory.Legend
                    : AbilityCategory.Epic;
            }

            player.AddAbility(Instance.GetRandomAbilities(player, GetRandom(), 1,
            [
                AbilityType.EPIC_PRIEST, AbilityType.LEGEND_RESURRECTION, AbilityType.EPIC_GRAVEROBBER
            ]).First());
        }

        if (player.Role.Type == RoleTypeId.Scp096)
        {
            Timing.CallDelayed(Timing.WaitForOneFrame, () =>
            {
                player.AddAbility(AbilityType.NORMAL_RABBIT);
                player.AddAbility(AbilityType.NORMAL_RABBIT);
            });
        }
    }
}

public static class ABattleExtensions
{
    public static void AddAbility(this Player player, AbilityType type)
    {
        ABattle.Instance.AddAbility(player, type);
    }

    public static void RemoveAbility(this Player player, AbilityType type)
    {
        ABattle.Instance.RemoveAbility(player, type);
    }

    public static void RemoveAbility(this Player player, Ability ability)
    {
        ABattle.Instance.RemoveAbility(player, ability);
    }

    public static void RemoveAllAbilities(this Player player)
    {
        ABattle.Instance.RemoveAllAbilities(player);
    }

    public static List<Ability> GetAbilities(this Player player)
    {
        return ABattle.Instance.GetAbilities(player);
    }

    public static Ability GetAbility(this Player player, AbilityType type)
    {
        return ABattle.Instance.GetAbility(player, type);
    }

    public static bool HasAbility(this Player player, AbilityType type)
    {
        return ABattle.Instance.HasAbility(player, type);
    }

    public static bool IsCaptured(this Player player, out Player anchorOwner) //[신화] 구속에 의해 붙잡혔는지 확인
    {
        foreach (var p in PlayerManager.List)
        {
            if (p == player) continue;

            Ability enemyAnchor = ABattle.Instance.GetAbility(p, AbilityType.MYTHIC_ANCHOR);
            if (enemyAnchor == null) continue;
            if (enemyAnchor is not Abilities.Mythic.Anchor { TargetPlayer: not null } anchor) continue;
            if (!anchor.TargetPlayer.Contains(player)) continue;

            anchorOwner = p;
            return true;
        }

        anchorOwner = null;
        return false;
    }
}