using CommandSystem;
using RGM.Modes.Lock.TFT;
using Exiled.API.Features;
using System;
using System.Collections.Generic;

namespace RGM.Modes.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class StartSelect : ICommand
{
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        Player player = Player.Get(sender);

        TFTBattle.StartUpgrade(new List<Player> { player });

        response = "선택창을 시작합니다.";
        return true;
    }

    public string Command { get; } = "startselect";

    public string[] Aliases { get; } = { "ss" };

    public string Description { get; } = "DAONㅣ지정한 유저를 위한 선택을 시작합니다.";
}