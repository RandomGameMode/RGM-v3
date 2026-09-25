using System;

using static RGM.Variables.Variable;

namespace RGM;

public abstract class Mode
{
    public abstract void OnEnabled();
    public abstract void OnDisabled();

    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract string Detail { get; }
    public abstract string Color { get; }
    public virtual string Author { get; set; } = "GoldenPig1205";
    public virtual string Suggester { get; set; } = "";
    public virtual string Map { get; } = "";

    public ModeData Data { get; set; }
}

public class ModeData
{
    public ModeCategory Category { get; set; }
    public ModeInfo Info { get; set; }
    public ModeType Type { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Detail { get; set; }
    public string Color { get; set; }
    public string Author { get; set; }
    public string Suggester { get; set; } = "";
    public string Map { get; set; } = "";
    public ModeHoliday Holiday { get; set; } = ModeHoliday.None;
}

[AttributeUsage(AttributeTargets.Class)]
public class ModeAttribute(ModeCategory category, ModeInfo info, ModeType type, ModeHoliday holiday = ModeHoliday.None) : Attribute
{
    public ModeCategory Category { get; } = category;
    public ModeInfo Info { get; } = info;
    public ModeType Type { get; } = type;
    public ModeHoliday Holiday { get; set; } = holiday;
}

public static class ModeExtensions 
{
    public static ModeData GetModeData(this ModeType modeType) => ModeList[modeType];
}

public enum ModeCategory 
{
    None,
    Public,
    Private,
    OnlySub
}

public enum ModeInfo
{
    None,
    Plus,
    Set,
    Lock,
}

public enum ModeHoliday
{
    None,
    Halloween,
    Christmas,
}

public enum ModeType
{
    None,
    AI,
    Alone,
    ABattle,
    AdditionalWave,
    Ascension,
    Blackout,
    Blessing,
    BomberMan,
    Builder,
    ChupaChups,
    Clairvoyance,
    BombParty,
    Collector,
    Cell,
    ChangedFate,
    Disguise,
    Distancing,
    DogFight,
    DeadLine,
    DeathRun,
    Develop,
    DoubleUp,
    FriendlyFire,
    FreeForAll,
    Gamble,
    Ghost,
    GrandBow,
    GGClub,
    GunGame,
    JohnWick,
    HIDE,
    Skeleton,
    KoreanSpeed,
    Outlaw,
    PaperMan,
    Radio,
    RandomEffect,
    RandomItem,
    RandomSize,
    RedLightGreenLight,
    RocketLauncher,
    SCPRUSH,
    Snake,
    SnowBallFight,
    SourceMan,
    Siberia,
    Silent,
    SoulMate,
    Spirit,
    SuperStar,
    TrickorTreat,
    TripleAxel,
    Unlimited,
    Juggernaut,
    LastOne,
    MiniGames,
    Original,
    PickMeUp,
    PirateRoulette,
    Infection,
    Rescue05,
    RussianRoulette,
    SpeedRun,
    Spleef,
    TailCatcter,
    TeamMatch,
    Tomb,
    TTT,
    WhereamI,
    WhoamI1,
    WhoamI2,
    WitGame,
    RUN,
    AddScpMode,
    SitDown,
    ClassSociety,
    Doppelganger,
    Moon,
    Curse,
    Tag,
    HideAndSeek,
    PVE,
    Agar,
    TFT,
    RandomBlockMode,
    Rank,
    Chess,
    Store,
    EchoBattle,
    DistractedDriver,
    Scp1509Hand,
    Stroking999,
    GulliversTravels
}


