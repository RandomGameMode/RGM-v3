using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using PlayerRoles;
using RGM.API.Features;

namespace RGM.Modes;

public abstract class Ability
{
    public virtual void OnEnabled() { } // 뭐... 여기에 이상한거 넣으면 호출되니... 하면.. 안되겠... 안되겠죠...?

    public virtual void OnDisabled() { } // 뭐... 여기에 이상한거 넣으면 호출되니... 하면.. 안되겠... 안되겠죠...?

    public AbilityData Data { get; set; }
    public Player Owner { get; set; }
}

public sealed class AddingAbilityEventArgs(
    Player player,
    AbilityType abilityType,
    int reflectorChain,
    bool allowReflector,
    int extraReflectorChain) : System.EventArgs
{
    public AddingAbilityEventArgs(Player player, AbilityType abilityType)
        : this(player, abilityType, 0, true, 0)
    {
    }

    public Player Player { get; } = player;
    public AbilityType AbilityType { get; } = abilityType;
    public int ReflectorChain { get; } = reflectorChain;
    public bool AllowReflector { get; } = allowReflector;
    public int ExtraReflectorChain { get; } = extraReflectorChain;
    public bool IsAllowed { get; set; } = true;
}

public abstract class EffectAbility : Ability
{
    public abstract Dictionary<EffectType, byte> EffectTypes { get; }

    public override void OnEnabled()
    {
        foreach (var effect in EffectTypes)
        {
            Owner.AddEffect(effect.Key, effect.Value);
        }
    }

    public override void OnDisabled()
    {
        foreach (var effect in EffectTypes)
        {
            Owner.RemoveEffect(effect.Key, effect.Value);
        }
    }
}

public abstract class ItemAbility : Ability
{
    public abstract ItemType ItemType { get; }
    public abstract int Amount { get; }

    public override void OnEnabled()
    {
        Owner.AddItem(ItemType, Amount);
    }

    public override void OnDisabled()
    {
        for (int i = 0; i < Amount; i++)
        {
            var item = Owner.Items.FirstOrDefault(x => x.Type == ItemType);

            if (item == null)
                break;

            Owner.RemoveItem(item);
        }
    }
}

public class AbilityData
{
    public Type Type { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public AbilityCategory Category { get; set; }
    public AbilityType AbilityType { get; set; }
    public AbilityHolidayType HolidayType { get; set; }
    public List<AbilityType> Requires { get; set; }
    public bool Keep { get; set; }
    public bool _79Allowed { get; set; }
    public RoleAbility RoleAbility { get; set; }

    public string GetFormattedName()
    {
        string CategoryName = Category.GetCategoryTranslation();
        string Text = "전용";
        return $"<color={Category.GetColor()}>[{(RoleAbility == RoleAbility.None ? CategoryName : $"{Text} {CategoryName}")}]</color> {Name}";
    }
}

[AttributeUsage(AttributeTargets.Class)]
public class AbilityAttribute(string name, string description, AbilityCategory category, AbilityType type, RoleAbility roleAbility = RoleAbility.None, bool _79Allowed = false, AbilityHolidayType holidayType = AbilityHolidayType.None, bool keep = false) : Attribute
{
    public string Name { get; } = name;
    public string Description { get; } = description;
    public AbilityCategory Category { get; } = category;
    public AbilityType Type { get; set; } = type;
    public bool _79Allowed { get; } = _79Allowed;
    public RoleAbility RoleAbility { get; } = roleAbility;
    public AbilityHolidayType HolidayType { get; set; } = holidayType;
    public bool Keep { get; set; } = false;
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class RequiresAbilityAttribute(params AbilityType[] abilities) : Attribute
{
    public AbilityType[] Abilities { get; } = abilities;
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ConditionAbilityAttribute(AbilityType[] abilities, AbilityType[] blocks) : Attribute
{
    public AbilityType[] Abilities { get; } = abilities;
    public AbilityType[] BlockAbilities { get; } = blocks;
}

public enum AbilityCategory
{
    None,
    Dummy,
    Normal,
    Rare,
    Epic,
    Legend,
    Mythic,
    Ancient,
    Synergy,
}

public enum RoleAbility
{
    None,
    ClassD,
    Scientist,
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
    Scp079,
    Flamingo,
    Human,
    Scp
}
public static class AbilityCategoryExtensions
{
    public static string GetCategoryTranslation(this AbilityCategory category)
    {
        return category switch
        {
            AbilityCategory.Normal => "일반",
            AbilityCategory.Rare => "희귀",
            AbilityCategory.Epic => "영웅",
            AbilityCategory.Legend => "전설",
            AbilityCategory.Mythic => "신화",
            AbilityCategory.Ancient => "고대",
            AbilityCategory.Synergy => "시너지",
            _ => "?"
        };
    }
    
    public static string GetColor(this AbilityCategory category)
    {
        return category switch
        {
            AbilityCategory.Normal => "#A4A4A4",
            AbilityCategory.Rare => "#2ECCFA",
            AbilityCategory.Epic => "#BF40BF",
            AbilityCategory.Legend => "#FFC000",
            AbilityCategory.Mythic => "#FF2400",
            AbilityCategory.Ancient => "#008000",
            AbilityCategory.Synergy => "#DEEFED",
            _ => "white"
        };
    }
}

public static class RoleAbilityExtensions
{
    public static bool IsFactionRoleFor(this RoleAbility roleAbility, Player player)
    {
        return roleAbility switch
        {
            RoleAbility.Human => player.Role.Type.IsHuman(),
            RoleAbility.Scp => player.Role.Type.IsScp(),
            _ => false
        };
    }

    public static RoleAbility GetRoleAbility(this Player player)
    {
        RoleTypeId role = player.Role.Type;
        return role switch
        {
            RoleTypeId.ClassD => RoleAbility.ClassD,
            RoleTypeId.Scientist => RoleAbility.Scientist,
            RoleTypeId.FacilityGuard => RoleAbility.NTF,
            RoleTypeId.NtfPrivate => RoleAbility.NTF,
            RoleTypeId.NtfSergeant => RoleAbility.NTF,
            RoleTypeId.NtfCaptain => RoleAbility.NTF,
            RoleTypeId.NtfSpecialist => RoleAbility.NTF,
            RoleTypeId.ChaosRifleman => RoleAbility.CHI,
            RoleTypeId.ChaosMarauder => RoleAbility.CHI,
            RoleTypeId.ChaosRepressor => RoleAbility.CHI,
            RoleTypeId.ChaosConscript => RoleAbility.CHI,
            RoleTypeId.Scp173 => RoleAbility.Scp173,
            RoleTypeId.Scp049 => RoleAbility.Scp049,
            RoleTypeId.Scp0492 => RoleAbility.Scp0492,
            RoleTypeId.Scp096 => RoleAbility.Scp096,
            RoleTypeId.Scp106 => RoleAbility.Scp106,
            RoleTypeId.Scp939 => RoleAbility.Scp939,
            RoleTypeId.Scp3114 => RoleAbility.Scp3114,
            RoleTypeId.Scp079 => RoleAbility.Scp079,
            RoleTypeId.Flamingo => RoleAbility.Flamingo,
            RoleTypeId.AlphaFlamingo => RoleAbility.Flamingo,
            RoleTypeId.ChaosFlamingo => RoleAbility.Flamingo,
            RoleTypeId.NtfFlamingo => RoleAbility.Flamingo,
            RoleTypeId.ZombieFlamingo => RoleAbility.Flamingo,
            _ => RoleAbility.None
        };
    }
}

public static class PlayerExtensions 
{
    public static int AbilityCount(this Player player, AbilityType abilityType)
    {
        return ABattle.Instance.PlayerAbilities[player].Count(x => x.Data.AbilityType == abilityType);
    }
}

public enum AbilityHolidayType
{
    None,
    Christmas,
    Halloween,
}

public enum AbilityType
{
    NONE,

    // 더미 //
    DUMMY_EXPIREDINSURANCE, // [더미] 만료된 보험
    DUMMY_DOPAMINERELEASED, // [더미] 방출된 도파민
    DUMMY_TESTSUCCESS, // [더미] 시험 성공
    DUMMY_TESTFAILURE, // [더미] 시험 실패
    DUMMY_USEDADHESIVEPLASTER, // [더미] 해진 반창고
    DUMMY_RARETRANSITIONSUCCESS, // [더미] 하급 변이 성공
    DUMMY_RARETRANSITIONFAILURE, // [더미] 하급 변이 실패
    DUMMY_EPICTRANSITIONSUCCESS, // [더미] 변이 성공
    DUMMY_EPICTRANSITIONFAILURE, // [더미] 변이 실패
    DUMMY_LEGENDTRANSITIONSUCCESS, // [더미] 상급 변이 성공
    DUMMY_LEGENDTRANSITIONFAILURE, // [더미] 상급 변이 실패
    DUMMY_USEDPINGHOOK, // [더미] 핑 갈?고리
    DUMMY_BACKEDUP, // [더미] 백업됨
    DUMMY_COLDSTEW, // [더미] 식은 스튜
    DUMMY_NOAFK, // [더미] 자리 다비움
    DUMMY_USEDSNAKEHANDRADIO, // [더미] 뱀의 손 수장
    DUMMY_TELEPORTED, // [더미] 순간이동됨
    DUMMY_FINALEXAMSUCCESS, // [더미] 기말고사 수석
    DUMMY_FINALEXAMFAIL, // [더미] 기말고사 낙제
    DUMMY_CSTCSUCCESS, // [더미] 대학수학능력시험 1등급
    DUMMY_CSTCFAIL, // [더미] 대학수학능력시험 9등급
    DUMMY_INFILTRATIONSUCCESS, // [더미] 침투 성공
    DUMMY_INFILTRATIONFAIL, // [더미] 침투 실패
    DUMMY_INFORMATIONLEAK, // [더미] 개인 정보 유출
    DUMMY_DONEDUPLICATION, // [더미] 인공 중복기연
    DUMMY_REBIRTHCOMPLETE, // [더미] 새로운 삶
    DUMMY_GOCMEMBER, // [더미] U.N.G.O.C 대원
    DUMMY_ALPHAONEMENBER, // [더미] ALPHA-1 대원

    // 일반 //
    NORMAL_WORKOUT, // [일반] 운동
    NORMAL_SWIFT, // [일반] 경공
    NORMAL_EVOLUTION, // [일반] 진화
    NORMAL_TRAINING, // [일반] 단련
    NORMAL_LUCKY, // [일반] 행운
    NORMAL_STAMINAREPLENISHMENT, // [일반] 체력 보충
    NORMAL_RANDOMBOX, // [일반] 랜덤박스
    NORMAL_INSURANCE, // [일반] 보험
    NORMAL_KICK, // [일반] 회축
    NORMAL_SUPPLY, // [일반] 보급
    NORMAL_PURIFICATION, // [일반] 정화
    NORMAL_TORCH, // [일반] 횃불
    NORMAL_SNEAK, // [일반] 잠행
    NORMAL_ESCAPE, // [일반] 위기 탈출
    NORMAL_FRIENDSHIP, // [일반] 우애
    NORMAL_RAINBOW, // [일반] 무지개
    NORMAL_BODYBACK, // [일반] 바디백
    NORMAL_DOPAMINE, // [일반] 도파민
    NORMAL_TEST, // [일반] 시험
    NORMAL_AGILITY, // [일반] 민첩
    NORMAL_REROLL, // [일반] 리롤
    NORMAL_SUSPICIOUSSTEW, // [일반] 수상한 스튜
    NORMAL_HEALGUN, // [일반] 치유 사제
    NORMAL_MILK, // [일반] 우유
    NORMAL_RUSH, // [일반] 황소
    NORMAL_EXCHANGE, // [일반] 교환
    NORMAL_RABBIT, // [일반] 토끼뜀
    NORMAL_RANDOMCANDY, // [일반] 트릭 오어 트릿
    NORMAL_CLOAK, // [일반] 망토
    NORMAL_NIGHTOWL, // [일반] 밤눈

    // 희귀 //
    RARE_PHYSICALSTRENGTHENING, // [희귀] 육체 강화
    RARE_STEELSHELL, // [희귀] 강철 껍질
    RARE_TRANSPARENTCLOAK, // [희귀] 투명 망토
    RARE_VAMPIRE, // [희귀] 흡혈귀
    RARE_TELEPORTATION, // [희귀] 순간이동
    RARE_BOMBERMAN, // [희귀] 봄버맨
    RARE_STOPWATCH, // [희귀] 회중시계
    RARE_ADRENALINE, // [희귀] 아드레날린
    RARE_MARTYRDOM, // [희귀] 순교
    RARE_HYPASS, // [희귀] 하이패스
    RARE_TRIPLEAXEL, // [희귀] 트리플악셀
    RARE_COLLECTOR, // [희귀] 수집가
    RARE_ADHESIVEPLASTER, // [희귀] 반창고
    RARE_WEAPONEXPERT, // [희귀] 무기 전문가
    RARE_PANACEA, // [희귀] 만병통치약
    RARE_SALAMANDRA, // [희귀] 불의 정령, 살라만드라
    RARE_UNDINE, // [희귀] 물의 정령, 운디네
    RARE_GNOME, // [희귀] 흙의 정령, 노움
    RARE_SYLPH, // [희귀] 바람의 정령, 실프
    RARE_CONTRACT, // [희귀] 계약
    RARE_TRANSITION, // [희귀] 하급 변이
    RARE_UPGRADE, // [희귀] 강화
    RARE_DND, // [희귀] 자리 비움
    RARE_SPACETRAVEL, // [희귀] 이차원 도약
    RARE_ORGANICMILK, // [희귀] 유기농 우유
    RARE_CANDYBAG, // [희귀] 사탕 봉지
    RARE_DOBBYISFREE, // [희귀] 도비는 자유에요
    RARE_FINALEXAM, // [희귀] 기말고사
    RARE_BULLSEYE, // [희귀] 불스아이
    RARE_HYPERBODY, // [희귀] 하이퍼 바디
    RARE_CLAYMORE, // [희귀] Claymore
    RARE_SAVELOCATION, // [희귀] 위치 저장

    // 영웅 //
    EPIC_TERRORISTREMAINS, // [영웅] 테러리스트의 유품
    EPIC_RANDOMCHEST, // [영웅] 랜덤상자
    EPIC_REPAIRMAN, // [영웅] 수리 기사
    EPIC_SUPERSTAR, // [영웅] 슈퍼 스타
    EPIC_LUCKYVIKEY, // [영웅] 럭키비키
    EPIC_EXTREMEPOISON, // [영웅] 극독
    EPIC_SURVIVOR, // [영웅] 구사일생
    EPIC_GHOSTRULE, // [영웅] 고스트룰
    EPIC_DIVER, // [영웅] 잠수부
    EPIC_BLINK, // [영웅] 점멸
    EPIC_TRANSITION, // [영웅] 변이
    EPIC_SUICIDEBOMBER, // [영웅] 수어사이드 봄버맨
    EPIC_DISGUISE, // [영웅] 위장술
    EPIC_GRAVEROBBER, // [영웅] 도굴꾼
    EPIC_FALLENKINGSSWORD, // [영웅] 몰락한 왕의 검
    EPIC_SCP1344, // [영웅] 투시
    EPIC_PRIEST, // [영웅] 성직자
    EPIC_ADDWORKSTATION, // [영웅] 업무 증가
    EPIC_MADSCIENTIST, // [영웅] 매드 사이언티스트
    EPIC_SCP127, // [영웅] 인생의 동반자
    EPIC_ANTISCP207, // [영웅] 초재생
    EPIC_FOODRESEARCHER, // [영웅] 요리 연구가,
    EPIC_SCP1509, // [영웅] 마체테
    EPIC_MARSHMELLOW, // [영웅] !!마쉬멜로우!!
    EPIC_CONTEXPERT, // [영웅] 격리 전문가
    EPIC_RAMBO, // [영웅] 람보
    EPIC_SPRINGFIELDM1A, // [영웅] Springfield M1A
    EPIC_CSTC, // [영웅] 대학수학능력시험
    EPIC_HOLYPROTECTION, // [영웅] 신성방어
    EPIC_AN94, // [영웅] AN-94
    EPIC_SHARPEYES, // [영웅] 샤프 아이즈
    EPIC_TURTLE, // [영웅] 거북 도사
    EPIC_CHAINLIGHTNING, // [영웅] 체인 라이트닝

    // 전설 //
    LEGEND_SPEEDWAGON, // [전설] 스피드왜건
    LEGEND_SNAKEHANDRADIO, // [전설] 뱀의 손 무전기
    LEGEND_RANDOMPACKAGE, // [전설] 랜덤택배
    LEGEND_MAGICIAN, // [전설] 마술사
    LEGEND_FLASHLIGHT, // [전설] 플래시라이트
    LEGEND_KILLSTREAK, // [전설] 킬스트릭
    LEGEND_SCREAM, // [전설] 괴성
    LEGEND_TRANSITION, // [전설] 상급 변이
    LEGEND_CANDYADDICT, // [전설] 마약 중독자
    LEGEND_REFLECTOR, // [전설] 반사경
    LEGEND_CATACLYSMGENERATOR, // [전설] 대격변 생성기
    LEGEND_LAVACHICKEN, // [전설] La-La-La Lava Ch-Ch-Ch Chicken
    LEGEND_FLAMETHROWER, // [전설] 화염 방사기
    LEGEND_OTHERWORLDLIGHT, // [전설] 이계의 빛
    LEGEND_CANDYPOWER, // [전설] 섬뜩한 힘
    LEGEND_JOHNWICK, // [전설] 존 윅
    LEGEND_REPLICATION, // [전설] 복제
    LEGEND_CLEARCACHE, // [전설] 캐시 청소
    LEGEND_REINCARNATION, // [전설] 리인카네이션
    LEGEND_ZERORULE, // [전설] 현을 푸는 제 0법칙
    LEGEND_GAMBLER, // [전설] 도박사
    LEGEND_RESURRECTION, // [전설] 리저렉션
    LEGEND_UNLIMITEDAMMO, // [전설] 무한 탄환

    // 신화 //
    MYTHIC_ROCKETLAUNCHER, // [신화] 로켓 런처
    MYTHIC_SPIRIT, // [신화] 스피릿
    MYTHIC_EYEMAN, // [신화] 눈빛맨
    MYTHIC_DIMENSIONTHIEF, // [신화] 차원 강탈자
    MYTHIC_JOKER, // [신화] 조커
    MYTHIC_BOMBGUN, // [신화] 워 머신
    MYTHIC_WARGOD, // [신화] 광전사
    MYTHIC_BALLISTAEM3, // [신화] 발리스타 MP3
    MYTHIC_TOOLGUN, // [신화] 툴건
    MYTHIC_KINGSCOLOR, // [신화] 패왕색 패기
    MYTHIC_ROSEHIP, // [신화] 장미칼
    MYTHIC_HAMMER, // [신화] 철퇴
    MYTHIC_UNLIMITED, // [신화] 무제한
    MYTHIC_ANCHOR, //[신화] 구속
    MYTHIC_SOLDIER76, // [신화] 솔져: 76

    // 고대 //
    ANCIENT_ALEPHONE, // [고대] Aleph-1
    ANCIENT_EXPLOSIVEAMMO, // [고대] Anti Matter
    ANCIENT_SATELLITE, // [고대] Satellite Attack
    
    
    // 전용 //
    // 인간진영 공통
    NORMAL_HUMAN_DENYMESSAGE, // [전용 일반] 수신 차단
    
    RARE_HUMAN_MEDICALOFFICER, // [전용 희귀] 의무병
    
    EPIC_HUMAN_REBIRTH, // [전용 영웅] 환생
    EPIC_HUMAN_URGENTSUPPORT, // [전용 영웅] 긴급 지원
    
    LEGEND_HUMAN_SCP008, // [전용 전설] SCP-008, 좀비 전염병
    LEGEND_HUMAN_SCP035, // [전용 전설] SCP-035, 빙의 가면
    LEGEND_HUMAN_SCP457, // [전용 전설] SCP-457, 불타는 남자
    LEGEND_HUMAN_SCP966, // [전용 전설] SCP-966, 잠을 죽이는 자
    LEGEND_HUMAN_SCP999, // [전용 전설] SCP-999, 간지럼 괴물
    
    // D계급
    NORMAL_CLASSD_LARCENY, // [전용 일반] 절도죄
    NORMAL_CLASSD_SEEDSOFCHI, // [전용 일반] 반란의 씨앗
    
    RARE_CLASSD_TRESPASSING, // [전용 희귀] 주거침입죄
    RARE_CLASSD_ILLEGALWEAPON, // [전용 희귀] 불법개조무기소지죄
    RARE_CLASSD_CHAOSTICKET, // [전용 희귀] CHAOS 이용권
    RARE_CLASSD_CLASSDSPEEDRUN, // [전용 희귀] 스피드런
    
    EPIC_CLASSD_CHAOSRECRUIT, // [전용 영웅] 징집
    

    // 과학자
    NORMAL_SCIENTIST_ENGINEERINGMAJOR, // [전용 일반] 공학 전공
    NORMAL_SCIENTIST_SEEDSOFMTF, // [전용 일반] 특무부대의 씨앗
    NORMAL_SCIENTIST_05, // [전용 일반] 05 평의회

    RARE_SCIENTIST_SCIENTISTSPEEDRUN, // [전용 희귀] 스피드런
    RARE_SCIENTIST_NTFTICKET, // [전용 희귀] NTF 이용권
    
    EPIC_SCIENTIST_NTFRECRUIT, // [전용 영웅] 모집
    
    // NTF
    NORMAL_NTF_HEALTHCENTERSTAFF, // [전용 일반] 보건소 직원
    NORMAL_NTF_INDUSTRIALACCIDENTINSURANCE, // [전용 희귀] 산업재해보험
    NORMAL_NTF_RADAR, // [전용 희귀] 레이더
    
    RARE_NTF_MANAGERIALOBLIGATIONPERSON, // [전용 희귀] 관리 의무자
    
    LEGEND_NTF_UNGOC, // [전용 전설] U.N.G.O.C

    // 혼돈의 반란
    NORMAL_CHI_TOUCHOFCHAOS, // [전용 일반] 혼돈의 손길
    
    RARE_CHI_CHAOSOFCHAOS, // [전용 희귀] 혼돈의 카오스
    
    LEGEND_CHI_ALPHAONE, // [전용 전설] ALPHA-1, Red Right Hand

    // 뱀의 손
    NORMAL_TUTORIAL_TONGUE, // [전용 일반] 세치 혀
    NORMAL_TUTORIAL_THIRDFORCE, // [전용 일반] 제3세력
    NORMAL_TUTORIAL_RESEARCHER, // [전용 희귀] SCP 연구자

    // SCP-173
    NORMAL_SCP173_FEAR, // [전용 일반] 공포
    NORMAL_SCP173_ABERRATION, // [전용 일반] 괴이
    
    RARE_SCP173_MIRAGE, // [전용 희귀] 신기루
    RARE_SCP173_IMMENSEWEIGHT, // [전용 희귀] 육중한 무게

    LEGEND_SCP173_DEBRIS, // [전용 전설] 파편

    MYTHIC_SCP173_COMPULSION, // [전용 신화] 강박증

    // SCP-049
    NORMAL_SCP049_COMPETENTDOCTOR, // [전용 일반] 유능한 의사
    NORMAL_SCP049_DEATH, // [전용 일반] 사신
    
    RARE_SCP049_MEDICALKNIFE, // [전용 희귀] 메스
    RARE_SCP049_PROFICIENCY, // [전용 희귀] 능수능란
    
    EPIC_SCP049_MEDICALACCIDENT, // [전용 영웅] 의료 사고
    EPIC_SCP049_PLAGUECURSE, // [전용 영웅] 역병 저주
    
    LEGEND_SCP049_CONTAGION, // [전용 전설] 전염병
    LEGEND_SCP049_MUTATION, // [전용 전설] 돌연변이
    
    MYTHIC_SCP049_PANDEMIC, // [전용 신화] PANDEMIC

    // SCP-0492
    NORMAL_SCP0492_CONFUSION, // [전용 일반] 당혹감
    NORMAL_SCP0492_INFECTION, // [전용 일반] 감염
    
    RARE_SCP0492_HUNGER, // [전용 희귀] 허기
    RARE_SCP0492_MEALS, // [전용 희귀] 급식
    RARE_SCP0492_SHIELD, // [전용 희귀] 보호막
    
    EPIC_SCP0492_MINIPLAGUEDOCTOR, // [전용 영웅] 작은 역병 의사
    
    LEGEND_SCP0492_GROWTH, // [전용 전설] 성장
    
    MYTHIC_SCP0492_ONEPUNCH, // [전용 신화] ONE PUNCH MAN

    // SCP-096
    NORMAL_SCP096_ENEMY, // [전용 일반] 원수
    NORMAL_SCP096_CANTMANAGEANGER, // [전용 일반] 분노 조절 문제
    NORMAL_SCP096_RAGE, // [전용 일반] 격노
    
    RARE_SCP096_SEER, // [전용 희귀] 천리안
    
    EPIC_SCP096_STARTEARING, // [전용 영웅] 별자리 찢기
    EPIC_SCP096_RAGINGATTACK, // [전용 영웅] 분노의 일격
    
    MYTHIC_SCP096_BERSERK, // [전용 신화] 광분

    // SCP-106
    NORMAL_SCP106_RECOVERY, // [전용 일반] 회춘
    NORMAL_SCP106_HUNTINGPREY, // [전용 일반] 사냥감 모색
    
    RARE_SCP106_STICKYSWAMP, // [전용 희귀] 끈적한 늪
    RARE_SCP106_DIGESTION, // [전용 희귀] 소화
    RARE_SCP106_ENERGIZER, // [전용 희귀] 에너자이저
    
    EPIC_SCP106_RETURN, // [전용 영웅] 회귀
    EPIC_SCP106_EVADE, // [전용 영웅] 긴급 탈출

    LEGEND_SCP106_FLASHBACK, // [전용 전설] 회상
    
    MYTHIC_SCP106_REMINISCENCE, // [전용 신화] 회고 
    
    // SCP-939
    NORMAL_SCP939_HUGME, // [전용 일반] 그 시절 댕댕이
    NORMAL_SCP939_NOEYES, // [전용 일반] 실명
    
    RARE_SCP939_REINFORCECLAW, // [전용 희귀] 발톱 강화
    RARE_SCP939_VAMPIRECLAW, // [전용 희귀] 흡혈 발톱
    RARE_SCP939_SHARPNESS, // [전용 희귀] 연마
    RARE_SCP939_BLEEDING, // [전용 희귀] 출혈
    
    EPIC_SCP939_AMNESIA, // [전용 영웅] 기억 소거
    
    LEGEND_SCP939_EARTHQUAKE, // [전용 전설] 지진

    // SCP-3114
    NORMAL_SCP3114_HALFBLOCK, // [전용 일반] 반블럭
    NORMAL_SCP3114_SKILLEDASSASSIN, // [전용 희귀] 숙련된 암살자
    NORMAL_SCP3114_DORAEMONPOCKET, // [전용 희귀] 도라에몽 주머니
    NORMAL_SCP3114_SHOWMANSHIP, // [전용 희귀] 쇼맨쉽

    // SCP-079
    NORMAL_SCP079_PINGREMOTE, // [전용 일반] 핑 리모컨
    //NORMAL_SCP079_PORTABLECHARGER, // [전용 일반] 간이 충전기
    NORMAL_SCP079_RANDOMFUNCTION, // [전용 일반] 랜덤 함수
    NORMAL_SCP079_SHUTDOWN, // [전용 일반] 셧다운제
    NORMAL_SCP079_OVERCLOCKING, // [전용 일반] 오버클럭
    NORMAL_SCP079_JUSTPRICE, // [전용 일반] 응당한 대가
    NORMAL_SCP079_CAMERAFLASH, // [전용 일반] 카메라 플래시
    NORMAL_SCP079_CASSIE, // [전용 일반] C.A.S.S.I.E.
    NORMAL_SCP079_ATTACKORDER, // [전용 일반] 공격 명령
    NORMAL_SCP079_AUTOTESLA, // [전용 일반] 자동 방어 시스템(x)
    NORMAL_SCP079_WORKOUTORDER, // [전용 일반] 운동 명령
    NORMAL_SCP079_SPAMMESSAGE, // [전용 일반] 스팸 문자

    RARE_SCP079_OVERCURRENT, // [전용 희귀] 과전류(x)
    RARE_SCP079_OVERWHELMING, // [전용 희귀] 고대의 존재 압도
    RARE_SCP079_POWERABSORPTION, // [전용 희귀] 전력 흡수
    RARE_SCP079_PINGHOOK, // [전용 희귀] 핑 갈고리
    RARE_SCP079_AVOIDORDER, // [전용 희귀] 회피 명령
    RARE_SCP079_LOCKDOWN, // [전용 희귀] 봉쇄(x)
    RARE_SCP079_REPAIR, // [전용 희귀] 수리수리 마수리
    RARE_SCP079_RESTAREA, // [전용 희귀] 휴게소
    RARE_SCP079_FREEDOM, // [전용 희귀] 자유
    RARE_SCP079_MOBILESTRIKEFORCE, // [전용 희귀] 기동타격대
    RARE_SCP079_AIRSTRIKE, // [전용 희귀] 폭격
    RARE_SCP079_SYSTEMHACKING, // [전용 희귀] 시스템 해킹
    RARE_SCP079_HIDE, // [전용 희귀] 은폐
    RARE_SCP079_DUPLICATION, // [전용 희귀] 중복

    EPIC_SCP079_CALLSCP, // [전용 영웅] SCP 지원 호출기
    EPIC_SCP079_LEVELUP, //[전용 영웅] 만렙
    EPIC_SCP079_SWIFTSUPPORT, // [전용 영웅] 신속 지원
    EPIC_SCP079_IMPORTUNITY, // [전용 영웅] 끈질김
    EPIC_SCP079_SystemInfiltration, // [전용 영웅] 시스템 침투
    EPIC_SCP079_SURVIVALORDER, // [전용 영웅] 생존 명령
    EPIC_SCP079_BLESSING, // [전용 영웅] 가호
    EPIC_SCP079_SUICIDEORDER, // [전용 영웅] 희생 명령
    EPIC_SCP079_SURPRISEATTACK, // [전용 영웅] 기습
    EPIC_SCP079_PROTECTION, // [전용 영웅] 보호

    LEGEND_SCP079_STARTWARHEAD, // [전용 전설] 자폭 시퀸스
    LEGEND_SCP079_BLACKOUT, // [전용 전설] 블랙아웃
    LEGEND_SCP079_ASSULTORDER, // [전용 전설] 돌격 명령
    LEGEND_SCP079_EXPLOSIONISART, // [전용 전설] 폭발은 예술이다
    LEGEND_SCP079_VIRUS, // [전용 전설] 바이러스
    LEGEND_SCP079_SECURITYCAMERA, // [전용 전설] 감시 카메라

    MYTHIC_SCP079_TOOLPING, // [전용 신화] 따아알깍
    MYTHIC_SCP079_TRANSENDENCE, // [전용 신화] 초월
    MYTHIC_SCP079_SEVEREVIRUS, // [전용 신화] 치명적인 바이러스
    MYTHIC_SCP079_BACKDOOR, // [전용 신화] 백도어
    MYTHIC_SCP079_FUSIONBOMB, // [전용 신화] 융단 폭격


    //79가 먹을 수 있는 범용 능력
    /*
      시험
      봄버맨
      기말고사
      변이
      대학수학능력시험
      반사경
      상급변이
      복제
      대격변 생성기
      무제한
     */

    // 플라밍고
    NORMAL_FLAMINGO_MINIFACTORY, // [전용 일반] 미니 공장

    // 시너지 //
    SYNERGY_SURVIVALEXPERT, // [시너지] 생존 전문가
    SYNERGY_GLORY, // [시너지] 광휘
    SYNERGY_RANDOMCOLLECTION, // [시너지] 랜덤 컬렉션
    SYNERGY_FOURMAJOREXERCISES, // [시너지] 4대 운동
    SYNERGY_DUPLICATEFATE, // [시너지] 중복 기연
    SYNERGY_DRUID, // [시너지] 드루이드
    SYNERGY_SUICIDESQUAD, // [시너지] 수어사이드 스쿼드
    SYNERGY_ASSASSIN, // [시너지] 암살자
    SYNERGY_LOSER, // [시너지] 패배자
    SYNERGY_WINNER, // [시너지] 승리자
    SYNERGY_VAMPIRE, // [시너지] 뱀파이어,
    SYNERGY_GMAN, // [시너지] G맨
    SYNERGY_RICH1, // [시너지] 부자Ⅰ
    SYNERGY_RICH2, // [시너지] 부자Ⅱ
    SYNERGY_AFK, // [시너지] AFK
    SYNERGY_BOMBPARTY, // [시너지] 폭탄 파티
    SYNERGY_BLACKMARKET, // [시너지] 암시장
    SYNERGY_WEAKPOINTATTACK, // [시너지] 약점 공격
    SYNERGY_CLOWN, // [시너지] 광대
    SYNERGY_HEALER, // [시너지] 비숍
    SYNERGY_REFLECTEDLIGHT, // [시너지] 반사광
    SYNERGY_JUGGERNAUT, // [시너지] 저거너트
}

public static class AbilityTypeExtensions
{
    public static string GetTranslation(this AbilityType type)
    {
        var aBattle = ABattle.Instance;

        if (!aBattle.Abilities.TryGetValue(type, out var ability))
            return "?";

        return ability.GetFormattedName();
    }
}

public static class ABattleTerm
{
    public static readonly Dictionary<string, string> ABattleTerms = new Dictionary<string, string>
    {
        //공통
        ["피격 제한"] = """ • 피해를 받을 시, 피격 제한 이상의 값을 받을 수 없습니다.""",
        ["죽음에 이르는 공격"] = """ • 공격 시, 대상을 즉시 처치하며 상대의 상태이상 면역을 무시합니다.""",
        ["사망"] = """ • 공격 시, 대상을 즉시 처치하며, 상대의 상태이상 면역, 피격 제한, 무적 효과를 발동시키지 않습니다.""",
        ["관통"] = """ • 공격 시, 상대의 피격 제한, 반사 효과를 무시합니다.""",
        ["파열"] = """ • 공격 시, 상대의 피격 제한, 반사, 무적 효과를 무시합니다.""",
        ["상태이상 면역"] = """ • 상태이상 효과를 받지 않습니다. (일부 효과 제외)""",
        ["무장해제"] = """ • 무장 해제 시, 아이템을 장착할 수 없습니다.""",
        ["속박"] = """ • 속박 시, 이동이 불가합니다.""",
        ["기절"] = """ • 기절 시, 모든 행동을 할 수 없습니다.""",       

        //전용
        ["사회적 거리두기 · 봉쇄"] =
        """
        • 서로 간 간격이 6m 이내일 경우, 중독 및 스태미너 감소 효과 적용.
        • 초당 최대 체력%에 비례하여 지속 데미지
        """, 

        ["전염"] = """ • SCP-049의 F 스킬 적중 시 대상에게 심장마비 효과 부여, 해당 대상과 8m 이내에 있는 모든 대상들은 심장 마비 효과가 전염됨.""",
        ["집결"] = """ • 보유 시, SCP-049가 소생한 SCP-049-2는 SCP-049의 위치로 순간이동""",
        ["긴급 탈출"] = """ • 보유 시, SCP-106의 스킬 사용에 무적 적용""",
        ["소화"] = """ • 보유 시, 적 처치 당 최대 HP 증가""",
        ["불안정"] =
        """
        • 보유 시, SCP-939의 런지 공격에 피격 또는 범위 내에 있을 시, 3초간 기절, 이후 2초간 이동 속도 감소. 
        • 최대 HP의 70%의 피해
        • 10초간 기억 소거 · 강화 효과 적용
        """,
        ["기억 소거 · 강화"] = """ • 보유 시, 범위 내 상대는 SCP-939의 위치가 은폐되며, 의료 아이템을 사용할 수 없음.""",
        ["보호막"] = """ • 보유 시, 최대 HS와 HS 회복이 일정 수치만큼 증가."""


    };
}