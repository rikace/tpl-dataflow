namespace Dataflow.WebCrawler
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using HtmlAgilityPack;
    using System.Net;
    using System.Threading.Tasks;
    using System.Collections.Concurrent;
    using System.Threading;


      public class ProducerConsumerWebCrawler
    {
        static IEnumerable<string> ExtractLinks(HtmlDocument doc)
        {
            try
            {
                return
                    (from a in doc.DocumentNode.SelectNodes("//a")
                        where a.Attributes.Contains("href")
                        let href = a.Attributes["href"].Value
                        where href.StartsWith("http://")
                        let endl = Math.Min(href.IndexOf('?'), href.IndexOf('#'))
                        select endl > 0 ? href.Substring(0, endl) : href).ToArray();
            }
            catch
            {
                return Enumerable.Empty<string>();
            }
        }

        static string GetTitle(HtmlDocument doc)
        {
            try
            {
                var title = doc.DocumentNode.SelectSingleNode("//title");
                return title != null ? title.InnerText.Trim() : "Untitled";
            }
            catch
            {
                return "Untitled";
            }
        }

        static async Task<HtmlDocument> DownloadDocument(string url)
        {
            try
            {
                var wc = new WebClient();
                var html = await wc.DownloadStringTaskAsync(new Uri(url));
                var doc = new HtmlDocument();
                doc.LoadHtml(html);
                return doc;
            }
            catch
            {
                return new HtmlDocument();
            }
        }

        static BlockingCollection<string> pending = new BlockingCollection<string>();
        static ConcurrentDictionary<string, bool> visited = new ConcurrentDictionary<string, bool>();


        static async Task Crawler()
        {
            while (pending.TryTake(out var url))
            {
                var content = await DownloadDocument(url);
                var tilte = GetTitle(content);
                Console.WriteLine($"The title of {url} is {tilte}");

                foreach (var link in ExtractLinks(content))
                {
                    pending.Add(link);
                }
            }
        }

        static void WebCrawlerProducerConsumer(List<string> urls)
        {
            foreach (var url in urls)
                pending.Add(url);

            pending.Add("https://www.cnn.com");
            pending.Add("https://www.foxnews.com");
            pending.Add("https://www.amazon.com");
            pending.Add("https://www.cnn.com");

            for (int i = 0; i < 10; i++)
                Task.Run(Crawler);
        }
    }
}
