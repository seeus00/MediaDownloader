using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfDownloader.Util.Database
{
    public class DanbooruInfo : BasicDbInfo
    {
        public string Tags { set; get; }
        public string Artist { set; get; }
        public string PostId { set; get; }
        public string FileName { set; get; }
        public string FileExt { set; get; }


    }

    public class DanbooruDbLoader
    {
        private static readonly string DB_PATH =
            $"{Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)}/danbooru/danbooru.db";

        private static readonly string PARAMS =
            "(tags TEXT, artist TEXT, post_id TEXT, file_name TEXT, file_ext TEXT)";

        public static async Task LoadDbInfo(DanbooruInfo info)
        {
            try
            {
                using (var conn = new SqliteConnection($"DataSource={DB_PATH}"))
                {
                    await conn.OpenAsync();

                    using var cmd = new SqliteCommand("", conn);
                    //await cmd.ExecuteNonQueryAsync();

                    cmd.CommandText = $@"CREATE TABLE IF NOT EXISTS posts {PARAMS}";
                    await cmd.ExecuteNonQueryAsync();

                    cmd.CommandText =
                        $"INSERT INTO posts(tags, artist, post_id, file_name, file_ext) VALUES ('{info.Tags}', '{DatabaseLoader.RemoveIllegalChars(info.Artist)}', '{info.PostId}', '{DatabaseLoader.RemoveIllegalChars(info.FileName)}', '{info.FileExt}') ";
                    await cmd.ExecuteNonQueryAsync();

                    //cmd.CommandText = "end";
                    //await cmd.ExecuteNonQueryAsync();
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
                return;
            }
        }

    }
}
