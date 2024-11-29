using RGM.API.Features;
using RGM.API.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Exiled.API.Features;
using Exiled.API.Features.Doors;

namespace RGM.Variables
{
    public static class ServerManagers
    {
        public static ModeType CurrentMode = ModeType.None;
        public static ModeType CurrentSubMode = ModeType.None;
        public static string SelectMode = null;
        public static string Tip = Tools.GetRandomValue(Tips.LobbyTips);
        public static string Logo = UnityEngine.Random.Range(1, 3) == 1 ? "❓" : "❔";
        public static int StartupRandom = UnityEngine.Random.Range(1, 31);
        public static bool FreezeGameStart = false;
        public static bool AutoNuke = false;
        public static bool IsScp3114Enabled = false;
        public static bool IsBugVoteProcessing = false;
        public static bool IsUsersFileLoaded = false;

        public static Dictionary<ModeType, ModeData> ModeList = new Dictionary<ModeType, ModeData>();
        public static Dictionary<ModeType, List<Player>> ModeVote = new Dictionary<ModeType, List<Player>>();
        public static Dictionary<Player, float> OnGround = new Dictionary<Player, float>();
        public static Dictionary<Player, Room> CurrentRoom = new Dictionary<Player, Room>();
        public static Dictionary<string, PlayerInfo> PlayersInfo = new Dictionary<string, PlayerInfo>();
        public static Dictionary<string, PlayerReport> PlayersReport = new Dictionary<string, PlayerReport>();
        public static Dictionary<Door, int> InteractedDoors = new Dictionary<Door, int>();
        public static Dictionary<string, string> KillEffects = new Dictionary<string, string>()
        {
            {"영혼 가출", "죽은 상대에게서 혼을 추출해냅니다!"},
            {"솔라 테라", "죽음에 햇빛 한 점 들기를.."},
            {"Kerfus", "귀여운 로봇으로 도장을 찍어보세요."},
            {"은제 말뚝", "비수를 꽂는 것처럼 소름끼칩니다!"},
            {"KO 사인", "넉 다운! 상대를 쓰러트리세요!"}
        };
        public static Dictionary<string, string> Customizations = new Dictionary<string, string>()
        {
            {"커스텀 닉네임", "표시되는 플레이어 이름을 수정합니다."},
            {"커스텀 인포", "플레이어 인포를 추가합니다."}
        };
        public static Dictionary<string, string> Paints = new Dictionary<string, string>()
        {
            {"블랙골드", "검은색과 금색의 달콤한 콜라보!"},
            {"핫핑크", "두근두근거리는 핑크들의 콜라보!"},
            {"레인보우", "R.A.I.N.B.O.W."}
        };

        public static List<ModeType> EnabledModeList = new List<ModeType>();
        public static List<ModeType> SubModeVote = new List<ModeType>();
        public static List<string> Requests = new List<string>();
        public static List<Player> GodModePlayers = new List<Player>();
        public static List<Player> ChatCooldown = new List<Player>();
        public static List<Player> BugVotePlayers = new List<Player>();

        public static List<Transform> First;
        public static List<Transform> Second;
        public static List<Transform> Third;
        public static List<Transform> Numbers;
        public static List<Transform> RandomColors;
        public static List<Transform> Balls;
    }
}
