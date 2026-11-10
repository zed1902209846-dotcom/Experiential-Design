using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace SolarSystemAR
{
    public enum AccountResult
    {
        Success,
        InvalidName,
        InvalidPassword,
        NameTaken,
        NotFound,
        WrongPassword
    }

    /// 本地演示账号存储：JSON 文件 + SHA256 哈希，不联网、不存明文。
    public static class AccountStore
    {
        [Serializable]
        private class Account
        {
            public string name;
            public string hash;
        }

        [Serializable]
        private class AccountDB
        {
            public List<Account> accounts = new List<Account>();
        }

        private static readonly object Lock = new object();

        private static string FilePath =>
            Path.Combine(Application.persistentDataPath, "accounts.json");

        public static AccountResult Register(string name, string password)
        {
            name = (name ?? string.Empty).Trim();
            if (!IsValidName(name)) return AccountResult.InvalidName;
            if (!IsValidPassword(password)) return AccountResult.InvalidPassword;

            lock (Lock)
            {
                var db = Load();
                if (db.accounts.Exists(a => a.name == name)) return AccountResult.NameTaken;
                db.accounts.Add(new Account { name = name, hash = Hash(name, password) });
                Save(db);
            }
            return AccountResult.Success;
        }

        public static AccountResult Login(string name, string password)
        {
            name = (name ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(name)) return AccountResult.NotFound;

            lock (Lock)
            {
                var db = Load();
                var acc = db.accounts.Find(a => a.name == name);
                if (acc == null) return AccountResult.NotFound;
                return acc.hash == Hash(name, password ?? string.Empty)
                    ? AccountResult.Success
                    : AccountResult.WrongPassword;
            }
        }

        private static bool IsValidName(string n)
        {
            return n.Length >= 3 && n.Length <= 16 && !n.Contains(" ");
        }

        private static bool IsValidPassword(string p)
        {
            return !string.IsNullOrEmpty(p) && p.Length >= 6;
        }

        private static string Hash(string name, string password)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes("vrss:" + name + ":" + password));
                var sb = new StringBuilder(bytes.Length * 2);
                foreach (var b in bytes)
                    sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
                return sb.ToString();
            }
        }

        private static AccountDB Load()
        {
            try
            {
                if (File.Exists(FilePath))
                    return JsonUtility.FromJson<AccountDB>(File.ReadAllText(FilePath)) ?? new AccountDB();
            }
            catch (Exception e)
            {
                Debug.LogWarning("AccountStore: 读取账号文件失败 " + e.Message);
            }
            return new AccountDB();
        }

        private static void Save(AccountDB db)
        {
            File.WriteAllText(FilePath, JsonUtility.ToJson(db, true));
        }
    }
}
