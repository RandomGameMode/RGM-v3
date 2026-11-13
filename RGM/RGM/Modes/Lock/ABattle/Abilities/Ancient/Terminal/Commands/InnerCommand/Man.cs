using System;
using System.Diagnostics.CodeAnalysis;
using CommandSystem;

namespace RGM.Modes.Abilities.Ancient.Commands.InnerCommand;


// 기습 고대 유출
[CommandHandler(typeof(Sudo))]
public class Man : ICommand
{
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, [UnscopedRef] out string response)
    {
        response = """
                   <b>----------[APT]----------</b>
                   - 모드를 추가합니다.
                   - Usage: apt install [MODENAME]
                   - Sub commands:
                     - purge: 모드를 제거합니다.
                     - Usage: apt purge [MODENAME]
                   <b>----------[ RM]----------</b>
                   - 특정 플레이어를 관전자로 변경합니다.
                   - Usage: rm [USERNAME]
                   - Flags: 
                     - 특정 진영 전체를 관전자로 변경합니다.
                     - Usage: rm -r <SCP/FOUNDATION/CHI/TUTORIAL>
                   <b>----------[TOUCH]----------</b>
                   - 특정 플레이어를 자신의 진영과 같은 진영으로 변경합니다.
                   - Usage: touch [USERNAME]
                   - Flags:
                     - soft: 모든 것을 유지하고 진영을 변경합니다.
                     - mixed: 능력을 제외한 모든 것을 해당 진영의 초기 상태로 되돌립니다.
                     - hard: 모든 것을 초기 상태로 되돌립니다.
                     - Usage: touch --<soft/mixed/hard> [USERNAME]
                   """;
        return true;
    }

    public string Command { get; } = "man";
    public string[] Aliases { get; } = ["nn"];
    public string Description { get; } = "도움창";
}