using Exiled.API.Enums;
using PlayerRoles;
using System.Collections.Generic;

namespace RGM.API.DataBases
{
    public class Datas
    {
        public static readonly Dictionary<string, string> Colors = new()
        {
            // {"gold", "#EFC01A"},
            // {"teal", "#008080"},
            // {"blue", "#005EBC"},
            // {"purple", "#8137CE"},
            // {"light_red", "#FD8272"},
            {"pink", "#FF96DE"},
            {"red", "#C50000"},
            {"default", "#FFFFFF"},
            {"brown", "#944710"},
            {"silver", "#A0A0A0"},
            {"light_green", "#32CD32"},
            {"crimson", "#DC143C"},
            {"cyan", "#00B7EB"},
            {"aqua", "#00FFFF"},
            {"deep_pink", "#FF1493"},
            {"tomato", "#FF6448"},
            {"yellow", "#FAFF86"},
            {"magenta", "#FF0090"},
            {"blue_green", "#4DFFB8"},
            // {"silver_blue", "#666699"},
            {"orange", "#FF9966"},
            // {"police_blue", "#002DB3"},
            {"lime", "#BFFF00"},
            {"green", "#228B22"},
            {"emerald", "#50C878"},
            {"carmine", "#960018"},
            {"nickel", "#727472"},
            {"mint", "#98FB98"},
            {"army_green", "#4B5320"},
            {"pumpkin", "#EE7600"}
        };

        public static readonly Dictionary<string, List<string>> KillEffectData = new()
        {
            {"영혼 가출", new List<string>() {"<b><color=#B47474>영</color><color=#915E5F>혼</color> <color=#4D3335>가</color><color=#2B1E20>출</color></b>", "영혼 추출"}},
            {"솔라 테라", new List<string>() {"<b><color=#FD2626>솔</color><color=#FD411E>라</color> <color=#FE770F>테</color><color=#FE9207>라</color></b>", "멜트다운" }},
            {"Kerfus", new List<string>() {"<b><color=#6D8F97>K</color><color=#608294>e</color><color=#547592>r</color><color=#48698F>f</color><color=#3C5C8D>u</color><color=#304F8B>s</color></b>", "찌부" }},
            {"은제 말뚝", new List<string>() {"<b><color=#1A996E>은</color><color=#2B916F>제</color> <color=#4F8271>말</color><color=#617A72>뚝</color></b>", "관.통" }},
            {"KO 사인", new List<string>() {"<b><color=#D6A624>K</color><color=#DBB729>O</color> <color=#E6DB33>사</color><color=#ECED38>인</color></b>", "<b>넉 다 운</b>" }},
            {"크리스마스 트리", new List<string>() {"<b><color=#FF0000>크</color><color=#FE3E3F>리</color><color=#FD7D7F>스</color><color=#FCBCBE>마</color><color=#FBFBFE>스</color> <color=#7DD77F>트</color><color=#3EC53F>리</color></b>", "<color=#FA5858>산타</color>" }},
            {"크리스마스 볼", new List<string>() {"<b><color=#FF0000>크</color><color=#FD5354>리</color><color=#FCA7A9>스</color><color=#FAFBFE>마</color><color=#A7E3A9>스</color> <color=#00B300>볼</color></b>", "<color=#B45F04>루돌프</color>" }},
            {"철퇴", new List<string>() { "<b><color=#000000>철</color><color=#4A4A4A>퇴</color></b>", "처형" }},
            {"수렴형 레이저", new List<string>() {"<b><color=#00E1FF>수</color><color=#00EBD7>렴</color><color=#00F5AF>형</color> <color=#1BFFA1>레</color><color=#36FFBA>이</color><color=#52FFD4>저</color></b>", "소각" }},
            {"5월 5일", new List<string>() {"<b><color=#E1FF00>5</color><color=#E7E700>월</color> <color=#F3B700>5</color><color=#F99F00>일</color></b>", "행복사" }},
            {"카피바라", new List<string>() {"<b><color=#A67B5B>카</color><color=#B28C6C>피</color><color=#BE9D7D>바</color><color=#CABF8E>라</color></b>", "카피바라"}},
            {"찰칵", new List<string>() { "<b><color=#D8FE9F>카</color><color=#A2FE78>메</color><color=#6DFE51>라</color></b>", "추억 속으로 박제"}},
            {"SCP999", new List<string>() { "<b><color=#fccd2f>SCP-9</color><color=#f6b636>9</color><color=#f4a238>9</color></b>", "_"}},
            {"Lightning", new List<string>() { "_", "<b><color=#fcf525>천</color><color=#0cfcf0>벌</color></b>"}} // _ 표시 -> 안 쓴다는 뜻
        };

        public static readonly List<DamageType> BlockDamageTypes =
        [
            DamageType.Warhead,
            DamageType.Crushed,
            DamageType.PocketDimension,
            DamageType.Falldown,
            DamageType.Scp106
        ];

        public static readonly List<RoleTypeId> AIRoles =
        [
            RoleTypeId.Scp049,
            RoleTypeId.Scp096,
            RoleTypeId.Scp106,
            RoleTypeId.Scp173
        ];

        public static readonly List<ItemType> ExceptItems =
        [
            ItemType.Snowball,
            ItemType.Coal,
            ItemType.SpecialCoal,
            ItemType.SCP1507Tape
        ];
    }
}
