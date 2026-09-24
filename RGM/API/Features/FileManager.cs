using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Exiled.API.Features;
using MEC;
using Newtonsoft.Json;
using static RGM.Variables.Variable;

namespace RGM.API.Features
{
    /**
     * <summary>파일 관련 작업을 수행합니다</summary>
     */
    public static class FileManager
    {
        /// <summary>
        /// RGM 설정 주소
        /// </summary>
        public static string FolderPath => Path.Combine(Paths.Configs, "RGM");
        
        /// <summary>
        /// 무결성을 위해 Mutex를 사용합니다
        /// </summary>
        private static readonly Mutex _fileMutex = new Mutex(false, "Global\\RGM_FileManager_Mutex");
        
        /**
         * <summary>파일 주소를 결합합니다</summary>
         * <param name="fileName">파일 이름</param>
         */
        private static string ResolvePath(string fileName)
        {
            return Path.IsPathRooted(fileName) ? fileName : Path.Combine(FolderPath, fileName);
        }
        
        /**
         * <summary>폴더를 생성합니다</summary>
         */
        public static void CreateFolder()
        {
            if (!Directory.Exists(FolderPath))
                Directory.CreateDirectory(FolderPath);
        }

        /**
         * <summary>파일에 새 내용을 작성합니다</summary>
         * <param name="fileName">작성할 파일 이름</param>
         * <param name="content">작성할 내용</param>
         */
        public static void WriteFile(string fileName, string content)
        {
            bool acquired = false;
            try
            {
                acquired = _fileMutex.WaitOne(5000);
                if (!acquired)
                {
                    Log.Warn($"[FileManager] File {fileName} is currently busy. Skipping save to prevent corruption.");
                    return;
                }

                string targetFile = ResolvePath(fileName);
                string directory = Path.GetDirectoryName(targetFile);
                if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                string tempFile = targetFile + ".tmp";

                File.WriteAllText(tempFile, content, Encoding.UTF8);

                if (File.Exists(targetFile))
                {
                    File.Replace(tempFile, targetFile, null, true);
                }
                else
                {
                    File.Move(tempFile, targetFile);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[FileManager] Exception while writing file {fileName}: {ex}");
            }
            finally
            {
                if (acquired)
                    _fileMutex.ReleaseMutex();
            }
        }

        /**
         * <summary>파일을 읽어옵니다</summary>
         * <param name="fileName">읽을 파일 이름</param>
         * <returns>파일의 내용</returns>
         */
        public static string ReadFile(string fileName)
        {
            bool acquired = false;
            try
            {
                acquired = _fileMutex.WaitOne(5000);
                if (!acquired)
                {
                    Log.Warn($"[FileManager] File {fileName} is currently busy. Skipping read to prevent corruption.");
                    return string.Empty;
                }

                string targetFile = ResolvePath(fileName);
                if (!File.Exists(targetFile))
                    return string.Empty;

                return File.ReadAllText(targetFile);
            }
            catch (System.Exception ex)
            {
                Log.Error($"[FileManager] Exception while reading file {fileName}: {ex}");
                return string.Empty;
            }
            finally
            {
                if (acquired)
                    _fileMutex.ReleaseMutex();
            }
        }
    }

    public enum UserDataStructure
    {
        /*
        Exp - 0
        랜덤코인 - 1
        Cash - 2
        보유한 킬 이펙트 - 3
        장착한 킬 이펙트 - 4
        커스텀 닉네임 - 5
        커스텀 인포 - 6
        보유한 커스터마이징 - 7
        보유한 페인트 - 8
        장착한 페인트 - 9
        보유한 칭호 - 10
        장착한 칭호 - 11
        닉네임 - 12
        연동된 디스코드 ID - 13
        연동 코드 - 14
        킬이펙트 랜덤 여부 - 15
        페인트 랜덤 여부 - 16
        칭호 랜덤 여부 - 17
        보유한 아이템 - 18
        보유한 스폰이펙트 - 19
        장착한 스폰이펙트 - 20
        스폰이펙트 랜덤 여부 - 21
        경고 - 22
        방해 금지 활성화 여부 - 23
        보유한 아이콘 - 24
        장착한 아이콘 - 25
        아이콘 랜덤 여부 - 26
        출석 일수 - 27
        최대 연속 출석 일수 - 28
        출석 여부 (오늘) - 29
        현재 연속 출석 일수 - 30
        */
        
        Exp = 0,
        RandomCoin = 1,
        Cash = 2,
        OwnKillEffect = 3,
        EquipKillEffect = 4,
        CustomNickName = 5,
        CustomInfo = 6,
        OwnCustomizing = 7,
        OwnPaint = 8,
        EquipPaint = 9,
        OwnTitle = 10,
        EquipTitle = 11,
        NickName = 12,
        DiscordID = 13,
        LinkCode = 14,
        KillEffectRandom = 15,
        PaintRandom = 16,
        TitleRandom = 17,
        OwnItem = 18,
        OwnSpawnEffect = 19,
        EquipSpawnEffect = 20,
        SpawnEffectRandom = 21,
        Warning = 22,
        DoNotDisturb = 23,
        OwnIcon = 24,
        EquipIcon = 25,
        IconRandom = 26,
        DayStreak = 27,
        MaxDayStreak = 28,
        TodayAttend = 29,
        NowDayStreak = 30
    }
    
    /**
     * <summary>유저 DB 관련 작업을 처리합니다</summary>
     */
    public static class UsersManager
    {
        
        /// <summary>
        /// 마지막 유저 DB 로드 시간 (UTC)
        /// </summary>
        private static DateTime _lastUsersFileLoadUtc = DateTime.MinValue;
        /// <summary>
        /// 마지막 유저 DB 쓰기 시간 (UTC)
        /// </summary>
        private static DateTime _lastUsersFileWriteUtc = DateTime.MinValue;
        
        /// <summary>
        /// 유저 DB 주소
        /// </summary>
        public static string UsersFileName = Path.Combine(Paths.Configs, "RGM/Users.db");
        /// <summary>
        /// 유저 캐시
        /// </summary>
        public static Dictionary<string, List<string>> UsersCache = new();
        
        /**
         * <summary>유저에 대한 정보를 불러옵니다</summary>
         * <param name="userId">유저의 아이디</param>
         * <param name="num">가져올 정보 ({PLACEHOLDER} 참조)</param>
         * <summary>유저의 정보</summary>
         */
        public static string CheckUser(string userId, int num)
        {
            if (UsersCache.ContainsKey(userId) && num >= 0 && num < UsersCache[userId].Count)
                return UsersCache[userId][num];

            return null;
        }
        
        /**
         * <summary>유저와 유저 정보를 추가합니다</summary>
         * <param name="userId"></param>
         * <returns>성공 여부 (무조건 참)</returns>
         */
        public static bool AddUser(string userId, List<string> UserInfo)
        {
            UsersCache[userId] = UserInfo;
            return true;
        }
        
        /**
         * <summary>유저 DB의 마지막 쓰기 시간을 가져옵니다</summary>
         * <returns>마지막 쓰기 시간 (UTC)</returns>
         */
        private static DateTime GetUsersFileLastWriteUtc()
        {
            try
            {
                if (!File.Exists(UsersFileName))
                    return DateTime.MinValue;

                return File.GetLastWriteTimeUtc(UsersFileName);
            }
            catch (Exception ex)
            {
                Log.Error($"[UsersManager] Failed to get Users.db last write time: {ex}");
                return DateTime.MinValue;
            }
        }
        
        /**
         * <summary>유저 DB를 읽으려 시도합니다</summary>
         * <param name="loadedDb">(OUT) 로딩된 DB 내용</param>
         * <returns>성공 여부</returns>
         */
        private static bool TryReadUsersFromDisk(out Dictionary<string, List<string>> loadedDb)
        {
            loadedDb = null;

            var dbText = FileManager.ReadFile(UsersFileName);
            if (string.IsNullOrWhiteSpace(dbText))
                return false;

            try
            {
                var parsed = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(dbText);
                if (parsed == null)
                    return false;

                loadedDb = parsed;
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"[UsersManager] Failed to parse Users.db: {ex}");
                return false;
            }
        }
        
        /**
         * <summary>외부에서 DB가 수정된 경우 다시 로드합니다</summary>
         * <returns>로드 여부</returns>
         */
        private static bool ReloadIfExternallyModified()
        {
            DateTime diskWriteUtc = GetUsersFileLastWriteUtc();

            if (diskWriteUtc == DateTime.MinValue)
                return false;

            DateTime baseline = _lastUsersFileWriteUtc > DateTime.MinValue ? _lastUsersFileWriteUtc : _lastUsersFileLoadUtc;
            if (baseline != DateTime.MinValue && diskWriteUtc <= baseline)
                return false;

            if (!TryReadUsersFromDisk(out var loadedDb))
                return false;

            UsersCache = loadedDb;
            _lastUsersFileLoadUtc = diskWriteUtc;

            Timing.RunCoroutine(RefreshDiscordId());
            Log.Warn("[UsersManager] Users.db was modified externally. Reloaded cache from disk and skipped this save cycle to preserve external changes.");
            return true;
        }
        
        /**
         * <summary>유저 DB를 저장합니다</summary>
         */
        public static void SaveUsers()
        {
            if (!IsUsersFileLoaded)
                return;

            if (ReloadIfExternallyModified())
                return;

            Timing.RunCoroutine(RefreshDiscordId());

            var text = JsonConvert.SerializeObject(UsersCache, Formatting.None);
            FileManager.WriteFile(UsersFileName, text);

            _lastUsersFileWriteUtc = GetUsersFileLastWriteUtc();
            if (_lastUsersFileWriteUtc == DateTime.MinValue)
                _lastUsersFileWriteUtc = DateTime.UtcNow;
        }
        
        /**
         * <summary>유저 DB를 불러와 캐시에 저장합니다</summary>
         */
        public static void LoadUsers()
        {
            Timing.RunCoroutine(RefreshDiscordId());

            if (TryReadUsersFromDisk(out var loadedDb))
                UsersCache = loadedDb;

            DateTime writeUtc = GetUsersFileLastWriteUtc();
            _lastUsersFileLoadUtc = writeUtc;
            _lastUsersFileWriteUtc = writeUtc;

            IsUsersFileLoaded = true;
        }
        
        /**
         * <summary>연동된 유저 목록을 다시 로드합니다</summary>
         * <returns>MEC 코루틴</returns>
         */
        public static IEnumerator<float> RefreshDiscordId()
        {
            var validUsers = UsersManager.UsersCache
            .Where(x => x.Value.Count > 13 && x.Value[13] != "0")
            .GroupBy(userData => userData.Value[13])
            .ToDictionary(group => group.Key, group => group.First().Key);

            DiscordIdToUserId = validUsers;

            yield break;
        }
    }
}
