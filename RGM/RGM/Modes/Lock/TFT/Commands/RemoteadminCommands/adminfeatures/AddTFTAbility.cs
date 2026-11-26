using System;
using System.Linq;
using CommandSystem;
using RGM.Modes.Lock.TFT;
using Exiled.API.Features;

namespace RGM.Modes.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class AddTFTAbility : ICommand
{
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (Round.IsStarted)
        {
            var player = Player.Get(arguments.At(0));
            var args = string.Join(" ", arguments.Skip(1));

            var ability = TFTBattle.FindTftAbility(args);

            if (ability == TFTAbilityType.None)
            {
                response = "해당 능력을 찾을 수 없습니다.";
                return false;
            }

            TFTBattle.AddTftAbility(player, ability);

            response = "AddAbility Complete!";
            return true;
        }

        response = "라운드 시작 전에는 사용할 수 없습니다.";

        return false;
    }

    public string Command { get; } = "addtftability";

    public string[] Aliases { get; } = { "addtft" };

    public string Description { get; } = "DAONㅣ능력을 추가합니다.";
}