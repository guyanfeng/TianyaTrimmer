using System;
using System.IO;
using System.Text;

namespace TianyaTrimmer
{
    class Program
    {
        static void Main(string[] args)
        {
            /*var t = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "《自废武功——通过做“减法”赚第一个1000万》.txt"), Encoding.UTF8);
            t = t.Replace("<br>", Environment.NewLine);
            var a = new LineFormatter().Format(t);
            Console.WriteLine(a);
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "1.txt"), a, Encoding.UTF8);
            return;*/
            var url = new Uri("http://bbs.tianya.cn/post-no100-22833-1.shtml");
            Console.WriteLine($"raw url:{url}");
            //http://bbs.tianya.cn/post-no100-22833-1.shtml#ty_vip_look[7857023]
            var trimmer = new MainTrimmer(url);
            Console.WriteLine($"parsing author id...");
            var content = trimmer.DownloadString(url);
            trimmer.InitAndAnalyse(content, new Uri(url.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped)));
            Console.WriteLine($"ItemId:{trimmer.ItemId},ArticleId:{trimmer.ArticleId},PageId:{trimmer.CurrentPage}, AuthorId:{trimmer.AuthorId}, AuthorName:{trimmer.AuthorName}");
            url = new Uri($"{url.AbsoluteUri}#ty_vip_look[{trimmer.AuthorId}]");

            var article = new StringBuilder();
            var formatter = new LineFormatter();
            while (true)
            {
                Console.WriteLine($"downloading {url}");
                trimmer = new MainTrimmer(url);
                content = trimmer.DownloadString(url);
                trimmer.InitAndAnalyse(content, new Uri(url.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped)));
                Console.WriteLine("extract author replies");
                var trimmed = trimmer.ExtractContent(content, trimmer.AuthorId);
                Console.WriteLine($"extracted {trimmed.Length} phases");
                if (trimmed.Length > 0)
                {
                    article.AppendLine(string.Join(Environment.NewLine, trimmed));
                }
                var next = trimmer.GetNextPageUrl();
                if (string.IsNullOrEmpty(next))
                    break;
                url = new Uri(next);
            }
            var file = Path.Combine(AppContext.BaseDirectory, trimmer.Title + ".txt");
            File.WriteAllText(file, article.ToString(), Encoding.UTF8);
            Console.WriteLine("Completed.");
        }
    }
}