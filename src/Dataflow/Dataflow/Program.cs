using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Dataflow.FuzzyMatch;
using Dataflow.WebCrawler;

namespace Dataflow
{
    class Program
    {
        static async Task Main(string[] args)
        {
           // await RunWebCrawler();

            await RunWordsCounter();

            // await RunFuzzyMatch();
            // DemoBlocks();

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        static void DemoBlocks()
        {
            DemoBuildingBlocks.BufferBlock_Simple();
            // DemoBuildingBlocks.BufferBlock_BoundCapacity();
            // DemoBuildingBlocks.ActionBlock_Simple();
            // DemoBuildingBlocks.ActionBlock_Parallel();
            // DemoBuildingBlocks.ActionBlock_Linking();
            // DemoBuildingBlocks.ActionBlock_Propagate();
            // DemoBuildingBlocks.TransformBlock_Simple();
            // DemoBuildingBlocks.BatchBlock_Simple();
            // DemoBuildingBlocks.TransformManyBlock_Simple();
            // DemoBuildingBlocks.BroadcastBlock_Simple();
        }

        static async Task RunWebCrawler()
        {
            var urls = new List<string>();

            urls.Add("https://www.cnn.com");
            urls.Add("https://www.bbc.com");
            urls.Add("https://www.amazon.com");
            urls.Add("https://www.jet.com");
            urls.Add("https://www.cnn.com");

            ServicePointManager.DefaultConnectionLimit = 100;

            DataFlowImageCrawler.Start(urls, 8, async (url, buffer) =>
            {
                string fileName = Path.GetFileNameWithoutExtension(Path.GetTempFileName()) + ".jpg";

                if (!Directory.Exists("Images"))
                    Directory.CreateDirectory("Images");

                string name = @"Images/" + fileName;

                using (Stream srm = File.OpenWrite(name))
                {
                    await srm.WriteAsync(buffer, 0, buffer.Length);
                }
            });
        }

        static async Task RunWordsCounter()
        {
            await WordsCounter.Start(4);
        }


        static async Task RunFuzzyMatch()
        {
            var dirDataText = new DirectoryInfo("./../../../../Data/Text");
            IList<string> files =
                dirDataText.EnumerateFiles("*.txt")
                    .OrderBy(f => f.Length)
                    .Select(f => f.FullName)
                    .Take(10).ToList();

            var wordsToSearch = new string[]
                {"ENGLISH", "RICHARD", "STEALLING", "MAGIC", "STARS", "MOON", "CASTLE"};
            await ParallelFuzzyMatch.RunFuzzyMatchDataFlow(wordsToSearch, files);
        }
    }
}
