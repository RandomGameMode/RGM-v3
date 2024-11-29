using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Exiled.API.Features;

using static RGM.Variables.ServerManagers;

namespace RGM.API.Features
{
    public static class FileManager
    {
        public static string FolderPath => Path.Combine(Paths.Configs, "RGM");

        public static void CreateFolder()
        {
            if (!Directory.Exists(FolderPath))
                Directory.CreateDirectory(FolderPath);
        }

        public static void WriteFile(string fileName, string content)
        {
            File.WriteAllText(Path.Combine(FolderPath, fileName), content);
        }

        public static string ReadFile(string fileName)
        {
            if (!File.Exists(Path.Combine(FolderPath, fileName)))
                File.WriteAllText(Path.Combine(FolderPath, fileName), "");

            return File.ReadAllText(Path.Combine(FolderPath, fileName));
        }
    }

    public static class UsersManager
    {
        /*
        Exp - 0
        RP - 1
        Cash - 2
        보유한 킬 이펙트 - 3
        장착한 킬 이펙트 - 4
        커스텀 닉네임 - 5
        커스텀 인포 - 6
        보유한 커스터마이징 - 7
        보유한 페인트 - 8
        장착한 페인트 - 9
        */

        public static string UsersFileName = Path.Combine(Paths.Configs, "RGM/Users.txt");
        public static Dictionary<string, List<string>> UsersCache = new Dictionary<string, List<string>>();

        public static string CheckUser(string UserId, int num)
        {
            if (UsersCache.ContainsKey(UserId) && num >= 0 && num < UsersCache[UserId].Count)
                return UsersCache[UserId][num];

            return null;
        }

        public static bool AddUser(string UserId, List<string> UserInfo) 
        {
            UsersCache[UserId] = UserInfo;

            return true;
        }

        public static void SaveUsers()
        {
            if (IsUsersFileLoaded)
            {
                var text = string.Join("\n", UsersCache.Select(x => $"{x.Key};{string.Join(";", x.Value)}"));

                FileManager.WriteFile(UsersFileName, text);
            }
        }

        public static void LoadUsers()
        {
            var text = FileManager.ReadFile(UsersFileName);

            if (string.IsNullOrWhiteSpace(text))
                return;

            UsersCache.Clear();

            foreach (var line in text.Split('\n'))
            {
                var parts = line.Split(';');

                if (parts.Length != parts.Count())
                    continue;

                UsersCache.Add(parts[0], parts.Skip(1).ToList());
            }

            IsUsersFileLoaded = true;
        }
    }
}
