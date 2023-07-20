using System.Threading;
using Microsoft.VisualBasic.CompilerServices;

namespace Dataflow.Pipeline
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Dataflow;


    public class TPLDataflowPipeline
    {
        //private ActionBlock<string> step1;
        public static TransformBlock<string, string> CreatePipeline(Action<string> resultCallback)
        {
            var step1 = new TransformBlock<string, string>((sentence) => ParseInputSequence(sentence));
            var step2 = new TransformBlock<string, int>((word) => word.Length);
            var step3 = new TransformBlock<int, string>((length) => { return (length % 2 == 1).ToString(); });
            var callBackStep = new ActionBlock<string>(resultCallback);
            var step4 = new ActionBlock<string>(result => Console.WriteLine($"Result is #{result}"));
            step1.LinkTo(step2, new DataflowLinkOptions());
            step2.LinkTo(step3, new DataflowLinkOptions());
            step3.LinkTo(callBackStep);
            return step1;
        }

        public static TransformBlock<string, string> CreatePipelineWithOptions(Action<string> resultCallback, int maxDegreeOfParallelism, int boundedCapacity)
        {
           var options = new ExecutionDataflowBlockOptions()
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism,
                BoundedCapacity = boundedCapacity,
            };

            var step1 = new TransformBlock<string, string>((sentence) => ParseInputSequence(sentence), options);
            var step2 = new TransformBlock<string, int>((word) => word.Length);
            var step3 = new TransformBlock<int, string>((length) => { return (length % 2 == 1).ToString(); }, options);
            var callBackStep = new ActionBlock<string>(resultCallback);
            var step4 = new ActionBlock<string>(result => Console.WriteLine($"Result is #{result}"), options);
            step1.LinkTo(step2, new DataflowLinkOptions());
            step2.LinkTo(step3, new DataflowLinkOptions());
            step3.LinkTo(callBackStep);
            return step1;
        }

        private static void Usage()
        {
            var pipeline = CreatePipeline(resultCallback: res => { Console.WriteLine(res); });

            Task.Run(async () =>
            {
                for (int i = 0; i < 50; i++)
                {
                    await pipeline.SendAsync("The pipeline pattern is the best pattern");
                }
            });
        }

        private static void UsageAsync()
        {
            var pipeline = CreatePipeline(resultCallback: res => { Console.WriteLine(res); });

            Task.Run(async () =>
            {
                for (int i = 0; i < 50; i++)
                {
                    await pipeline.SendAsync("The pipeline pattern is the best pattern");
                }
            });
        }


        private static string ParseInputSequence(string input)
        {
            return input.Split(' ')
                .GroupBy(word => word)
                .OrderBy(group => group.Count())
                .Last()
                .Key;
        }
    }
}
