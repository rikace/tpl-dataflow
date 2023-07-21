using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using ParallelPatterns.Common;

namespace Dataflow
{
    public static class WordsCounter
    {
        public static async Task Start(int dop)
        {
            // Create a cancellation token source
            CancellationTokenSource cancellationSource = new CancellationTokenSource();

            var opts = new ExecutionDataflowBlockOptions
            {
                CancellationToken = cancellationSource.Token,
                MaxDegreeOfParallelism = dop
            };

            // Download a book as a string
            var downloadBook = new TransformBlock<string, string>(async uri =>
            {
                Console.WriteLine("Downloading the book...");
                return await new WebClient().DownloadStringTaskAsync(uri);
            }, opts);


            // splits text into an array of strings.
            var createWordList = new TransformBlock<string, string[]>(text =>
            {
                Console.WriteLine("Creating list of words...");

                // Remove punctuation
                char[] tokens = text.ToArray();
                for (int i = 0; i < tokens.Length; i++)
                {
                    if (!char.IsLetter(tokens[i]))
                        tokens[i] = ' ';
                }

                text = new string(tokens);

                return text.Split(new char[] {' '},
                    StringSplitOptions.RemoveEmptyEntries);
            }, opts);

            var broadcast = new BroadcastBlock<string[]>(s => s);
            var accumulator = new BufferBlock<string[]>();

            // Remove short words and return the count
            var filterWordList = new TransformBlock<string[], int>(words =>
            {
                Console.WriteLine("Counting words...");

                var wordList = words.Where(word => word.Length > 3)
                    .OrderBy(word => word)
                    .Distinct().ToArray();
                return wordList.Count();
            }, opts);

            var printWordCount = new ActionBlock<int>(wordcount =>
            {
                Console.WriteLine($"Action -> Found {wordcount} words - Thread Id#{Thread.CurrentThread.ManagedThreadId}");
            }, opts);

            downloadBook.LinkTo(createWordList);
            createWordList.LinkTo(broadcast);
            broadcast.LinkTo(filterWordList);
            broadcast.LinkTo(accumulator);
            filterWordList.LinkTo(printWordCount);

            accumulator
                .AsObservable()
                .Scan(new HashSet<string>(), (state, words) =>
                {
                    state.AddRange(words);
                    return state;
                })
                .Subscribe(words => Console.WriteLine("Observable -> Found {0} words", words.Count));



            try
            {
                Console.WriteLine("Starting...");

                // Download Origin of Species
                await downloadBook.SendAsync("http://www.gutenberg.org/files/2009/2009.txt", cancellationSource.Token);
                await downloadBook.SendAsync("https://www.gutenberg.org/files/4300/4300-0.txt", cancellationSource.Token);
                await downloadBook.SendAsync("https://www.gutenberg.org/files/43/43-0.txt", cancellationSource.Token);
                await downloadBook.SendAsync("https://www.gutenberg.org/files/1184/1184-0.txt", cancellationSource.Token);
                await downloadBook.SendAsync("http://www.gutenberg.org/cache/epub/345/pg345.txt", cancellationSource.Token);


                // Mark the head of the pipeline as complete.
                downloadBook.Complete();

                // Cancel the operation
                // cancellationSource.Cancel();

                // create a continuation task that marks the next block in the pipeline as completed.
                await downloadBook.Completion.ContinueWith(t =>
                {
                    if (t.IsFaulted) ((IDataflowBlock) createWordList).Fault(t.Exception);
                    else createWordList.Complete();
                }, cancellationSource.Token);
                await createWordList.Completion.ContinueWith(t =>
                {
                    if (t.IsFaulted) ((IDataflowBlock) filterWordList).Fault(t.Exception);
                    else filterWordList.Complete();
                }, cancellationSource.Token);
                await filterWordList.Completion.ContinueWith(t =>
                {
                    if (t.IsFaulted) ((IDataflowBlock) printWordCount).Fault(t.Exception);
                    else printWordCount.Complete();
                }, cancellationSource.Token);

                printWordCount.Completion.Wait(cancellationSource.Token);
            }
            catch (AggregateException ae)
            {
                foreach (Exception ex in ae.InnerExceptions)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            finally
            {
                Console.WriteLine("Finished. Press any key to exit.");
            }
        }
    }
}
