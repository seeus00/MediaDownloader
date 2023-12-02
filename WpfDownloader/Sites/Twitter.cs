using ChromeCookie;
using Downloader.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using WpfDownloader.Util.UserAgent;
using WpfDownloader.WpfData;

namespace WpfDownloader.Sites
{
    public class Twitter : Site
    {
        private static readonly string GET_USER_INFO =
            "https://twitter.com/i/api/graphql/G3KGOASz96M-Qu0nwmGXNg/UserByScreenName";

        private static readonly string GET_USER_MEDIA =
            "https://twitter.com/i/api/graphql/lo965xQZdN2-eSM1Jc-W_A/UserMedia";

        private static readonly string AUTH =
            "AAAAAAAAAAAAAAAAAAAAANRILgAAAAAAnNwIzUejRCOuH5E6I8xnZz4puTs%3D1Zv7ttfk8LF81IUq16cHjhLTvJu4FA33AGWWjCpTnA";

        private static readonly string VALID_TWEET_TYPE =
            "Tweet";


        private static CookieContainer cookieContainer = null;

        private string name;

        public Twitter(string url, string args) : base(url, args)
        {
            name = Url.Split('/').Last();
        }

        public override async Task DownloadAll(UrlEntry entry)
        {
            entry.StatusMsg = "Retrieving";
            entry.Name = $"[Twitter] {name}";

            var mediaUrls = await GetMediaUrls(entry);
            var newPath = $"{DEFAULT_PATH}/twitter/{name}";

            await DownloadUtil.DownloadAllUrls(mediaUrls, newPath, entry);
        }

        private async Task<IEnumerable<string>> GetMediaUrls(UrlEntry urlEntry)
        {
            //var tokenJson = 
            //    await Requests.GetStrPost("https://api.twitter.com/1.1/guest/activate.json", INFO_HEADERS);
            //string guestToken = JsonParser.Parse(tokenJson)["guest_token"].Value;

            var twitCookies = ChromeCookies.GetCookies(".twitter.com");
            if (cookieContainer == null)
            {
                var baseAddress = new Uri($"https://twitter.com");
                cookieContainer = new CookieContainer();

                cookieContainer.Add(baseAddress, twitCookies);
                Requests.AddCookies(cookieContainer, baseAddress);
            }

            string csrfToken = twitCookies["ct0"].Value;
            var headers = new List<Tuple<string, string>>()
            {
                new Tuple<string, string>("Authorization", $"Bearer {AUTH}"),
                new Tuple<string, string>("Referer", "https://twitter.com/"),
                new Tuple<string, string>("host", "twitter.com"),
                new Tuple<string, string>("x-csrf-token", csrfToken),
                new Tuple<string, string>("x-twitter-client-language", "en"),
                new Tuple<string, string>("x-twitter-active-user", "yes"),
                new Tuple<string, string>("User-Agent", UserAgentUtil.CURR_USER_AGENT),
            };

            var data = new JDict();
            data["features"] = new JDict();
            //data["features"] = JsonParser.Parse("{\"hidden_profile_likes_enabled\":true,\"hidden_profile_subscriptions_enabled\":true,\"responsive_web_graphql_exclude_directive_enabled\":true,\"verified_phone_label_enabled\":false,\"subscriptions_verification_info_is_identity_verified_enabled\":false,\"subscriptions_verification_info_verified_since_enabled\":true,\"highlights_tweets_tab_ui_enabled\":true,\"creator_subscriptions_tweet_preview_api_enabled\":true,\"responsive_web_graphql_skip_user_profile_image_extensions_enabled\":false,\"responsive_web_graphql_timeline_navigation_enabled\":true}");

            data["features"]["rweb_lists_timeline_redesign_enabled"] = new JType("true");
            data["features"]["responsive_web_graphql_exclude_directive_enabled"] = new JType("true");
            data["features"]["verified_phone_label_enabled"] = new JType("false");
            data["features"]["creator_subscriptions_tweet_preview_api_enabled"] = new JType("true");
            data["features"]["responsive_web_graphql_timeline_navigation_enabled"] = new JType("true");
            data["features"]["responsive_web_graphql_skip_user_profile_"] = new JType("false");
            data["features"]["responsive_web_graphql_skip_user_profile_image_extensions_enabled"] = new JType("false");
            data["features"]["tweetypie_unmention_optimization_enabled"] = new JType("true");
            data["features"]["responsive_web_edit_tweet_api_enabled"] = new JType("true");
            data["features"]["graphql_is_translatable_rweb_tweet_is_translatable_enabled"] = new JType("true");
            data["features"]["view_counts_everywhere_api_enabled"] = new JType("true");
            data["features"]["longform_notetweets_consumption_enabled"] = new JType("true");
            data["features"]["tweet_awards_web_tipping_enabled"] = new JType("false");
            data["features"]["freedom_of_speech_not_reach_fetch_enabled"] = new JType("true");
            data["features"]["standardized_nudges_misinfo"] = new JType("true");
            data["features"]["tweet_with_visibility_results_prefer_gql_"] = new JType("false");
            data["features"]["limited_actions_policy_enabled"] = new JType("false");
            data["features"]["interactive_text_enabled"] = new JType("true");
            data["features"]["responsive_web_text_conversations_enabled"] = new JType("false");
            data["features"]["longform_notetweets_rich_text_read_enabled"] = new JType("true");
            data["features"]["longform_notetweets_inline_media_enabled"] = new JType("false");
            data["features"]["responsive_web_enhance_cards_enabled"] = new JType("false");
            data["features"]["hidden_profile_subscriptions_enabled"] = new JType("false");
            data["features"]["subscriptions_verification_info_is_identity_verified_enabled"] = new JType("false");
            data["features"]["hidden_profile_likes_enabled"] = new JType("false");
            data["features"]["highlights_tweets_tab_ui_enabled"] = new JType("false");
            data["features"]["subscriptions_verification_info_verified_since_enabled"] = new JType("false");



            data["variables"] = new JDict();
            data["variables"]["screen_name"] = new JType(name);
            data["variables"]["withSafetyModeUserFields"] = new JType("true");

            string jsonStr = JsonParser.Serialize(data).ToString();
            string encodedStr = HttpUtility.UrlEncode(jsonStr);


            await Task.Delay(1000);

            var r = await Requests.Get(GET_USER_INFO, data, headers);
            string infoJsonStr = await r.Content.ReadAsStringAsync();

            var infoData = JsonParser.Parse(infoJsonStr);
            var result = infoData["data"]["user"]["result"];

            string id = result["id"].ToString();
            string restId = result["rest_id"].ToString();


            data["variables"] = new JDict();
            data["variables"]["userId"] = new JType(restId);
            data["variables"]["count"] = new JType("100");
            data["variables"]["includePromotedContent"] = new JType("false");
            data["variables"]["withClientEventToken"] = new JType("false");
            data["variables"]["withBirdwatchNotes"] = new JType("false");
            data["variables"]["withVoice"] = new JType("true");
            data["variables"]["withV2Timeline"] = new JType("true");

            data["features"]["tweet_with_visibility_results_prefer_gql_limited_actions_policy_enabled"] = new JType("false");

            var urls = new List<string>();
            while (true && !urlEntry.CancelToken.IsCancellationRequested)
            {
                await Task.Delay(1000);

                var resp = await Requests.Get(GET_USER_MEDIA, data, headers);

                if (!resp.IsSuccessStatusCode) break;

                string tweetJsonStr = await resp.Content.ReadAsStringAsync();
                var tweetsJsonData = JsonParser.Parse(tweetJsonStr);

                if (tweetsJsonData["errors"] != null)
                    break;

                var initEntries = tweetsJsonData["data"]["user"]["result"]["timeline_v2"]["timeline"]["instructions"]
                    .First()["entries"];

                //If entries only has top and bottom cursors, that means the end was reached
                if (initEntries.Count() == 2) break;

                var entries = initEntries.Take(initEntries.Count() - 2);
                var botCursor = initEntries.Skip(initEntries.Count() - 2)
                    .Where(cursor => cursor["entryId"].ToString().Contains("bottom"))
                    .FirstOrDefault();


                foreach (var entry in entries)
                {
                    var itemContent = entry["content"]["itemContent"];
                    if (itemContent == null)
                        continue;

                    var tweetResult = itemContent["tweet_results"]["result"];
                    if (tweetResult == null || tweetResult["legacy"] == null
                        /*itemContent["tweet_results"]["result"]["__typename"].Value != VALID_TWEET_TYPE*/)
                        continue;

                    var legacy = tweetResult["legacy"];
                    //if (legacy["entities"] != null)
                    //{
                    //    urls.AddRange(legacy["entities"]["media"]
                    //        .Where(media => media["media_url_https"] != null)
                    //        .Select(media => media["media_url_https"].Value + "?format=jpg&name=large"));

                    //}

                    if (legacy["extended_entities"] != null)
                    {
                        urls.AddRange(legacy["extended_entities"]["media"]
                            .Where(media => media["media_url_https"] != null)
                            .Select(media => media["media_url_https"].Value + "?format=jpg&name=large"));


                        var videoVariants = legacy["extended_entities"]["media"]
                            .Where(media => media["video_info"] != null)
                            .Select(media => media["video_info"]["variants"]);

                        foreach (var variant in videoVariants)
                        {
                            var videosWithBitrates = variant.Where(video => video["bitrate"] != null);
                            //Sort by bitrate, get video with the highest bitrate = highest quality (usually 720p)
                            var highestQualVideo = videosWithBitrates.MaxBy(media => int.Parse(media["bitrate"].ToString()));
                            urls.Add(highestQualVideo["url"].ToString());
                        }
                    }
                }

                if (botCursor == null)
                    break;

                data["variables"]["cursor"] = new JType(botCursor["content"]["value"].Value);

            }

            return urls;
        }
    }
}
