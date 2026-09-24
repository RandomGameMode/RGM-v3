using System;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Core.UserSettings;
using Exiled.API.Features.Doors;
using Exiled.API.Features.Toys;
using InventorySystem.Items.Usables.Scp330;

using PlayerRoles;
using RGM.API.DataBases;
using RGM.API.Features;
using RGM.API.Interfaces;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RGM.Variables
{
    public static class Variable
    {
        public static ModeType CurrentMode = ModeType.None;
        public static ModeType CurrentSubMode = ModeType.None;
        public static AudioPlayer GlobalPlayer;
        public static string SelectMode = "";
        public static readonly string Tip = Tips.LobbyTips.GetRandomValue();
        public static readonly string Logo = Convert.ToByte(Random.Range(1, 3)) == 1 ? "❓" : "❔";
        public static readonly byte StartupRandom = Convert.ToByte(Random.Range(1, 31));
        public static string WinMessage = "";
        public static bool FreezeGameStart = false;
        public static bool ShootingTargetSignal = false;
        public static bool IsBugVoteProcessing = false;
        public static bool IsUsersFileLoaded = false;
        public static bool IsWinnerSelected = false;
        public static bool IsWarningAlone = false;
        public static bool IsClearCitizen = false;
        public static bool IsSuggestProcessing = false;
        public static bool IsWaveEnabled = true;
        public static bool IsNonePlayerAllowed = true;

        public static ShootingTargetToy Target1;
        public static ShootingTargetToy Target2;
        
        public static TimeSpan BanTime =>
            TimeSpan.TryParse(Main.Instance?.Config?.BanTime, out var data) ? data : TimeSpan.FromDays(7);

        // -------------------------------------------------------------------------------------------------

        public static List<Transform> First = new();
        public static List<Transform> Second = new();
        public static List<Transform> Third = new();
        public static List<Transform> Fourth = new();
        public static List<Transform> Numbers = new();
        public static List<Transform> RandomColors = new();
        public static List<Transform> RandomLights = new();
        public static List<Transform> Balls = new();
        public static List<Mode> EnabledModeList = new();
        public static List<ModeType> SubModeVote = new();
        public static List<Player> JumpScareCooldown = new();
        public static List<Player> GodModePlayers = new();
        public static List<Player> ChatCooldown = new();
        public static List<Player> EmotionCooldown = new();
        public static List<Player> BugVotePlayers = new();
        public static List<Player> BugVoteUsers = new();
        public static List<Player> IntercomPlayers = new();
        public static List<Player> ShopCooldown = new();
        public static List<ModeType> HighlightModes = new();
        public static List<Player> SuggestPlayers = new();
        public static List<Player> MuteBGMPlayers = new();
        public static List<string> UsedItems = new();
        public static List<string> UsingGameChipUsers = new();
        
        public static readonly List<string> Maps =
        [
            "BarotraumaWinterhalter3",
            "City17v3",
            "DeathInAir4",
            "NoMercyCP1v1",
            "Airship",
            "Lighthouse1",
            "TarkovStreet",
            "Airplane6",
            "OperationHarvest",
            // "OperationNightFlight",
            "China",
            "CityPlusCITY",
            "Castle",
            "FNAF2",
            // "VirtualWorld",
            "SnowForest"
        ];
        
        public static readonly List<string> Specials =
        [
            //"TRRBR",
            "Moszka",
            "Agar"
        ];
        
        public static readonly List<Product> Products =
        [

            new()
            {
                IsPubliced = true,
                Name = "인형 소환",
                Description = ".구매 인형/0ㅣ랜덤한 역할군의 인형을 소환합니다. 로비에서만 사용할 수 있습니다.",
                Price = 3,
                Check = (player, arg) => Round.IsLobby,
                Script = (player, arg) =>
                {
                    Ragdoll.CreateAndSpawn(Tools.EnumToList<RoleTypeId>().GetRandomValue(), "인형", "이 깜찍한 인형 좀 보세요.",
                        player.Position);
                }
            },

            new()
            {
                IsPubliced = true,
                Name = "랜덤박스",
                Description = ".구매 랜덤박스/0ㅣ랜덤한 아이템을 얻습니다. 로비 또는 라운드 종료 시에만 사용할 수 있습니다.",
                Price = 3,
                Check = (player, arg) => Round.IsLobby || Round.IsEnded,
                Script = (player, arg) => { player.AddRandomItem(); }
            },

            new()
            {
                IsPubliced = true,
                Name = "모드 추천서",
                Description = $".구매 모드 추천서/{{모드 이름}}ㅣ해당 모드가 투표 목록에 있다면 이름을 강조 처리합니다.",
                Price = 5,
                Check = (player, arg) =>
                {
                    return Round.IsLobby && ModeList.Keys.Select(x => x.GetModeData().Name).Contains(arg);
                },
                Script = (player, arg) =>
                {
                    HighlightModes.Add(ModeList.Keys.First(x => x.GetModeData().Name == arg));

                    Tools.MessageTranslated("",
                        $"<b><i>{player.DisplayNickname}</i></b>(이)가 <b>{ModeList.Keys.First(x => x.GetModeData().Name == arg).GetModeData().Name}</b> 모드를 추천하였습니다.");
                }
            },

            new()
            {
                IsPubliced = true,
                Name = "휴대용 라디오",
                Description = $".구매 휴대용 라디오/<노래 이름>ㅣ이 서버에 등록된 소리 파일 중 하나를 랜덤으로 재생합니다.",
                Price = 5,
                Check = (player, arg) => player.IsAlive,
                Script = (player, arg) =>
                {
                    AudioPlayer radio = AudioPlayer.CreateOrGet($"Radio {player.UserId}",
                        condition: (ReferenceHub hub) => !MuteBGMPlayers.Contains(Player.Get(hub)),
                        onIntialCreation: (p) =>
                        {
                            p.transform.parent = player.GameObject.transform;
                            Speaker speaker = p.AddSpeaker("Main", isSpatial: true, minDistance: 1f, maxDistance: 50f);
                            speaker.transform.parent = player.GameObject.transform;
                            speaker.transform.localPosition = Vector3.zero;
                        });

                    string audioDir = Paths.Plugins + "/audio/";
                    string[] audioFiles = Directory.GetFiles(audioDir).Select(Path.GetFileName).ToArray();
                    string fileName = audioDir + $"{arg}.ogg";
                    string clipName = audioFiles.GetRandomValue().Replace(".ogg", "").Replace(audioDir, "");

                    if (radio.TryPlay(arg, 0.5f) != null)
                        clipName = arg;

                    else
                        radio.TryPlay(clipName, 0.5f);

                    player.AddHint("휴대용 라디오", $"<size=20>{clipName} 재생 중..</size>", 5);
                }
            },

            new()
            {
                IsPubliced = true,
                Name = "확성기",
                Description = $".구매 확성기/{{내용}}ㅣ{{내용}}을 큼지막한 글씨로 띄웁니다. 로비 또는 라운드 종료 시에만 사용할 수 있습니다.",
                Price = 5,
                Check = (player, arg) => Round.IsLobby || Round.IsEnded,
                Script = (player, arg) =>
                {
                    string text = string.Concat(new string[]
                    {
                        $"<size=40><b>확성기</b>ㅣ{Tools.BadgeFormat(player)}<color={player.Role.Color.ToHex()}>",
                        Trans.Role[player.Role.Type],
                        $"</color> (<b><i>{player.DisplayNickname}</i></b>) <b> | </b>",
                        arg.Replace("=", "❤️"),
                        "</size>"
                    });

                    foreach (Player ply in PlayerManager.List)
                    {
                        ply.AddBroadcast(10, text);
                    }
                }
            },

            new()
            {
                IsPubliced = true,
                Name = "모드 제안서",
                Description =
                    $".구매 모드 제안서/{{모드 이름}}ㅣ현재 투표 목록에 없는 모드로 4번째 투표 목록을 10% 확률로 교체합니다. 한 라운드 당 한번만 구매할 수 있습니다.",
                Price = 10,
                Check = (player, arg) =>
                {
                    return Round.IsLobby && ModeList.Keys.Select(x => x.GetModeData().Name).Contains(arg) &&
                           !ModeVote.Keys.Select(x => x.GetModeData().Name).Contains(arg) &&
                           !UsedItems.Contains("모드 제안서");
                },
                Script = (player, arg) =>
                {
                    if (ModeVote.Keys.Select(x => x.GetModeData().Name).Contains(arg))
                        return;

                    UsedItems.Add("모드 제안서");

                    string modeName = ModeList.Keys
                        .First(x => x.GetModeData().Name == arg && x.GetModeData().Category != ModeCategory.Private)
                        .GetModeData().Name;
                    bool flag = Convert.ToByte(Random.Range(1, 101)) <= 10;

                    if (flag)
                    {
                        ModeVote.Remove(ModeVote.ElementAt(3).Key);
                        ModeVote.Add(ModeList.First(x => x.Value.Name == arg).Key, new List<Player>());

                        Tools.MessageTranslated("",
                            $"<b><i>{player.DisplayNickname}</i></b>(이)가 <b>{modeName}</b> 모드를 제안하는 데 성공했습니다!!");
                    }
                    else
                    {
                        Tools.MessageTranslated("",
                            $"<b><i>{player.DisplayNickname}</i></b>(이)가 <b>{modeName}</b> 모드를 제안하는 데 실패했습니다.");
                    }
                }
            },

            new()
            {
                IsPubliced = false,
                Name = "고급 모드 제안서",
                Description = $".사용 고급 모드 제안서/{{모드 이름}}ㅣ현재 투표 목록에 없는 모드로 4번째 투표 목록을 무조건 교체합니다.",
                Price = 1205,
                Check = (player, arg) =>
                {
                    return Round.IsLobby && ModeList.Keys.Select(x => x.GetModeData().Name).Contains(arg) &&
                           !ModeVote.Keys.Select(x => x.GetModeData().Name).Contains(arg) &&
                           !UsedItems.Contains("고급 모드 제안서");
                },
                Script = (player, arg) =>
                {
                    if (ModeVote.Keys.Select(x => x.GetModeData().Name).Contains(arg))
                        return;

                    UsedItems.Add("고급 모드 제안서");

                    string modeName = ModeList.Keys
                        .First(x => x.GetModeData().Name == arg && x.GetModeData().Category != ModeCategory.Private)
                        .GetModeData().Name;

                    ModeVote.Remove(ModeVote.ElementAt(3).Key);
                    ModeVote.Add(ModeList.First(x => x.Value.Name == arg).Key, new List<Player>());

                    Tools.MessageTranslated("",
                        $"<b><i>{player.DisplayNickname}</i></b>(이)가 <b><color=#ffd700>고급 모드 제안서</color></b>를 사용하여, <b>{modeName}</b> 모드를 제안하는 데 성공했습니다!!");
                }
            },

            new()
            {
                IsPubliced = true,
                Name = "모드 리롤권",
                Description = $".사용 모드 리롤권ㅣ10% 확률로 모든 투표 목록을 변경합니다. 판 엎기에 딱 좋네요. 한 라운드 당 한번만 사용할 수 있습니다.",
                Price = 10,
                Check = (player, arg) => Round.IsLobby && !UsedItems.Contains("모드 리롤권"),
                Script = (player, arg) =>
                {
                    UsedItems.Add("모드 리롤권");

                    bool flag = Convert.ToByte(Random.Range(1, 101)) <= 10;

                    if (flag)
                    {
                        Tools.PickModes();

                        Tools.MessageTranslated("",
                            $"<b><i>{player.DisplayNickname}</i></b>(이)가 투표 목록을 갱신하는 데 성공했습니다!!");
                    }
                    else
                    {
                        Tools.MessageTranslated("",
                            $"<b><i>{player.DisplayNickname}</i></b>(이)가 투표 목록을 갱신하는 데 실패했습니다.");
                    }
                }
            },

            new()
            {
                IsPubliced = false,
                Name = "고급 모드 리롤권",
                Description = $".사용 고급 모드 리롤권ㅣ무조건적으로 모든 투표 목록을 변경합니다.",
                Price = 1205,
                Check = (player, arg) => Round.IsLobby && !UsedItems.Contains("고급 모드 리롤권"),
                Script = (player, arg) =>
                {
                    UsedItems.Add("고급 모드 리롤권");

                    Tools.PickModes();

                    Tools.MessageTranslated("",
                        $"<b><i>{player.DisplayNickname}</i></b>(이)가 <b><color=#ffd700>고급 모드 리롤권</color></b>를 사용하여, 투표 목록을 갱신하는 데 성공했습니다!!");
                }
            },

            new()
            {
                IsPubliced = true,
                Name = "게임 칩",
                Description = $".사용 이번 라운드에서 승리 시 10배만큼 랜덤코인을 추가로 얻습니다.",
                Price = 5,
                Check = (player, arg) => Round.IsLobby && !UsingGameChipUsers.Contains(player.UserId),
                Script = (player, arg) => { UsingGameChipUsers.Add(player.UserId); }
            }
        ];

        public static readonly List<ModeType> SuicideBlockedModes =
        [
            ModeType.Spirit
        ];

        public static readonly List<RoleTypeId> SuicideBlockedRoles = 
        [
            RoleTypeId.Tutorial
        ];

        public static readonly List<ModeType> ScpSuicideAvailableModes =
        [
            ModeType.Infection
        ];
        
        // -------------------------------------------------------------------------------------------------

        public static Dictionary<ModeType, ModeData> ModeList = new();
        public static Dictionary<ModeType, List<Player>> ModeVote = new();
        public static Dictionary<string, float> OnGround = new();
        public static Dictionary<Player, Room> CurrentRoom = new();
        public static Dictionary<string, PlayerInfo> PlayersInfo = new();
        public static Dictionary<string, PlayerReport> PlayersReport = new();
        public static Dictionary<Player, AudioPlayer> PlayersAudio = new();
        public static Dictionary<Door, int> InteractedDoors = new();
        public static Dictionary<string, string> DiscordIdToUserId = new();
        public static Dictionary<Player, List<string>> Chats = new();
        public static Dictionary<Player, LabApi.Features.Wrappers.TextToy> Texts = new();
        public static Dictionary<Player, List<SettingBase>> PlayerSetting = new();
        public static Dictionary<Player, string> TranslatorPlayers = new();
        public static Dictionary<Player, Dictionary<EffectType, int>> EffectIntensities = new();
        public static readonly Dictionary<string, string> KillEffects = new()
        {
            {"영혼 가출", "죽은 상대에게서 혼을 추출해냅니다!"},
            {"솔라 테라", "죽음에 햇빛 한 점 들기를.."},
            {"Kerfus", "귀여운 로봇으로 도장을 찍어보세요."},
            {"은제 말뚝", "비수를 꽂는 것처럼 소름끼칩니다!"},
            {"KO 사인", "넉 다운! 상대를 쓰러트리세요!"},
            {"크리스마스 트리", "축제를 돋보이게 하는 아담한 트리입니다."},
            {"크리스마스 볼", "축제의 연출을 돕는 스노우볼입니다."},
            {"철퇴", "이유가 무엇이던간에 처형은 이루어집니다."},
            {"수렴형 레이저", "찰나의 순간, 아래에서 올라오는 빛을 보게 되겠죠."},
            {"5월 5일", "마음만큼은 어린이날에 머물러 있답니다."},
            {"카피바라", "카피바라~ 카피바라카피바라카피바라카피바라.."},
            {"찰칵", "연말의 마지막 순간을 박제하는 소리가 들려요."},
            {"SCP999", "이 귀여운 생명체는 뭐죠...?"},
            {"Lightning", "찌리찌리 짜라짜라"}
        };
        public static readonly Dictionary<string, string> SpawnEffects = new()
        {
            {"Connected", "연결되었습니다."}
        };
        public static readonly Dictionary<string, string> Customizations = new()
        {
            {"커스텀 닉네임", "표시되는 플레이어 이름을 수정합니다."},
            {"커스텀 인포", "플레이어 인포를 추가합니다."},
            {"커스텀 키카드", "들고 있는 키카드를 수정합니다."}
        };
        public static readonly Dictionary<string, string> Paints = new()
        {
            {"블랙골드", "검은색과 금색의 달콤한 콜라보!"},
            {"핫핑크", "두근두근거리는 핑크들의 콜라보!"},
            {"레인보우", "R.A.I.N.B.O.W."},
            {"분홍색", "이 느낌은 뭔가 편안함을 줍니다."},
            {"빨간색", "강렬한 느낌!"},
            {"흰색", "기본적인 색상입니다. 도화지 같다고나 할까요."},
            {"갈색", "달콤하고도 안정적인 무언가가 생각나네요."},
            {"은색", "은의 힘을 받아 혈을 물리칩니다."},
            {"밝은 녹색", "녹색이 밝으면 보기 좋죠."},
            {"진홍색", "찐한 분홍색! 색다른 느낌이죠?"},
            {"청록색", "진한 초록색! 어두운 진실이 감춰진 것 같습니다."},
            {"옥색", "청량한 바다가 연상되네요."},
            {"진한 분홍색", "진한 분홍색? 으음.."},
            {"토마토색", "나는야 멋쟁이 토 마 토"},
            {"노란색", "GoldenPig1205가 좋아하는 색상입니다."},
            {"짙은 홍색", "시산혈해"},
            {"푸른 녹색", "파릇 파릇하니, 기분이 좋군요."},
            {"주황색", "결심이 확실한 색상이에요."},
            {"라임색", "라임 색상은 라임을 잘 맞춰..ㅋㅋ"},
            {"초록색", "자연이 연상되는 색이군요."},
            {"에메랄드색", "에메랄드보다 더 희귀한 광석은 무엇일까요? (웃음)"},
            {"카민색", "이건 뭔 색이지"},
            {"니켈색", "어떤 색이랑 많이 비슷하군요."},
            {"박하색", "민트 초코 좋아하신다구요?"},
            {"군대 녹색", "이름이 왜 군대(army) 녹색일까요?"},
            {"호박색", "할로윈 좋아하세요?"}
        };
        public static readonly Dictionary<string, string> Badges = new()
        {
            {"RGM Owner", "랜덤게임모드(RGM) 공식 운영자 칭호"},
            {"RGM Administrator", "랜덤게임모드(RGM) 공식 관리자 칭호"},
            {"RGM Developer", "랜덤게임모드(RGM) 공식 개발자 칭호"},
            {"RGM Contributor", "랜덤게임모드(RGM) 공식 기여자 칭호" },
            {"호기심 많은 자", "상점에 칭호가 있어서 사봤어요!"},
            {"Merry Christmas", "즐거운 크리스마스 보내세요."},
            {"1st Anniversary", "랜덤게임모드 1주년을 기념하는 이벤트 우승자"},
            {"Adieu, Polaris", "초대 개발자의 마지막 이벤트 우승자"},
            {"Adieu! 2023", "2023년의 마지막을 기념하며"},
            {"2023 RGM Summer", "2023년도 여름에 진행되었던 이벤트 우승자"},
            {"꾸요미 D계급", "D계급은 기본적으로 귀엽게 생겼다고 할 수 있습니다."},
            {"야근중인 과학자", "오늘도 대업을 위해 일을 멈추지 않는 과학자입니다."},
            {"복면이 그리운 시설 경비", "이런! 한기가 느껴지는 재단에서는 복면이 참 좋았는데.."},
            {"무능한 구미호", "너무 무능하다고 하지 마세요. 그래도 총은 있잖.. 어?"},
            {"난동꾼 반란", "\"시설에 난입하여 그들만의 이익을 취합니다.\""},
            {"동네북 뱀의 손", "너는 누구 팀이야? SCP? 인간?"},
            {"Adios! 2024", "2024년의 마지막을 기념하며"},
            {"Nostalgic Story", "2025년의 추억의 이야기"},
            {"협동 선호", "협동은 일 처리를 수월하게 합니다."},
            {"개인주의 플레이", "가장 중요한 건 바로 나!"},
            {"반란 주의", "성공하면 혁명, 실패하면 반역"},
            {"순종적인 자세", "일단.. 살고 봐야져..."},
            {"올라운더", "불가능한 임무는 없다!"},
            {"포기는 금물", "(대충 길어서 설명은 디스코드 확인하세요)"},
            {"도파민 우선", "하지만 재밌었죠?"},
            {"2026", "아무도 알아주지 않을 길을 걷는 자"},
            {"2025 Last Survivor", "2025 연말 이벤트의 마지막 생존자"},
            {"마음만큼은 어린이", "2026년도 어린이날(5월 5일) 기념"}
        };
        public static readonly Dictionary<string, string> Icons = new()
        {
            {"⭐", "반짝 반짝 작은 별"},
            {"✿", "작은 꿈은 벚꽃처럼 만개하리라"},
        };
        public static readonly Dictionary<CandyKindID, ICandy> CandyDataDict = new()
        {
            { CandyKindID.Brown, new HauntedCandyBrown() },
            { CandyKindID.Gray, new HauntedCandyGray() },
            { CandyKindID.Black, new HauntedCandyBlack() },
            { CandyKindID.Evil, new HauntedCandyEvil() },
            { CandyKindID.Orange, new HauntedCandyOrange() },
            { CandyKindID.White, new HauntedCandyWhite() },
        };

        // -------------------------------------------------------------------------------------------------
    }
}
