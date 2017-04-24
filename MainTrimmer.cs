using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.IO;
using System.Net;
using System.Linq;
using System.Text.RegularExpressions;

namespace TianyaTrimmer
{
    public class MainTrimmer : IDisposable
    {
        public CookieContainer Cookies { get; set; }
        HttpClient client;
        AngleSharp.Dom.IDocument doc;
        LineFormatter formatter;
        public string AuthorName { get; set; }
        public string AuthorId { get; set; }
        public int TotalPage { get; set; }
        public int CurrentPage { get; set; }
        public string ItemId { get; set; }
        public string ArticleId { get; set; }
        public string Title { get; set; }
        public Uri NextPageUrl { get; set; }
        public Uri Url { get; set; }

        public MainTrimmer(Uri url)
        {
            Cookies = new CookieContainer();
            var handler = new HttpClientHandler()
            {
                CookieContainer = Cookies
            };
            formatter = new LineFormatter();
            Url = url;
            var reg = new Regex(@"post\-(\w+)\-(\w+)\-(\w+)\.shtml");
            var match = reg.Match(url.AbsoluteUri);
            if (match.Success)
            {
                ItemId = match.Groups[1].Value;
                ArticleId = match.Groups[2].Value;
                CurrentPage = Convert.ToInt32(match.Groups[3].Value);
            }
            client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, sdch");
            client.DefaultRequestHeaders.Add("Accept-Language", "zh,zh-CN;q=0.8,en-US;q=0.6,en;q=0.4");
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/57.0.2987.133 Safari/537.36");
        }

        public string DownloadString(Uri url)
        {
            var msg = client.GetAsync(url).Result;
            msg.EnsureSuccessStatusCode();
            return msg.Content.ReadAsStringAsync().Result;
            // var encoding = Encoding.UTF8;
            // //switch (msg.Content.Headers.ContentType.CharSet.ToLower())
            // //{
            // //    case "utf-8":
            // //        encoding = Encoding.UTF8;
            // //        break;
            // //    default:
            // //        break;
            // //}
            // using (var stream = new StreamReader(msg.Content.ReadAsStreamAsync().Result, encoding))
            // {
            //     return stream.ReadToEnd();
            // }
        }

        public void InitAndAnalyse(string content, Uri baseUrl)
        {
            AuthorName = null;
            NextPageUrl = null;
            TotalPage = 0;
            doc = new AngleSharp.Parser.Html.HtmlParser().Parse(content);
            var nodeAuthor = doc.QuerySelector("div#post_head div.atl-info span a");
            if (nodeAuthor != null)
            {
                AuthorId = nodeAuthor.GetAttribute("uid");
                AuthorName = nodeAuthor.GetAttribute("uname");
            }
            Title = doc.QuerySelector("div#post_head span.s_title").TextContent;
            /*var pages = doc.QuerySelectorAll("div#post_head div.atl-pages form a");
            var lastPage = (from p in pages
                            where p.TextContent == "下页"
                            select p).FirstOrDefault();
            if (lastPage != null)
            {
                var anchor = lastPage as AngleSharp.Dom.Html.IHtmlAnchorElement;
                NextPageUrl = new Uri(baseUrl, anchor.PathName);
                TotalPage = Convert.ToInt32(lastPage.PreviousElementSibling.TextContent);
            }*/
        }

        public string[] ExtractContent(string content, string authorId)
        {
            var nodes = doc.QuerySelectorAll($"div.atl-item['_hostid'='{authorId}'] div.bbs-content");
            if (nodes.Length == 0)
                return null;
            var phaseList = new List<string>();
            foreach (var n in nodes)
            {
                var phase = n.InnerHtml;
                phase = phase.Replace("<br>", Environment.NewLine);
                phase = formatter.Format(phase);
                if (phase.Length > 300)
                {
                    phaseList.Add(phase);
                }
            }
            return phaseList.ToArray();
        }

        public string GetNextPageUrl()
        {
            var url = new Uri($"http://bbs.tianya.cn/api?method=bbs.api.hasAuthorReplyPages&params.item={ItemId}&params.articleId={ArticleId}&params.pageNum={CurrentPage}");
            var content = DownloadString(url);
            var json = Newtonsoft.Json.Linq.JObject.Parse(content);
            if (json.Value<int>("success") == 1)
            {
                var data = json["data"];
                if (!data.Value<bool>("hasNext"))
                {
                    return null;
                }
                //var first = data["rows"][0].Value<int>(null);
                var rows = (from p in data["rows"]
                            select Convert.ToInt32(p.ToString())).ToArray();
                var next = (from p in rows
                            where p > CurrentPage
                            select p).FirstOrDefault();
                if (next == 0)
                {
                    return null;
                }
                var reg = new Regex(@"\w+\.shtml");
                return reg.Replace(Url.AbsoluteUri, next + ".shtml");
            }
            return content;
        }

        public void Dispose()
        {
            if (client != null)
            {
                client.Dispose();
            }
        }
    }
}
