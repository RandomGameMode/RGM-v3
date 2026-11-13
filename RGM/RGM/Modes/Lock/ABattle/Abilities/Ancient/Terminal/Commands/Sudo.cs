using System;
using System.Diagnostics.CodeAnalysis;
using CommandSystem;
using Exiled.API.Features;
using RGM.Modes.Abilities.Ancient.Commands.InnerCommand;

namespace RGM.Modes.Abilities.Ancient.Commands;

public class Sudo : ParentCommand
{
    protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender, [UnscopedRef] out string response)
    {
        var player = Player.Get(sender);
        if (player == null || 
            player.IsNPC || 
            player.IsDead || 
            !player.GetAbilities().Exists(x => x.Data.AbilityType == AbilityType.ANCIENT_TERMINAL))
        {
            response = "<color=red>Command Not found.</color>";
            return false;    
        }
        
        response = "Please enter command or write \"sudo man\"";
        return false;
    }

    public override string Command => "sudo";
    public override string[] Aliases { get; } = ["su"];
    public override string Description => "툴의 가장 기초인 명령 (원하시는 대로 수정하시면 됩니다)";

    public override void LoadGeneratedCommands()
    {
        RegisterCommand(new Man());
        
    }
}