using Google.Protobuf.WellKnownTypes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace WpfDownloader.Util.LinkSaver
{
    public static class LinkSaveManager
    {
        //The default directory for saved links to look for
        public static string DefaultSavePath { get; set; }

        //Default link save file (json)
        public static string DefaultSaveLinkPath { get; set; }

        private static string prevLinkData; //Determines if link data was changed
        public static JToken linkData;

        static LinkSaveManager()
        {
            //DefaultSavePath = linkSavePath;
            //DefaultSaveLinkPath = defaultLinkPath;
        }

        public static async Task InitLinkManager()
        {
            if (!Directory.Exists(DefaultSavePath)) Directory.CreateDirectory(DefaultSavePath);
            if (!File.Exists(DefaultSaveLinkPath)) File.Create(DefaultSaveLinkPath);

            string jsonStr = await File.ReadAllTextAsync(DefaultSaveLinkPath);
            if (string.IsNullOrEmpty(jsonStr)) jsonStr = "[]";

            prevLinkData = jsonStr;
            linkData = JsonParser.Parse(jsonStr);
        }

        public static bool ContainsLinkData(LinkData searchData) =>
            linkData.Where(data => data["url"].ToString() == searchData.Url).Any();

        public static void RemoveLinkData(LinkData removeData)
        {
            var filteredData = linkData.Where(obj => obj["url"].ToString() != removeData.Url &&
                obj["fullPath"].ToString() != removeData.FullPath);

            linkData.Children = filteredData.ToList();
        }

        public static IEnumerable<LinkData> LoadData()
        {
            return linkData.Select(obj => new LinkData()
            {
                Url = obj["url"].ToString(),
                FullPath = obj["fullPath"].ToString(),
            });
        }

        public static void AddLinkData(LinkData dataToWrite)
        {
            string fullPath = dataToWrite.FullPath;
            string url = dataToWrite.Url;

            fullPath = fullPath.Replace("\\", "/");
            url = url.EndsWith("/") ? url.Substring(0, url.Length - 1) : url;
            fullPath = fullPath.EndsWith("/") ? fullPath.Substring(0, fullPath.Length - 1) : fullPath;

            dataToWrite.FullPath = fullPath;
            dataToWrite.Url = url;

            //If url is already saved, skip it
            var objToWrite = JsonParser.ParseObject(dataToWrite);
            if (linkData.Where(obj => obj["url"].Value == objToWrite["url"].Value &&
                obj["fullPath"].Value == objToWrite["fullPath"].Value).Any())
            {
                return;
            }


            linkData.Add(objToWrite);
        }

        public static async Task WriteLinkData()
        {
            if (prevLinkData == JsonParser.Serialize(linkData).ToString()) return; //If information didn't change

            await File.WriteAllTextAsync(DefaultSaveLinkPath, string.Empty);
            await File.WriteAllTextAsync(DefaultSaveLinkPath, JsonParser.Serialize(linkData).ToString());
        }

    }
}
