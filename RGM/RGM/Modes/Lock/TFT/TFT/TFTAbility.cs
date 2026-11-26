using Exiled.API.Features;
using PlayerRoles;
using System;
using System.Collections.Generic;
using System.Linq;
using static RGM.Modes.Lock.TFT.Variables.Base;

namespace DAONTFT.Core.TFT;

public abstract class TFTAbility
{
    public virtual void OnEnabled() { } // 뭐... 여기에 이상한거 넣으면 호출되니... 하면.. 안되겠... 안되겠죠...?

    public virtual void OnDisabled() { } // 뭐... 여기에 이상한거 넣으면 호출되니... 하면.. 안되겠... 안되겠죠...?

    public TFTAbilityData Data { get; set; }
    public Player Owner { get; set; }
}

public class TFTAbilityData
{
    public Type Type { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Emoji { get; set; } = "🎲";
    public TFTAbilityLevel Level { get; set; }
    public TFTAbilityCategory Category { get; set; }
    public TFTAbilityPoint Point { get; set; }
    public TFTAbilityType TFTAbilityType { get; set; }

    public string GetFormattedName()
    {
        return $"<color={Level.GetColor()}>[{Level.GetTranslation()}]</color> {Emoji} {Name}";
    }
}

[AttributeUsage(AttributeTargets.Class)]
public class TFTAbilityAttribute(
    string name,
    string description,
    TFTAbilityLevel level,
    TFTAbilityCategory category,
    TFTAbilityPoint point,
    TFTAbilityType type,
    string emoji = "🎲") : Attribute
{
    public string Name { get; } = name;
    public string Description { get; } = description;
    public string Emoji { get; set; } = emoji;
    public TFTAbilityLevel Level { get; } = level;
    public TFTAbilityCategory Category { get; } = category;
    public TFTAbilityPoint Point { get; } = point;
    public TFTAbilityType Type { get; set; } = type;
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class RequiresTFTAbilityAttribute(params TFTAbilityType[] abilities) : Attribute
{
    public TFTAbilityType[] Abilities { get; } = abilities;
}

public static class TFTAbilityLevelExtensions
{
    public static string GetTranslation(this TFTAbilityLevel category)
    {
        return category switch
        {
            TFTAbilityLevel.Safe => "Safe",
            TFTAbilityLevel.Euclid => "Euclid",
            TFTAbilityLevel.Keter => "Keter",
            _ => "?"
        };
    }

    public static string GetColor(this TFTAbilityLevel category)
    {
        return category switch
        {
            TFTAbilityLevel.Safe => "#80f537",
            TFTAbilityLevel.Euclid => "#f4ee38",
            TFTAbilityLevel.Keter => "#f43838",
            _ => "white"
        };
    }
}

public static class PlayerExtensions
{
    public static int TFTAbilityCount(this Player player, TFTAbilityType TFTAbilityType)
    {
        return PlayerTFTAbilities[player].Count(x => x.Data.TFTAbilityType == TFTAbilityType);
    }

    public static Dictionary<TFTAbilityData, int> GetValidAbilities(this Player player, TFTAbilityLevel TFTAbilityLevel)
    {
        Dictionary<TFTAbilityData, int> abilities = new();

        void add(TFTAbilityCategory TFTAbilityCategory)
        {
            foreach (var TFTAbility in TFTAbilities.Values.Where(x =>
                         x.Level == TFTAbilityLevel && x.Category == TFTAbilityCategory))
            {
                if (player.GetAbilities().Count(x => x.Data != null && x.Data.Type == TFTAbility.Type) >
                    0 || // 중복되는 증강은 얻을 수 없습니다.
                    (TFTAbility.Point == TFTAbilityPoint.ALT && player.GetAbilities()
                            .Count(x => x.Data != null && x.Data.Point == TFTAbilityPoint.ALT) >
                        0) || // ALT를 사용하는 증강은 중복이 불가합니다.
                    (Selections.ContainsKey(player) &&
                     Selections[player].ContainsKey(TFTAbility.TFTAbilityType))) // 선택지에 있는 증강은 나타나지 않습니다.
                    continue;

                abilities.Add(TFTAbility, 1);
            }
        }

        if (player.Role.Type == RoleTypeId.Scp079)
        {
            add(TFTAbilityCategory.Scp079);

            return abilities;
        }
        else
        {
            add(TFTAbilityCategory.All);

            add(player.IsScp ? TFTAbilityCategory.Scp : TFTAbilityCategory.Human);

            switch (player.Role.Type)
            {
                case RoleTypeId.ClassD:
                    add(TFTAbilityCategory.ClassD);
                    return abilities;
                case RoleTypeId.Scientist:
                    add(TFTAbilityCategory.Scientist);
                    return abilities;
                case RoleTypeId.FacilityGuard:
                    add(TFTAbilityCategory.FacilityGuard);
                    return abilities;
                case RoleTypeId.NtfPrivate:
                case RoleTypeId.NtfSergeant:
                case RoleTypeId.NtfSpecialist:
                case RoleTypeId.NtfCaptain:
                    add(TFTAbilityCategory.NTF);
                    return abilities;
                case RoleTypeId.ChaosConscript:
                case RoleTypeId.ChaosMarauder:
                case RoleTypeId.ChaosRepressor:
                case RoleTypeId.ChaosRifleman:
                    add(TFTAbilityCategory.CHI);
                    return abilities;
                case RoleTypeId.Tutorial:
                    add(TFTAbilityCategory.Tutorial);
                    return abilities;
                case RoleTypeId.Scp173:
                    add(TFTAbilityCategory.Scp173);
                    return abilities;
                case RoleTypeId.Scp049:
                    add(TFTAbilityCategory.Scp049);
                    return abilities;
                case RoleTypeId.Scp0492:
                    add(TFTAbilityCategory.Scp0492);
                    return abilities;
                case RoleTypeId.Scp096:
                    add(TFTAbilityCategory.Scp096);
                    return abilities;
                case RoleTypeId.Scp106:
                    add(TFTAbilityCategory.Scp106);
                    return abilities;
                case RoleTypeId.Scp939:
                    add(TFTAbilityCategory.Scp939);
                    return abilities;
                case RoleTypeId.Scp3114:
                    add(TFTAbilityCategory.Scp3114);
                    return abilities;
                default: return abilities;
            }
        }
    }
}

public static class TFTAbilityTypeExtensions
{
    public static TFTAbilityData GetData(this TFTAbilityType type)
    {
        if (!TFTAbilities.TryGetValue(type, out var TFTAbility))
            return null;

        return TFTAbility;
    }
}

public enum TFTAbilityLevel
{
    None,
    Safe,
    Euclid,
    Keter
}

public enum TFTAbilityCategory
{
    None,
    All, // 모든 진영
    Human, // 인간 전용
    Scp, // SCP 전용
    ClassD,
    Scientist,
    FacilityGuard,
    NTF,
    CHI,
    Tutorial,
    Scp173,
    Scp049,
    Scp0492,
    Scp096,
    Scp106,
    Scp939,
    Scp3114,
    Scp079
}

public enum TFTAbilityPoint
{
    None,
    Once, // 일회성 능력
    Continuous, // 지속적인 적용되는 능력
    ALT // [ALT] 키를 눌렀을 때 발동되는 능력 (쿨타운 있음)
}

public enum TFTAbilityType
{
    None,

    // Safe
    // All
    AddHealth1, // 운동Ⅰ
    AddSpeed1, // 경공Ⅰ
    AddPower1, // 단련Ⅰ
    AddDefense1, // 방어Ⅰ
    AddJump1, // 종아리Ⅰ
    AddQuick1, // 순발력Ⅰ
    BomberMan, // 봄버맨
    TransparentCloak, // 투명 망토
    // Human
    RemoteSteal, // 슬쩍하기
    Miniaturization1, // 소인화 · 입문
    TrickOrTreat, // 트릭 오어 트릿
    InventoryUpgrade, // 간이 914
    Seed1, // 꿈나무
    Kick, // 태권도
    // Scp
    // SCP-079
    Generator1, // 발전기Ⅰ
    Charger, // 간이 충전기
    Repair, // 수리 업체
    Overload, // 과부화
    Freedom, // 자유
    JustPrice, // 응당한 대가
    AirStrike, // 폭격
    SystemHacking, // 시스템 해킹

    // Euclid
    // All
    AddHealth2, // 운동Ⅱ
    AddSpeed2, // 경공Ⅱ
    AddPower2, // 단련Ⅱ
    AddDefense2, // 방어Ⅱ
    AddJump2, // 종아리Ⅱ
    AddQuick2, // 순발력Ⅱ
    QuickJudgement, // 빠른 판단
    // Human
    AmmoBox, // 탄약고
    Survivor, // 생존 전문가
    Vampire, // 흡혈귀
    Armory1, // 무기고
    IllegalWeapon, // 폭탄병
    Seed2, // 꿈나무+
    Peace, // 평화주의자
    Ambush, // 매복자
    AdhesivePlaster, // 반창고
    AntiScp207, // 초재생
    Scp1853, // 무기 전문가
    Scp207, // 스피드왜건
    Miniaturization2, // 소인화 · 숙련
    // Scp
    FindLocation, // 본능
    // SCP-049
    GoodSense, // 날카로운 촉
    Call, // 심부름
    // SCP-079
    Generator2, // 발전기Ⅱ
    BlackoutZone, // 어두컴컴
    RoomLockdown, // 폐소공포증
    AutoTesla, // 자동 방어 시스템
    ResolveProblem, // 문제 해결사
    Charger2, // 간이 충전기+
    // SCP-096
    OutSider, // 아웃사이더
    Seer, // 천리안
    StarTearing, // 별자리 찢기
    Charge, // 5톤 트럭
    Enrage, // 분노 조절 문제
    Enraging, // 지구력
    // SCP-106
    Capture, // 잡았다 요놈
    Swamp, // 끈적이는 늪
    // SCP-173
    Blink, // 눈 깜빡할 사이에
    Scare, // 괴이
    Mirage, // 신기루
    // SCP-939
    Attack, // 안아줘요
    AmnesticCloud, // 망각 안개
    Mimicry, // 흉내쟁이
    // SCP-3114
    Disguise, // 변장술사
    Half, // 반블럭

    // Keter
    // All
    AddHealth3, // 운동Ⅲ
    AddSpeed3, // 경공Ⅲ
    AddPower3, // 단련Ⅲ
    AddDefense3, // 방어Ⅲ
    AddJump3, // 종아리Ⅲ
    AddQuick3, // 순발력Ⅲ
    Ghost, // 유령화
    Fortunate, // 행운아
    Diver, // 잠수부
    // Human
    DevilContract, // 악마와의 계약
    CandyAddict, // 마약 중독자
    Gambler, // 도박꾼
    Armory2, // 무기고+
    Adventure, // 고귀한 모험
    Bullseye, // 불스아이
    InfiniteAmmo, // 판도라의 인벤토리
    Miniaturization3, // 소인화 · 통달
    // Scp
    TestSubject, // 실험체
    // SCP-049
    BestDoctor, // 명의
    // SCP-079
    Generator3, // 발전기Ⅲ
    CameraFlash, // 카메라 플래시
    RestArea, // 휴게소
    PingRemote, // 휴대용 정전기
    Shutdown, // 셧다운제
    RandomFunction, // 랜덤 함수
    ResolveProblem2, // 문제 해결사+
    // SCP-096
    RoaringSound, // 괴성
}