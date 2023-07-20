namespace Dataflow
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Reactive.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Dataflow;
    using System.Xml.Linq;

    public static class DemoBuildingBlocks
    {
        public static void BufferBlock_Simple()
        {
            var bufferBlock = new BufferBlock<int>();

            for (int i = 0; i < 10; i++)
            {
                bufferBlock.Post(i);
            }

            for (int i = 0; i < 10; i++)
            {
                int result = bufferBlock.Receive();
                Console.WriteLine(result);
            }
        }

        public static void BufferBlock_BoundCapacity()
        {
            var bb = new BufferBlock<int>(new DataflowBlockOptions() {BoundedCapacity = 2});

            var a1 = new ActionBlock<int>(
                a =>
                {
                    Console.WriteLine("Action A1 executing with value {0}", a);
                    Thread.Sleep(100);
                }
                , new ExecutionDataflowBlockOptions() {BoundedCapacity = 1}
            );

            var a2 = new ActionBlock<int>(
                a =>
                {
                    Console.WriteLine("Action A2 executing with value {0}", a);
                    Thread.Sleep(50);
                }
                , new ExecutionDataflowBlockOptions() {BoundedCapacity = 1}
            );
            var a3 = new ActionBlock<int>(
                a =>
                {
                    Console.WriteLine("Action A3 executing with value {0}", a);
                    Thread.Sleep(50);
                }
                , new ExecutionDataflowBlockOptions() {BoundedCapacity = 1}
            );

            bb.LinkTo(a1);
            bb.LinkTo(a2);
            bb.LinkTo(a3);

            for (int i = 0; i < 10; i++)
            {
                Thread.Sleep(10);
                bb
                    .SendAsync(i)
                    .ContinueWith(a => Console.WriteLine($"Message {i} sent #{a.Result}"));
            }
        }

        public static void ActionBlock_Simple()
        {
            var actionBlock = new ActionBlock<int>(n =>
            {
                Thread.Sleep(1000);
                Console.WriteLine($"Message : {n} - Thread Id#{Thread.CurrentThread.ManagedThreadId}");
            });

            for (int i = 0; i < 10; i++)
            {
                actionBlock.Post(i);
                Console.WriteLine($"Message {i} processed - queue count {actionBlock.InputCount}");
            }

            actionBlock.Complete();
            actionBlock.Completion.Wait();
        }

        public static void ActionBlock_Parallel()
        {
            var actionBlock = new ActionBlock<int>(n =>
            {
                Thread.Sleep(10);
                Console.WriteLine($"Message : {n} - Thread Id#{Thread.CurrentThread.ManagedThreadId}");
            }, new ExecutionDataflowBlockOptions() {MaxDegreeOfParallelism = 4});

            for (int i = 0; i < 10; i++)
            {
                actionBlock.Post(i);
                Console.WriteLine($"Message {i} processed - queue count {actionBlock.InputCount}");
            }

            actionBlock.Complete();
            actionBlock.Completion.Wait();
        }

        public static void ActionBlock_Linking()
        {
            var actionBlock1 = new ActionBlock<int>(n =>
            {
                Thread.Sleep(100);
                Console.WriteLine($"Message : {n} - Thread Id#{Thread.CurrentThread.ManagedThreadId}");
            });

            var actionBlock2 = new ActionBlock<int>(n =>
            {
                Thread.Sleep(100);
                Console.WriteLine($"Message : {n} - Thread Id#{Thread.CurrentThread.ManagedThreadId}");
            });

            var bcBlock = new TransformBlock<int, int>(n => n);

            bcBlock.LinkTo(actionBlock1, n => n % 2 == 0);
            bcBlock.LinkTo(actionBlock2);

            for (int i = 0; i < 10; i++)
            {
                bcBlock.Post(i);
            }
        }

        public static void ActionBlock_Propagate()
        {
            var source = new BufferBlock<string>();

            var actionBlock = new ActionBlock<string>(n =>
            {
                Thread.Sleep(200);
                Console.WriteLine($"Message : {n} - Thread Id#{Thread.CurrentThread.ManagedThreadId}");
            });

            source.LinkTo(actionBlock, new DataflowLinkOptions()
            {
                PropagateCompletion = true
            });
            for (int i = 0; i < 10; i++)
            {
                source.Post($"Item #{i}");
            }

            actionBlock.Completion.ContinueWith(a => Console.WriteLine("actionBlock completed"));
            source.Complete();
            actionBlock.Completion.Wait();
        }

        public static void TransformBlock_Simple()
        {
            var actionBlock = new ActionBlock<int>(n =>
            {
                Thread.Sleep(100);
                Console.WriteLine($"Message : {n} - Thread Id#{Thread.CurrentThread.ManagedThreadId}");
            });

            var tfBlock = new TransformBlock<int, int>(
                n =>
                {
                    Thread.Sleep(500);
                    return n * n;
                }, new ExecutionDataflowBlockOptions() {MaxDegreeOfParallelism = 1}); // Change DOP

            tfBlock.LinkTo(actionBlock);
            for (int i = 0; i < 10; i++)
            {
                tfBlock.Post(i);
                Console.WriteLine($"Message {i} processed - queue count {actionBlock.InputCount}");
            }

            for (int i = 0; i < 10; i++)
            {
                int result = tfBlock.Receive();
                Console.WriteLine($"Message {i} received - value {result}");
            }
        }

        public static void BatchBlock_Receive()
        {
            var batchBlock = new BatchBlock<int>(2);
            for (int i = 0; i < 10; i++)
            {
                batchBlock.Post(i);
            }

            batchBlock.Complete();

            for (int i = 0; i < 5; i++)
            {
                int[] result = batchBlock.Receive();

                // PROPER if(batchBlock.TryReceive(null,out result)){

                foreach (var r in result)
                {
                    Console.Write(r + " ");
                }

                Console.Write("\n");
            }
        }

        public static void TransformManyBlock_Simple()
        {
            var printAction = new ActionBlock<string>(n =>
            {
                Thread.Sleep(100);
                Console.WriteLine($"Message : {n} - Thread Id#{Thread.CurrentThread.ManagedThreadId}");
            });

            IEnumerable<string> IsEven(int n)
            {
                for (int i = 0; i < n; i++)
                    if (i % 2 == 0)
                        yield return $"{n} : {i}";
            }

            var tfManyBlock = new TransformManyBlock<int, string>(n => IsEven(n));
            tfManyBlock.LinkTo(printAction);

            for (int i = 0; i < 10; i++)
            {
                tfManyBlock.Post(i);
                Console.WriteLine($"Message {i} processed - queue count {printAction.InputCount}");
            }
        }

        public static void BroadcastBlock_Simple()
        {
            var bcBlock = new BroadcastBlock<int>(n => n);
            var actionBlock1 = new ActionBlock<int>(n =>
            {
                Thread.Sleep(100);
                Console.WriteLine($"Message Action block 1: {n} - Thread Id#{Thread.CurrentThread.ManagedThreadId}");
            });
            var actionBlock2 = new ActionBlock<int>(n =>
            {
                Thread.Sleep(100);
                Console.WriteLine($"Message Action block 2: {n} - Thread Id#{Thread.CurrentThread.ManagedThreadId}");
            });

            bcBlock.LinkTo(actionBlock1);
            bcBlock.LinkTo(actionBlock2);

            for (int i = 0; i < 10; i++)
            {
                bcBlock.Post(i);
            }
        }

        public static void JoinBlock_Simple()
        {
            var jBlock = new JoinBlock<string, string>();

            for (int i = 0; i < 10; i++)
            {
                jBlock.Target1.Post($"Message Target 1: {i} - Thread Id#{Thread.CurrentThread.ManagedThreadId}");
            }

            for (int i = 0; i < 10; i++)
            {
                jBlock.Target2.Post($"Message Target 2: {i} - Thread Id#{Thread.CurrentThread.ManagedThreadId}");
            }

            for (int i = 0; i < 10; i++)
            {
                var res = jBlock.Receive();
                Console.WriteLine(res.Item1 + ";" + res.Item2);
            }
        }

        public static void JoinBlock_SendAsync()
        {
            var jBlock = new JoinBlock<string, string>(
                new GroupingDataflowBlockOptions
                {
                    Greedy = false,
                    BoundedCapacity = 2
                });

            for (int i = 0; i < 10; i++)
            {
                Task<bool> task =
                    jBlock.Target1.SendAsync(
                        $"Message Target 1: {i} - Thread Id#{Thread.CurrentThread.ManagedThreadId}");
                int iCopy = i;
                task.ContinueWith(t =>
                {
                    if (t.Result)
                    {
                        Console.WriteLine(
                            $"Message Target 1: {i} ACCEPTED - Thread Id#{Thread.CurrentThread.ManagedThreadId}");
                    }
                    else
                    {
                        Console.WriteLine(
                            $"Message Target 1: {i} REFUSED - Thread Id#{Thread.CurrentThread.ManagedThreadId}");
                    }
                });
            }

            for (int i = 0; i < 10; i++)
            {
                Task<bool> task =
                    jBlock.Target2.SendAsync(
                        $"Message Target 2: {i} - Thread Id#{Thread.CurrentThread.ManagedThreadId}");
                int iCopy = i;
                task.ContinueWith(t =>
                {
                    if (t.Result)
                    {
                        Console.WriteLine(
                            $"Message Target 2: {i} ACCEPTED - Thread Id#{Thread.CurrentThread.ManagedThreadId}");
                    }
                    else
                    {
                        Console.WriteLine(
                            $"Message Target 2: {i} REFUSED - Thread Id#{Thread.CurrentThread.ManagedThreadId}");
                    }
                });
            }


            for (int i = 0; i < 10; i++)
            {
                var res = jBlock.Receive();
                Console.WriteLine(res.Item1 + ";" + res.Item2);
            }
        }
    }
}
