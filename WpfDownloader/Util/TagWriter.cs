using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfDownloader.Sites;
using WpfDownloader.Util.Database;

namespace Downloader.Util
{
    public class TagWriter
    {
        private static readonly string DB_FILE = "info.db";
        private static readonly string JSON_FILE = $"{Site.DEFAULT_PATH}/nhentai/info.json";

        public static async Task WriteTags(JToken tags, string path, string
            filename = null)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            path += "/info.json";

            if (await Task.Run(() => File.Exists(path)))
                return;

            string jsonStr = JsonParser.Serialize(tags).ToString();
            await File.WriteAllTextAsync(path, jsonStr);
        }

        public static async Task WriteNhentaiTags(NhentaiInfo info)
        {
            string basePath = $"{Site.DEFAULT_PATH}/nhentai";
            if (!Directory.Exists(basePath)) Directory.CreateDirectory(basePath);

            if (!File.Exists(JSON_FILE))
            {
                await File.WriteAllTextAsync(JSON_FILE, string.Empty);
            }

            var addInfo = new JDict();
            addInfo["code"] = new JType(info.CodeId.ToString());
            addInfo["title"] = new JType(info.Title);
            addInfo["pages"] = new JType(info.Pages.ToString());
            addInfo["name"] = new JType(info.Title);
            addInfo["cover_name"] = new JType(info.CoverName);
            addInfo["tags"] = new JType(info.Tags);
            addInfo["artist"] = new JType(info.Artist);

            string currInfo = await File.ReadAllTextAsync(JSON_FILE);
            if (string.IsNullOrEmpty(currInfo))
            {
                var arrOfEntries = new JArray();
                arrOfEntries.Add(addInfo);

                await File.WriteAllTextAsync(JSON_FILE, string.Empty);
                await File.WriteAllTextAsync(JSON_FILE, JsonParser.Serialize(arrOfEntries).ToString());
            }
            else
            {
                var data = JsonParser.Parse(currInfo);
                if (data.Where(entry => entry["code"].Value == info.CodeId.ToString()).Any())
                {
                    //Entry already exist, don't add duplicates
                    return;
                }
                data.Add(addInfo);

                await File.WriteAllTextAsync(JSON_FILE, string.Empty);
                await File.WriteAllTextAsync(JSON_FILE, JsonParser.Serialize(data).ToString());
            }
        }

        public static async Task WriteTags(string tags, string path, string
            filename = null)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            filename = (!string.IsNullOrEmpty(filename)) ? filename + ".db" : DB_FILE;

            path = $"{path}/{filename}";
            if (await Task.Run(() => File.Exists(path)))
                return;

            try
            {
                using (var conn = new SqliteConnection($"DataSource={path}"))
                {
                    await conn.OpenAsync();

                    using var cmd = new SqliteCommand("begin", conn);
                    await cmd.ExecuteNonQueryAsync();

                    cmd.CommandText = @"CREATE TABLE info(tags TEXT)";
                    await cmd.ExecuteNonQueryAsync();

                    cmd.CommandText = $"INSERT INTO info(tags) VALUES('{tags}')";
                    await cmd.ExecuteNonQueryAsync();

                    cmd.CommandText = "end";
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            catch (Exception e)
            {
                return;
            }

        }
    }
}
