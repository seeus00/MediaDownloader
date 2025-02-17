using ChromeCookie;
using Downloader.Data;
using Downloader.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Downloader.Sites
{
    public class Yandex : Site
    {
        private static readonly string FETCH_API = "https://disk.yandex.com/public/api/fetch-list";

        private string _creator;

        public Yandex(string url, string args) : base(url, args)
        {

        }

        public override async Task DownloadAll()
        {
            using var messageBar = new MessageBar();
            messageBar.PrintMsg("[Yandex] downloading", Url);

            var mediaUrls = await GetMediaUrls();

            var newPath = $"{DEFAULT_PATH}/Yandex/{_creator}";
            await DownloadUtil.DownloadAllUrls(mediaUrls, newPath, $"{_creator}",
                messageBar);
        }

        public async Task<IEnumerable<ImgData>> GetMediaUrls()
        {
            string html = await Requests.GetStr(Url);
            string initDataJsonStr = 
                new Regex("store-prefetch\">(.*)<").Match(html).Groups[1].Value;

            var initData = JsonParser.Parse(initDataJsonStr);

            string hash = initData["resources"].First()["hash"].Value;
            string sk = initData["environment"]["sk"].Value;

            var headers = new List<Tuple<string, string>>()
            {
                new Tuple<string, string>("User-Agent", Requests.DEFAULT_USER_AGENT),
                new Tuple<string, string>("Origin", "https://disk.yandex.com")
            };

            var payload = new JDict();
            payload["hash"] = new JType(hash);
            payload["offset"] = new JType("40");
            payload["withSizes"] = new JType("true");
            payload["sk"] = new JType(sk);

            string jsonStr = await Requests.GetStrPost(FETCH_API, payload, headers);
            var data = JsonParser.Parse(jsonStr);

            _creator = RemoveIllegalChars(data["resources"].First()["name"].Value);

            var urls = new List<ImgData>();

            int currOffset = 40;
            while (data["completed"].Value != "true")
            {
                var medias = data["resources"]
                    .Where(r => r["meta"]["mediatype"] != null && r["meta"]["mediatype"].Value.Contains("image"));

                urls.AddRange(medias
                    .Select(media => new ImgData()
                    {
                        Url = media["meta"]["original"].Value,
                        Filename = media["name"].Value
                    }));

                currOffset += 40;

                payload["offset"] = new JType(currOffset.ToString());
                jsonStr = await Requests.GetStrPost(FETCH_API, payload, headers);
                data = JsonParser.Parse(jsonStr);
            }

            return urls;
        }
    }
}
