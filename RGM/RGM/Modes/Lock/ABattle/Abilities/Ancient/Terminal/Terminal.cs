using RemoteAdmin;
using RGM.API.Features;
using RGM.Modes.Abilities.Ancient.Commands;

namespace RGM.Modes.Abilities.Ancient;

/*[Ability("Terminal",
    """
    자신의 콘솔 창이 명령 프롬프트가 됩니다!
    콘솔에 특정 명령어를 입력하여 라운드를 지배해버릴 수 있습니다!
    """,
    AbilityCategory.Ancient, AbilityType.ANCIENT_TERMINAL)]*/

public class Terminal : Ability
{
    private static bool _terminalEnabled;
    
    public override void OnEnabled()
    {
        Owner.AddHint("Terminal", "sudo man을 입력해보세요.", 5f);

        if (_terminalEnabled) return;
        QueryProcessor.DotCommandHandler.RegisterCommand(new Sudo());
        _terminalEnabled = true;
    }
    
    
}