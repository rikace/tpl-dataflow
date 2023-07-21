using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ReactiveStock.ActorModel.Actors;
using ReactiveStock.ActorModel.Actors.UI;
using ReactiveStock.ActorModel.Messages;

namespace ReactiveStock
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var chartAgent = new LineChartingActor();
            var stockCoordinator = new StocksCoordinatorActor(chartAgent.Actor);

            var stocks = new List<(string, ConsoleColor)>();
            stocks.Add(("MSFT", ConsoleColor.Red));
            stocks.Add(("APPL", ConsoleColor.Cyan));
            stocks.Add(("GOOG", ConsoleColor.Green));
            //stocks.Add(("AMZN", ConsoleColor.Yellow));

            foreach (var item in stocks)
            {
                stockCoordinator.Actor.Post(new WatchStockMessage(item.Item1, item.Item2));
            }

            Console.ReadLine();
        }
    }
}
