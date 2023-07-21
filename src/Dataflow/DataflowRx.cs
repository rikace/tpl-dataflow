using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Dataflow
{
    public class DataflowRx
    {
        static void Example_1()
        {
            IPropagatorBlock<int, string> source = new TransformBlock<int, string>(i => (i + i).ToString());
            IObservable<int> observable = source.AsObservable().Select(int.Parse);
            IDisposable subscription = observable.Subscribe(i =>
                Console.WriteLine($"Value {i} - Time {DateTime.Now.ToString("hh:mm:ss.fff")}"));

            for (int i = 0; i < 100; i++)
                source.Post(i);

            IPropagatorBlock<string, int> target = new TransformBlock<string, int>(s => int.Parse(s));
            IDisposable link = target.LinkTo(new ActionBlock<int>(i =>
                Console.WriteLine($"Value {i} - Time {DateTime.Now.ToString("hh:mm:ss.fff")}")));

            IObserver<string> observer = target.AsObserver();
            IObservable<string> observable_2 = Observable.Range(1, 20).Select(i => (i * i).ToString());
            observable_2.Subscribe(observer);

            for (int i = 0; i < 100; i++)
                target.Post(i.ToString());

            link.Dispose();
        }

        static void Example_2()
        {
            IObservable<int> originalInts = Observable.Range(1, 20);

            IPropagatorBlock<int, int[]> batch = new BatchBlock<int>(2);
            IObservable<int[]> batched = batch.AsObservable();
            originalInts.Subscribe(batch.AsObserver());

            IObservable<int> added = batched.Timeout(TimeSpan.FromMilliseconds(250)).Select(a => a.Sum());

            IPropagatorBlock<int, string> toString = new TransformBlock<int, string>(i => i.ToString());
            added.Subscribe(toString.AsObserver());

            JoinBlock<string, int> join = new JoinBlock<string, int>();
            toString.LinkTo(join.Target1);

            IObserver<int> joinIn2 = join.Target2.AsObserver();
            originalInts.Subscribe(joinIn2);

            IObservable<Tuple<string, int>> joined = join.AsObservable();

            joined.Subscribe(t => Console.WriteLine("{0};{1}", t.Item1, t.Item2));
        }

        // RX
        static void Example_3()
        {
            var blockOptions = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = 2,
                BoundedCapacity = 5
            };

            ActionBlock<int> warmupBlock = new ActionBlock<int>(async i =>
            {
                await Task.Delay(1000);
                Console.WriteLine(i);
            }, blockOptions);

            ActionBlock<int> postBlock = new ActionBlock<int>(async i =>
            {
                await Task.Delay(1000);
                Console.WriteLine(i);
            }, blockOptions);

            IObservable<int> warmUpSource = Observable.Range(1, 100).TakeUntil(DateTimeOffset.UtcNow.AddSeconds(5));
            warmUpSource.Subscribe(warmupBlock.AsObserver());

            IObservable<int> testSource = Observable.Range(1000, 1000).TakeUntil(DateTimeOffset.UtcNow.AddSeconds(10));
            testSource.Subscribe(postBlock.AsObserver());
        }
    }
}
