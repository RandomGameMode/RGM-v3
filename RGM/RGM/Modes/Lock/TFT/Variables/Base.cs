using System.Collections.Generic;
using Exiled.API.Extensions;
using Exiled.API.Features;
using PlayerRoles;
using Hint = HintServiceMeow.Core.Models.Hints.Hint;

namespace RGM.Modes.Lock.TFT.Variables;

public static class Base
{
    public static RoleTypeId Encounter = RoleTypeId.None;

    public static readonly Dictionary<TFTAbilityType, TFTAbilityData> TFTAbilities = new();
    public static readonly Dictionary<Player, List<TFTAbility>> PlayerTFTAbilities = new();
    public static readonly Dictionary<Player, Dictionary<TFTAbilityType, int>> Selections = new();
    public static readonly Dictionary<Player, bool> IsSelecting = new();
    public static readonly Dictionary<Player, bool> IsLifeUsed = new();
    public static readonly Dictionary<Player, List<Hint>> PlayerHints = new();
    public static readonly Dictionary<Player, bool> PlayerShowTFTs = new();

    public static readonly Dictionary<string, (RoleTypeId, string)> Encounters = new()
    {
        { "조우자 없음", (RoleTypeId.None, "재단이 참 평화로운걸요?") },
        {
            "D계급",
            (RoleTypeId.ClassD,
                $"<color={RoleTypeId.ClassD.GetColor().ToHex()}>D계급</color>이 모두에게 죄수의 손길 기술을 알려주었군요..\n증강 간격이 40% 짧아집니다.")
        },
        {
            "과학자",
            (RoleTypeId.Scientist,
                $"<color={RoleTypeId.Scientist.GetColor().ToHex()}>과학자</color>가 탈모약을 개발하다가 실수로 엄청난 걸 개발했습니다.\n증강 간격이 55% 짧아집니다.")
        },
        {
            "시설 경비",
            (RoleTypeId.FacilityGuard,
                $"<color={RoleTypeId.FacilityGuard.GetColor().ToHex()}>시설 경비</color>는 복면을 그리워하는군요.\n증강 간격이 20% 짧아집니다.")
        },
        {
            "구미호 대위",
            (RoleTypeId.NtfCaptain,
                $"<color={RoleTypeId.NtfCaptain.GetColor().ToHex()}>구미호 대위</color>는 무능합니다.\n실수로 모두에게 <color=red>Keter</color> 증강을 뿌렸다는군요?")
        },
        {
            "구미호 하사",
            (RoleTypeId.NtfSergeant,
                $"<color={RoleTypeId.NtfSergeant.GetColor().ToHex()}>구미호 하사</color>는 대위가 너무 무능해서 화났습니다.\n그래서 모두에게 <color=yellow>Euclid</color> 증강을 지급합니다.")
        },
        {
            "구미호 상등병",
            (RoleTypeId.NtfSpecialist,
                $"과학자였을 때 탈출한 <color={RoleTypeId.NtfSpecialist.GetColor().ToHex()}>상등병</color>은 MTF 본부에 연락해서 지원을 요청했습니다.\n모두의 처음 증강이 <color=red>Keter</color>로 고정됩니다.")
        },
        {
            "구미호 이등병",
            (RoleTypeId.NtfPrivate,
                $"<color={RoleTypeId.NtfPrivate.GetColor().ToHex()}>구미호 이등병</color>은 생각했습니다. 언젠가 자신이 승급한다면..\n아무튼 모두의 마지막 증강이 <color=red>Keter</color>로 고정됩니다.")
        },
        {
            "반란 압제자",
            (RoleTypeId.ChaosRepressor,
                $"<color={RoleTypeId.ChaosRepressor.GetColor().ToHex()}>반란 압제자</color>는 존재만으로 흉기입니다.\n시작부터 모두에게 총기가 지급됩니다.")
        },
        {
            "반란 약탈자",
            (RoleTypeId.ChaosMarauder,
                $"<color={RoleTypeId.ChaosMarauder.GetColor().ToHex()}>반란 약탈자</color>는 생각보다 쓸모가 없습니다.\n시작부터 모두에게 수류탄 또는 섬광탄이 지급됩니다.")
        },
        {
            "반란 징집병",
            (RoleTypeId.ChaosConscript,
                $"<color={RoleTypeId.ChaosConscript.GetColor().ToHex()}>반란 징집병</color>은 귀엽습니다. 그냥 그렇다고요.\n시작부터 모두에게 무작위 SCP 아이템을 지급됩니다.")
        },
        {
            "반란 소총수",
            (RoleTypeId.ChaosRifleman,
                $"<color={RoleTypeId.ChaosRifleman.GetColor().ToHex()}>반란 소총수</color>는 굳이 전장에 나가기 싫어합니다.\n대신 여러분들에게 무작위 아이템을 준다네요.")
        },
        {
            "튜토리얼",
            (RoleTypeId.Tutorial,
                $"아무도 출처를 모르는 <color={RoleTypeId.Tutorial.GetColor().ToHex()}>튜토리얼</color>입니다.\n모두의 증강 리롤권을 5회로 늘립니다.")
        },
        {
            "SCP-049-2",
            (RoleTypeId.Scp0492,
                $"본래 SCP는 조우자로 개발되지 않았지만 <color={RoleTypeId.Scp0492.GetColor().ToHex()}>좀비</color>는 예외입니다. 하나 더 만들고 싶으니까요.\n모두의 최대 증강 개수가 8개로 조정됩니다.")
        }
    };
}