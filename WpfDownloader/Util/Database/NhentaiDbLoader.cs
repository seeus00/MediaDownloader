using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfDownloader.Util.Database
{
    public class NhentaiInfo : BasicDbInfo
    {
        public int CodeId { set; get; }
        public string Title { set; get; }
        public int Pages { get; set; }
        public string CoverName { get; set; }
        public string Tags { get; set; }
        public string Artist { get; set; }
    }

    public static class NhentaiDbLoader 
    {
        public static async Task LoadDbInfo(NhentaiInfo info)
        {
            try
            {
                string connStr = "Server=localhost,3306;uid=root; pwd=Caseyisawizard06;database=base_scheme;";
                //var con = ConfigurationManager.ConnectionStrings["local"].ToString();
                using (var conn = new MySqlConnection(connStr))
                {
                    await conn.OpenAsync();

                    string cmdText = "INSERT IGNORE INTO codes(code_id, title, pages, cover_name, tags, artist) VALUES(@code_id, @title, @pages, @cover_name, @tags, @artist)";
                    MySqlCommand cmd = new MySqlCommand(cmdText, conn);
                    cmd.Parameters.AddWithValue("@code_id", info.CodeId);
                    cmd.Parameters.AddWithValue("@title", info.Title);
                    cmd.Parameters.AddWithValue("@pages", info.Pages);
                    cmd.Parameters.AddWithValue("@cover_name", info.CoverName);
                    cmd.Parameters.AddWithValue("@tags", info.Tags);
                    cmd.Parameters.AddWithValue("@artist", info.Artist);
                    await cmd.ExecuteNonQueryAsync();
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
