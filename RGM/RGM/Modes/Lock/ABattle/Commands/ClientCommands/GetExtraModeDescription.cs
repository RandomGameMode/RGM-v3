using System;
using System.Diagnostics.CodeAnalysis;
using CommandSystem;
using Exiled.API.Features;

namespace RGM.Modes.Commands;

public class GetExtraMode : ICommand
{
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, [UnscopedRef] out string response)
    {
        ABattle.ExtraModeNotion(Player.Get(sender), false);

        response = "워크스테이션 업그레이드 모드의 추가 모드 조회에 성공하였습니다.";
        return true;
    }

    public string Command { get; } = "추가모드";
    public string[] Aliases { get; } = Array.Empty<string>();
    public string Description { get; } = "워크스테이션 업그레이드ㅣ추가 모드를 확인합니다.";
}