namespace Dataflow.Pipeline
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IPipelineStep<TStepIn>
    {
        BlockingCollection<TStepIn> Buffer { get; set; }
    }

    public class BlockingCollectionPipelineStep<TStepIn, TStepOut> : IPipelineStep<TStepIn>
    {
        public BlockingCollection<TStepIn> Buffer { get; set; } = new BlockingCollection<TStepIn>();
        public Func<TStepIn, TStepOut> StepAction { get; set; }
    }


    public class BlockingCollectionPipeline<TPipeIn, TPipeOut>
    {
        readonly List<object> _pipelineSteps = new List<object>();

        public event Action<TPipeOut> Finished;

        public BlockingCollectionPipeline(
            Func<TPipeIn, BlockingCollectionPipeline<TPipeIn, TPipeOut>, TPipeOut> steps)
        {
            steps.Invoke(default(TPipeIn), this);
        }

        public void Execute(TPipeIn input)
        {
            var first = _pipelineSteps[0] as IPipelineStep<TPipeIn>;
            if (first != null) first.Buffer.Add(input);
        }

        public BlockingCollectionPipelineStep<TStepIn, TStepOut> GenerateStep<TStepIn, TStepOut>()
        {
            var pipelineStep = new BlockingCollectionPipelineStep<TStepIn, TStepOut>();
            var stepIndex = _pipelineSteps.Count;

            Task.Run(() =>
            {
                IPipelineStep<TStepOut> nextPipelineStep = null;

                foreach (var input in pipelineStep.Buffer.GetConsumingEnumerable())
                {
                    bool isLastStep = stepIndex == _pipelineSteps.Count - 1;
                    var output = pipelineStep.StepAction(input);
                    if (isLastStep)
                    {
                        Finished?.Invoke((TPipeOut) (object) output);
                    }
                    else
                    {
                        nextPipelineStep = nextPipelineStep ??
                                           (isLastStep
                                               ? null
                                               : _pipelineSteps[stepIndex + 1] as IPipelineStep<TStepOut>);
                        nextPipelineStep.Buffer.Add(output);
                    }
                }
            });

            _pipelineSteps.Add(pipelineStep);
            return pipelineStep;
        }
    }
}
