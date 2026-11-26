using System;
using System.Diagnostics.CodeAnalysis;
using CommandSystem;
using Exiled.API.Features;
using RGM.Modes.Lock.TFT;

namespace RGM.Modes.Lock.TFT.Commands.ClientCommands.basicfeatures;

public class RerollTFTFirst : ICommand
{
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, [UnscopedRef] out string response)
    {
        var player = Player.Get(sender);

        if (player == null)
        {
            response = "플레이어를 찾을 수 없습니다.";
            return false;
        }

        try
        {
            var result = TFTBattle.Reroll(player, 1, out response);

            return result;
        }
        catch (ArgumentOutOfRangeException)
        {
            response = "1번에 할당된 능력이 존재하지 않습니다.";
            return false;
        }
    }

    public string Command { get; } = "r1";
    public string[] Aliases { get; } = Array.Empty<string>();
    public string Description { get; } = "DAONㅣ1번 능력 리롤";
}

public class RerollTFTSecond : ICommand
{
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, [UnscopedRef] out string response)
    {
        var player = Player.Get(sender);

        if (player == null)
        {
            response = "플레이어를 찾을 수 없습니다.";
            return false;
        }

        try
        {
            var result = TFTBattle.Reroll(player, 2, out response);

            return result;
        }
        catch (ArgumentOutOfRangeException)
        {
            response = "2번에 할당된 능력이 존재하지 않습니다.";
            return false;
        }
    }

    public string Command { get; } = "r2";
    public string[] Aliases { get; } = Array.Empty<string>();
    public string Description { get; } = "DAONㅣ2번 능력 리롤";
}