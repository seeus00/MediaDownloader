using ChromeCookie;
using Downloader.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using WpfDownloader.WpfData;

namespace WpfDownloader.Sites
{
    public class Twitter : Site
    {
        private static readonly string GET_USER_INFO =
            "https://twitter.com/i/api/graphql/1CL-tn62bpc-zqeQrWm4Kw/UserByScreenName?variables=";

        private static readonly string GET_USER_TWEETS =
            "https://twitter.com/i/api/graphql/jpCmlX6UgnPEZJknGKbmZA/UserTweets?variables=";

        private static readonly string AUTH =
            "AAAAAAAAAAAAAAAAAAAAANRILgAAAAAAnNwIzUejRCOuH5E6I8xnZz4puTs%3D1Zv7ttfk8LF81IUq16cHjhLTvJu4FA33AGWWjCpTnA";

        private static readonly string VALID_TWEET_TYPE =
            "Tweet";


        private static readonly List<Tuple<string, string>> INFO_HEADERS =
            new List<Tuple<string, string>>()
            {
                new Tuple<string, string>("Authorization", $"Bearer {AUTH}"),
                new Tuple<string, string>("User-Agent", Requests.DEFAULT_USER_AGENT),
            };

        private string _name;

        public Twitter(string url, string args) : base(url, args)
        {
            _name = Url.Split('/').Last();
        }

        public override async Task DownloadAll(UrlEntry entry)
        {
            entry.StatusMsg = "Retrieving";
            entry.Name = $"[Twitter] {_name}";

            var mediaUrls = await GetMediaUrls();
            var newPath = $"{DEFAULT_PATH}/twitter/{_name}";

            await DownloadUtil.DownloadAllUrls(mediaUrls, newPath, entry);
        }

        private async Task<IEnumerable<string>> GetMediaUrls()
        {
            var tokenJson = 
                await Requests.GetStrPost("https://api.twitter.com/1.1/guest/activate.json", INFO_HEADERS);
            string guestToken = JsonParser.Parse(tokenJson)["guest_token"].Value;
            //var twitCookies = ChromeCookies.GetCookies(".twitter.com");

            //string guestToken = twitCookies
            //    .Where(cookie => cookie.Name == "gt")
            //    .FirstOrDefault()
            //    .Value;

            var data = new JDict();
            data["screen_name"] = new JType(_name);
            data["withSafetyModeUserFields"] = new JType("true");
            data["withSuperFollowsUserFields"] = new JType("true");
            data["withNftAvatar"] = new JType("false");

            INFO_HEADERS.Add(new Tuple<string, string>("x-guest-token", guestToken));
            string jsonStr = JsonParser.Serialize(data).ToString();
            string encodedStr = HttpUtility.UrlEncode(jsonStr);

            string infoJsonStr = 
                await Requests.GetStr(GET_USER_INFO + encodedStr, INFO_HEADERS);

            var infoData = JsonParser.Parse(infoJsonStr);
            var result = infoData["data"]["user"]["result"];

            string id = result["id"].Value;
            string restId = result["rest_id"].Value;

            var tweetsData = new JDict();
            tweetsData["userId"] = new JType(restId);
            tweetsData["count"] = new JType("20");
            tweetsData["withTweetQuoteCount"] = new JType("true");
            tweetsData["includePromotedContent"] = new JType("true");
            tweetsData["withQuickPromoteEligibilityTweetFields"] = new JType("false");
            tweetsData["withSuperFollowsUserFields"] = new JType("true");
            tweetsData["withUserResults"] = new JType("true");
            tweetsData["withNftAvatar"] = new JType("false");
            tweetsData["withBirdwatchPivots"] = new JType("false");
            tweetsData["withReactionsMetadata"] = new JType("false");
            tweetsData["withReactionsPerspective"] = new JType("false");
            tweetsData["withSuperFollowsTweetFields"] = new JType("true");
            tweetsData["withVoice"] = new JType("true");

            jsonStr = JsonParser.Serialize(tweetsData).ToString();
            encodedStr = HttpUtility.UrlEncode(jsonStr);

            var urls = new List<string>();
            while (true)
            {
                jsonStr = JsonParser.Serialize(tweetsData).ToString();
                encodedStr = HttpUtility.UrlEncode(jsonStr);

                var resp =
                    await Requests.GetReq(GET_USER_TWEETS + encodedStr, INFO_HEADERS);

                if (!resp.IsSuccessStatusCode)
                    break;

                string tweetJsonStr = await resp.Content.ReadAsStringAsync();  
                var tweetsJsonData = JsonParser.Parse(tweetJsonStr);

                if (tweetsJsonData["errors"] != null)
                    break;

                var initEntries = tweetsJsonData["data"]["user"]["result"]["timeline"]["timeline"]["instructions"]
                    .First()["entries"];
                var entries = initEntries.Take(initEntries.Count() - 2);
                var botCursor = initEntries.Skip(initEntries.Count() - 2)
                    .Where(cursor => cursor["entryId"].Value.Contains("bottom"))
                    .FirstOrDefault();

                if (botCursor == null)
                    break;
                
                foreach (var entry in entries)
                {
                    var itemContent = entry["content"]["itemContent"];
                    if (itemContent == null)
                        continue;
                    if (itemContent["tweet_results"]["result"] == null ||
                        itemContent["tweet_results"]["result"]["__typename"].Value != VALID_TWEET_TYPE)
                        continue;

                    if (itemContent["tweet_results"]["result"]["legacy"]["entities"] == null)
                        continue;

                    var medias = itemContent["tweet_results"]["result"]["legacy"]["entities"]["media"];
                    if (medias == null) continue;

                    var mediaUrls = 
                        medias.Select(media => media["media_url_https"].Value);
                    
                    if (mediaUrls != null && mediaUrls.Any())
                        urls.AddRange(mediaUrls);
                }

                tweetsData["cursor"] = new JType(botCursor["content"]["value"].Value);
                Debug.WriteLine(tweetsData["cursor"].Value);
            }

            return urls;
        }
    }
}
