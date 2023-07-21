namespace Dataflow.WebCrawler
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using HtmlAgilityPack;
    using System.Linq;
    using System.Net;
    using WebCrawler;
    using Memoization = FunctionalHelpers.Memoization;

    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Reactive.Disposables;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Dataflow;
    using System.Text.RegularExpressions;

     public static class DataFlowImageCrawler
    {
        private static ConsoleColor[] colors = new ConsoleColor[]
        {
            ConsoleColor.Black,
            ConsoleColor.DarkBlue,
            ConsoleColor.DarkGreen,
            ConsoleColor.DarkCyan,
            ConsoleColor.DarkRed,
            ConsoleColor.DarkMagenta,
            ConsoleColor.DarkYellow,
            ConsoleColor.Gray,
            ConsoleColor.DarkGray,
            ConsoleColor.Blue,
            ConsoleColor.Green,
            ConsoleColor.Cyan,
            ConsoleColor.Red,
            ConsoleColor.Magenta,
            ConsoleColor.Yellow,
            ConsoleColor.White
        };

        static int index = 0;
        static ConcurrentDictionary<int, ConsoleColor> mapColors = new ConcurrentDictionary<int, ConsoleColor>();

        private static ConsoleColor ColorByInt(int id)
            => mapColors.GetOrAdd(id, _ => colors[Interlocked.Increment(ref index) % (colors.Length - 1)]);


        private static void WriteLineInColor(string message, ConsoleColor foregroundColor)
        {
            Console.ForegroundColor = foregroundColor;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        private const string LINK_REGEX_HREF = "\\shref=('|\\\")?(?<LINK>http\\://.*?(?=\\1)).*>";
        private static readonly Regex _linkRegexHRef = new Regex(LINK_REGEX_HREF);

        private const string IMG_REGEX =
            "<\\s*img [^\\>]*src=('|\")?(?<IMG>http\\://.*?(?=\\1)).*>\\s*([^<]+|.*?)?\\s*</a>";

        private static readonly Regex _imgRegex = new Regex(IMG_REGEX);

        private static ThreadLocal<Regex> httpRgx = new ThreadLocal<Regex>(() => new Regex(@"^(http|https|www)://.*$"));

        public static IDisposable Start(List<string> urls, int dop,  Func<string, byte[], Task> compute)
        {
            var downloaderOptions = new ExecutionDataflowBlockOptions()
            {
                MaxDegreeOfParallelism = dop
            };

            var downloadUrl = Memoization.MemoizeLazyThreadSafe<string, string>(async (url) =>
            {
                using (WebClient wc = new WebClient())
                {
                    string result = await wc.DownloadStringTaskAsync(url);
                    return result;
                }
            });


            var downloader = new TransformBlock<string, string>(downloadUrl, downloaderOptions);

            var contentBroadcaster = new BroadcastBlock<string>(s => s);

            var linkParser = new TransformManyBlock<string, string>(
                (html) =>
                {
                    var output = new List<string>();
                    var doc = new HtmlDocument();
                    doc.LoadHtml(html);

                    var links =
                        from link in doc.DocumentNode.Descendants("a")
                        where link.Attributes.Contains("href")
                        select link.GetAttributeValue("href", "");

                    var linksValidated =
                        from link in links
                        where httpRgx.Value.IsMatch(link)
                        select link;

                    foreach (var link in linksValidated)
                    {
                        Console.WriteLine($"Link {link} ready to be crawled");
                        output.Add(link);
                    }

                    return output;
                });

            var imgParser = new TransformManyBlock<string, string>(
                (html) =>
                {
                    var output = new List<string>();
                    var doc = new HtmlDocument();
                    doc.LoadHtml(html);

                    var images =
                        from img in doc.DocumentNode.Descendants("img")
                        where img.Attributes.Contains("src")
                        select img.GetAttributeValue("src", "");

                    var imagesValidated =
                        from img in images
                        where httpRgx.Value.IsMatch(img)
                        select img;

                    foreach (var img in imagesValidated)
                    {
                        Console.WriteLine($"image {img} ready to be downloaded");
                        output.Add(img);
                    }
                    return output;
                });

            var linkBroadcaster = new BroadcastBlock<string>(s => s);

            var printer = new ActionBlock<string>(msg =>
            {
                Console.WriteLine($"Message {DateTime.UtcNow.ToString()} - Thread ID {Thread.CurrentThread.ManagedThreadId} : {msg}");
            });

            var writerData = new ActionBlock<string>(async url =>
            {
                url = url.StartsWith("http") ? url : "http:" + url;
                using (WebClient wc = new WebClient())
                {
                    // using IOCP the thread pool worker thread does return to the pool
                    byte[] buffer = await wc.DownloadDataTaskAsync(url);
                    Console.WriteLine($"Downloading {url}..");
                    await compute(url, buffer);
                }
            });


            StringComparison comparison = StringComparison.InvariantCultureIgnoreCase;
            Predicate<string> linkFilter = link =>
                link.IndexOf(".aspx", comparison) != -1 ||
                link.IndexOf(".php", comparison) != -1 ||
                link.IndexOf(".htm", comparison) != -1 ||
                link.IndexOf(".html", comparison) != -1;

            Predicate<string> imgFilter = url =>
                url.EndsWith(".jpg", comparison) ||
                url.EndsWith(".png", comparison) ||
                url.EndsWith(".gif", comparison);

            IDisposable disposeAll = new CompositeDisposable(
                downloader.LinkTo(contentBroadcaster),
                contentBroadcaster.LinkTo(imgParser),
                contentBroadcaster.LinkTo(linkParser),
                linkParser.LinkTo(linkBroadcaster),
                linkBroadcaster.LinkTo(downloader, linkFilter),
                linkBroadcaster.LinkTo(writerData, imgFilter),
                linkBroadcaster.LinkTo(printer),
                imgParser.LinkTo(writerData)
            );

            foreach (var url in urls)
            {
                downloader.Post(url);
            }

            return disposeAll;
        }
    }
}
