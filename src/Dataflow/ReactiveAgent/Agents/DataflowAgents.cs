using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace ReactiveAgent.Agents.Dataflow
{
    public class StatefulDataflowAgent<TState, TMessage> : IAgent<TMessage>
    {
        private TState state;
        private readonly ActionBlock<TMessage> actionBlock;

        public StatefulDataflowAgent(
            TState initialState,
            Func<TState, TMessage, Task<TState>> action,
            CancellationTokenSource cts = null)
        {
            state = initialState;

            var options = new ExecutionDataflowBlockOptions
            {
                CancellationToken = cts?.Token ?? CancellationToken.None
            };

            actionBlock = new ActionBlock<TMessage>(
                async msg => state = await action(state, msg), options);
        }

        public Task Send(TMessage message) => actionBlock.SendAsync(message);
        public void Post(TMessage message) => actionBlock.Post(message);

        public StatefulDataflowAgent(TState initialState, Func<TState, TMessage, TState> action,
            CancellationTokenSource cts = null)
        {
            state = initialState;
            var options = new ExecutionDataflowBlockOptions
            {
                CancellationToken = cts != null ? cts.Token : CancellationToken.None
            };
            actionBlock = new ActionBlock<TMessage>(
                msg => state = action(state, msg), options);
        }

        public TState State => state;
    }


    public class StatelessDataflowAgent<TMessage> : IAgent<TMessage>
    {
        private readonly ActionBlock<TMessage> actionBlock;

        public StatelessDataflowAgent(Action<TMessage> action, CancellationTokenSource cts = null)
        {
            var options = new ExecutionDataflowBlockOptions
            {
                CancellationToken = cts != null ? cts.Token : CancellationToken.None
            };
            actionBlock = new ActionBlock<TMessage>(action, options);
        }

        public StatelessDataflowAgent(Func<TMessage, Task> action, CancellationTokenSource cts = null)
        {
            var options = new ExecutionDataflowBlockOptions
            {
                CancellationToken = cts == null ? cts.Token : CancellationToken.None
            };
            actionBlock = new ActionBlock<TMessage>(action, options);
        }

        public void Post(TMessage message) => actionBlock.Post(message);
        public Task Send(TMessage message) => actionBlock.SendAsync(message);

    }

    public class StatefulDataflowAgentWithRx<TState, TMessage> : IAgentRx<TMessage, TState>
    {
        private TState state;
        private TransformBlock<TMessage, TState> block;

        public StatefulDataflowAgentWithRx(
            TState initialState,
            Func<TState, TMessage, Task<TState>> action,
            CancellationTokenSource cts = null)
        {
            state = initialState;
            var options = new ExecutionDataflowBlockOptions
            {
                CancellationToken = cts != null ? cts.Token : CancellationToken.None
            };
            block = new TransformBlock<TMessage, TState>(
                async msg => state = await action(state, msg), options);
        }

        public Task Send(TMessage message) => block.SendAsync(message);
        public void Post(TMessage message) => block.Post(message);

        public IObservable<TState> AsObservable() => block.AsObservable();
        public TState State => state;
    }

    public class Agent<TMessage, TState> : IAgentRx<TMessage, TState>
    {
        private TState _state;
        private readonly TransformBlock<TMessage, TState> _buildingBlock;

        public Agent(
            TState initialState,
            Func<TState, TMessage, TState> action,
            CancellationTokenSource cts = null)
        {
            _state = initialState;
            var options = new ExecutionDataflowBlockOptions
            {
                CancellationToken = cts?.Token ?? CancellationToken.None
            };
            _buildingBlock = new TransformBlock<TMessage, TState>(
                msg => _state = action(_state, msg)
                , options);
        }

        public Agent(
            TState initialState,
            Func<TState, TMessage, Task<TState>> action,
            CancellationTokenSource cts = null)
        {

            _state = initialState;
            var options = new ExecutionDataflowBlockOptions
            {
                CancellationToken = cts?.Token ?? CancellationToken.None
            };
            _buildingBlock = new TransformBlock<TMessage, TState>(
                async msg => _state = await action(_state, msg)
                , options);
        }

        public Task Send(TMessage message)
            => _buildingBlock.SendAsync(message);

        public void Post(TMessage message)
            => _buildingBlock.Post(message);

        public IObservable<TState> AsObservable()
            => _buildingBlock.AsObservable();

    }
}
