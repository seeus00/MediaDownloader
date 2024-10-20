﻿using Azure;
using MySqlX.XDevAPI;
using PuppeteerSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using WpfDownloader.Util;
using WpfDownloader.Util.HttpExtensions;
using WpfDownloader.WpfData;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Downloader.Util
{
    public static class Requests
    {
        private static readonly CookieContainer container = new CookieContainer();
        private static readonly HttpClientHandler _handler = new HttpClientHandler()
        {
            CookieContainer = container,
            AllowAutoRedirect = false,
            MaxAutomaticRedirections = 50
        };

        private static HttpClient client;
        public static readonly string DEFAULT_USER_AGENT =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/89.0.4389.114 Safari/537.36";

        private static readonly int MAX_RETRIES = 5;
        private static readonly int TIMEOUT_RETRY = 2000; //Wait x seconds before retrying

        static Requests()
        {
            client = new HttpClient(_handler);
            //client.DefaultRequestHeaders.Add("User-Agent", DEFAULT_USER_AGENT);

            client.DefaultRequestVersion = HttpVersion.Version20;
            client.Timeout = TimeSpan.FromMinutes(10);

            client.DefaultRequestHeaders.UserAgent.TryParseAdd("request");
        }

        public static async Task<string> GetLatestEdgeUserAgent()
        {
            string html = await GetStr("https://www.whatismybrowser.com/guides/the-latest-user-agent/windows");
            string edgeUa = 
                new Regex("Edge.*?\"code\">(.*?)<", RegexOptions.Singleline).Match(html).Groups[1].Value;

            string chromeHtml = await GetStr("https://omahaproxy.appspot.com/all?csv=1");
            string chromeUaVersion = 
                "Chrome/" + new Regex("win64,stable,(.*?),").Match(chromeHtml).Groups[1].Value + " ";

            return new Regex("Chrome\\/(.*?) ").Replace(edgeUa, chromeUaVersion);
        }


        public static string GetFileNameWithoutExtension(string url)
        {
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
                uri = new Uri(url);

            return Path.GetFileNameWithoutExtension(uri.LocalPath);
        }

        public static string GetFileNameFromUrl(string url)
        {
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
                uri = new Uri(url);

            return Path.GetFileName(uri.LocalPath);
        }

        public static string GetFileExtensionFromUrl(string url)
        {
            url = url.Split('?')[0];
            url = url.Split('/').Last();
            return url.Contains('.') ? url.Substring(url.LastIndexOf('.')) : "";
        }

        public static string DuplicateFilePath(string path)
        {
            string fn = Path.GetFileName(path);
            path = Path.GetDirectoryName(path);

            int i = 1;
            string saveFileAs = fn;
            while (File.Exists($"{path}/{saveFileAs}"))
            {
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(fn);
                string ext = Path.GetExtension(fn);
                saveFileAs = fileNameWithoutExt + " " + string.Concat("(", i.ToString(), ")", ext);

                i++;
            }

            return $"{path}/{saveFileAs}";
        }

        public static IEnumerable<Cookie> GetCookies(Uri uri) =>
            container.GetCookies(uri).Cast<Cookie>();

        public static void AddCookies(CookieContainer addContainer, Uri uri)
        {
            foreach (var cookie in addContainer.GetCookies(uri))
            {
                container.Add((Cookie)cookie);
            }
        }

        public static async Task<byte[]> GetBytes(string url)
        {
            var req = await client.GetAsync(url);
            if (!req.IsSuccessStatusCode)
            {
                return null;
            }


            return await req.Content.ReadAsByteArrayAsync();
        }
        public static async Task<byte[]> GetBytes(string url, List<Tuple<string, string>> headers = null, CancellationToken cancelToken = 
            default(CancellationToken))
        {
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, url))
            {
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        if (requestMessage.Headers.Contains(header.Item1))
                        {
                            requestMessage.Headers.Remove(header.Item1);
                        }
                        requestMessage.Headers.TryAddWithoutValidation(header.Item1, header.Item2);
                    }
                }

                var req = await client.SendAsync(requestMessage, cancelToken);
                if (!req.IsSuccessStatusCode)
                {
                    return null;
                }

                return await req.Content.ReadAsByteArrayAsync();
            }
        }

        public static async Task<string> GetStr(string url, List<Tuple<string, string>> headers,
            CancellationToken cancelToken = default(CancellationToken))
        {
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, url))
            {
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        if (requestMessage.Headers.Contains(header.Item1))
                        {
                            requestMessage.Headers.Remove(header.Item1);
                        }
                        requestMessage.Headers.TryAddWithoutValidation(header.Item1, header.Item2);
                    }
                }

                var req = await client.SendAsync(requestMessage, cancelToken);
                if (!req.IsSuccessStatusCode)
                {
                    return null;
                }

                return await req.Content.ReadAsStringAsync();
            }
        }

        public static async Task<string> GetStr(string url,
           JDict payload, List<Tuple<string, string>> headers = null, 
           CancellationToken cancelToken = default(CancellationToken))
        {
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, url))
            {
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        if (requestMessage.Headers.Contains(header.Item1))
                        {
                            requestMessage.Headers.Remove(header.Item1);
                        }
                        requestMessage.Headers.TryAddWithoutValidation(header.Item1, header.Item2);
                    }
                }

                string payloadStr = JsonParser.Serialize(payload).ToString();
                requestMessage.Content = new StringContent(payloadStr,
                    Encoding.UTF8, "application/json");

                var resp = await client.SendAsync(requestMessage, cancelToken);
                if (!resp.IsSuccessStatusCode)
                {
                    return resp.ReasonPhrase;
                }

                return await resp.Content.ReadAsStringAsync();
            }
        }

        public static async Task<HttpResponseMessage> Get(string url, JDict payload = null, List<Tuple<string, string>> headers = null,
           CancellationToken cancelToken = default(CancellationToken))
        {
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, url))
            {
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        if (requestMessage.Headers.Contains(header.Item1))
                        {
                            requestMessage.Headers.Remove(header.Item1);
                        }
                        requestMessage.Headers.TryAddWithoutValidation(header.Item1, header.Item2);
                    }
                }

                if (payload != null)
                {
                    string payloadStr = JsonParser.Serialize(payload).ToString();
                    requestMessage.Content = new StringContent(payloadStr,
                        Encoding.UTF8, "application/json");
                }

                var resp = await client.SendAsync(requestMessage, cancelToken);
                return resp;
            }
        }

        //public static async Task<HttpResponseMessage> Get(string url, List<Tuple<string, string>> headers = null,
        //   CancellationToken cancelToken = default(CancellationToken))
        //{
        //    using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, url))
        //    {
        //        if (headers != null)
        //        {
        //            foreach (var header in headers)
        //            {
        //                if (requestMessage.Headers.Contains(header.Item1))
        //                {
        //                    requestMessage.Headers.Remove(header.Item1);
        //                }
        //                requestMessage.Headers.TryAddWithoutValidation(header.Item1, header.Item2);
        //            }
        //        }

        //        var resp = await client.SendAsync(requestMessage, cancelToken);
        //        return resp;
        //    }
        //}

        public static async Task<HttpResponseMessage> Get(string url)
        {
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, url))
            {
                var resp = await client.SendAsync(requestMessage);
                return resp;
            }
        }


        public static async Task<HttpResponseMessage> Post(string url, List<KeyValuePair<string, string>> payload,
            List<Tuple<string, string>> headers = null)
        {
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, url))
            {
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        if (requestMessage.Headers.Contains(header.Item1))
                        {
                            requestMessage.Headers.Remove(header.Item1);
                        }
                        requestMessage.Headers.TryAddWithoutValidation(header.Item1, header.Item2);
                    }
                }
                
                var formEncodedContent = new FormUrlEncodedContent(payload);
                requestMessage.Content = formEncodedContent;
                var resp = await client.SendAsync(requestMessage);
                resp.EnsureSuccessStatusCode();

                return resp;
            }
        }

        public static async Task<HttpResponseMessage> PostAsync(string url, List<Tuple<string, string>> headers = null)
        {
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, url))
            {
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        if (requestMessage.Headers.Contains(header.Item1))
                        {
                            requestMessage.Headers.Remove(header.Item1);
                        }
                        requestMessage.Headers.TryAddWithoutValidation(header.Item1, header.Item2);
                    }
                }

                var resp = await client.SendAsync(requestMessage);
                return resp;
            }
        }

        public static async Task<string> GetStrPost(string url, List<KeyValuePair<string, string>> payload, List<Tuple<string, string>> headers = null)
        {
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, url))
            {
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        if (requestMessage.Headers.Contains(header.Item1))
                        {
                            requestMessage.Headers.Remove(header.Item1);
                        }
                        requestMessage.Headers.TryAddWithoutValidation(header.Item1, header.Item2);
                    }
                }

                var formEncodedContent = new FormUrlEncodedContent(payload);
                var resp = await client.PostAsync(url, formEncodedContent);

                if (!resp.IsSuccessStatusCode)
                {
                    return resp.ReasonPhrase;
                }

                return await resp.Content.ReadAsStringAsync();
            }
        }

        public static async Task<string> GetStrPost(string url, List<Tuple<string, string>> headers = null)
        {
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, url))
            {
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        if (requestMessage.Headers.Contains(header.Item1))
                        {
                            requestMessage.Headers.Remove(header.Item1);
                        }
                        requestMessage.Headers.TryAddWithoutValidation(header.Item1, header.Item2);
                    }
                }

                var resp = await client.SendAsync(requestMessage);
                if (!resp.IsSuccessStatusCode)
                {
                    return resp.ReasonPhrase;
                }

                return await resp.Content.ReadAsStringAsync();
            }
        }

        public static async Task<string> GetStrPost(string url,
            JDict payload, List<Tuple<string, string>> headers = null)
        {
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, url))
            {
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        if (requestMessage.Headers.Contains(header.Item1))
                        {
                            requestMessage.Headers.Remove(header.Item1);
                        }
                        requestMessage.Headers.TryAddWithoutValidation(header.Item1, header.Item2);
                    }
                }

                string payloadStr = JsonParser.Serialize(payload).ToString();
                requestMessage.Content = new StringContent(payloadStr,
                    Encoding.UTF8);

                var resp = await client.SendAsync(requestMessage);
                if (!resp.IsSuccessStatusCode)
                {
                    return resp.ReasonPhrase;
                }

                return await resp.Content.ReadAsStringAsync();
            }
        }

        public static async Task<string> GetStr(string url, CancellationToken cancelToken = default(CancellationToken))
        {
            var req = await client.GetAsync(url, cancelToken);
            if (!req.IsSuccessStatusCode)
            {
                return req.ReasonPhrase;
            }

            return await req.Content.ReadAsStringAsync();
        }

        public static async Task<HttpResponseMessage> GetReq(string url, List<Tuple<string, string>> headers = null,
            CancellationToken cancelToken = default(CancellationToken))
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    if (requestMessage.Headers.Contains(header.Item1))
                    {
                        requestMessage.Headers.Remove(header.Item1);
                    }
                    requestMessage.Headers.TryAddWithoutValidation(header.Item1, header.Item2);
                }
            }


            var resp = await client.SendAsync(requestMessage, cancelToken);
            return resp;
        }

        public static async Task<string> GetStr(string url, List<Tuple<string, string>> headers)
        {
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, url))
            {
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        if (requestMessage.Headers.Contains(header.Item1))
                        {
                            requestMessage.Headers.Remove(header.Item1);
                        }
                        requestMessage.Headers.TryAddWithoutValidation(header.Item1, header.Item2);
                    }
                }


                var resp = await client.SendAsync(requestMessage);
                if (!resp.IsSuccessStatusCode)
                {
                    return resp.ReasonPhrase;
                }

                var byteArr = await resp.Content.ReadAsByteArrayAsync();
                return Encoding.UTF8.GetString(byteArr, 0, byteArr.Length);
            }
        }

        public static async Task DownloadParticalContent(string url, string path, List<Tuple<string, string>> headers = null, string fileName = null)
        {
            if (!string.IsNullOrEmpty(fileName))
            {
                path = $"{path}/{fileName}";
            }

            using (var req = new HttpRequestMessage(HttpMethod.Head, url))
            {
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        if (req.Headers.Contains(header.Item1))
                        {
                            req.Headers.Remove(header.Item1);
                        }
                        req.Headers.TryAddWithoutValidation(header.Item1, header.Item2);
                    }
                }
                var response = await client.SendAsync(req);

                var parallelDownloadSuported = response.Headers.AcceptRanges.Contains("bytes");
                var contentLength = response.Content.Headers.ContentLength ?? 0;

                Debug.WriteLine(contentLength);
                if (parallelDownloadSuported)
                {
                    const double numberOfParts = 100;
                    var tasks = new List<Task>();
                    var partSize = (long)Math.Ceiling(contentLength / numberOfParts);

                    File.Create(path).Dispose();

                    for (var i = 0; i < numberOfParts; i++)
                    {
                        var start = i * partSize + Math.Min(1, i);
                        var end = Math.Min((i + 1) * partSize, contentLength);

                        tasks.Add(Task.Run(async () => await DownloadPart(url, path, start, end, headers)));
                    }

                    await Task.WhenAll(tasks);
                }
            }
        }

        private static async Task DownloadPart(string url, string saveAs, long start, long end, List<Tuple<string, string>> headers = null)
        {
            using (var fileStream = new FileStream(saveAs, FileMode.Open, FileAccess.Write, FileShare.Write))
            {
                var message = new HttpRequestMessage(HttpMethod.Get, url);
                message.Headers.Add("Range", string.Format("bytes={0}-{1}", start, end));
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        if (message.Headers.Contains(header.Item1))
                        {
                            message.Headers.Remove(header.Item1);
                        }
                        message.Headers.TryAddWithoutValidation(header.Item1, header.Item2);
                    }
                }


                fileStream.Position = start;
                await client.SendAsync(message).Result.Content.CopyToAsync(fileStream);

                message.Dispose();
            }
        }

        public static async Task<string> GetRedirectedUrl(string url, List<Tuple<string, string>> headers = null)
        {
            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            if (headers != null)
            {
                if (headers != null)
                {
                    foreach (var header in headers) requestMessage.Headers.Add(header.Item1, header.Item2);
                }
            }

            using HttpResponseMessage response = await client.SendAsync(requestMessage);
            using HttpContent content = response.Content;

            string redirectedUri = string.Empty;
            if (response.StatusCode == HttpStatusCode.Found)
            {
                HttpResponseHeaders redirectedHeaders = response.Headers;
                if (redirectedHeaders != null && redirectedHeaders.Location != null)
                {
                    redirectedUri = redirectedHeaders.Location.AbsoluteUri;
                }
            }

            return redirectedUri;
        }

        public static async Task DownloadFileFromUrl(string url, string path,
            List<Tuple<string, string>> headers = null, string fileName = null, int retries = 0, 
            bool duplicateFileName = false, CancellationToken cancelToken = default(CancellationToken), UrlEntry entry = null, 
            bool overrideFile = true, bool redirectUrl = false)
        {
            if (string.IsNullOrEmpty(url))
                return;

            long downloadedRange = 0; //Make it so the download can resume where it failed.
            for (int i = 0; i < MAX_RETRIES; i++)
            {
                if (cancelToken.IsCancellationRequested) return;
                using var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
                
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        requestMessage.Headers.Add(header.Item1, header.Item2);
                    }
                }

                if (downloadedRange > 0) requestMessage.Headers.Add("Range", $"bytes={downloadedRange}");

                string ext = GetFileExtensionFromUrl(url);
                fileName = (!string.IsNullOrEmpty(fileName)) ? fileName : GetFileNameFromUrl(url);

                //Don't add extension if the filename already has it
                string currPath = (!fileName.EndsWith(ext)) ? $"{path}/{fileName}{ext}" : $"{path}/{fileName}";

                //If file exists skip the file if not wanted
                if (!overrideFile && File.Exists(currPath))
                {
                    if (entry != null)
                    {
                        entry.DownloadPath = currPath;
                        entry.StatusMsg = UrlEntry.FINISHED;
                        entry.FilesMsg = "Already downloaded";
                        entry.Bar.Value = 100;
                    }

                    return;
                }

                try
                {
                    var resp = await client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancelToken);

                    if (redirectUrl)
                    {
                        string redirectedUri = string.Empty;
                        if (resp.StatusCode == HttpStatusCode.Found)
                        {
                            HttpResponseHeaders redirectedHeaders = resp.Headers;
                            redirectedUri = redirectedHeaders.Where(header => header.Key == "Location").FirstOrDefault().Value.First();

                            if (!redirectedUri.Contains("http"))
                            {
                                string host = new Uri(url).Host;
                                redirectedUri = $"https://{host}{redirectedUri}";
                            }
                        }

                        using var newRequestMessage = new HttpRequestMessage(HttpMethod.Get, redirectedUri);
                        if (headers != null)
                        {
                            foreach (var header in headers) newRequestMessage.Headers.Add(header.Item1, header.Item2);
                        }

                        resp = await client.SendAsync(newRequestMessage, HttpCompletionOption.ResponseHeadersRead, cancelToken);
                    }

                    if (!resp.IsSuccessStatusCode)
                    {
                        Debug.WriteLine("ERROR in Requests: " + resp.StatusCode);
                        continue;
                    }

                    if (entry != null) entry.Name = fileName;

                    using (var fs = new FileStream(currPath, FileMode.Create, FileAccess.Write,
                        FileShare.None, useAsync: true, bufferSize: 4096))
                    {
                        downloadedRange = await HttpClientExtensions.CopyToAsyncProgress(resp, fs, entry: entry,
                            cancellationToken: cancelToken);
                    }

                    if (currPath.EndsWith("png"))
                    {
                        currPath = await ImageUtil.PngToJpg(currPath);
                    }
                    else if (currPath.EndsWith("webp"))
                    {
                        currPath = await ImageUtil.WebpToJpg(currPath);
                    }

                    if (entry != null)
                    {
                        entry.DownloadPath = currPath;
                    }

                    break;
                }
                catch (Exception e)
                {
                    Debug.WriteLine("ERROR in Requests: " + e);
                    await Task.Delay(TIMEOUT_RETRY);
                }
            }
        }   
    }
}
