using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CommandSystem;
using RGM.API.Features;
using RGM.Variables;

namespace RGM.Modes.Abilities.Ancient.Commands.InnerCommand;

[CommandHandler(typeof(Sudo))]
public class Apt : ICommand
{
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, [UnscopedRef] out string response)
    {
        if (arguments.Count < 1)
        {
            response = """
                       모드를 추가합니다
                       Usage: apt <purge> [모드이름]
                       """;
            return false;
        }

        if (arguments.Count >= 2 && arguments.At(0) == "purge")
        {
            if (Variable.ModeList.Keys.Any(_ => ModeType.ABattle.GetModeData().Name == arguments.At(1)))
            {
                response = "";
                return false;
            }
            
            if (!Tools.UnInstallMode(Variable.ModeList.Keys.FirstOrDefault(x => x.GetModeData().Name == arguments.At(1))))
            {
                response = "Unknown Mode name.";
                return false;       
            }

            response = $"Mode {arguments.At(1)} Has been disabled!";
            return true;
        }
    }

    public string Command { get; } = "apt";
    public string[] Aliases { get; } = ["아파트"];
    public string Description { get; } = "모드를 관리하는 명령어";
}