using System;
using ReactiveAgent.Agents;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using IO = System.IO;
using File = ReactiveAgent.Agents.File;

namespace ParallelPatterns
{
    public static class AgentAggregate
    {
        private static string CreateFileNameFromUrl(string _) =>
            Path.Combine(Path.GetTempPath(), Path.GetTempFileName());

        public static void Run()
        {
            // Producer/consumer using TPL Dataflow
            var urls = new List<string>
            {
                @"http://www.google.com",
                @"http://www.microsoft.com",
                @"http://www.bing.com",
                @"http://www.google.com"
            };

           // Agent fold over state and messages - Aggregate
            urls.Aggregate(ImmutableDictionary<string, string>.Empty,
                (state, url) =>
                {
                    if (state.TryGetValue(url, out var content))
                        return state;

                    using (var webClient = new HttpClient())
                    {
                        System.Console.WriteLine($"Downloading '{url}' sync ...");
                        content = webClient.GetStringAsync(url).GetAwaiter().GetResult();
                        IO.File.WriteAllText(CreateFileNameFromUrl(url), content);
                        return state.Add(url, content);
                    }
                });



            var agentStateful = Agent.StartWithRx(
                ImmutableDictionary<string, string>.Empty,
                async (ImmutableDictionary<string, string> state, string url) =>
                {
                    if (state.TryGetValue(url, out string content))
                        return state;

                    using (var webClient = new HttpClient())
                    {
                        System.Console.WriteLine($"Downloading '{url}' async ...");
                        content = await webClient.GetStringAsync(url);
                        await File.WriteAllTextAsync(CreateFileNameFromUrl(url), content);
                        return state.Add(url, content);
                    }
                });


            // run this code
            urls.ForEach(x =>
                agentStateful.Post(x));
        }
    }
}
