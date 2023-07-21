namespace Dataflow.Pipeline
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Dataflow;

    public class TPLDataflowPipelineBuilder<TIn, TOut>
    {
        private List<IDataflowBlock> _steps = new List<IDataflowBlock>();

        public TPLDataflowPipelineBuilder<TIn, TOut> AddStep<TLocalIn, TLocalOut>(Func<TLocalIn, TLocalOut> stepFunc)
        {
            var step = new TransformBlock<TaskResult<TLocalIn, TOut>, TaskResult<TLocalOut, TOut>>((tc) =>
            {
                try
                {
                    return new TaskResult<TLocalOut, TOut>(stepFunc(tc.Input), tc.TaskCompletionSource);
                }
                catch (Exception e)
                {
                    tc.TaskCompletionSource.SetException(e);
                    return new TaskResult<TLocalOut, TOut>(default(TLocalOut), tc.TaskCompletionSource);
                }
            });

            if (_steps.Count > 0)
            {
                var lastStep = _steps.Last();
                var targetBlock = (lastStep as ISourceBlock<TaskResult<TLocalIn, TOut>>);
                targetBlock.LinkTo(step, new DataflowLinkOptions(),
                    tc => !tc.TaskCompletionSource.Task.IsFaulted);
                targetBlock.LinkTo(DataflowBlock.NullTarget<TaskResult<TLocalIn, TOut>>(), new DataflowLinkOptions(),
                    tc => tc.TaskCompletionSource.Task.IsFaulted);
            }

            _steps.Add(step);
            return this;
        }

        public TPLDataflowPipelineBuilder<TIn, TOut> CreatePipeline()
        {
            var setResultStep =
                new ActionBlock<TaskResult<TOut, TOut>>((tc) => tc.TaskCompletionSource.SetResult(tc.Input));
            var lastStep = _steps.Last();
            var setResultBlock = (lastStep as ISourceBlock<TaskResult<TOut, TOut>>);
            setResultBlock.LinkTo(setResultStep);
            return this;
        }

        public Task<TOut> Execute(TIn input)
        {
            var firstStep = _steps[0] as ITargetBlock<TaskResult<TIn, TOut>>;
            var tcs = new TaskCompletionSource<TOut>();
            firstStep.SendAsync(new TaskResult<TIn, TOut>(input, tcs));
            return tcs.Task;
        }


        public static void Builder()
        {
            var pipeline = new TPLDataflowPipelineBuilder<string, bool>()
                .AddStep<string, string>(sentence => ParseInputSequence(sentence))
                .AddStep<string, int>(word => word.Length)
                .AddStep<int, bool>(length => length % 2 == 1)
                .CreatePipeline();

            Task.Run(async () =>
            {
                bool res;
                res = await pipeline.Execute("the brown fox jumped over the lazy dog");
                Console.WriteLine(res);

                res = await pipeline.Execute("the brown fox jumped over the lazy dog");
                Console.WriteLine(res);

                res = await pipeline.Execute("the brown fox jumped over the lazy dog");
                Console.WriteLine(res);

                res = await pipeline.Execute("the brown fox jumped over the lazy dog");
                Console.WriteLine(res);

                res = await pipeline.Execute("the brown fox jumped over the lazy dog");
                Console.WriteLine(res);
                res = await pipeline.Execute("the brown fox jumped over the lazy dog");
                Console.WriteLine(res);
                res = await pipeline.Execute("the brown fox jumped over the lazy dog");
                Console.WriteLine(res);
            }).Wait();
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

    public class TaskResult<TInput, TOutput>
    {
        public TaskResult(TInput input, TaskCompletionSource<TOutput> tcs)
        {
            Input = input;
            TaskCompletionSource = tcs;
        }

        public TInput Input { get; set; }
        public TaskCompletionSource<TOutput> TaskCompletionSource { get; set; }
    }
}
