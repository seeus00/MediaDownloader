using ChromeCookie;
using Downloader.Util;
using Microsoft.Identity.Client;
using PuppeteerSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using WpfDownloader.Util;
using WpfDownloader.Util.UserAgent;
using WpfDownloader.WpfData;

namespace WpfDownloader.Sites
{
    public class Test : Site
    {

        private static CookieContainer _cookieContainer = null;

        public Test(string url, string args) : base(url, args)
        {

        }

        public override async Task DownloadAll(UrlEntry entry)
        {
            entry.FilesMsg = "12313/23123127381293";

            ChromeCookies.GetCookies("8chan.moe");

            //var p = new Process();
            //p.StartInfo.FileName = "../../../tools/dejsonlz4.exe";
            //p.StartInfo.Arguments = "\"C:\\Users\\casey\\AppData\\Roaming\\Mozilla\\Firefox\\Profiles\\b79zftfg.default-release\\sessionstore-backups\\recovery.jsonlz4\" \"C:/Users/casey/Desktop/nigger\"";
            //p.StartInfo.RedirectStandardOutput = true;
            //p.StartInfo.RedirectStandardOutput = true;
            //p.StartInfo.RedirectStandardError = true;
            //p.StartInfo.CreateNoWindow = true;
            //p.Start();
            //string stdoutx = p.StandardOutput.ReadToEnd();
            //string stderrx = p.StandardError.ReadToEnd();
            //p.WaitForExit();

            //Debug.WriteLine(stderrx);
            //var browserFetcherOptions = new BrowserFetcherOptions { Path = "C:/Users/casey/Downloads" };
            //using var browserFetcher = new BrowserFetcher(browserFetcherOptions);
            //await browserFetcher.DownloadAsync();
            //using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            //{
            //    ExecutablePath = "C:/Program Files/Google/Chrome/Application/chrome.exe",
            //    Headless = false
            //    // false if you need to see the browser
            //});

            //using var page = await browser.NewPageAsync();


            //await page.GoToAsync("https://anchira.to");

            ////await page.WaitForTimeoutAsync(5000);
            ////await page.WaitForFunctionAsync("js");

            //await Task.Delay(500000);
            ////await page.WaitForSelectorAsync(".d[title=\"Download\"]");
            //var content = await page.GetContentAsync();

            ////Requests.AddCookies(cookieContainer, new Uri("https://anchira.to"));
            ////var resp = await Requests.Get(url);
            ////resp.EnsureSuccessStatusCode();

            ////string html = await resp.Content.ReadAsStringAsync();


        }
    }
}
