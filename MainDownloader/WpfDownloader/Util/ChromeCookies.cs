﻿using Google.Protobuf.WellKnownTypes;
using K4os.Compression.LZ4;
using K4os.Compression.LZ4.Encoders;
using Microsoft.Data.Sqlite;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace ChromeCookie
{
    //Supports firefox too
    public static class ChromeCookies
    {
        private static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

        private static byte[] _key = null;
        private static readonly string CHROME_LOCAL_STATE_PATH =
            $"{Environment.GetEnvironmentVariable("LocalAppData")}/Google/Chrome/User Data/Local State";

        private static readonly string CHROME_COOKIES_PATH =
            $"{Environment.GetEnvironmentVariable("LocalAppData")}/Google/Chrome/User Data/Default/Network/Cookies";

        private static readonly string EDGE_LOCAL_STATE_PATH =
            $"{Environment.GetEnvironmentVariable("LocalAppData")}/Microsoft/Edge/User Data/Local State";

        private static readonly string EDGE_COOKIES_PATH =
            $"{Environment.GetEnvironmentVariable("LocalAppData")}/Microsoft/Edge/User Data/Default/Network/Cookies";

        private static readonly string BRAVE_LOCAL_STATE_PATH =
           $"{Environment.GetEnvironmentVariable("LocalAppData")}/BraveSoftware/Brave-Browser/User Data/Local State";

        private static readonly string BRAVE_COOKIES_PATH =
            $"{Environment.GetEnvironmentVariable("LocalAppData")}/BraveSoftware/Brave-Browser/User Data/Default/Network/Cookies";

        private static readonly string FIREFOX_USER_PATH =
            $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/Mozilla/Firefox";

        private static string FIREFOX_COOKIES_PATH = null;
        private static string DEFAULT_PROFILE;

        static ChromeCookies()
        {
            if (string.IsNullOrEmpty(FIREFOX_COOKIES_PATH))
            {
                DEFAULT_PROFILE = GetDefaultFirefoxProfile();
                string origCookiePath = $"{FIREFOX_USER_PATH}/{DEFAULT_PROFILE}/cookies.sqlite";
                string tempCookiePath = $"{FIREFOX_USER_PATH}/{DEFAULT_PROFILE}/cookies_temp.sqlite";

                if (File.Exists(tempCookiePath))
                {
                    File.Delete(tempCookiePath);
                }

                File.Copy(origCookiePath, tempCookiePath);


                FIREFOX_COOKIES_PATH = tempCookiePath;
            }
        }


        private static string GetDefaultFirefoxProfile()
        {
            string iniFilePath = $"{FIREFOX_USER_PATH}/profiles.ini";
            string data = File.ReadAllText(iniFilePath);

            return new Regex("Name=default-release.*Path=(.*?)$", 
                RegexOptions.Singleline | RegexOptions.Multiline).
                Match(data).Groups[1].ToString().Trim();
        }


        private static string Decrypt(byte[] data)
        {
            var lstData = data.Skip(3);
            var nonce = lstData.Take(12);
            var tag = lstData.TakeLast(16);

            var slicedData = lstData.SkipLast(16).Skip(12);

            byte[] decryptedKey = new byte[slicedData.Count()];
            using (var aes = new AesGcm(_key))
            {
                aes.Decrypt(nonce.ToArray(), slicedData.ToArray(), tag.ToArray(), decryptedKey);
            }

            return Encoding.UTF8.GetString(decryptedKey);
        }


        private static void InitKey(string localStatePath)
        {
            if (_key != null) return;

            var jsonStr = File.ReadAllText(localStatePath);
            var encryptedKey = JsonParser.Parse(jsonStr)["os_crypt"]["encrypted_key"].ToString();

            var baseKey = Convert.FromBase64String(encryptedKey);
            baseKey = new List<byte>(baseKey).Skip(5).ToArray();

            _key = ProtectedData.Unprotect(baseKey, null, DataProtectionScope.CurrentUser);
        }

        public static CookieCollection GetChromeCookies(string domain, string cookiePath, 
            string localStatePath)
        {
            InitKey(localStatePath);


            ////Create temporary copy
            string tempCookiePath = cookiePath + "_temp";
            using (var inputFile = new FileStream(
                cookiePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite))
            {
                using (var outputFile = new FileStream(tempCookiePath, FileMode.Create))
                {
                    var buffer = new byte[0x10000];
                    int bytes;

                    while ((bytes = inputFile.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        outputFile.Write(buffer, 0, bytes);
                    }
                }
            }

            var cookies = new CookieCollection();
            using (var connection = new SqliteConnection($"Data Source={tempCookiePath}"))
            {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText =
                    @$"SELECT host_key, path, expires_utc, name, value, encrypted_value, is_httponly 
                        FROM cookies WHERE host_key='{domain}'";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        
                        var name = (string) reader["name"];
                        var encryptedValue = (byte[]) reader["encrypted_value"];
                        cookies.Add(new Cookie(name, Decrypt(encryptedValue)));
                    }
                }
            }

            return cookies;
        }

  
        private static CookieCollection GetFirefoxCookies(string domain)
        {
            var cookies = new CookieCollection();
            using (var connection = new SqliteConnection($"Data Source={FIREFOX_COOKIES_PATH}"))
            {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText =
                    @$"SELECT host, path, isSecure, expiry, name, value, isHttpOnly from 
                        moz_cookies WHERE host='{domain}'";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var name = (string)reader["name"];
                        var value = (string)reader["value"];
                        cookies.Add(new Cookie(name, value));
                    }
                }
            }

            //Get session cookies
            string backups = $"{FIREFOX_USER_PATH}/{DEFAULT_PROFILE}/sessionstore-backups";
            string decodedSessionJsPath = $"{backups}/decoded.json";
            string sessionStoreJsPath = $"{backups}/recovery.jsonlz4";
            var p = new Process();
            p.StartInfo.FileName = "../../../tools/dejsonlz4.exe";
            p.StartInfo.Arguments = $"\"{sessionStoreJsPath}\" \"{decodedSessionJsPath}\"";
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.CreateNoWindow = true;
            p.Start();
            p.WaitForExit();
            
            string jsonStr = File.ReadAllText(decodedSessionJsPath);
            var data = JsonParser.Parse(jsonStr);
            var sessionCookies = data["cookies"].Where(info => info["host"].ToString() == domain).Select(info => 
                new Tuple<string, string>(info["name"].ToString(), (string)info["value"].ToString()));

            if (sessionCookies.Any())
            {
                foreach (var sessionCookie in sessionCookies)
                {
                    cookies.Add(new Cookie(HttpUtility.UrlEncode(sessionCookie.Item1), HttpUtility.UrlEncode(sessionCookie.Item2)));
                }
            }

            return cookies;
        }

        public static CookieCollection GetCookies(string domain)
        {
            const string userChoice = @"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\http\UserChoice";
            string progId;

            string browser = string.Empty;
            using (RegistryKey userChoiceKey = Registry.CurrentUser.OpenSubKey(userChoice))
            {
                if (userChoiceKey == null)
                {
                    return null;
                }
                object progIdValue = userChoiceKey.GetValue("Progid");
                if (progIdValue == null)
                {
                    return null;
                }
                progId = progIdValue.ToString();
                switch (progId)
                {
                    case "ChromeHTML":
                        return GetChromeCookies(domain, CHROME_COOKIES_PATH, CHROME_LOCAL_STATE_PATH);
                    case "BraveHTML":
                        return GetChromeCookies(domain, BRAVE_COOKIES_PATH, BRAVE_LOCAL_STATE_PATH);
                    case "MSEdgeHTM":
                        return GetChromeCookies(domain, EDGE_COOKIES_PATH, EDGE_LOCAL_STATE_PATH);
                    default:
                        if (progId.Contains("FirefoxURL"))
                        {
                            return GetFirefoxCookies(domain);
                        }
                        else
                        {
                            return null;
                        }
                }
            }
        }
    }
}
