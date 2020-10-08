using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using ParallelPatterns;
using TPLAgent;

namespace  ReactiveAgent
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // AgentAggregate.Run();

            PingPongAgents.Start();

            Console.WriteLine("Finished. Press any key to exit.");
            Console.ReadLine();
        }
    }
}
