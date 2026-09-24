using Exiled.API.Features;
using Exiled.API.Features.Core.UserSettings;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UserSettings.ServerSpecific;

namespace RGM.Modes;

public static class EchoSetting
{
    private const int SettingIdStart = 3100;
    private const int InfoKeyId = 31001001;
    private const int WeaponSettingId = 3099;

    private const string NoneOption = "없음";
    private const string AutoOption = "자동 (Echo 기본)";

    private static readonly Dictionary<int, SettingMeta> Meta = new();
    // 클라이언트가 같은 설정값을 재동기화할 수 있으므로, 처리한 선택값을 기억합니다.
    private static readonly Dictionary<Player, Dictionary<int, string>> LastProcessedSelections = new();
    private static List<string> _weaponOptions;

    private static HeaderSetting Header { get; set; } = new(190031, "에코 전투");
    private static KeybindSetting InfoKey { get; set; }

    private enum EchoSlotKind
    {
        Main,
        Sub0,
        Sub1,
        Sub2,
        Sub3
    }

    private enum SettingKind
    {
        Echo,
        MainStat,
        Weapon
    }

    private class SettingMeta
    {
        public SettingKind Kind;
        public EchoSlotKind Slot;
        public int SlotIndex;
        public List<string> Options;
    }

    public static void Init()
    {
        Meta.Clear();
        LastProcessedSelections.Clear();

        var list = new List<SettingBase>();

        InfoKey = new KeybindSetting(
            id: InfoKeyId,
            label: "<b>에코 전투 정보 키</b>",
            suggested: KeyCode.F1,
            preventInteractionOnGUI: true,
            hintDescription: "힌트 표시를 토글합니다.",
            header: Header
        );
        list.Add(InfoKey);

        // 전용무기: 메인 Echo 슬롯 위
        _weaponOptions = BuildWeaponOptions().Prepend(NoneOption).ToList();
        list.Add(new DropdownSetting(
            id: WeaponSettingId,
            label: "<color=#ffcc66>전용무기</color>",
            hintDescription: BuildWeaponHintDescription(),
            options: _weaponOptions,
            header: Header
        ));
        Meta[WeaponSettingId] = new SettingMeta
        {
            Kind = SettingKind.Weapon,
            Options = _weaponOptions
        };

        int nextId = SettingIdStart;

        // 배치: Echo → 메인 스탯 → Echo → 메인 스탯 ...
        AddEchoAndStatPair(list, ref nextId, "<color=#f43838>메인 Echo</color>", EchoSlotKind.Main, 0);
        AddEchoAndStatPair(list, ref nextId, "<color=#88aaff>부가 Echo 1</color>", EchoSlotKind.Sub0, 1);
        AddEchoAndStatPair(list, ref nextId, "<color=#88aaff>부가 Echo 2</color>", EchoSlotKind.Sub1, 2);
        AddEchoAndStatPair(list, ref nextId, "<color=#88aaff>부가 Echo 3</color>", EchoSlotKind.Sub2, 3);
        AddEchoAndStatPair(list, ref nextId, "<color=#88aaff>부가 Echo 4</color>", EchoSlotKind.Sub3, 4);

        SettingBase.Register(list);
    }

    private static void AddEchoAndStatPair(List<SettingBase> list, ref int nextId, string echoLabel, EchoSlotKind slot, int slotIndex)
    {
        var echoOptions = BuildEchoOptions(slot).Prepend(NoneOption).ToList();
        int echoId = nextId++;
        list.Add(new DropdownSetting(
            id: echoId,
            label: echoLabel,
            hintDescription: BuildEchoHintDescription(slot),
            options: echoOptions,
            header: Header
        ));
        Meta[echoId] = new SettingMeta
        {
            Kind = SettingKind.Echo,
            Slot = slot,
            SlotIndex = slotIndex,
            Options = echoOptions
        };

        var statOptions = BuildMainStatOptions().Prepend(AutoOption).ToList();
        int statId = nextId++;
        list.Add(new DropdownSetting(
            id: statId,
            label: $"{echoLabel} 메인 스탯",
            hintDescription: BuildMainStatHintDescription(),
            options: statOptions,
            header: Header
        ));
        Meta[statId] = new SettingMeta
        {
            Kind = SettingKind.MainStat,
            Slot = slot,
            SlotIndex = slotIndex,
            Options = statOptions
        };
    }

    private static IEnumerable<string> BuildEchoOptions(EchoSlotKind slot)
    {
        foreach (var pair in EchoInfo.Echoes.OrderByDescending(x => (int)x.Value.Cost).ThenBy(x => x.Value.Name))
        {
            if (!IsEchoAllowedInSlot(slot, pair.Value))
                continue;

            yield return FormatEchoOption(pair.Value);
        }
    }

    private static IEnumerable<string> BuildMainStatOptions()
    {
        return EchoStats.GetAllSelectableMainStats().Select(FormatMainStatOption);
    }

    private static IEnumerable<string> BuildWeaponOptions()
    {
        return ExclusiveWeaponCore.GetAllOrdered().Select(FormatWeaponOption);
    }

    private static string BuildWeaponHintDescription()
    {
        var lines = ExclusiveWeaponCore.GetAllOrdered()
            .Select(x => $"• {x.Name}: {x.Description}");

        return string.Join("\n", lines) +
               "\n\n능력치 레벨 1~90 / 공진 1~5\n" +
               "공진은 전용 1회성 퀘스트 완료 시 자동 승급됩니다.";
    }

    private static string FormatWeaponOption(ExclusiveWeaponData data)
    {
        // 스펙: Server-Specific 선택창에 이모지 추가하지 않음
        return data.Name;
    }

    private static string BuildEchoHintDescription(EchoSlotKind slot)
    {
        var lines = EchoInfo.Echoes.Values
            .Where(x => IsEchoAllowedInSlot(slot, x))
            .OrderByDescending(x => (int)x.Cost)
            .Select(x => $"• {FormatEchoOption(x)}: {x.Description}");

        string slotLimit = slot == EchoSlotKind.Main
            ? "\n메인 슬롯은 Cost4/Cost3 Echo만 장착할 수 있습니다."
            : "";

        return string.Join("\n", lines) + $"\n\n최대 {EchoInfo.MaxEquippedEchoes}개 / 합산 Cost {EchoInfo.MaxTotalCost}{slotLimit}";
    }

    private static string BuildMainStatHintDescription()
    {
        return
            "장착한 Echo의 Cost에 맞는 메인 스탯을 고르세요.\n" +
            "• Cost4: 공격%/HP%/방어%/크리%/이속·점프/스태미나감소\n" +
            "• Cost3: 공격%/HP%/방어%/SCP데미지%/인간데미지%/헤드샷%/AHP·HS/크기감소\n" +
            "• Cost1: 공격%/HP%/방어%\n" +
            $"• '{AutoOption}'이면 Echo 기본 메인 스탯을 사용합니다.\n" +
            "• Cost에 없는 스탯을 고르면 적용되지 않습니다.";
    }

    private static string FormatEchoOption(EchoData data)
    {
        return $"<color={data.Cost.GetColor()}>[C{(int)data.Cost}]</color> {data.Emoji} {data.Name}";
    }

    private static string FormatMainStatOption(EchoMainStatType type)
    {
        return EchoStats.GetMainStatDisplayName(type);
    }

    private static bool IsEchoAllowedInSlot(EchoSlotKind slot, EchoData data)
    {
        if (data == null)
            return false;

        return slot != EchoSlotKind.Main
            || data.Cost == EchoCost.Cost4
            || data.Cost == EchoCost.Cost3;
    }

    public static void OnSSInput(ReferenceHub sender, ServerSpecificSettingBase setting)
    {
        Player player = Player.Get(sender);
        if (player == null)
            return;

        if (setting is SSKeybindSetting keybind && keybind.SyncIsPressed && keybind.SettingId == InfoKeyId)
        {
            bool currentlyShown = !EchoInfo.PlayerShowHints.TryGetValue(player, out bool shown) || shown;
            bool showStatus = !currentlyShown;
            EchoInfo.PlayerShowHints[player] = showStatus;
            EchoBattleCore.ShowNotification(
                player,
                $"에코 전투 상태창: <color={(showStatus ? "#7CFC00" : "#ff6666")}>{(showStatus ? "ON" : "OFF")}</color>",
                1.5f);
            return;
        }

        if (setting is not SSDropdownSetting dropdown)
            return;

        if (!Meta.TryGetValue(setting.SettingId, out var meta))
            return;

        if (!TryResolveSelection(dropdown, meta, out string selected))
            return;

        // ServerOnSettingValueReceived는 사용자의 변경 외에도 기존 선택값 재동기화로 호출될 수 있습니다.
        // 동일한 값은 이미 로드아웃에 반영되어 있으므로, 알림과 메인 스탯 초기화를 반복하지 않습니다.
        if (IsPreviouslyProcessedSelection(player, setting.SettingId, selected))
            return;

        if (!EchoInfo.PlayerLoadouts.ContainsKey(player))
            EchoInfo.PlayerLoadouts[player] = new EchoLoadout();

        var loadout = EchoInfo.PlayerLoadouts[player];

        if (meta.Kind == SettingKind.Echo)
            HandleEchoSelection(player, loadout, meta, selected);
        else if (meta.Kind == SettingKind.Weapon)
            HandleWeaponSelection(player, loadout, selected);
        else
            HandleMainStatSelection(player, loadout, meta, selected);
    }

    private static bool IsPreviouslyProcessedSelection(Player player, int settingId, string selected)
    {
        if (!LastProcessedSelections.TryGetValue(player, out var selections))
        {
            selections = new Dictionary<int, string>();
            LastProcessedSelections[player] = selections;
        }

        if (selections.TryGetValue(settingId, out string previous) && previous == selected)
            return true;

        selections[settingId] = selected;
        return false;
    }

    /// <summary>
    /// SyncSelectionText보다 SyncSelectionIndexRaw를 우선해 옵션을 확정합니다.
    /// 텍스트 불일치로 이전 스탯이 남는 문제를 방지합니다.
    /// </summary>
    private static bool TryResolveSelection(SSDropdownSetting dropdown, SettingMeta meta, out string selected)
    {
        selected = null;

        if (meta.Options is { Count: > 0 })
        {
            int index = dropdown.SyncSelectionIndexRaw;
            if (index >= 0 && index < meta.Options.Count)
            {
                selected = meta.Options[index];
                return true;
            }
        }

        selected = dropdown.SyncSelectionText;
        return !string.IsNullOrWhiteSpace(selected);
    }

    private static void HandleWeaponSelection(Player player, EchoLoadout loadout, string selected)
    {
        if (selected == NoneOption)
        {
            loadout.EquippedWeapon = null;
            EchoBattleCore.ShowNotification(player, "전용무기 해제", 2);
            return;
        }

        var match = ExclusiveWeaponInfo.Weapons.Values.FirstOrDefault(x => FormatWeaponOption(x) == selected);
        if (match == null)
            return;

        loadout.EquippedWeapon = match.WeaponType;
        var progress = ExclusiveWeaponInfo.GetOrCreateProgress(player);
        int level = progress.GetLevel(match.WeaponType);
        int resonance = progress.GetResonance(match.WeaponType);
        EchoBattleCore.ShowNotification(player, $"전용무기: {match.Name}  Lv.{level}  공진 {resonance}", 3);
    }

    private static void HandleEchoSelection(Player player, EchoLoadout loadout, SettingMeta meta, string selected)
    {
        EchoType? selectedType = null;

        if (selected != NoneOption)
        {
            var match = EchoInfo.Echoes.Values.FirstOrDefault(x => FormatEchoOption(x) == selected);
            if (match == null)
                return;

            if (!IsEchoAllowedInSlot(meta.Slot, match))
            {
                EchoBattleCore.ShowNotification(player, "<color=red>메인 슬롯에는 Cost4/Cost3 Echo만 장착할 수 있습니다.</color>", 3);
                return;
            }

            selectedType = match.EchoType;

            if (loadout.Contains(selectedType.Value) && GetCurrentSlotType(loadout, meta.Slot) != selectedType)
            {
                EchoBattleCore.ShowNotification(player, "<color=red>같은 Echo는 중복 장착할 수 없습니다.</color>", 3);
                return;
            }
        }

        var previous = GetCurrentSlotType(loadout, meta.Slot);
        SetSlot(loadout, meta.Slot, selectedType);

        if (loadout.GetEquippedCount() > EchoInfo.MaxEquippedEchoes
            || loadout.GetTotalCost() > EchoInfo.MaxTotalCost)
        {
            SetSlot(loadout, meta.Slot, previous);
            EchoBattleCore.ShowNotification(player, $"<color=red>제한 초과</color> (최대 {EchoInfo.MaxEquippedEchoes}개 / Cost {EchoInfo.MaxTotalCost})", 3);
            return;
        }

        // Echo 변경 시 서버 로드아웃의 메인 스탯만 초기화 (UI 강제 동기화는 크래시 유발 → 안내문으로 대체)
        loadout.SetSlotMainStat(meta.SlotIndex, null);
        loadout.SanitizeAllMainStats();

        int cost = loadout.GetTotalCost();
        int count = loadout.GetEquippedCount();

        if (!selectedType.HasValue)
        {
            EchoBattleCore.ShowNotification(player, $"Echo 장착: {count}/{EchoInfo.MaxEquippedEchoes} | Cost {cost}/{EchoInfo.MaxTotalCost}", 2);
            return;
        }

        var data = selectedType.Value.GetData();
        string defaultStat = data != null
            ? EchoStats.GetMainStatDisplayName(loadout.ResolveMainStat(meta.SlotIndex, data))
            : "?";
        EchoBattleCore.ShowNotification(
            player,
            $"Echo 장착: {count}/{EchoInfo.MaxEquippedEchoes} | Cost {cost}/{EchoInfo.MaxTotalCost}\n" +
            $"<color=#ffcc66>메인 스탯 드롭다운을 '{AutoOption}' 또는 원하는 스탯으로 다시 선택하세요.</color>\n" +
            $"(현재 적용: {defaultStat})",
            4);
    }

    private static void HandleMainStatSelection(Player player, EchoLoadout loadout, SettingMeta meta, string selected)
    {
        var echoType = GetCurrentSlotType(loadout, meta.Slot);
        if (!echoType.HasValue)
        {
            loadout.SetSlotMainStat(meta.SlotIndex, null);
            EchoBattleCore.ShowNotification(player, "<color=red>먼저 Echo를 장착하세요.</color>", 2);
            return;
        }

        var data = echoType.Value.GetData();
        if (data == null)
            return;

        if (selected == AutoOption)
        {
            loadout.SetSlotMainStat(meta.SlotIndex, null);
            var resolved = loadout.ResolveMainStat(meta.SlotIndex, data);
            EchoBattleCore.ShowNotification(player, $"메인 스탯: {EchoStats.GetMainStatDisplayName(resolved)} (기본)", 2);
            return;
        }

        EchoMainStatType? selectedStat = null;
        foreach (var type in EchoStats.GetAllSelectableMainStats())
        {
            if (FormatMainStatOption(type) == selected)
            {
                selectedStat = type;
                break;
            }
        }

        if (!selectedStat.HasValue)
        {
            // 알 수 없는 값이면 잔존 스탯을 비워 오적용 방지
            loadout.SetSlotMainStat(meta.SlotIndex, null);
            return;
        }

        if (!EchoStats.IsMainStatAvailable(data.Cost, selectedStat.Value))
        {
            // 거부 시 이전 값(AHP 등)을 유지하지 않고 초기화
            loadout.SetSlotMainStat(meta.SlotIndex, null);
            EchoBattleCore.ShowNotification(
                player,
                $"<color=red>{data.Cost.GetTranslation()}에서는 '{FormatMainStatOption(selectedStat.Value)}'을(를) 쓸 수 없습니다.</color>",
                3);
            return;
        }

        loadout.SetSlotMainStat(meta.SlotIndex, selectedStat.Value);
        EchoBattleCore.ShowNotification(player, $"메인 스탯: {FormatMainStatOption(selectedStat.Value)}", 2);
    }

    private static EchoType? GetCurrentSlotType(EchoLoadout loadout, EchoSlotKind kind)
    {
        return kind switch
        {
            EchoSlotKind.Main => loadout.MainSlot,
            EchoSlotKind.Sub0 => loadout.SubSlots[0],
            EchoSlotKind.Sub1 => loadout.SubSlots[1],
            EchoSlotKind.Sub2 => loadout.SubSlots[2],
            EchoSlotKind.Sub3 => loadout.SubSlots[3],
            _ => null
        };
    }

    private static void SetSlot(EchoLoadout loadout, EchoSlotKind kind, EchoType? type)
    {
        switch (kind)
        {
            case EchoSlotKind.Main:
                loadout.MainSlot = type;
                break;
            case EchoSlotKind.Sub0:
                loadout.SubSlots[0] = type;
                break;
            case EchoSlotKind.Sub1:
                loadout.SubSlots[1] = type;
                break;
            case EchoSlotKind.Sub2:
                loadout.SubSlots[2] = type;
                break;
            case EchoSlotKind.Sub3:
                loadout.SubSlots[3] = type;
                break;
        }
    }
}
