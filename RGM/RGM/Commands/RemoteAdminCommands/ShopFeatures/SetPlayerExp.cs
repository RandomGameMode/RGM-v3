using System;
using System.Collections.Generic;
using System.Linq;
using CommandSystem;
using RGM.API.Features;

namespace RGM.Commands.RemoteAdminCommands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class SetPlayerExp : ICommand
    {
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (arguments.Count < 1)
            {
                response = "사용 방법: /경험치 <플레이어의 ID> <지급할 값>";
                return false;
            }
            
            string userId = Tools.TryGetUserId(arguments.At(0));
            bool result = int.TryParse(arguments.Count() < 2 ? "dum" : arguments.At(1), out int rc);
            List<string> uc = UsersManager.UsersCache[userId];

            bool flag = userId.SetExp(rc, out response, result);
            return flag;
        }

        public string Command { get; } = "경험치";

        public string[] Aliases { get; } = [];

        public string Description { get; } = "특정 유저의 경험치를 정합니다.";

        public bool SanitizeResponse { get; } = true;
    }
}
